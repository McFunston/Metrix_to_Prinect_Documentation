using System.Xml.Linq;

namespace Signa.Jdf.Cli.Validate;

public static class SchemaComparer
{
    // Compares schema violations across JDF 1.3 vs 1.7 to highlight deltas.
    public static SchemaComparisonResult Compare(
        XDocument document,
        string schema13Path,
        string schema17Path)
    {
        var issues13 = SchemaValidator.Validate(document, schema13Path);
        var issues17 = SchemaValidator.Validate(document, schema17Path);

        var key13 = new HashSet<string>(issues13.Select(issue => issue.Message), StringComparer.Ordinal);
        var key17 = new HashSet<string>(issues17.Select(issue => issue.Message), StringComparer.Ordinal);

        var only13 = key13.Except(key17).ToList();
        var only17 = key17.Except(key13).ToList();
        var both = key13.Intersect(key17).ToList();

        return new SchemaComparisonResult(
            issues13,
            issues17,
            only13,
            only17,
            both);
    }
}

public sealed record SchemaComparisonResult(
    IReadOnlyList<ValidationIssue> Issues13,
    IReadOnlyList<ValidationIssue> Issues17,
    IReadOnlyList<string> MessagesOnly13,
    IReadOnlyList<string> MessagesOnly17,
    IReadOnlyList<string> MessagesInBoth);
