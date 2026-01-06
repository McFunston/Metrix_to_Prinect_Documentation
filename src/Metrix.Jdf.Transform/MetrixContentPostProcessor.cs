using System;
using System.Globalization;
using System.Xml.Linq;

namespace Metrix.Jdf.Transform;

public static class MetrixContentPostProcessor
{
    public static void ApplyContentPlacement(XDocument signaDocument, MetrixJdfDocument metrixDocument, MetrixMxmlDocument? metrixMxml)
    {
        if (signaDocument.Root is null)
        {
            throw new InvalidOperationException("Missing JDF root element.");
        }

        var ns = signaDocument.Root.Name.Namespace;
        var hdm = signaDocument.Root.GetNamespaceOfPrefix("HDM") ?? XNamespace.Get("www.heidelberg.com/schema/HDM");

        var resourcePool = signaDocument.Root.Element(ns + "ResourcePool");
        var resourceLinkPool = signaDocument.Root.Element(ns + "ResourceLinkPool");
        var layoutRoot = resourcePool?.Element(ns + "Layout");
        var paperMedia = resourcePool?.Elements(ns + "Media")
            .FirstOrDefault(element => string.Equals(Attr(element, "MediaType"), "Paper", StringComparison.OrdinalIgnoreCase));
        var plateMedia = resourcePool?.Elements(ns + "Media")
            .FirstOrDefault(element => string.Equals(Attr(element, "MediaType"), "Plate", StringComparison.OrdinalIgnoreCase));
        var transferCurvePoolId = resourcePool?.Elements(ns + "TransferCurvePool")
            .Select(element => Attr(element, "ID"))
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        var paperMediaId = paperMedia?.Attribute("ID")?.Value;
        var plateMediaId = plateMedia?.Attribute("ID")?.Value;
        var metrixLayout = metrixDocument.Layout;
        if (layoutRoot is null || resourcePool is null || metrixLayout is null)
        {
            return;
        }

        var useMetrixLayout = false;
        if (useMetrixLayout)
        {
            layoutRoot = ReplaceLayoutWithMetrixLayout(resourcePool, layoutRoot, metrixDocument, metrixLayout);
        }

        var normalizeTransferCurvePool = false;
        var normalizeTransferCurvePoolBySignature = true;
        var paperDimensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plateDimensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paperOffsets = new Dictionary<(string Signature, string Sheet), (decimal X, decimal Y)>();
        var paperPositions = new Dictionary<(string Signature, string Sheet), SheetPosition>();
        var ssi = metrixDocument.SsiNamespace;

        foreach (var signature in metrixLayout.Signatures)
        {
            var signatureNode = useMetrixLayout
                ? layoutRoot.Elements(ns + "Signature")
                    .FirstOrDefault(element => IsMatch(element, "SignatureName", signature.Name) || IsMatch(element, "Name", signature.Name))
                : layoutRoot.Elements(ns + "Layout")
                    .FirstOrDefault(element => IsMatch(element, "SignatureName", signature.Name));
            if (signatureNode is null)
            {
                continue;
            }

            EnsureMediaRef(signatureNode, ns, paperMediaId);
            EnsureMediaRef(signatureNode, ns, plateMediaId);
            if (normalizeTransferCurvePoolBySignature)
            {
                EnsureTransferCurvePoolRef(signatureNode, ns, transferCurvePoolId);
            }

            foreach (var sheet in signature.Sheets)
            {
                var sheetNode = useMetrixLayout
                    ? signatureNode.Elements(ns + "Sheet")
                        .FirstOrDefault(element => IsMatch(element, "SheetName", sheet.Name) || IsMatch(element, "Name", sheet.Name))
                    : signatureNode.Elements(ns + "Layout")
                        .FirstOrDefault(element => IsMatch(element, "SheetName", sheet.Name));
                if (sheetNode is null)
                {
                    continue;
                }

                var firstSurface = sheet.Surfaces.FirstOrDefault();
                var plateSize = ResolvePlateSize(sheet, firstSurface);
                if (plateSize is not null)
                {
                    sheetNode.SetAttributeValue(
                        "SurfaceContentsBox",
                        FormatBox(plateSize.Value.Width, plateSize.Value.Height));
                    var plateDim = SetMediaDimension(plateMedia, ns, signature.Name, sheet.Name, plateSize.Value.Width, plateSize.Value.Height);
                    if (!string.IsNullOrWhiteSpace(plateDim))
                    {
                        plateDimensions.Add(plateDim);
                    }
                }

                var paperSize = ResolvePaperSize(firstSurface);
                if (paperSize is not null)
                {
                    var paperOffset = ResolvePaperOffset(firstSurface, plateSize, paperSize.Value);
                    paperOffsets[(signature.Name ?? string.Empty, sheet.Name ?? string.Empty)] = paperOffset;
                    if (plateSize is not null)
                    {
                        paperPositions[(signature.Name ?? string.Empty, sheet.Name ?? string.Empty)] = new SheetPosition(
                            paperOffset.X,
                            paperOffset.Y,
                            paperSize.Value.Width,
                            paperSize.Value.Height,
                            plateSize.Value.Width,
                            plateSize.Value.Height);
                    }
                    sheetNode.SetAttributeValue(
                        hdm + "PaperRect",
                        FormatPaperRect(paperOffset.X, paperOffset.Y, paperSize.Value.Width, paperSize.Value.Height));
                    var surfaceElements = useMetrixLayout
                        ? sheetNode.Elements(ns + "Surface")
                        : sheetNode.Elements(ns + "Layout");
                    foreach (var surfaceElement in surfaceElements)
                    {
                        var paperRect = FormatPaperRect(paperOffset.X, paperOffset.Y, paperSize.Value.Width, paperSize.Value.Height);
                        surfaceElement.SetAttributeValue(hdm + "PaperRect", paperRect);
                    }

                    var paperDim = SetMediaDimension(paperMedia, ns, signature.Name, sheet.Name, paperSize.Value.Width, paperSize.Value.Height);
                    if (!string.IsNullOrWhiteSpace(paperDim))
                    {
                        paperDimensions.Add(paperDim);
                    }
                }

                if (IsSimplex(sheet.WorkStyle))
                {
                    var sideElements = useMetrixLayout
                        ? sheetNode.Elements(ns + "Surface")
                        : sheetNode.Elements(ns + "Layout");
                    foreach (var backSide in sideElements
                                 .Where(element => string.Equals(Attr(element, "Side"), "Back", StringComparison.OrdinalIgnoreCase))
                                 .ToList())
                    {
                        backSide.Remove();
                    }
                }

            var applyContentGeometry = true;
            var applyMarkGeometry = true;
                if (!useMetrixLayout && (applyContentGeometry || applyMarkGeometry))
                {
                    foreach (var surface in sheet.Surfaces)
                    {
                        if (IsSimplex(sheet.WorkStyle) &&
                            string.Equals(surface.Side, "Back", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var sideNode = sheetNode.Elements(ns + "Layout")
                            .FirstOrDefault(element => Attr(element, "Side") == surface.Side);
                        if (sideNode is null)
                        {
                            continue;
                        }

                        sideNode.Elements(ns + "MarkObject").Remove();
                        sideNode.Elements(ns + "ContentObject").Remove();

                        if (applyMarkGeometry && applyContentGeometry)
                        {
                            // Preserve Metrix ordering (mark, content, mark) to avoid preview crashes.
                            var markIndex = 0;
                            if (surface.MarkObjects.Count > 0)
                            {
                                sideNode.Add(BuildMarkObject(ns, surface.MarkObjects[0]));
                                markIndex = 1;
                            }

                            var originOffset = (X: 0m, Y: 0m);
                            foreach (var content in surface.ContentObjects)
                            {
                                sideNode.Add(BuildContentObject(ns, hdm, ssi, surface.Side, content, originOffset, includeAssemblyFace: true));
                            }

                            for (var i = markIndex; i < surface.MarkObjects.Count; i++)
                            {
                                sideNode.Add(BuildMarkObject(ns, surface.MarkObjects[i]));
                            }
                        }
                        else
                        {
                            if (applyMarkGeometry)
                            {
                                foreach (var mark in surface.MarkObjects)
                                {
                                    sideNode.Add(BuildMarkObject(ns, mark));
                                }
                            }

                            if (applyContentGeometry)
                            {
                                var originOffset = (X: 0m, Y: 0m);
                                foreach (var content in surface.ContentObjects)
                                {
                                    sideNode.Add(BuildContentObject(ns, hdm, ssi, surface.Side, content, originOffset, includeAssemblyFace: true));
                                }
                            }
                        }
                    }
                }
            }
        }

        var normalizeMediaPartitions = false;
        if (normalizeMediaPartitions)
        {
            NormalizeMediaPartitions(paperMedia, ns);
            NormalizeMediaPartitions(plateMedia, ns);
            ClearTopLevelDimensionIfMixed(paperMedia, paperDimensions);
            ClearTopLevelDimensionIfMixed(plateMedia, plateDimensions);
        }

        if (normalizeTransferCurvePool)
        {
            NormalizeTransferCurvePool(resourcePool, ns, paperOffsets);
        }
        else if (normalizeTransferCurvePoolBySignature)
        {
            NormalizeTransferCurvePoolBySignature(resourcePool, ns, paperOffsets);
        }
        var applyPaperMetadata = metrixMxml is not null;
        if (applyPaperMetadata)
        {
            ApplyPaperMetadata(metrixMxml, paperMedia, ns, metrixLayout);
        }
        else
        {
            StripPaperMetadata(paperMedia, ns);
        }
        NormalizeMarksRunList(signaDocument, metrixDocument, metrixLayout, hdm);
        RemoveCuttingAndStripping(resourcePool, resourceLinkPool, ns);
        // AlignResourcesToPythonStyle(signaDocument);
        // Use the main layout until we confirm a preview layout is required.
        NormalizeRootToImposition(signaDocument, metrixDocument);

        ApplyLabels(signaDocument, metrixDocument, metrixMxml);
    }

    private static XElement BuildContentObject(XNamespace ns, XNamespace hdm, XNamespace ssi, string? side, MetrixContentObject content, (decimal X, decimal Y) originOffset, bool includeAssemblyFace)
    {
        var element = new XElement(ns + "ContentObject");

        if (!string.IsNullOrWhiteSpace(content.Ctm))
        {
            element.SetAttributeValue("CTM", ShiftTransform(content.Ctm, originOffset));
        }
        if (!string.IsNullOrWhiteSpace(content.TrimCtm))
        {
            element.SetAttributeValue("TrimCTM", ShiftTransform(content.TrimCtm, originOffset));
        }
        if (!string.IsNullOrWhiteSpace(content.TrimSize))
        {
            element.SetAttributeValue("TrimSize", content.TrimSize);
        }
        if (!string.IsNullOrWhiteSpace(content.ClipBox))
        {
            element.SetAttributeValue("ClipBox", ShiftBox(content.ClipBox, originOffset));
        }
        if (!string.IsNullOrWhiteSpace(content.TrimBox1))
        {
            element.SetAttributeValue(ssi + "TrimBox1", content.TrimBox1);
        }
        if (!string.IsNullOrWhiteSpace(content.Comp))
        {
            element.SetAttributeValue(ssi + "Comp", content.Comp);
        }

        if (!string.IsNullOrWhiteSpace(content.Ord))
        {
            element.SetAttributeValue("Ord", content.Ord);
            element.SetAttributeValue("DescriptiveName", content.Ord);
        }

        if (includeAssemblyFace && !string.IsNullOrWhiteSpace(side))
        {
            element.SetAttributeValue(hdm + "AssemblyFB", side);
        }

        var finalBox = !string.IsNullOrWhiteSpace(content.TrimBox1)
            ? content.TrimBox1
            : content.ClipBox;
        if (!string.IsNullOrWhiteSpace(finalBox))
        {
            element.SetAttributeValue(hdm + "FinalPageBox", ShiftBox(finalBox, originOffset));
        }

        var orientation = ResolveOrientation(content.Ctm);
        element.SetAttributeValue(hdm + "PageOrientation", orientation);

        return element;
    }

    private static XElement ReplaceLayoutWithMetrixLayout(
        XElement resourcePool,
        XElement existingLayout,
        MetrixJdfDocument metrixDocument,
        MetrixLayout metrixLayout)
    {
        var ns = existingLayout.Name.Namespace;
        var hdm = existingLayout.GetNamespaceOfPrefix("HDM") ?? XNamespace.Get("www.heidelberg.com/schema/HDM");
        var ssi = metrixDocument.SsiNamespace;

        var rebuiltLayout = new XElement(existingLayout.Name, existingLayout.Attributes());

        foreach (var signature in metrixLayout.Signatures)
        {
            if (string.IsNullOrWhiteSpace(signature.Name))
            {
                continue;
            }

            var signatureElement = new XElement(ns + "Signature",
                new XAttribute("Name", signature.Name),
                new XAttribute("SignatureName", signature.Name));

            foreach (var sheet in signature.Sheets)
            {
                if (string.IsNullOrWhiteSpace(sheet.Name))
                {
                    continue;
                }

                var sheetElement = new XElement(ns + "Sheet",
                    new XAttribute("Name", sheet.Name),
                    new XAttribute("SheetName", sheet.Name));

                if (!string.IsNullOrWhiteSpace(sheet.WorkStyle))
                {
                    sheetElement.SetAttributeValue(ssi + "WorkStyle", sheet.WorkStyle);
                }

                var sheetSurfaceBox = sheet.SurfaceContentsBox ?? sheet.Surfaces.FirstOrDefault()?.SurfaceContentsBox;
                if (!string.IsNullOrWhiteSpace(sheetSurfaceBox))
                {
                    sheetElement.SetAttributeValue("SurfaceContentsBox", sheetSurfaceBox);
                }

                var firstSurface = sheet.Surfaces.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstSurface?.MediaOrigin))
                {
                    sheetElement.SetAttributeValue(ssi + "MediaOrigin", firstSurface.MediaOrigin);
                }

                foreach (var surface in sheet.Surfaces)
                {
                    if (IsSimplex(sheet.WorkStyle) &&
                        string.Equals(surface.Side, "Back", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var surfaceElement = new XElement(ns + "Surface",
                        new XAttribute("Side", surface.Side ?? "Front"),
                        new XAttribute("Status", "Available"));

                    if (!string.IsNullOrWhiteSpace(surface.Dimension))
                    {
                        surfaceElement.SetAttributeValue(ssi + "Dimension", surface.Dimension);
                    }

                    if (!string.IsNullOrWhiteSpace(surface.MediaOrigin))
                    {
                        surfaceElement.SetAttributeValue(ssi + "MediaOrigin", surface.MediaOrigin);
                    }

                    if (!string.IsNullOrWhiteSpace(surface.SurfaceContentsBox))
                    {
                        surfaceElement.SetAttributeValue("SurfaceContentsBox", surface.SurfaceContentsBox);
                    }

                    foreach (var mark in surface.MarkObjects)
                    {
                        surfaceElement.Add(BuildMarkObject(ns, mark));
                    }

                    var originOffset = (X: 0m, Y: 0m);
                    foreach (var content in surface.ContentObjects)
                    {
                        surfaceElement.Add(BuildContentObject(ns, hdm, ssi, surface.Side, content, originOffset, includeAssemblyFace: false));
                    }

                    sheetElement.Add(surfaceElement);
                }

                signatureElement.Add(sheetElement);
            }

            rebuiltLayout.Add(signatureElement);
        }

        existingLayout.ReplaceWith(rebuiltLayout);
        return rebuiltLayout;
    }

    private static bool IsMatch(XElement element, string attributeName, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        return string.Equals(Attr(element, attributeName), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static XElement BuildMarkObject(XNamespace ns, MetrixMarkObject mark)
    {
        var element = new XElement(ns + "MarkObject");

        if (!string.IsNullOrWhiteSpace(mark.Ord))
        {
            element.SetAttributeValue("Ord", mark.Ord);
        }

        if (!string.IsNullOrWhiteSpace(mark.Ctm))
        {
            element.SetAttributeValue("CTM", mark.Ctm);
        }

        if (!string.IsNullOrWhiteSpace(mark.ClipBox))
        {
            element.SetAttributeValue("ClipBox", mark.ClipBox);
        }

        return element;
    }

    private static string ResolveOrientation(string? ctm)
    {
        if (string.IsNullOrWhiteSpace(ctm))
        {
            return "0";
        }

        var parts = ctm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return "0";
        }

        if (!decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var a) ||
            !decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var b) ||
            !decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var c) ||
            !decimal.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return "0";
        }

        if (a == 1 && b == 0 && c == 0 && d == 1)
        {
            return "0";
        }
        if (a == 0 && b == 1 && c == -1 && d == 0)
        {
            return "90";
        }
        if (a == -1 && b == 0 && c == 0 && d == -1)
        {
            return "180";
        }
        if (a == 0 && b == -1 && c == 1 && d == 0)
        {
            return "270";
        }

        return "0";
    }

