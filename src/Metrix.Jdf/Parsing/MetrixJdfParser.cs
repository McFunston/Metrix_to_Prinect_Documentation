using System.Xml.Linq;

namespace Metrix.Jdf;

public static class MetrixJdfParser
{
    // Parses the Metrix imposition JDF into a minimal in-memory model used for normalization.
    public static MetrixJdfDocument Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        // Preserve whitespace so CTM/box strings remain byte-for-byte as exported.
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidDataException("Missing JDF root element.");
        var ns = root.Name.Namespace;
        var hdm = root.GetNamespaceOfPrefix("HDM") ?? XNamespace.Get("www.heidelberg.com/schema/HDM");
        var ssi = root.GetNamespaceOfPrefix("SSi") ?? XNamespace.Get("http://www.creo.com/SSI/JDFExtensions.xsd");

        var rootNode = new MetrixJdfNode
        {
            Type = Attr(root, "Type"),
            JobId = Attr(root, "JobID"),
            JobPartId = Attr(root, "JobPartID"),
            Version = Attr(root, "Version"),
            MaxVersion = Attr(root, "MaxVersion"),
            DescriptiveName = Attr(root, "DescriptiveName"),
            Id = Attr(root, "ID"),
            Status = Attr(root, "Status")
        };

        MetrixLayout? layout = null;
        var runLists = new List<MetrixRunListResource>();

        // Metrix imposition JDFs tend to keep layout and RunList resources only.
        var resourcePool = root.Element(ns + "ResourcePool");
        if (resourcePool is not null)
        {
            var layoutElement = resourcePool.Element(ns + "Layout");
            if (layoutElement is not null)
            {
                layout = ParseLayout(layoutElement, ns, ssi);
            }

            foreach (var runList in resourcePool.Elements(ns + "RunList"))
            {
                runLists.Add(ParseRunList(runList, ns, hdm));
            }
        }

        var resourceLinks = ParseResourceLinks(root.Element(ns + "ResourceLinkPool"));

