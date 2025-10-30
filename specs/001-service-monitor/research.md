# Research: Windowsサービス監視システム

**Feature**: 001-service-monitor  
**Date**: 2025-10-30  
**Status**: Complete

## Overview

This document contains research findings and technical decisions for implementing a Windows service monitoring application with popup notifications. All NEEDS CLARIFICATION items from the Technical Context have been resolved.

## Research Areas

### 1. Windows Service Monitoring API

**Decision**: Use System.ServiceProcess.ServiceController

**Rationale**:
- Native .NET API specifically designed for Windows service management
- Provides direct access to Service Control Manager (SCM)
- No additional dependencies required
- Well-documented and stable API since .NET Framework 2.0
- Supports all required operations: enumerate services, query status, watch for changes

**Alternatives Considered**:
- **WMI (Windows Management Instrumentation)**: More powerful but adds complexity and overhead. Can be used for event-driven monitoring in future iterations.
- **Win32 API (P/Invoke)**: Lower-level access but requires manual memory management and more complex code. Unnecessary for current requirements.
- **PowerShell Integration**: Adds runtime dependency and performance overhead. ServiceController is more efficient.

**Implementation Notes**:
```csharp
// Enumerate all services
ServiceController[] services = ServiceController.GetServices();

// Get specific service and monitor status
ServiceController sc = new ServiceController("ServiceName");
ServiceControllerStatus status = sc.Status;

// Refresh to get current state
sc.Refresh();
```

### 2. UI Framework Selection

**Decision**: Windows Forms (WinForms)

**Rationale**:
- Lightweight and fast startup time (critical for monitoring tool)
- Simple API for popup notifications and forms
- Native look and feel on Windows
- Smaller deployment size compared to WPF
- Sufficient for the application's UI requirements

**Alternatives Considered**:
- **WPF (Windows Presentation Foundation)**: More modern UI capabilities but heavier runtime, longer startup time, and unnecessary complexity for simple notification popups.
- **UWP/WinUI 3**: Modern Windows UI but requires Windows 10 1809+ and adds deployment complexity. Overkill for system tray application.
- **Avalonia/MAUI**: Cross-platform but adds unnecessary dependencies since target is Windows-only.

**Implementation Notes**:
- Use `Form.ShowDialog()` for modal notification popups
- Use `NotifyIcon` for system tray integration (future enhancement)
- Keep UI thread responsive by running monitoring on background thread

### 3. Configuration File Format

**Decision**: JSON with System.Text.Json

**Rationale**:
- Human-readable and editable
- Native support in .NET (System.Text.Json) - no external dependencies
- Better performance than XML
- Industry standard for configuration files
- Easy to version control and diff

**Alternatives Considered**:
- **XML**: More verbose, harder to edit manually, slower parsing
- **YAML**: Requires external library (YamlDotNet), adds deployment size
- **INI files**: Limited structure, no arrays support, outdated format
- **Binary format**: Not human-readable, defeats purpose of file-based config

**Configuration Schema**:
```json
{
  "monitoringInterval": 5,
  "notificationDisplayTime": 30,
  "monitoredServices": [
    {
      "serviceName": "wuauserv",
      "displayName": "Windows Update",
      "notificationEnabled": true
    }
  ]
}
```

### 4. Polling Strategy vs Event-Driven Monitoring

**Decision**: Polling-based with configurable interval (5 seconds default)

**Rationale**:
- Simpler implementation for MVP
- Predictable resource usage
- Meets <1 second notification requirement (5-second polling + 1 second detection)
- Reliable across all Windows versions
- No dependency on WMI eventing

**Alternatives Considered**:
- **WMI Event Subscriptions**: More efficient but adds complexity, requires elevated permissions in some cases, and can be unreliable on older systems.
- **File System Watchers**: Not applicable to service monitoring
- **Windows Event Log Monitoring**: Delayed notifications, requires parsing event logs

**Future Enhancement**: Add optional WMI event-driven mode for users who need absolute minimal latency.

**Implementation Notes**:
- Use `System.Threading.Timer` for polling
- Maintain previous state to detect transitions
- Only trigger notifications on "Running" → "Stopped" transitions

### 5. Background Operation & Threading

**Decision**: Use BackgroundWorker or Task-based async pattern

**Rationale**:
- Keep UI responsive during monitoring operations
- Prevent blocking on Windows API calls
- Standard .NET pattern for background operations
- Easy cancellation support

**Implementation Pattern**:
```csharp
private async Task MonitorServicesAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        await Task.Run(() => CheckAllServices());
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
    }
}
```

### 6. Logging Framework

**Decision**: Microsoft.Extensions.Logging with File Provider

**Rationale**:
- Standard .NET logging abstraction
- Can swap providers without code changes
- Minimal dependencies (only Microsoft.Extensions.Logging.Abstractions)
- Built-in support for structured logging
- Can add console, file, or event log providers as needed

