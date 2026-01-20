using Signa.Jdf;

namespace Metrix.Jdf.Transform;

public sealed class MetrixToSignaTransformer
{
    // Builds Signa generator defaults from Metrix JDF + MXML metadata.
    public GeneratorOptions BuildGeneratorOptions(
        MetrixJdfDocument jdf,
        MetrixMxmlDocument? mxml,
        MetrixToSignaOptions options)
    {
        if (jdf is null)
        {
            throw new ArgumentNullException(nameof(jdf));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var layout = jdf.Layout;
        var signature = layout?.Signatures.FirstOrDefault();
        var firstSheet = signature?.Sheets.FirstOrDefault();
        var firstSurface = firstSheet?.Surfaces.FirstOrDefault();

        var plateWidth = options.FallbackPlateWidth;
        var plateHeight = options.FallbackPlateHeight;
        var paperWidth = options.FallbackPaperWidth;
        var paperHeight = options.FallbackPaperHeight;
        var paperOffsetX = options.FallbackPaperOffsetX;
        var paperOffsetY = options.FallbackPaperOffsetY;

        if (firstSurface is not null && options.UseSurfaceDimensions)
        {
            if (TryParseBox(firstSurface.SurfaceContentsBox, out var surfaceWidth, out var surfaceHeight))
            {
                plateWidth = surfaceWidth;
                plateHeight = surfaceHeight;
            }

            if (TryParseDimension(firstSurface.Dimension, out var surfacePaperWidth, out var surfacePaperHeight))
            {
                paperWidth = surfacePaperWidth;
                paperHeight = surfacePaperHeight;
            }
        }

        if (options.CenterPaperRect)
        {
            (paperOffsetX, paperOffsetY) = ResolvePaperOffset(firstSurface, plateWidth, plateHeight, paperWidth, paperHeight);
        }

        // Capture signature names and per-signature work style to preserve mixed layouts.
        var signatures = layout is null
            ? new List<SignatureDefinition>()
            : layout.Signatures
                .Select(sig => new SignatureDefinition(
                    sig.Name ?? options.FallbackSignatureName,
                    sig.Sheets.Select(sheet => sheet.Name ?? options.FallbackSheetName).ToArray(),
                    MapWorkStyle(sig.Sheets.FirstOrDefault()?.WorkStyle, mxml)))
                .ToList();

        var contentStats = ResolveContentStats(layout);

        var documentPageCount = ResolveDocumentPageCount(jdf);
        var uniformWorkStyle = ResolveUniformWorkStyle(layout, mxml);
        var signaJobPartNames = ResolveSignaJobPartNames(mxml);

        // GeneratorOptions defaults favor cockpit importability over completeness.
        var generatorOptions = new GeneratorOptions
        {
            JobId = jdf.Root.JobId ?? options.FallbackJobId,
            JobPartId = jdf.Root.JobPartId ?? options.FallbackJobPartId,
            DescriptiveName = jdf.Root.DescriptiveName ?? options.FallbackDescription,
            WorkStyle = uniformWorkStyle ?? MapWorkStyle(firstSheet?.WorkStyle, mxml),
            PlateWidth = plateWidth,
            PlateHeight = plateHeight,
            PaperWidth = paperWidth,
            PaperHeight = paperHeight,
            PaperRectOffsetX = paperOffsetX,
            PaperRectOffsetY = paperOffsetY,
            MarksFileName = options.MarksFileName,
            DocumentFileName = options.DocumentFileName,
            IncludeDocumentFileSpec = options.IncludeDocumentFileSpec,
            IncludeDocumentRunList = options.IncludeDocumentRunList,
            IncludeDocumentPageMapping = options.IncludeDocumentPageMapping,
            DocumentPageCount = documentPageCount,
            IncludePagePool = options.IncludePagePool,
            IncludeOutputRunList = options.IncludeOutputRunList,
            IncludeRunListClassAttributes = options.IncludeRunListClassAttributes,
            IncludeContentPlacement = contentStats.IncludeContentPlacement,
            IncludeBackContentPlacement = contentStats.IncludeBackContentPlacement,
            ContentGridColumns = contentStats.GridColumns,
            ContentGridRows = contentStats.GridRows,
            ContentTrimWidth = contentStats.TrimWidth,
            ContentTrimHeight = contentStats.TrimHeight,
            ContentOrdBase = contentStats.OrdBase,
            ContentOrdStep = contentStats.OrdStep,
            ContentNameBase = contentStats.NameBase,
            ContentNameStep = contentStats.NameStep,
            ContentIncrementPerSlot = true,
            IncludeTransferCurvePool = options.IncludeTransferCurvePool,
            PaperCtmOffsetX = -paperOffsetX,
            PaperCtmOffsetY = -paperOffsetY,
            IncludeMarksSeparations = options.IncludeMarksSeparations,
            IncludeMarksPartitions = options.IncludeMarksPartitions,
            IncludePrintingParamsPartitions = options.IncludePrintingParamsPartitions,
            IncludePaperMedia = options.IncludePaperMedia,
            IncludePlateMedia = options.IncludePlateMedia,
            IncludePaperRect = options.IncludePaperRect,
            IncludeSignaMetadata = options.IncludeSignaMetadata,
            IncludeSignaBlob = options.IncludeSignaBlob,
            IncludeSignaJdf = options.IncludeSignaJdf,
            IncludeSignaJob = options.IncludeSignaJob,
            SignaJobPartNames = signaJobPartNames,
            IncludeContentJobPart = signaJobPartNames.Count > 1,
            Signatures = signatures
        };

        return generatorOptions;
    }

    private static string MapWorkStyle(string? ssiWorkStyle, MetrixMxmlDocument? mxml)
    {
        // Prefer explicit SSI work style; fall back to the MXML printing method.
        if (!string.IsNullOrWhiteSpace(ssiWorkStyle))
        {
            var workStyle = ssiWorkStyle!;
            return workStyle switch
            {
                "PE" => "Perfecting",
                "TN" => "WorkAndTurn",
                "TO" => "WorkAndTumble",
                "SH" => "Sheetwise",
                "SF" => "Simplex",
                "SS" => "Simplex",
                "SW" => "Sheetwise",
                _ => workStyle
            };
        }

        var printingMethod = mxml?.Project.Layouts.FirstOrDefault()?.PrintingMethod;
        return printingMethod switch
        {
            "OneSided" => "SingleSided",
            "Perfected" => "Perfecting",
            "WorkAndTurn" => "WorkAndTurn",
            "WorkAndBack" => "WorkAndBack",
            _ => "Perfecting"
        };
    }

    private static List<string> ResolveSignaJobPartNames(MetrixMxmlDocument? mxml)
    {
        // Multi-product Metrix jobs become Signa job parts to keep page labels separated.
        if (mxml is null || mxml.Project.Products.Count <= 1)
        {
            return new List<string>();
        }

        var names = new List<string>();
        var index = 1;
        foreach (var product in mxml.Project.Products)
        {
            var name = product.Description ?? product.Name ?? product.Id ?? $"Part_{index}";
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
            index++;
        }

        return names;
    }

    private static bool TryParseBox(string? value, out decimal width, out decimal height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return false;
        }

        if (!decimal.TryParse(parts[2], out width) ||
            !decimal.TryParse(parts[3], out height))
        {
            return false;
        }

        if (decimal.TryParse(parts[0], out var left) &&
            decimal.TryParse(parts[1], out var bottom))
        {
            width -= left;
            height -= bottom;
        }

        return true;
    }

