namespace Signa.Jdf;

public sealed class ResourceLink
{
    // ResourceLink entries wire RunLists/Media/Layout to the process chain.
    public string? LinkType { get; init; }
    public string? ProcessUsage { get; init; }
    public string? CombinedProcessIndex { get; init; }
    public string? Usage { get; init; }
    public string? RefId { get; init; }
}
