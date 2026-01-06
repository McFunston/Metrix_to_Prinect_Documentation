using System.Xml.Linq;

namespace Metrix.Jdf;

public sealed class MetrixMxmlDocument
{
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
    public List<MetrixMxmlFoldingScheme> FoldingSchemes { get; } = new();
    public List<MetrixMxmlMarkFile> MarkFiles { get; } = new();
    public List<MetrixMxmlStock> Stocks { get; } = new();
}

public sealed class MetrixMxmlFoldingScheme
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? JdfFoldCatalog { get; set; }
}

public sealed class MetrixMxmlMarkFile
{
    public string? Id { get; set; }
    public string? FileName { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
}

public sealed class MetrixMxmlStock
{
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
    public string? ProjectId { get; set; }
    public string? Name { get; set; }
    public string? MisId { get; set; }
    public List<MetrixMxmlProduct> Products { get; } = new();
    public List<MetrixMxmlLayout> Layouts { get; } = new();
}

public sealed class MetrixMxmlProduct
{
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
    public string? Id { get; set; }
    public string? MisId { get; set; }
    public string? PrintingMethod { get; set; }
    public string? SheetsRequired { get; set; }
    public string? StockSheetRefId { get; set; }
}

public sealed class MetrixMxmlPage
{
    public string? Folio { get; set; }
    public string? Number { get; set; }
}
