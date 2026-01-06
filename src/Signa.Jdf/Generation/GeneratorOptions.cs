namespace Signa.Jdf;

public sealed class GeneratorOptions
{
    public string JobId { get; init; } = "Job001";
    public string JobPartId { get; init; } = "Part001";
    public string Status { get; init; } = "Waiting";
    public string Version { get; init; } = "1.3";
    public string MaxVersion { get; init; } = "1.7";
    public string DescriptiveName { get; init; } = "unnamed";
    public string WorkStyle { get; init; } = "Perfecting";
    public string SignatureName { get; init; } = "Sig001";
    public string SheetName { get; init; } = "Sheet1";
    public IReadOnlyList<string> SheetNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SignatureDefinition> Signatures { get; init; } = Array.Empty<SignatureDefinition>();
    public bool IncludeBackSide { get; init; } = true;
    public bool IncludePagePool { get; init; } = true;
    public bool IncludeOutputRunList { get; init; } = true;
    public bool IncludeAssembly { get; init; } = false;
    public bool IncludeSignaMetadata { get; init; } = true;
    public bool IncludeSignaBlob { get; init; } = true;
    public bool IncludeSignaJdf { get; init; } = true;
    public bool IncludeSignaJob { get; init; } = true;
    public bool IncludeSignaJobParts { get; init; } = true;
    public string SignaJobPartName { get; init; } = "A";
    public IReadOnlyList<string> SignaJobPartNames { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> SignatureJobParts { get; init; } = new Dictionary<string, string>();
    public string SignaBlobUrl { get; init; } = "SignaData.sdf";
    public string SignaJdfUrl { get; init; } = "data.jdf";
    public string DocumentFileName { get; init; } = "data.pdf";
    public string MarksFileName { get; init; } = "data.pdf";
    public string FileSpecMimeType { get; init; } = "application/pdf";
    public string DocumentRunListPartIdKeys { get; init; } = "Run";
    public string MarksRunListPartIdKeys { get; init; } = "SignatureName SheetName Side";
    public string RunListHdmOfw { get; init; } = "1.0";
    public string? SignaProductName { get; init; } = "PrinectSignaStation";
    public string? SignaProductMajorVersion { get; init; } = "21";
    public string? SignaProductMinorVersion { get; init; } = "10";
    public bool IncludePaperMedia { get; init; }
    public string PaperMediaId { get; init; } = "r_media_paper";
    public decimal PaperWidth { get; init; } = 2592;
    public decimal PaperHeight { get; init; } = 1728;
    public decimal? PaperThickness { get; init; }
    public decimal? PaperWeight { get; init; }
    public string? PaperGrainDirection { get; init; }
    public string? PaperFeedDirection { get; init; }
    public string? PaperBrand { get; init; }
    public string? PaperProductId { get; init; }
    public string? PaperGrade { get; init; }
    public bool IncludeContentPlacement { get; init; }
    public bool IncludeContentJobPart { get; init; }
    public bool IncludeContentRunlistIndex { get; init; }
    public IReadOnlySet<string> ContentJobPartSignatures { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> ContentJobPartSheets { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public decimal ContentTrimWidth { get; init; } = 612;
    public decimal ContentTrimHeight { get; init; } = 792;
    public decimal ContentOffsetX { get; init; }
    public decimal ContentOffsetY { get; init; }
    public int ContentOrdBase { get; init; } = 0;
    public int ContentOrdStep { get; init; } = 1;
    public int ContentNameBase { get; init; } = 1;
    public int ContentNameStep { get; init; } = 1;
    public bool ContentIncrementPerSheet { get; init; } = false;
    public bool ContentIncrementPerSlot { get; init; } = false;
    public int ContentGridColumns { get; init; } = 1;
    public int ContentGridRows { get; init; } = 1;
    public decimal ContentGapX { get; init; } = 0m;
    public decimal ContentGapY { get; init; } = 0m;
    public bool ContentGridReverseBackColumnOrder { get; init; } = false;
    public bool ContentGridReverseBackRowOrder { get; init; } = false;
    public string? ContentAssemblyIdBase { get; init; }
    public string? ContentAssemblyIdBase2 { get; init; }
    public int ContentAssemblySplitIndex { get; init; } = 0;
    public int ContentAssemblyIdStart { get; init; } = 1;
    public int ContentAssemblyIdStep { get; init; } = 1;
    public bool IncludeBackContentPlacement { get; init; }
    public decimal BackContentOffsetX { get; init; }
    public decimal BackContentOffsetY { get; init; }
    public bool MirrorBackContent { get; init; }
    public decimal PaperRectOffsetX { get; init; }
    public decimal PaperRectOffsetY { get; init; }
    public bool IncludePaperRect { get; init; } = true;
    public bool IncludeDocumentPageMapping { get; init; }
    public bool IncludeMarksPartitions { get; init; }
    public bool MarksResetLogicalPagePerSignature { get; init; }
    public bool MarksSplitRunListPerSignature { get; init; }
    public bool MarksResetLogicalPagePerSheet { get; init; }
    public int MarksPageCount { get; init; } = 2;
    public int MarksPagesPerSide { get; init; } = 2;
    public bool MarksIncludeBackSide { get; init; }
    public int DocumentPageCount { get; init; } = 1;
    public bool DocumentPagesPerSheet { get; init; } = false;
    public bool IncludeMarksSeparations { get; init; }
    public bool IncludeRunListClassAttributes { get; init; } = true;
    public bool IncludeDocumentFileSpec { get; init; } = true;
    public bool IncludeDocumentRunList { get; init; } = true;
    public bool IncludeMarkObjectGeometry { get; init; }
    public bool IncludePrintingParamsPartitions { get; init; }
    public bool IncludeStrippingParams { get; init; }
    public string StrippingParamsId { get; init; } = "r_strip";
    public string? StrippingWorkStyle { get; init; }
    public decimal StripRelLeft { get; init; } = 0;
    public decimal StripRelBottom { get; init; } = 0;
    public decimal StripRelRight { get; init; } = 1;
    public decimal StripRelTop { get; init; } = 1;
    public decimal StripTrimWidth { get; init; } = 612;
    public decimal StripTrimHeight { get; init; } = 792;
    public string? StripSheetLay { get; init; }
    public bool IncludeBinderySignature { get; init; }
    public bool BinderySignaturePerSignature { get; init; }
    public string BinderySignatureId { get; init; } = "r_bindery";
    public string AssemblyId { get; init; } = "r_assembly";
    public string AssemblyOrder { get; init; } = "Gathering";
    public string BinderySignatureType { get; init; } = "Fold";
    public string BinderySignatureNumberUp { get; init; } = "1 1";
    public string BinderyFrontPages { get; init; } = "1";
    public string BinderyBackPages { get; init; } = "2";
    public string BinderyFrontOrientation { get; init; } = "0";
    public string BinderyBackOrientation { get; init; } = "0";
    public bool IncludeTransferCurvePool { get; init; }
    public string TransferCurvePoolId { get; init; } = "r_tcp";
    public decimal PaperCtmOffsetX { get; init; }
    public decimal PaperCtmOffsetY { get; init; }
    public bool IncludePlateMedia { get; init; }
    public string PlateMediaId { get; init; } = "r_media_plate";
    public decimal PlateWidth { get; init; } = 2592;
    public decimal PlateHeight { get; init; } = 1728;
    public decimal? PlateLeadingEdge { get; init; }
    public IReadOnlyList<string> Types { get; init; } = new[]
    {
        "Imposition",
        "ConventionalPrinting",
        "Cutting",
        "Folding",
        "Trimming"
    };
}

public sealed record SignatureDefinition(string Name, IReadOnlyList<string> Sheets, string? WorkStyle);
