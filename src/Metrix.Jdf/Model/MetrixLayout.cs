namespace Metrix.Jdf;

public sealed class MetrixLayout
{
    // Lightweight layout model: Signature -> Sheet -> Surface with mark/content geometry.
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? DescriptiveName { get; set; }
    public string? Status { get; set; }
    public List<MetrixSignature> Signatures { get; } = new();
}

public sealed class MetrixSignature
{
    // Metrix signature grouping (name + sheets) used for partition mapping.
    public string? Name { get; set; }
    public List<MetrixSheet> Sheets { get; } = new();
}

public sealed class MetrixSheet
{
    // Sheet-level layout data including work style and surfaces.
    public string? Name { get; set; }
    public string? WorkStyle { get; set; }
    public string? SurfaceContentsBox { get; set; }
    public List<MetrixSurface> Surfaces { get; } = new();
}

public sealed class MetrixSurface
{
    // Surface corresponds to a side; carries paper dimensions and placements.
    public string? Side { get; set; }
    public string? Dimension { get; set; }
    public string? MediaOrigin { get; set; }
    public string? SurfaceContentsBox { get; set; }
    public List<MetrixMarkObject> MarkObjects { get; } = new();
    public List<MetrixContentObject> ContentObjects { get; } = new();
}

public sealed class MetrixMarkObject
{
    // Minimal mark geometry (CTM/ClipBox/Ord) used to rebuild marks RunList geometry.
    public string? Ord { get; set; }
    public string? Ctm { get; set; }
    public string? ClipBox { get; set; }
}

public sealed class MetrixContentObject
{
    // Content placement data (CTM/TrimCTM/TrimSize) for page list + preview layout.
    public string? Ord { get; set; }
    public string? Ctm { get; set; }
    public string? TrimCtm { get; set; }
    public string? TrimSize { get; set; }
    public string? TrimBox1 { get; set; }
    public string? ClipBox { get; set; }
    public string? Comp { get; set; }
}
