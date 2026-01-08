namespace Signa.Jdf;

public sealed class SheetSizeInfo
{
    // Convenience container for sheet-level size hints.
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Side { get; init; }
    public string? SurfaceContentsBox { get; init; }
    public string? PaperRect { get; init; }
}
