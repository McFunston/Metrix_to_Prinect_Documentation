using System.Text;
using System.Xml.Linq;
using Signa.Jdf;

namespace Signa.Jdf.Cli.Validate;

public static class BatchValidator
{
    public static int Run(
        string rootPath,
        string csvPath,
        string textPath,
        bool includeWarnings,
        string? schemaPath,
        bool schemaOnly,
        string? schemaCsvPath,
        string? schemaTextPath)
    {
        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Directory not found: {rootPath}");
            return 1;
        }

        var validator = new SignaJdfValidator();
        var csvLines = new List<string> { "file,severity,code,message,context,job_shape,output_block_expansions,montage" };
        var textBuilder = new StringBuilder();
        var schemaCsvLines = new List<string> { "file,severity,code,message,context,job_shape" };
        var schemaTextBuilder = new StringBuilder();
        var totalIssues = 0;
        var schemaIssueCount = 0;
        var issueRecords = new List<BatchIssueRecord>();

        foreach (var path in Directory.EnumerateFiles(rootPath, "*.jdf", SearchOption.AllDirectories))
        {
            var document = JdfParser.Parse(path);
            var jobContext = ValidationContextBuilder.BuildJobContext(document);
            var jobShape = ValidationContextBuilder.BuildJobShape(document);
            var isMontage = ValidationContextBuilder.IsMontage(document);
            var issues = validator.Validate(document).ToList();
            var schemaIssues = new List<ValidationIssue>();
            if (schemaPath is not null)
            {
                schemaIssues = SchemaValidator.Validate(document.XmlDocument, schemaPath);
            }

            var filteredIssues = issues
                .Where(issue => includeWarnings || issue.Severity == ValidationSeverity.Error)
                .ToList();
            var filteredSchemaIssues = schemaIssues
                .Where(issue => includeWarnings || issue.Severity == ValidationSeverity.Error)
                .ToList();

            if (schemaPath is not null && filteredSchemaIssues.Count > 0)
            {
                schemaTextBuilder.AppendLine(path);
                schemaTextBuilder.AppendLine($"  Context: {jobContext}");
                foreach (var issue in filteredSchemaIssues)
                {
                    schemaTextBuilder.AppendLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
                    if (!string.IsNullOrWhiteSpace(issue.Context))
                    {
                        schemaTextBuilder.AppendLine($"    Hint: {issue.Context}");
                    }
                    schemaCsvLines.Add($"{Escape(path)},{issue.Severity},{Escape(issue.Code)},{Escape(issue.Message)},{Escape(issue.Context ?? string.Empty)},{Escape(jobShape)}");
                    schemaIssueCount++;
                }

                schemaTextBuilder.AppendLine();
            }

            var combinedIssues = schemaOnly
                ? filteredSchemaIssues
                : filteredIssues;

            if (combinedIssues.Count == 0)
            {
                continue;
            }

            textBuilder.AppendLine(path);
            textBuilder.AppendLine($"  Context: {jobContext}");
            AppendOutputBlockExpansion(textBuilder, document);
            AppendMinimalImportabilitySummary(textBuilder, combinedIssues);
            var outputBlockExpansion = BuildOutputBlockExpansionString(document);
            foreach (var issue in combinedIssues)
            {
                textBuilder.AppendLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
                if (!string.IsNullOrWhiteSpace(issue.Context))
                {
                    textBuilder.AppendLine($"    Hint: {issue.Context}");
                }
                csvLines.Add($"{Escape(path)},{issue.Severity},{Escape(issue.Code)},{Escape(issue.Message)},{Escape(issue.Context ?? string.Empty)},{Escape(jobShape)},{Escape(outputBlockExpansion)},{isMontage.ToString().ToLowerInvariant()}");
                issueRecords.Add(new BatchIssueRecord(path, jobShape, isMontage, issue));
                totalIssues++;
            }

            textBuilder.AppendLine();
        }

        AppendJobShapeSummary(textBuilder, issueRecords);
        AppendMontageSummary(textBuilder, issueRecords);

        File.WriteAllLines(csvPath, csvLines);
        File.WriteAllText(textPath, textBuilder.ToString());
        if (schemaPath is not null && schemaIssueCount > 0)
        {
            var schemaCsvOut = schemaCsvPath ?? "schema-summary.csv";
            var schemaTextOut = schemaTextPath ?? "schema-summary.txt";
            File.WriteAllLines(schemaCsvOut, schemaCsvLines);
            File.WriteAllText(schemaTextOut, schemaTextBuilder.ToString());
        }

        Console.WriteLine($"Validated JDFs under: {rootPath}");
        Console.WriteLine($"Issues recorded: {totalIssues}");
        if (schemaPath is not null && schemaIssueCount > 0)
        {
            Console.WriteLine($"Schema issues recorded: {schemaIssueCount}");
        }
        Console.WriteLine($"CSV: {csvPath}");
        Console.WriteLine($"Text: {textPath}");
        if (schemaPath is not null && schemaIssueCount > 0)
        {
            Console.WriteLine($"Schema CSV: {schemaCsvPath ?? "schema-summary.csv"}");
            Console.WriteLine($"Schema Text: {schemaTextPath ?? "schema-summary.txt"}");
        }

