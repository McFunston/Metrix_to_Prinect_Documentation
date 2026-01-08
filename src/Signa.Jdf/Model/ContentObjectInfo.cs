namespace Signa.Jdf;

public sealed class ContentObjectInfo
{
    // Lightweight content object metadata used for page list validation.
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Side { get; init; }
    public string? Ord { get; init; }
    public string? DescriptiveName { get; init; }
    public string? AssemblyFrontBack { get; init; }
    public string? JobPart { get; init; }
    public string? RunlistIndex { get; init; }
}
