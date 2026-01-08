using System.Xml.Linq;

namespace Signa.Jdf;

public sealed class SignaJdfValidator
{
    // Validates a Signa-style JDF against empirical Cockpit/Signa expectations.
    public IReadOnlyList<ValidationIssue> Validate(JdfDocument document)
    {
        var issues = new List<ValidationIssue>();

        if (!string.Equals(document.Root.Type, "ProcessGroup", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "ROOT_TYPE",
                $"Root Type is '{document.Root.Type ?? "(missing)"}'; expected 'ProcessGroup'.");
        }

        if (!document.Types.Contains("Imposition", StringComparer.OrdinalIgnoreCase))
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "ROOT_TYPES_IMPOSITION",
                "Root Types does not include 'Imposition'.");
        }

        if (document.Layout is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "LAYOUT_MISSING",
                "Missing ResourcePool/Layout.");
        }
        else if (!string.Equals(document.Layout.PartIdKeys, "SignatureName SheetName Side", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "LAYOUT_PARTIDKEYS",
                $"Layout PartIDKeys is '{document.Layout.PartIdKeys ?? "(missing)"}'; expected 'SignatureName SheetName Side'.");
        }

        if (!document.ResourceLinks.Any())
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "RESOURCELINK_MISSING",
                "Missing ResourceLinkPool or no ResourceLink entries found.");
        }

        // RunList links are the minimum wiring required for importability checks.
        RequireRunListUsage(document, issues, "Document", ValidationSeverity.Error);
        RequireRunListUsage(document, issues, "Marks", ValidationSeverity.Error);
        if (HasPagePoolRunListOrLink(document))
        {
            RequireRunListUsage(document, issues, "PagePool", ValidationSeverity.Warning);
        }
        ValidateRunListLinkConsistency(document, issues);

        if (!document.PrintingParams.Any())
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "PRINTING_PARAMS_MISSING",
                "No ConventionalPrintingParams found; work style mapping may be incomplete.");
        }

        ValidateRequiredHdmFields(document, issues);
        ValidateMinimalCockpitImportability(document, issues);

        return issues;
    }

    private static bool HasPagePoolRunListOrLink(JdfDocument document)
    {
        var hasLink = document.ResourceLinks.Any(link =>
            string.Equals(link.LinkType, "RunListLink", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(link.ProcessUsage, "PagePool", StringComparison.OrdinalIgnoreCase));
        if (hasLink)
        {
            return true;
        }

        return document.RunLists.Any(list =>
            string.Equals(list.DescriptiveName, "PagePool", StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateRunListLinkConsistency(JdfDocument document, List<ValidationIssue> issues)
    {
        // Heuristics reflect common Signa partitions; mismatches are warnings, not fatal errors.
        var runListById = document.RunLists
            .Where(list => !string.IsNullOrWhiteSpace(list.Id))
            .ToDictionary(list => list.Id!, StringComparer.OrdinalIgnoreCase);

        var marksPartIdMismatch = 0;
        var documentPartIdMismatch = 0;
        var pagePoolPartIdMismatch = 0;
        foreach (var link in document.ResourceLinks.Where(link =>
                     string.Equals(link.LinkType, "RunListLink", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.Equals(link.ProcessUsage, "Document", StringComparison.OrdinalIgnoreCase))
            {
                if (link.RefId is null || !runListById.TryGetValue(link.RefId, out var documentRunList))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(documentRunList.PartIdKeys) &&
                    !string.Equals(documentRunList.PartIdKeys, "Run", StringComparison.OrdinalIgnoreCase))
                {
                    documentPartIdMismatch++;
                }
            }

            if (string.Equals(link.ProcessUsage, "Marks", StringComparison.OrdinalIgnoreCase))
            {
                if (link.RefId is null || !runListById.TryGetValue(link.RefId, out var marksRunList))
                {
                    continue;
                }

                if (!string.Equals(marksRunList.PartIdKeys, "SignatureName SheetName Side", StringComparison.OrdinalIgnoreCase))
                {
                    marksPartIdMismatch++;
                }
            }

            if (!string.Equals(link.ProcessUsage, "PagePool", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (link.RefId is null || !runListById.TryGetValue(link.RefId, out var pagePoolRunList))
            {
                continue;
            }

            if (!string.Equals(pagePoolRunList.PartIdKeys, "Run", StringComparison.OrdinalIgnoreCase))
            {
                pagePoolPartIdMismatch++;
            }
        }

        if (documentPartIdMismatch > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "RUNLIST_DOCUMENT_PARTIDKEYS_MISMATCH",
                $"Document RunList PartIDKeys is not 'Run' for {documentPartIdMismatch} RunListLink entries.",
                "Signa uses either PartIDKeys='Run' or omits PartIDKeys on document RunLists.");
        }

        if (marksPartIdMismatch > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "RUNLIST_MARKS_PARTIDKEYS_MISMATCH",
                $"Marks RunList PartIDKeys is not 'SignatureName SheetName Side' for {marksPartIdMismatch} RunListLink entries.",
                "Signa exports typically partition marks by SignatureName/SheetName/Side.");
        }

        if (pagePoolPartIdMismatch > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "RUNLIST_PAGEPOOL_PARTIDKEYS_MISMATCH",
                $"PagePool RunList PartIDKeys is not 'Run' for {pagePoolPartIdMismatch} RunListLink entries.",
                "Signa usually uses PartIDKeys='Run' on PagePool RunLists; missing keys appear in some builds.");
        }

        // PagePool presence is used as a signal for assigned PDF usage.
        var documentRunListResource = document.FindRunListById(document.GetRunListRef("Document"));
        var pagePoolRunListResource = document.FindRunListById(document.GetRunListRef("PagePool"));
        var hasDocumentPdf = RunListHasFileSpec(documentRunListResource);
        var hasPagePoolPdf = RunListHasFileSpec(pagePoolRunListResource);
        var hasAssignedPdf = hasDocumentPdf || hasPagePoolPdf;

        if (hasAssignedPdf && pagePoolRunListResource is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "RUNLIST_PAGEPOOL_MISSING_WITH_PDF",
                "PDF assignments are present, but PagePool RunList is missing.",
                "Signa typically emits PagePool when pages are assigned; Cockpit-initiated jobs may populate PagePool even without manual assignment. PagePool is optional for third-party JDFs or workflows without Signa.");
        }

        if (pagePoolRunListResource is not null && !hasAssignedPdf)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "RUNLIST_PAGEPOOL_WITHOUT_PDF",
                "PagePool RunList is present, but no PDF FileSpec is assigned in Document or PagePool.",
                "Signa typically emits PagePool only when pages are assigned.");
        }
    }

    private static void ValidateMinimalCockpitImportability(JdfDocument document, List<ValidationIssue> issues)
    {
        // Minimal checklist is derived from Cockpit import experiments and may evolve.
        var summaryFailures = new List<string>();
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return;
        }

        var ns = document.JdfNamespace;
        var hdm = document.HdmNamespace;
        var resourcePool = root.Element(ns + "ResourcePool");
        var layout = resourcePool?.Element(ns + "Layout");

        var marksLink = document.ResourceLinks.FirstOrDefault(link =>
            string.Equals(link.LinkType, "RunListLink", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(link.ProcessUsage, "Marks", StringComparison.OrdinalIgnoreCase));
        var marksRunList = document.FindRunListById(marksLink?.RefId)
            ?? document.RunLists.FirstOrDefault(list =>
                string.Equals(list.DescriptiveName, "Marks", StringComparison.OrdinalIgnoreCase));
        if (marksRunList is null)
        {
            return;
        }

        if (!document.Types.Contains("ConventionalPrinting", StringComparer.OrdinalIgnoreCase))
        {
            summaryFailures.Add("types:missing_conventional_printing");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_TYPES_CONVENTIONAL_PRINTING",
                "Root Types does not include 'ConventionalPrinting'; Cockpit importability is unverified without it.");
        }

        if (layout is null)
        {
            summaryFailures.Add("layout:missing");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_LAYOUT_MISSING",
                "Layout is required for Cockpit importability (minimal emitter checklist).");
            return;
        }

        // SurfaceContentsBox drives sheet placement; missing values cause offset previews.
        var sheetLayouts = resourcePool?.Descendants(ns + "Layout")
            .Where(element => !string.IsNullOrWhiteSpace(element.Attribute("SheetName")?.Value))
            .ToList() ?? new List<XElement>();
        var hasSurfaceContentsBox = sheetLayouts.Any(element =>
            !string.IsNullOrWhiteSpace(element.Attribute("SurfaceContentsBox")?.Value));
        if (sheetLayouts.Count == 0 || !hasSurfaceContentsBox)
        {
            summaryFailures.Add("layout:surface_contents_box");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_LAYOUT_SURFACE_CONTENTS_BOX",
                "Layout SurfaceContentsBox is missing on sheet-level Layout; Cockpit may reject or misplace sheets.");
        }

        // PaperRect anchors pages to sheet origin; missing values lead to plate-relative placements.
        var sideLayouts = resourcePool?.Descendants(ns + "Layout")
            .Where(element => !string.IsNullOrWhiteSpace(element.Attribute("Side")?.Value))
            .ToList() ?? new List<XElement>();
        var hasPaperRect = sideLayouts.Any(element =>
            !string.IsNullOrWhiteSpace(element.Attribute(hdm + "PaperRect")?.Value));
        if (sideLayouts.Count == 0 || !hasPaperRect)
        {
            summaryFailures.Add("layout:paper_rect");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_LAYOUT_PAPERRECT",
                "Layout HDM:PaperRect is missing on side-level Layout; page placement may be relative to the plate instead of the sheet.");
        }

        // Marks geometry is required for imposed PDF generation in Cockpit.
        var markObjects = root.Descendants(ns + "MarkObject").ToList();
        if (markObjects.Count == 0)
        {
            summaryFailures.Add("marks:missing");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MARKOBJECT_MISSING",
                "No MarkObject entries found; imposed PDF generation can fail without mark geometry.");
        }
        else if (markObjects.Any(mark =>
                     string.IsNullOrWhiteSpace(mark.Attribute("CTM")?.Value) ||
                     string.IsNullOrWhiteSpace(mark.Attribute("ClipBox")?.Value)))
        {
            summaryFailures.Add("marks:geometry");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MARKOBJECT_GEOMETRY",
                "MarkObject entries missing CTM or ClipBox; imposed PDF generation can fail.");
        }

        // ContentObjects drive page lists and assignment labels.
        var contentObjects = root.Descendants(ns + "ContentObject").ToList();
        if (contentObjects.Count == 0)
        {
            summaryFailures.Add("content:missing");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_CONTENTOBJECT_MISSING",
                "No ContentObject entries found; Cockpit will warn about an empty layout and no page list.");
        }
        else if (contentObjects.All(content =>
                     string.IsNullOrWhiteSpace(content.Attribute("Ord")?.Value) &&
                     string.IsNullOrWhiteSpace(content.Attribute("DescriptiveName")?.Value)))
        {
            summaryFailures.Add("content:labels");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_CONTENTOBJECT_LABELS",
                "ContentObjects are missing Ord/DescriptiveName; Cockpit page list labels may be incorrect.");
        }

        var hasPaper = document.Media.Any(media =>
            string.Equals(media.MediaType, "Paper", StringComparison.OrdinalIgnoreCase) &&
            HasMediaDimension(media));
        if (!hasPaper)
        {
            summaryFailures.Add("media:paper_dimension");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MEDIA_PAPER",
                "Paper Media with Dimension is missing; Cockpit may report missing media size.");
        }

        var hasPlate = document.Media.Any(media =>
            string.Equals(media.MediaType, "Plate", StringComparison.OrdinalIgnoreCase) &&
            HasMediaDimension(media));
        if (!hasPlate)
        {
            summaryFailures.Add("media:plate_dimension");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MEDIA_PLATE",
                "Plate Media with Dimension is missing; imposed PDF geometry may be incorrect.");
        }

        if (!document.ResourceLinks.Any(link =>
                string.Equals(link.LinkType, "MediaLink", StringComparison.OrdinalIgnoreCase)))
        {
            summaryFailures.Add("media:links");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MEDIA_LINKS",
                "MediaLink entries are missing; Cockpit may ignore media dimensions.");
        }

        if (string.IsNullOrWhiteSpace(marksRunList.FileSpecUrl))
        {
            summaryFailures.Add("marks:filespec_url");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_MARKS_FILESPEC",
                "Marks RunList FileSpec URL is missing; Cockpit cannot copy marks PDF.");
        }

        var hasTransferCurve = root.Element(ns + "ResourcePool")
            ?.Elements(ns + "TransferCurvePool")
            .Any() == true;
        if (!hasTransferCurve)
        {
            summaryFailures.Add("transfer_curve_pool");
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_TRANSFER_CURVE_POOL",
                "TransferCurvePool is missing; Layout Preview alignment may be incorrect.");
        }

        if (summaryFailures.Count > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "MINIMAL_IMPORTABILITY_SUMMARY",
                "Minimal Cockpit importability checklist has missing items.",
                string.Join(", ", summaryFailures));
        }
    }

    private static bool HasMediaDimension(MediaResource media)
    {
        if (!string.IsNullOrWhiteSpace(media.Dimension))
        {
            return true;
        }

        return media.Parts.Any(part => !string.IsNullOrWhiteSpace(part.Dimension));
    }

    private static void ValidateRequiredHdmFields(JdfDocument document, List<ValidationIssue> issues)
    {
        // HDM extensions are vendor-specific but are essential for Signa/Cockpit behavior.
        var ns = document.JdfNamespace;
        var hdm = document.HdmNamespace;
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return;
        }

        var resourcePool = root.Element(ns + "ResourcePool");
        var layout = resourcePool?.Element(ns + "Layout");

        if (layout is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_LAYOUT_MISSING",
                "Missing Layout for required HDM checks.");
            return;
        }

        // Signa transport metadata is required for layout edit round-trips.
        var signaBlob = layout.Element(hdm + "SignaBLOB");
        var signaJdf = layout.Element(hdm + "SignaJDF");
        var signaContext = layout.Element(hdm + "SignaGenContext");
        var workStyleSummary = BuildWorkStyleSummary(root, ns);
        var assemblyIdSummary = BuildAssemblyIdSummary(root, ns);
        var hdmNamespaceDeclared = HasHdmNamespace(root, hdm);

        if (signaBlob is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_SIGNABLOB_MISSING",
                "Missing HDM:SignaBLOB element.");
        }
        else if (string.IsNullOrWhiteSpace(signaBlob.Attribute("URL")?.Value))
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_SIGNABLOB_URL_MISSING",
                "HDM:SignaBLOB is missing URL attribute.");
        }
        else
        {
            var blobUrl = signaBlob.Attribute("URL")?.Value ?? string.Empty;
            if (!string.Equals(blobUrl, "SignaData.sdf", StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    issues,
                    ValidationSeverity.Error,
                    "HDM_SIGNABLOB_URL_VALUE",
                    $"HDM:SignaBLOB URL is '{blobUrl}'; expected 'SignaData.sdf'.");
            }
        }

        if (signaJdf is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_SIGNAJDF_MISSING",
                "Missing HDM:SignaJDF element.");
        }
        else if (string.IsNullOrWhiteSpace(signaJdf.Attribute("URL")?.Value))
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_SIGNAJDF_URL_MISSING",
                "HDM:SignaJDF is missing URL attribute.");
        }
        else
        {
            var signaJdfUrl = signaJdf.Attribute("URL")?.Value ?? string.Empty;
            if (!string.Equals(signaJdfUrl, "data.jdf", StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    issues,
                    ValidationSeverity.Error,
                    "HDM_SIGNAJDF_URL_VALUE",
                    $"HDM:SignaJDF URL is '{signaJdfUrl}'; expected 'data.jdf'.");
            }
        }

        if (signaContext is null)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_SIGNAGENCONTEXT_MISSING",
                "Missing HDM:SignaGenContext element.");
        }

        var signaJob = layout.Element(hdm + "SignaJob");
        if (signaJob is null)
        {
            var context = hdmNamespaceDeclared
                ? "HDM namespace declared; SignaJob usually present on Signa exports."
                : "HDM namespace not declared on root; SignaJob may be absent in non-Signa JDF.";
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_SIGNAJOB_MISSING",
                "Missing HDM:SignaJob element.",
                CombineHints(
                    context,
                    BuildSignaJobHint(layout, hdm)));
        }
        else if (!signaJob.Elements(hdm + "SignaJobPart").Any())
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_SIGNAJOBPART_MISSING",
                "HDM:SignaJob has no HDM:SignaJobPart elements.",
                "SignaJobPart usually appears once per product/job part.");
        }
        else
        {
            var signaJobPartCount = signaJob.Elements(hdm + "SignaJobPart").Count();
            if (signaJobPartCount > 1)
            {
                var signatureCount = root.Descendants(ns + "Layout")
                    .Select(element => element.Attribute("SignatureName")?.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                AddIssue(
                    issues,
                    ValidationSeverity.Investigation,
                    "HDM_SIGNAJOBPART_MULTIPLE",
                    $"HDM:SignaJob lists {signaJobPartCount} job parts across {signatureCount} signatures.",
                    "Signa may duplicate the layout tree per job part, tagging ContentObject with HDM:JobPart and HDM:RunlistIndex.");
            }
        }

        // ContentObject tags are checked against Signa job-part declarations.
        var contentObjects = root.Descendants(ns + "ContentObject").ToList();
        var contentJobParts = contentObjects
            .Select(element => element.Attribute(hdm + "JobPart")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var signaJobParts = signaJob?
            .Elements(hdm + "SignaJobPart")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (contentJobParts.Count > 0 && signaJobParts.Count == 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "HDM_JOBPART_WITHOUT_SIGNAPART",
                "HDM:JobPart appears on ContentObject, but no HDM:SignaJobPart entries are declared.",
                "Cockpit uses SignaJobPart + JobPart together to split products/page lists.");
        }
        else if (signaJobParts.Count == 1)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "HDM_SIGNAJOBPART_SINGLE",
                $"HDM:SignaJob declares a single job part ('{signaJobParts[0]}').",
                "Cockpit collapses to End Product unless multiple job parts are declared.");
        }

        if (signaJobParts.Count > 0)
        {
            if (contentJobParts.Count == 0)
            {
                AddIssue(
                    issues,
                    ValidationSeverity.Warning,
                    "HDM_JOBPART_MISSING",
                    "No HDM:JobPart tags found on ContentObject elements.",
                    "Cockpit does not build page lists without JobPart tags.");
            }
            else
            {
                var undeclared = contentJobParts
                    .Except(signaJobParts, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (undeclared.Count > 0)
                {
                    AddIssue(
                        issues,
                        ValidationSeverity.Warning,
                        "HDM_JOBPART_UNDECLARED",
                        $"HDM:JobPart values not declared in HDM:SignaJobPart: {string.Join(", ", undeclared)}.",
                        "Undeclared JobPart values create an End Product bucket and can desync page lists.");
                }

                var missingJobPartCount = contentObjects.Count(element =>
                    string.IsNullOrWhiteSpace(element.Attribute(hdm + "JobPart")?.Value));
                if (missingJobPartCount > 0 && contentJobParts.Count > 0)
                {
                    AddIssue(
                        issues,
                        ValidationSeverity.Warning,
                        "HDM_JOBPART_PARTIAL",
                        $"HDM:JobPart is missing on {missingJobPartCount} ContentObject elements.",
                        "Partially tagged JobPart sets can cause End Product splits or page list gaps.");
                }
            }
        }

        var sideLayouts = root.Descendants(ns + "Layout")
            .Where(element => element.Attribute("Side") is not null)
            .ToList();
        var missingPaperRect = sideLayouts.Count(element => element.Attribute(hdm + "PaperRect") is null);
        if (missingPaperRect > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAPERRECT_MISSING",
                $"HDM:PaperRect missing on {missingPaperRect} side Layout elements.");
        }
        var invalidPaperRect = sideLayouts.Count(element =>
            !IsNumericList(element.Attribute(hdm + "PaperRect")?.Value, 4));
        if (invalidPaperRect > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAPERRECT_VALUE",
                $"HDM:PaperRect is not four numeric values on {invalidPaperRect} side Layout elements.");
        }
        var mismatchedPaperRect = sideLayouts.Count(element =>
            PaperRectMismatch(element, hdm));
        if (mismatchedPaperRect > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAPERRECT_DERIVED",
                $"HDM:PaperRect does not match derived Paper Media + TransferCurveSet on {mismatchedPaperRect} side Layout elements.");
        }

        // FinalPageBox + PageOrientation consistency is critical for Cockpit preview.
        var missingFinalPageBox = contentObjects.Count(element => element.Attribute(hdm + "FinalPageBox") is null);
        if (missingFinalPageBox > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_FINALPAGEBOX_MISSING",
                $"HDM:FinalPageBox missing on {missingFinalPageBox} ContentObject elements.");
        }
        var invalidFinalPageBox = contentObjects.Count(element =>
            !IsNumericList(element.Attribute(hdm + "FinalPageBox")?.Value, 4));
        if (invalidFinalPageBox > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_FINALPAGEBOX_VALUE",
                $"HDM:FinalPageBox is not four numeric values on {invalidFinalPageBox} ContentObject elements.");
        }
        var mismatchedFinalPageBox = contentObjects.Count(element =>
            FinalPageBoxMismatch(element, hdm));
        if (mismatchedFinalPageBox > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_FINALPAGEBOX_DERIVED",
                $"HDM:FinalPageBox does not match derived TrimCTM+TrimSize on {mismatchedFinalPageBox} ContentObject elements.");
        }
        var mismatchedFinalPageBoxClip = contentObjects.Count(element =>
            FinalPageBoxClipMismatch(element, hdm));
        if (mismatchedFinalPageBoxClip > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_FINALPAGEBOX_CLIP_DERIVED",
                $"HDM:FinalPageBox does not match derived ClipBox+TrimSize on {mismatchedFinalPageBoxClip} ContentObject elements.",
                "priority=low; ClipBox derivation is a fallback when TrimCTM is unavailable.");
        }
        var clipBoxOutOfBounds = contentObjects.Count(element =>
            ClipBoxOutOfBounds(element, hdm));
        if (clipBoxOutOfBounds > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "CLIPBOX_OUT_OF_BOUNDS",
                $"ContentObject ClipBox does not fully contain derived TrimCTM+TrimSize on {clipBoxOutOfBounds} ContentObject elements.",
                "priority=low; check ClipBox bounds against trimmed placement.");
        }

        var missingPageOrientation = contentObjects.Count(element => element.Attribute(hdm + "PageOrientation") is null);
        if (missingPageOrientation > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAGEORIENTATION_MISSING",
                $"HDM:PageOrientation missing on {missingPageOrientation} ContentObject elements.");
        }
        var invalidPageOrientation = contentObjects.Count(element =>
            !IsValidPageOrientation(element.Attribute(hdm + "PageOrientation")?.Value));
        if (invalidPageOrientation > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAGEORIENTATION_VALUE",
                $"HDM:PageOrientation is not 0/90/180/270 on {invalidPageOrientation} ContentObject elements.");
        }
        var mismatchedPageOrientation = contentObjects.Count(element =>
            PageOrientationMismatch(element, hdm));
        if (mismatchedPageOrientation > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_PAGEORIENTATION_DERIVED",
                $"HDM:PageOrientation does not match derived orientation on {mismatchedPageOrientation} ContentObject elements.");
        }
        // Perfecting layouts expect mirrored back-side geometry unless a symmetric fold scheme is used.
        var perfectingBackGeometryMismatch = CountPerfectingBackGeometryMismatches(root, ns, hdm);
        if (perfectingBackGeometryMismatch > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Warning,
                "PERFECTING_BACK_GEOMETRY_MISMATCH",
                $"Perfecting work style has {perfectingBackGeometryMismatch} back-side ContentObject elements without mirrored CTM or 180 orientation.",
                "Perfecting back-side placements are typically mirrored (CTM -1 0 0 -1) with HDM:PageOrientation=180; WorkAndBack keeps orientation 0.");
        }

        var missingAssemblyFb = contentObjects.Count(element => element.Attribute(hdm + "AssemblyFB") is null);
        if (missingAssemblyFb > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_ASSEMBLYFB_MISSING",
                $"HDM:AssemblyFB missing on {missingAssemblyFb} ContentObject elements.",
                CombineHints(
                    $"{workStyleSummary}; {assemblyIdSummary}",
                    BuildContentAssemblyHint(root, ns, hdm)));
        }

        var missingAssemblyIds = contentObjects.Count(element => element.Attribute(hdm + "AssemblyIDs") is null);
        if (missingAssemblyIds > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_ASSEMBLYIDS_MISSING",
                $"HDM:AssemblyIDs missing on {missingAssemblyIds} ContentObject elements.",
                CombineHints(
                    workStyleSummary,
                    BuildContentAssemblyHint(root, ns, hdm)));
        }

        var signatureLayouts = root.Descendants(ns + "Layout")
            .Where(element => element.Attribute("SignatureName") is not null)
            .ToList();
        var missingOrigName = signatureLayouts.Count(element => element.Attribute(hdm + "OrigNameBySigna") is null);
        if (missingOrigName > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_ORIGNAMEBYSIGNA_MISSING",
                $"HDM:OrigNameBySigna missing on {missingOrigName} Layout elements with SignatureName.",
                "Signature layout naming may differ for imported or remapped signatures.");
        }

        var sheetLayouts = root.Descendants(ns + "Layout")
            .Where(element => element.Attribute("SheetName") is not null)
            .ToList();
        // SurfaceContentsBox is derived from plate media + transfer curves.
        var mismatchedSurfaceBox = sheetLayouts.Count(element =>
            SurfaceContentsBoxMismatch(element));
        if (mismatchedSurfaceBox > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "SURFACECONTENTSBOX_DERIVED",
                $"SurfaceContentsBox does not match derived Plate Media + TransferCurveSet on {mismatchedSurfaceBox} sheet Layout elements.");
        }

        var plateMedia = root.Descendants(ns + "Media")
            .Where(element => string.Equals(element.Attribute("MediaType")?.Value, "Plate", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var missingLeadingEdge = plateMedia.Count(element => element.Attribute(hdm + "LeadingEdge") is null);
        var mismatchedLeadingEdge = plateMedia.Count(element => LeadingEdgeMismatch(element, hdm));
        if (mismatchedLeadingEdge > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_LEADINGEDGE_DERIVED",
                $"HDM:LeadingEdge does not match Plate Dimension height on {mismatchedLeadingEdge} Plate Media elements.");
        }
        if (missingLeadingEdge > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_LEADINGEDGE_MISSING",
                $"HDM:LeadingEdge missing on {missingLeadingEdge} Plate Media elements.",
                CombineHints(
                    "Plate leading edge appears in most Signa exports; omission may indicate legacy build or non-plate media.",
                    BuildLeadingEdgeHint(layout, ns)));
        }

        // CIP3 transforms are derived by offsetting BlockTrf with PaperRect.
        var cutBlocks = root.Descendants(ns + "CutBlock").ToList();
        var missingBlockTrf = cutBlocks.Count(element => element.Attribute(hdm + "CIP3BlockTrf") is null);
        var mismatchedBlockTrf = 0;
        foreach (var cutBlock in cutBlocks)
        {
            var paperRect = GetPaperRectForCutBlock(cutBlock, hdm, ns);
            if (paperRect is null)
            {
                continue;
            }

            if (BlockTrfMismatch(cutBlock, paperRect, hdm))
            {
                mismatchedBlockTrf++;
            }
        }
        if (mismatchedBlockTrf > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_CIP3BLOCKTRF_DERIVED",
                $"HDM:CIP3BlockTrf does not match BlockTrf + PaperRect on {mismatchedBlockTrf} CutBlock elements.");
        }
        if (missingBlockTrf > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_CIP3BLOCKTRF_MISSING",
                $"HDM:CIP3BlockTrf missing on {missingBlockTrf} CutBlock elements.",
                "CIP3 transforms can be omitted when downstream folding/cutting data is not generated.");
        }

        // Fold dimensions are only required for non-montage layouts with real folding data.
        var foldingParams = root.Descendants(ns + "FoldingParams").ToList();
        var isMontage = IsMontageLayout(root, ns);
        var missingFoldIn1 = 0;
        var missingFoldIn2 = 0;
        if (!isMontage)
        {
            missingFoldIn1 = foldingParams.Count(element =>
                IsTopLevelFoldingParams(element, ns) &&
                RequiresFoldDimensions(element, ns) &&
                element.Attribute(hdm + "CIP3FoldSheetIn_1") is null);
            missingFoldIn2 = foldingParams.Count(element =>
                IsTopLevelFoldingParams(element, ns) &&
                RequiresFoldDimensions(element, ns) &&
                element.Attribute(hdm + "CIP3FoldSheetIn_2") is null);
        }
        if (missingFoldIn1 > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_CIP3FOLDSHEETIN1_MISSING",
                $"HDM:CIP3FoldSheetIn_1 missing on {missingFoldIn1} FoldingParams elements.",
                CombineHints(
                    BuildFoldingHint(root, ns),
                    BuildFoldSheetPriorityHint(root, ns, hdm)));
        }
        if (missingFoldIn2 > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_CIP3FOLDSHEETIN2_MISSING",
                $"HDM:CIP3FoldSheetIn_2 missing on {missingFoldIn2} FoldingParams elements.",
                CombineHints(
                    BuildFoldingHint(root, ns),
                    BuildFoldSheetPriorityHint(root, ns, hdm)));
        }
        var combiningOutputBlocks = BuildCombiningOutputBlockMap(root, hdm);
        var mismatchedFoldSheet = foldingParams.Count(element =>
            IsTopLevelFoldingParams(element, ns) &&
            RequiresFoldDimensions(element, ns) &&
            FoldSheetMismatch(element, root, ns, hdm, combiningOutputBlocks));
        if (mismatchedFoldSheet > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Error,
                "HDM_CIP3FOLDSHEETIN_DERIVED",
                $"HDM:CIP3FoldSheetIn_1/2 does not match derived folding dimensions on {mismatchedFoldSheet} FoldingParams elements.");
        }

        var blockComponents = root.Descendants(ns + "Component")
            .Where(element =>
                (element.Attribute("ComponentType")?.Value ?? string.Empty)
                .Contains("Block", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var blockComponentsWithAssembly = blockComponents
            .Where(element => !string.IsNullOrWhiteSpace(element.Attribute("AssemblyIDs")?.Value))
            .ToList();
        var missingClosedDims = 0;
        var missingOpenedDims = 0;
        if (!isMontage)
        {
            missingClosedDims = blockComponentsWithAssembly.Count(element => element.Attribute(hdm + "ClosedFoldingSheetDimensions") is null);
            missingOpenedDims = blockComponentsWithAssembly.Count(element => element.Attribute(hdm + "OpenedFoldingSheetDimensions") is null);
        }
        if (missingClosedDims > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_CLOSEDFOLDINGDIMENSIONS_MISSING",
                $"HDM:ClosedFoldingSheetDimensions missing on {missingClosedDims} Component Block elements with AssemblyIDs.",
                BuildBlockDimensionHint(root, ns));
        }
        if (missingOpenedDims > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_OPENEDFOLDINGDIMENSIONS_MISSING",
                $"HDM:OpenedFoldingSheetDimensions missing on {missingOpenedDims} Component Block elements with AssemblyIDs.",
                BuildBlockDimensionHint(root, ns));
        }

        var combiningParams = root.Descendants(hdm + "CombiningParams").ToList();
        var topLevelCombining = combiningParams
            .Where(element => element.Parent?.Name != hdm + "CombiningParams")
            .ToList();
        var missingOutputBlock = topLevelCombining.Count(element => !HasOutputBlockName(element, hdm));
        if (missingOutputBlock > 0)
        {
            AddIssue(
                issues,
                ValidationSeverity.Investigation,
                "HDM_OUTPUTBLOCKNAME_MISSING",
                $"HDM:OutputBlockName missing in {missingOutputBlock} CombiningParams trees.",
                BuildCombiningHint(combiningParams));
        }
    }

    private static bool RunListHasFileSpec(RunListResource? runList)
    {
        if (runList is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(runList.FileSpecUrl))
        {
            return true;
        }

        foreach (var part in runList.Parts)
        {
            if (RunListPartHasFileSpec(part))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RunListPartHasFileSpec(RunListPart part)
    {
        if (!string.IsNullOrWhiteSpace(part.FileSpecUrl))
        {
            return true;
        }

        foreach (var child in part.Children)
        {
            if (RunListPartHasFileSpec(child))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RequiresFoldDimensions(XElement foldingParams, XNamespace ns)
    {
        var noOp = foldingParams.Attribute("NoOp")?.Value;
        if (string.Equals(noOp, "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var foldCatalog = foldingParams.Attribute("FoldCatalog")?.Value;
        if (string.Equals(foldCatalog, "unnamed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (foldingParams.Elements(ns + "Fold").Any())
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(foldCatalog))
        {
            return true;
        }

        return false;
    }

    private static bool IsMontageLayout(XElement root, XNamespace ns)
    {
        return root.Descendants(ns + "BinderySignature")
            .Any(element => string.Equals(element.Attribute("BinderySignatureType")?.Value, "Grid", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTopLevelFoldingParams(XElement foldingParams, XNamespace ns)
    {
        var parent = foldingParams.Parent;
        while (parent is not null)
        {
            if (parent.Name == ns + "FoldingParams")
            {
                return false;
            }
            parent = parent.Parent;
        }

        return true;
    }

    private static bool FoldSheetMismatch(
        XElement foldingParams,
        XElement root,
        XNamespace ns,
        XNamespace hdm,
        IReadOnlyDictionary<string, List<CombiningOutputBlockEntry>> combiningOutputBlocks)
    {
        var in1Value = foldingParams.Attribute(hdm + "CIP3FoldSheetIn_1")?.Value;
        var in2Value = foldingParams.Attribute(hdm + "CIP3FoldSheetIn_2")?.Value;
        if (!decimal.TryParse(in1Value, out var in1) || !decimal.TryParse(in2Value, out var in2))
        {
            return false;
        }

        var assemblyIds = foldingParams.Attribute("AssemblyIDs")?.Value;
        if (string.IsNullOrWhiteSpace(assemblyIds))
        {
            return false;
        }

        var blockNames = foldingParams
            .DescendantsAndSelf(ns + "FoldingParams")
            .Select(element => element.Attribute("BlockName")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (blockNames.Count == 0)
        {
            return false;
        }

        var (signatureName, sheetName) = GetPartNames(foldingParams);
        if (blockNames.Count == 1 && combiningOutputBlocks.TryGetValue(blockNames[0], out var outputBlocks))
        {
            var expanded = ExpandOutputBlockName(outputBlocks, signatureName, sheetName);
            if (expanded is null)
            {
                return false;
            }

            blockNames = expanded.ToList();
        }

        if (blockNames.Count == 0)
        {
            return false;
        }

        var components = root.Descendants(ns + "Component")
            .Where(element =>
                string.Equals(element.Attribute("AssemblyIDs")?.Value, assemblyIds, StringComparison.OrdinalIgnoreCase) &&
                (element.Attribute("ComponentType")?.Value ?? string.Empty)
                    .Contains("Block", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var matchingComponents = components
            .Where(component => ComponentMatchesParts(component, ns, signatureName, sheetName, blockNames))
            .ToList();
        if (matchingComponents.Count == 0)
        {
            return false;
        }

        if (matchingComponents.Count == 1)
        {
            return !TryMatchFoldDimensions(matchingComponents[0], hdm, in1, in2);
        }

        if (matchingComponents.Any(component => !HasFoldDimensions(component, hdm)))
        {
            return false;
        }

        foreach (var component in matchingComponents)
        {
            if (!TryMatchFoldDimensions(component, hdm, in1, in2))
            {
                return true;
            }
        }

        return false;
    }

    private static (string? signatureName, string? sheetName) GetPartNames(XElement element)
    {
        string? signatureName = null;
        string? sheetName = null;
        foreach (var ancestor in element.AncestorsAndSelf())
        {
            signatureName ??= ancestor.Attribute("SignatureName")?.Value;
            sheetName ??= ancestor.Attribute("SheetName")?.Value;
            if (!string.IsNullOrWhiteSpace(signatureName) && !string.IsNullOrWhiteSpace(sheetName))
            {
                break;
            }
        }

        return (signatureName, sheetName);
    }

    private static bool ComponentMatchesParts(
        XElement component,
        XNamespace ns,
        string? signatureName,
        string? sheetName,
        IReadOnlyCollection<string> blockNames)
    {
        var partComponents = component.DescendantsAndSelf(ns + "Component").ToList();
        if (!string.IsNullOrWhiteSpace(signatureName) &&
            !partComponents.Any(part => string.Equals(part.Attribute("SignatureName")?.Value, signatureName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(sheetName) &&
            !partComponents.Any(part => string.Equals(part.Attribute("SheetName")?.Value, sheetName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return partComponents.Any(part =>
            blockNames.Contains(part.Attribute("BlockName")?.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));
    }

    private static bool TryMatchFoldDimensions(XElement component, XNamespace hdm, decimal in1, decimal in2)
    {
        var openedValue = component.Attribute(hdm + "OpenedFoldingSheetDimensions")?.Value;
        var closedValue = component.Attribute(hdm + "ClosedFoldingSheetDimensions")?.Value;
        if (TryParseSize(openedValue, out var opened) && PairMatches(opened, in1, in2))
        {
            return true;
        }

        if (TryParseSize(closedValue, out var closed) && PairMatches(closed, in1, in2))
        {
            return true;
        }

        return false;
    }

    private static bool HasFoldDimensions(XElement component, XNamespace hdm)
    {
        var openedValue = component.Attribute(hdm + "OpenedFoldingSheetDimensions")?.Value;
        if (TryParseSize(openedValue, out _))
        {
            return true;
        }

        var closedValue = component.Attribute(hdm + "ClosedFoldingSheetDimensions")?.Value;
        return TryParseSize(closedValue, out _);
    }

    private static bool PairMatches((decimal width, decimal height) size, decimal in1, decimal in2)
    {
        if (Math.Abs(size.width - in1) <= 0.6m && Math.Abs(size.height - in2) <= 0.6m)
        {
            return true;
        }

        return Math.Abs(size.width - in2) <= 0.6m && Math.Abs(size.height - in1) <= 0.6m;
    }

    private static bool LooksLikeFoldScheme(string token)
    {
        if (token.Length < 5)
        {
            return false;
        }

        if (token[0] != 'F')
        {
            return false;
        }

        var dashIndex = token.IndexOf('-');
        if (dashIndex <= 1 || dashIndex >= token.Length - 1)
        {
            return false;
        }

        return char.IsDigit(token[1]);
    }

    private static bool HasHdmNamespace(XElement root, XNamespace hdm)
    {
        if (string.IsNullOrWhiteSpace(hdm.NamespaceName))
        {
            return false;
        }

        return root.Attributes()
            .Any(attribute =>
                attribute.IsNamespaceDeclaration &&
                string.Equals(attribute.Value, hdm.NamespaceName, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildWorkStyleSummary(XElement root, XNamespace ns)
    {
        var workStyles = root
            .Descendants(ns + "ConventionalPrintingParams")
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (workStyles.Count == 0)
        {
            return "workStyles=unknown";
        }

        return $"workStyles={string.Join(",", workStyles)}";
    }

    private static string BuildAssemblyIdSummary(XElement root, XNamespace ns)
    {
        var tokens = root
            .Descendants(ns + "ContentObject")
            .Select(element => element.Attribute("AssemblyIDs")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        var foldToken = tokens.FirstOrDefault(LooksLikeFoldScheme);
        if (!string.IsNullOrWhiteSpace(foldToken))
        {
            return $"assemblyIds={foldToken}";
        }

        if (tokens.Count > 0)
        {
            return "assemblyIds=present";
        }

        return "assemblyIds=none";
    }

    private static string BuildFoldingHint(XElement root, XNamespace ns)
    {
        var foldingParams = root.Descendants(ns + "FoldingParams").ToList();
        if (foldingParams.Count == 0)
        {
            return "no FoldingParams found; folding metadata may be omitted for non-folded jobs";
        }

        var hints = new List<string>();
        if (foldingParams.Any(element =>
                string.Equals(element.Attribute("NoOp")?.Value, "true", StringComparison.OrdinalIgnoreCase)))
        {
            hints.Add("FoldingParams@NoOp=true");
        }

        if (!foldingParams.Any(element => element.Elements(ns + "Fold").Any()))
        {
            hints.Add("no Fold elements");
        }

        if (!foldingParams.Any(element => !string.IsNullOrWhiteSpace(element.Attribute("FoldCatalog")?.Value)))
        {
            hints.Add("no FoldCatalog");
        }

        return hints.Count == 0 ? "folding hints present" : string.Join("; ", hints);
    }

    private static string BuildFoldSheetPriorityHint(XElement root, XNamespace ns, XNamespace hdm)
    {
        var topLevelFolds = root
            .Descendants(ns + "FoldingParams")
            .Where(element => IsTopLevelFoldingParams(element, ns))
            .ToList();
        foreach (var foldingParams in topLevelFolds)
        {
            var foldCatalog = foldingParams.Attribute("FoldCatalog")?.Value;
            if (string.IsNullOrWhiteSpace(foldCatalog) ||
                string.Equals(foldCatalog, "unnamed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var foldIn1 = foldingParams.Attribute(hdm + "CIP3FoldSheetIn_1")?.Value;
            var foldIn2 = foldingParams.Attribute(hdm + "CIP3FoldSheetIn_2")?.Value;
            if ((string.IsNullOrWhiteSpace(foldIn1) && !string.IsNullOrWhiteSpace(foldIn2)) ||
                (!string.IsNullOrWhiteSpace(foldIn1) && string.IsNullOrWhiteSpace(foldIn2)))
            {
                return "priority=low; observed cases missing one dimension with FoldCatalog present";
            }
        }

        return string.Empty;
    }

    private static string BuildSignaJobHint(XElement layout, XNamespace hdm)
    {
        var context = layout.Element(hdm + "SignaGenContext");
        var build = context?.Attribute("Build")?.Value;
        if (!string.IsNullOrWhiteSpace(build))
        {
            var numericPart = build.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            if (int.TryParse(numericPart.Split('.')[0], out var buildNumber) && buildNumber < 10000)
            {
                return $"priority=low; older Signa build ({build}) may omit SignaJob";
            }
        }

        return string.Empty;
    }

    private static string BuildContentAssemblyHint(XElement root, XNamespace ns, XNamespace hdm)
    {
        var contentObjects = root.Descendants(ns + "ContentObject").ToList();
        if (contentObjects.Count == 0)
        {
            return "priority=low; no ContentObject elements";
        }

        var hasContentAssembly = contentObjects.Any(element =>
            element.Attribute(hdm + "AssemblyFB") is not null ||
            element.Attribute(hdm + "AssemblyIDs") is not null);
        if (hasContentAssembly)
        {
            return string.Empty;
        }

        var workStyles = root.Descendants(ns + "ConventionalPrintingParams")
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (workStyles.Count == 1 &&
            string.Equals(workStyles[0], "Simplex", StringComparison.OrdinalIgnoreCase))
        {
            return "priority=low; Simplex work style often omits AssemblyIDs on ContentObject";
        }

        var hasOtherAssembly = root.Descendants()
            .Any(element =>
                element.Attribute("AssemblyIDs") is not null ||
                element.Attribute(hdm + "AssemblyIDs") is not null);
        if (hasOtherAssembly)
        {
            return "priority=low; AssemblyIDs present on non-ContentObject nodes";
        }

        return "priority=low; no AssemblyIDs found outside ContentObject";
    }

    private static string BuildLeadingEdgeHint(XElement layout, XNamespace ns)
    {
        var root = layout.Document?.Root;
        if (root is null)
        {
            return string.Empty;
        }

        var plateMedia = root.Descendants(ns + "Media")
            .Where(element => string.Equals(element.Attribute("MediaType")?.Value, "Plate", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (plateMedia.Count == 0)
        {
            return string.Empty;
        }

        if (plateMedia.Any(element => string.IsNullOrWhiteSpace(element.Attribute("Dimension")?.Value)))
        {
            return "priority=low; plate Dimension missing; LeadingEdge often equals plate height when present";
        }

        if (plateMedia.Count == 1)
        {
            return "priority=low; single Plate Media without LeadingEdge; LeadingEdge usually equals Dimension height";
        }

        return string.Empty;
    }

    private static string CombineHints(string primary, string secondary)
    {
        if (string.IsNullOrWhiteSpace(primary))
        {
            return secondary;
        }

        if (string.IsNullOrWhiteSpace(secondary))
        {
            return primary;
        }

        return $"{primary}; {secondary}";
    }

    private static string BuildBlockDimensionHint(XElement root, XNamespace ns)
    {
        var blockCount = root
            .Descendants(ns + "Component")
            .Count(element =>
                (element.Attribute("ComponentType")?.Value ?? string.Empty)
                .Contains("Block", StringComparison.OrdinalIgnoreCase));
        var foldingParamsCount = root.Descendants(ns + "FoldingParams").Count();

        return $"componentBlocks={blockCount}; foldingParams={foldingParamsCount}";
    }

    private static string BuildCombiningHint(IEnumerable<XElement> combiningParams)
    {
        var paramsList = combiningParams.ToList();
        if (paramsList.Count == 0)
        {
            return "no CombiningParams present";
        }

        var withChildren = paramsList.Count(element => element.Elements().Any());
        return $"combiningParams={paramsList.Count}; withChildren={withChildren}";
    }

    private static bool HasOutputBlockName(XElement element, XNamespace hdm)
    {
        return element
            .DescendantsAndSelf(hdm + "CombiningParams")
            .Any(node => node.Attribute(hdm + "OutputBlockName") is not null);
    }

    private static void RequireRunListUsage(
        JdfDocument document,
        List<ValidationIssue> issues,
        string processUsage,
        ValidationSeverity severity)
    {
        var refId = document.GetRunListRef(processUsage);
        if (string.IsNullOrWhiteSpace(refId))
        {
            var code = $"RUNLIST_{processUsage.ToUpperInvariant()}_MISSING";
            AddIssue(
                issues,
                severity,
                code,
                $"Missing RunListLink with ProcessUsage=\"{processUsage}\".");
            return;
        }

        if (document.FindRunListById(refId) is null)
        {
            var code = $"RUNLIST_{processUsage.ToUpperInvariant()}_REF_MISSING";
            AddIssue(
                issues,
                severity,
                code,
                $"RunListLink ProcessUsage=\"{processUsage}\" references missing RunList ID '{refId}'.");
        }
    }

    private static void AddIssue(
        List<ValidationIssue> issues,
        ValidationSeverity severity,
        string code,
        string message,
        string? context = null)
    {
        issues.Add(new ValidationIssue(severity, code, message, context));
    }

    private static bool IsNumericList(string? value, int expectedCount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != expectedCount)
        {
            return false;
        }

        foreach (var token in tokens)
        {
            if (!decimal.TryParse(token, out _))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPageOrientation(string? value)
    {
        if (!int.TryParse(value, out var orientation))
        {
            return false;
        }

        return orientation == 0 || orientation == 90 || orientation == 180 || orientation == 270;
    }

    private static bool PageOrientationMismatch(XElement contentObject, XNamespace hdm)
    {
        var orientationValue = contentObject.Attribute(hdm + "PageOrientation")?.Value;
        if (!int.TryParse(orientationValue, out var orientation))
        {
            return false;
        }

        var ctmValue = contentObject.Attribute("TrimCTM")?.Value
            ?? contentObject.Attribute("CTM")?.Value;
        if (!TryParseMatrix(ctmValue, out var matrix))
        {
            return false;
        }

        if (!TryGetOrientationFromMatrix(matrix, out var derived))
        {
            return false;
        }

        return orientation != derived;
    }

    private static int CountPerfectingBackGeometryMismatches(XElement root, XNamespace ns, XNamespace hdm)
    {
        var usesPerfecting = root.Descendants(ns + "ConventionalPrintingParams")
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .Any(value => string.Equals(value, "Perfecting", StringComparison.OrdinalIgnoreCase));
        if (!usesPerfecting)
        {
            return 0;
        }

        if (IsMontageLayout(root, ns))
        {
            return 0;
        }

        var backLayouts = root.Descendants(ns + "Layout")
            .Where(element => string.Equals(element.Attribute("Side")?.Value, "Back", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (backLayouts.Count == 0)
        {
            return 0;
        }

        var mismatchCount = 0;
        foreach (var backLayout in backLayouts)
        {
            var sheetWorkStyle = ResolveSheetWorkStyle(backLayout, root, ns);
            if (!string.Equals(sheetWorkStyle, "Perfecting", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasSymmetricSignatureOrientation(backLayout, root, ns, hdm))
            {
                continue;
            }

            var contentObjects = backLayout.Elements(ns + "ContentObject").ToList();
            if (contentObjects.Count == 0)
            {
                continue;
            }

            var hasMirrorSignal = contentObjects.Any(contentObject =>
            {
                var orientationValue = contentObject.Attribute(hdm + "PageOrientation")?.Value;
                var orientationIs180 = int.TryParse(orientationValue, out var orientation) && orientation == 180;

                var ctmValue = contentObject.Attribute("TrimCTM")?.Value
                    ?? contentObject.Attribute("CTM")?.Value;
                var ctmIsMirror = TryParseMatrix(ctmValue, out var matrix) && IsMirrorCtm(matrix);

                return orientationIs180 || ctmIsMirror;
            });

            if (!hasMirrorSignal)
            {
                continue;
            }

            foreach (var contentObject in contentObjects)
            {
                var orientationValue = contentObject.Attribute(hdm + "PageOrientation")?.Value;
                var orientationOk = int.TryParse(orientationValue, out var orientation) && orientation == 180;

                var ctmValue = contentObject.Attribute("TrimCTM")?.Value
                    ?? contentObject.Attribute("CTM")?.Value;
                var ctmOk = TryParseMatrix(ctmValue, out var matrix) && IsMirrorCtm(matrix);

                if (!orientationOk || !ctmOk)
                {
                    mismatchCount += 1;
                }
            }
        }

        return mismatchCount;
    }

    private static bool HasSymmetricSignatureOrientation(
        XElement layout,
        XElement root,
        XNamespace ns,
        XNamespace hdm)
    {
        var (signatureName, sheetName) = GetPartNames(layout);
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return false;
        }

        var strippingParams = root.Descendants(ns + "StrippingParams")
            .Where(element => element.Elements(ns + "BinderySignatureRef").Any())
            .ToList();

        foreach (var strippingParam in strippingParams)
        {
            if (!MatchesPart(strippingParam, signatureName, sheetName))
            {
                continue;
            }

            foreach (var binderyRef in strippingParam.Elements(ns + "BinderySignatureRef"))
            {
                var refId = binderyRef.Attribute("rRef")?.Value;
                if (string.IsNullOrWhiteSpace(refId))
                {
                    continue;
                }

                var binderySignature = root.Descendants(ns + "BinderySignature")
                    .FirstOrDefault(element => string.Equals(element.Attribute("ID")?.Value, refId, StringComparison.OrdinalIgnoreCase));
                if (binderySignature is null)
                {
                    continue;
                }

                var signatureCell = binderySignature.Element(ns + "SignatureCell");
                var front = signatureCell?.Attribute(hdm + "FrontSchemePageOrientation")?.Value;
                var back = signatureCell?.Attribute(hdm + "BackSchemePageOrientation")?.Value;
                if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
                {
                    continue;
                }

                if (string.Equals(NormalizeOrientationList(front), NormalizeOrientationList(back), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesPart(XElement element, string? signatureName, string? sheetName)
    {
        var (elementSignature, elementSheet) = GetPartNames(element);

        if (!string.IsNullOrWhiteSpace(signatureName) &&
            !string.Equals(signatureName, elementSignature, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(sheetName) &&
            !string.Equals(sheetName, elementSheet, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeOrientationList(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts);
    }

    private static string? ResolveSheetWorkStyle(XElement layout, XElement root, XNamespace ns)
    {
        var sheetLayout = layout
            .AncestorsAndSelf(ns + "Layout")
            .FirstOrDefault(element => element.Attribute("SheetName") is not null);
        if (sheetLayout is null)
        {
            return null;
        }

        var sourceWorkStyle = sheetLayout.Attribute("SourceWorkStyle")?.Value;
        if (!string.IsNullOrWhiteSpace(sourceWorkStyle))
        {
            return sourceWorkStyle;
        }

        var signatureName = sheetLayout.Attribute("SignatureName")?.Value;
        var sheetName = sheetLayout.Attribute("SheetName")?.Value;
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return null;
        }

        var workStyle = root.Descendants(ns + "ConventionalPrintingParams")
            .Where(element =>
                string.Equals(element.Attribute("SheetName")?.Value, sheetName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(signatureName) ||
                 string.Equals(element.Attribute("SignatureName")?.Value, signatureName, StringComparison.OrdinalIgnoreCase)))
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return workStyle;
    }

    private static bool IsMirrorCtm(double[] matrix)
    {
        const double epsilon = 0.0001;
        return Math.Abs(matrix[0] + 1) < epsilon
            && Math.Abs(matrix[1]) < epsilon
            && Math.Abs(matrix[2]) < epsilon
            && Math.Abs(matrix[3] + 1) < epsilon;
    }

    private static bool FinalPageBoxMismatch(XElement contentObject, XNamespace hdm)
    {
        var finalPageBox = contentObject.Attribute(hdm + "FinalPageBox")?.Value;
        if (!TryParseBox(finalPageBox, out var box))
        {
            return false;
        }

        var trimSizeValue = contentObject.Attribute("TrimSize")?.Value;
        if (!TryParseSize(trimSizeValue, out var size))
        {
            return false;
        }

        var trimCtmValue = contentObject.Attribute("TrimCTM")?.Value;
        if (!TryParseMatrix(trimCtmValue, out var matrix))
        {
            return false;
        }

        var orientationValue = contentObject.Attribute(hdm + "PageOrientation")?.Value;
        if (!int.TryParse(orientationValue, out var orientation))
        {
            return false;
        }

        var scale1 = Math.Sqrt((matrix[0] * matrix[0]) + (matrix[1] * matrix[1]));
        var scale2 = Math.Sqrt((matrix[2] * matrix[2]) + (matrix[3] * matrix[3]));
        if (Math.Abs(scale1 - scale2) > 0.001)
        {
            return false;
        }

        var width = size.width;
        var height = size.height;
        if (orientation == 90 || orientation == 270)
        {
            (width, height) = (height, width);
        }

        var derived = DeriveBoxFromMatrix(matrix, width, height);
        return !BoxEquals(box, derived, 2.0m);
    }

    private static bool FinalPageBoxClipMismatch(XElement contentObject, XNamespace hdm)
    {
        var finalPageBox = contentObject.Attribute(hdm + "FinalPageBox")?.Value;
        if (!TryParseBox(finalPageBox, out var box))
        {
            return false;
        }

        if (contentObject.Attribute("TrimCTM") is not null)
        {
            return false;
        }

        var clipBoxValue = contentObject.Attribute("ClipBox")?.Value;
        if (!TryParseBox(clipBoxValue, out var clipBox))
        {
            return false;
        }

        var trimSizeValue = contentObject.Attribute("TrimSize")?.Value;
        if (!TryParseSize(trimSizeValue, out var size))
        {
            return false;
        }

        var clipWidth = clipBox[2] - clipBox[0];
        var clipHeight = clipBox[3] - clipBox[1];
        if (clipWidth <= 0 || clipHeight <= 0)
        {
            return false;
        }

        if (size.width > clipWidth || size.height > clipHeight)
        {
            return false;
        }

        var insetX = (clipWidth - size.width) / 2m;
        var insetY = (clipHeight - size.height) / 2m;
        var derived = new[]
        {
            clipBox[0] + insetX,
            clipBox[1] + insetY,
            clipBox[2] - insetX,
            clipBox[3] - insetY
        };

        return !BoxEquals(box, derived, 1.0m);
    }

    private static bool ClipBoxOutOfBounds(XElement contentObject, XNamespace hdm)
    {
        var clipBoxValue = contentObject.Attribute("ClipBox")?.Value;
        if (!TryParseBox(clipBoxValue, out var clipBox))
        {
            return false;
        }

        var trimSizeValue = contentObject.Attribute("TrimSize")?.Value;
        if (!TryParseSize(trimSizeValue, out var size))
        {
            return false;
        }

        var trimCtmValue = contentObject.Attribute("TrimCTM")?.Value;
        if (!TryParseMatrix(trimCtmValue, out var matrix))
        {
            return false;
        }

        var scale1 = Math.Sqrt((matrix[0] * matrix[0]) + (matrix[1] * matrix[1]));
        var scale2 = Math.Sqrt((matrix[2] * matrix[2]) + (matrix[3] * matrix[3]));
        if (Math.Abs(scale1 - scale2) > 0.001)
        {
            return false;
        }

        var orientationValue = contentObject.Attribute(hdm + "PageOrientation")?.Value;
        if (!int.TryParse(orientationValue, out var orientation))
        {
            orientation = 0;
        }

        var width = size.width;
        var height = size.height;
        if (orientation == 90 || orientation == 270)
        {
            (width, height) = (height, width);
        }

        var derived = DeriveBoxFromMatrix(matrix, width, height);
        var clipWidth = clipBox[2] - clipBox[0];
        var clipHeight = clipBox[3] - clipBox[1];
        var derivedWidth = derived[2] - derived[0];
        var derivedHeight = derived[3] - derived[1];

        if (Math.Abs(clipWidth - derivedWidth) > 1.0m || Math.Abs(clipHeight - derivedHeight) > 1.0m)
        {
            return false;
        }

        return !BoxContains(derived, clipBox, 0.6m);
    }

    private static bool PaperRectMismatch(XElement sideLayout, XNamespace hdm)
    {
        var paperRectValue = sideLayout.Attribute(hdm + "PaperRect")?.Value;
        if (!TryParseBox(paperRectValue, out var paperRect))
        {
            return false;
        }

        var root = sideLayout.Document?.Root;
        if (root is null)
        {
            return false;
        }

        var ns = sideLayout.Name.Namespace;
        var transferRef = sideLayout
            .Ancestors(ns + "Layout")
            .SelectMany(layout => layout.Elements(ns + "TransferCurvePoolRef"))
            .FirstOrDefault()
            ?? root.Descendants(ns + "TransferCurvePoolRef").FirstOrDefault();
        if (transferRef is null)
        {
            return false;
        }

        var refId = transferRef.Attribute("rRef")?.Value;
        if (string.IsNullOrWhiteSpace(refId))
        {
            return false;
        }

        var transferPool = root.Descendants(ns + "TransferCurvePool")
            .FirstOrDefault(element => string.Equals(element.Attribute("ID")?.Value, refId, StringComparison.OrdinalIgnoreCase));
        if (transferPool is null)
        {
            return false;
        }

        var paperDimension = FindPaperMediaDimension(root, sideLayout.Name.Namespace, sideLayout);
        if (paperDimension is null)
        {
            return false;
        }

        var paperSets = transferPool.Descendants(ns + "TransferCurveSet")
            .Where(element => string.Equals(element.Attribute("Name")?.Value, "Paper", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (paperSets.Count == 0)
        {
            return false;
        }

        foreach (var paperSet in paperSets)
        {
            var ctmValue = paperSet.Attribute("CTM")?.Value;
            if (!TryParseMatrix(ctmValue, out var matrix))
            {
                continue;
            }

            var derived = DerivePaperRectFromMedia(paperDimension.Value, matrix);
            if (BoxEquals(paperRect, derived, 0.6m))
            {
                return false;
            }
        }

        return true;
    }

    private static (decimal width, decimal height)? FindPaperMediaDimension(
        XElement root,
        XNamespace ns,
        XElement sideLayout)
    {
        var sheetName = sideLayout.Parent?.Attribute("SheetName")?.Value;
        var signatureName = sideLayout.Parent?.Parent?.Attribute("SignatureName")?.Value;

        var media = root.Descendants(ns + "Media")
            .Where(element => string.Equals(element.Attribute("MediaType")?.Value, "Paper", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (media.Count == 0)
        {
            return null;
        }

        XElement? match = null;
        if (!string.IsNullOrWhiteSpace(sheetName) || !string.IsNullOrWhiteSpace(signatureName))
        {
            foreach (var candidate in media)
            {
                var partitionMatch = candidate
                    .Descendants(ns + "Media")
                    .Any(part =>
                        (string.IsNullOrWhiteSpace(sheetName) || string.Equals(part.Attribute("SheetName")?.Value, sheetName, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(signatureName) || string.Equals(part.Attribute("SignatureName")?.Value, signatureName, StringComparison.OrdinalIgnoreCase)));
                if (partitionMatch)
                {
                    match = candidate;
                    break;
                }
            }
        }

        match ??= media.Count == 1 ? media[0] : null;
        if (match is null)
        {
            return null;
        }

        var dimensionValue = match.Attribute("Dimension")?.Value;
        if (string.IsNullOrWhiteSpace(dimensionValue))
        {
            return null;
        }

        if (!TryParseSize(dimensionValue, out var size))
        {
            return null;
        }

        return (size.width, size.height);
    }

    private static decimal[] DerivePaperRectFromMedia((decimal width, decimal height) size, double[] matrix)
    {
        var translateX = (decimal)matrix[4];
        var translateY = (decimal)matrix[5];
        var minX = -translateX;
        var minY = -translateY;
        var maxX = minX + size.width;
        var maxY = minY + size.height;

        return new[]
        {
            minX,
            minY,
            maxX,
            maxY
        };
    }

    private static bool SurfaceContentsBoxMismatch(XElement sheetLayout)
    {
        var surfaceValue = sheetLayout.Attribute("SurfaceContentsBox")?.Value;
        if (!TryParseBox(surfaceValue, out var surfaceBox))
        {
            return false;
        }

        var root = sheetLayout.Document?.Root;
        if (root is null)
        {
            return false;
        }

        var ns = sheetLayout.Name.Namespace;
        var transferRef = sheetLayout
            .Elements(ns + "TransferCurvePoolRef")
            .FirstOrDefault()
            ?? root.Descendants(ns + "TransferCurvePoolRef").FirstOrDefault();
        if (transferRef is null)
        {
            return false;
        }

        var refId = transferRef.Attribute("rRef")?.Value;
        if (string.IsNullOrWhiteSpace(refId))
        {
            return false;
        }

        var transferPool = root.Descendants(ns + "TransferCurvePool")
            .FirstOrDefault(element => string.Equals(element.Attribute("ID")?.Value, refId, StringComparison.OrdinalIgnoreCase));
        if (transferPool is null)
        {
            return false;
        }

        var plateDimension = FindPlateMediaDimension(root, ns, sheetLayout);
        if (plateDimension is null)
        {
            return false;
        }

        var plateSets = transferPool.Descendants(ns + "TransferCurveSet")
            .Where(element => string.Equals(element.Attribute("Name")?.Value, "Plate", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (plateSets.Count == 0)
        {
            return false;
        }

        foreach (var plateSet in plateSets)
        {
            var ctmValue = plateSet.Attribute("CTM")?.Value;
            if (!TryParseMatrix(ctmValue, out var matrix))
            {
                continue;
            }

            var derived = DerivePaperRectFromMedia(plateDimension.Value, matrix);
            if (BoxEquals(surfaceBox, derived, 0.6m))
            {
                return false;
            }
        }

        return true;
    }

    private static (decimal width, decimal height)? FindPlateMediaDimension(
        XElement root,
        XNamespace ns,
        XElement sheetLayout)
    {
        var sheetName = sheetLayout.Attribute("SheetName")?.Value;
        var signatureName = sheetLayout.Parent?.Attribute("SignatureName")?.Value;

        var media = root.Descendants(ns + "Media")
            .Where(element => string.Equals(element.Attribute("MediaType")?.Value, "Plate", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (media.Count == 0)
        {
            return null;
        }

        XElement? match = null;
        if (!string.IsNullOrWhiteSpace(sheetName) || !string.IsNullOrWhiteSpace(signatureName))
        {
            foreach (var candidate in media)
            {
                var partitionMatch = candidate
                    .Descendants(ns + "Media")
                    .Any(part =>
                        (string.IsNullOrWhiteSpace(sheetName) || string.Equals(part.Attribute("SheetName")?.Value, sheetName, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(signatureName) || string.Equals(part.Attribute("SignatureName")?.Value, signatureName, StringComparison.OrdinalIgnoreCase)));
                if (partitionMatch)
                {
                    match = candidate;
                    break;
                }
            }
        }

        match ??= media.Count == 1 ? media[0] : null;
        if (match is null)
        {
            return null;
        }

        var dimensionValue = match.Attribute("Dimension")?.Value;
        if (string.IsNullOrWhiteSpace(dimensionValue))
        {
            return null;
        }

        if (!TryParseSize(dimensionValue, out var size))
        {
            return null;
        }

        return (size.width, size.height);
    }

    private static decimal[]? GetPaperRectForCutBlock(XElement cutBlock, XNamespace hdm, XNamespace ns)
    {
        var root = cutBlock.Document?.Root;
        if (root is null)
        {
            return null;
        }

        var (signatureName, sheetName) = GetCutBlockPartNames(cutBlock, ns);
        var layouts = root.Descendants(ns + "Layout")
            .Where(element => element.Attribute(hdm + "PaperRect") is not null)
            .ToList();

        IEnumerable<XElement> candidates = layouts;
        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            candidates = candidates.Where(element =>
                string.Equals(element.Attribute("SheetName")?.Value, sheetName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(signatureName))
        {
            candidates = candidates.Where(element =>
                string.Equals(element.Parent?.Attribute("SignatureName")?.Value, signatureName, StringComparison.OrdinalIgnoreCase));
        }

        decimal[]? paperRect = null;
        foreach (var layout in candidates)
        {
            var paperRectValue = layout.Attribute(hdm + "PaperRect")?.Value;
            if (!TryParseBox(paperRectValue, out var parsed))
            {
                continue;
            }

            if (paperRect is null)
            {
                paperRect = parsed;
                continue;
            }

            if (!BoxEquals(paperRect, parsed, 0.6m))
            {
                return null;
            }
        }

        return paperRect;
    }

    private static (string? signatureName, string? sheetName) GetCutBlockPartNames(XElement cutBlock, XNamespace ns)
    {
        string? signatureName = null;
        string? sheetName = null;
        foreach (var ancestor in cutBlock.Ancestors(ns + "CuttingParams"))
        {
            signatureName ??= ancestor.Attribute("SignatureName")?.Value;
            sheetName ??= ancestor.Attribute("SheetName")?.Value;
        }

        return (signatureName, sheetName);
    }

    private static Dictionary<string, List<CombiningOutputBlockEntry>> BuildCombiningOutputBlockMap(XElement root, XNamespace hdm)
    {
        var map = new Dictionary<string, List<CombiningOutputBlockEntry>>(StringComparer.OrdinalIgnoreCase);
        var topLevel = root.Descendants(hdm + "CombiningParams")
            .Where(element => element.Parent?.Name != hdm + "CombiningParams")
            .ToList();
        foreach (var combining in topLevel)
        {
            var outputName = combining.Attribute(hdm + "OutputBlockName")?.Value;
            if (string.IsNullOrWhiteSpace(outputName))
            {
                continue;
            }

            var entries = combining
                .Descendants(hdm + "CombiningParams")
                .Where(element => !string.IsNullOrWhiteSpace(element.Attribute("BlockName")?.Value))
                .Select(element =>
                {
                    var blockName = element.Attribute("BlockName")?.Value ?? string.Empty;
                    var (signature, sheet) = GetCombiningPartNames(element, hdm);
                    return new { blockName, signature, sheet };
                })
                .ToList();

            if (entries.Count == 0)
            {
                continue;
            }

            if (!map.TryGetValue(outputName, out var blocks))
            {
                blocks = new List<CombiningOutputBlockEntry>();
                map[outputName] = blocks;
            }

            foreach (var entry in entries)
            {
                var existing = blocks.FirstOrDefault(item =>
                    string.Equals(item.SignatureName, entry.signature, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.SheetName, entry.sheet, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    existing = new CombiningOutputBlockEntry(entry.signature, entry.sheet);
                    blocks.Add(existing);
                }

                existing.BlockNames.Add(entry.blockName);
            }
        }

        return map;
    }

    private static IReadOnlyCollection<string>? ExpandOutputBlockName(
        IReadOnlyCollection<CombiningOutputBlockEntry> entries,
        string? signatureName,
        string? sheetName)
    {
        var candidates = entries
            .Where(entry =>
                (string.IsNullOrWhiteSpace(entry.SignatureName) ||
                 string.IsNullOrWhiteSpace(signatureName) ||
                 string.Equals(entry.SignatureName, signatureName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(entry.SheetName) ||
                 string.IsNullOrWhiteSpace(sheetName) ||
                 string.Equals(entry.SheetName, sheetName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].BlockNames;
        }

        var first = candidates[0].BlockNames;
        if (candidates.All(entry => entry.BlockNames.SetEquals(first)))
        {
            return first;
        }

        return null;
    }

    private static (string? signatureName, string? sheetName) GetCombiningPartNames(XElement element, XNamespace hdm)
    {
        string? signatureName = null;
        string? sheetName = null;
        foreach (var ancestor in element.AncestorsAndSelf(hdm + "CombiningParams"))
        {
            signatureName ??= ancestor.Attribute("SignatureName")?.Value;
            sheetName ??= ancestor.Attribute("SheetName")?.Value;
        }

        return (signatureName, sheetName);
    }

    private sealed class CombiningOutputBlockEntry
    {
        public CombiningOutputBlockEntry(string? signatureName, string? sheetName)
        {
            SignatureName = signatureName;
            SheetName = sheetName;
        }

        public string? SignatureName { get; }
        public string? SheetName { get; }
        public HashSet<string> BlockNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static bool LeadingEdgeMismatch(XElement plateMedia, XNamespace hdm)
    {
        var leadingEdgeValue = plateMedia.Attribute(hdm + "LeadingEdge")?.Value;
        if (string.IsNullOrWhiteSpace(leadingEdgeValue))
        {
            return false;
        }

        var dimensionValue = plateMedia.Attribute("Dimension")?.Value;
        if (!TryParseSize(dimensionValue, out var size))
        {
            return false;
        }

        if (!decimal.TryParse(leadingEdgeValue, out var leadingEdge))
        {
            return false;
        }

        return Math.Abs(leadingEdge - size.height) > 0.6m;
    }

    private static bool BlockTrfMismatch(XElement cutBlock, decimal[] paperRect, XNamespace hdm)
    {
        var blockTrfValue = cutBlock.Attribute("BlockTrf")?.Value;
        var cip3Value = cutBlock.Attribute(hdm + "CIP3BlockTrf")?.Value;
        if (!TryParseMatrix(blockTrfValue, out var blockTrf))
        {
            return false;
        }

        if (!TryParseMatrix(cip3Value, out var cip3Trf))
        {
            return false;
        }

        var expected = new[]
        {
            blockTrf[0],
            blockTrf[1],
            blockTrf[2],
            blockTrf[3],
            blockTrf[4] + (double)paperRect[0],
            blockTrf[5] + (double)paperRect[1]
        };

        for (var i = 0; i < expected.Length; i++)
        {
            if (Math.Abs(expected[i] - cip3Trf[i]) > 0.6)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseMatrix(string? value, out double[] matrix)
    {
        matrix = Array.Empty<double>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 6)
        {
            return false;
        }

        var result = new double[6];
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!double.TryParse(tokens[i], out var parsed))
            {
                return false;
            }
            result[i] = parsed;
        }

        matrix = result;
        return true;
    }

    private static bool TryParseBox(string? value, out decimal[] box)
    {
        box = Array.Empty<decimal>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 4)
        {
            return false;
        }

        var result = new decimal[4];
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!decimal.TryParse(tokens[i], out var parsed))
            {
                return false;
            }
            result[i] = parsed;
        }

        box = result;
        return true;
    }

    private static bool TryParseSize(string? value, out (decimal width, decimal height) size)
    {
        size = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        if (!decimal.TryParse(tokens[0], out var width))
        {
            return false;
        }

        if (!decimal.TryParse(tokens[1], out var height))
        {
            return false;
        }

        size = (width, height);
        return true;
    }

    private static decimal[] DeriveBoxFromMatrix(double[] matrix, decimal width, decimal height)
    {
        var a = (decimal)matrix[0];
        var b = (decimal)matrix[1];
        var c = (decimal)matrix[2];
        var d = (decimal)matrix[3];
        var e = (decimal)matrix[4];
        var f = (decimal)matrix[5];

        var p1 = TransformPoint(a, b, c, d, e, f, 0m, 0m);
        var p2 = TransformPoint(a, b, c, d, e, f, width, 0m);
        var p3 = TransformPoint(a, b, c, d, e, f, 0m, height);
        var p4 = TransformPoint(a, b, c, d, e, f, width, height);

        var minX = Min(p1.x, p2.x, p3.x, p4.x);
        var minY = Min(p1.y, p2.y, p3.y, p4.y);
        var maxX = Max(p1.x, p2.x, p3.x, p4.x);
        var maxY = Max(p1.y, p2.y, p3.y, p4.y);

        return new[] { minX, minY, maxX, maxY };
    }

    private static (decimal x, decimal y) TransformPoint(
        decimal a,
        decimal b,
        decimal c,
        decimal d,
        decimal e,
        decimal f,
        decimal x,
        decimal y)
    {
        var tx = (a * x) + (c * y) + e;
        var ty = (b * x) + (d * y) + f;
        return (tx, ty);
    }

    private static bool BoxEquals(decimal[] left, decimal[] right, decimal tolerance)
    {
        if (left.Length != 4 || right.Length != 4)
        {
            return false;
        }

        for (var i = 0; i < 4; i++)
        {
            if (Math.Abs(left[i] - right[i]) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool BoxContains(decimal[] outer, decimal[] inner, decimal tolerance)
    {
        if (outer.Length != 4 || inner.Length != 4)
        {
            return false;
        }

        return inner[0] >= outer[0] - tolerance &&
               inner[1] >= outer[1] - tolerance &&
               inner[2] <= outer[2] + tolerance &&
               inner[3] <= outer[3] + tolerance;
    }

    private static decimal Min(params decimal[] values)
    {
        var min = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] < min)
            {
                min = values[i];
            }
        }

        return min;
    }

    private static decimal Max(params decimal[] values)
    {
        var max = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] > max)
            {
                max = values[i];
            }
        }

        return max;
    }

    private static bool TryGetOrientationFromMatrix(double[] matrix, out int orientation)
    {
        orientation = 0;
        if (matrix.Length != 6)
        {
            return false;
        }

        var a = matrix[0];
        var b = matrix[1];
        var c = matrix[2];
        var d = matrix[3];

        var scale = Math.Sqrt((a * a) + (b * b));
        if (scale <= 0.0001)
        {
            return false;
        }

        // Check if matrix resembles a pure rotation + uniform scale.
        var scale2 = Math.Sqrt((c * c) + (d * d));
        if (Math.Abs(scale - scale2) > 0.001)
        {
            return false;
        }

        var angle = Math.Atan2(c, d) * (180.0 / Math.PI);
        angle = NormalizeAngle(angle);

        if (!TryRoundToRightAngle(angle, out orientation))
        {
            return false;
        }

        return true;
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }

    private static bool TryRoundToRightAngle(double angle, out int orientation)
    {
        orientation = 0;
        var candidates = new[] { 0, 90, 180, 270 };
        foreach (var candidate in candidates)
        {
            if (Math.Abs(angle - candidate) <= 1.0)
            {
                orientation = candidate;
                return true;
            }
        }

        return false;
    }
}
