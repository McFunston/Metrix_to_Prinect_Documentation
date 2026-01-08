namespace Signa.Jdf;

public sealed class LayoutPart
{
    // Recursive layout partition: Signature -> Sheet -> Side with ContentObject summaries.
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Side { get; init; }
    public string? Name { get; init; }
    public string? DescriptiveName { get; init; }
    public string? SourceWorkStyle { get; init; }
    public string? SurfaceContentsBox { get; init; }
    public string? PaperRect { get; init; }
    public string? PartIdKeys { get; init; }
    public int MarkObjectCount { get; init; }
    public int ContentObjectCount { get; init; }
    public List<ContentObjectInfo> ContentObjects { get; } = new();
    public List<LayoutPart> Children { get; } = new();
}
