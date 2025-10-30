using System.ServiceProcess;
using ServiceWatcher.Models;

namespace ServiceWatcher.Utils;

/// <summary>
/// Extension methods for ServiceController.
/// </summary>
public static class ServiceControllerExtensions
{
    /// <summary>
    /// Converts ServiceControllerStatus to application ServiceStatus enum.
    /// </summary>
    /// <param name="status">The ServiceControllerStatus to convert.</param>
    /// <returns>The corresponding ServiceStatus value.</returns>
    public static ServiceStatus ToServiceStatus(this ServiceControllerStatus status)
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

    /// <summary>
    /// Safely gets the status of a service controller, returning Unknown on error.
    /// </summary>
    /// <param name="controller">The ServiceController instance.</param>
    /// <returns>The service status, or Unknown if an error occurred.</returns>
    public static ServiceStatus GetStatusSafely(this ServiceController controller)
    {
        try
        {
            controller.Refresh();
            return controller.Status.ToServiceStatus();
        }
        catch
        {
            return ServiceStatus.Unknown;
        }
    }
}
