using System.Xml.Linq;

namespace Signa.Jdf;

public sealed class JdfDocument
{
    public JdfDocument(
        string sourcePath,
        JdfNode root,
        XNamespace jdfNamespace,
        XNamespace hdmNamespace,
        XDocument xmlDocument,
        LayoutPart? layout,
        SignaJobInfo? signaJob,
        IReadOnlyList<RunListResource> runLists,
        IReadOnlyList<ResourceLink> resourceLinks,
        IReadOnlyList<ConventionalPrintingParamsPart> printingParams,
        IReadOnlyList<StrippingParamsPart> strippingParams,
        IReadOnlyList<MediaResource> media,
        IReadOnlyList<BinderySignatureInfo> binderySignatures,
        IReadOnlyList<ComponentInfo> components)
    {
        SourcePath = sourcePath;
        Root = root;
        JdfNamespace = jdfNamespace;
        HdmNamespace = hdmNamespace;
        XmlDocument = xmlDocument;
        Layout = layout;
        SignaJob = signaJob;
        RunLists = runLists;
        ResourceLinks = resourceLinks;
        PrintingParams = printingParams;
        StrippingParams = strippingParams;
        Media = media;
        BinderySignatures = binderySignatures;
        Components = components;
    }

    public string SourcePath { get; }
    public JdfNode Root { get; }
    public XNamespace JdfNamespace { get; }
    public XNamespace HdmNamespace { get; }
    public XDocument XmlDocument { get; }
    public LayoutPart? Layout { get; }
    public SignaJobInfo? SignaJob { get; }
    public IReadOnlyList<RunListResource> RunLists { get; }
    public IReadOnlyList<ResourceLink> ResourceLinks { get; }
    public IReadOnlyList<ConventionalPrintingParamsPart> PrintingParams { get; }
    public IReadOnlyList<StrippingParamsPart> StrippingParams { get; }
    public IReadOnlyList<MediaResource> Media { get; }
    public IReadOnlyList<BinderySignatureInfo> BinderySignatures { get; }
    public IReadOnlyList<ComponentInfo> Components { get; }

    public IEnumerable<string> Types =>
        (Root.Types ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public string? GetRunListRef(string processUsage)
    {
        return ResourceLinks
            .FirstOrDefault(link =>
                string.Equals(link.LinkType, "RunListLink", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(link.ProcessUsage, processUsage, StringComparison.OrdinalIgnoreCase))
            ?.RefId;
    }

    public RunListResource? FindRunListById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return RunLists.FirstOrDefault(list =>
            string.Equals(list.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
