using Signa.Jdf;
using Signa.Jdf.Cli.Validate;

if (args.Length < 1 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    PrintUsage();
    return 1;
}

if (args.Contains("--batch", StringComparer.OrdinalIgnoreCase))
{
    return RunBatch(args);
}

if (args.Contains("--schema-compare", StringComparer.OrdinalIgnoreCase))
{
    return RunSchemaCompare(args);
}

return RunSingle(args);

static int RunSingle(string[] args)
{
    var path = args[0];
    var document = JdfParser.Parse(path);
    var validator = new SignaJdfValidator();
    var issues = validator.Validate(document).ToList();
    var schemaPath = GetSchemaPath(args);
    var schemaOnly = args.Contains("--schema-only", StringComparer.OrdinalIgnoreCase);
    if (schemaPath is not null && !File.Exists(schemaPath))
    {
        Console.WriteLine($"Schema not found: {schemaPath}");
        return 1;
    }
    if (schemaPath is not null)
    {
        var schemaIssues = SchemaValidator.Validate(document.XmlDocument, schemaPath);
        if (schemaOnly)
        {
            issues = schemaIssues;
        }
        else
        {
            issues.AddRange(schemaIssues);
        }
    }
    var jobContext = ValidationContextBuilder.BuildJobContext(document);

    Console.WriteLine($"Context: {jobContext}");
    PrintRunListSummary(document);
    PrintComponentSummary(document);

    if (issues.Count == 0)
    {
        Console.WriteLine("No validation issues found.");
        return 0;
    }
    foreach (var issue in issues)
    {
        Console.WriteLine($"[{issue.Severity}] {issue.Code}: {issue.Message}");
        if (!string.IsNullOrWhiteSpace(issue.Context))
        {
            Console.WriteLine($"  Hint: {issue.Context}");
        }
    }

    return issues.Any(issue => issue.Severity == ValidationSeverity.Error) ? 1 : 0;
}

static void PrintRunListSummary(JdfDocument document)
{
    if (document.RunLists.Count == 0)
    {
        return;
    }

    Console.WriteLine($"RunLists: {document.RunLists.Count}");
    foreach (var runList in document.RunLists)
    {
        var parts = new List<string>
        {
            $"id={runList.Id ?? "(none)"}"
        };
        AppendRunListMeta(parts, runList.FileSpecUrl, runList.FileSpecMimeType, runList.LayoutElementType, runList.HdmOfw, runList.NPage, runList.Pages, runList.LogicalPage);
        if (runList.Parts.Count > 0)
        {
            parts.Add($"parts={runList.Parts.Count}");
        }

        Console.WriteLine($"  - {string.Join(", ", parts)}");

        foreach (var part in runList.Parts)
        {
            PrintRunListPart(part, 2);
        }
    }
}

static void PrintComponentSummary(JdfDocument document)
{
    if (document.Components.Count == 0)
    {
        return;
    }

    var componentCount = 0;
    var coverCount = 0;
    var bodyCount = 0;
    var signatureCount = 0;
    var sheetCount = 0;
    var blockCount = 0;

    foreach (var component in document.Components)
    {
        CountComponents(component, ref componentCount, ref coverCount, ref bodyCount, ref signatureCount, ref sheetCount, ref blockCount);
    }

    Console.WriteLine($"Components: total={componentCount}, cover={coverCount}, body={bodyCount}, signatures={signatureCount}, sheets={sheetCount}, blocks={blockCount}");
}

