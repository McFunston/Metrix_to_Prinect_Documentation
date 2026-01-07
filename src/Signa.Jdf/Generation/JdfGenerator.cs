using System.Globalization;
using System.Xml.Linq;

namespace Signa.Jdf;

public static class JdfGenerator
{
    public static XDocument Generate(GeneratorOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var ns = XNamespace.Get("http://www.CIP4.org/JDFSchema_1_1");
        var hdm = XNamespace.Get("www.heidelberg.com/schema/HDM");
        var ssi = XNamespace.Get("http://www.creo.com/SSI/JDFExtensions.xsd");
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        var types = string.Join(' ', options.Types);

        var root = new XElement(ns + "JDF",
            new XAttribute("Type", "ProcessGroup"),
            new XAttribute("Types", types),
            new XAttribute("Version", options.Version),
            new XAttribute("MaxVersion", options.MaxVersion),
            new XAttribute("JobID", options.JobId),
            new XAttribute("JobPartID", options.JobPartId),
            new XAttribute("Status", options.Status),
            new XAttribute("DescriptiveName", options.DescriptiveName),
            new XAttribute(XNamespace.Xmlns + "HDM", hdm),
            new XAttribute(XNamespace.Xmlns + "SSI", ssi),
            new XAttribute(XNamespace.Xmlns + "xsi", xsi));

        var resourcePool = new XElement(ns + "ResourcePool");
        root.Add(resourcePool);

        var layout = new XElement(ns + "Layout",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", "r_layout"),
            new XAttribute("PartIDKeys", "SignatureName SheetName Side"),
            new XAttribute("Status", "Available"));

        if (options.IncludeSignaMetadata)
        {
            if (options.IncludeSignaBlob)
            {
                layout.Add(new XElement(hdm + "SignaBLOB",
                    new XAttribute("URL", options.SignaBlobUrl)));
            }
            if (options.IncludeSignaJdf)
            {
                layout.Add(new XElement(hdm + "SignaJDF",
                    new XAttribute("URL", options.SignaJdfUrl)));
            }
            if (options.IncludeSignaJob)
            {
                var signaJob = new XElement(hdm + "SignaJob");
                if (options.IncludeSignaJobParts)
                {
                    var jobParts = options.SignaJobPartNames.Count > 0
                        ? options.SignaJobPartNames
                        : options.SignatureJobParts.Count > 0
                            ? options.SignatureJobParts.Values.Distinct().ToArray()
                            : new[] { options.SignaJobPartName };
                    foreach (var jobPartName in jobParts)
                    {
                        if (string.IsNullOrWhiteSpace(jobPartName))
                        {
                            continue;
                        }
                        signaJob.Add(new XElement(hdm + "SignaJobPart",
                            new XAttribute("Name", jobPartName)));
                    }
                }
                layout.Add(signaJob);
            }

            var signaContext = new XElement(hdm + "SignaGenContext");
            if (!string.IsNullOrWhiteSpace(options.SignaProductName))
            {
                signaContext.SetAttributeValue("ProductName", options.SignaProductName);
            }
            if (!string.IsNullOrWhiteSpace(options.SignaProductMajorVersion))
            {
                signaContext.SetAttributeValue("ProductMajorVersion", options.SignaProductMajorVersion);
            }
            if (!string.IsNullOrWhiteSpace(options.SignaProductMinorVersion))
            {
                signaContext.SetAttributeValue("ProductMinorVersion", options.SignaProductMinorVersion);
            }
            layout.Add(signaContext);
        }

        var signatureDefinitions = options.Signatures.Count > 0
            ? options.Signatures
            : new[]
            {
                new SignatureDefinition(options.SignatureName,
                    options.SheetNames.Count > 0 ? options.SheetNames : new[] { options.SheetName },
                    options.WorkStyle)
            };

        if (options.IncludeTransferCurvePool)
        {
            layout.Add(new XElement(ns + "TransferCurvePoolRef",
                new XAttribute("rRef", options.TransferCurvePoolId)));
        }

        var sheetLayouts = new List<XElement>();
        foreach (var signatureDef in signatureDefinitions)
        {
            var signature = new XElement(ns + "Layout",
                new XAttribute("SignatureName", signatureDef.Name));
            var signatureWorkStyle = signatureDef.WorkStyle ?? options.WorkStyle;

            var sheets = signatureDef.Sheets.Count > 0
                ? signatureDef.Sheets
                : new[] { options.SheetName };

            foreach (var sheetName in sheets)
            {
                var sheet = new XElement(ns + "Layout",
                    new XAttribute("SheetName", sheetName),
                    new XAttribute("SourceWorkStyle", signatureWorkStyle));
                if (options.IncludePlateMedia)
                {
                    sheet.SetAttributeValue("SurfaceContentsBox", $"0 0 {options.PlateWidth} {options.PlateHeight}");
                }

                sheet.Add(CreateSide(ns, hdm, "Front", options, sheetLayouts.Count, signatureDef.Name, sheetName));
                if (options.IncludeBackSide)
                {
                    sheet.Add(CreateSide(ns, hdm, "Back", options, sheetLayouts.Count, signatureDef.Name, sheetName));
                }

                signature.Add(sheet);
                sheetLayouts.Add(sheet);
            }

            layout.Add(signature);
        }
        resourcePool.Add(layout);

        var signatureSheetPairs = signatureDefinitions
            .SelectMany(signature => signature.Sheets.Count > 0
                ? signature.Sheets.Select(sheet => (signature.Name, sheet))
                : new[] { (signature.Name, options.SheetName) })
            .ToList();

        XElement? documentRunList = null;
        if (options.IncludeDocumentRunList)
        {
            documentRunList = new XElement(ns + "RunList",
                new XAttribute("Class", "Parameter"),
                new XAttribute("ID", "r_doc"),
                new XAttribute("PartIDKeys", options.DocumentRunListPartIdKeys),
                new XAttribute("Status", options.IncludeDocumentFileSpec ? "Available" : "Unavailable"));

            if (options.IncludeDocumentFileSpec)
            {
                documentRunList.Add(BuildLayoutElement(ns, options, options.DocumentFileName));
            }

            if (options.IncludeDocumentPageMapping)
            {
                documentRunList.SetAttributeValue("LogicalPage", "0");
                var documentPageCount = options.DocumentPageCount;
                if (options.DocumentPagesPerSheet && signatureSheetPairs.Count > 1)
                {
                    documentPageCount *= signatureSheetPairs.Count;
                }
                documentRunList.SetAttributeValue("NPage", documentPageCount.ToString());
                documentRunList.SetAttributeValue("Pages", BuildPageRange(documentPageCount));

                var logicalPage = 0;
                foreach (var (signatureName, sheetName) in signatureSheetPairs)
                {
                    var docPart = new XElement(ns + "RunList",
                        new XAttribute("SignatureName", signatureName),
                        new XAttribute("SheetName", sheetName),
                        new XAttribute("Side", "Front"),
                        new XAttribute("LogicalPage", logicalPage.ToString()),
                        new XAttribute("Pages", BuildPageRangeWithOffset(logicalPage, options.DocumentPageCount)),
                        BuildLayoutElement(ns, options, options.DocumentFileName));
                    documentRunList.Add(docPart);
                    if (options.DocumentPagesPerSheet)
                    {
                        logicalPage += options.DocumentPageCount;
                    }
                }
            }
        }

        var marksRunLists = new List<XElement>();
        if (options.MarksSplitRunListPerSignature && options.IncludeMarksPartitions)
        {
            var perSheetPages = Math.Max(1, options.MarksPagesPerSide)
                                * (options.MarksIncludeBackSide ? 2 : 1);
            var totalPages = options.MarksPageCount;
            if (!options.MarksResetLogicalPagePerSignature
                && signatureSheetPairs.Count > 1
                && totalPages < perSheetPages * signatureSheetPairs.Count)
            {
                totalPages = perSheetPages * signatureSheetPairs.Count;
            }

            var logicalPageOffset = 0;
            var signatureIndex = 1;
            foreach (var signatureGroup in signatureDefinitions)
            {
                var signaturePages = perSheetPages * signatureGroup.Sheets.Count;
                var runListTotalPages = options.MarksResetLogicalPagePerSignature
                    ? signaturePages
                    : totalPages;
                var runListLogicalStart = options.MarksResetLogicalPagePerSignature
                    ? 0
                    : logicalPageOffset;

                var runList = BuildMarksRunList(ns, hdm, options, signatureGroup,
                    $"r_marks_{signatureIndex}", runListTotalPages, runListLogicalStart);
                marksRunLists.Add(runList);

                if (!options.MarksResetLogicalPagePerSignature)
                {
                    logicalPageOffset += signaturePages;
                }

                signatureIndex++;
            }
        }
        else
        {
            var marksRunList = new XElement(ns + "RunList",
                new XAttribute("Class", "Parameter"),
                new XAttribute("ID", "r_marks"),
                new XAttribute("PartIDKeys", options.MarksRunListPartIdKeys),
                new XAttribute(hdm + "OFW", options.RunListHdmOfw),
                new XAttribute("Status", "Available"),
                BuildLayoutElement(ns, options, options.MarksFileName));

            if (options.IncludeMarksPartitions)
            {
                marksRunList.SetAttributeValue("LogicalPage", "0");
                var perSheetPages = Math.Max(1, options.MarksPagesPerSide)
                                    * (options.MarksIncludeBackSide ? 2 : 1);
                var totalPages = options.MarksPageCount;
                if (!options.MarksResetLogicalPagePerSignature
                    && signatureSheetPairs.Count > 1
                    && totalPages < perSheetPages * signatureSheetPairs.Count)
                {
                    totalPages = perSheetPages * signatureSheetPairs.Count;
                }
                marksRunList.SetAttributeValue("NPage", totalPages.ToString());
                marksRunList.SetAttributeValue("Pages", BuildPageRange(totalPages));

                var logicalPage = 0;
                foreach (var signatureGroup in signatureDefinitions)
                {
                    if (options.MarksResetLogicalPagePerSignature)
                    {
                        logicalPage = 0;
                    }
                    var sigPart = new XElement(ns + "RunList",
                        new XAttribute("SignatureName", signatureGroup.Name));

                foreach (var sheetName in signatureGroup.Sheets)
                {
                    if (options.MarksResetLogicalPagePerSheet)
                    {
                        logicalPage = 0;
                    }
                    var sheetPart = new XElement(ns + "RunList",
                        new XAttribute("SheetName", sheetName));

                        var frontPages = Math.Max(1, options.MarksPagesPerSide);
                        var frontPart = new XElement(ns + "RunList",
                            new XAttribute("Side", "Front"),
                            new XAttribute("LogicalPage", logicalPage.ToString()),
                            new XAttribute("Pages", BuildPageRangeWithOffset(logicalPage, frontPages)),
                            BuildLayoutElement(ns, options, options.MarksFileName));
                        if (options.IncludeMarksSeparations)
                        {
                            AddDefaultMarkSeparations(frontPart.Element(ns + "LayoutElement"), ns, hdm);
                        }
                        sheetPart.Add(frontPart);
                        logicalPage += frontPages;

                        if (options.MarksIncludeBackSide)
                        {
                            var backPages = Math.Max(1, options.MarksPagesPerSide);
                            var backPart = new XElement(ns + "RunList",
                                new XAttribute("Side", "Back"),
                                new XAttribute("LogicalPage", logicalPage.ToString()),
                                new XAttribute("Pages", BuildPageRangeWithOffset(logicalPage, backPages)),
                                BuildLayoutElement(ns, options, options.MarksFileName));
                            if (options.IncludeMarksSeparations)
                            {
                                AddDefaultMarkSeparations(backPart.Element(ns + "LayoutElement"), ns, hdm);
                            }
                            sheetPart.Add(backPart);
                            logicalPage += backPages;
                        }

                        sigPart.Add(sheetPart);
                    }

                    marksRunList.Add(sigPart);
                }
            }
            else if (options.IncludeMarksSeparations)
            {
                AddDefaultMarkSeparations(marksRunList.Element(ns + "LayoutElement"), ns, hdm);
            }

            marksRunLists.Add(marksRunList);
        }

        var pagePoolRunList = new XElement(ns + "RunList",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", "r_pagepool"),
            new XAttribute("DescriptiveName", "PagePool"),
            new XAttribute("PartIDKeys", "Run"),
            new XAttribute("Status", "Available"));

        var outputRunList = new XElement(ns + "RunList",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", "r_output"),
            new XAttribute("Status", "Unavailable"),
            new XElement(ns + "LayoutElement",
                new XAttribute("ElementType", "Reservation")));

        var printingParams = new XElement(ns + "ConventionalPrintingParams",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", "r_print"),
            new XAttribute("PartIDKeys", "SignatureName SheetName Side"),
            new XAttribute("WorkStyle", options.WorkStyle),
            new XAttribute("Status", "Available"));
        if (options.IncludePrintingParamsPartitions)
        {
            foreach (var signatureDef in signatureDefinitions)
            {
                var signatureWorkStyle = signatureDef.WorkStyle ?? options.WorkStyle;
                var sigPart = new XElement(ns + "ConventionalPrintingParams",
                    new XAttribute("SignatureName", signatureDef.Name),
                    new XAttribute("WorkStyle", signatureWorkStyle));
                foreach (var sheetName in signatureDef.Sheets)
                {
                    var sheetPart = new XElement(ns + "ConventionalPrintingParams",
                        new XAttribute("SheetName", sheetName));
                    sheetPart.Add(new XElement(ns + "ConventionalPrintingParams",
                        new XAttribute("Side", "Front")));
                    if (options.IncludeBackSide)
                    {
                        sheetPart.Add(new XElement(ns + "ConventionalPrintingParams",
                            new XAttribute("Side", "Back")));
                    }

                    sigPart.Add(sheetPart);
                }
                printingParams.Add(sigPart);
            }
        }

        XElement? transferCurvePool = null;
        if (options.IncludeTransferCurvePool)
        {
            transferCurvePool = new XElement(ns + "TransferCurvePool",
                new XAttribute("Class", "Parameter"),
                new XAttribute("ID", options.TransferCurvePoolId),
                new XAttribute("PartIDKeys", "SignatureName SheetName"),
                new XAttribute("Status", "Available"));

            foreach (var signatureDef in signatureDefinitions)
            {
                foreach (var sheetName in signatureDef.Sheets)
                {
                    transferCurvePool.Add(new XElement(ns + "TransferCurvePool",
                        new XAttribute("SignatureName", signatureDef.Name),
                        new XAttribute("SheetName", sheetName)));
                }
            }

            transferCurvePool.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Paper"),
                new XAttribute("CTM", $"1 0 0 1 {options.PaperCtmOffsetX} {options.PaperCtmOffsetY}")));

