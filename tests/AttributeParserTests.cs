using SpecOptionExtractor;
using Xunit;

namespace Tests;

public class AttributeParserTests
{
    [Fact]
    public void Parses_SpecOption_with_string_literals()
    {
        var code = """
            [SpecOption(Category = "Revaluation", Name = "CPI-Capped", Description = "Desc here")]
            public class CpiCapped { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Options);
        var opt = result.Options[0];
        Assert.Equal("CpiCapped", opt.ClassName);
        Assert.Equal("Revaluation", opt.Fields["Category"]);
        Assert.Equal("CPI-Capped", opt.Fields["Name"]);
        Assert.Equal("Desc here", opt.Fields["Description"]);
    }

    [Fact]
    public void Parses_SpecOption_with_constant_reference()
    {
        var code = """
            [SpecOption(Category = SpecCategories.GMP, Name = "GMP Eq")]
            public class GmpEq { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Options);
        Assert.Equal("GMP", result.Options[0].Fields["Category"]);
    }

    [Fact]
    public void String_literal_takes_priority_over_constant()
    {
        var code = """
            [SpecOption(Category = "Custom", Name = "Test")]
            public class Test { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Equal("Custom", result.Options[0].Fields["Category"]);
    }

    [Fact]
    public void Skips_class_without_required_fields()
    {
        var code = """
            [SpecOption(Description = "No category or name")]
            public class Incomplete { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Empty(result.Options);
    }

    [Fact]
    public void Parses_WhyItMatters_when_present()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Test", WhyItMatters = "Important reason")]
            public class Test { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Equal("Important reason", result.Options[0].Fields["WhyItMatters"]);
    }

    [Fact]
    public void WhyItMatters_absent_when_not_specified()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Test")]
            public class Test { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.False(result.Options[0].Fields.ContainsKey("WhyItMatters"));
    }

    [Fact]
    public void Parses_SpecCapability_with_method_signature()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Parent")]
            public class MyClass
            {
                [SpecCapability(Category = "GMP", Name = "Check Something")]
                public bool CheckSomething(decimal amount, decimal threshold)
                {
                    return amount > threshold;
                }
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Capabilities);
        var cap = result.Capabilities[0];
        Assert.Equal("CheckSomething", cap.MethodName);
        Assert.Equal("bool", cap.ReturnType);
        Assert.Equal("decimal amount, decimal threshold", cap.Parameters);
    }

