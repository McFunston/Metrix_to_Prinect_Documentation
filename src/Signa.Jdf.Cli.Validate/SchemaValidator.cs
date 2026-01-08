using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Signa.Jdf.Cli.Validate;

public static class SchemaValidator
{
    // XSD validation helper; used for strict schema checks and comparisons.
    private static readonly Dictionary<string, XmlSchemaSet> SchemaCache = new(StringComparer.OrdinalIgnoreCase);

    public static List<ValidationIssue> Validate(XDocument document, string schemaPath)
    {
        var issues = new List<ValidationIssue>();
        if (!File.Exists(schemaPath))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "SCHEMA_NOT_FOUND",
                $"Schema file not found: {schemaPath}"));
            return issues;
        }

        if (!SchemaCache.TryGetValue(schemaPath, out var schemaSet))
        {
            schemaSet = new XmlSchemaSet
            {
                XmlResolver = new XmlUrlResolver()
            };
            schemaSet.Add(null, schemaPath);
            SchemaCache[schemaPath] = schemaSet;
        }

        document.Validate(schemaSet, (sender, args) =>
        {
            var severity = args.Severity == XmlSeverityType.Error
                ? ValidationSeverity.Error
                : ValidationSeverity.Warning;
            issues.Add(new ValidationIssue(
                severity,
                "SCHEMA_VALIDATION",
                args.Message));
        });

        return issues;
    }
}
