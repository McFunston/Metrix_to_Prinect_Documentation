using Metrix.Jdf;
using Metrix.Jdf.Transform;
using Xunit;
using Xunit.Sdk;

public sealed class ParsingTests
{
    // Integration-style tests that rely on private sample bundles (not committed to repo).
    [Fact]
    public void ParseJdf_SampleA_LoadsLayoutAndRunLists()
    {
        var path = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", "Sample_A.jdf");
        var document = MetrixJdfParser.Parse(path);

        Assert.Equal("Sample_A", document.Root.JobId);
        Assert.NotNull(document.Layout);
        Assert.True(document.Layout!.Signatures.Count > 0);
        Assert.Equal(2, document.RunLists.Count);

        var docRunList = document.FindRunListById(document.GetRunListRef("Document"));
        Assert.Equal("520", docRunList?.NPage);
    }

    [Fact]
    public void ParseMxml_SampleB_LoadsProjectAndLayouts()
    {
        var path = ResolveSamplePathOrSkip("Metrix_Samples", "mxml", "Sample_B.mxml");
        var document = MetrixMxmlParser.Parse(path);

        Assert.Equal("Sample_B", document.Project.ProjectId);
        Assert.True(document.Project.Products.Count > 0);
        Assert.True(document.Project.Layouts.Count > 0);
        Assert.True(document.ResourcePool.FoldingSchemes.Count > 0);
    }

    [Fact]
    public void Transform_UsesFirstSheetWorkStyle_WhenAvailable()
    {
        var jdfPath = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", "Sample_A.jdf");
        var mxmlPath = ResolveSamplePathOrSkip("Metrix_Samples", "mxml", "Sample_A.mxml");
        var jdf = MetrixJdfParser.Parse(jdfPath);
        var mxml = MetrixMxmlParser.Parse(mxmlPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, mxml, new MetrixToSignaOptions());

        Assert.Equal("SS", options.WorkStyle);
    }

    [Fact]
    public void Transform_UsesSurfaceDimensions_WhenConfigured()
    {
        var jdfPath = ResolveSamplePathOrSkip("Metrix_Samples", "jdf", "Sample_B.jdf");
        var jdf = MetrixJdfParser.Parse(jdfPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, null, new MetrixToSignaOptions
        {
            UseSurfaceDimensions = true
        });

        Assert.InRange(options.PlateWidth, 2919.3m, 2919.4m);
        Assert.InRange(options.PlateHeight, 2238.7m, 2238.8m);
    }

    private static string ResolveSamplePathOrSkip(params string[] parts)
    {
        // Walk upward from bin/ to locate the private sample root in local setups.
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, parts[0]);
            if (Directory.Exists(candidate))
            {
                return Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            }

            current = current.Parent;
        }

        throw new SkipException($"Private samples not found starting from {baseDir}.");
    }
}