    private static bool TryParseDimension(string? value, out decimal width, out decimal height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        return decimal.TryParse(parts[0], out width)
               && decimal.TryParse(parts[1], out height);
    }

    private static int ResolveDocumentPageCount(MetrixJdfDocument jdf)
    {
        var docRef = jdf.GetRunListRef("Document");
        var runList = jdf.FindRunListById(docRef);
        if (runList is null)
        {
            return 1;
        }

        if (int.TryParse(runList.NPage, out var npage) && npage > 0)
        {
            return npage;
        }

        return runList.PageList.Count > 0 ? runList.PageList.Count : 1;
    }

    private static string? ResolveUniformWorkStyle(MetrixLayout? layout, MetrixMxmlDocument? mxml)
    {
        if (layout is null)
        {
            return null;
        }

        var workStyles = layout.Signatures
            .SelectMany(signature => signature.Sheets)
            .Select(sheet => MapWorkStyle(sheet.WorkStyle, mxml))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return workStyles.Count == 1 ? workStyles[0] : null;
    }


    private static (decimal X, decimal Y) ResolvePaperOffset(
        MetrixSurface? surface,
        decimal plateWidth,
        decimal plateHeight,
        decimal paperWidth,
        decimal paperHeight)
    {
        if (surface is null)
        {
            return (0, 0);
        }

        if (TryParseDimension(surface.MediaOrigin, out var originX, out var originY))
        {
            return (originX, originY);
        }

        if (TryParseBox(surface.SurfaceContentsBox, out var scbWidth, out var scbHeight))
        {
            var offsetX = Math.Max(0, (scbWidth - paperWidth) / 2);
            var offsetY = Math.Max(0, (scbHeight - paperHeight) / 2);
            return (offsetX, offsetY);
        }

        var fallbackX = Math.Max(0, (plateWidth - paperWidth) / 2);
        var fallbackY = Math.Max(0, (plateHeight - paperHeight) / 2);
        return (fallbackX, fallbackY);
    }

