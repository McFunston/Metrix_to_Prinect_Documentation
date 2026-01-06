namespace Metrix.Jdf;

public sealed class MetrixLayout
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? DescriptiveName { get; set; }
    public string? Status { get; set; }
    public List<MetrixSignature> Signatures { get; } = new();
}

public sealed class MetrixSignature
{
    public string? Name { get; set; }
    public List<MetrixSheet> Sheets { get; } = new();
}

public sealed class MetrixSheet
{
    public string? Name { get; set; }
    public string? WorkStyle { get; set; }
    public string? SurfaceContentsBox { get; set; }
    public List<MetrixSurface> Surfaces { get; } = new();
}

public sealed class MetrixSurface
{
    public string? Side { get; set; }
    public string? Dimension { get; set; }
    public string? MediaOrigin { get; set; }
    public string? SurfaceContentsBox { get; set; }
    public List<MetrixMarkObject> MarkObjects { get; } = new();
    public List<MetrixContentObject> ContentObjects { get; } = new();
}

public sealed class MetrixMarkObject
{
    public string? Ord { get; set; }
    public string? Ctm { get; set; }
    public string? ClipBox { get; set; }
}

public sealed class MetrixContentObject
{
    public string? Ord { get; set; }
    public string? Ctm { get; set; }
    public string? TrimCtm { get; set; }
    public string? TrimSize { get; set; }
    public string? TrimBox1 { get; set; }
    public string? ClipBox { get; set; }
    public string? Comp { get; set; }
}
