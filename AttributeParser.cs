using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecOptionExtractor;

/// <summary>
/// Roslyn-based parser that extracts [SpecOption] and [SpecCapability] attributes from C# syntax trees.
/// Single parse per file — returns options, capabilities, and public method counts in one pass.
/// </summary>
public static class AttributeParser
{
    private static readonly Dictionary<string, Dictionary<string, string>> KnownConstants = new()
    {
        ["SpecCategories"] = new()
        {
            ["Revaluation"] = "Revaluation",
            ["Commutation"] = "Commutation",
            ["GMP"] = "GMP",
            ["Transfers"] = "Transfers",
            ["Builder"] = "Builder",
        }
    };

    /// <summary>
    /// Parse a source file once. Returns options, capabilities, and public method counts.
    /// Use module.Parsed to cache and reuse the result.
    /// </summary>
    public static ParseResult Parse(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var walker = new SpecAttributeWalker();
        walker.Visit(root);
        return new ParseResult
        {
            Options = walker.Options,
            Capabilities = walker.Capabilities,
            PublicMethodCounts = walker.PublicMethodCounts,
        };
    }

    /// <summary>
    /// Get cached parse result for a module, parsing only if not already cached.
    /// </summary>
    public static ParseResult GetOrParse(SourceModule module)
    {
        module.Parsed ??= Parse(module.Code);
        return module.Parsed;
    }

    /// <summary>
    /// Convert PascalCase name to snake_case id.
    /// Matches the Python to_snake_id logic exactly.
    /// </summary>
    public static string ToSnakeId(string name)
    {
        // Insert underscore between a run of uppercase and an uppercase followed by lowercase
        var s = Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1_$2");
        // Insert underscore between lowercase/digit and uppercase
        s = Regex.Replace(s, @"([a-z0-9])([A-Z])", "$1_$2");
        // Replace spaces/hyphens with underscores
        s = Regex.Replace(s, @"[\s\-]+", "_");
        return s.ToLower();
    }

    /// <summary>
    /// Extract named arguments from an attribute syntax node.
    /// Handles string literals and constant references (e.g. SpecCategories.GMP).
    /// </summary>
    private static Dictionary<string, string> ExtractArguments(AttributeSyntax attr)
    {
        var fields = new Dictionary<string, string>();
        if (attr.ArgumentList == null) return fields;

        foreach (var arg in attr.ArgumentList.Arguments)
        {
            var paramName = arg.NameEquals?.Name.Identifier.Text;
            if (paramName == null) continue;

            // String literal: Name = "value"
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                fields[paramName] = literal.Token.ValueText;
            }
            // Constant reference: Category = SpecCategories.GMP
            else if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var className = memberAccess.Expression.ToString();
                var memberName = memberAccess.Name.Identifier.Text;

                if (KnownConstants.TryGetValue(className, out var members) &&
                    members.TryGetValue(memberName, out var resolved))
                {
                    // Don't override string literal (string takes priority)
                    if (!fields.ContainsKey(paramName))
                        fields[paramName] = resolved;
                }
                else
                {
                    if (!fields.ContainsKey(paramName))
                        fields[paramName] = memberName;
                }
            }
        }

        return fields;
    }

    /// <summary>
    /// Find the attribute with the given name on a syntax node's attribute lists.
    /// </summary>
    private static AttributeSyntax? FindAttribute(SyntaxList<AttributeListSyntax> attributeLists, string name)
    {
        foreach (var attrList in attributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();
                if (attrName == name || attrName == name + "Attribute")
                    return attr;
            }
        }
        return null;
    }

    private class SpecAttributeWalker : CSharpSyntaxWalker
    {
        public List<ParsedAttribute> Options { get; } = new();
        public List<ParsedAttribute> Capabilities { get; } = new();
        public Dictionary<string, int> PublicMethodCounts { get; } = new();

        // Track the current class context for capabilities
        private string? _currentClassName;
        private ParsedAttribute? _currentClassOption;

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var prevClassName = _currentClassName;
            var prevOption = _currentClassOption;

            _currentClassName = node.Identifier.Text;
            _currentClassOption = null;

            var specOptionAttr = FindAttribute(node.AttributeLists, "SpecOption");
            if (specOptionAttr != null)
            {
                var fields = ExtractArguments(specOptionAttr);
                if (fields.ContainsKey("Category") && fields.ContainsKey("Name"))
                {
                    var parsed = new ParsedAttribute
                    {
                        Fields = fields,
                        ClassName = node.Identifier.Text,
                    };
                    Options.Add(parsed);
                    _currentClassOption = parsed;

                    // Count public methods for this class (for coverage report)
                    int count = 0;
                    foreach (var method in node.Members.OfType<MethodDeclarationSyntax>())
                    {
                        if (method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                            method.Identifier.Text != node.Identifier.Text)
                        {
                            count++;
                        }
                    }
                    PublicMethodCounts[node.Identifier.Text] = count;
                }
            }

            // Visit child nodes (methods)
            base.VisitClassDeclaration(node);

            _currentClassName = prevClassName;
            _currentClassOption = prevOption;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var specCapAttr = FindAttribute(node.AttributeLists, "SpecCapability");
            if (specCapAttr != null)
            {
                var fields = ExtractArguments(specCapAttr);
                if (fields.ContainsKey("Category") && fields.ContainsKey("Name"))
                {
                    var parsed = new ParsedAttribute
                    {
                        Fields = fields,
                        ClassName = _currentClassName,
                        MethodName = node.Identifier.Text,
                        ReturnType = node.ReturnType.ToString().Trim(),
                        Parameters = string.Join(", ", node.ParameterList.Parameters.Select(p =>
                        {
                            var type = p.Type?.ToString() ?? "";
                            var name = p.Identifier.Text;
                            return $"{type} {name}".Trim();
                        })),
                    };

                    // Link to parent class option via dedicated property
                    if (_currentClassOption != null)
                    {
                        parsed.ParentOption = new ParsedParentRef
                        {
                            ClassName = _currentClassOption.ClassName!,
                            Name = _currentClassOption.Fields["Name"],
                        };
                    }

                    Capabilities.Add(parsed);
                }
            }

            base.VisitMethodDeclaration(node);
        }
    }
}
