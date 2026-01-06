using Signa.Jdf;

if (args.Length < 1)
{
    Console.WriteLine("Usage: signa-jdf-extract <path-to-jdf> [--types] [--work-styles] [--sheet-sizes] [--separations] [--finishing]");
    return 1;
}

var path = args[0];
var options = args.Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);

var showTypes = options.Contains("--types");
var showWorkStyles = options.Contains("--work-styles");
var showSheetSizes = options.Contains("--sheet-sizes");
var showSeparations = options.Contains("--separations");
var showFinishing = options.Contains("--finishing");

if (!showTypes && !showWorkStyles && !showSheetSizes && !showSeparations && !showFinishing)
{
    showTypes = showWorkStyles = showSheetSizes = showSeparations = showFinishing = true;
}

var document = JdfParser.Parse(path);

if (showTypes)
{
    Console.WriteLine("Types:");
    Console.WriteLine($"- {string.Join(", ", document.Types)}");
}

if (showWorkStyles)
{
    Console.WriteLine("WorkStyles:");
    var styles = JdfExtractor.GetWorkStyles(document);
    Console.WriteLine($"- {string.Join(", ", styles.OrderBy(s => s))}");
}

if (showSheetSizes)
{
    Console.WriteLine("Sheet sizes:");
    foreach (var sheet in JdfExtractor.GetSheetSizes(document))
    {
        Console.WriteLine($"- {sheet.SignatureName ?? "?"} / {sheet.SheetName ?? "?"} / {sheet.Side ?? "?"} | Surface={sheet.SurfaceContentsBox ?? "?"} | PaperRect={sheet.PaperRect ?? "?"}");
    }
}

if (showSeparations)
{
    Console.WriteLine("Separations:");
    var names = JdfExtractor.GetSeparationNames(document);
    Console.WriteLine($"- {string.Join(", ", names.OrderBy(n => n))}");
}

if (showFinishing)
{
    Console.WriteLine("Finishing steps:");
    var finishing = document.Types
        .Where(type => !string.Equals(type, "Imposition", StringComparison.OrdinalIgnoreCase))
        .ToList();
    Console.WriteLine($"- {string.Join(", ", finishing)}");
}

return 0;
