using Metrix.Jdf;
using Metrix.Jdf.Transform;
using System.Xml.Linq;
using Xunit;
using Xunit.Sdk;

public sealed class ParsingTests
{
    private static string SampleAId =>
        Environment.GetEnvironmentVariable("METRIX_SAMPLE_A_ID") ?? "S2328";

    private static string SampleBId =>
        Environment.GetEnvironmentVariable("METRIX_SAMPLE_B_ID") ?? "S2326";

    // Integration-style tests that rely on private sample bundles (not committed to repo).
    [Fact]
    public void ParseJdf_SampleA_LoadsLayoutAndRunLists()
    {
        var path = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", $"{SampleAId}.jdf");
        var document = MetrixJdfParser.Parse(path);

        Assert.Equal(SampleAId, document.Root.JobId);
        Assert.NotNull(document.Layout);
        Assert.True(document.Layout!.Signatures.Count > 0);
        Assert.Equal(2, document.RunLists.Count);

        var docRunList = document.FindRunListById(document.GetRunListRef("Document"));
        Assert.Equal("520", docRunList?.NPage);
    }

    [Fact]
    public void ParseMxml_SampleB_LoadsProjectAndLayouts()
    {
        var path = ResolveSamplePathOrSkip("Metrix_Samples", "mxml", $"{SampleBId}.mxml");
        var document = MetrixMxmlParser.Parse(path);

        Assert.Equal(SampleBId, document.Project.ProjectId);
        Assert.True(document.Project.Products.Count > 0);
        Assert.True(document.Project.Layouts.Count > 0);
        Assert.True(document.ResourcePool.FoldingSchemes.Count > 0);
    }

    [Fact]
    public void Transform_UsesFirstSheetWorkStyle_WhenAvailable()
    {
        var jdfPath = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", $"{SampleAId}.jdf");
        var mxmlPath = ResolveSamplePathOrSkip("Metrix_Samples", "mxml", $"{SampleAId}.mxml");
        var jdf = MetrixJdfParser.Parse(jdfPath);
        var mxml = MetrixMxmlParser.Parse(mxmlPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, mxml, new MetrixToSignaOptions());

        Assert.True(
            string.Equals(options.WorkStyle, "SS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.WorkStyle, "Simplex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Transform_UsesSurfaceDimensions_WhenConfigured()
    {
        var jdfPath = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", $"{SampleBId}.jdf");
        var jdf = MetrixJdfParser.Parse(jdfPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, null, new MetrixToSignaOptions
        {
            UseSurfaceDimensions = true
        });

        Assert.InRange(options.PlateWidth, 2919.3m, 2919.4m);
        Assert.InRange(options.PlateHeight, 2238.7m, 2238.8m);
    }

    [Fact]
    public void Transform_MapsSheetwiseCodeToWorkAndBack()
    {
        var layout = new MetrixLayout();
        var signature = new MetrixSignature { Name = "Sig001" };
        var sheet = new MetrixSheet { Name = "Sheet1", WorkStyle = "SH" };
        sheet.Surfaces.Add(new MetrixSurface { Side = "Front" });
        signature.Sheets.Add(sheet);
        layout.Signatures.Add(signature);

        var jdf = CreateMinimalJdf(layout);
        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, null, new MetrixToSignaOptions());

        Assert.Equal("WorkAndBack", options.WorkStyle);
        Assert.Single(options.Signatures);
        Assert.Equal("WorkAndBack", options.Signatures[0].WorkStyle);
    }

    [Fact]
    public void Transform_MapsOneSidedPrintingMethodToSimplex()
    {
        var layout = new MetrixLayout();
        var signature = new MetrixSignature { Name = "Sig001" };
        var sheet = new MetrixSheet { Name = "Sheet1" };
        sheet.Surfaces.Add(new MetrixSurface { Side = "Front" });
        signature.Sheets.Add(sheet);
        layout.Signatures.Add(signature);

        var jdf = CreateMinimalJdf(layout);
        var mxml = CreateMinimalMxml("OneSided");
        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, mxml, new MetrixToSignaOptions());

        Assert.Equal("Simplex", options.WorkStyle);
        Assert.Single(options.Signatures);
        Assert.Equal("Simplex", options.Signatures[0].WorkStyle);
    }

    private static string ResolveSamplePathOrSkip(params string[] parts)
    {
        static string? ResolveIfExists(string root, string[] parts)
        {
            var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
            return File.Exists(path) ? path : null;
        }

        var configuredRoot = Environment.GetEnvironmentVariable("METRIX_PRIVATE_SAMPLES");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var candidate = Path.Combine(configuredRoot, parts[0]);
            if (Directory.Exists(candidate))
            {
                var resolved = ResolveIfExists(configuredRoot, parts);
                if (resolved is not null)
                {
                    return resolved;
                }
            }
        }

        var homeRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Metrix_to_Cockpit_PrivateSamples");
        if (Directory.Exists(Path.Combine(homeRoot, parts[0])))
        {
            var resolved = ResolveIfExists(homeRoot, parts);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        // Walk upward from bin/ to locate the private sample root in local setups.
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, parts[0]);
            if (Directory.Exists(candidate))
            {
                var resolved = ResolveIfExists(current.FullName, parts);
                if (resolved is not null)
                {
                    return resolved;
                }
            }

            current = current.Parent;
        }

        throw SkipException.ForSkip($"Private samples not found starting from {baseDir}.");
    }

    private static MetrixJdfDocument CreateMinimalJdf(MetrixLayout layout)
    {
        return new MetrixJdfDocument(
            sourcePath: "in-memory",
            root: new MetrixJdfNode
            {
                JobId = "Job001",
                JobPartId = "Part001",
                DescriptiveName = "Test"
            },
            jdfNamespace: XNamespace.Get("http://www.CIP4.org/JDFSchema_1_1"),
            hdmNamespace: XNamespace.Get("www.heidelberg.com/schema/HDM"),
            ssiNamespace: XNamespace.Get("http://www.creo.com/SSI/JDFExtensions.xsd"),
            xmlDocument: new XDocument(new XElement("JDF")),
            layout: layout,
            runLists: new List<MetrixRunListResource>(),
            resourceLinks: new List<MetrixResourceLink>());
    }

    private static MetrixMxmlDocument CreateMinimalMxml(string printingMethod)
    {
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
}
