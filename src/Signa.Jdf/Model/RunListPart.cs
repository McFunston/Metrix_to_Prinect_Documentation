namespace Signa.Jdf;

public sealed class RunListPart
{
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Side { get; init; }
    public string? LogicalPage { get; init; }
    public string? Pages { get; init; }
    public string? Run { get; init; }
    public string? NPage { get; init; }
    public string? FileSpecUrl { get; init; }
    public string? FileSpecMimeType { get; init; }
    public string? LayoutElementType { get; init; }
    public List<SeparationSpecInfo> SeparationSpecs { get; } = new();
    public List<RunListPart> Children { get; } = new();
}
