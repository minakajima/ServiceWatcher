# Data Model: Windowsサービス監視システム

**Feature**: 001-service-monitor  
**Date**: 2025-10-30

## Overview

This document defines the core data structures, their relationships, validation rules, and serialization formats for the Windows service monitoring application.

## Domain Entities

### 1. MonitoredService (監視対象サービス)

Represents a Windows service that is being monitored for status changes.

#### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| ServiceName | string | Yes | - | Internal Windows service name (e.g., "wuauserv") |
| DisplayName | string | Yes | - | Human-readable service name (e.g., "Windows Update") |
| NotificationEnabled | bool | Yes | true | Whether to show notifications for this service |
| LastKnownStatus | ServiceStatus | No | Unknown | Last recorded service status |
| LastChecked | DateTime | No | null | Timestamp of last status check |
| IsAvailable | bool | Yes | true | Whether service is accessible (exists and permissions allow) |
| ErrorMessage | string | No | null | Error message if service is unavailable |

#### C# Implementation

```csharp
public class MonitoredService
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool NotificationEnabled { get; set; } = true;
    public ServiceStatus LastKnownStatus { get; set; } = ServiceStatus.Unknown;
    public DateTime? LastChecked { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? ErrorMessage { get; set; }
    
    // Navigation property (not persisted)
    [JsonIgnore]
    public ServiceController? ServiceController { get; set; }
}
```

#### Validation Rules

- **ServiceName**: 
  - Must not be null or whitespace
  - Max length: 256 characters
  - Must match Windows service naming rules (alphanumeric, spaces, hyphens, underscores)
  - Pattern: `^[a-zA-Z0-9_ -]+$`

- **DisplayName**:
  - Must not be null or whitespace
  - Max length: 256 characters

- **LastChecked**:
  - Must be in the past or null
  - Must not be more than 1 hour in the past (stale data indicator)

#### Business Rules

1. If `IsAvailable == false`, `LastKnownStatus` should be `ServiceStatus.Unknown`
2. If `NotificationEnabled == false`, no notifications are sent for this service
3. `LastChecked` must be updated on every monitoring cycle
4. `ErrorMessage` must be set when `IsAvailable == false`

---

### 2. ApplicationConfiguration (設定情報)

Represents the application's configuration settings, persisted to a JSON file.

#### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| MonitoringIntervalSeconds | int | Yes | 5 | Interval between service status checks (seconds) |
| NotificationDisplayTimeSeconds | int | Yes | 30 | How long to display notification popups (seconds) |
| UiLanguage | string | Yes | "ja" or "en" | UI display language (detected from OS on first launch) |
| MonitoredServices | List<MonitoredService> | Yes | [] | List of services to monitor |
| ConfigurationVersion | string | Yes | "1.0" | Config file format version |
| LastModified | DateTime | Yes | DateTime.Now | Last time config was modified |
| StartMinimized | bool | Yes | false | Start application minimized to tray |
| AutoStartMonitoring | bool | Yes | false | Start monitoring on application launch |

#### C# Implementation

```csharp
public class ApplicationConfiguration
{
    public int MonitoringIntervalSeconds { get; set; } = 5;
    public int NotificationDisplayTimeSeconds { get; set; } = 30;
    public string UiLanguage { get; set; } = DetectDefaultLanguage(); // "ja" or "en"
    public List<MonitoredService> MonitoredServices { get; set; } = new();
    public string ConfigurationVersion { get; set; } = "1.0";
    public DateTime LastModified { get; set; } = DateTime.Now;
    public bool StartMinimized { get; set; } = false;
    public bool AutoStartMonitoring { get; set; } = false;
    
    private static string DetectDefaultLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return culture == "ja" ? "ja" : "en"; // Default to English for non-Japanese OS
    }
}
```

#### Validation Rules

- **MonitoringIntervalSeconds**:
  - Min: 1 second
  - Max: 3600 seconds (1 hour)
  - Recommended: 5-30 seconds

- **NotificationDisplayTimeSeconds**:
  - Min: 5 seconds
  - Max: 300 seconds (5 minutes)
  - 0 = infinite (manual close only)

- **UiLanguage**:
  - Must be exactly "ja" or "en" (case-sensitive)
  - Invalid values should fallback to "en"
  - Auto-detected on first launch based on OS language

- **MonitoredServices**:
  - Max count: 50 services (performance constraint from constitution)
  - Must not contain duplicate ServiceName entries

- **ConfigurationVersion**:
  - Must match supported versions: "1.0"

