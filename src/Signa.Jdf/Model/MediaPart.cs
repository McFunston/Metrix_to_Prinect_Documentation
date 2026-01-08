namespace Signa.Jdf;

public sealed class MediaPart
{
    // Media partition keyed by Signature/Sheet for dimension overrides.
    public string? SignatureName { get; init; }
    public string? SheetName { get; init; }
    public string? Dimension { get; init; }
}
