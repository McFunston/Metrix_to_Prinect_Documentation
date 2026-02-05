using Metrix.Jdf;
using Metrix.Jdf.Transform;
using Signa.Jdf;
using System.Xml.Linq;
using Xunit;

public sealed class TransformPipelineTests
{
    [Fact]
    public void TransformPipeline_OneSidedMxml_ProducesSimplexPrintingParams()
    {
        var signa = RunTransformPipeline(workStyleCode: null, printingMethod: "OneSided", includeBackSurface: false);
        var ns = signa.Root!.Name.Namespace;
        var resourcePool = signa.Root.Element(ns + "ResourcePool")!;

        var printingParams = resourcePool.Elements(ns + "ConventionalPrintingParams").First();
        Assert.Equal("Simplex", Attr(printingParams, "WorkStyle"));

        var sigPart = printingParams.Elements(ns + "ConventionalPrintingParams").First();
        Assert.Equal("Simplex", Attr(sigPart, "WorkStyle"));

        var sheetPart = sigPart.Elements(ns + "ConventionalPrintingParams").First();
        var printSides = sheetPart.Elements(ns + "ConventionalPrintingParams")
            .Select(side => Attr(side, "Side"))
            .ToList();
        Assert.Single(printSides);
        Assert.Equal("Front", printSides[0]);

    }

