using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

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
