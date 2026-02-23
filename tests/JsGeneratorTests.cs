using SpecOptionExtractor;
using Xunit;

namespace Tests;

public class JsGeneratorTests
{
    [Fact]
    public void Options_js_uses_unix_line_endings()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["gmp"] = [new SpecOption { Id = "test", Name = "Test", CodeClass = "Test" }]
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        Assert.DoesNotContain("\r\n", js);
        Assert.Contains("\n", js);
    }

    [Fact]
    public void Capabilities_js_uses_unix_line_endings()
    {
        var categories = new SortedDictionary<string, List<SpecCapability>>
        {
            ["gmp"] = [new SpecCapability { Id = "test", Name = "Test", CodeClass = "Test" }]
        };

        var js = JsGenerator.GenerateCapabilitiesJs(categories);
        Assert.DoesNotContain("\r\n", js);
        Assert.Contains("\n", js);
    }

    [Fact]
    public void Options_js_format_matches_python_output()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["gmp"] =
            [
                new SpecOption
                {
                    Id = "gmp_equaliser",
                    Name = "GMP Equalisation",
                    Description = "Applies GMP equalisation.",
                    WhyItMatters = "Required for schemes.",
                    CodeClass = "GmpEqualiser",
                    Scheme = "Core",
                    LastModified = "2025-12-01",
                }
            ]
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        Assert.StartsWith("const CODE_OPTIONS = {\n", js);
        Assert.Contains("    gmp: [\n", js);
        Assert.Contains("            id: \"gmp_equaliser\",\n", js);
        Assert.Contains("            whyItMatters: \"Required for schemes.\",\n", js);
        Assert.EndsWith("};\n", js);
    }

    [Fact]
    public void WhyItMatters_omitted_when_null()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["gmp"] = [new SpecOption { Id = "x", Name = "X", CodeClass = "X" }]
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        Assert.DoesNotContain("whyItMatters", js);
    }

    [Fact]
    public void Categories_sorted_alphabetically()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["revaluation"] = [new SpecOption { Id = "r", Name = "R", CodeClass = "R" }],
            ["commutation"] = [new SpecOption { Id = "c", Name = "C", CodeClass = "C" }],
            ["gmp"] = [new SpecOption { Id = "g", Name = "G", CodeClass = "G" }],
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        int commIdx = js.IndexOf("commutation:");
        int gmpIdx = js.IndexOf("gmp:");
        int revIdx = js.IndexOf("revaluation:");
        Assert.True(commIdx < gmpIdx);
        Assert.True(gmpIdx < revIdx);
    }

    [Fact]
    public void Trailing_commas_between_items_not_after_last()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["gmp"] =
            [
                new SpecOption { Id = "a", Name = "A", CodeClass = "A" },
                new SpecOption { Id = "b", Name = "B", CodeClass = "B" },
            ]
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        // First item should have trailing comma after closing brace
        Assert.Contains("        },\n        {\n", js);
        // Last item should NOT have trailing comma
        Assert.Contains("        }\n    ]\n", js);
    }

    [Fact]
    public void Capabilities_js_includes_parent_option()
    {
        var categories = new SortedDictionary<string, List<SpecCapability>>
        {
            ["gmp"] =
            [
                new SpecCapability
                {
                    Id = "check",
                    Name = "Check",
                    CodeClass = "GmpEq",
                    MethodName = "Check",
                    ReturnType = "bool",
                    Parameters = "decimal x",
                    ParentOption = new ParentOptionRef { Id = "gmp_eq", Name = "GMP Eq" },
                }
            ]
        };

        var js = JsGenerator.GenerateCapabilitiesJs(categories);
        Assert.Contains("parentOption: { id: \"gmp_eq\", name: \"GMP Eq\" },", js);
    }

    [Fact]
    public void Escapes_quotes_and_backslashes()
    {
        var categories = new SortedDictionary<string, List<SpecOption>>
        {
            ["gmp"] =
            [
                new SpecOption
                {
                    Id = "test",
                    Name = "Test \"quoted\"",
                    Description = "Path: C:\\foo\\bar",
                    CodeClass = "Test",
                }
            ]
        };

        var js = JsGenerator.GenerateOptionsJs(categories);
        Assert.Contains("name: \"Test \\\"quoted\\\"\",", js);
        Assert.Contains("description: \"Path: C:\\\\foo\\\\bar\",", js);
    }
}
