namespace SpecOptionExtractor;

/// <summary>
/// Generates a coverage report showing documented vs total public methods per [SpecOption] class.
/// </summary>
public static class CoverageAnalyzer
{
    public static string GenerateReport(List<SourceModule> modules)
    {
        var lines = new List<string>();
        int totalDocumented = 0;
        int totalPublic = 0;
        int classCount = 0;

        foreach (var module in modules)
        {
            if (string.IsNullOrEmpty(module.Code))
                continue;

            var result = AttributeParser.GetOrParse(module);
            if (result.Options.Count == 0)
                continue;

            var className = result.Options[0].ClassName ?? module.ModuleName;
            result.PublicMethodCounts.TryGetValue(className, out int publicMethods);
            int documented = result.Capabilities.Count;

            totalDocumented += documented;
            totalPublic += publicMethods;
            classCount++;

            var pct = publicMethods > 0
                ? $"{(double)documented / publicMethods * 100:0}%"
                : "n/a";
            lines.Add($"  {className}: {documented}/{publicMethods} methods documented ({pct})");
        }

        if (classCount == 0)
            return "Coverage Report:\n  No classes with [SpecOption] found.\n";

        var overallPct = totalPublic > 0
            ? $"{(double)totalDocumented / totalPublic * 100:0}%"
            : "n/a";
        lines.Add("  ---");
        lines.Add($"  Overall: {totalDocumented}/{totalPublic} methods documented ({overallPct})");

        return "Coverage Report:\n" + string.Join("\n", lines) + "\n";
    }
}
