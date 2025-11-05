# Internal Contracts Overview

**Feature**: 001-service-monitor  
**Date**: 2025-10-30

## Purpose

This directory contains internal API contracts for the Windows Service Monitoring application. These contracts define the interfaces between architectural layers (Services ↔ UI, Services ↔ Storage).

**Note**: This is a desktop application, not a web service. These are in-process interfaces, not REST APIs.

## Contract Files

1. **IServiceMonitor.md** - Service monitoring operations
2. **IConfigurationManager.md** - Configuration persistence operations
3. **INotificationService.md** - User notification operations
4. **ILocalizationService.md** - UI localization and language switching (added 2025-11-05)
5. **ILogger.md** - Logging abstraction (uses Microsoft.Extensions.Logging)

## Architecture Layers

```
┌─────────────────────────────────────┐
│         UI Layer (Forms)            │
│  - MainForm.cs                      │
│  - ServiceListForm.cs               │
│  - NotificationForm.cs              │
└──────────────┬──────────────────────┘
               │ uses
               ↓
┌─────────────────────────────────────┐
│      Service Layer (Business)       │
│  - ServiceMonitor                   │ implements IServiceMonitor
│  - ConfigurationManager             │ implements IConfigurationManager
│  - NotificationService              │ implements INotificationService
└──────────────┬──────────────────────┘
               │ uses
               ↓
┌─────────────────────────────────────┐
│       Models (Data Structures)      │
│  - MonitoredService                 │
│  - ApplicationConfiguration         │
│  - ServiceStatusChange              │
└─────────────────────────────────────┘
```

## Dependency Injection

### DI Container (Minimal)

No heavy DI framework needed. Use simple constructor injection:

```csharp
// Services/ServiceProviderFactory.cs
public static class ServiceProviderFactory
{
    public static IServiceMonitor CreateServiceMonitor(ILogger logger, IConfigurationManager config)
    {
        return new ServiceMonitor(logger, config);
    }
    
    public static IConfigurationManager CreateConfigurationManager(ILogger logger)
    {
        return new ConfigurationManager(logger);
    }
    
    public static INotificationService CreateNotificationService(ILogger logger)
    {
        return new NotificationService(logger);
    }
}
```

### Initialization in Program.cs

```csharp
static void Main()
{
    // Setup logging
    var logger = LoggerFactory.Create(builder => 
    {
        builder.AddFile("logs/servicewatcher.log");
    }).CreateLogger<Program>();
    
    // Create services
    var configManager = ServiceProviderFactory.CreateConfigurationManager(logger);
    var notificationService = ServiceProviderFactory.CreateNotificationService(logger);
    var serviceMonitor = ServiceProviderFactory.CreateServiceMonitor(logger, configManager);
    
    // Start UI
    Application.Run(new MainForm(serviceMonitor, configManager, notificationService));
}
```

## Contract Versioning

All contracts are versioned. Breaking changes require new interface version:
- `IServiceMonitor` → `IServiceMonitorV2`

Non-breaking changes can extend existing interface.

## Testing Strategy

### Unit Tests
Mock all interfaces using Moq:

```csharp
[Fact]
public void ServiceMonitor_DetectsServiceStop()
{
    // Arrange
    var mockLogger = new Mock<ILogger>();
    var mockConfig = new Mock<IConfigurationManager>();
    var monitor = new ServiceMonitor(mockLogger.Object, mockConfig.Object);
    
    // Act & Assert
    // ...
}
```

### Integration Tests
Use real implementations with test configuration:

```csharp
[Fact]
public void ConfigurationManager_SaveAndLoad_RoundTrip()
{
    var logger = new TestLogger();
    var configManager = new ConfigurationManager(logger);
    var testConfig = new ApplicationConfiguration { /* ... */ };
    
    configManager.Save(testConfig);
    var loaded = configManager.Load();
    
    Assert.Equal(testConfig.MonitoringIntervalSeconds, loaded.MonitoringIntervalSeconds);
}
```

## Contract Principles

1. **Interface Segregation**: Each interface has single responsibility
2. **Dependency Inversion**: UI depends on abstractions, not concrete services
3. **Explicit Error Handling**: All methods document exceptions they throw
4. **Testability**: All interfaces are mockable
5. **Performance**: All async operations return Task<T>
6. **Cancellation**: Long-running operations support CancellationToken

## Error Handling Pattern

All service methods follow this pattern:

```csharp
public async Task<Result<T>> OperationAsync()
{
    try
    {
        // Operation logic
        return Result<T>.Success(value);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        return Result<T>.Failure(ex.Message);
    }
}
```

**Result<T> Type**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = error;
    }
}
```

## Contract Maturity

| Contract | Status | Stability |
|----------|--------|-----------|
| IServiceMonitor | Draft | Stable for MVP |
| IConfigurationManager | Draft | Stable for MVP |
| INotificationService | Draft | May add async in v2 |
| ILogger | External | Stable (Microsoft.Extensions.Logging) |

## Next Steps

1. Review each contract file for completeness
2. Implement concrete classes in Services/ layer
3. Write unit tests for each interface
4. Update contracts based on implementation feedback
