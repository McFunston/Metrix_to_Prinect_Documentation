namespace Signa.Jdf;

public sealed class JdfNode
{
    // Root-level attributes extracted from the JDF header.
    public string? Type { get; init; }
    public string? Types { get; init; }
    public string? JobId { get; init; }
    public string? JobPartId { get; init; }
    public string? Version { get; init; }
    public string? MaxVersion { get; init; }
    public string? IcsVersions { get; init; }
    public string? DescriptiveName { get; init; }
    public string? Id { get; init; }
    public string? Status { get; init; }
}