            transferCurvePool.Add(new XElement(ns + "TransferCurveSet",
                new XAttribute("Name", "Plate"),
                new XAttribute("CTM", "1 0 0 1 0 0")));

            resourcePool.Add(transferCurvePool);
        }

        var binderySignatures = new Dictionary<string, string>();
        if (options.IncludeBinderySignature)
        {
            if (options.BinderySignaturePerSignature && signatureDefinitions.Count > 0)
            {
                var index = 1;
                foreach (var signatureDef in signatureDefinitions)
                {
                    var binderyId = $"{options.BinderySignatureId}_{index}";
                    binderySignatures[signatureDef.Name] = binderyId;
                    var binderySignature = new XElement(ns + "BinderySignature",
                        new XAttribute("Class", "Parameter"),
                        new XAttribute("ID", binderyId),
                        new XAttribute("BinderySignatureType", options.BinderySignatureType),
                        new XAttribute("NumberUp", options.BinderySignatureNumberUp),
                        new XAttribute("Status", "Available"),
                        new XElement(ns + "SignatureCell",
                            new XAttribute("FrontPages", options.BinderyFrontPages),
                            new XAttribute("BackPages", options.BinderyBackPages),
                            new XAttribute(hdm + "FrontSchemePageOrientation", options.BinderyFrontOrientation),
                            new XAttribute(hdm + "BackSchemePageOrientation", options.BinderyBackOrientation)));
                    resourcePool.Add(binderySignature);
                    index++;
                }
            }
            else
            {
                var binderySignature = new XElement(ns + "BinderySignature",
                    new XAttribute("Class", "Parameter"),
                    new XAttribute("ID", options.BinderySignatureId),
                    new XAttribute("BinderySignatureType", options.BinderySignatureType),
                    new XAttribute("NumberUp", options.BinderySignatureNumberUp),
                    new XAttribute("Status", "Available"),
                    new XElement(ns + "SignatureCell",
                        new XAttribute("FrontPages", options.BinderyFrontPages),
                        new XAttribute("BackPages", options.BinderyBackPages),
                        new XAttribute(hdm + "FrontSchemePageOrientation", options.BinderyFrontOrientation),
                        new XAttribute(hdm + "BackSchemePageOrientation", options.BinderyBackOrientation)));
                resourcePool.Add(binderySignature);
                binderySignatures[options.SignatureName] = options.BinderySignatureId;
            }
        }

        XElement? strippingParams = null;
        if (options.IncludeStrippingParams)
        {
            var workStyle = options.StrippingWorkStyle ?? options.WorkStyle;
            strippingParams = new XElement(ns + "StrippingParams",
                new XAttribute("Class", "Parameter"),
                new XAttribute("ID", options.StrippingParamsId),
                new XAttribute("PartIDKeys", "SignatureName SheetName"),
                new XAttribute("SectionList", "0"),
                new XAttribute("Status", "Available"),
                new XAttribute("WorkStyle", workStyle));
            if (!string.IsNullOrWhiteSpace(options.StripSheetLay))
            {
                strippingParams.SetAttributeValue("SheetLay", options.StripSheetLay);
            }

            foreach (var signatureDef in signatureDefinitions)
            {
                foreach (var sheetName in signatureDef.Sheets)
                {
                    var part = new XElement(ns + "StrippingParams",
                        new XAttribute("SignatureName", signatureDef.Name),
                        new XAttribute("SheetName", sheetName));
                    if (options.IncludeBinderySignature
                        && binderySignatures.TryGetValue(signatureDef.Name, out var binderyId))
                    {
                        part.Add(new XElement(ns + "BinderySignatureRef",
                            new XAttribute("rRef", binderyId)));
                    }
                    strippingParams.Add(part);
                }
            }

            if (options.IncludePaperMedia)
            {
                strippingParams.Add(new XElement(ns + "MediaRef", new XAttribute("rRef", options.PaperMediaId)));
            }

            if (options.IncludePlateMedia)
            {
                strippingParams.Add(new XElement(ns + "MediaRef", new XAttribute("rRef", options.PlateMediaId)));
            }

            strippingParams.Add(new XElement(ns + "Position",
                new XAttribute("RelativeBox", $"{options.StripRelLeft} {options.StripRelBottom} {options.StripRelRight} {options.StripRelTop}")));
            strippingParams.Add(new XElement(ns + "StripCellParams",
                new XAttribute("TrimSize", $"{options.StripTrimWidth} {options.StripTrimHeight}")));

            resourcePool.Add(strippingParams);
        }

        XElement? assembly = null;
        if (options.IncludeAssembly)
        {
            assembly = new XElement(ns + "Assembly",
                new XAttribute("Class", "Parameter"),
                new XAttribute("ID", options.AssemblyId),
                new XAttribute("Order", options.AssemblyOrder),
                new XAttribute("Status", "Available"));

            if (!string.IsNullOrWhiteSpace(options.ContentAssemblyIdBase))
            {
                var slotCount = Math.Max(1, options.ContentGridColumns * options.ContentGridRows);
                foreach (var signatureDef in signatureDefinitions)
                {
                    foreach (var sheetName in signatureDef.Sheets)
                    {
                        for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
                        {
                            var assemblyId = BuildAssemblyId(options, slotIndex);
                            assembly.Add(new XElement(ns + "AssemblySection",
                                new XAttribute("AssemblyIDs", assemblyId),
                                new XAttribute("DescriptiveName", assemblyId),
                                new XAttribute(hdm + "SignatureName", signatureDef.Name),
                                new XAttribute(hdm + "SheetName", sheetName)));
                        }
                    }
                }
            }

            resourcePool.Add(assembly);
        }
        XElement? paperMedia = null;
        if (options.IncludePaperMedia)
        {
            paperMedia = new XElement(ns + "Media",
                new XAttribute("Class", "Consumable"),
                new XAttribute("ID", options.PaperMediaId),
                new XAttribute("MediaType", "Paper"),
                new XAttribute("Dimension", $"{options.PaperWidth} {options.PaperHeight}"),
                new XAttribute("Status", "Available"),
                new XAttribute("PartIDKeys", "SignatureName SheetName"));

            if (options.PaperThickness is not null)
            {
                paperMedia.SetAttributeValue("Thickness", options.PaperThickness.Value);
            }

            if (options.PaperWeight is not null)
            {
                paperMedia.SetAttributeValue("Weight", options.PaperWeight.Value);
            }

            if (!string.IsNullOrWhiteSpace(options.PaperGrainDirection))
            {
                paperMedia.SetAttributeValue("GrainDirection", options.PaperGrainDirection);
            }

            if (!string.IsNullOrWhiteSpace(options.PaperFeedDirection))
            {
                paperMedia.SetAttributeValue(hdm + "FeedDirection", options.PaperFeedDirection);
            }

            if (!string.IsNullOrWhiteSpace(options.PaperBrand))
            {
                paperMedia.SetAttributeValue("Brand", options.PaperBrand);
                paperMedia.SetAttributeValue("DescriptiveName", options.PaperBrand);
            }

            if (!string.IsNullOrWhiteSpace(options.PaperProductId))
            {
                paperMedia.SetAttributeValue("ProductID", options.PaperProductId);
            }

            if (!string.IsNullOrWhiteSpace(options.PaperGrade))
            {
                paperMedia.SetAttributeValue("Grade", options.PaperGrade);
            }

            foreach (var signatureDef in signatureDefinitions)
            {
                foreach (var sheetName in signatureDef.Sheets)
                {
                    var paperPart = new XElement(ns + "Media",
                        new XAttribute("SignatureName", signatureDef.Name),
                        new XAttribute("SheetName", sheetName));
                    paperMedia.Add(paperPart);
                }
            }
            resourcePool.Add(paperMedia);
        }

        XElement? plateMedia = null;
        if (options.IncludePlateMedia)
        {
            plateMedia = new XElement(ns + "Media",
                new XAttribute("Class", "Consumable"),
                new XAttribute("ID", options.PlateMediaId),
                new XAttribute("MediaType", "Plate"),
                new XAttribute("Dimension", $"{options.PlateWidth} {options.PlateHeight}"),
                new XAttribute("Status", "Available"),
                new XAttribute("PartIDKeys", "SignatureName SheetName"));

            if (options.PlateLeadingEdge is not null)
            {
                plateMedia.SetAttributeValue(hdm + "LeadingEdge", options.PlateLeadingEdge.Value);
            }

            foreach (var signatureDef in signatureDefinitions)
            {
                foreach (var sheetName in signatureDef.Sheets)
                {
                    var platePart = new XElement(ns + "Media",
                        new XAttribute("SignatureName", signatureDef.Name),
                        new XAttribute("SheetName", sheetName));
                    plateMedia.Add(platePart);
                }
            }
            resourcePool.Add(plateMedia);
        }

        if (options.IncludePaperMedia || options.IncludePlateMedia)
        {
            foreach (var sheet in sheetLayouts)
            {
                if (options.IncludePaperMedia)
                {
                    sheet.Add(new XElement(ns + "MediaRef", new XAttribute("rRef", options.PaperMediaId)));
                }
                if (options.IncludePlateMedia)
                {
                    sheet.Add(new XElement(ns + "MediaRef", new XAttribute("rRef", options.PlateMediaId)));
                }
            }
        }

        if (documentRunList is not null)
        {
            resourcePool.Add(documentRunList);
        }
        foreach (var marksRunList in marksRunLists)
        {
            resourcePool.Add(marksRunList);
        }
        if (options.IncludePagePool)
        {
            resourcePool.Add(pagePoolRunList);
        }
        if (options.IncludeOutputRunList)
        {
            resourcePool.Add(outputRunList);
        }
        resourcePool.Add(printingParams);

        var resourceLinkPool = new XElement(ns + "ResourceLinkPool",
            new XElement(ns + "LayoutLink",
                new XAttribute("CombinedProcessIndex", "0 1"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_layout")),
            new XElement(ns + "ConventionalPrintingParamsLink",
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_print")));

        foreach (var marksRunList in marksRunLists)
        {
            resourceLinkPool.Add(new XElement(ns + "RunListLink",
                new XAttribute("CombinedProcessIndex", "0"),
                new XAttribute("ProcessUsage", "Marks"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", marksRunList.Attribute("ID")?.Value ?? "r_marks")));
        }

        if (options.IncludeDocumentRunList)
        {
            resourceLinkPool.Add(new XElement(ns + "RunListLink",
                new XAttribute("CombinedProcessIndex", "0"),
                new XAttribute("ProcessUsage", "Document"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_doc")));
        }

        if (options.IncludePaperMedia && paperMedia is not null)
        {
            resourceLinkPool.Add(new XElement(ns + "MediaLink",
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", options.PaperMediaId)));
        }

        if (options.IncludePlateMedia && plateMedia is not null)
        {
            resourceLinkPool.Add(new XElement(ns + "MediaLink",
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", options.PlateMediaId)));
        }

        if (options.IncludeTransferCurvePool && transferCurvePool is not null)
        {
            resourceLinkPool.Add(new XElement(ns + "TransferCurvePoolLink",
                new XAttribute("CombinedProcessIndex", "1"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", options.TransferCurvePoolId)));
        }

        if (options.IncludeStrippingParams && strippingParams is not null)
        {
            resourceLinkPool.Add(new XElement(ns + "StrippingParamsLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", options.StrippingParamsId)));
        }

        if (options.IncludeBinderySignature)
        {
            foreach (var binderyId in binderySignatures.Values.Distinct())
            {
                resourceLinkPool.Add(new XElement(ns + "BinderySignatureLink",
                    new XAttribute("Usage", "Input"),
                    new XAttribute("rRef", binderyId)));
            }
        }

        if (options.IncludeAssembly && assembly is not null)
        {
            resourceLinkPool.Add(new XElement(ns + "AssemblyLink",
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", options.AssemblyId)));
        }

        if (options.IncludePagePool)
        {
            resourceLinkPool.Add(new XElement(ns + "RunListLink",
                new XAttribute("CombinedProcessIndex", "0"),
                new XAttribute("ProcessUsage", "PagePool"),
                new XAttribute("Usage", "Input"),
                new XAttribute("rRef", "r_pagepool")));
        }

        if (options.IncludeOutputRunList)
        {
            resourceLinkPool.Add(new XElement(ns + "RunListLink",
                new XAttribute("CombinedProcessIndex", "0"),
                new XAttribute("Usage", "Output"),
                new XAttribute("rRef", "r_output")));
        }

        root.Add(resourceLinkPool);

        return new XDocument(new XDeclaration("1.0", "UTF-8", "no"), root);
    }

    private static XElement CreateSide(XNamespace ns, XNamespace hdm, string side, GeneratorOptions options, int sheetIndex, string signatureName, string sheetName)
    {
        var paperLeft = options.PaperRectOffsetX;
        var paperBottom = options.PaperRectOffsetY;
        var paperRight = options.PaperRectOffsetX + options.PaperWidth;
        var paperTop = options.PaperRectOffsetY + options.PaperHeight;

        var layoutSide = new XElement(ns + "Layout",
            new XAttribute("Side", side));
        if (options.IncludePaperRect)
        {
            layoutSide.SetAttributeValue(hdm + "PaperRect", $"{paperLeft} {paperBottom} {paperRight} {paperTop}");
        }
        layoutSide.Add(BuildMarkObject(ns, options));
        if (options.IncludeContentPlacement || options.IncludeBackContentPlacement)
        {
            var isBack = string.Equals(side, "Back", StringComparison.OrdinalIgnoreCase);
            var jobPart = ResolveJobPart(options, signatureName, sheetName);
            if (options.ContentJobPartSignatures.Count > 0
                && !options.ContentJobPartSignatures.Contains(signatureName))
            {
                jobPart = null;
            }
            var columns = Math.Max(1, options.ContentGridColumns);
            var rows = Math.Max(1, options.ContentGridRows);
            var slotIndex = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var slotRow = row;
                    var slotCol = col;
                    if (isBack && options.ContentGridReverseBackRowOrder)
                    {
                        slotRow = (rows - 1) - row;
                    }
                    if (isBack && options.ContentGridReverseBackColumnOrder)
                    {
                        slotCol = (columns - 1) - col;
                    }
                    var baseX = isBack ? options.BackContentOffsetX : options.ContentOffsetX;
                    var baseY = isBack ? options.BackContentOffsetY : options.ContentOffsetY;
                    var x = baseX + (slotCol * (options.ContentTrimWidth + options.ContentGapX));
                    var y = baseY + (slotRow * (options.ContentTrimHeight + options.ContentGapY));
                    layoutSide.Add(BuildContentObject(ns, hdm, options, side, sheetIndex, slotIndex, x, y, jobPart));
                    slotIndex++;
                }
            }
        }
        return layoutSide;
    }

    private static XElement BuildContentObject(XNamespace ns, XNamespace hdm, GeneratorOptions options, string side, int sheetIndex, int slotIndex, decimal x, decimal y, string? jobPart)
    {
        var content = new XElement(ns + "ContentObject");
        if (!options.IncludeContentPlacement)
        {
            return content;
        }

        var isBack = string.Equals(side, "Back", StringComparison.OrdinalIgnoreCase);
        if (isBack && !options.IncludeBackContentPlacement)
        {
            return content;
        }

        var w = options.ContentTrimWidth;
        var h = options.ContentTrimHeight;

        var clipLeft = x;
        var clipBottom = y;
        var clipRight = x + w;
        var clipTop = y + h;

        if (isBack && options.MirrorBackContent)
        {
            var tx = x + w;
            var ty = y + h;
            content.SetAttributeValue("CTM", $"-1 0 0 -1 {tx} {ty}");
            content.SetAttributeValue("TrimCTM", $"-1 0 0 -1 {tx} {ty}");
        }
        else
        {
            content.SetAttributeValue("CTM", $"1 0 0 1 {x} {y}");
            content.SetAttributeValue("TrimCTM", $"1 0 0 1 {x} {y}");
        }
        content.SetAttributeValue("TrimSize", $"{w} {h}");
        content.SetAttributeValue("ClipBox", $"{clipLeft} {clipBottom} {clipRight} {clipTop}");
        content.SetAttributeValue(hdm + "FinalPageBox", $"{clipLeft} {clipBottom} {clipRight} {clipTop}");
        content.SetAttributeValue(hdm + "PageOrientation", "0");
        content.SetAttributeValue(hdm + "AssemblyFB", side);
        if (!string.IsNullOrWhiteSpace(options.ContentAssemblyIdBase))
        {
            var assemblyId = BuildAssemblyId(options, slotIndex);
            content.SetAttributeValue(hdm + "AssemblyIDs", assemblyId);
        }

        var slotCount = Math.Max(1, options.ContentGridColumns * options.ContentGridRows);
        var index = 0;
        if (options.ContentIncrementPerSheet)
        {
            index += sheetIndex * slotCount;
        }
        if (options.ContentIncrementPerSlot)
        {
            index += slotIndex;
        }
        var ord = options.ContentOrdBase + (options.ContentOrdStep * index);
        var name = options.ContentNameBase + (options.ContentNameStep * index);
        content.SetAttributeValue("Ord", ord.ToString(CultureInfo.InvariantCulture));
        var nameValue = name.ToString(CultureInfo.InvariantCulture);
        content.SetAttributeValue("DescriptiveName", nameValue);
        if (options.IncludeContentRunlistIndex)
        {
            content.SetAttributeValue(hdm + "RunlistIndex", nameValue);
        }
        if (options.IncludeContentJobPart && !string.IsNullOrWhiteSpace(jobPart))
        {
            content.SetAttributeValue(hdm + "JobPart", jobPart);
        }

        return content;
    }

    private static string? ResolveJobPart(GeneratorOptions options, string signatureName, string sheetName)
    {
        if (options.ContentJobPartSheets.Count > 0
            && options.ContentJobPartSheets.TryGetValue(sheetName, out var sheetJobPart))
        {
            return sheetJobPart;
        }
        if (options.SignatureJobParts.Count > 0
            && options.SignatureJobParts.TryGetValue(signatureName, out var jobPart))
        {
            return jobPart;
        }
        if (options.SignaJobPartNames.Count > 0)
        {
            return options.SignaJobPartNames[0];
        }
        return string.IsNullOrWhiteSpace(options.SignaJobPartName) ? null : options.SignaJobPartName;
    }

    private static XElement BuildMarkObject(XNamespace ns, GeneratorOptions options)
    {
        if (!options.IncludeMarkObjectGeometry)
        {
            return new XElement(ns + "MarkObject");
        }

        return new XElement(ns + "MarkObject",
            new XAttribute("CTM", "1 0 0 1 0 0"),
            new XAttribute("ClipBox", $"0 0 {options.PlateWidth} {options.PlateHeight}"),
            new XAttribute("Ord", "0"));
    }

    private static XElement BuildMarksRunList(
        XNamespace ns,
        XNamespace hdm,
        GeneratorOptions options,
        SignatureDefinition signatureGroup,
        string id,
        int totalPages,
        int logicalPageStart)
    {
        var runList = new XElement(ns + "RunList",
            new XAttribute("Class", "Parameter"),
            new XAttribute("ID", id),
            new XAttribute("PartIDKeys", options.MarksRunListPartIdKeys),
            new XAttribute(hdm + "OFW", options.RunListHdmOfw),
            new XAttribute("Status", "Available"),
            BuildLayoutElement(ns, options, options.MarksFileName));

        runList.SetAttributeValue("LogicalPage", "0");
        runList.SetAttributeValue("NPage", totalPages.ToString());
        runList.SetAttributeValue("Pages", BuildPageRange(totalPages));

        var sigPart = new XElement(ns + "RunList",
            new XAttribute("SignatureName", signatureGroup.Name));

        var logicalPage = logicalPageStart;
        foreach (var sheetName in signatureGroup.Sheets)
        {
            if (options.MarksResetLogicalPagePerSheet)
            {
                logicalPage = logicalPageStart;
            }
            var sheetPart = new XElement(ns + "RunList",
                new XAttribute("SheetName", sheetName));

            var frontPages = Math.Max(1, options.MarksPagesPerSide);
            var frontPart = new XElement(ns + "RunList",
                new XAttribute("Side", "Front"),
                new XAttribute("LogicalPage", logicalPage.ToString()),
                new XAttribute("Pages", BuildPageRangeWithOffset(logicalPage, frontPages)),
                BuildLayoutElement(ns, options, options.MarksFileName));
            if (options.IncludeMarksSeparations)
            {
                AddDefaultMarkSeparations(frontPart.Element(ns + "LayoutElement"), ns, hdm);
            }
            sheetPart.Add(frontPart);
            logicalPage += frontPages;

            if (options.MarksIncludeBackSide)
            {
                var backPages = Math.Max(1, options.MarksPagesPerSide);
                var backPart = new XElement(ns + "RunList",
                    new XAttribute("Side", "Back"),
                    new XAttribute("LogicalPage", logicalPage.ToString()),
                    new XAttribute("Pages", BuildPageRangeWithOffset(logicalPage, backPages)),
                    BuildLayoutElement(ns, options, options.MarksFileName));
                if (options.IncludeMarksSeparations)
                {
                    AddDefaultMarkSeparations(backPart.Element(ns + "LayoutElement"), ns, hdm);
                }
                sheetPart.Add(backPart);
                logicalPage += backPages;
            }

            sigPart.Add(sheetPart);
        }

        runList.Add(sigPart);
        return runList;
    }

    private static string BuildAssemblyId(GeneratorOptions options, int slotIndex)
    {
        var prefix = options.ContentAssemblyIdBase;
        if (!string.IsNullOrWhiteSpace(options.ContentAssemblyIdBase2)
            && options.ContentAssemblySplitIndex > 0
            && slotIndex >= options.ContentAssemblySplitIndex)
        {
            prefix = options.ContentAssemblyIdBase2;
        }

        return prefix
               + (options.ContentAssemblyIdStart + (options.ContentAssemblyIdStep * slotIndex));
    }

    private static string BuildPageRange(int count)
    {
        if (count <= 1)
        {
            return "0";
        }

        return $"0 ~ {count - 1}";
    }

    private static string BuildPageRangeWithOffset(int start, int count)
    {
        if (count <= 1)
        {
            return start.ToString();
        }

        return $"{start} ~ {start + count - 1}";
    }

    private static void AddDefaultMarkSeparations(XElement? layoutElement, XNamespace ns, XNamespace hdm)
    {
        if (layoutElement is null)
        {
            return;
        }

        var mapRelNames = new[] { "B", "C", "M", "Y", "X", "Z" };
        foreach (var name in mapRelNames)
        {
            layoutElement.Add(new XElement(ns + "SeparationSpec",
                new XAttribute("Name", name),
                new XAttribute(hdm + "IsMapRel", "true"),
                new XAttribute(hdm + "SubType", "Control"),
                new XAttribute(hdm + "Type", "Printing")));
        }

        var spotNames = new[] { "U", "V", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };
        foreach (var name in spotNames)
        {
            layoutElement.Add(new XElement(ns + "SeparationSpec",
                new XAttribute("Name", name),
                new XAttribute(hdm + "IsMapRel", "false"),
                new XAttribute(hdm + "Type", "Printing")));
        }
    }

    private static XElement BuildLayoutElement(XNamespace ns, GeneratorOptions options, string url)
    {
        var layout = new XElement(ns + "LayoutElement");
        if (options.IncludeRunListClassAttributes)
        {
            layout.SetAttributeValue("Class", "Parameter");
        }

        var fileSpec = new XElement(ns + "FileSpec",
            new XAttribute("URL", url),
            new XAttribute("MimeType", options.FileSpecMimeType));
        if (options.IncludeRunListClassAttributes)
        {
            fileSpec.SetAttributeValue("Class", "Parameter");
        }

        layout.Add(fileSpec);
        return layout;
    }
}
