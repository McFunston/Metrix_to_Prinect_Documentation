using System.Xml.Linq;

namespace Metrix.Jdf;

public sealed class MetrixJdfDocument
{
    // Parsed JDF wrapper used by the transformer; preserves namespaces and raw XML.
    public MetrixJdfDocument(
        string sourcePath,
        MetrixJdfNode root,
        XNamespace jdfNamespace,
        XNamespace hdmNamespace,
        XNamespace ssiNamespace,
        XDocument xmlDocument,
        MetrixLayout? layout,
        IReadOnlyList<MetrixRunListResource> runLists,
        IReadOnlyList<MetrixResourceLink> resourceLinks)
    {
        SourcePath = sourcePath;
        Root = root;
        JdfNamespace = jdfNamespace;
        HdmNamespace = hdmNamespace;
        SsiNamespace = ssiNamespace;
        XmlDocument = xmlDocument;
        Layout = layout;
        RunLists = runLists;
        ResourceLinks = resourceLinks;
    }

    public string SourcePath { get; }
    public MetrixJdfNode Root { get; }
    public XNamespace JdfNamespace { get; }
    public XNamespace HdmNamespace { get; }
    public XNamespace SsiNamespace { get; }
    public XDocument XmlDocument { get; }
    public MetrixLayout? Layout { get; }
    public IReadOnlyList<MetrixRunListResource> RunLists { get; }
    public IReadOnlyList<MetrixResourceLink> ResourceLinks { get; }

    public string? GetRunListRef(string processUsage)
    {
        return ResourceLinks
            .FirstOrDefault(link =>
                string.Equals(link.LinkType, "RunListLink", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(link.ProcessUsage, processUsage, StringComparison.OrdinalIgnoreCase))
            ?.RefId;
    }

    public MetrixRunListResource? FindRunListById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return RunLists.FirstOrDefault(list =>
            string.Equals(list.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
