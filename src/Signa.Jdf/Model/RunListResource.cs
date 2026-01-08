namespace Signa.Jdf;

public sealed class RunListResource
{
    // Top-level RunList (Document/Marks/PagePool) with nested partitions.
    public string? Id { get; init; }
    public string? PartIdKeys { get; init; }
    public string? DescriptiveName { get; init; }
    public string? Status { get; init; }
    public string? LogicalPage { get; init; }
    public string? Pages { get; init; }
    public string? NPage { get; init; }
    public string? FileSpecUrl { get; init; }
    public string? FileSpecMimeType { get; init; }
    public string? LayoutElementType { get; init; }
    public string? HdmOfw { get; init; }
    public List<SeparationSpecInfo> SeparationSpecs { get; } = new();
    public List<RunListPart> Parts { get; } = new();
}
