using System.ServiceProcess;

namespace ServiceWatcher.Models;

/// <summary>
/// Represents the status of a Windows service.
/// Mirrors ServiceControllerStatus for application use.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// The service status is unknown or unavailable.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The service is stopped.
    /// </summary>
    Stopped = ServiceControllerStatus.Stopped,

    /// <summary>
    /// The service is starting.
    /// </summary>
    StartPending = ServiceControllerStatus.StartPending,

    /// <summary>
    /// The service is stopping.
    /// </summary>
    StopPending = ServiceControllerStatus.StopPending,

    /// <summary>
    /// The service is running.
    /// </summary>
    Running = ServiceControllerStatus.Running,

    /// <summary>
    /// The service is resuming from a paused state.
    /// </summary>
    ContinuePending = ServiceControllerStatus.ContinuePending,

    /// <summary>
    /// The service is pausing.
    /// </summary>
    PausePending = ServiceControllerStatus.PausePending,

    /// <summary>
    /// The service is paused.
    /// </summary>
    Paused = ServiceControllerStatus.Paused
}
