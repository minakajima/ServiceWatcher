namespace ServiceWatcher.Utils;

/// <summary>
/// Represents the result of a validation operation with multiple error messages.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation passed.
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Gets the list of validation error messages.
    /// </summary>
    public List<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of ValidationResult.
    /// </summary>
    public ValidationResult()
    {
        Errors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of ValidationResult with existing errors.
    /// </summary>
    /// <param name="errors">Initial error messages.</param>
    public ValidationResult(IEnumerable<string> errors)
    {
        Errors = new List<string>(errors);
    }

    /// <summary>
    /// Adds an error message to the validation result.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// Gets a combined error message with all errors.
    /// </summary>
    /// <returns>A single string containing all error messages.</returns>
    public string GetCombinedErrorMessage()
    {
        return string.Join(Environment.NewLine, Errors);
    }

    /// <summary>
    /// Creates a successful validation result (no errors).
    /// </summary>
    public static ValidationResult Success() => new ValidationResult();

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Failure(string error) => new ValidationResult(new[] { error });

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors) => new ValidationResult(errors);
}
