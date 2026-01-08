namespace Signa.Jdf;

public sealed class SignaJobInfo
{
    // Signa job metadata (job-part names) extracted from Layout/SignaJob.
    public IReadOnlyList<string> JobParts { get; init; } = Array.Empty<string>();
}
