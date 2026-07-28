namespace ModularityKit.Mutator.Abstractions.Results;

/// <summary>
/// Encapsulates the result of a mutation validation, including errors, warnings, and informational messages.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ValidationResult"/> collects detailed validation feedback during mutation processing.
/// It distinguishes between three severity levels:
/// <list type="bullet">
/// <item><description><see cref="Errors"/> - critical issues that prevent mutation execution.</description></item>
/// <item><description><see cref="Warnings"/> - non-blocking issues that may indicate potential problems.</description></item>
/// <item><description><see cref="Info"/> - informational messages about validation context or suggestions.</description></item>
/// </list>
/// </para>
/// <para>
/// The <see cref="IsValid"/> property returns <c>true</c> only when no errors are present.
/// Static factory methods (<see cref="Success"/>, <see cref="WithError"/>, <see cref="WithErrors"/>)
/// provide convenient creation patterns.
/// </para>
/// </remarks>
public sealed class ValidationResult
{
    private static readonly ValidationResult _success = new();
    private readonly List<ValidationError> _errors = [];
    private readonly List<ValidationWarning> _warnings = [];
    private readonly List<ValidationInfo> _info = [];

    /// <summary>
    /// Indicates whether the validation passed (no errors).
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// List of validation errors that prevent mutation execution.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>
    /// List of validation warnings (non-blocking issues).
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings => _warnings;

    /// <summary>
    /// List of informational validation messages.
    /// </summary>
    public IReadOnlyList<ValidationInfo> Info => _info;

    /// <summary>
    /// Adds validation error with the specified path, message, and optional code.
    /// </summary>
    /// <param name="path">Path to the invalid property or field.</param>
    /// <param name="message">Description of the validation error.</param>
    /// <param name="code">Optional error code for categorization or localization.</param>
    public void AddError(string path, string message, string? code = null)
        => _errors.Add(new ValidationError(path, message, code));

    /// <summary>
    /// Adds validation warning with the specified path, message, and optional code.
    /// </summary>
    /// <param name="path">Path to the property or field causing the warning.</param>
    /// <param name="message">Description of the warning.</param>
    /// <param name="code">Optional code for categorization or localization.</param>
    public void AddWarning(string path, string message, string? code = null)
        => _warnings.Add(new ValidationWarning(path, message, code));

    /// <summary>
    /// Adds an informational validation message.
    /// </summary>
    /// <param name="path">Path to the property or concept for this informational message.</param>
    /// <param name="message">The informational message.</param>
    public void AddInfo(string path, string message)
        => _info.Add(new ValidationInfo(path, message));

    /// <summary>
    /// Adds a <see cref="ValidationError"/> instance directly.
    /// </summary>
    /// <param name="error">The validation error to add.</param>
    public void AddError(ValidationError error) => _errors.Add(error);

    /// <summary>
    /// Adds a <see cref="ValidationWarning"/> instance directly.
    /// </summary>
    /// <param name="warning">The validation warning to add.</param>
    public void AddWarning(ValidationWarning warning) => _warnings.Add(warning);

    /// <summary>
    /// Adds a <see cref="ValidationInfo"/> instance directly.
    /// </summary>
    /// <param name="info">The informational message to add.</param>
    public void AddInfo(ValidationInfo info) => _info.Add(info);

    /// <summary>
    /// Returns a successful (empty) validation result.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> with no errors, warnings, or info.</returns>
    public static ValidationResult Success() => _success;

    /// <summary>
    /// Creates a validation result with a single error.
    /// </summary>
    /// <param name="path">Path to the invalid property or field.</param>
    /// <param name="message">Description of the error.</param>
    /// <param name="code">Optional error code for categorization or localization.</param>
    /// <returns>A <see cref="ValidationResult"/> with the specified error.</returns>
    public static ValidationResult WithError(string path, string message, string? code = null)
    {
        var result = new ValidationResult();
        result.AddError(path, message, code);
        return result;
    }

    /// <summary>
    /// Creates a validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The validation errors to include.</param>
    /// <returns>A <see cref="ValidationResult"/> with the specified errors.</returns>
    public static ValidationResult WithErrors(params ValidationError[] errors)
    {
        var result = new ValidationResult();
        foreach (var error in errors)
            result.AddError(error);
        return result;
    }
}
