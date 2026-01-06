namespace Signa.Jdf;

public sealed class MediaResource
{
    public string? Id { get; init; }
    public string? MediaType { get; init; }
    public string? Dimension { get; init; }
    public string? Thickness { get; init; }
    public string? Weight { get; init; }
    public string? PartIdKeys { get; init; }
    public string? HdmLeadingEdge { get; init; }
    public List<MediaPart> Parts { get; } = new();
}