        return 0;
    }

    private static void AppendJobShapeSummary(StringBuilder builder, List<BatchIssueRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        builder.AppendLine("Issue summary by job shape:");

        var shapeGroups = records
            .GroupBy(record => record.JobShape)
            .OrderByDescending(group => group.Count())
            .ToList();

        foreach (var shapeGroup in shapeGroups)
        {
            var fileCount = shapeGroup.Select(record => record.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            builder.AppendLine($"  {shapeGroup.Key} (issues={shapeGroup.Count()}, files={fileCount})");

            foreach (var issueGroup in shapeGroup
                .GroupBy(record => new { record.Issue.Severity, record.Issue.Code })
                .OrderByDescending(group => group.Count())
                .Take(15))
            {
                builder.AppendLine($"    [{issueGroup.Key.Severity}] {issueGroup.Key.Code}: {issueGroup.Count()}");
            }
        }
    }

    private static void AppendOutputBlockExpansion(StringBuilder builder, JdfDocument document)
    {
        var expansionString = BuildOutputBlockExpansionString(document);
        if (string.IsNullOrWhiteSpace(expansionString))
        {
            return;
        }

        builder.AppendLine("  OutputBlock expansion:");
        foreach (var line in expansionString.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.AppendLine($"    {line}");
        }
    }

    private static void AppendMinimalImportabilitySummary(StringBuilder builder, List<ValidationIssue> issues)
    {
        var summary = issues.FirstOrDefault(issue =>
            string.Equals(issue.Code, "MINIMAL_IMPORTABILITY_SUMMARY", StringComparison.OrdinalIgnoreCase));
        if (summary is null || string.IsNullOrWhiteSpace(summary.Context))
        {
            return;
        }

        builder.AppendLine("  Minimal importability missing:");
        foreach (var item in summary.Context.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.AppendLine($"    - {item.Trim()}");
        }
    }

    private static string BuildOutputBlockExpansionString(JdfDocument document)
    {
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return string.Empty;
        }

        var hdm = document.HdmNamespace;
        var topLevel = root.Descendants(hdm + "CombiningParams")
            .Where(element => element.Parent?.Name != hdm + "CombiningParams")
            .ToList();
        if (topLevel.Count == 0)
        {
            return string.Empty;
        }

        var expansions = new Dictionary<OutputBlockKey, HashSet<string>>();
        foreach (var combining in topLevel)
        {
            var outputName = combining.Attribute(hdm + "OutputBlockName")?.Value;
            if (string.IsNullOrWhiteSpace(outputName))
            {
                continue;
            }

            foreach (var part in combining.Descendants(hdm + "CombiningParams"))
            {
                var blockName = part.Attribute("BlockName")?.Value;
                if (string.IsNullOrWhiteSpace(blockName))
                {
                    continue;
                }

                var (signatureName, sheetName) = GetCombiningPartNames(part, hdm);
                var key = new OutputBlockKey(outputName, signatureName, sheetName);
                if (!expansions.TryGetValue(key, out var blocks))
                {
                    blocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    expansions[key] = blocks;
                }

                blocks.Add(blockName);
            }
        }

        if (expansions.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var entry in expansions
            .OrderBy(item => item.Key.OutputName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.SignatureName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.SheetName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            var parts = string.Empty;
            if (!string.IsNullOrWhiteSpace(entry.Key.SignatureName) || !string.IsNullOrWhiteSpace(entry.Key.SheetName))
            {
                parts = $" (SignatureName={entry.Key.SignatureName ?? "-"}, SheetName={entry.Key.SheetName ?? "-"})";
            }

            lines.Add($"{entry.Key.OutputName}{parts} -> {string.Join(", ", entry.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}");
        }

        return string.Join("\n", lines);
    }

    private static (string? signatureName, string? sheetName) GetCombiningPartNames(XElement element, XNamespace hdm)
    {
        string? signatureName = null;
        string? sheetName = null;
        foreach (var ancestor in element.AncestorsAndSelf(hdm + "CombiningParams"))
        {
            signatureName ??= ancestor.Attribute("SignatureName")?.Value;
            sheetName ??= ancestor.Attribute("SheetName")?.Value;
        }

        return (signatureName, sheetName);
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static void AppendMontageSummary(StringBuilder builder, List<BatchIssueRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        builder.AppendLine("Issue summary by montage flag:");

        foreach (var montageGroup in records
            .GroupBy(record => record.IsMontage)
            .OrderByDescending(group => group.Count()))
        {
            var fileCount = montageGroup.Select(record => record.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            builder.AppendLine($"  montage={montageGroup.Key.ToString().ToLowerInvariant()} (issues={montageGroup.Count()}, files={fileCount})");

            foreach (var issueGroup in montageGroup
                .GroupBy(record => new { record.Issue.Severity, record.Issue.Code })
                .OrderByDescending(group => group.Count())
                .Take(10))
            {
                builder.AppendLine($"    [{issueGroup.Key.Severity}] {issueGroup.Key.Code}: {issueGroup.Count()}");
            }
        }
    }

    private sealed record BatchIssueRecord(string FilePath, string JobShape, bool IsMontage, ValidationIssue Issue);

    private sealed record OutputBlockKey(string OutputName, string? SignatureName, string? SheetName);
}