        return new MetrixJdfDocument(
            path,
            rootNode,
            ns,
            hdm,
            ssi,
            document,
            layout,
            runLists,
            resourceLinks);
    }

    private static MetrixLayout ParseLayout(XElement layoutElement, XNamespace ns, XNamespace ssi)
    {
        // Metrix layout uses Signature → Sheet → Surface rather than nested Layout partitions.
        var layout = new MetrixLayout
        {
            Id = Attr(layoutElement, "ID"),
            Name = Attr(layoutElement, "Name"),
            DescriptiveName = Attr(layoutElement, "DescriptiveName"),
            Status = Attr(layoutElement, "Status")
        };

        foreach (var signatureElement in layoutElement.Elements(ns + "Signature"))
        {
            var signature = new MetrixSignature
            {
                Name = Attr(signatureElement, "Name")
            };

            foreach (var sheetElement in signatureElement.Elements(ns + "Sheet"))
            {
                var sheet = new MetrixSheet
                {
                    Name = Attr(sheetElement, "Name"),
                    WorkStyle = sheetElement.Attribute(ssi + "WorkStyle")?.Value,
                    SurfaceContentsBox = Attr(sheetElement, "SurfaceContentsBox")
                };

                foreach (var surfaceElement in sheetElement.Elements(ns + "Surface"))
                {
                    var surface = new MetrixSurface
                    {
                        Side = Attr(surfaceElement, "Side"),
                        Dimension = surfaceElement.Attribute(ssi + "Dimension")?.Value,
                        MediaOrigin = surfaceElement.Attribute(ssi + "MediaOrigin")?.Value,
                        SurfaceContentsBox = Attr(surfaceElement, "SurfaceContentsBox")
                    };

                    foreach (var markObject in surfaceElement.Elements(ns + "MarkObject"))
                    {
                        surface.MarkObjects.Add(new MetrixMarkObject
                        {
                            Ord = Attr(markObject, "Ord"),
                            Ctm = Attr(markObject, "CTM"),
                            ClipBox = Attr(markObject, "ClipBox")
                        });
                    }

                    foreach (var contentObject in surfaceElement.Elements(ns + "ContentObject"))
                    {
                        surface.ContentObjects.Add(new MetrixContentObject
                        {
                            // Preserve raw CTM/TrimCTM strings for downstream matrix calculations.
                            Ord = Attr(contentObject, "Ord"),
                            Ctm = Attr(contentObject, "CTM"),
                            TrimCtm = Attr(contentObject, "TrimCTM"),
                            TrimSize = Attr(contentObject, "TrimSize"),
                            TrimBox1 = contentObject.Attribute(ssi + "TrimBox1")?.Value,
                            ClipBox = Attr(contentObject, "ClipBox"),
                            Comp = contentObject.Attribute(ssi + "Comp")?.Value
                        });
                    }

                    sheet.Surfaces.Add(surface);
                }

                signature.Sheets.Add(sheet);
            }

            layout.Signatures.Add(signature);
        }

        return layout;
    }

    private static MetrixRunListResource ParseRunList(XElement runListElement, XNamespace ns, XNamespace hdm)
    {
        // RunList entries include marks and (optionally) document/page list references.
        var resource = new MetrixRunListResource
        {
            Id = Attr(runListElement, "ID"),
            PartIdKeys = Attr(runListElement, "PartIDKeys"),
            DescriptiveName = Attr(runListElement, "DescriptiveName"),
            Status = Attr(runListElement, "Status"),
            NPage = Attr(runListElement, "NPage")
        };

        foreach (var runList in runListElement.Elements(ns + "RunList"))
        {
            var entry = new MetrixRunListEntry
            {
                Pages = Attr(runList, "Pages"),
                NPage = Attr(runList, "NPage"),
                Run = Attr(runList, "Run"),
                Status = Attr(runList, "Status")
            };

            var layoutElement = runList.Element(ns + "LayoutElement");
            if (layoutElement is not null)
            {
                entry.IsBlank = string.Equals(Attr(layoutElement, "IsBlank"), "true", StringComparison.OrdinalIgnoreCase);
                var fileSpec = layoutElement.Element(ns + "FileSpec");
                if (fileSpec is not null)
                {
                    entry.FileSpecUrl = Attr(fileSpec, "URL");
                }

                foreach (var separation in layoutElement.Elements(ns + "SeparationSpec"))
                {
                    entry.SeparationSpecs.Add(new MetrixSeparationSpec
                    {
                        Name = Attr(separation, "Name"),
                        HdmType = separation.Attribute(hdm + "Type")?.Value,
                        HdmSubType = separation.Attribute(hdm + "SubType")?.Value,
                        HdmIsMapRel = separation.Attribute(hdm + "IsMapRel")?.Value
                    });
                }
            }

            resource.Entries.Add(entry);
        }

        var pageList = runListElement.Element(ns + "PageList");
        if (pageList is not null)
        {
            // Page list labels drive Cockpit page assignments after normalization.
            foreach (var pageData in pageList.Elements(ns + "PageData"))
            {
                resource.PageList.Add(new MetrixPageData
                {
                    PageIndex = Attr(pageData, "PageIndex"),
                    DescriptiveName = Attr(pageData, "DescriptiveName")
                });
            }
        }

        return resource;
    }

    private static List<MetrixResourceLink> ParseResourceLinks(XElement? resourceLinkPool)
    {
        if (resourceLinkPool is null)
        {
            return new List<MetrixResourceLink>();
        }

        return resourceLinkPool.Elements()
            .Select(element => new MetrixResourceLink
            {
                LinkType = element.Name.LocalName,
                Usage = Attr(element, "Usage"),
                ProcessUsage = Attr(element, "ProcessUsage"),
                RefId = Attr(element, "rRef")
            })
            .ToList();
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }
}
