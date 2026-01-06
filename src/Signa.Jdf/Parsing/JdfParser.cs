using System.Xml.Linq;

namespace Signa.Jdf;

public static class JdfParser
{
    public static JdfDocument Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidDataException("Missing JDF root element.");
        var ns = root.Name.Namespace;
        var hdm = root.GetNamespaceOfPrefix("HDM") ?? XNamespace.Get("www.heidelberg.com/schema/HDM");

        var rootNode = new JdfNode
        {
            Type = Attr(root, "Type"),
            Types = Attr(root, "Types"),
            JobId = Attr(root, "JobID"),
            JobPartId = Attr(root, "JobPartID"),
            Version = Attr(root, "Version"),
            MaxVersion = Attr(root, "MaxVersion"),
            IcsVersions = Attr(root, "ICSVersions"),
            DescriptiveName = Attr(root, "DescriptiveName"),
            Id = Attr(root, "ID"),
            Status = Attr(root, "Status")
        };

        LayoutPart? layout = null;
        SignaJobInfo? signaJob = null;
        var runLists = new List<RunListResource>();
        var printingParams = new List<ConventionalPrintingParamsPart>();
        var strippingParams = new List<StrippingParamsPart>();
        var media = new List<MediaResource>();
        var binderySignatures = new List<BinderySignatureInfo>();
        var components = new List<ComponentInfo>();

        var resourcePool = root.Element(ns + "ResourcePool");
        if (resourcePool is not null)
        {
            var layoutElement = resourcePool.Element(ns + "Layout");
            if (layoutElement is not null)
            {
            layout = ParseLayout(layoutElement, ns, hdm);
            signaJob = ParseSignaJob(layoutElement, hdm);
            }

            foreach (var runList in resourcePool.Elements(ns + "RunList"))
            {
                runLists.Add(ParseRunList(runList, ns));
            }

            foreach (var printing in resourcePool.Elements(ns + "ConventionalPrintingParams"))
            {
                ParsePrintingParams(printing, ns, printingParams);
            }

            foreach (var stripping in resourcePool.Elements(ns + "StrippingParams"))
            {
                ParseStrippingParams(stripping, ns, strippingParams);
            }

            foreach (var mediaElement in resourcePool.Elements(ns + "Media"))
            {
                media.Add(ParseMedia(mediaElement));
            }

            foreach (var binderySignature in resourcePool.Elements(ns + "BinderySignature"))
            {
                binderySignatures.Add(ParseBinderySignature(binderySignature, ns, hdm));
            }

            foreach (var component in resourcePool.Elements(ns + "Component"))
            {
                components.Add(ParseComponent(component, ns, hdm));
            }
        }

        var resourceLinks = ParseResourceLinks(root.Element(ns + "ResourceLinkPool"));