    [Fact]
    public void Capability_links_to_parent_option()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "GMP Equalisation")]
            public class GmpEqualiser
            {
                [SpecCapability(Category = "GMP", Name = "Anti-Franking")]
                public bool Check(decimal x) => x > 0;
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Capabilities);
        Assert.NotNull(result.Capabilities[0].ParentOption);
        Assert.Equal("GmpEqualiser", result.Capabilities[0].ParentOption!.ClassName);
        Assert.Equal("GMP Equalisation", result.Capabilities[0].ParentOption!.Name);
    }

    [Fact]
    public void Capability_without_parent_option_has_null_parent()
    {
        var code = """
            public class NoOption
            {
                [SpecCapability(Category = "Transfers", Name = "Partial CETV")]
                public decimal Calc(decimal x) => x;
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Capabilities);
        Assert.Null(result.Capabilities[0].ParentOption);
    }

    [Fact]
    public void Handles_sealed_abstract_partial_classes()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Sealed")]
            public sealed class SealedClass { }

            [SpecOption(Category = "GMP", Name = "Abstract")]
            public abstract class AbstractClass { }

            [SpecOption(Category = "GMP", Name = "Partial")]
            public partial class PartialClass { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Equal(3, result.Options.Count);
        Assert.Equal("SealedClass", result.Options[0].ClassName);
        Assert.Equal("AbstractClass", result.Options[1].ClassName);
        Assert.Equal("PartialClass", result.Options[2].ClassName);
    }

    [Fact]
    public void Handles_generic_return_types()
    {
        var code = """
            public class Svc
            {
                [SpecCapability(Category = "GMP", Name = "Async Op")]
                public async Task<decimal> DoWork(int id) { return 0; }
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Capabilities);
        Assert.Equal("Task<decimal>", result.Capabilities[0].ReturnType);
    }

    [Fact]
    public void Handles_expression_bodied_methods()
    {
        var code = """
            public class Calc
            {
                [SpecCapability(Category = "GMP", Name = "Simple")]
                public decimal Add(decimal a, decimal b) => a + b;
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Capabilities);
        Assert.Equal("Add", result.Capabilities[0].MethodName);
        Assert.Equal("decimal a, decimal b", result.Capabilities[0].Parameters);
    }

    [Fact]
    public void Counts_public_methods_excluding_constructors()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Test")]
            public class MyClass
            {
                public MyClass() { }
                public void DoA() { }
                public void DoB() { }
                private void DoC() { }
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.True(result.PublicMethodCounts.ContainsKey("MyClass"));
        Assert.Equal(2, result.PublicMethodCounts["MyClass"]);
    }

    [Fact]
    public void Nested_class_restores_parent_context()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Outer")]
            public class OuterClass
            {
                public class InnerClass
                {
                    [SpecCapability(Category = "GMP", Name = "Inner Method")]
                    public void InnerMethod() { }
                }

                [SpecCapability(Category = "GMP", Name = "Outer Method")]
                public void OuterMethod() { }
            }
            """;

        var result = AttributeParser.Parse(code);
        // Inner capability should NOT link to OuterClass's SpecOption
        // (inner class has no SpecOption of its own)
        var innerCap = result.Capabilities.First(c => c.MethodName == "InnerMethod");
        Assert.Null(innerCap.ParentOption);

        // Outer capability should link to OuterClass's SpecOption
        var outerCap = result.Capabilities.First(c => c.MethodName == "OuterMethod");
        Assert.NotNull(outerCap.ParentOption);
        Assert.Equal("OuterClass", outerCap.ParentOption!.ClassName);
    }

    [Fact]
    public void Handles_SpecOptionAttribute_suffix()
    {
        var code = """
            [SpecOptionAttribute(Category = "GMP", Name = "With Suffix")]
            public class WithSuffix { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Options);
        Assert.Equal("WithSuffix", result.Options[0].ClassName);
    }

    [Fact]
    public void Unknown_constant_uses_member_name()
    {
        var code = """
            [SpecOption(Category = UnknownClass.SomeValue, Name = "Test")]
            public class Test { }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Single(result.Options);
        Assert.Equal("SomeValue", result.Options[0].Fields["Category"]);
    }

    [Fact]
    public void GetOrParse_caches_result()
    {
        var module = new SourceModule
        {
            ModuleName = "Test",
            Code = """
                [SpecOption(Category = "GMP", Name = "Test")]
                public class Test { }
                """,
        };

        var result1 = AttributeParser.GetOrParse(module);
        var result2 = AttributeParser.GetOrParse(module);
        Assert.Same(result1, result2);
    }

    [Fact]
    public void No_options_for_file_without_attributes()
    {
        var code = """
            public class PlainClass
            {
                public void DoWork() { }
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Empty(result.Options);
        Assert.Empty(result.Capabilities);
    }

    [Fact]
    public void Multiple_capabilities_on_same_class()
    {
        var code = """
            [SpecOption(Category = "GMP", Name = "Parent")]
            public class GmpCalc
            {
                [SpecCapability(Category = "GMP", Name = "First")]
                public decimal First(decimal x) => x;

                [SpecCapability(Category = "GMP", Name = "Second")]
                public decimal Second(decimal x) => x;
            }
            """;

        var result = AttributeParser.Parse(code);
        Assert.Equal(2, result.Capabilities.Count);
        Assert.All(result.Capabilities, c =>
        {
            Assert.NotNull(c.ParentOption);
            Assert.Equal("GmpCalc", c.ParentOption!.ClassName);
        });
    }
}
