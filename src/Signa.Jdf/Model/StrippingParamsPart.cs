namespace Signa.Jdf;

public sealed class StrippingParamsPart
{
    // Flattened stripping params partition used for assembly/fold context.
    public string? WorkStyle { get; init; }
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? BinderySignatureName { get; init; }
    public string? AssemblyIds { get; init; }
    public string? SectionList { get; init; }
}
