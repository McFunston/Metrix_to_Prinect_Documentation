namespace Signa.Jdf;

public enum ValidationSeverity
{
    Error,
    Warning,
    Investigation
}

public sealed class ValidationIssue
{
    public ValidationIssue(
        ValidationSeverity severity,
        string code,
        string message,
        string? context = null)
    {
        Severity = severity;
        Code = code;
        Message = message;
        Context = context;
    }

    public ValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string? Context { get; }
}