#### Business Rules

1. Changes to configuration must update `LastModified` timestamp
2. Configuration must be validated before saving
3. Invalid configuration should not overwrite existing valid configuration
4. Default configuration is created on first run if file doesn't exist

---

### 3. ServiceStatusChange (通知イベント)

Represents a detected change in service status that triggers a notification.

#### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| ServiceName | string | Yes | - | Service that changed |
| DisplayName | string | Yes | - | Human-readable service name |
| PreviousStatus | ServiceStatus | Yes | - | Status before change |
| CurrentStatus | ServiceStatus | Yes | - | Status after change |
| DetectedAt | DateTime | Yes | DateTime.Now | When change was detected |
| NotificationShown | bool | Yes | false | Whether notification was displayed |
| UserAcknowledged | bool | Yes | false | Whether user closed notification |

#### C# Implementation

```csharp
public class ServiceStatusChange
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ServiceStatus PreviousStatus { get; set; }
    public ServiceStatus CurrentStatus { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.Now;
    public bool NotificationShown { get; set; } = false;
    public bool UserAcknowledged { get; set; } = false;
    
    // Helper property
    [JsonIgnore]
    public bool IsStopEvent => 
        PreviousStatus == ServiceStatus.Running && 
        CurrentStatus == ServiceStatus.Stopped;
}
```

#### Validation Rules

- **ServiceName**, **DisplayName**: Same as MonitoredService
- **DetectedAt**: Must be in the past or DateTime.Now
- **PreviousStatus**, **CurrentStatus**: Must be different (represents a change)

#### Business Rules

1. Only "Running → Stopped" transitions trigger notifications (MVP scope)
2. Events are not persisted to disk (in-memory only)
3. Events are logged to application log file
4. Maximum event history: 100 most recent events (in-memory circular buffer)

---

## Enumerations

### ServiceStatus

Maps to `System.ServiceProcess.ServiceControllerStatus` with additional custom states.

```csharp
public enum ServiceStatus
{
    Unknown = 0,           // Service state cannot be determined
    Stopped = 1,           // Service is stopped
    StartPending = 2,      // Service is starting
    StopPending = 3,       // Service is stopping
    Running = 4,           // Service is running
    ContinuePending = 5,   // Service is resuming
    PausePending = 6,      // Service is pausing
    Paused = 7,            // Service is paused
    Unavailable = 99       // Service not accessible (custom state)
}
```

**Mapping from ServiceControllerStatus**:
```csharp
public static ServiceStatus FromServiceControllerStatus(ServiceControllerStatus status)
{
    return status switch
    {
        ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
        ServiceControllerStatus.StartPending => ServiceStatus.StartPending,
        ServiceControllerStatus.StopPending => ServiceStatus.StopPending,
        ServiceControllerStatus.Running => ServiceStatus.Running,
        ServiceControllerStatus.ContinuePending => ServiceStatus.ContinuePending,
        ServiceControllerStatus.PausePending => ServiceStatus.PausePending,
        ServiceControllerStatus.Paused => ServiceStatus.Paused,
        _ => ServiceStatus.Unknown
    };
}
```

---

## Relationships

```
ApplicationConfiguration (1)
    ↓ contains
MonitoredServices (0..*)
    ↓ generates
ServiceStatusChange (0..*)
```

### Relationship Rules

1. **ApplicationConfiguration → MonitoredService**: One-to-many composition
   - Lifecycle: MonitoredServices are owned by configuration
   - Persistence: Saved together in single JSON file

2. **MonitoredService → ServiceStatusChange**: One-to-many association
   - Lifecycle: Independent (events are transient)
   - Persistence: Events not persisted, only logged

---

## Serialization

### JSON Format

**Configuration File** (`config.json`):

```json
{
  "configurationVersion": "1.0",
  "lastModified": "2025-10-30T12:34:56Z",
  "monitoringIntervalSeconds": 5,
  "notificationDisplayTimeSeconds": 30,
  "startMinimized": false,
  "autoStartMonitoring": true,
  "monitoredServices": [
    {
      "serviceName": "wuauserv",
      "displayName": "Windows Update",
      "notificationEnabled": true,
      "lastKnownStatus": 4,
      "lastChecked": "2025-10-30T12:35:00Z",
      "isAvailable": true,
      "errorMessage": null
    },
    {
      "serviceName": "MSSQLSERVER",
      "displayName": "SQL Server (MSSQLSERVER)",
      "notificationEnabled": true,
      "lastKnownStatus": 1,
      "lastChecked": "2025-10-30T12:35:00Z",
      "isAvailable": true,
      "errorMessage": null
    }
  ]
}
```

