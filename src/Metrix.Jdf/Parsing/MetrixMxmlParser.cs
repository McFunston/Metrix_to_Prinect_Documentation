using System.Xml.Linq;

namespace Metrix.Jdf;

public static class MetrixMxmlParser
{
    // Parses Metrix MXML companion data that fills gaps in the JDF (paper, marks, products).
    public static MetrixMxmlDocument Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        // Preserve whitespace so numeric strings remain exactly as exported.
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidDataException("Missing MetrixXML root element.");
        var ns = root.Name.Namespace;
        var units = Attr(root, "Units");

        var resourcePool = new MetrixMxmlResourcePool();
        // ResourcePool carries folding schemes, marks files, and stock definitions.
        var resourcePoolElement = root.Element(ns + "ResourcePool");
        if (resourcePoolElement is not null)
        {
            foreach (var foldingScheme in resourcePoolElement.Elements(ns + "FoldingScheme"))
            {
                resourcePool.FoldingSchemes.Add(new MetrixMxmlFoldingScheme
                {
                    Id = Attr(foldingScheme, "ID"),
                    Name = Attr(foldingScheme, "Name"),
                    JdfFoldCatalog = Attr(foldingScheme, "JDFFoldCatalog")
                });
            }

            foreach (var markFile in resourcePoolElement.Elements(ns + "MarkFile"))
            {
                resourcePool.MarkFiles.Add(new MetrixMxmlMarkFile
                {
                    Id = Attr(markFile, "ID"),
                    FileName = Attr(markFile, "FileName"),
                    Width = Attr(markFile, "Width"),
                    Height = Attr(markFile, "Height")
                });
            }

            foreach (var stockElement in resourcePoolElement.Elements(ns + "Stock"))
            {
                var stock = new MetrixMxmlStock
                {
                    Id = Attr(stockElement, "ID"),
                    MisId = Attr(stockElement, "MIS_ID"),
                    Name = Attr(stockElement, "Name"),
                    Description = Attr(stockElement, "Description"),
                    Vendor = Attr(stockElement, "Vendor"),
                    Grade = Attr(stockElement, "Grade"),
                    Weight = Attr(stockElement, "Weight"),
                    WeightUnit = Attr(stockElement, "WeightUnit"),
                    Thickness = Attr(stockElement, "Thickness")
                };

                foreach (var stockSheetElement in stockElement.Elements(ns + "StockSheet"))
                {
                    // StockSheet references usually carry buy-sheet size and grain info.
                    stock.StockSheets.Add(new MetrixMxmlStockSheet
                    {
                        Id = Attr(stockSheetElement, "ID"),
                        MisId = Attr(stockSheetElement, "MIS_ID"),
                        Width = Attr(stockSheetElement, "Width"),
                        Height = Attr(stockSheetElement, "Height"),
                        Grain = Attr(stockSheetElement, "Grain"),
                        BuySheetLongGrain = Attr(stockSheetElement, "BuySheetLongGrain"),
                        Thickness = Attr(stockSheetElement, "Thickness")
                    });
                }

                resourcePool.Stocks.Add(stock);
            }
        }

        // Project holds products, layouts, and identifiers used to build job parts.
        var projectElement = root.Element(ns + "Project") ?? throw new InvalidDataException("Missing Project element.");
        var project = new MetrixMxmlProject
        {
            ProjectId = Attr(projectElement, "ProjectID"),
            Name = Attr(projectElement, "Name"),
            MisId = Attr(projectElement, "MIS_ID")
        };

        var productPool = projectElement.Element(ns + "ProductPool");
        if (productPool is not null)
        {
            // ProductPool drives page labels and multi-product job-part ranges.
            foreach (var productElement in productPool.Elements(ns + "Product"))
            {
                var product = new MetrixMxmlProduct
                {
                    Id = Attr(productElement, "ID"),
                    Name = Attr(productElement, "Name"),
                    Description = Attr(productElement, "Description"),
                    Type = Attr(productElement, "Type"),
                    MisId = Attr(productElement, "MIS_ID"),
                    FinishedTrimWidth = Attr(productElement, "FinishedTrimWidth"),
                    FinishedTrimHeight = Attr(productElement, "FinishedTrimHeight"),
                    RequiredQuantity = Attr(productElement, "RequiredQuantity")
                };

                var pagePool = productElement.Element(ns + "PagePool");
                if (pagePool is not null)
                {
                    // Page numbers/folios are used for ContentObject labeling in Cockpit.
                    foreach (var page in pagePool.Elements(ns + "Page"))
                    {
                        product.Pages.Add(new MetrixMxmlPage
                        {
                            Folio = Attr(page, "Folio"),
                            Number = Attr(page, "Number")
                        });
                    }
                }

                project.Products.Add(product);
            }
        }

        var layoutPool = projectElement.Element(ns + "LayoutPool");
        if (layoutPool is not null)
        {
            // LayoutPool encodes printing methods and links to stock sheets.
            foreach (var layoutElement in layoutPool.Elements(ns + "Layout"))
            {
                var stockSheetRef = layoutElement.Element(ns + "StockSheetRef");
                project.Layouts.Add(new MetrixMxmlLayout
                {
                    Id = Attr(layoutElement, "ID"),
                    MisId = Attr(layoutElement, "MIS_ID"),
                    PrintingMethod = Attr(layoutElement, "PrintingMethod"),
                    SheetsRequired = Attr(layoutElement, "SheetsRequired"),
                    StockSheetRefId = stockSheetRef is null ? null : Attr(stockSheetRef, "rRef")
                });
            }
        }

        return new MetrixMxmlDocument(
            path,
            ns,
            document,
            units,
            resourcePool,
            project);
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }
}