    private static (decimal Width, decimal Height)? ResolvePlateSize(MetrixSheet sheet, MetrixSurface? surface)
    {
        if (TryParseBox(sheet.SurfaceContentsBox, out var sheetWidth, out var sheetHeight))
        {
            return (sheetWidth, sheetHeight);
        }

        if (surface is not null && TryParseBox(surface.SurfaceContentsBox, out var surfaceWidth, out var surfaceHeight))
        {
            return (surfaceWidth, surfaceHeight);
        }

        return null;
    }

    private static (decimal Width, decimal Height)? ResolvePaperSize(MetrixSurface? surface)
    {
        if (surface is null)
        {
            return null;
        }

        return TryParseDimension(surface.Dimension, out var width, out var height)
            ? (width, height)
            : null;
    }

    private static (decimal X, decimal Y) ResolvePaperOffset(
        MetrixSurface? surface,
        (decimal Width, decimal Height)? plateSize,
        (decimal Width, decimal Height) paperSize)
    {
        if (surface is not null && TryParseDimension(surface.MediaOrigin, out var originX, out var originY))
        {
            return (originX, originY);
        }

        if (plateSize is not null)
        {
            var offsetX = Math.Max(0, (plateSize.Value.Width - paperSize.Width) / 2);
            var offsetY = Math.Max(0, (plateSize.Value.Height - paperSize.Height) / 2);
            return (offsetX, offsetY);
        }

        return (0, 0);
    }

