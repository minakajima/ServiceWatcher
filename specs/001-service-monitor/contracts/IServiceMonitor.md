# IServiceMonitor Contract

**Version**: 1.0  
**Status**: Draft  
**Last Updated**: 2025-10-30

## Purpose

Defines the interface for monitoring Windows services and detecting status changes. This is the core business logic interface that orchestrates service status polling and change detection.

## Interface Definition

```csharp
namespace ServiceWatcher.Services
{
    /// <summary>
    /// Interface for monitoring Windows services and detecting status changes.
    /// </summary>
    public interface IServiceMonitor : IDisposable
    {
        /// <summary>
        /// Gets the current monitoring state.
        /// </summary>
        bool IsMonitoring { get; }
        
        /// <summary>
        /// Gets the list of services currently being monitored.
        /// </summary>
        IReadOnlyList<MonitoredService> MonitoredServices { get; }
        
        /// <summary>
        /// Event raised when a service status change is detected.
        /// </summary>
        event EventHandler<ServiceStatusChangeEventArgs> ServiceStatusChanged;
        
        /// <summary>
        /// Event raised when monitoring encounters an error.
        /// </summary>
        event EventHandler<MonitoringErrorEventArgs> MonitoringError;
        
        /// <summary>
        /// Starts monitoring all configured services.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel monitoring.</param>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> StartMonitoringAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops monitoring all services.
        /// </summary>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> StopMonitoringAsync();
        
        /// <summary>
        /// Refreshes the list of monitored services from configuration.
        /// </summary>
        /// <returns>Result with count of services loaded.</returns>
        Task<Result<int>> RefreshMonitoredServicesAsync();
        
        /// <summary>
        /// Gets the current status of all monitored services.
        /// </summary>
        /// <returns>List of services with current status.</returns>
        Task<Result<IReadOnlyList<MonitoredService>>> GetServiceStatusesAsync();
        
        /// <summary>
        /// Gets the current status of a specific service.
        /// </summary>
        /// <param name="serviceName">Windows service name.</param>
        /// <returns>Service status or error.</returns>
        Task<Result<MonitoredService>> GetServiceStatusAsync(string serviceName);
        
        /// <summary>
        /// Adds a service to the monitoring list.
        /// </summary>
        /// <param name="service">Service to add.</param>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> AddServiceAsync(MonitoredService service);
        
        /// <summary>
        /// Removes a service from the monitoring list.
        /// </summary>
        /// <param name="serviceName">Service name to remove.</param>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> RemoveServiceAsync(string serviceName);
        
        /// <summary>
        /// Updates monitoring settings (interval, etc.).
        /// </summary>
        /// <param name="intervalSeconds">New monitoring interval in seconds.</param>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> UpdateMonitoringIntervalAsync(int intervalSeconds);
    }
    
    /// <summary>
    /// Event args for service status changes.
    /// </summary>
    public class ServiceStatusChangeEventArgs : EventArgs
    {
        public ServiceStatusChange StatusChange { get; }
        public DateTime Timestamp { get; }
        
        public ServiceStatusChangeEventArgs(ServiceStatusChange statusChange)
        {
            StatusChange = statusChange;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Event args for monitoring errors.
    /// </summary>
    public class MonitoringErrorEventArgs : EventArgs
    {
        public string ServiceName { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }
        
        public MonitoringErrorEventArgs(string serviceName, string errorMessage, Exception? exception = null)
        {
            ServiceName = serviceName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
```

## Usage Example

### Starting Monitoring

```csharp
public class MainForm : Form
{
    private readonly IServiceMonitor _serviceMonitor;
    private CancellationTokenSource _cts;
    
    public MainForm(IServiceMonitor serviceMonitor)
    {
        _serviceMonitor = serviceMonitor;
        
        // Subscribe to events
        _serviceMonitor.ServiceStatusChanged += OnServiceStatusChanged;
        _serviceMonitor.MonitoringError += OnMonitoringError;
    }
    
    private async void StartButton_Click(object sender, EventArgs e)
    {
        _cts = new CancellationTokenSource();
        var result = await _serviceMonitor.StartMonitoringAsync(_cts.Token);
        
        if (result.IsSuccess)
        {
            StatusLabel.Text = "監視中...";
            StartButton.Enabled = false;
            StopButton.Enabled = true;
        }
        else
        {
            MessageBox.Show($"監視開始失敗: {result.ErrorMessage}", "エラー", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void OnServiceStatusChanged(object? sender, ServiceStatusChangeEventArgs e)
    {
        // Update UI on UI thread
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnServiceStatusChanged(sender, e)));
            return;
        }
        
        var change = e.StatusChange;
        if (change.IsStopEvent)
        {
            // Show notification popup
            var notification = new NotificationForm(change);
            notification.Show();
        }
        
        // Update service list grid
        RefreshServiceList();
    }
    
    private void OnMonitoringError(object? sender, MonitoringErrorEventArgs e)
    {
        // Log error, update UI
        LogError($"Service: {e.ServiceName}, Error: {e.ErrorMessage}");
    }
}
```

### Adding a Service

```csharp
private async void AddServiceButton_Click(object sender, EventArgs e)
{
    var serviceName = ServiceNameTextBox.Text.Trim();
    
    // Get all services to validate
    var allServices = ServiceController.GetServices();
    var serviceInfo = allServices.FirstOrDefault(s => 
        s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
    
    if (serviceInfo == null)
    {
        MessageBox.Show($"サービス '{serviceName}' が見つかりません。", "エラー");
        return;
    }
    
    var monitoredService = new MonitoredService
    {
        ServiceName = serviceInfo.ServiceName,
        DisplayName = serviceInfo.DisplayName,
        NotificationEnabled = true
    };
    
    var result = await _serviceMonitor.AddServiceAsync(monitoredService);
    
    if (result.IsSuccess)
    {
        MessageBox.Show("サービスを追加しました。", "成功");
        await RefreshServiceListAsync();
    }
    else
    {
        MessageBox.Show($"追加失敗: {result.ErrorMessage}", "エラー");
    }
}
```

