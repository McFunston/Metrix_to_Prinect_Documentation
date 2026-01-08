namespace Metrix.Jdf;

public sealed class MetrixRunListResource
{
    // RunList resource with nested entries and optional PageList labels.
    public string? Id { get; set; }
    public string? PartIdKeys { get; set; }
    public string? DescriptiveName { get; set; }
    public string? Status { get; set; }
    public string? NPage { get; set; }
    public List<MetrixRunListEntry> Entries { get; } = new();
    public List<MetrixPageData> PageList { get; } = new();
}

public sealed class MetrixRunListEntry
{
    // RunList entry for marks/document, including FileSpec and separations.
    public string? Pages { get; set; }
    public string? NPage { get; set; }
    public string? Run { get; set; }
    public string? Status { get; set; }
    public bool IsBlank { get; set; }
    public string? FileSpecUrl { get; set; }
    public List<MetrixSeparationSpec> SeparationSpecs { get; } = new();
}

public sealed class MetrixSeparationSpec
{
    // SeparationSpec placeholder for marks RunList (HDM extensions included).
    public string? Name { get; set; }
    public string? HdmType { get; set; }
    public string? HdmSubType { get; set; }
    public string? HdmIsMapRel { get; set; }
}

public sealed class MetrixPageData
{
    // PageList metadata used for page assignment labels.
    public string? PageIndex { get; set; }
    public string? DescriptiveName { get; set; }
}

public sealed class MetrixResourceLink
{
    // ResourceLink entry captured for RunList lookup and wiring checks.
    public string? LinkType { get; set; }
    public string? Usage { get; set; }
    public string? ProcessUsage { get; set; }
    public string? RefId { get; set; }
}