static void CountComponents(
    ComponentInfo component,
    ref int componentCount,
    ref int coverCount,
    ref int bodyCount,
    ref int signatureCount,
    ref int sheetCount,
    ref int blockCount)
{
    componentCount++;

    if (string.Equals(component.ProductType, "Cover", StringComparison.OrdinalIgnoreCase))
    {
        coverCount++;
    }
    else if (string.Equals(component.ProductType, "Body", StringComparison.OrdinalIgnoreCase))
    {
        bodyCount++;
    }

    if (!string.IsNullOrWhiteSpace(component.SignatureName))
    {
        signatureCount++;
    }

    if (!string.IsNullOrWhiteSpace(component.SheetName))
    {
        sheetCount++;
    }

    if (!string.IsNullOrWhiteSpace(component.BlockName))
    {
        blockCount++;
    }

    foreach (var child in component.Children)
    {
        CountComponents(child, ref componentCount, ref coverCount, ref bodyCount, ref signatureCount, ref sheetCount, ref blockCount);
    }
}

static void PrintRunListPart(RunListPart part, int indent)
{
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(part.SignatureName))
    {
        parts.Add($"sig={part.SignatureName}");
    }
    if (!string.IsNullOrWhiteSpace(part.SheetName))
    {
        parts.Add($"sheet={part.SheetName}");
    }
    if (!string.IsNullOrWhiteSpace(part.Side))
    {
        parts.Add($"side={part.Side}");
    }

    AppendRunListMeta(parts, part.FileSpecUrl, part.FileSpecMimeType, part.LayoutElementType, null, part.NPage, part.Pages, part.LogicalPage);

    if (parts.Count > 0)
    {
        Console.WriteLine($"{new string(' ', indent)}- {string.Join(", ", parts)}");
    }

    foreach (var child in part.Children)
    {
        PrintRunListPart(child, indent + 2);
    }
}

static void AppendRunListMeta(
    List<string> parts,
    string? fileSpecUrl,
    string? mimeType,
    string? layoutElementType,
    string? hdmOfw,
    string? nPage,
    string? pages,
    string? logicalPage)
{
    if (!string.IsNullOrWhiteSpace(fileSpecUrl))
    {
        parts.Add($"url={fileSpecUrl}");
    }
    if (!string.IsNullOrWhiteSpace(mimeType))
    {
        parts.Add($"mime={mimeType}");
    }
    if (!string.IsNullOrWhiteSpace(layoutElementType))
    {
        parts.Add($"layoutType={layoutElementType}");
    }
    if (!string.IsNullOrWhiteSpace(hdmOfw))
    {
        parts.Add($"hdmOfw={hdmOfw}");
    }
    if (!string.IsNullOrWhiteSpace(nPage))
    {
        parts.Add($"npage={nPage}");
    }
    if (!string.IsNullOrWhiteSpace(pages))
    {
        parts.Add($"pages={pages}");
    }
    if (!string.IsNullOrWhiteSpace(logicalPage))
    {
        parts.Add($"logicalPage={logicalPage}");
    }
}

static int RunBatch(string[] args)
{
    var root = GetValue(args, "--batch") ?? ".";
    var csv = GetValue(args, "--csv") ?? "validation-summary.csv";
    var text = GetValue(args, "--text") ?? "validation-summary.txt";
    var includeWarnings = args.Contains("--include-warnings", StringComparer.OrdinalIgnoreCase);
    var schemaPath = GetSchemaPath(args);
    var schemaOnly = args.Contains("--schema-only", StringComparer.OrdinalIgnoreCase);
    var schemaCsv = GetValue(args, "--schema-csv");
    var schemaText = GetValue(args, "--schema-text");
    if (schemaPath is not null && !File.Exists(schemaPath))
    {
        Console.WriteLine($"Schema not found: {schemaPath}");
        return 1;
    }

    return BatchValidator.Run(root, csv, text, includeWarnings, schemaPath, schemaOnly, schemaCsv, schemaText);
}

