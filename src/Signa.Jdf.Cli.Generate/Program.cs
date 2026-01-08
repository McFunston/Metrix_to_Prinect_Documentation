using Signa.Jdf;

// Generator defaults are "safe" for minimal Cockpit import; flags opt into strict details.
if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    PrintUsage();
    return 0;
}

string? outputPath = null;
// Defaults favor a minimal, Cockpit-importable Signa-style ticket.
var jobId = "Job001";
var jobPartId = "Part001";
var status = "Waiting";
var descriptiveName = "unnamed";
var version = "1.3";
var maxVersion = "1.7";
var workStyle = "Perfecting";
var signatureName = "Sig001";
var sheetName = "Sheet1";
var sheetNames = new List<string>();
var includeBackSide = true;
// "Safe" defaults: keep PagePool/Output links unless explicitly disabled.
var includePagePool = true;
var includeOutputRunList = true;
// "Safe" defaults: include Signa transport metadata unless testing a non-Signa JDF.
var includeSignaMetadata = true;
var includeSignaBlob = true;
var includeSignaJdf = true;
var includeSignaJob = true;
var includeSignaJobParts = true;
var signaJobPartName = "A";
var signaJobPartNames = new List<string>();
var signatureJobParts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var signaBlobUrl = "SignaData.sdf";
var signaJdfUrl = "data.jdf";
var documentPdf = "data.pdf";
var marksPdf = "data.pdf";
var fileSpecMimeType = "application/pdf";
// Paper/plate/media options are opt-in to keep baseline minimal.
var includePaperMedia = false;
var paperMediaId = "r_media_paper";
var paperWidth = 2592m;
var paperHeight = 1728m;
decimal? paperThickness = null;
decimal? paperWeight = null;
string? paperGrainDirection = null;
string? paperFeedDirection = null;
string? paperBrand = null;
string? paperProductId = null;
string? paperGrade = null;
var includePlateMedia = false;
var plateMediaId = "r_media_plate";
var plateWidth = 2592m;
var plateHeight = 1728m;
decimal? plateLeadingEdge = null;
// Content placement is opt-in; it drives page list creation in Cockpit.
var includeContentPlacement = false;
var includeContentJobPart = false;
var includeContentRunlistIndex = false;
var contentJobPartSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var contentJobPartSheets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var contentWidth = 612m;
var contentHeight = 792m;
var contentOffsetX = 0m;
var contentOffsetY = 0m;
// Document page mapping is opt-in; enable when ContentObject labels are needed.
var includeDocumentPageMapping = false;
// Marks partitions are opt-in; use when marks PDFs are multi-page.
var includeMarksPartitions = false;
var marksPageCount = 2;
var marksPagesPerSide = 2;
var marksIncludeBackSide = false;
var documentPageCount = 1;
var documentPagesPerSheet = false;
// BCMY placeholders are opt-in; enable for Cockpit marks remapping.
var includeMarksSeparations = false;
var includeDocumentFileSpec = true;
var includeDocumentRunList = true;
var contentOrdBase = 0;
var contentOrdStep = 1;
var contentNameBase = 1;
var contentNameStep = 1;
var contentIncrementPerSheet = false;
var contentIncrementPerSlot = false;
var contentGridColumns = 1;
var contentGridRows = 1;
var contentGapX = 0m;
var contentGapY = 0m;
string? contentAssemblyIdBase = null;
string? contentAssemblyIdBase2 = null;
var contentAssemblyIdStart = 1;
var contentAssemblyIdStep = 1;
var contentAssemblySplitIndex = 0;
var contentGridReverseBackColumnOrder = false;
var contentGridReverseBackRowOrder = false;
var includeMarkObjectGeometry = false;
var includeBackContentPlacement = false;
var backContentOffsetX = 0m;
var backContentOffsetY = 0m;
var mirrorBackContent = false;
var includePrintingParamsPartitions = false;
var paperRectOffsetX = 0m;
var paperRectOffsetY = 0m;
var includePaperRect = true;
var includeStrippingParams = false;
var includeBinderySignature = false;
var binderySignaturePerSignature = false;
var includeTransferCurvePool = false;
var includeAssembly = false;
var signatureDefinitions = new List<SignatureDefinition>();
var marksResetLogicalPagePerSignature = false;
var marksSplitRunListPerSignature = false;
var marksResetLogicalPagePerSheet = false;
var stripRelLeft = 0m;
var stripRelBottom = 0m;
var stripRelRight = 1m;
var stripRelTop = 1m;
var stripTrimWidth = 612m;
var stripTrimHeight = 792m;
var binderySignatureType = "Fold";
var binderyNumberUp = "1 1";
var binderyFrontPages = "1";
var binderyBackPages = "2";
var binderyFrontOrientation = "0";
var binderyBackOrientation = "0";
var paperCtmX = 0m;
var paperCtmY = 0m;
string? stripSheetLay = null;
var typeList = new List<string>
{
    "Imposition",
    "ConventionalPrinting",
    "Cutting",
    "Folding",
    "Trimming"
};

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--output":
            outputPath = GetValue(args, ref i);
            break;
        case "--job-id":
            jobId = GetValue(args, ref i);
            break;
        case "--job-part-id":
            jobPartId = GetValue(args, ref i);
            break;
        case "--status":
            status = GetValue(args, ref i);
            break;
        case "--desc":
            descriptiveName = GetValue(args, ref i);
            break;
        case "--version":
            version = GetValue(args, ref i);
            break;
        case "--max-version":
            maxVersion = GetValue(args, ref i);
            break;
        case "--work-style":
            workStyle = GetValue(args, ref i);
            break;
        case "--signature":
            signatureName = GetValue(args, ref i);
            break;
        case "--sheet":
            sheetName = GetValue(args, ref i);
            break;
        case "--sheets":
            var sheetList = GetValue(args, ref i);
            sheetNames = sheetList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
            if (sheetNames.Count > 0)
            {
                sheetName = sheetNames[0];
            }
            break;
        case "--signatures":
            var signatureList = GetValue(args, ref i);
            signatureDefinitions = ParseSignatures(signatureList, sheetName);
            if (signatureDefinitions.Count > 0)
            {
                signatureName = signatureDefinitions[0].Name;
                var firstSheets = signatureDefinitions[0].Sheets;
                if (firstSheets.Count > 0)
                {
                    sheetName = firstSheets[0];
                }
            }
            break;
        case "--sides":
            var sides = GetValue(args, ref i);
            includeBackSide = sides.Contains("back", StringComparison.OrdinalIgnoreCase);
            break;
        case "--no-pagepool":
            includePagePool = false;
            break;
        case "--no-output-runlist":
            includeOutputRunList = false;
            break;
        case "--no-signa":
            includeSignaMetadata = false;
            break;
        case "--no-signa-blob":
            includeSignaBlob = false;
            break;
        case "--no-signa-jdf":
            includeSignaJdf = false;
            break;
        case "--no-signa-job":
            includeSignaJob = false;
            break;
        case "--no-signa-job-parts":
            includeSignaJobParts = false;
            break;
        case "--signa-job-part":
            signaJobPartName = GetValue(args, ref i);
            break;
        case "--signa-job-parts":
            var parts = GetValue(args, ref i);
            signaJobPartNames = parts.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            break;
        case "--signature-job-parts":
            var signatureParts = GetValue(args, ref i);
            signatureJobParts = ParseSignatureJobParts(signatureParts);
            break;
        case "--signa-blob-url":
            signaBlobUrl = GetValue(args, ref i);
            break;
        case "--signa-jdf-url":
            signaJdfUrl = GetValue(args, ref i);
            break;
        case "--document-pdf":
            documentPdf = GetValue(args, ref i);
            break;
        case "--marks-pdf":
            marksPdf = GetValue(args, ref i);
            break;
        case "--mime":
            fileSpecMimeType = GetValue(args, ref i);
            break;
        case "--paper":
            var dimension = GetValue(args, ref i);
            if (!TryParseDimension(dimension, out paperWidth, out paperHeight))
            {
                throw new ArgumentException($"Invalid --paper value '{dimension}'. Expected <width>x<height>.");
            }
            includePaperMedia = true;
            break;
        case "--paper-id":
            paperMediaId = GetValue(args, ref i);
            break;
        case "--paper-thickness":
            paperThickness = decimal.Parse(GetValue(args, ref i));
            break;
        case "--paper-weight":
            paperWeight = decimal.Parse(GetValue(args, ref i));
            break;
        case "--paper-grain":
            paperGrainDirection = GetValue(args, ref i);
            break;
        case "--paper-feed":
            paperFeedDirection = GetValue(args, ref i);
            break;
        case "--paper-brand":
            paperBrand = GetValue(args, ref i);
            break;
        case "--paper-product-id":
            paperProductId = GetValue(args, ref i);
            break;
        case "--paper-grade":
            paperGrade = GetValue(args, ref i);
            break;
        case "--plate":
            var plateDimension = GetValue(args, ref i);
            if (!TryParseDimension(plateDimension, out plateWidth, out plateHeight))
            {
                throw new ArgumentException($"Invalid --plate value '{plateDimension}'. Expected <width>x<height>.");
            }
            includePlateMedia = true;
            break;
        case "--plate-id":
            plateMediaId = GetValue(args, ref i);
            break;
        case "--plate-leading-edge":
            plateLeadingEdge = decimal.Parse(GetValue(args, ref i));
            includePlateMedia = true;
            break;
        case "--content":
            includeContentPlacement = true;
            includeDocumentPageMapping = true;
            break;
        case "--content-job-part":
            includeContentJobPart = true;
            break;
        case "--content-job-part-signatures":
            var jobPartSigs = GetValue(args, ref i);
            contentJobPartSignatures = jobPartSigs
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            includeContentJobPart = true;
            break;
        case "--content-job-part-sheets":
            var jobPartSheets = GetValue(args, ref i);
            contentJobPartSheets = ParseSignatureJobParts(jobPartSheets);
            includeContentJobPart = true;
            break;
        case "--content-runlist-index":
            includeContentRunlistIndex = true;
            break;
        case "--content-size":
            var contentSize = GetValue(args, ref i);
            if (!TryParseDimension(contentSize, out contentWidth, out contentHeight))
            {
                throw new ArgumentException($"Invalid --content-size value '{contentSize}'. Expected <width>x<height>.");
            }
            includeContentPlacement = true;
            includeDocumentPageMapping = true;
            break;
        case "--content-offset":
            var contentOffset = GetValue(args, ref i);
            if (!TryParsePair(contentOffset, out contentOffsetX, out contentOffsetY))
            {
                throw new ArgumentException($"Invalid --content-offset value '{contentOffset}'. Expected <x>,<y>.");
            }
            includeContentPlacement = true;
            includeDocumentPageMapping = true;
            break;
        case "--content-ord-base":
            contentOrdBase = int.Parse(GetValue(args, ref i));
            break;
        case "--content-ord-step":
            contentOrdStep = int.Parse(GetValue(args, ref i));
            break;
        case "--content-name-base":
            contentNameBase = int.Parse(GetValue(args, ref i));
            break;
        case "--content-name-step":
            contentNameStep = int.Parse(GetValue(args, ref i));
            break;
        case "--content-increment-per-sheet":
            contentIncrementPerSheet = true;
            break;
        case "--content-increment-per-slot":
            contentIncrementPerSlot = true;
            break;
        case "--content-grid":
            var grid = GetValue(args, ref i);
            if (!TryParseDimension(grid, out var gridCols, out var gridRows))
            {
                throw new ArgumentException($"Invalid --content-grid value '{grid}'. Expected <cols>x<rows>.");
            }
            contentGridColumns = (int)gridCols;
            contentGridRows = (int)gridRows;
            break;
        case "--content-gap":
            var gap = GetValue(args, ref i);
            if (!TryParsePair(gap, out contentGapX, out contentGapY))
            {
                throw new ArgumentException($"Invalid --content-gap value '{gap}'. Expected <x>,<y>.");
            }
            break;
        case "--content-assembly-base":
            contentAssemblyIdBase = GetValue(args, ref i);
            break;
        case "--content-assembly-base-2":
            contentAssemblyIdBase2 = GetValue(args, ref i);
            break;
        case "--content-assembly-split":
            contentAssemblySplitIndex = int.Parse(GetValue(args, ref i));
            break;
        case "--content-assembly-start":
            contentAssemblyIdStart = int.Parse(GetValue(args, ref i));
            break;
        case "--content-assembly-step":
            contentAssemblyIdStep = int.Parse(GetValue(args, ref i));
            break;
        case "--content-grid-reverse-back-cols":
            contentGridReverseBackColumnOrder = true;
            break;
        case "--content-grid-reverse-back-rows":
            contentGridReverseBackRowOrder = true;
            break;
        case "--marks-partitions":
            includeMarksPartitions = true;
            break;
        case "--marks-pages":
            marksPageCount = int.Parse(GetValue(args, ref i));
            includeMarksPartitions = true;
            break;
        case "--marks-pages-per-side":
            marksPagesPerSide = int.Parse(GetValue(args, ref i));
            includeMarksPartitions = true;
            break;
        case "--marks-include-back":
            marksIncludeBackSide = true;
            includeMarksPartitions = true;
            break;
        case "--marks-reset-per-signature":
            marksResetLogicalPagePerSignature = true;
            includeMarksPartitions = true;
            break;
        case "--marks-per-signature":
            marksSplitRunListPerSignature = true;
            includeMarksPartitions = true;
            break;
        case "--marks-reset-per-sheet":
            marksResetLogicalPagePerSheet = true;
            includeMarksPartitions = true;
            break;
        case "--marks-separations":
            includeMarksSeparations = true;
            break;
        case "--document-reservation":
            includeDocumentFileSpec = false;
            includeDocumentPageMapping = false;
            break;
        case "--no-document-runlist":
            includeDocumentRunList = false;
            break;
        case "--markobject":
            includeMarkObjectGeometry = true;
            break;
        case "--back-content":
            includeBackContentPlacement = true;
            break;
        case "--back-offset":
            var backOffset = GetValue(args, ref i);
            if (!TryParsePair(backOffset, out backContentOffsetX, out backContentOffsetY))
            {
                throw new ArgumentException($"Invalid --back-offset value '{backOffset}'. Expected <x>,<y>.");
            }
            includeBackContentPlacement = true;
            break;
        case "--mirror-back":
            mirrorBackContent = true;
            includeBackContentPlacement = true;
            break;
        case "--printing-partitions":
            includePrintingParamsPartitions = true;
            break;
        case "--paperrect-offset":
            var rectOffset = GetValue(args, ref i);
            if (!TryParsePair(rectOffset, out paperRectOffsetX, out paperRectOffsetY))
            {
                throw new ArgumentException($"Invalid --paperrect-offset value '{rectOffset}'. Expected <x>,<y>.");
            }
            break;
        case "--no-paper-rect":
            includePaperRect = false;
            break;
        case "--document-pages":
            documentPageCount = int.Parse(GetValue(args, ref i));
            includeDocumentPageMapping = true;
            break;
        case "--document-pages-per-sheet":
            documentPagesPerSheet = true;
            includeDocumentPageMapping = true;
            break;
        case "--stripping":
            includeStrippingParams = true;
            break;
        case "--stripping-sheetlay":
            stripSheetLay = GetValue(args, ref i);
            includeStrippingParams = true;
            break;
        case "--stripping-rel":
            var rel = GetValue(args, ref i);
            if (!TryParseQuad(rel, out stripRelLeft, out stripRelBottom, out stripRelRight, out stripRelTop))
            {
                throw new ArgumentException($"Invalid --stripping-rel value '{rel}'. Expected <l>,<b>,<r>,<t>.");
            }
            includeStrippingParams = true;
            break;
        case "--strip-trim":
            var trim = GetValue(args, ref i);
            if (!TryParseDimension(trim, out stripTrimWidth, out stripTrimHeight))
            {
                throw new ArgumentException($"Invalid --strip-trim value '{trim}'. Expected <w>x<h>.");
            }
            includeStrippingParams = true;
            break;
        case "--bindery":
            includeBinderySignature = true;
            break;
        case "--bindery-per-signature":
            binderySignaturePerSignature = true;
            includeBinderySignature = true;
            break;
        case "--bindery-type":
            binderySignatureType = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--bindery-numberup":
            binderyNumberUp = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--bindery-front":
            binderyFrontPages = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--bindery-back":
            binderyBackPages = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--bindery-front-orient":
            binderyFrontOrientation = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--bindery-back-orient":
            binderyBackOrientation = GetValue(args, ref i);
            includeBinderySignature = true;
            break;
        case "--transfer-curve":
            includeTransferCurvePool = true;
            break;
        case "--assembly":
            includeAssembly = true;
            break;
        case "--paper-ctm":
            var ctm = GetValue(args, ref i);
            if (!TryParsePair(ctm, out paperCtmX, out paperCtmY))
            {
                throw new ArgumentException($"Invalid --paper-ctm value '{ctm}'. Expected <x>,<y>.");
            }
            includeTransferCurvePool = true;
            break;
        case "--types":
            var types = GetValue(args, ref i);
            typeList = types.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
            break;
    }
}

