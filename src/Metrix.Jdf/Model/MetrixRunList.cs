namespace Metrix.Jdf;

public sealed class MetrixRunListResource
{
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
    public string? Name { get; set; }
    public string? HdmType { get; set; }
    public string? HdmSubType { get; set; }
    public string? HdmIsMapRel { get; set; }
}

public sealed class MetrixPageData
{
    public string? PageIndex { get; set; }
    public string? DescriptiveName { get; set; }
}

public sealed class MetrixResourceLink
{
    public string? LinkType { get; set; }
    public string? Usage { get; set; }
    public string? ProcessUsage { get; set; }
    public string? RefId { get; set; }
}
