using Signa.Jdf;

if (args.Length < 1)
{
    Console.WriteLine("Usage: signa-jdf-parse <path-to-jdf>");
    return 1;
}

var path = args[0];
var document = JdfParser.Parse(path);

Console.WriteLine($"File: {document.SourcePath}");
Console.WriteLine($"Type: {document.Root.Type}");
Console.WriteLine($"Types: {string.Join(", ", document.Types)}");
Console.WriteLine($"Layout PartIDKeys: {document.Layout?.PartIdKeys ?? "(missing)"}");

var layoutStats = CountLayout(document.Layout);
Console.WriteLine($"Layout signatures: {layoutStats.Signatures}");
Console.WriteLine($"Layout sheets: {layoutStats.Sheets}");
Console.WriteLine($"Layout sides: {layoutStats.Sides}");
Console.WriteLine($"Layout mark objects: {layoutStats.MarkObjects}");
Console.WriteLine($"Layout content objects: {layoutStats.ContentObjects}");

Console.WriteLine($"RunLists: {document.RunLists.Count}");

PrintRunListUsage(document, "Document");
PrintRunListUsage(document, "Marks");
PrintRunListUsage(document, "PagePool");

return 0;

static void PrintRunListUsage(JdfDocument document, string usage)
{
    var refId = document.GetRunListRef(usage);
    var runList = document.FindRunListById(refId);
    var npage = runList?.NPage ?? "(missing)";
    Console.WriteLine($"{usage} RunList: {refId ?? "(missing)"} (NPage={npage})");
}

static (int Signatures, int Sheets, int Sides, int MarkObjects, int ContentObjects) CountLayout(LayoutPart? layout)
{
    var signatureCount = 0;
    var sheetCount = 0;
    var sideCount = 0;
    var markCount = 0;
    var contentCount = 0;

    if (layout is null)
    {
        return (0, 0, 0, 0, 0);
    }

    void Visit(LayoutPart part)
    {
        if (!string.IsNullOrWhiteSpace(part.SignatureName) &&
            string.IsNullOrWhiteSpace(part.SheetName) &&
            string.IsNullOrWhiteSpace(part.Side))
        {
            signatureCount++;
        }

        if (!string.IsNullOrWhiteSpace(part.SheetName) &&
            string.IsNullOrWhiteSpace(part.Side))
        {
            sheetCount++;
        }

        if (!string.IsNullOrWhiteSpace(part.Side))
        {
            sideCount++;
        }

        markCount += part.MarkObjectCount;
        contentCount += part.ContentObjectCount;

        foreach (var child in part.Children)
        {
            Visit(child);
        }
    }

    Visit(layout);
    return (signatureCount, sheetCount, sideCount, markCount, contentCount);
}
