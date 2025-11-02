using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Interface for managing application configuration persistence.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Loads the configuration from the JSON file asynchronously.
    /// </summary>
    /// <returns>Result containing the loaded configuration or error message.</returns>
    Task<Result<ApplicationConfiguration>> LoadAsync();

    /// <summary>
    /// Saves the configuration to the JSON file asynchronously with backup creation.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <returns>Result indicating success or failure with error message.</returns>
    Task<Result<bool>> SaveAsync(ApplicationConfiguration configuration);

    /// <summary>
    /// Creates a default configuration file for first-run scenarios.
    /// </summary>
    /// <returns>Result containing the default configuration or error message.</returns>
    Task<Result<ApplicationConfiguration>> CreateDefaultAsync();

    /// <summary>
    /// Validates the configuration against all validation rules.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>ValidationResult with any validation errors.</returns>
    ValidationResult Validate(ApplicationConfiguration configuration);

    /// <summary>
    /// Attempts to load from backup file if main config is corrupted.
    /// </summary>
    /// <returns>Result containing the backup configuration or error message.</returns>
    Task<Result<ApplicationConfiguration>> TryLoadBackupAsync();

    /// <summary>
    /// Reloads the configuration from file, discarding in-memory changes.
    /// </summary>
    /// <returns>Result containing the reloaded configuration or error message.</returns>
    Task<Result<ApplicationConfiguration>> ReloadAsync();

    /// <summary>
    /// Restores configuration from the most recent backup file.
    /// </summary>
    /// <returns>Result indicating success or failure with error message.</returns>
    Task<Result<bool>> RestoreFromBackupAsync();

    /// <summary>
    /// Checks if a configuration file exists at the expected location.
    /// </summary>
    /// <returns>True if configuration file exists, false otherwise.</returns>
    bool ConfigurationExists();

    /// <summary>
    /// Event raised when configuration is successfully loaded or saved.
    /// </summary>
    event EventHandler<ApplicationConfiguration>? ConfigurationChanged;
}