if (string.IsNullOrWhiteSpace(outputPath))
{
    Console.WriteLine("Missing required --output <path>.");
    PrintUsage();
    return 1;
}

var options = new GeneratorOptions
{
    JobId = jobId,
    JobPartId = jobPartId,
    Status = status,
    DescriptiveName = descriptiveName,
    Version = version,
    MaxVersion = maxVersion,
    WorkStyle = workStyle,
    SignatureName = signatureName,
    SheetName = sheetName,
    SheetNames = sheetNames,
    Signatures = signatureDefinitions,
    IncludeBackSide = includeBackSide,
    IncludePagePool = includePagePool,
    IncludeOutputRunList = includeOutputRunList,
    IncludeSignaMetadata = includeSignaMetadata,
    IncludeSignaBlob = includeSignaBlob,
    IncludeSignaJdf = includeSignaJdf,
    IncludeSignaJob = includeSignaJob,
    IncludeSignaJobParts = includeSignaJobParts,
    SignaJobPartName = signaJobPartName,
    SignaJobPartNames = signaJobPartNames,
    SignatureJobParts = signatureJobParts,
    SignaBlobUrl = signaBlobUrl,
    SignaJdfUrl = signaJdfUrl,
    DocumentFileName = documentPdf,
    MarksFileName = marksPdf,
    FileSpecMimeType = fileSpecMimeType,
    IncludePaperMedia = includePaperMedia,
    PaperMediaId = paperMediaId,
    PaperWidth = paperWidth,
    PaperHeight = paperHeight,
    PaperThickness = paperThickness,
    PaperWeight = paperWeight,
    PaperGrainDirection = paperGrainDirection,
    PaperFeedDirection = paperFeedDirection,
    PaperBrand = paperBrand,
    PaperProductId = paperProductId,
    PaperGrade = paperGrade,
    IncludePlateMedia = includePlateMedia,
    PlateMediaId = plateMediaId,
    PlateWidth = plateWidth,
    PlateHeight = plateHeight,
    PlateLeadingEdge = plateLeadingEdge,
    IncludeContentPlacement = includeContentPlacement,
    IncludeContentJobPart = includeContentJobPart,
    IncludeContentRunlistIndex = includeContentRunlistIndex,
    ContentJobPartSignatures = contentJobPartSignatures,
    ContentJobPartSheets = contentJobPartSheets,
    ContentTrimWidth = contentWidth,
    ContentTrimHeight = contentHeight,
    ContentOffsetX = contentOffsetX,
    ContentOffsetY = contentOffsetY,
    IncludeDocumentPageMapping = includeDocumentPageMapping,
    IncludeMarksPartitions = includeMarksPartitions,
    MarksPageCount = marksPageCount,
    MarksPagesPerSide = marksPagesPerSide,
    MarksIncludeBackSide = marksIncludeBackSide,
    MarksResetLogicalPagePerSignature = marksResetLogicalPagePerSignature,
    MarksSplitRunListPerSignature = marksSplitRunListPerSignature,
    MarksResetLogicalPagePerSheet = marksResetLogicalPagePerSheet,
    DocumentPageCount = documentPageCount,
    DocumentPagesPerSheet = documentPagesPerSheet,
    IncludeMarksSeparations = includeMarksSeparations,
    IncludeStrippingParams = includeStrippingParams,
    IncludeBinderySignature = includeBinderySignature,
    IncludeTransferCurvePool = includeTransferCurvePool,
    IncludeAssembly = includeAssembly,
    StripRelLeft = stripRelLeft,
    StripRelBottom = stripRelBottom,
    StripRelRight = stripRelRight,
    StripRelTop = stripRelTop,
    StripTrimWidth = stripTrimWidth,
    StripTrimHeight = stripTrimHeight,
    StripSheetLay = stripSheetLay,
    BinderySignatureType = binderySignatureType,
    BinderySignatureNumberUp = binderyNumberUp,
    BinderyFrontPages = binderyFrontPages,
    BinderyBackPages = binderyBackPages,
    BinderyFrontOrientation = binderyFrontOrientation,
    BinderyBackOrientation = binderyBackOrientation,
    BinderySignaturePerSignature = binderySignaturePerSignature,
    PaperCtmOffsetX = paperCtmX,
    PaperCtmOffsetY = paperCtmY,
    IncludeDocumentFileSpec = includeDocumentFileSpec,
    IncludeDocumentRunList = includeDocumentRunList,
    ContentOrdBase = contentOrdBase,
    ContentOrdStep = contentOrdStep,
    ContentNameBase = contentNameBase,
    ContentNameStep = contentNameStep,
    ContentIncrementPerSheet = contentIncrementPerSheet,
    ContentIncrementPerSlot = contentIncrementPerSlot,
    ContentGridColumns = contentGridColumns,
    ContentGridRows = contentGridRows,
    ContentGapX = contentGapX,
    ContentGapY = contentGapY,
    ContentAssemblyIdBase = contentAssemblyIdBase,
    ContentAssemblyIdBase2 = contentAssemblyIdBase2,
    ContentAssemblyIdStart = contentAssemblyIdStart,
    ContentAssemblyIdStep = contentAssemblyIdStep,
    ContentAssemblySplitIndex = contentAssemblySplitIndex,
    ContentGridReverseBackColumnOrder = contentGridReverseBackColumnOrder,
    ContentGridReverseBackRowOrder = contentGridReverseBackRowOrder,
    IncludeMarkObjectGeometry = includeMarkObjectGeometry,
    IncludeBackContentPlacement = includeBackContentPlacement,
    BackContentOffsetX = backContentOffsetX,
    BackContentOffsetY = backContentOffsetY,
    MirrorBackContent = mirrorBackContent,
    IncludePrintingParamsPartitions = includePrintingParamsPartitions,
    PaperRectOffsetX = paperRectOffsetX,
    PaperRectOffsetY = paperRectOffsetY,
    IncludePaperRect = includePaperRect,
    Types = typeList
};