    [Fact]
    public void TransformPipeline_SimplexCode_ProducesFrontOnlyMarks()
    {
        var signa = RunTransformPipeline(workStyleCode: "SS", printingMethod: null, includeBackSurface: true);
        var ns = signa.Root!.Name.Namespace;
        var resourcePool = signa.Root.Element(ns + "ResourcePool")!;
        var marksRunList = resourcePool.Elements(ns + "RunList")
            .First(element => string.Equals(Attr(element, "ID"), "r_marks", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("4", Attr(marksRunList, "NPage"));

        var marksSheetPart = marksRunList.Elements(ns + "RunList")
            .First(part => string.Equals(Attr(part, "SignatureName"), "Sig001", StringComparison.OrdinalIgnoreCase))
            .Elements(ns + "RunList")
            .First(part => string.Equals(Attr(part, "SheetName"), "Sheet1", StringComparison.OrdinalIgnoreCase));

        var marksSides = marksSheetPart.Elements(ns + "RunList").ToList();
        Assert.Single(marksSides);
        Assert.Equal("Front", Attr(marksSides[0], "Side"));
        Assert.Equal("4", Attr(marksSides[0], "NPage"));
    }

    [Fact]
    public void TransformPipeline_SheetwiseCode_ProducesWorkAndBackWithFrontBackMarks()
    {
        var signa = RunTransformPipeline(workStyleCode: "SH", printingMethod: null, includeBackSurface: true);
        var ns = signa.Root!.Name.Namespace;
        var resourcePool = signa.Root.Element(ns + "ResourcePool")!;

        var printingParams = resourcePool.Elements(ns + "ConventionalPrintingParams").First();
        Assert.Equal("WorkAndBack", Attr(printingParams, "WorkStyle"));

        var sigPart = printingParams.Elements(ns + "ConventionalPrintingParams").First();
        Assert.Equal("WorkAndBack", Attr(sigPart, "WorkStyle"));

        var sheetPart = sigPart.Elements(ns + "ConventionalPrintingParams").First();
        var printSides = sheetPart.Elements(ns + "ConventionalPrintingParams")
            .Select(side => Attr(side, "Side"))
            .ToList();
        Assert.Equal(2, printSides.Count);
        Assert.Contains("Front", printSides);
        Assert.Contains("Back", printSides);

        var marksRunList = resourcePool.Elements(ns + "RunList")
            .First(element => string.Equals(Attr(element, "ID"), "r_marks", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("4", Attr(marksRunList, "NPage"));

        var marksSheetPart = marksRunList.Elements(ns + "RunList")
            .First(part => string.Equals(Attr(part, "SignatureName"), "Sig001", StringComparison.OrdinalIgnoreCase))
            .Elements(ns + "RunList")
            .First(part => string.Equals(Attr(part, "SheetName"), "Sheet1", StringComparison.OrdinalIgnoreCase));

        var marksSides = marksSheetPart.Elements(ns + "RunList").ToList();
        Assert.Equal(2, marksSides.Count);
        Assert.Equal("2", Attr(marksSides[0], "NPage"));
        Assert.Equal("2", Attr(marksSides[1], "NPage"));
        Assert.Contains("Front", marksSides.Select(side => Attr(side, "Side")));
        Assert.Contains("Back", marksSides.Select(side => Attr(side, "Side")));
    }

    private static XDocument RunTransformPipeline(string? workStyleCode, string? printingMethod, bool includeBackSurface)
    {
        var metrix = CreateMetrixDocument(workStyleCode, includeBackSurface);
        var mxml = CreateMxmlDocument(printingMethod);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(metrix, mxml, new MetrixToSignaOptions
        {
            MarksFileName = "./Content/marks.pdf",
            DocumentFileName = "./Content/content.pdf",
            IncludeDocumentFileSpec = false,
            IncludeDocumentPageMapping = true,
            IncludePrintingParamsPartitions = true,
            IncludePaperMedia = true,
            IncludePlateMedia = true,
            IncludeMarksSeparations = true,
            IncludeMarksPartitions = true,
            IncludePaperRect = true,
            IncludeSignaBlob = false
        });

        var signa = JdfGenerator.Generate(options);
        MetrixContentPostProcessor.ApplyContentPlacement(signa, metrix, mxml);
        return signa;
    }

    private static MetrixJdfDocument CreateMetrixDocument(string? workStyleCode, bool includeBackSurface)
    {
        var layout = new MetrixLayout { Id = "r_layout" };
        var signature = new MetrixSignature { Name = "Sig001" };
        var sheet = new MetrixSheet
        {
            Name = "Sheet1",
            WorkStyle = workStyleCode,
            SurfaceContentsBox = "0 0 1000 700"
        };

        sheet.Surfaces.Add(new MetrixSurface
        {
            Side = "Front",
            Dimension = "900 600",
            SurfaceContentsBox = "0 0 1000 700"
        });

        if (includeBackSurface)
        {
            sheet.Surfaces.Add(new MetrixSurface
            {
                Side = "Back",
                Dimension = "900 600",
                SurfaceContentsBox = "0 0 1000 700"
            });
        }

        signature.Sheets.Add(sheet);
        layout.Signatures.Add(signature);

        var marksRunList = new MetrixRunListResource
        {
            Id = "r_marks",
            PartIdKeys = "SignatureName SheetName Side",
            NPage = "4"
        };
        marksRunList.Entries.Add(new MetrixRunListEntry
        {
            NPage = "4",
            Pages = "0 ~ 3",
            Status = "Available",
            FileSpecUrl = "./Content/marks.pdf"
        });

        return new MetrixJdfDocument(
            sourcePath: "in-memory",
            root: new MetrixJdfNode
            {
                Id = "JDF_0000",
                JobId = "Job001",
                JobPartId = "Part001",
                DescriptiveName = "Unit test job",
                Type = "Imposition",
                Status = "Ready"
            },
            jdfNamespace: XNamespace.Get("http://www.CIP4.org/JDFSchema_1_1"),
            hdmNamespace: XNamespace.Get("www.heidelberg.com/schema/HDM"),
            ssiNamespace: XNamespace.Get("http://www.creo.com/SSI/JDFExtensions.xsd"),
            xmlDocument: new XDocument(new XElement("JDF")),
            layout: layout,
            runLists: new[] { marksRunList },
            resourceLinks: new[]
            {
                new MetrixResourceLink
                {
                    LinkType = "RunListLink",
                    ProcessUsage = "Marks",
                    RefId = "r_marks"
                }
            });
    }

    private static MetrixMxmlDocument? CreateMxmlDocument(string? printingMethod)
    {
        if (string.IsNullOrWhiteSpace(printingMethod))
        {
            return null;
        }

        var project = new MetrixMxmlProject();
        project.Layouts.Add(new MetrixMxmlLayout { PrintingMethod = printingMethod });

        return new MetrixMxmlDocument(
            sourcePath: "in-memory",
            mxmlNamespace: XNamespace.Get("https://www.imposition.com"),
            xmlDocument: new XDocument(new XElement("MetrixProject")),
            units: null,
            resourcePool: new MetrixMxmlResourcePool(),
            project: project);
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }
}
