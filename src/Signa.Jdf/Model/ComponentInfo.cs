namespace Signa.Jdf;

public sealed class ComponentInfo
{
    // Component tree entries used for finishing outputs and folding dimensions.
    public string? Id { get; init; }
    public string? Class { get; init; }
    public string? ComponentType { get; init; }
    public string? PartIdKeys { get; init; }
    public string? ProductTypeDetails { get; init; }
    public string? Status { get; init; }
    public string? AssemblyIds { get; init; }
    public string? Dimensions { get; init; }
    public string? ProductType { get; init; }
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? BlockName { get; init; }
    public string? Side { get; init; }
    public string? HdmClosedFoldingSheetDimensions { get; init; }
    public string? HdmOpenedFoldingSheetDimensions { get; init; }
    public string? HdmIsCover { get; init; }
    public List<ComponentInfo> Children { get; } = new();
}