var document = JdfGenerator.Generate(options);
document.Save(outputPath);
Console.WriteLine($"Generated JDF: {outputPath}");
return 0;

static string GetValue(string[] args, ref int index)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"Missing value for {args[index]}");
    }

    index++;
    return args[index];
}

static void PrintUsage()
{
    Console.WriteLine("Usage: signa-jdf-gen --output <path> [options]");
    Console.WriteLine("  --work-style <style>         WorkStyle value (default Perfecting).");
    Console.WriteLine("  --signature <name>           SignatureName value (default Sig001).");
    Console.WriteLine("  --sheet <name>               SheetName value (default Sheet1).");
    Console.WriteLine("  --sheets <name1,name2>       Multiple SheetName values (same signature).");
    Console.WriteLine("  --signatures <list>          Signature definitions (Sig001=Cover@Sheetwise;Sig002=Body@Perfecting).");
    Console.WriteLine("  --sides <front|front,back>   Which sides to emit (default front,back).");
    Console.WriteLine("  --types <comma-list>         Root Types list.");
    Console.WriteLine("  --job-id <id>                JobID value.");
    Console.WriteLine("  --job-part-id <id>           JobPartID value.");
    Console.WriteLine("  --status <status>            Root Status value.");
    Console.WriteLine("  --desc <name>                Root DescriptiveName value.");
    Console.WriteLine("  --version <ver>              JDF Version attribute.");
    Console.WriteLine("  --max-version <ver>          JDF MaxVersion attribute.");
    Console.WriteLine("  --no-pagepool                Omit PagePool RunList and link.");
    Console.WriteLine("  --no-output-runlist          Omit Output RunList and link.");
    Console.WriteLine("  --no-signa                   Omit HDM:Signa* metadata under Layout.");
    Console.WriteLine("  --no-signa-blob              Omit HDM:SignaBLOB (Signa Station data file).");
    Console.WriteLine("  --no-signa-jdf               Omit HDM:SignaJDF reference.");
    Console.WriteLine("  --no-signa-job               Omit HDM:SignaJob and SignaJobPart entries.");
    Console.WriteLine("  --no-signa-job-parts         Omit HDM:SignaJobPart entries (keeps SignaJob).");
    Console.WriteLine("  --signa-job-part <name>      HDM:SignaJobPart Name.");
    Console.WriteLine("  --signa-job-parts <list>     Comma-separated HDM:SignaJobPart names.");
    Console.WriteLine("  --signature-job-parts <map>  Signature-to-job part map (Sig001=A;Sig002=B).");
    Console.WriteLine("  --signa-blob-url <url>       HDM:SignaBLOB URL (default SignaData.sdf).");
    Console.WriteLine("  --signa-jdf-url <url>        HDM:SignaJDF URL (default data.jdf).");
    Console.WriteLine("  --document-pdf <url>         Document RunList FileSpec URL.");
    Console.WriteLine("  --marks-pdf <url>            Marks RunList FileSpec URL.");
    Console.WriteLine("  --mime <type>                FileSpec MimeType (default application/pdf).");
    Console.WriteLine("  --paper <w>x<h>              Include Paper Media with Dimension.");
    Console.WriteLine("  --paper-id <id>              Paper Media ID (default r_media_paper).");
    Console.WriteLine("  --paper-thickness <value>    Paper Thickness attribute.");
    Console.WriteLine("  --paper-weight <value>       Paper Weight attribute (grammage).");
    Console.WriteLine("  --paper-grain <value>        Paper GrainDirection (e.g., LongEdge).");
    Console.WriteLine("  --paper-feed <value>         HDM:FeedDirection (custom, if needed).");
    Console.WriteLine("  --paper-brand <value>        Paper Brand/DescriptiveName.");
    Console.WriteLine("  --paper-product-id <value>   Paper ProductID (article number).");
    Console.WriteLine("  --paper-grade <value>        Paper Grade value.");
    Console.WriteLine("  --plate <w>x<h>              Include Plate Media with Dimension.");
    Console.WriteLine("  --plate-id <id>              Plate Media ID (default r_media_plate).");
    Console.WriteLine("  --plate-leading-edge <val>   HDM:LeadingEdge on Plate Media.");
    Console.WriteLine("  --content                    Add a minimal ContentObject placement + doc page mapping.");
    Console.WriteLine("  --content-job-part           Add HDM:JobPart to ContentObject.");
    Console.WriteLine("  --content-job-part-signatures <list> Comma-separated signatures that receive HDM:JobPart.");
    Console.WriteLine("  --content-job-part-sheets <map> Sheet-to-job part map (Sheet1=A;Sheet2=B).");
    Console.WriteLine("  --content-runlist-index      Add HDM:RunlistIndex to ContentObject.");
    Console.WriteLine("  --content-size <w>x<h>       TrimSize for ContentObject placement.");
    Console.WriteLine("  --content-offset <x>,<y>     Placement offset for ContentObject.");
    Console.WriteLine("  --content-ord-base <n>       Base Ord value for ContentObject.");
    Console.WriteLine("  --content-ord-step <n>       Ord increment per sheet when enabled.");
    Console.WriteLine("  --content-name-base <n>      Base DescriptiveName (numeric) for ContentObject.");
    Console.WriteLine("  --content-name-step <n>      DescriptiveName increment per sheet when enabled.");
    Console.WriteLine("  --content-increment-per-sheet Enable per-sheet Ord/DescriptiveName increments.");
    Console.WriteLine("  --content-increment-per-slot Enable per-slot Ord/DescriptiveName increments.");
    Console.WriteLine("  --content-grid <cols>x<rows> ContentObject grid dimensions.");
    Console.WriteLine("  --content-gap <x>,<y>        Gap between content grid cells.");
    Console.WriteLine("  --content-assembly-base <s>  Base AssemblyIDs prefix for content slots.");
    Console.WriteLine("  --content-assembly-base-2 <s> Second AssemblyIDs prefix for content slots.");
    Console.WriteLine("  --content-assembly-split <n> Slot index at which to switch to base-2 prefix.");
    Console.WriteLine("  --content-assembly-start <n> AssemblyIDs starting index.");
    Console.WriteLine("  --content-assembly-step <n>  AssemblyIDs increment per slot.");
    Console.WriteLine("  --content-grid-reverse-back-cols Reverse content grid column order on back.");
    Console.WriteLine("  --content-grid-reverse-back-rows Reverse content grid row order on back.");
    Console.WriteLine("  --marks-partitions           Add Marks RunList partitions with page ranges.");
    Console.WriteLine("  --marks-pages <count>        Marks RunList page count (default 2).");
    Console.WriteLine("  --marks-pages-per-side <n>   Marks pages per side (default 2).");
    Console.WriteLine("  --marks-include-back         Add back-side Marks RunList partition.");
    Console.WriteLine("  --marks-reset-per-signature  Reset marks logical pages per signature.");
    Console.WriteLine("  --marks-per-signature        Emit one Marks RunList per signature.");
    Console.WriteLine("  --marks-reset-per-sheet      Reset marks logical pages per sheet.");
    Console.WriteLine("  --marks-separations          Add default SeparationSpec list to Marks RunList.");
    Console.WriteLine("  --document-pages <count>     Document RunList page count (default 1).");
    Console.WriteLine("  --document-pages-per-sheet   Offset document LogicalPage per sheet.");
    Console.WriteLine("  --document-reservation       Emit Document RunList as reservation (no FileSpec).");
    Console.WriteLine("  --no-document-runlist        Omit Document RunList and link entirely.");
    Console.WriteLine("  --markobject                 Add MarkObject CTM/ClipBox/Ord attributes.");
    Console.WriteLine("  --back-content               Add a back-side ContentObject placement.");
    Console.WriteLine("  --back-offset <x>,<y>        Placement offset for back-side ContentObject.");
    Console.WriteLine("  --mirror-back                Use mirrored CTM for back-side placement.");
    Console.WriteLine("  --printing-partitions        Add ConventionalPrintingParams partitions by Signature/Sheet/Side.");
    Console.WriteLine("  --paperrect-offset <x>,<y>   Offset for HDM:PaperRect lower-left.");
    Console.WriteLine("  --no-paper-rect              Omit HDM:PaperRect from per-side Layout.");
    Console.WriteLine("  --stripping                  Add StrippingParams with Position/StripCellParams.");
    Console.WriteLine("  --stripping-rel <l>,<b>,<r>,<t>  RelativeBox for StrippingParams Position.");
    Console.WriteLine("  --strip-trim <w>x<h>         StripCellParams TrimSize.");
    Console.WriteLine("  --stripping-sheetlay <value> SheetLay on StrippingParams (e.g., Right).");
    Console.WriteLine("  --bindery                    Add BinderySignature + SignatureCell.");
    Console.WriteLine("  --bindery-per-signature      Add one BinderySignature per signature.");
    Console.WriteLine("  --bindery-type <value>       BinderySignatureType (default Fold).");
    Console.WriteLine("  --bindery-numberup <value>   BinderySignature NumberUp (default \"1 1\").");
    Console.WriteLine("  --bindery-front <list>       BinderySignature FrontPages list.");
    Console.WriteLine("  --bindery-back <list>        BinderySignature BackPages list.");
    Console.WriteLine("  --bindery-front-orient <list> FrontSchemePageOrientation list.");
    Console.WriteLine("  --bindery-back-orient <list>  BackSchemePageOrientation list.");
    Console.WriteLine("  --transfer-curve             Add TransferCurvePool with Paper/Plate CTM.");
    Console.WriteLine("  --paper-ctm <x>,<y>          Paper TransferCurveSet CTM offset.");
    Console.WriteLine("  --assembly                   Add Assembly with per-slot AssemblySection entries.");
}