**Alternatives Considered**:
- **NLog**: Feature-rich but adds external dependency
- **Serilog**: Powerful but unnecessary for simple file logging
- **Custom implementation**: Reinventing the wheel, error-prone

**Log Levels**:
- **Error**: Failed to access service, configuration errors
- **Warning**: Service not found, permission issues
- **Information**: Service state changes, configuration reloads
- **Debug**: Detailed monitoring loop information (optional)

### 7. Error Handling & Graceful Degradation

**Decision**: Try-catch around all Windows API calls with specific exception handling

**Exception Handling Strategy**:
```csharp
try
{
    var sc = new ServiceController(serviceName);
    var status = sc.Status;
}
catch (InvalidOperationException ex)
{
    // Service not found or doesn't exist
    Logger.Warning($"Service {serviceName} not found: {ex.Message}");
    // Remove from monitoring list or mark as unavailable
}
catch (System.ComponentModel.Win32Exception ex)
{
    // Access denied or SCM unavailable
    Logger.Warning($"Cannot access service {serviceName}: {ex.Message}");
    // Continue monitoring other services
}
```

**Rationale**:
- Constitution requires handling: permission errors, service not found, SCM unavailable
- Application must remain functional even if some services are inaccessible
- Provide clear feedback to user about what cannot be monitored

### 8. Testing Strategy

**Decision**: xUnit with Moq for unit tests, manual integration tests

**Unit Testing**:
- Mock ServiceController behavior for service state transitions
- Test configuration loading/saving with in-memory JSON
- Test notification logic independently

**Integration Testing**:
- Manual tests with real Windows services
- Create test service (simple Windows service) for automated integration tests
- Test permission scenarios (admin vs non-admin)

**Test Coverage Goals**:
- 80%+ coverage for Services layer
- 60%+ coverage for Models layer
- UI layer tested manually (form interactions)

## Technical Decisions Summary

| Area | Decision | Key Benefit |
|------|----------|-------------|
| Service API | System.ServiceProcess.ServiceController | Native, no dependencies, well-supported |
| UI Framework | Windows Forms | Lightweight, fast, sufficient for needs |
| Configuration | JSON with System.Text.Json | Human-readable, native support, no dependencies |
| Monitoring | Polling (5s interval) | Simple, reliable, predictable |
| Threading | Task-based async/await | Responsive UI, standard .NET pattern |
| Logging | Microsoft.Extensions.Logging | Standard abstraction, minimal dependencies |
| Testing | xUnit + Moq | Industry standard, good .NET integration |

## Architecture Patterns

### Separation of Concerns

```
UI Layer (Forms)
    ↓ calls
Service Layer (Business Logic)
    ↓ uses
Models (Data Structures)
    ↓ uses
Utils (Helpers)
```

### Dependency Injection

Use minimal DI for testability:
- Services injected into UI forms
- Configuration manager injected into services
- Logging injected into all layers

**Implementation**: Use simple constructor injection, no DI container needed for small app.

### Observer Pattern

```
ServiceMonitor (Subject)
    ↓ notifies
NotificationService (Observer)
    ↓ displays
NotificationForm (UI)
```

## Performance Considerations

1. **Service Enumeration**: Cache service list, only enumerate when user requests refresh
2. **Status Checks**: Batch status checks for multiple services in single loop
3. **Memory Management**: Dispose ServiceController instances after use
4. **Thread Pool**: Use async/await to avoid thread pool starvation

## Security Considerations

1. **Permissions**: Run as normal user by default, detect and handle permission errors gracefully
2. **Configuration File**: Validate all input, reject malformed JSON
3. **Service Names**: Sanitize service names to prevent injection attacks
4. **Error Messages**: Don't expose sensitive system information in UI

## Deployment Strategy

**MVP (Initial Release)**:
- Single executable (.exe) with embedded dependencies
- Config file generated on first run
- Manual deployment (copy to Program Files or user directory)

**Future Enhancement**:
- MSI installer with optional Windows Service mode
- Auto-update capability
- Per-user vs per-machine installation options

## Open Questions Resolved

All questions from Technical Context have been resolved:

✅ **Language/Version**: C# 12 / .NET 8.0  
✅ **Primary Dependencies**: System.ServiceProcess, Windows Forms, System.Text.Json  
✅ **Storage**: JSON file  
✅ **Testing**: xUnit + Moq  
✅ **Target Platform**: Windows 10+, Server 2016+  
✅ **Performance Goals**: <1s notification, <1% CPU, <50MB memory  
✅ **Constraints**: No admin required, offline, polling-based  
✅ **Scale/Scope**: 50 services max, single-user desktop app

## References

- [ServiceController Class Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.serviceprocess.servicecontroller)
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Windows Forms Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
- [Microsoft.Extensions.Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