    private static ContentStats ResolveContentStats(MetrixLayout? layout)
    {
        if (layout is null)
        {
            return ContentStats.Empty;
        }

        var surfaces = layout.Signatures
            .SelectMany(signature => signature.Sheets)
            .SelectMany(sheet => sheet.Surfaces)
            .ToList();

        var maxContent = surfaces.Max(surface => surface.ContentObjects.Count);
        if (maxContent <= 0)
        {
            return ContentStats.Empty;
        }

        var columns = (int)Math.Ceiling(Math.Sqrt(maxContent));
        var rows = (int)Math.Ceiling(maxContent / (double)columns);

        var firstContent = surfaces
            .SelectMany(surface => surface.ContentObjects)
            .FirstOrDefault();

        var trimWidth = ContentStats.DefaultTrimWidth;
        var trimHeight = ContentStats.DefaultTrimHeight;
        if (firstContent is not null &&
            TryParseDimension(firstContent.TrimSize, out var parsedWidth, out var parsedHeight))
        {
            trimWidth = parsedWidth;
            trimHeight = parsedHeight;
        }

        var includeBack = surfaces.Any(surface =>
            string.Equals(surface.Side, "Back", StringComparison.OrdinalIgnoreCase));

        var ords = surfaces
            .SelectMany(surface => surface.ContentObjects)
            .Select(content => ParseOrd(content.Ord))
            .Where(ord => ord is not null)
            .Select(ord => ord!.Value)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        var ordBase = ords.Count > 0 ? ords.Min() : 0;
        var ordStep = ords.Count > 1 ? ords[1] - ords[0] : 1;

        return new ContentStats(true, includeBack, columns, rows, trimWidth, trimHeight, ordBase, ordStep, ordBase, ordStep);
    }

    private static int? ParseOrd(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}

internal readonly record struct ContentStats(
    bool IncludeContentPlacement,
    bool IncludeBackContentPlacement,
    int GridColumns,
    int GridRows,
    decimal TrimWidth,
    decimal TrimHeight,
    int OrdBase,
    int OrdStep,
    int NameBase,
    int NameStep)
{
    public static ContentStats Empty => new(false, false, 1, 1, DefaultTrimWidth, DefaultTrimHeight, 0, 1, 1, 1);
    public const decimal DefaultTrimWidth = 612;
    public const decimal DefaultTrimHeight = 792;
}

public sealed class MetrixToSignaOptions
{
    public string FallbackJobId { get; set; } = "MetrixJob";
    public string FallbackJobPartId { get; set; } = "Part_0";
    public string FallbackDescription { get; set; } = "Metrix job";
    public string FallbackSignatureName { get; set; } = "Signature_1";
    public string FallbackSheetName { get; set; } = "Sheet_1";
    public decimal FallbackPlateWidth { get; set; } = 3000;
    public decimal FallbackPlateHeight { get; set; } = 2200;
    public decimal FallbackPaperWidth { get; set; } = 2592;
    public decimal FallbackPaperHeight { get; set; } = 1728;
    public decimal FallbackPaperOffsetX { get; set; }
    public decimal FallbackPaperOffsetY { get; set; }
    public bool UseSurfaceDimensions { get; set; } = true;

    public string MarksFileName { get; set; } = "marks.pdf";
    public string DocumentFileName { get; set; } = "content.pdf";
    public bool IncludeDocumentFileSpec { get; set; } = true;
    public bool IncludeDocumentRunList { get; set; } = true;
    public bool IncludeDocumentPageMapping { get; set; } = true;
    public bool IncludePagePool { get; set; } = true;
    public bool IncludeOutputRunList { get; set; } = true;
    public bool IncludeRunListClassAttributes { get; set; } = true;
    public bool CenterPaperRect { get; set; } = true;
    public bool IncludeTransferCurvePool { get; set; } = true;
    public bool IncludeMarksSeparations { get; set; } = true;
    public bool IncludeMarksPartitions { get; set; } = true;
    public bool IncludePrintingParamsPartitions { get; set; } = true;
    public bool IncludePaperMedia { get; set; } = true;
    public bool IncludePlateMedia { get; set; } = true;
    public bool IncludePaperRect { get; set; } = true;
    public bool IncludeSignaMetadata { get; set; } = true;
    public bool IncludeSignaBlob { get; set; } = true;
    public bool IncludeSignaJdf { get; set; } = true;
    public bool IncludeSignaJob { get; set; } = true;
}
