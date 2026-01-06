using Metrix.Jdf;
using Metrix.Jdf.Transform;
using Xunit;

public sealed class ParsingTests
{
    [Fact]
    public void ParseJdf_S2328_LoadsLayoutAndRunLists()
    {
        var path = ResolveSamplePath("Metrix_Samples", "jdf", "S2328.jdf");
        var document = MetrixJdfParser.Parse(path);

        Assert.Equal("S2328", document.Root.JobId);
        Assert.NotNull(document.Layout);
        Assert.True(document.Layout!.Signatures.Count > 0);
        Assert.Equal(2, document.RunLists.Count);

        var docRunList = document.FindRunListById(document.GetRunListRef("Document"));
        Assert.Equal("520", docRunList?.NPage);
    }

    [Fact]
    public void ParseMxml_S2326_LoadsProjectAndLayouts()
    {
        var path = ResolveSamplePath("Metrix_Samples", "mxml", "S2326.mxml");
        var document = MetrixMxmlParser.Parse(path);

        Assert.Equal("S2326", document.Project.ProjectId);
        Assert.True(document.Project.Products.Count > 0);
        Assert.True(document.Project.Layouts.Count > 0);
        Assert.True(document.ResourcePool.FoldingSchemes.Count > 0);
    }

    [Fact]
    public void Transform_UsesFirstSheetWorkStyle_WhenAvailable()
    {
        var jdfPath = ResolveSamplePath("Metrix_Samples", "jdf", "S2328.jdf");
        var mxmlPath = ResolveSamplePath("Metrix_Samples", "mxml", "S2328.mxml");
        var jdf = MetrixJdfParser.Parse(jdfPath);
        var mxml = MetrixMxmlParser.Parse(mxmlPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, mxml, new MetrixToSignaOptions());

        Assert.Equal("SS", options.WorkStyle);
    }

    [Fact]
    public void Transform_UsesSurfaceDimensions_WhenConfigured()
    {
        var jdfPath = ResolveSamplePath("Metrix_Samples", "jdf", "S2326.jdf");
        var jdf = MetrixJdfParser.Parse(jdfPath);

        var transformer = new MetrixToSignaTransformer();
        var options = transformer.BuildGeneratorOptions(jdf, null, new MetrixToSignaOptions
        {
            UseSurfaceDimensions = true
        });

        Assert.InRange(options.PlateWidth, 2919.3m, 2919.4m);
        Assert.InRange(options.PlateHeight, 2238.7m, 2238.8m);
    }

    private static string ResolveSamplePath(params string[] parts)
    {
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

        throw new DirectoryNotFoundException($"Unable to locate sample path starting from {baseDir}.");
    }
}
