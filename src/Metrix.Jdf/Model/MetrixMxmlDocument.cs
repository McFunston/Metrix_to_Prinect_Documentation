using System.Xml.Linq;

namespace Metrix.Jdf;

public sealed class MetrixMxmlDocument
{
    // Companion MXML data used to fill gaps in the JDF (paper, marks, products).
    public MetrixMxmlDocument(
        string sourcePath,
        XNamespace mxmlNamespace,
        XDocument xmlDocument,
        string? units,
        MetrixMxmlResourcePool resourcePool,
        MetrixMxmlProject project)
    {
        SourcePath = sourcePath;
        MxmlNamespace = mxmlNamespace;
        XmlDocument = xmlDocument;
        Units = units;
        ResourcePool = resourcePool;
        Project = project;
    }

    public string SourcePath { get; }
    public XNamespace MxmlNamespace { get; }
    public XDocument XmlDocument { get; }
    public string? Units { get; }
    public MetrixMxmlResourcePool ResourcePool { get; }
    public MetrixMxmlProject Project { get; }
}

public sealed class MetrixMxmlResourcePool
{
    // ResourcePool is the primary source for folding schemes, marks, and stock metadata.
    public List<MetrixMxmlFoldingScheme> FoldingSchemes { get; } = new();
    public List<MetrixMxmlMarkFile> MarkFiles { get; } = new();
    public List<MetrixMxmlStock> Stocks { get; } = new();
}

public sealed class MetrixMxmlFoldingScheme
{
    // Folding scheme entries map to JDFFoldCatalog identifiers.
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? JdfFoldCatalog { get; set; }
}

public sealed class MetrixMxmlMarkFile
{
    // Marks file metadata (filename and size) referenced by layout marks.
    public string? Id { get; set; }
    public string? FileName { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
}

public sealed class MetrixMxmlStock
{
    // Stock metadata captures paper descriptions and weight/thickness defaults.
    public string? Id { get; set; }
    public string? MisId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Vendor { get; set; }
    public string? Grade { get; set; }
    public string? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Thickness { get; set; }
    public List<MetrixMxmlStockSheet> StockSheets { get; } = new();
}

public sealed class MetrixMxmlStockSheet
{
    // StockSheet records buy-sheet size and grain direction.
    public string? Id { get; set; }
    public string? MisId { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? Grain { get; set; }
    public string? BuySheetLongGrain { get; set; }
    public string? Thickness { get; set; }
}

public sealed class MetrixMxmlProject
{
    // Project ties products and layouts to a single Metrix job/estimate.
    public string? ProjectId { get; set; }
    public string? Name { get; set; }
    public string? MisId { get; set; }
    public List<MetrixMxmlProduct> Products { get; } = new();
    public List<MetrixMxmlLayout> Layouts { get; } = new();
}

public sealed class MetrixMxmlProduct
{
    // Product metadata drives job-part labeling and trim sizes.
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? MisId { get; set; }
    public string? FinishedTrimWidth { get; set; }
    public string? FinishedTrimHeight { get; set; }
    public string? RequiredQuantity { get; set; }
    public List<MetrixMxmlPage> Pages { get; } = new();
}

public sealed class MetrixMxmlLayout
{
    // Layout entries capture printing method and stock sheet references.
    public string? Id { get; set; }
    public string? MisId { get; set; }
    public string? PrintingMethod { get; set; }
    public string? SheetsRequired { get; set; }
    public string? StockSheetRefId { get; set; }
}

public sealed class MetrixMxmlPage
{
    // Page entries are used for Folio/Number labeling in Cockpit page lists.
    public string? Folio { get; set; }
    public string? Number { get; set; }
}
