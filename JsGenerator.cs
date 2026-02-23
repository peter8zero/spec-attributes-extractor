using System.Text;

namespace SpecOptionExtractor;

/// <summary>
/// Generates code-options.js and code-capabilities.js in the same format as the Python tool.
/// Uses explicit \n line endings for cross-platform consistency.
/// </summary>
public static class JsGenerator
{
    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private static void AppendLn(StringBuilder sb, string line = "")
    {
        sb.Append(line);
        sb.Append('\n');
    }

    /// <summary>
    /// Generate CODE_OPTIONS JS from categorised options.
    /// </summary>
    public static string GenerateOptionsJs(SortedDictionary<string, List<SpecOption>> categories)
    {
        var sb = new StringBuilder();
        AppendLn(sb, "const CODE_OPTIONS = {");

        var catKeys = categories.Keys.ToList();
        for (int i = 0; i < catKeys.Count; i++)
        {
            var cat = catKeys[i];
            var options = categories[cat];
            AppendLn(sb, $"    {cat}: [");

            for (int j = 0; j < options.Count; j++)
            {
                var opt = options[j];
                AppendLn(sb, "        {");
                AppendLn(sb, $"            id: \"{Escape(opt.Id)}\",");
                AppendLn(sb, $"            name: \"{Escape(opt.Name)}\",");
                AppendLn(sb, $"            description: \"{Escape(opt.Description)}\",");
                if (opt.WhyItMatters != null)
                    AppendLn(sb, $"            whyItMatters: \"{Escape(opt.WhyItMatters)}\",");
                AppendLn(sb, $"            codeClass: \"{Escape(opt.CodeClass)}\",");
                AppendLn(sb, $"            scheme: \"{Escape(opt.Scheme)}\",");
                AppendLn(sb, $"            lastModified: \"{Escape(opt.LastModified)}\"");

                var trailing = j < options.Count - 1 ? "," : "";
                AppendLn(sb, $"        }}{trailing}");
            }

            var catTrailing = i < catKeys.Count - 1 ? "," : "";
            AppendLn(sb, $"    ]{catTrailing}");
        }

        AppendLn(sb, "};");
        return sb.ToString();
    }

    /// <summary>
    /// Generate CODE_CAPABILITIES JS from categorised capabilities.
    /// </summary>
    public static string GenerateCapabilitiesJs(SortedDictionary<string, List<SpecCapability>> categories)
    {
        var sb = new StringBuilder();
        AppendLn(sb, "const CODE_CAPABILITIES = {");

        var catKeys = categories.Keys.ToList();
        for (int i = 0; i < catKeys.Count; i++)
        {
            var cat = catKeys[i];
            var caps = categories[cat];
            AppendLn(sb, $"    {cat}: [");

            for (int j = 0; j < caps.Count; j++)
            {
                var cap = caps[j];
                AppendLn(sb, "        {");
                AppendLn(sb, $"            id: \"{Escape(cap.Id)}\",");
                AppendLn(sb, $"            name: \"{Escape(cap.Name)}\",");
                AppendLn(sb, $"            description: \"{Escape(cap.Description)}\",");
                if (cap.WhyItMatters != null)
                    AppendLn(sb, $"            whyItMatters: \"{Escape(cap.WhyItMatters)}\",");
                if (cap.MethodName != null)
                    AppendLn(sb, $"            methodName: \"{Escape(cap.MethodName)}\",");
                if (cap.ReturnType != null)
                    AppendLn(sb, $"            returnType: \"{Escape(cap.ReturnType)}\",");
                if (cap.Parameters != null)
                    AppendLn(sb, $"            parameters: \"{Escape(cap.Parameters)}\",");
                if (cap.ParentOption != null)
                    AppendLn(sb, $"            parentOption: {{ id: \"{Escape(cap.ParentOption.Id)}\", name: \"{Escape(cap.ParentOption.Name)}\" }},");
                AppendLn(sb, $"            codeClass: \"{Escape(cap.CodeClass)}\",");
                AppendLn(sb, $"            scheme: \"{Escape(cap.Scheme)}\",");
                AppendLn(sb, $"            lastModified: \"{Escape(cap.LastModified)}\"");

                var trailing = j < caps.Count - 1 ? "," : "";
                AppendLn(sb, $"        }}{trailing}");
            }

            var catTrailing = i < catKeys.Count - 1 ? "," : "";
            AppendLn(sb, $"    ]{catTrailing}");
        }

        AppendLn(sb, "};");
        return sb.ToString();
    }
}