static int RunSchemaCompare(string[] args)
{
    var path = GetSchemaComparePath(args);
    if (string.IsNullOrWhiteSpace(path))
    {
        Console.WriteLine("Schema compare requires a JDF path.");
        return 1;
    }

    var schema13Path = GetSchemaPathValue(args, "1.3");
    var schema17Path = GetSchemaPathValue(args, "1.7");
    if (!File.Exists(schema13Path))
    {
        Console.WriteLine($"Schema not found: {schema13Path}");
        return 1;
    }
    if (!File.Exists(schema17Path))
    {
        Console.WriteLine($"Schema not found: {schema17Path}");
        return 1;
    }

    var document = JdfParser.Parse(path);
    var result = SchemaComparer.Compare(document.XmlDocument, schema13Path, schema17Path);

    Console.WriteLine($"Schema comparison for {path}:");
    Console.WriteLine($"  1.3 issues: {result.Issues13.Count}");
    Console.WriteLine($"  1.7 issues: {result.Issues17.Count}");
    Console.WriteLine($"  Unique to 1.3: {result.MessagesOnly13.Count}");
    Console.WriteLine($"  Unique to 1.7: {result.MessagesOnly17.Count}");
    Console.WriteLine($"  Shared: {result.MessagesInBoth.Count}");

    PrintMessageSummary("Unique to 1.3 (top 10)", result.MessagesOnly13);
    PrintMessageSummary("Unique to 1.7 (top 10)", result.MessagesOnly17);
    PrintMessageSummary("Shared (top 10)", result.MessagesInBoth);

    return 0;
}

static string? GetSchemaComparePath(string[] args)
{
    var schemaCompareIndex = Array.FindIndex(args, arg => string.Equals(arg, "--schema-compare", StringComparison.OrdinalIgnoreCase));
    if (schemaCompareIndex >= 0 && schemaCompareIndex + 1 < args.Length)
    {
        return args[schemaCompareIndex + 1];
    }

    return args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase));
}

static string GetSchemaPathValue(string[] args, string fallback)
{
    var value = GetValue(args, "--schema");
    if (string.IsNullOrWhiteSpace(value))
    {
        value = fallback;
    }

    var normalized = value.Trim();
    var rootDir = Directory.GetCurrentDirectory();
    return normalized switch
    {
        "1.3" or "1_3" => Path.Combine(rootDir, "JDF_Schema", "Version_1_3", "JDF.xsd"),
        "1.7" or "1_7" => Path.Combine(rootDir, "JDF_Schema", "Version_1_7", "JDF.xsd"),
        _ => normalized
    };
}

static void PrintMessageSummary(string title, IReadOnlyList<string> messages)
{
    if (messages.Count == 0)
    {
        return;
    }

    Console.WriteLine(title + ":");
    foreach (var message in messages.OrderBy(message => message, StringComparer.Ordinal).Take(10))
    {
        Console.WriteLine($"  - {message}");
    }
}

static string? GetValue(string[] args, string flag)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    if (index < 0 || index + 1 >= args.Length)
    {
        return null;
    }

    return args[index + 1];
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  signa-jdf-validate <path-to-jdf>");
    Console.WriteLine("  signa-jdf-validate <path-to-jdf> [--schema <1.3|1.7|path>] [--schema-only] [--schema-csv <path>] [--schema-text <path>]");
    Console.WriteLine("  signa-jdf-validate --batch <dir> [--csv <path>] [--text <path>] [--include-warnings] [--schema <1.3|1.7|path>] [--schema-only] [--schema-csv <path>] [--schema-text <path>]");
    Console.WriteLine("  signa-jdf-validate --schema-compare <path-to-jdf>");
}

static string? GetSchemaPath(string[] args)
{
    var value = GetValue(args, "--schema");
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var normalized = value.Trim();
    var rootDir = Directory.GetCurrentDirectory();
    return normalized switch
    {
        "1.3" or "1_3" => Path.Combine(rootDir, "JDF_Schema", "Version_1_3", "JDF.xsd"),
        "1.7" or "1_7" => Path.Combine(rootDir, "JDF_Schema", "Version_1_7", "JDF.xsd"),
        _ => normalized
    };
}