### Serialization Configuration

```csharp
public static class JsonConfig
{
    public static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
}
```

### File Locations

- **Configuration File**: `%LOCALAPPDATA%\ServiceWatcher\config.json`
- **Log File**: `%LOCALAPPDATA%\ServiceWatcher\logs\servicewatcher.log`
- **Backup Configuration**: `%LOCALAPPDATA%\ServiceWatcher\config.backup.json` (created before saving)

---

## Migration Strategy

### Version 1.0 → Future Versions

If configuration format changes in future versions:

1. Check `configurationVersion` field
2. Apply migration transformations
3. Update `configurationVersion` to new version
4. Save migrated configuration

```csharp
public interface IConfigurationMigrator
{
    bool CanMigrate(string fromVersion, string toVersion);
    ApplicationConfiguration Migrate(string json, string fromVersion);
}
```

---

## Validation Implementation

### Configuration Validator

```csharp
public class ConfigurationValidator
{
    public ValidationResult Validate(ApplicationConfiguration config)
    {
        var errors = new List<string>();
        
        // Validate monitoring interval
        if (config.MonitoringIntervalSeconds < 1 || config.MonitoringIntervalSeconds > 3600)
            errors.Add("MonitoringIntervalSeconds must be between 1 and 3600");
        
        // Validate notification time
        if (config.NotificationDisplayTimeSeconds < 0 || config.NotificationDisplayTimeSeconds > 300)
            errors.Add("NotificationDisplayTimeSeconds must be between 0 and 300");
        
        // Validate service count
        if (config.MonitoredServices.Count > 50)
            errors.Add("Cannot monitor more than 50 services");
        
        // Validate duplicate services
        var duplicates = config.MonitoredServices
            .GroupBy(s => s.ServiceName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        if (duplicates.Any())
            errors.Add($"Duplicate services: {string.Join(", ", duplicates)}");
        
        // Validate each service
        foreach (var service in config.MonitoredServices)
        {
            if (string.IsNullOrWhiteSpace(service.ServiceName))
                errors.Add("Service name cannot be empty");
            
            if (service.ServiceName.Length > 256)
                errors.Add($"Service name too long: {service.ServiceName}");
        }
        
        return new ValidationResult(errors);
    }
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; }
    
    public ValidationResult(List<string> errors)
    {
        Errors = errors;
    }
}
```

---

## Performance Considerations

### Memory Estimates

| Entity | Typical Size | Max Count | Total Memory |
|--------|--------------|-----------|--------------|
| MonitoredService | ~500 bytes | 50 | 25 KB |
| ApplicationConfiguration | ~26 KB | 1 | 26 KB |
| ServiceStatusChange | ~300 bytes | 100 | 30 KB |
| **Total** | | | **~81 KB** |

Well within the <50MB memory constraint from constitution.

### Serialization Performance

- **Load config**: <10ms for 50 services
- **Save config**: <20ms for 50 services (includes backup creation)
- **Validation**: <1ms for 50 services

---

## Error Handling

### Invalid Configuration File

**Scenario**: config.json is corrupted or invalid JSON

**Action**:
1. Log error
2. Attempt to load `config.backup.json`
3. If backup also invalid, create default configuration
4. Notify user of configuration reset

### Service Not Found

**Scenario**: Monitored service doesn't exist on the system

**Action**:
1. Set `MonitoredService.IsAvailable = false`
2. Set `ErrorMessage = "Service not found"`
3. Continue monitoring other services
4. Show warning in UI (but don't popup notification)

### Access Denied

**Scenario**: User lacks permissions to query service status

**Action**:
1. Set `MonitoredService.IsAvailable = false`
2. Set `ErrorMessage = "Access denied"`
3. Continue monitoring other services
4. Show warning in UI with recommendation to run as administrator

---

## Testing Checklist

- [ ] Serialize/deserialize all entities to/from JSON
- [ ] Validate all validation rules
- [ ] Test configuration migration (future)
- [ ] Test error handling for corrupted JSON
- [ ] Test max service count enforcement (50 services)
- [ ] Test duplicate service name detection
- [ ] Verify memory usage stays under 50MB with 50 services
- [ ] Test configuration backup/restore
- [ ] Validate ServiceStatus enum mapping
- [ ] Test file access errors (readonly, missing directory)