    private static string FormatBox(decimal width, decimal height)
    {
        return string.Join(' ', new[]
        {
            "0",
            "0",
            width.ToString(CultureInfo.InvariantCulture),
            height.ToString(CultureInfo.InvariantCulture)
        });
    }

    private static string FormatPaperRect(decimal offsetX, decimal offsetY, decimal width, decimal height)
    {
        var right = offsetX + width;
        var top = offsetY + height;
        return string.Join(' ', new[]
        {
            offsetX.ToString(CultureInfo.InvariantCulture),
            offsetY.ToString(CultureInfo.InvariantCulture),
            right.ToString(CultureInfo.InvariantCulture),
            top.ToString(CultureInfo.InvariantCulture)
        });
    }

    private static string? SetMediaDimension(
        XElement? media,
        XNamespace ns,
        string? signatureName,
        string? sheetName,
        decimal width,
        decimal height)
    {
        if (media is null || string.IsNullOrWhiteSpace(signatureName) || string.IsNullOrWhiteSpace(sheetName))
        {
            return null;
        }

        var part = media.Elements(ns + "Media")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "SignatureName"), signatureName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Attr(element, "SheetName"), sheetName, StringComparison.OrdinalIgnoreCase));

        if (part is null)
        {
            return null;
        }

        var value = $"{width.ToString(CultureInfo.InvariantCulture)} {height.ToString(CultureInfo.InvariantCulture)}";
        part.SetAttributeValue("Dimension", value);
        return value;
    }

    private static void EnsureMediaRef(XElement signatureNode, XNamespace ns, string? mediaId)
    {
        if (string.IsNullOrWhiteSpace(mediaId))
        {
            return;
        }

        var exists = signatureNode.Elements(ns + "MediaRef")
            .Any(element => string.Equals(Attr(element, "rRef"), mediaId, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            signatureNode.Add(new XElement(ns + "MediaRef", new XAttribute("rRef", mediaId)));
        }
    }

    private static void EnsureTransferCurvePoolRef(XElement layoutNode, XNamespace ns, string? transferCurvePoolId)
    {
        if (string.IsNullOrWhiteSpace(transferCurvePoolId))
        {
            return;
        }

        var exists = layoutNode.Elements(ns + "TransferCurvePoolRef")
            .Any(element => string.Equals(Attr(element, "rRef"), transferCurvePoolId, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            layoutNode.Add(new XElement(ns + "TransferCurvePoolRef", new XAttribute("rRef", transferCurvePoolId)));
        }
    }

    private static void NormalizeMediaPartitions(XElement? media, XNamespace ns)
    {
        if (media is null)
        {
            return;
        }

        var directParts = media.Elements(ns + "Media").ToList();
        var leafParts = directParts
            .Where(part => !string.IsNullOrWhiteSpace(Attr(part, "SignatureName"))
                           && !string.IsNullOrWhiteSpace(Attr(part, "SheetName")))
            .ToList();

        if (leafParts.Count == 0)
        {
            return;
        }

        var signatureParts = directParts
            .Where(part => !string.IsNullOrWhiteSpace(Attr(part, "SignatureName"))
                           && string.IsNullOrWhiteSpace(Attr(part, "SheetName")))
            .ToDictionary(part => Attr(part, "SignatureName")!, StringComparer.OrdinalIgnoreCase);

        foreach (var leaf in leafParts)
        {
            var signatureName = Attr(leaf, "SignatureName");
            if (string.IsNullOrWhiteSpace(signatureName))
            {
                continue;
            }

            if (!signatureParts.TryGetValue(signatureName, out var signaturePart))
            {
                signaturePart = new XElement(ns + "Media", new XAttribute("SignatureName", signatureName));
                media.Add(signaturePart);
                signatureParts[signatureName] = signaturePart;
            }

            leaf.Remove();
            leaf.SetAttributeValue("SignatureName", null);
            signaturePart.Add(leaf);
        }
    }

    private static void ClearTopLevelDimensionIfMixed(XElement? media, HashSet<string> dimensions)
    {
        if (media is null || dimensions.Count <= 1)
        {
            return;
        }

        media.SetAttributeValue("Dimension", null);
    }

    private static void NormalizeTransferCurvePool(
        XElement? resourcePool,
        XNamespace ns,
        Dictionary<(string Signature, string Sheet), (decimal X, decimal Y)> offsets)
    {
        if (resourcePool is null || offsets.Count == 0)
        {
            return;
        }

        var transferPool = resourcePool.Elements(ns + "TransferCurvePool").FirstOrDefault();
        if (transferPool is null)
        {
            return;
        }

        var normalized = new XElement(ns + "TransferCurvePool");
        foreach (var attribute in transferPool.Attributes())
        {
            normalized.SetAttributeValue(attribute.Name, attribute.Value);
        }

        var signatureParts = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in offsets.OrderBy(item => item.Key.Signature).ThenBy(item => item.Key.Sheet))
        {
            if (!signatureParts.TryGetValue(entry.Key.Signature, out var sigPart))
            {
                sigPart = new XElement(ns + "TransferCurvePool",
                    new XAttribute("SignatureName", entry.Key.Signature));
                signatureParts[entry.Key.Signature] = sigPart;
                normalized.Add(sigPart);
            }

            var sheetPart = new XElement(ns + "TransferCurvePool",
                new XAttribute("SheetName", entry.Key.Sheet));

            sheetPart.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Paper"),
                new XAttribute("CTM", $"1 0 0 1 {(-entry.Value.X).ToString(CultureInfo.InvariantCulture)} {(-entry.Value.Y).ToString(CultureInfo.InvariantCulture)}")));
            sheetPart.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Plate"),
                new XAttribute("CTM", "1 0 0 1 0 0")));

            sigPart.Add(sheetPart);
        }

        transferPool.ReplaceWith(normalized);
    }

    private static void NormalizeTransferCurvePoolBySignature(
        XElement? resourcePool,
        XNamespace ns,
        Dictionary<(string Signature, string Sheet), (decimal X, decimal Y)> offsets)
    {
        if (resourcePool is null || offsets.Count == 0)
        {
            return;
        }

        var transferPool = resourcePool.Elements(ns + "TransferCurvePool").FirstOrDefault();
        if (transferPool is null)
        {
            return;
        }

        var normalized = new XElement(ns + "TransferCurvePool");
        foreach (var attribute in transferPool.Attributes())
        {
            normalized.SetAttributeValue(attribute.Name, attribute.Value);
        }

        normalized.SetAttributeValue("PartIDKeys", "SignatureName");

        foreach (var signatureGroup in offsets
                     .GroupBy(entry => entry.Key.Signature, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key))
        {
            var firstOffset = signatureGroup
                .OrderBy(entry => entry.Key.Sheet)
                .Select(entry => entry.Value)
                .FirstOrDefault();

            var sigPart = new XElement(ns + "TransferCurvePool",
                new XAttribute("SignatureName", signatureGroup.Key));

            sigPart.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Paper"),
                new XAttribute("CTM", $"1 0 0 1 {(-firstOffset.X).ToString(CultureInfo.InvariantCulture)} {(-firstOffset.Y).ToString(CultureInfo.InvariantCulture)}")));
            sigPart.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Plate"),
                new XAttribute("CTM", "1 0 0 1 0 0")));

            normalized.Add(sigPart);
        }

        transferPool.ReplaceWith(normalized);
    }

    private static void ApplyPaperMetadata(
        MetrixMxmlDocument? metrixMxml,
        XElement? paperMedia,
        XNamespace ns,
        MetrixLayout metrixLayout)
    {
        if (metrixMxml is null || paperMedia is null)
        {
            return;
        }

        var stockSequence = BuildStockSequence(metrixMxml);
        if (stockSequence.Count == 0)
        {
            return;
        }

        var sheetPairs = metrixLayout.Signatures
            .SelectMany(signature => signature.Sheets.Select(sheet =>
                (Signature: signature.Name ?? string.Empty, Sheet: sheet.Name ?? string.Empty)))
            .ToList();

        var count = Math.Min(stockSequence.Count, sheetPairs.Count);
        for (var index = 0; index < count; index++)
        {
            var pair = sheetPairs[index];
            if (string.IsNullOrWhiteSpace(pair.Signature) || string.IsNullOrWhiteSpace(pair.Sheet))
            {
                continue;
            }

            var leaf = FindPaperMediaLeaf(paperMedia, ns, pair.Signature, pair.Sheet);
            if (leaf is null)
            {
                continue;
            }

            ApplyStockAttributes(leaf, stockSequence[index]);
        }

        ApplyUniformPaperAttributes(paperMedia, ns);
    }

    private static void NormalizeMarksRunList(
        XDocument signaDocument,
        MetrixJdfDocument metrixDocument,
        MetrixLayout metrixLayout,
        XNamespace hdm)
    {
        if (signaDocument.Root is null)
        {
            return;
        }

        var marksPagesPerSide = ResolveMarksPagesPerSide(metrixDocument, metrixLayout);
        if (marksPagesPerSide <= 0)
        {
            return;
        }

        var ns = signaDocument.Root.Name.Namespace;
        var marksRunList = signaDocument.Root
            .Element(ns + "ResourcePool")
            ?.Elements(ns + "RunList")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "ID"), "r_marks", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Attr(element, "ID"), "RUN_0001", StringComparison.OrdinalIgnoreCase));
        if (marksRunList is null)
        {
            return;
        }

        var layoutElementTemplate = marksRunList
            .Descendants(ns + "LayoutElement")
            .FirstOrDefault(element => element.Elements(ns + "SeparationSpec").Any())
            ?? marksRunList.Element(ns + "LayoutElement");

        var flattenMarksRunList = false;
        if (flattenMarksRunList)
        {
            foreach (var child in marksRunList.Elements(ns + "RunList").ToList())
            {
                child.Remove();
            }

            marksRunList.SetAttributeValue("PartIDKeys", "Run");
            marksRunList.SetAttributeValue("LogicalPage", "0");
            marksRunList.SetAttributeValue("NPage", ResolveMarksTotalPages(metrixDocument).ToString(CultureInfo.InvariantCulture));
            marksRunList.SetAttributeValue("Pages", BuildPageRange(ResolveMarksTotalPages(metrixDocument)));

            if (layoutElementTemplate is not null && marksRunList.Element(ns + "LayoutElement") is null)
            {
                marksRunList.Add(new XElement(layoutElementTemplate));
            }

            ApplyMarksSeparations(metrixDocument, marksRunList, ns, hdm, layoutElementTemplate);
            return;
        }

        var layoutElementCopy = layoutElementTemplate is null ? null : new XElement(layoutElementTemplate);
        marksRunList.SetAttributeValue("PartIDKeys", "SignatureName SheetName Side");
        marksRunList.Elements().Remove();

        var logicalPage = 0;
        var totalPages = ResolveMarksTotalPages(metrixDocument);
        if (totalPages > 0)
        {
            var baseRunList = new XElement(ns + "RunList",
                new XAttribute("Pages", BuildPageRange(totalPages)),
                new XAttribute("Run", "0"),
                new XAttribute("Status", "Available"));
            if (layoutElementCopy is not null)
            {
                baseRunList.Add(new XElement(layoutElementCopy));
            }
            marksRunList.Add(baseRunList);
        }

        foreach (var signature in metrixLayout.Signatures)
        {
            if (string.IsNullOrWhiteSpace(signature.Name))
            {
                continue;
            }

            var sigPart = new XElement(ns + "RunList", new XAttribute("SignatureName", signature.Name));

            foreach (var sheet in signature.Sheets)
            {
                if (string.IsNullOrWhiteSpace(sheet.Name))
                {
                    continue;
                }

                var sheetPart = new XElement(ns + "RunList", new XAttribute("SheetName", sheet.Name));

                var frontPart = new XElement(ns + "RunList", new XAttribute("Side", "Front"));

                SetRunListPages(frontPart, logicalPage, marksPagesPerSide, layoutElementTemplate);
                logicalPage += marksPagesPerSide;
                sheetPart.Add(frontPart);

                if (IsSimplex(sheet.WorkStyle))
                {
                }
                else
                {
                    var backPart = new XElement(ns + "RunList", new XAttribute("Side", "Back"));

                    SetRunListPages(backPart, logicalPage, marksPagesPerSide, layoutElementTemplate);
                    logicalPage += marksPagesPerSide;
                    sheetPart.Add(backPart);
                }

                sigPart.Add(sheetPart);
            }

            marksRunList.Add(sigPart);
        }

        marksRunList.SetAttributeValue("LogicalPage", "0");
        marksRunList.SetAttributeValue("NPage", logicalPage.ToString(CultureInfo.InvariantCulture));
        marksRunList.SetAttributeValue("Pages", BuildPageRange(logicalPage));

        ApplyMarksSeparations(metrixDocument, marksRunList, ns, hdm, layoutElementTemplate);
    }

    private static int ResolveMarksPagesPerSide(MetrixJdfDocument metrixDocument, MetrixLayout metrixLayout)
    {
        var marksRef = metrixDocument.GetRunListRef("Marks");
        var marksRunList = metrixDocument.FindRunListById(marksRef);
        if (marksRunList is null)
        {
            return 2;
        }

        var totalPages = marksRunList.Entries
            .Select(entry => ParsePageCount(entry.Pages))
            .FirstOrDefault(count => count > 0);
        if (totalPages <= 0)
        {
            return 2;
        }

        var totalSides = metrixLayout.Signatures
            .SelectMany(signature => signature.Sheets)
            .Sum(sheet => IsSimplex(sheet.WorkStyle) ? 1 : 2);
        if (totalSides <= 0)
        {
            return 2;
        }

        var perSide = totalPages / totalSides;
        return perSide > 0 ? perSide : 2;
    }

    private static int ResolveMarksTotalPages(MetrixJdfDocument metrixDocument)
    {
        var marksRef = metrixDocument.GetRunListRef("Marks");
        var marksRunList = metrixDocument.FindRunListById(marksRef);
        if (marksRunList is null)
        {
            return 0;
        }

        foreach (var entry in marksRunList.Entries)
        {
            var count = ParsePageCount(entry.Pages);
            if (count > 0)
            {
                return count;
            }
        }

        if (int.TryParse(marksRunList.NPage, out var npage))
        {
            return npage;
        }

        return 0;
    }

    private static int ParsePageCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out var single))
        {
            return single + 1;
        }

        if (parts.Length >= 3 &&
            int.TryParse(parts[0], out var start) &&
            int.TryParse(parts[2], out var end))
        {
            return Math.Abs(end - start) + 1;
        }

        return 0;
    }

    private static void SetRunListPages(XElement runList, int logicalPage, int count, XElement? layoutTemplate)
    {
        runList.SetAttributeValue("LogicalPage", logicalPage.ToString(CultureInfo.InvariantCulture));
        runList.SetAttributeValue("Pages", BuildPageRangeWithOffset(logicalPage, count));
        runList.SetAttributeValue("NPage", count.ToString(CultureInfo.InvariantCulture));
        if (runList.Element(runList.Name.Namespace + "LayoutElement") is null && layoutTemplate is not null)
        {
            runList.Add(new XElement(layoutTemplate));
        }
    }

    private static string BuildPageRange(int totalPages)
    {
        if (totalPages <= 0)
        {
            return "0";
        }

        if (totalPages == 1)
        {
            return "0";
        }

        return $"0 ~ {totalPages - 1}";
    }

    private static string BuildPageRangeWithOffset(int start, int count)
    {
        if (count <= 1)
        {
            return start.ToString(CultureInfo.InvariantCulture);
        }

        var end = start + count - 1;
        return $"{start.ToString(CultureInfo.InvariantCulture)} ~ {end.ToString(CultureInfo.InvariantCulture)}";
    }

    private static void ApplyMarksSeparations(
        MetrixJdfDocument metrixDocument,
        XElement marksRunList,
        XNamespace ns,
        XNamespace hdm,
        XElement? layoutTemplate)
    {
        var specs = new List<SeparationSpecInfo>();
        if (layoutTemplate is not null)
        {
            specs.AddRange(layoutTemplate.Elements(ns + "SeparationSpec")
                .Select(spec => new SeparationSpecInfo(
                    spec.Attribute("Name")?.Value,
                    spec.Attribute(hdm + "Type")?.Value,
                    spec.Attribute(hdm + "SubType")?.Value,
                    spec.Attribute(hdm + "IsMapRel")?.Value))
                .Where(spec => !string.IsNullOrWhiteSpace(spec.Name)));
        }

        var marksRef = metrixDocument.GetRunListRef("Marks");
        var marksSource = metrixDocument.FindRunListById(marksRef);
        if (marksSource is not null)
        {
            specs.AddRange(marksSource.Entries
                .SelectMany(entry => entry.SeparationSpecs)
                .Where(spec => !string.IsNullOrWhiteSpace(spec.Name))
                .Select(spec => new SeparationSpecInfo(
                    spec.Name,
                    spec.HdmType,
                    spec.HdmSubType,
                    spec.HdmIsMapRel)));
        }

        if (specs.Count == 0)
        {
            return;
        }

        var distinctSpecs = specs
            .Where(spec => !string.IsNullOrWhiteSpace(spec.Name))
            .GroupBy(spec => spec.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var reduceMarksSeparations = true;
        if (reduceMarksSeparations)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "B",
                "C",
                "M",
                "Y",
                "ProofColor"
            };

            distinctSpecs = distinctSpecs
                .Where(spec => allowed.Contains(spec.Name!))
                .ToList();
        }

        foreach (var layoutElement in marksRunList.Descendants(ns + "LayoutElement"))
        {
            if (reduceMarksSeparations)
            {
                foreach (var existing in layoutElement.Elements(ns + "SeparationSpec").ToList())
                {
                    var name = existing.Attribute("Name")?.Value;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!distinctSpecs.Any(spec => string.Equals(spec.Name, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        existing.Remove();
                    }
                }
            }

            foreach (var spec in distinctSpecs)
            {
                var exists = layoutElement.Elements(ns + "SeparationSpec")
                    .Any(element => string.Equals(element.Attribute("Name")?.Value, spec.Name, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    continue;
                }

                var sep = new XElement(ns + "SeparationSpec",
                    new XAttribute("Name", spec.Name!));
                if (!string.IsNullOrWhiteSpace(spec.HdmType))
                {
                    sep.SetAttributeValue(hdm + "Type", spec.HdmType);
                }
                if (!string.IsNullOrWhiteSpace(spec.HdmSubType))
                {
                    sep.SetAttributeValue(hdm + "SubType", spec.HdmSubType);
                }
                if (!string.IsNullOrWhiteSpace(spec.HdmIsMapRel))
                {
                    sep.SetAttributeValue(hdm + "IsMapRel", spec.HdmIsMapRel);
                }

                layoutElement.Add(sep);
            }
        }
    }

    private static void AddPreviewLayout(
        XDocument signaDocument,
        MetrixJdfDocument metrixDocument,
        MetrixLayout metrixLayout)
    {
        if (signaDocument.Root is null)
        {
            return;
        }

        var ns = signaDocument.Root.Name.Namespace;
        var resourcePool = signaDocument.Root.Element(ns + "ResourcePool");
        if (resourcePool is null)
        {
            return;
        }

        var layout = resourcePool.Elements(ns + "Layout")
            .FirstOrDefault(element => string.Equals(Attr(element, "ID"), "r_layout", StringComparison.OrdinalIgnoreCase));
        if (layout is null)
        {
            return;
        }

        var previewId = "r_layout_preview";
        var existingPreview = resourcePool.Elements(ns + "Layout")
            .FirstOrDefault(element => string.Equals(Attr(element, "ID"), previewId, StringComparison.OrdinalIgnoreCase));
        existingPreview?.Remove();

        var previewLayout = new XElement(ns + "Layout",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", previewId),
            new XAttribute("Status", "Available"),
            new XAttribute("DescriptiveName", "Layout Preview"));

        var ssi = metrixDocument.SsiNamespace;

        foreach (var signature in metrixLayout.Signatures)
        {
            if (string.IsNullOrWhiteSpace(signature.Name))
            {
                continue;
            }

            var sigElement = new XElement(ns + "Signature",
                new XAttribute("Name", signature.Name));

            foreach (var sheet in signature.Sheets)
            {
                if (string.IsNullOrWhiteSpace(sheet.Name))
                {
                    continue;
                }

                var sheetElement = new XElement(ns + "Sheet",
                    new XAttribute("Name", sheet.Name));

                if (!string.IsNullOrWhiteSpace(sheet.WorkStyle))
                {
                    sheetElement.SetAttributeValue(ssi + "WorkStyle", sheet.WorkStyle);
                }

                var sheetSurfaceBox = sheet.SurfaceContentsBox ?? sheet.Surfaces.FirstOrDefault()?.SurfaceContentsBox;
                if (!string.IsNullOrWhiteSpace(sheetSurfaceBox))
                {
                    sheetElement.SetAttributeValue("SurfaceContentsBox", sheetSurfaceBox);
                }

                foreach (var surface in sheet.Surfaces)
                {
                    if (IsSimplex(sheet.WorkStyle) &&
                        string.Equals(surface.Side, "Back", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var surfaceElement = new XElement(ns + "Surface",
                        new XAttribute("Side", surface.Side ?? "Front"),
                        new XAttribute("Status", "Available"));

                    if (!string.IsNullOrWhiteSpace(surface.Dimension))
                    {
                        surfaceElement.SetAttributeValue(ssi + "Dimension", surface.Dimension);
                    }

                    if (!string.IsNullOrWhiteSpace(surface.MediaOrigin))
                    {
                        surfaceElement.SetAttributeValue(ssi + "MediaOrigin", surface.MediaOrigin);
                    }

                    if (!string.IsNullOrWhiteSpace(surface.SurfaceContentsBox))
                    {
                        surfaceElement.SetAttributeValue("SurfaceContentsBox", surface.SurfaceContentsBox);
                    }

                    foreach (var mark in surface.MarkObjects)
                    {
                        // Preview layout omits mark geometry for crash isolation.
                    }

                    foreach (var content in surface.ContentObjects)
                    {
                        // Preview layout omits content geometry for crash isolation.
                    }

                    sheetElement.Add(surfaceElement);
                }

                sigElement.Add(sheetElement);
            }

            previewLayout.Add(sigElement);
        }

        resourcePool.Add(previewLayout);

        var linkPool = signaDocument.Root.Element(ns + "ResourceLinkPool");
        var layoutLink = linkPool?.Elements(ns + "LayoutLink").FirstOrDefault();
        if (layoutLink is not null)
        {
            layoutLink.SetAttributeValue("rRef", previewId);
        }
    }

    private static void NormalizeRootToImposition(XDocument signaDocument, MetrixJdfDocument metrixDocument)
    {
        var root = signaDocument.Root;
        if (root is null)
        {
            return;
        }

        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        root.SetAttributeValue("Type", "Imposition");
        root.SetAttributeValue(xsi + "type", "Imposition");
        root.SetAttributeValue("Version", "1.2");
        root.SetAttributeValue("MaxVersion", "1.2");

        if (string.IsNullOrWhiteSpace(root.Attribute("ID")?.Value))
        {
            root.SetAttributeValue("ID", "JDF_0000");
        }

        if (string.IsNullOrWhiteSpace(root.Attribute("Activation")?.Value))
        {
            root.SetAttributeValue("Activation", "Active");
        }

        root.SetAttributeValue("Types", null);
    }

    private static void RemoveCuttingAndStripping(XElement? resourcePool, XElement? resourceLinkPool, XNamespace ns)
    {
        if (resourcePool is null)
        {
            return;
        }

        foreach (var element in resourcePool.Elements()
                     .Where(item => item.Name == ns + "CuttingParams" || item.Name == ns + "StrippingParams")
                     .ToList())
        {
            element.Remove();
        }

        if (resourceLinkPool is null)
        {
            return;
        }

        foreach (var link in resourceLinkPool.Elements()
                     .Where(item => item.Name == ns + "CuttingParamsLink" || item.Name == ns + "StrippingParamsLink")
                     .ToList())
        {
            link.Remove();
        }
    }

    private static void AlignResourcesToPythonStyle(XDocument signaDocument)
    {
        if (signaDocument.Root is null)
        {
            return;
        }

        var ns = signaDocument.Root.Name.Namespace;
        var resourcePool = signaDocument.Root.Element(ns + "ResourcePool");
        if (resourcePool is null)
        {
            return;
        }

        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["r_layout"] = "LAY_0000",
            ["r_doc"] = "RUN_0000",
            ["r_marks"] = "RUN_0001",
            ["r_print"] = "r_ConvPrint_001",
            ["r_media_paper"] = "r_Paper_Metrix",
            ["r_media_plate"] = "r_Plate_Metrix",
            ["r_tcp"] = "r_TransferCTM"
        };

        var removeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "r_pagepool",
            "r_output"
        };

        foreach (var element in resourcePool.Elements().ToList())
        {
            var id = Attr(element, "ID");
            if (!string.IsNullOrWhiteSpace(id) && removeIds.Contains(id))
            {
                element.Remove();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(id) && idMap.TryGetValue(id, out var replacement))
            {
                element.SetAttributeValue("ID", replacement);
            }
        }

        foreach (var element in signaDocument.Root.DescendantsAndSelf())
        {
            var rRef = element.Attribute("rRef");
            if (rRef is not null && idMap.TryGetValue(rRef.Value, out var replacement))
            {
                rRef.Value = replacement;
            }
        }

        EnsureColorantControl(resourcePool, ns);
        NormalizeConventionalPrintingParams(resourcePool, ns);
        NormalizeDocumentRunList(resourcePool, ns);

        var linkPool = new XElement(ns + "ResourceLinkPool",
            new XElement(ns + "LayoutLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "LAY_0000")),
            new XElement(ns + "RunListLink",
                new XAttribute("ProcessUsage", "Document"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "RUN_0000"),
                new XAttribute("CombinedProcessIndex", "0")),
            new XElement(ns + "RunListLink",
                new XAttribute("ProcessUsage", "Marks"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "RUN_0001"),
                new XAttribute("CombinedProcessIndex", "0")),
            new XElement(ns + "ConventionalPrintingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("rRef", "r_ConvPrint_001")),
            new XElement(ns + "MediaLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("CombinedProcessIndex", "1 2"),
                new XAttribute("rRef", "r_Paper_Metrix")),
            new XElement(ns + "MediaLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("CombinedProcessIndex", "1 2"),
                new XAttribute("rRef", "r_Plate_Metrix")),
            new XElement(ns + "ColorantControlLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("rRef", "r_Colorants")),
            new XElement(ns + "CuttingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_CutDummy")),
            new XElement(ns + "TransferCurvePoolLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_TransferCTM")),
            new XElement(ns + "StrippingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_StripPos")));

        var existingLinkPool = signaDocument.Root.Element(ns + "ResourceLinkPool");
        existingLinkPool?.ReplaceWith(linkPool);
        if (existingLinkPool is null)
        {
            signaDocument.Root.Add(linkPool);
        }
    }

    private static void EnsureColorantControl(XElement resourcePool, XNamespace ns)
    {
        var existing = resourcePool.Elements(ns + "ColorantControl")
            .FirstOrDefault(element => string.Equals(Attr(element, "ID"), "r_Colorants", StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.SetAttributeValue("Class", "Parameter");
            existing.SetAttributeValue("Status", "Available");
            return;
        }

        var colorantControl = new XElement(ns + "ColorantControl",
            new XAttribute("ID", "r_Colorants"),
            new XAttribute("Class", "Parameter"),
            new XAttribute("Status", "Available"));
        resourcePool.Add(colorantControl);
    }

    private static void NormalizeConventionalPrintingParams(XElement resourcePool, XNamespace ns)
    {
        var printingParams = resourcePool.Elements(ns + "ConventionalPrintingParams").FirstOrDefault();
        if (printingParams is null)
        {
            return;
        }

        printingParams.SetAttributeValue("ID", "r_ConvPrint_001");
        printingParams.SetAttributeValue("Class", "Parameter");
        printingParams.SetAttributeValue("Status", "Available");
        printingParams.SetAttributeValue("PrintingType", "SheetFed");
        printingParams.SetAttributeValue("PartIDKeys", "SignatureName SheetName Side");
    }

    private static void NormalizeDocumentRunList(XElement resourcePool, XNamespace ns)
    {
        var docRunList = resourcePool.Elements(ns + "RunList")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "ID"), "r_doc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Attr(element, "ID"), "RUN_0000", StringComparison.OrdinalIgnoreCase));
        if (docRunList is null)
        {
            return;
        }

        var layoutElementTemplate = docRunList
            .Descendants(ns + "LayoutElement")
            .FirstOrDefault()
            ?? docRunList.Element(ns + "LayoutElement");

        foreach (var layoutElement in docRunList.Elements(ns + "LayoutElement").ToList())
        {
            layoutElement.Remove();
        }

        var totalPages = ParsePageCount(Attr(docRunList, "Pages"));
        if (totalPages <= 0 && int.TryParse(Attr(docRunList, "NPage"), out var npage))
        {
            totalPages = npage;
        }

        docRunList.Elements(ns + "RunList").Remove();
        docRunList.SetAttributeValue("PartIDKeys", "Run");
        docRunList.SetAttributeValue("LogicalPage", "0");

        if (totalPages > 0)
        {
            docRunList.SetAttributeValue("NPage", totalPages.ToString(CultureInfo.InvariantCulture));
            docRunList.SetAttributeValue("Pages", BuildPageRange(totalPages));
        }

        var runPart = new XElement(ns + "RunList",
            new XAttribute("Run", "0"),
            new XAttribute("Status", "Available"));
        if (totalPages > 0)
        {
            runPart.SetAttributeValue("Pages", BuildPageRange(totalPages));
        }

        if (layoutElementTemplate is not null)
        {
            runPart.Add(new XElement(layoutElementTemplate));
        }

        docRunList.Add(runPart);
    }

    private static void EnsureCuttingParams(
        XElement? resourcePool,
        XElement? resourceLinkPool,
        XNamespace ns,
        XNamespace hdm,
        Dictionary<(string Signature, string Sheet), SheetPosition> positions)
    {
        if (resourcePool is null || resourceLinkPool is null || positions.Count == 0)
        {
            return;
        }

        const string cuttingId = "r_CutDummy";
        var cuttingParams = resourcePool.Elements(ns + "CuttingParams")
            .FirstOrDefault(element => string.Equals(Attr(element, "ID"), cuttingId, StringComparison.OrdinalIgnoreCase))
            ?? new XElement(ns + "CuttingParams", new XAttribute("ID", cuttingId));

        cuttingParams.SetAttributeValue("Class", "Parameter");
        cuttingParams.SetAttributeValue("Status", "Available");
        cuttingParams.SetAttributeValue("PartIDKeys", "SignatureName SheetName");
        cuttingParams.Elements().Remove();

        foreach (var entry in positions.OrderBy(item => item.Key.Signature).ThenBy(item => item.Key.Sheet))
        {
            var sigPart = new XElement(ns + "CuttingParams",
                new XAttribute("SignatureName", entry.Key.Signature));
            var sheetPart = new XElement(ns + "CuttingParams",
                new XAttribute("SheetName", entry.Key.Sheet));

            var block = new XElement(ns + "CutBlock",
                new XAttribute("Class", "Parameter"),
                new XAttribute("BlockElementType", "CutElement"),
                new XAttribute("BlockType", "CutBlock"),
                new XAttribute("BlockName", $"{entry.Key.Signature}_{entry.Key.Sheet}_B_1_1"),
                new XAttribute("BlockSize", $"{entry.Value.Width.ToString(CultureInfo.InvariantCulture)} {entry.Value.Height.ToString(CultureInfo.InvariantCulture)}"),
                new XAttribute("BlockTrf", "1 0 0 1 0 0"),
                new XAttribute(hdm + "CIP3BlockTrf", $"1 0 0 1 {entry.Value.X.ToString(CultureInfo.InvariantCulture)} {entry.Value.Y.ToString(CultureInfo.InvariantCulture)}"));

            sheetPart.Add(block);
            sigPart.Add(sheetPart);
            cuttingParams.Add(sigPart);
        }

        if (cuttingParams.Parent is null)
        {
            resourcePool.Add(cuttingParams);
        }

        var hasLink = resourceLinkPool.Elements(ns + "CuttingParamsLink")
            .Any(link => string.Equals(Attr(link, "rRef"), cuttingId, StringComparison.OrdinalIgnoreCase));
        if (!hasLink)
        {
            resourceLinkPool.Add(new XElement(ns + "CuttingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", cuttingId)));
        }
    }

    private static void EnsureStrippingParams(
        XElement? resourcePool,
        XElement? resourceLinkPool,
        XNamespace ns,
        MetrixLayout layout,
        Dictionary<(string Signature, string Sheet), SheetPosition> positions,
        string? paperMediaId,
        string? plateMediaId)
    {
        if (resourcePool is null || resourceLinkPool is null || positions.Count == 0)
        {
            return;
        }

        const string stripId = "r_StripPos";
        var strippingParams = resourcePool.Elements(ns + "StrippingParams")
            .FirstOrDefault(element => string.Equals(Attr(element, "ID"), stripId, StringComparison.OrdinalIgnoreCase))
            ?? new XElement(ns + "StrippingParams", new XAttribute("ID", stripId));

        strippingParams.SetAttributeValue("Class", "Parameter");
        strippingParams.SetAttributeValue("Status", "Available");
        strippingParams.SetAttributeValue("PartIDKeys", "SignatureName SheetName");
        strippingParams.SetAttributeValue("SectionList", "0");

        if (string.IsNullOrWhiteSpace(Attr(strippingParams, "WorkStyle")))
        {
            var workStyle = layout.Signatures.SelectMany(sig => sig.Sheets)
                .Select(sheet => MapWorkStyle(sheet.WorkStyle))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (!string.IsNullOrWhiteSpace(workStyle))
            {
                strippingParams.SetAttributeValue("WorkStyle", workStyle);
            }
        }

        EnsureMediaRef(strippingParams, ns, paperMediaId);
        EnsureMediaRef(strippingParams, ns, plateMediaId);

        foreach (var position in strippingParams.Elements(ns + "Position").ToList())
        {
            position.Remove();
        }

        var signatureParts = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in positions.OrderBy(item => item.Key.Signature).ThenBy(item => item.Key.Sheet))
        {
            if (!signatureParts.TryGetValue(entry.Key.Signature, out var sigPart))
            {
                sigPart = new XElement(ns + "StrippingParams", new XAttribute("SignatureName", entry.Key.Signature));
                signatureParts[entry.Key.Signature] = sigPart;
                strippingParams.Add(sigPart);
            }

            var sheetPart = new XElement(ns + "StrippingParams", new XAttribute("SheetName", entry.Key.Sheet));
            sigPart.Add(sheetPart);

            if (entry.Value.PlateWidth <= 0 || entry.Value.PlateHeight <= 0)
            {
                continue;
            }

            var left = entry.Value.X / entry.Value.PlateWidth;
            var bottom = entry.Value.Y / entry.Value.PlateHeight;
            var right = (entry.Value.X + entry.Value.Width) / entry.Value.PlateWidth;
            var top = (entry.Value.Y + entry.Value.Height) / entry.Value.PlateHeight;
            sigPart.Add(new XElement(ns + "Position",
                new XAttribute("RelativeBox", string.Join(' ', new[]
                {
                    left.ToString(CultureInfo.InvariantCulture),
                    bottom.ToString(CultureInfo.InvariantCulture),
                    right.ToString(CultureInfo.InvariantCulture),
                    top.ToString(CultureInfo.InvariantCulture)
                }))));
        }

        if (strippingParams.Parent is null)
        {
            resourcePool.Add(strippingParams);
        }

        var hasLink = resourceLinkPool.Elements(ns + "StrippingParamsLink")
            .Any(link => string.Equals(Attr(link, "rRef"), stripId, StringComparison.OrdinalIgnoreCase));
        if (!hasLink)
        {
            resourceLinkPool.Add(new XElement(ns + "StrippingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", stripId)));
        }
    }

    private static string? MapWorkStyle(string? ssiWorkStyle)
    {
        if (string.IsNullOrWhiteSpace(ssiWorkStyle))
        {
            return null;
        }

        return ssiWorkStyle switch
        {
            "PE" => "Perfecting",
            "TN" => "WorkAndTurn",
            "TO" => "WorkAndTumble",
            "SH" => "Sheetwise",
            "SF" => "Simplex",
            "SS" => "Simplex",
            "SW" => "Sheetwise",
            _ => ssiWorkStyle
        };
    }

    private static XElement? FindPaperMediaLeaf(XElement paperMedia, XNamespace ns, string signature, string sheet)
    {
        var direct = paperMedia
            .Descendants(ns + "Media")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "SignatureName"), signature, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Attr(element, "SheetName"), sheet, StringComparison.OrdinalIgnoreCase));
        if (direct is not null)
        {
            return direct;
        }

        var signaturePart = paperMedia
            .Descendants(ns + "Media")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "SignatureName"), signature, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(Attr(element, "SheetName")));
        if (signaturePart is null)
        {
            return null;
        }

        return signaturePart
            .Elements(ns + "Media")
            .FirstOrDefault(element =>
                string.Equals(Attr(element, "SheetName"), sheet, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyStockAttributes(XElement leaf, StockSheetInfo info)
    {
        SetAttrIfPresent(leaf, "Brand", info.Brand);
        SetAttrIfPresent(leaf, "DescriptiveName", info.DescriptiveName);
        SetAttrIfPresent(leaf, "Manufacturer", info.Manufacturer);
        SetAttrIfPresent(leaf, "Grade", info.Grade);
        SetAttrIfPresent(leaf, "ProductID", info.ProductId);
        SetAttrIfPresent(leaf, "GrainDirection", info.GrainDirection);
        leaf.SetAttributeValue("MediaUnit", "Sheet");

        if (info.WeightGsm.HasValue)
        {
            leaf.SetAttributeValue("Weight", info.WeightGsm.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (info.ThicknessMicrons.HasValue)
        {
            leaf.SetAttributeValue("Thickness", info.ThicknessMicrons.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void SetAttrIfPresent(XElement element, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            element.SetAttributeValue(name, value);
        }
    }

    private static List<StockSheetInfo> BuildStockSequence(MetrixMxmlDocument metrixMxml)
    {
        var units = metrixMxml.Units ?? "Inches";
        var stockSheets = new List<StockSheetInfo>();
        var sheetById = new Dictionary<string, StockSheetInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var stock in metrixMxml.ResourcePool.Stocks)
        {
            foreach (var sheet in stock.StockSheets)
            {
                if (string.IsNullOrWhiteSpace(sheet.Id))
                {
                    continue;
                }

                var width = TryParseDecimal(sheet.Width);
                var height = TryParseDecimal(sheet.Height);
                if (!width.HasValue || !height.HasValue)
                {
                    continue;
                }

                var widthPoints = ConvertToPoints(width.Value, units);
                var heightPoints = ConvertToPoints(height.Value, units);
                var grainLong = ResolveGrainLong(sheet, width.Value, height.Value);
                var grainDirection = grainLong.HasValue ? (grainLong.Value ? "LongEdge" : "ShortEdge") : null;

                var descriptive = stock.Description ?? stock.Name;
                var brand = stock.Name ?? stock.Description;
                var manufacturer = stock.Vendor;
                var grade = stock.Grade;
                var productId = sheet.MisId ?? stock.MisId;

                var basisWeight = ResolveBasisWeight(stock.Weight, stock.WeightUnit);
                var weightGsm = ResolveWeightGsm(basisWeight, grade, descriptive, brand) ?? DefaultWeightGsm;
                var thicknessMicron = ResolveThicknessMicrons(sheet.Thickness, stock.Thickness) ?? DefaultThicknessMicrons;

                var info = new StockSheetInfo(
                    sheet.Id,
                    widthPoints,
                    heightPoints,
                    grainDirection,
                    brand,
                    descriptive,
                    manufacturer,
                    grade,
                    productId,
                    weightGsm,
                    thicknessMicron);

                sheetById[sheet.Id] = info;
                stockSheets.Add(info);
            }
        }

        var sequence = new List<StockSheetInfo>();
        foreach (var layout in metrixMxml.Project.Layouts)
        {
            if (!string.IsNullOrWhiteSpace(layout.StockSheetRefId) &&
                sheetById.TryGetValue(layout.StockSheetRefId, out var info))
            {
                sequence.Add(info);
            }
        }

        if (sequence.Count == 0)
        {
            sequence.AddRange(stockSheets);
        }

        return sequence;
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal ConvertToPoints(decimal value, string units)
    {
        if (units.Contains("mm", StringComparison.OrdinalIgnoreCase))
        {
            return value * 72m / 25.4m;
        }

        return value * 72m;
    }

    private static bool? ResolveGrainLong(MetrixMxmlStockSheet sheet, decimal width, decimal height)
    {
        if (TryParseBool(sheet.BuySheetLongGrain, out var longGrain))
        {
            return longGrain;
        }

        if (string.IsNullOrWhiteSpace(sheet.Grain))
        {
            return null;
        }

        var grain = sheet.Grain.Trim().ToLowerInvariant();
        if (grain is "horizontal" or "horiz" or "h")
        {
            return width >= height;
        }

        if (grain is "vertical" or "vert" or "v")
        {
            return height >= width;
        }

        return null;
    }

    private static bool TryParseBool(string? value, out bool parsed)
    {
        parsed = false;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "true" or "yes" or "1")
        {
            parsed = true;
            return true;
        }

        if (normalized is "false" or "no" or "0")
        {
            parsed = false;
            return true;
        }

        return false;
    }

    private static decimal? ResolveBasisWeight(string? weightValue, string? weightUnit)
    {
        var weight = TryParseDecimal(weightValue);
        if (!weight.HasValue)
        {
            return null;
        }

        var unit = weightUnit?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(unit) && unit is not ("lb" or "lbs" or "pound" or "pounds"))
        {
            return null;
        }

        return weight.Value;
    }

    private static int? ResolveWeightGsm(decimal? basisWeight, string? grade, string? descriptive, string? brand)
    {
        if (!basisWeight.HasValue)
        {
            return null;
        }

        var gradeKey = grade?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(gradeKey) || !LbToGsmFactors.ContainsKey(gradeKey))
        {
            gradeKey = InferGradeFromName(descriptive) ?? InferGradeFromName(brand);
        }

        if (gradeKey is null || !LbToGsmFactors.TryGetValue(gradeKey, out var factor))
        {
            return null;
        }

        return (int)Math.Round(basisWeight.Value * factor, MidpointRounding.AwayFromZero);
    }

    private static string? InferGradeFromName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lowered = value.ToLowerInvariant();
        foreach (var (grade, keywords) in GradeKeywords)
        {
            if (keywords.Any(keyword => lowered.Contains(keyword, StringComparison.Ordinal)))
            {
                return grade;
            }
        }

        return null;
    }

    private static int? ResolveThicknessMicrons(string? sheetThickness, string? stockThickness)
    {
        var thickness = TryParseDecimal(sheetThickness) ?? TryParseDecimal(stockThickness);
        if (!thickness.HasValue)
        {
            return null;
        }

        return (int)Math.Round(thickness.Value * 25400m, MidpointRounding.AwayFromZero);
    }

    private const int DefaultWeightGsm = 135;
    private const int DefaultThicknessMicrons = 120;

    private static readonly Dictionary<string, decimal> LbToGsmFactors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TEXT"] = 1.480m,
        ["COVER"] = 2.708m,
        ["INDEX"] = 1.810m,
        ["TAG"] = 1.629m,
        ["BRISTOL"] = 2.197m,
        ["BOND"] = 3.760m
    };

    private static readonly Dictionary<string, string[]> GradeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TEXT"] = new[] { "text", "book", "offset" },
        ["COVER"] = new[] { "cover", "c1s", "c2s" },
        ["INDEX"] = new[] { "index" },
        ["TAG"] = new[] { "tag" },
        ["BRISTOL"] = new[] { "bristol" },
        ["BOND"] = new[] { "bond", "writing" }
    };

    private static void ApplyUniformPaperAttributes(XElement paperMedia, XNamespace ns)
    {
        var leaves = paperMedia.Descendants(ns + "Media")
            .Where(element => !string.IsNullOrWhiteSpace(Attr(element, "SheetName")))
            .ToList();
        if (leaves.Count == 0)
        {
            return;
        }

        var attributes = new[] { "Brand", "DescriptiveName", "Manufacturer", "Grade", "ProductID", "GrainDirection", "Weight", "Thickness", "MediaUnit" };
        foreach (var name in attributes)
        {
            var values = leaves
                .Select(element => element.Attribute(name)?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count == 1)
            {
                paperMedia.SetAttributeValue(name, values[0]);
            }
        }
    }

    private static void StripPaperMetadata(XElement? paperMedia, XNamespace ns)
    {
        if (paperMedia is null)
        {
            return;
        }

        var attributes = new[]
        {
            "Brand",
            "DescriptiveName",
            "Manufacturer",
            "Grade",
            "ProductID",
            "GrainDirection",
            "Weight",
            "Thickness",
            "MediaUnit"
        };

        foreach (var element in paperMedia.DescendantsAndSelf(ns + "Media"))
        {
            foreach (var name in attributes)
            {
                element.SetAttributeValue(name, null);
            }
        }
    }

    private static bool TryParseBox(string? value, out decimal width, out decimal height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return false;
        }

        if (!decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out width) ||
            !decimal.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out height))
        {
            return false;
        }

        if (decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) &&
            decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var bottom))
        {
            width -= left;
            height -= bottom;
        }

        return true;
    }

    private static bool TryParseDimension(string? value, out decimal width, out decimal height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        return decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out width)
               && decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out height);
    }

    private static string ShiftTransform(string ctm, (decimal X, decimal Y) offset)
    {
        var parts = ctm.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count < 6)
        {
            return ctm;
        }

        if (!decimal.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !decimal.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return ctm;
        }

        x += offset.X;
        y += offset.Y;

        parts[4] = x.ToString(CultureInfo.InvariantCulture);
        parts[5] = y.ToString(CultureInfo.InvariantCulture);

        return string.Join(' ', parts);
    }

    private static string ShiftBox(string box, (decimal X, decimal Y) offset)
    {
        var parts = box.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count < 4)
        {
            return box;
        }

        if (!decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) ||
            !decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var bottom) ||
            !decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var right) ||
            !decimal.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var top))
        {
            return box;
        }

        left += offset.X;
        right += offset.X;
        bottom += offset.Y;
        top += offset.Y;

        return string.Join(' ', new[]
        {
            left.ToString(CultureInfo.InvariantCulture),
            bottom.ToString(CultureInfo.InvariantCulture),
            right.ToString(CultureInfo.InvariantCulture),
            top.ToString(CultureInfo.InvariantCulture)
        });
    }

    private static void ApplyLabels(XDocument signaDocument, MetrixJdfDocument metrixDocument, MetrixMxmlDocument? metrixMxml)
    {
        if (metrixMxml is null)
        {
            return;
        }

        var ords = metrixDocument.Layout?
            .Signatures.SelectMany(signature => signature.Sheets)
            .SelectMany(sheet => sheet.Surfaces)
            .SelectMany(surface => surface.ContentObjects)
            .Select(content => ParseOrdValue(content.Ord))
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .Distinct()
            .Order()
            .ToList();

        if (ords is null || ords.Count == 0)
        {
            return;
        }

        var labels = BuildLabels(metrixDocument, metrixMxml, ords);
        if (labels.Count == 0)
        {
            return;
        }

        var ns = signaDocument.Root?.Name.Namespace;
        if (ns is null)
        {
            return;
        }

        foreach (var content in signaDocument.Descendants(ns + "ContentObject"))
        {
            var ord = ParseOrdValue(content.Attribute("Ord")?.Value);
            if (ord is null)
            {
                continue;
            }

            if (labels.TryGetValue(ord.Value, out var label))
            {
                content.SetAttributeValue("DescriptiveName", label);
            }
        }
    }

    private static Dictionary<int, string> BuildLabels(MetrixJdfDocument metrixDocument, MetrixMxmlDocument metrixMxml, List<int> ords)
    {
        var mode = ResolveLabelMode(metrixDocument, metrixMxml);
        var labels = new Dictionary<int, string>();

        var baseMap = BuildPostcardBaseMap(metrixDocument);
        if (baseMap.Count > 0)
        {
            foreach (var ord in ords)
            {
                if (baseMap.TryGetValue(ord, out var baseName))
                {
                    labels[ord] = $"{baseName}-{ord + 1}";
                }
            }
        }

        var folios = mode == LabelMode.MultiProduct
            ? BuildProductFolioStream(metrixMxml, includeProductPrefix: true)
            : BuildProductFolioStream(metrixMxml, includeProductPrefix: false);

        foreach (var ord in ords)
        {
            if (labels.ContainsKey(ord))
            {
                continue;
            }

            if (ord >= 0 && ord < folios.Count)
            {
                labels[ord] = folios[ord];
            }
        }

        return labels;
    }

    private static LabelMode ResolveLabelMode(MetrixJdfDocument metrixDocument, MetrixMxmlDocument metrixMxml)
    {
        if (metrixMxml.Project.Products.Count > 1)
        {
            return LabelMode.MultiProduct;
        }

        var pageData = metrixDocument.RunLists
            .SelectMany(runList => runList.PageList)
            .Select(page => page.DescriptiveName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToList();

        if (pageData.Any(name => !name.StartsWith("cover", StringComparison.OrdinalIgnoreCase)))
        {
            return LabelMode.Postcards;
        }

        return LabelMode.Book;
    }

    private static Dictionary<int, string> BuildPostcardBaseMap(MetrixJdfDocument metrixDocument)
    {
        var baseMap = new Dictionary<int, string>();
        foreach (var page in metrixDocument.RunLists.SelectMany(runList => runList.PageList))
        {
            if (string.IsNullOrWhiteSpace(page.DescriptiveName) ||
                string.IsNullOrWhiteSpace(page.PageIndex))
            {
                continue;
            }

            var token = page.PageIndex.Replace(" ", "", StringComparison.Ordinal);
            if (token.Contains("~"))
            {
                var parts = token.Split('~', 2);
                if (int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end))
                {
                    for (var i = start; i <= end; i++)
                    {
                        baseMap[i] = page.DescriptiveName;
                    }
                }
            }
            else if (int.TryParse(token, out var index))
            {
                baseMap[index] = page.DescriptiveName;
            }
        }

        return baseMap;
    }

    private static List<string> BuildProductFolioStream(MetrixMxmlDocument metrixMxml, bool includeProductPrefix)
    {
        var labels = new List<string>();

        foreach (var product in metrixMxml.Project.Products)
        {
            var productLabel = product.Description ?? product.Name ?? product.Id ?? "Product";
            var pageIndex = 0;
            foreach (var page in product.Pages)
            {
                var folio = page.Folio ?? page.Number ?? (pageIndex + 1).ToString(CultureInfo.InvariantCulture);
                var label = includeProductPrefix ? $"{productLabel}_{folio}" : folio;
                labels.Add(label);
                pageIndex++;
            }
        }

        return labels;
    }

    private static bool IsSimplex(string? workStyle)
    {
        return string.Equals(workStyle, "SS", StringComparison.OrdinalIgnoreCase)
               || string.Equals(workStyle, "SF", StringComparison.OrdinalIgnoreCase)
               || string.Equals(workStyle, "Simplex", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ParseOrdValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }

    private enum LabelMode
    {
        Book,
        Postcards,
        MultiProduct
    }
}

internal readonly record struct StockSheetInfo(
    string Id,
    decimal WidthPoints,
    decimal HeightPoints,
    string? GrainDirection,
    string? Brand,
    string? DescriptiveName,
    string? Manufacturer,
    string? Grade,
    string? ProductId,
    int? WeightGsm,
    int? ThicknessMicrons);

internal readonly record struct SeparationSpecInfo(
    string? Name,
    string? HdmType,
    string? HdmSubType,
    string? HdmIsMapRel);

internal readonly record struct SheetPosition(
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    decimal PlateWidth,
    decimal PlateHeight);
