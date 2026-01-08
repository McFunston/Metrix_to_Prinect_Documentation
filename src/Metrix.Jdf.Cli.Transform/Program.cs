using Metrix.Jdf;
using Metrix.Jdf.Transform;
using Signa.Jdf;

// Pipeline: parse Metrix JDF/MXML -> generate Signa-style skeleton -> postprocess geometry -> write bundle.
if (args.Length < 2)
{
    Console.WriteLine("Usage: metrix-jdf-transform <path-to-jdf> <output-bundle-dir> [path-to-mxml]");
    return 1;
}

var jdfPath = args[0];
var outputDir = args[1];
var mxmlPath = args.Length > 2 ? args[2] : null;

// Safe defaults: keep marks PDF wiring, omit content PDF injection (Cockpit normalizes content separately).
var metrixJdf = MetrixJdfParser.Parse(jdfPath);
var metrixMxml = !string.IsNullOrWhiteSpace(mxmlPath) ? MetrixMxmlParser.Parse(mxmlPath!) : null;

var marksFileName = ResolveMarksFileName(metrixJdf);
var transformer = new MetrixToSignaTransformer();
// "Safe" normalization flags favor Cockpit importability over full semantic coverage.
var options = transformer.BuildGeneratorOptions(metrixJdf, metrixMxml, new MetrixToSignaOptions
{
    MarksFileName = marksFileName is null ? "./Content/marks.pdf" : $"./Content/{marksFileName}",
    DocumentFileName = "./Content/content.pdf",
    // Safe: avoid bypassing Cockpit's content normalization by injecting a document PDF.
    IncludeDocumentFileSpec = false,
    // Safe: provide page mapping so Cockpit can build page lists.
    IncludeDocumentPageMapping = true,
    // Safe: partitions communicate WorkStyle per Signature/Sheet/Side.
    IncludePrintingParamsPartitions = true,
    // Safe: media dimensions drive plate/trim geometry and preview placement.
    IncludePaperMedia = true,
    IncludePlateMedia = true,
    // Safe: BCMY placeholders + partitions enable marks mapping in Cockpit.
    IncludeMarksSeparations = true,
    IncludeMarksPartitions = true,
    // Safe: PaperRect anchors previews to sheet rather than plate origin.
    IncludePaperRect = true,
    // Safe: omit SignaBLOB reference (no SignaStation data is available).
    IncludeSignaBlob = false
});

// Post-process: replace layout geometry + labels with Metrix-derived placements.
var document = JdfGenerator.Generate(options);
MetrixContentPostProcessor.ApplyContentPlacement(document, metrixJdf, metrixMxml);

Directory.CreateDirectory(outputDir);
var contentDir = Path.Combine(outputDir, "Content");
Directory.CreateDirectory(contentDir);

var outputJdfPath = Path.Combine(outputDir, "data.jdf");
BackupExistingJdf(outputJdfPath);
document.Save(outputJdfPath);

if (options.IncludeDocumentFileSpec)
{
    WriteMinimalPdf(Path.Combine(contentDir, "content.pdf"));
}

if (!string.IsNullOrWhiteSpace(marksFileName))
{
    var marksSource = ResolveMarksSourcePath(jdfPath, marksFileName!);
    if (marksSource is null)
    {
        Console.WriteLine($"Warning: could not locate marks PDF for {marksFileName}.");
    }
    else
    {
        var destination = Path.Combine(contentDir, marksFileName!);
        if (!Path.GetFullPath(marksSource).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(marksSource, destination, true);
        }
    }
}

Console.WriteLine($"Wrote: {outputJdfPath}");
return 0;

static void BackupExistingJdf(string outputJdfPath)
{
    if (!File.Exists(outputJdfPath))
    {
        return;
    }

    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    var backupPath = $"{outputJdfPath}.bak-{timestamp}";
    File.Copy(outputJdfPath, backupPath, true);
    Console.WriteLine($"Backed up: {backupPath}");
}

static string? ResolveMarksFileName(MetrixJdfDocument document)
{
    var marksRef = document.GetRunListRef("Marks");
    var marksRunList = document.FindRunListById(marksRef);
    var marksEntry = marksRunList?.Entries.FirstOrDefault(entry => !string.IsNullOrWhiteSpace(entry.FileSpecUrl));
    if (marksEntry?.FileSpecUrl is null)
    {
        return null;
    }

    return Path.GetFileName(marksEntry.FileSpecUrl);
}

static string? ResolveMarksSourcePath(string jdfPath, string marksFileName)
{
    var jdfDir = Path.GetDirectoryName(jdfPath);
    if (string.IsNullOrWhiteSpace(jdfDir))
    {
        return null;
    }

    var directCandidate = Path.Combine(jdfDir, "Content", marksFileName);
    if (File.Exists(directCandidate))
    {
        return directCandidate;
    }

    var samplesRoot = Directory.GetParent(jdfDir);
    if (samplesRoot is not null)
    {
        var bundleCandidate = Path.Combine(samplesRoot.FullName, Path.GetFileName(jdfPath), "Content", marksFileName);
        if (File.Exists(bundleCandidate))
        {
            return bundleCandidate;
        }

        var marksCandidate = Path.Combine(samplesRoot.FullName, "marks", marksFileName);
        if (File.Exists(marksCandidate))
        {
            return marksCandidate;
        }
    }

    return null;
}

static void WriteMinimalPdf(string path)
{
    if (File.Exists(path))
    {
        return;
    }

    var objects = new[]
    {
        "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj",
        "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj",
        "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj",
        "4 0 obj\n<< /Length 0 >>\nstream\n\nendstream\nendobj"
    };

    var offsets = new List<int> { 0 };
    var content = "%PDF-1.4\n";

    foreach (var obj in objects)
    {
        offsets.Add(content.Length);
        content += obj + "\n";
    }

    var xrefOffset = content.Length;
    content += "xref\n";
    content += $"0 {objects.Length + 1}\n";
    content += "0000000000 65535 f \n";
    foreach (var offset in offsets.Skip(1))
    {
        content += $"{offset:D10} 00000 n \n";
    }

    content += "trailer\n";
    content += "<< /Size 5 /Root 1 0 R >>\n";
    content += "startxref\n";
    content += $"{xrefOffset}\n";
    content += "%%EOF\n";

    File.WriteAllText(path, content);
}
