namespace Signa.Jdf;

public sealed class ConventionalPrintingParamsPart
{
    // Flattened printing params partition used to resolve WorkStyle by part.
    public string? WorkStyle { get; init; }
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Side { get; init; }
}
