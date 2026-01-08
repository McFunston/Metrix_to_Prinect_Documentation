using Metrix.Jdf;

if (args.Length < 1)
{
    Console.WriteLine("Usage: metrix-jdf-parse <path-to-jdf> [path-to-mxml]");
    return 1;
}

var jdfPath = args[0];
// Parse is diagnostic-only; no normalization is applied here.
var jdfDocument = MetrixJdfParser.Parse(jdfPath);

Console.WriteLine($"JDF File: {jdfDocument.SourcePath}");
Console.WriteLine($"Type: {jdfDocument.Root.Type}");
Console.WriteLine($"JobID: {jdfDocument.Root.JobId}");
Console.WriteLine($"Description: {jdfDocument.Root.DescriptiveName}");

var layoutStats = CountLayout(jdfDocument.Layout);
Console.WriteLine($"Layout signatures: {layoutStats.Signatures}");
Console.WriteLine($"Layout sheets: {layoutStats.Sheets}");
Console.WriteLine($"Layout surfaces: {layoutStats.Surfaces}");
Console.WriteLine($"Layout mark objects: {layoutStats.MarkObjects}");
Console.WriteLine($"Layout content objects: {layoutStats.ContentObjects}");

Console.WriteLine($"RunLists: {jdfDocument.RunLists.Count}");
PrintRunListUsage(jdfDocument, "Document");
PrintRunListUsage(jdfDocument, "Marks");

if (args.Length > 1)
{
    var mxmlPath = args[1];
    var mxmlDocument = MetrixMxmlParser.Parse(mxmlPath);

    Console.WriteLine();
    Console.WriteLine($"MXML File: {mxmlDocument.SourcePath}");
    Console.WriteLine($"Project: {mxmlDocument.Project.ProjectId} - {mxmlDocument.Project.Name}");
    Console.WriteLine($"Products: {mxmlDocument.Project.Products.Count}");
    Console.WriteLine($"Layouts: {mxmlDocument.Project.Layouts.Count}");
    Console.WriteLine($"Folding schemes: {mxmlDocument.ResourcePool.FoldingSchemes.Count}");
}

return 0;

static void PrintRunListUsage(MetrixJdfDocument document, string usage)
{
    var refId = document.GetRunListRef(usage);
    var runList = document.FindRunListById(refId);
    var npage = runList?.NPage ?? "(missing)";
    Console.WriteLine($"{usage} RunList: {refId ?? "(missing)"} (NPage={npage})");
}

static (int Signatures, int Sheets, int Surfaces, int MarkObjects, int ContentObjects) CountLayout(MetrixLayout? layout)
{
    if (layout is null)
    {
        return (0, 0, 0, 0, 0);
    }

    var signatureCount = layout.Signatures.Count;
    var sheetCount = layout.Signatures.Sum(signature => signature.Sheets.Count);
    var surfaceCount = layout.Signatures.Sum(signature => signature.Sheets.Sum(sheet => sheet.Surfaces.Count));
    var markCount = layout.Signatures.Sum(signature =>
        signature.Sheets.Sum(sheet => sheet.Surfaces.Sum(surface => surface.MarkObjects.Count)));
    var contentCount = layout.Signatures.Sum(signature =>
        signature.Sheets.Sum(sheet => sheet.Surfaces.Sum(surface => surface.ContentObjects.Count)));

    return (signatureCount, sheetCount, surfaceCount, markCount, contentCount);
}
