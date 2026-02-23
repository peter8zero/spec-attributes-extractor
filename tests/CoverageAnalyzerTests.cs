using SpecOptionExtractor;
using Xunit;

namespace Tests;

public class CoverageAnalyzerTests
{
    [Fact]
    public void Reports_documented_vs_total_methods()
    {
        var modules = new List<SourceModule>
        {
            new()
            {
                ModuleName = "MyClass",
                Code = """
                    [SpecOption(Category = "GMP", Name = "Test")]
                    public class MyClass
                    {
                        [SpecCapability(Category = "GMP", Name = "Documented")]
                        public void Documented() { }
                        public void Undocumented() { }
                    }
                    """,
            }
        };

        var report = CoverageAnalyzer.GenerateReport(modules);
        Assert.Contains("MyClass: 1/2 methods documented (50%)", report);
        Assert.Contains("Overall: 1/2 methods documented (50%)", report);
    }

    [Fact]
    public void Reports_no_classes_when_none_found()
    {
        var modules = new List<SourceModule>
        {
            new()
            {
                ModuleName = "Plain",
                Code = "public class Plain { public void Do() { } }",
            }
        };

        var report = CoverageAnalyzer.GenerateReport(modules);
        Assert.Contains("No classes with [SpecOption] found.", report);
    }

    [Fact]
    public void Reports_na_when_no_public_methods()
    {
        var modules = new List<SourceModule>
        {
            new()
            {
                ModuleName = "Empty",
                Code = """
                    [SpecOption(Category = "GMP", Name = "Empty")]
                    public class Empty { }
                    """,
            }
        };

        var report = CoverageAnalyzer.GenerateReport(modules);
        Assert.Contains("Empty: 0/0 methods documented (n/a)", report);
    }

    [Fact]
    public void Uses_cached_parse_results()
    {
        var module = new SourceModule
        {
            ModuleName = "Cached",
            Code = """
                [SpecOption(Category = "GMP", Name = "Test")]
                public class Cached
                {
                    public void Method() { }
                }
                """,
        };

        // Pre-parse
        var result1 = AttributeParser.GetOrParse(module);
        // Coverage should reuse
        CoverageAnalyzer.GenerateReport(new List<SourceModule> { module });
        var result2 = module.Parsed;

        Assert.Same(result1, result2);
    }

    [Fact]
    public void Skips_empty_code_modules()
    {
        var modules = new List<SourceModule>
        {
            new() { ModuleName = "Empty", Code = "" }
        };

        var report = CoverageAnalyzer.GenerateReport(modules);
        Assert.Contains("No classes with [SpecOption] found.", report);
    }
}
