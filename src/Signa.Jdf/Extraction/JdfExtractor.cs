namespace Signa.Jdf;

public static class JdfExtractor
{
    // Convenience helpers for summarizing layout/RunList data without validation.
    public static IReadOnlyList<SheetSizeInfo> GetSheetSizes(JdfDocument document)
    {
        var results = new List<SheetSizeInfo>();
        if (document.Layout is null)
        {
            return results;
        }

        foreach (var part in FlattenLayout(document.Layout))
        {
            if (string.IsNullOrWhiteSpace(part.SheetName))
            {
                continue;
            }

            results.Add(new SheetSizeInfo
            {
                SignatureName = part.SignatureName,
                SheetName = part.SheetName,
                Side = part.Side,
                SurfaceContentsBox = part.SurfaceContentsBox,
                PaperRect = part.PaperRect
            });
        }

        return results;
    }

    public static IReadOnlyCollection<string> GetWorkStyles(JdfDocument document)
    {
        // Merge WorkStyle signals from layout, printing, and stripping params.
        var styles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in FlattenLayout(document.Layout))
        {
            if (!string.IsNullOrWhiteSpace(part.SourceWorkStyle))
            {
                styles.Add(part.SourceWorkStyle);
            }
        }

        foreach (var printing in document.PrintingParams)
        {
            if (!string.IsNullOrWhiteSpace(printing.WorkStyle))
            {
                styles.Add(printing.WorkStyle);
            }
        }

        foreach (var stripping in document.StrippingParams)
        {
            if (!string.IsNullOrWhiteSpace(stripping.WorkStyle))
            {
                styles.Add(stripping.WorkStyle);
            }
        }

        return styles;
    }

    public static IReadOnlyCollection<string> GetSeparationNames(JdfDocument document)
    {
        // Collect separation placeholders across top-level RunLists and partitions.
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var runList in document.RunLists)
        {
            foreach (var spec in runList.SeparationSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec.Name))
                {
                    names.Add(spec.Name);
                }
            }

            foreach (var part in runList.Parts)
            {
                CollectSeparationNames(part, names);
            }
        }

        return names;
    }

    private static void CollectSeparationNames(RunListPart part, HashSet<string> names)
    {
        foreach (var spec in part.SeparationSpecs)
        {
            if (!string.IsNullOrWhiteSpace(spec.Name))
            {
                names.Add(spec.Name);
            }
        }

        foreach (var child in part.Children)
        {
            CollectSeparationNames(child, names);
        }
    }

    private static IEnumerable<LayoutPart> FlattenLayout(LayoutPart? root)
    {
        if (root is null)
        {
            yield break;
        }

        yield return root;

        foreach (var child in root.Children)
        {
            foreach (var nested in FlattenLayout(child))
            {
                yield return nested;
            }
        }
    }
}