static bool TryParseDimension(string value, out decimal width, out decimal height)
{
    width = 0;
    height = 0;
    var parts = value.Split('x', 'X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
    {
        return false;
    }

    return decimal.TryParse(parts[0], out width)
        && decimal.TryParse(parts[1], out height);
}

static bool TryParsePair(string value, out decimal first, out decimal second)
{
    first = 0;
    second = 0;
    var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
    {
        return false;
    }

    return decimal.TryParse(parts[0], out first)
        && decimal.TryParse(parts[1], out second);
}

static bool TryParseQuad(string value, out decimal left, out decimal bottom, out decimal right, out decimal top)
{
    left = 0;
    bottom = 0;
    right = 0;
    top = 0;
    var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 4)
    {
        return false;
    }

    return decimal.TryParse(parts[0], out left)
        && decimal.TryParse(parts[1], out bottom)
        && decimal.TryParse(parts[2], out right)
        && decimal.TryParse(parts[3], out top);
}

static List<SignatureDefinition> ParseSignatures(string value, string fallbackSheet)
{
    var results = new List<SignatureDefinition>();
    var signatureParts = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var part in signatureParts)
    {
        var tokens = part.Split('=', 2, StringSplitOptions.TrimEntries);
        var name = tokens[0].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            continue;
        }

        var sheets = new List<string>();
        string? workStyle = null;
        if (tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1]))
        {
            var sheetSpec = tokens[1];
            var styleSplit = sheetSpec.Split('@', 2, StringSplitOptions.TrimEntries);
            if (styleSplit.Length == 2)
            {
                sheetSpec = styleSplit[0];
                workStyle = styleSplit[1];
            }

            sheets = sheetSpec
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToList();
        }

        if (sheets.Count == 0)
        {
            sheets.Add(fallbackSheet);
        }

        results.Add(new SignatureDefinition(name, sheets, workStyle));
    }

    return results;
}

static Dictionary<string, string> ParseSignatureJobParts(string value)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var entries = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var entry in entries)
    {
        var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            throw new ArgumentException($"Invalid --signature-job-parts entry '{entry}'. Expected <Signature>=<JobPart>.");
        }
        map[parts[0]] = parts[1];
    }
    return map;
}