        return new JdfDocument(
            path,
            rootNode,
            ns,
            hdm,
            document,
            layout,
            signaJob,
            runLists,
            resourceLinks,
            printingParams,
            strippingParams,
            media,
            binderySignatures,
            components);
    }

    private static LayoutPart ParseLayout(XElement layoutElement, XNamespace ns, XNamespace hdm)
    {
        var part = new LayoutPart
        {
            SignatureName = Attr(layoutElement, "SignatureName"),
            SheetName = Attr(layoutElement, "SheetName"),
            Side = Attr(layoutElement, "Side"),
            Name = Attr(layoutElement, "Name"),
            DescriptiveName = Attr(layoutElement, "DescriptiveName"),
            SourceWorkStyle = Attr(layoutElement, "SourceWorkStyle"),
            SurfaceContentsBox = Attr(layoutElement, "SurfaceContentsBox"),
            PaperRect = Attr(layoutElement, "PaperRect"),
            PartIdKeys = Attr(layoutElement, "PartIDKeys"),
            MarkObjectCount = layoutElement.Elements(ns + "MarkObject").Count(),
            ContentObjectCount = layoutElement.Elements(ns + "ContentObject").Count()
        };

        foreach (var contentObject in layoutElement.Elements(ns + "ContentObject"))
        {
            part.ContentObjects.Add(new ContentObjectInfo
            {
                SignatureName = part.SignatureName,
                SheetName = part.SheetName,
                Side = part.Side,
                Ord = Attr(contentObject, "Ord"),
                DescriptiveName = Attr(contentObject, "DescriptiveName"),
                AssemblyFrontBack = contentObject.Attribute(hdm + "AssemblyFB")?.Value,
                JobPart = contentObject.Attribute(hdm + "JobPart")?.Value,
                RunlistIndex = contentObject.Attribute(hdm + "RunlistIndex")?.Value
            });
        }

        foreach (var child in layoutElement.Elements(ns + "Layout"))
        {
            part.Children.Add(ParseLayout(child, ns, hdm));
        }

        return part;
    }

    private static ComponentInfo ParseComponent(XElement element, XNamespace ns, XNamespace hdm)
    {
        var info = new ComponentInfo
        {
            Id = Attr(element, "ID"),
            Class = Attr(element, "Class"),
            ComponentType = Attr(element, "ComponentType"),
            PartIdKeys = Attr(element, "PartIDKeys"),
            ProductTypeDetails = Attr(element, "ProductTypeDetails"),
            Status = Attr(element, "Status"),
            AssemblyIds = Attr(element, "AssemblyIDs"),
            Dimensions = Attr(element, "Dimensions"),
            ProductType = Attr(element, "ProductType"),
            SignatureName = Attr(element, "SignatureName"),
            SheetName = Attr(element, "SheetName"),
            BlockName = Attr(element, "BlockName"),
            Side = Attr(element, "Side"),
            HdmClosedFoldingSheetDimensions = element.Attribute(hdm + "ClosedFoldingSheetDimensions")?.Value,
            HdmOpenedFoldingSheetDimensions = element.Attribute(hdm + "OpenedFoldingSheetDimensions")?.Value,
            HdmIsCover = element.Attribute(hdm + "IsCover")?.Value
        };

        foreach (var child in element.Elements(ns + "Component"))
        {
            info.Children.Add(ParseComponent(child, ns, hdm));
        }

        return info;
    }

    private static SignaJobInfo? ParseSignaJob(XElement layoutElement, XNamespace hdm)
    {
        var signaJob = layoutElement.Element(hdm + "SignaJob");
        if (signaJob is null)
        {
            return null;
        }

        var parts = signaJob.Elements(hdm + "SignaJobPart")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SignaJobInfo
        {
            JobParts = parts
        };
    }

    private static RunListResource ParseRunList(XElement runListElement, XNamespace ns)
    {
        var resource = new RunListResource
        {
            Id = Attr(runListElement, "ID"),
            PartIdKeys = Attr(runListElement, "PartIDKeys"),
            DescriptiveName = Attr(runListElement, "DescriptiveName"),
            Status = Attr(runListElement, "Status"),
            LogicalPage = Attr(runListElement, "LogicalPage"),
            Pages = Attr(runListElement, "Pages"),
            NPage = Attr(runListElement, "NPage"),
            FileSpecUrl = GetFileSpecUrl(runListElement, ns),
            FileSpecMimeType = GetFileSpecMimeType(runListElement, ns),
            LayoutElementType = GetLayoutElementType(runListElement, ns),
            HdmOfw = Attr(runListElement, "OFW")
        };

        foreach (var spec in GetSeparationSpecs(runListElement, ns))
        {
            resource.SeparationSpecs.Add(spec);
        }

        foreach (var part in runListElement.Elements(ns + "RunList"))
        {
            resource.Parts.Add(ParseRunListPart(part, ns));
        }

        return resource;
    }

    private static RunListPart ParseRunListPart(XElement element, XNamespace ns)
    {
        var part = new RunListPart
        {
            SignatureName = Attr(element, "SignatureName"),
            SheetName = Attr(element, "SheetName"),
            Side = Attr(element, "Side"),
            LogicalPage = Attr(element, "LogicalPage"),
            Pages = Attr(element, "Pages"),
            Run = Attr(element, "Run"),
            NPage = Attr(element, "NPage"),
            FileSpecUrl = GetFileSpecUrl(element, ns),
            FileSpecMimeType = GetFileSpecMimeType(element, ns),
            LayoutElementType = GetLayoutElementType(element, ns)
        };

        foreach (var spec in GetSeparationSpecs(element, ns))
        {
            part.SeparationSpecs.Add(spec);
        }

        foreach (var child in element.Elements(ns + "RunList"))
        {
            part.Children.Add(ParseRunListPart(child, ns));
        }

        return part;
    }

    private static IEnumerable<SeparationSpecInfo> GetSeparationSpecs(XElement element, XNamespace ns)
    {
        var layoutElement = element.Element(ns + "LayoutElement");
        if (layoutElement is null)
        {
            yield break;
        }

        foreach (var spec in layoutElement.Elements(ns + "SeparationSpec"))
        {
            yield return new SeparationSpecInfo
            {
                Name = Attr(spec, "Name"),
                IsMapRel = Attr(spec, "IsMapRel"),
                Type = Attr(spec, "Type"),
                SubType = Attr(spec, "SubType")
            };
        }
    }

    private static string? GetFileSpecMimeType(XElement element, XNamespace ns)
    {
        var layoutElement = element.Element(ns + "LayoutElement");
        return layoutElement?.Element(ns + "FileSpec")?.Attribute("MimeType")?.Value;
    }

    private static string? GetLayoutElementType(XElement element, XNamespace ns)
    {
        return element.Element(ns + "LayoutElement")?.Attribute("ElementType")?.Value;
    }

    private static BinderySignatureInfo ParseBinderySignature(XElement element, XNamespace ns, XNamespace hdm)
    {
        var signatureCell = element.Element(ns + "SignatureCell");

        return new BinderySignatureInfo
        {
            Id = Attr(element, "ID"),
            SignatureName = Attr(element, "SignatureName"),
            SheetName = Attr(element, "SheetName"),
            BinderySignatureName = Attr(element, "BinderySignatureName"),
            BinderySignatureType = Attr(element, "BinderySignatureType"),
            FoldCatalog = Attr(element, "FoldCatalog"),
            FrontSchemePageOrientation = signatureCell?.Attribute(hdm + "FrontSchemePageOrientation")?.Value,
            BackSchemePageOrientation = signatureCell?.Attribute(hdm + "BackSchemePageOrientation")?.Value
        };
    }

    private static string? GetFileSpecUrl(XElement element, XNamespace ns)
    {
        return element
            .Element(ns + "LayoutElement")
            ?.Element(ns + "FileSpec")
            ?.Attribute("URL")
            ?.Value;
    }

    private static void ParsePrintingParams(
        XElement element,
        XNamespace ns,
        List<ConventionalPrintingParamsPart> parts)
    {
        parts.Add(new ConventionalPrintingParamsPart
        {
            WorkStyle = Attr(element, "WorkStyle"),
            SignatureName = Attr(element, "SignatureName"),
            SheetName = Attr(element, "SheetName"),
            Side = Attr(element, "Side")
        });

        foreach (var child in element.Elements(ns + "ConventionalPrintingParams"))
        {
            ParsePrintingParams(child, ns, parts);
        }
    }

    private static void ParseStrippingParams(
        XElement element,
        XNamespace ns,
        List<StrippingParamsPart> parts)
    {
        parts.Add(new StrippingParamsPart
        {
            WorkStyle = Attr(element, "WorkStyle"),
            SignatureName = Attr(element, "SignatureName"),
            SheetName = Attr(element, "SheetName"),
            BinderySignatureName = Attr(element, "BinderySignatureName"),
            AssemblyIds = Attr(element, "AssemblyIDs"),
            SectionList = Attr(element, "SectionList")
        });

        foreach (var child in element.Elements(ns + "StrippingParams"))
        {
            ParseStrippingParams(child, ns, parts);
        }
    }

    private static MediaResource ParseMedia(XElement element)
    {
        var media = new MediaResource
        {
            Id = Attr(element, "ID"),
            MediaType = Attr(element, "MediaType"),
            Dimension = Attr(element, "Dimension"),
            Thickness = Attr(element, "Thickness"),
            Weight = Attr(element, "Weight"),
            PartIdKeys = Attr(element, "PartIDKeys"),
            HdmLeadingEdge = Attr(element, "LeadingEdge")
        };

        foreach (var part in element.Elements())
        {
            if (!string.Equals(part.Name.LocalName, "Media", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var signatureName = Attr(part, "SignatureName");
            var partDimension = Attr(part, "Dimension");
            var sheetName = part.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "Media", StringComparison.OrdinalIgnoreCase))
                ?.Attribute("SheetName")
                ?.Value;

            media.Parts.Add(new MediaPart
            {
                SignatureName = signatureName,
                SheetName = sheetName,
                Dimension = partDimension
            });
        }

        return media;
    }

    private static List<ResourceLink> ParseResourceLinks(XElement? resourceLinkPool)
    {
        var links = new List<ResourceLink>();
        if (resourceLinkPool is null)
        {
            return links;
        }

        foreach (var link in resourceLinkPool.Elements())
        {
            links.Add(new ResourceLink
            {
                LinkType = link.Name.LocalName,
                ProcessUsage = Attr(link, "ProcessUsage"),
                CombinedProcessIndex = Attr(link, "CombinedProcessIndex"),
                Usage = Attr(link, "Usage"),
                RefId = Attr(link, "rRef")
            });
        }

        return links;
    }

    private static string? Attr(XElement element, string localName)
    {
        return element.Attributes()
            .FirstOrDefault(attr => string.Equals(attr.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }
}