## Implementation Requirements

### Constructor

```csharp
public class ServiceMonitor : IServiceMonitor
{
    private readonly ILogger<ServiceMonitor> _logger;
    private readonly IConfigurationManager _configurationManager;
    private System.Threading.Timer? _monitoringTimer;
    private readonly SemaphoreSlim _monitoringLock = new(1, 1);
    private ApplicationConfiguration _configuration;
    private List<MonitoredService> _monitoredServices = new();
    
    public ServiceMonitor(
        ILogger<ServiceMonitor> logger,
        IConfigurationManager configurationManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationManager = configurationManager ?? 
            throw new ArgumentNullException(nameof(configurationManager));
    }
}
```

### Monitoring Loop

```csharp
private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("Monitoring loop started");
    
    while (!cancellationToken.IsCancellationRequested)
    {
        await _monitoringLock.WaitAsync(cancellationToken);
        try
        {
            await CheckAllServicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitoring loop");
        }
        finally
        {
            _monitoringLock.Release();
        }
        
        await Task.Delay(
            TimeSpan.FromSeconds(_configuration.MonitoringIntervalSeconds),
            cancellationToken);
    }
    
    _logger.LogInformation("Monitoring loop stopped");
}

private async Task CheckAllServicesAsync()
{
    foreach (var service in _monitoredServices.ToList())
    {
        try
        {
            var previousStatus = service.LastKnownStatus;
            var currentStatus = await GetCurrentStatusAsync(service.ServiceName);
            
            service.LastKnownStatus = currentStatus;
            service.LastChecked = DateTime.Now;
            service.IsAvailable = true;
            service.ErrorMessage = null;
            
            // Detect status change
            if (previousStatus != currentStatus && previousStatus != ServiceStatus.Unknown)
            {
                var statusChange = new ServiceStatusChange
                {
                    ServiceName = service.ServiceName,
                    DisplayName = service.DisplayName,
                    PreviousStatus = previousStatus,
                    CurrentStatus = currentStatus,
                    DetectedAt = DateTime.Now
                };
                
                // Raise event
                ServiceStatusChanged?.Invoke(this, 
                    new ServiceStatusChangeEventArgs(statusChange));
            }
        }
        catch (InvalidOperationException ex)
        {
            // Service not found
            service.IsAvailable = false;
            service.ErrorMessage = "サービスが見つかりません";
            _logger.LogWarning(ex, "Service {ServiceName} not found", service.ServiceName);
            
            MonitoringError?.Invoke(this, 
                new MonitoringErrorEventArgs(service.ServiceName, service.ErrorMessage, ex));
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Access denied
            service.IsAvailable = false;
            service.ErrorMessage = "アクセスが拒否されました";
            _logger.LogWarning(ex, "Access denied to service {ServiceName}", service.ServiceName);
            
            MonitoringError?.Invoke(this, 
                new MonitoringErrorEventArgs(service.ServiceName, service.ErrorMessage, ex));
        }
    }
}
```

## Error Handling

### Expected Exceptions

| Exception | Scenario | Handling |
|-----------|----------|----------|
| `InvalidOperationException` | Service not found | Mark as unavailable, continue monitoring others |
| `Win32Exception` | Access denied | Mark as unavailable, log warning |
| `TimeoutException` | Service query timeout | Retry once, then mark unavailable |
| `OperationCanceledException` | Monitoring cancelled | Clean up, log normal shutdown |

### Error Recovery

- **Transient errors**: Retry on next monitoring cycle
- **Persistent errors**: Mark service unavailable, show in UI
- **Fatal errors**: Stop monitoring, notify user

## Performance Requirements

| Operation | Max Duration | Notes |
|-----------|--------------|-------|
| StartMonitoringAsync | <100ms | Initialization only, actual monitoring is async |
| StopMonitoringAsync | <500ms | Wait for current cycle to finish |
| GetServiceStatusAsync | <200ms | Single service query |
| AddServiceAsync | <100ms | Add + save config |
| RefreshMonitoredServicesAsync | <1s | Load + validate all services |

## Thread Safety

- All public methods are thread-safe
- Events are raised on background thread (UI must marshal to UI thread)
- Internal state protected by `SemaphoreSlim`

## Lifecycle

1. **Constructor**: Initialize, load configuration
2. **StartMonitoringAsync**: Start background timer
3. **Monitoring Loop**: Check services on interval
4. **Events**: Notify subscribers of changes
5. **StopMonitoringAsync**: Cancel timer, cleanup
6. **Dispose**: Stop monitoring, release resources

## Testing Checklist

- [ ] Start/stop monitoring
- [ ] Detect service stop event (Running → Stopped)
- [ ] Handle service not found
- [ ] Handle access denied
- [ ] Add/remove services
- [ ] Update monitoring interval
- [ ] Thread safety (concurrent operations)
- [ ] Cancellation token support
- [ ] Event subscription/unsubscription
- [ ] Dispose pattern
- [ ] Memory leaks (long-running monitoring)
- [ ] Performance with 50 services

## Dependencies

- `Microsoft.Extensions.Logging.ILogger<ServiceMonitor>`
- `IConfigurationManager`
- `System.ServiceProcess.ServiceController`
- `System.Threading.Timer`
