using System.ComponentModel;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Monitors Windows services and detects status changes.
/// </summary>
public class ServiceMonitor : IServiceMonitor
{
    private readonly ILogger<ServiceMonitor> _logger;
    private readonly List<MonitoredService> _monitoredServices;
    private readonly object _lock = new object();
    private System.Threading.Timer? _monitoringTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _monitoringIntervalSeconds = 5;
    private bool _isDisposed;

    /// <summary>
    /// Event raised when a service status change is detected.
    /// </summary>
    public event EventHandler<ServiceStatusChangeEventArgs>? ServiceStatusChanged;

    /// <summary>
    /// Event raised when monitoring encounters an error.
    /// </summary>
    public event EventHandler<MonitoringErrorEventArgs>? MonitoringError;

    /// <summary>
    /// Gets the current monitoring state.
    /// </summary>
    public bool IsMonitoring { get; private set; }

    /// <summary>
    /// Gets the list of services currently being monitored.
    /// </summary>
    public IReadOnlyList<MonitoredService> MonitoredServices
    {
        get
        {
            lock (_lock)
            {
                return _monitoredServices.AsReadOnly();
            }
        }
    }

    public ServiceMonitor(ILogger<ServiceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoredServices = new List<MonitoredService>();
    }

    /// <summary>
    /// Starts monitoring all configured services.
    /// </summary>
    public async Task<Result<bool>> StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsMonitoring)
            {
                return Result<bool>.Failure("Monitoring is already active");
            }

            lock (_lock)
            {
                if (_monitoredServices.Count == 0)
                {
                    return Result<bool>.Failure("No services configured for monitoring");
                }
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsMonitoring = true;

            // Initialize current status for all services
            await CheckAllServicesAsync();

            // Start timer-based polling
            var intervalMs = _monitoringIntervalSeconds * 1000;
            _monitoringTimer = new System.Threading.Timer(
                async _ => await CheckAllServicesAsync(),
                null,
                intervalMs,
                intervalMs);

            _logger.LogInformation($"Started monitoring {_monitoredServices.Count} services with {_monitoringIntervalSeconds}s interval");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring");
            IsMonitoring = false;
            return Result<bool>.Failure(ex);
        }
    }

    /// <summary>
    /// Stops monitoring all services.
    /// </summary>
    public async Task<Result<bool>> StopMonitoringAsync()
    {
        try
        {
            if (!IsMonitoring)
            {
                return Result<bool>.Failure("Monitoring is not active");
            }

            _cancellationTokenSource?.Cancel();
            
            if (_monitoringTimer != null)
            {
                await _monitoringTimer.DisposeAsync();
                _monitoringTimer = null;
            }

            IsMonitoring = false;
            _logger.LogInformation("Stopped monitoring services");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring");
            return Result<bool>.Failure(ex);
        }
    }

    /// <summary>
    /// Checks all monitored services and raises events for status changes.
    /// </summary>
    private async Task CheckAllServicesAsync()
    {
        if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
        {
            return;
        }

        List<MonitoredService> servicesToCheck;
        lock (_lock)
        {
            servicesToCheck = new List<MonitoredService>(_monitoredServices);
        }

        foreach (var service in servicesToCheck)
        {
            try
            {
                var currentStatus = await GetCurrentStatusAsync(service.ServiceName);
                
                if (currentStatus != service.LastKnownStatus && service.LastKnownStatus != ServiceStatus.Unknown)
                {
                    // Status changed - raise event
                    var statusChange = new ServiceStatusChange
                    {
                        ServiceName = service.ServiceName,
                        DisplayName = service.DisplayName,
                        PreviousStatus = service.LastKnownStatus,
                        CurrentStatus = currentStatus,
                        DetectedAt = DateTime.Now,
                        NotificationShown = false,
                        UserAcknowledged = false
                    };

                    service.LastKnownStatus = currentStatus;
                    service.LastChecked = DateTime.Now;

                    _logger.LogInformation($"Service '{service.ServiceName}' status changed: {statusChange.PreviousStatus} → {statusChange.CurrentStatus}");
                    
                    // Raise event on thread pool to avoid blocking monitoring
                    _ = Task.Run(() => ServiceStatusChanged?.Invoke(this, new ServiceStatusChangeEventArgs(statusChange)));
                }
                else
                {
                    service.LastKnownStatus = currentStatus;
                    service.LastChecked = DateTime.Now;
                }

                service.IsAvailable = true;
                service.ErrorMessage = null;
            }
            catch (InvalidOperationException ex)
            {
                // Service not found
                HandleServiceError(service, "Service not found", ex);
            }
            catch (Win32Exception ex)
            {
                // Access denied or other Win32 error
                HandleServiceError(service, "Access denied or Win32 error", ex);
            }
            catch (Exception ex)
            {
                // Other unexpected errors
                HandleServiceError(service, "Unexpected error", ex);
            }
        }
    }

    /// <summary>
    /// Gets the current status of a specific service.
    /// </summary>
    private async Task<ServiceStatus> GetCurrentStatusAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            using var controller = new ServiceController(serviceName);
            controller.Refresh();
            return controller.Status.ToServiceStatus();
        });
    }

    /// <summary>
    /// Handles errors when checking service status.
    /// </summary>
    private void HandleServiceError(MonitoredService service, string errorType, Exception ex)
    {
        service.IsAvailable = false;
        service.ErrorMessage = $"{errorType}: {ex.Message}";
        service.LastKnownStatus = ServiceStatus.Unknown;
        service.LastChecked = DateTime.Now;

        _logger.LogWarning(ex, $"Error checking service '{service.ServiceName}': {errorType}");
        
        var errorArgs = new MonitoringErrorEventArgs(service.ServiceName, service.ErrorMessage, ex);
        _ = Task.Run(() => MonitoringError?.Invoke(this, errorArgs));
    }

    /// <summary>
    /// Refreshes the list of monitored services from configuration.
    /// </summary>
    public async Task<Result<int>> RefreshMonitoredServicesAsync()
    {
        // This will be implemented when ConfigurationManager is available (US2)
        await Task.CompletedTask;
        return Result<int>.Success(_monitoredServices.Count);
    }

    /// <summary>
    /// Gets the current status of all monitored services.
    /// </summary>
    public async Task<Result<IReadOnlyList<MonitoredService>>> GetServiceStatusesAsync()
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            return Result<IReadOnlyList<MonitoredService>>.Success(_monitoredServices.AsReadOnly());
        }
    }

    /// <summary>
    /// Gets the current status of a specific service.
    /// </summary>
    public async Task<Result<MonitoredService>> GetServiceStatusAsync(string serviceName)
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            var service = _monitoredServices.FirstOrDefault(s => 
                s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            
            if (service == null)
            {
                return Result<MonitoredService>.Failure($"Service '{serviceName}' is not in the monitoring list");
            }

            return Result<MonitoredService>.Success(service);
        }
    }

    /// <summary>
    /// Adds a service to the monitoring list.
    /// </summary>
    public async Task<Result<bool>> AddServiceAsync(MonitoredService service)
    {
        if (service == null)
        {
            return Result<bool>.Failure("Service cannot be null");
        }

        if (!service.IsValid())
        {
            return Result<bool>.Failure("Service data is invalid");
        }

        lock (_lock)
        {
            if (_monitoredServices.Any(s => s.ServiceName.Equals(service.ServiceName, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<bool>.Failure($"Service '{service.ServiceName}' is already being monitored");
            }

            _monitoredServices.Add(service);
        }

        _logger.LogInformation($"Added service '{service.ServiceName}' to monitoring list");
        await Task.CompletedTask;
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Removes a service from the monitoring list.
    /// </summary>
    public async Task<Result<bool>> RemoveServiceAsync(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return Result<bool>.Failure("Service name cannot be empty");
        }

        lock (_lock)
        {
            var service = _monitoredServices.FirstOrDefault(s => 
                s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            
            if (service == null)
            {
                return Result<bool>.Failure($"Service '{serviceName}' is not in the monitoring list");
            }

            _monitoredServices.Remove(service);
        }

        _logger.LogInformation($"Removed service '{serviceName}' from monitoring list");
        await Task.CompletedTask;
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Updates monitoring settings (interval, etc.).
    /// </summary>
    public async Task<Result<bool>> UpdateMonitoringIntervalAsync(int intervalSeconds)
    {
        if (intervalSeconds < 1 || intervalSeconds > 3600)
        {
            return Result<bool>.Failure("Monitoring interval must be between 1 and 3600 seconds");
        }

        _monitoringIntervalSeconds = intervalSeconds;

        // If monitoring is active, restart the timer with new interval
        if (IsMonitoring && _monitoringTimer != null)
        {
            var wasMonitoring = IsMonitoring;
            await StopMonitoringAsync();
            
            if (wasMonitoring)
            {
                await StartMonitoringAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            }
        }

        _logger.LogInformation($"Updated monitoring interval to {intervalSeconds} seconds");
        return Result<bool>.Success(true);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _monitoringTimer?.Dispose();
        _cancellationTokenSource?.Dispose();
        _isDisposed = true;
    }
}
