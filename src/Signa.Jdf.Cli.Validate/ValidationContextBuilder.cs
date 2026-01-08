using System.Xml.Linq;
using Signa.Jdf;

namespace Signa.Jdf.Cli.Validate;

internal static class ValidationContextBuilder
{
    // Builds compact summaries for batch reports and issue clustering.
    public static bool IsMontage(JdfDocument document)
    {
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return false;
        }

        var ns = document.JdfNamespace;
        return root.Descendants(ns + "BinderySignature")
            .Any(element => string.Equals(element.Attribute("BinderySignatureType")?.Value, "Grid", StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildJobContext(JdfDocument document)
    {
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return "context=unknown";
        }

        var ns = document.JdfNamespace;
        var hdm = document.HdmNamespace;

        var workStyles = root
            .Descendants(ns + "ConventionalPrintingParams")
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var workStyleSummary = workStyles.Count == 0
            ? "workStyles=unknown"
            : $"workStyles={string.Join(",", workStyles)}";

        var signatureCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("SignatureName") is not null);
        var sheetCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("SheetName") is not null);
        var sideCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("Side") is not null);
        var componentCount = root.Descendants(ns + "Component").Count();
        var blockCount = root.Descendants(ns + "Component")
            .Count(element =>
                (element.Attribute("ComponentType")?.Value ?? string.Empty)
                .Contains("Block", StringComparison.OrdinalIgnoreCase));
        var foldingCount = root.Descendants(ns + "FoldingParams").Count();
        var combiningCount = root.Descendants(hdm + "CombiningParams").Count();
        var montage = IsMontage(document);

        var assemblyIds = root.Descendants(ns + "ContentObject")
            .Select(element => element.Attribute("AssemblyIDs")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        var foldToken = assemblyIds.FirstOrDefault(LooksLikeFoldScheme);
        var assemblySummary = string.IsNullOrWhiteSpace(foldToken)
            ? (assemblyIds.Count > 0 ? "assemblyIds=present" : "assemblyIds=none")
            : $"assemblyIds={foldToken}";

        return string.Join("; ", new[]
        {
            workStyleSummary,
            $"signatures={signatureCount}",
            $"sheets={sheetCount}",
            $"sides={sideCount}",
            $"components={componentCount}(block={blockCount})",
            $"foldingParams={foldingCount}",
            $"combiningParams={combiningCount}",
            assemblySummary,
            $"montage={montage.ToString().ToLowerInvariant()}"
        });
    }

    public static string BuildJobShape(JdfDocument document)
    {
        var root = document.XmlDocument.Root;
        if (root is null)
        {
            return "shape=unknown";
        }

        var ns = document.JdfNamespace;
        var hdm = document.HdmNamespace;

        var workStyles = root
            .Descendants(ns + "ConventionalPrintingParams")
            .Select(element => element.Attribute("WorkStyle")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var workStyleSummary = workStyles.Count == 0
            ? "ws=?"
            : $"ws={string.Join(",", workStyles)}";

        var signatureCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("SignatureName") is not null);
        var sheetCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("SheetName") is not null);
        var sideCount = root.Descendants(ns + "Layout")
            .Count(element => element.Attribute("Side") is not null);
        var blockCount = root.Descendants(ns + "Component")
            .Count(element =>
                (element.Attribute("ComponentType")?.Value ?? string.Empty)
                .Contains("Block", StringComparison.OrdinalIgnoreCase));
        var foldingCount = root.Descendants(ns + "FoldingParams").Count();
        var combiningCount = root.Descendants(hdm + "CombiningParams").Count();
        var montage = IsMontage(document);

        return $"{workStyleSummary}; sig={signatureCount}; sheet={sheetCount}; side={sideCount}; block={blockCount}; fold={foldingCount}; combine={combiningCount}; montage={montage.ToString().ToLowerInvariant()}";
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
}
