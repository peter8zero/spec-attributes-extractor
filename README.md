# Spec Option Extractor (Roslyn)

A C# console app that parses `[SpecOption]` and `[SpecCapability]` attributes from C# source files using Roslyn and generates JavaScript data files for the specbuilder UI.

This is a rewrite of the [Python regex-based extractor](../spec-option-extractor/) using Microsoft's Roslyn compiler platform for proper AST-based parsing.

## Why Roslyn and C#

The original Python extractor uses regex to parse C# syntax. It works, but regex-based parsing has inherent fragility:

- **Regex can't handle nesting.** Attributes with nested generics, string literals containing parentheses, or multi-line formatting can confuse pattern matching. Roslyn parses the actual syntax tree — it handles every valid C# construct by definition.
- **Native to the ecosystem.** The code being parsed is C#. A C# tool can be maintained by the same developers who write the attributes, without needing Python installed or understood.
- **Roslyn is battle-tested.** It's the same parser that powers the C# compiler, Visual Studio, and OmniSharp. If Roslyn can parse it, so can this tool.
- **Structured access to syntax.** Instead of regex groups, the extractor walks typed AST nodes (`ClassDeclarationSyntax`, `MethodDeclarationSyntax`, `AttributeSyntax`). Adding support for new attribute parameters or node types is a code change, not a regex rewrite.

The tool uses **syntax tree parsing only** — no semantic model, no full compilation. It reads attribute decorations and method signatures from raw `.cs` files without needing a project or solution file.

## Prerequisites

- .NET 8.0 SDK or later

## Quick Start

```bash
dotnet run -- /path/to/csharp/modules/ -o ../code-options.js -c ../code-capabilities.js -v
```

## CLI Reference

```
Usage: dotnet run -- <directory> [options]

Arguments:
  <directory>                         Directory of .cs files to scan

Options:
  -o, --output <path>                 Output file for options (default: ./code-options.js)
  -c, --capabilities-output <path>    Output file for capabilities (default: ./code-capabilities.js)
  --preview                           Print summary to stdout
  --coverage                          Print coverage report
  -v, --verbose                       Diagnostic output
  -h, --help                          Show this help
```

### Examples

```bash
# Standard extraction with verbose output
dotnet run -- /path/to/modules/ -o ../code-options.js -c ../code-capabilities.js -v

# Preview what was found without inspecting the JS files
dotnet run -- /path/to/modules/ --preview

# Coverage report: how many public methods are documented
dotnet run -- /path/to/modules/ --coverage

# All together
dotnet run -- /path/to/modules/ -o ../code-options.js -c ../code-capabilities.js --preview --coverage -v
```

## Output Format

The tool generates two JavaScript files identical in format to the Python extractor. The specbuilder loads them at runtime.

**`code-options.js`** — one entry per `[SpecOption]`-decorated class:

```javascript
const CODE_OPTIONS = {
    gmp: [
        {
            id: "gmp_equaliser",
            name: "GMP Equalisation (Dual Record)",
            description: "Applies GMP equalisation using the dual-record method.",
            whyItMatters: "Required for schemes with GMP service between 1990-1997.",
            codeClass: "GmpEqualiser",
            scheme: "Core",
            lastModified: "2025-12-01"
        }
    ]
};
```

**`code-capabilities.js`** — one entry per `[SpecCapability]`-decorated method:

```javascript
const CODE_CAPABILITIES = {
    gmp: [
        {
            id: "check_anti_franking",
            name: "Anti-Franking Check",
            description: "Checks whether excess pension above GMP covers GMP increases.",
            methodName: "CheckAntiFranking",
            returnType: "bool",
            parameters: "decimal totalPension, decimal gmpAmount, decimal gmpIncrease",
            parentOption: { id: "gmp_equaliser", name: "GMP Equalisation (Dual Record)" },
            codeClass: "GmpEqualiser",
            scheme: "Core",
            lastModified: "2025-12-01"
        }
    ]
};
```

## Project Structure

```
spec-option-extractor-ros/
├── SpecOptionExtractor.sln     Solution file (open this in VS Code / Visual Studio)
├── SpecOptionExtractor.csproj  Console app, depends on Microsoft.CodeAnalysis.CSharp
├── Program.cs                  CLI entry point, argument parsing, orchestration
├── AttributeParser.cs          Roslyn CSharpSyntaxWalker — extracts attributes in a single pass
├── JsGenerator.cs              Generates code-options.js and code-capabilities.js
├── CoverageAnalyzer.cs         Coverage report (documented vs total public methods)
├── Models.cs                   Data models (SpecOption, SpecCapability, ParseResult, etc.)
└── tests/
    ├── Tests.csproj            xUnit test project
    ├── ToSnakeIdTests.cs       ID generation: PascalCase → snake_case (15 cases)
    ├── AttributeParserTests.cs Roslyn parsing: attributes, capabilities, nesting (20 cases)
    ├── JsGeneratorTests.cs     JS output: format, line endings, escaping (8 cases)
    └── CoverageAnalyzerTests.cs Coverage report logic (5 cases)
```

## Tests

53 tests covering the core logic. Run them with:

```bash
dotnet test tests/
```

### VS Code Test Explorer

Open the `spec-option-extractor-ros/` folder in VS Code with the **C# Dev Kit** extension installed. The solution file (`SpecOptionExtractor.sln`) will be detected automatically, and all 53 tests will appear in the **Test Explorer** panel where you can browse, run, and debug them individually or by group.

### What's tested

**`ToSnakeIdTests`** — The ID generation function must match the Python extractor exactly, since both tools produce IDs consumed by the same specbuilder UI. Tests cover:
- Standard PascalCase (`TrivialCommutation` → `trivial_commutation`)
- Consecutive uppercase runs / acronyms (`GMPEqualiser` → `gmp_equaliser`, `CETVCalculator` → `cetv_calculator`, `DCSchemeBuilder` → `dc_scheme_builder`)
- Mixed separators (spaces, hyphens)
- Multi-acronym sequences (`HTMLToXML` → `html_to_xml`)

**`AttributeParserTests`** — Roslyn walker correctness:
- String literal and constant reference extraction
- Required field validation (skips attributes missing `Category` or `Name`)
- Optional `WhyItMatters` field (present or absent)
- Method signature extraction (name, return type, parameters)
- Parent option linking (capability → class's `[SpecOption]`)
- Standalone capabilities (no parent)
- Class modifiers: `sealed`, `abstract`, `partial`
- Generic return types (`Task<decimal>`)
- Expression-bodied methods (`=>`)
- Nested class context restoration
- `Attribute` suffix handling (`[SpecOptionAttribute(...)]`)
- Unknown constant fallback
- Parse result caching (`GetOrParse` returns same instance)

**`JsGeneratorTests`** — Output format:
- Unix line endings (`\n`, not `\r\n`) on all platforms
- Field ordering matches Python tool
- Alphabetical category sorting
- Trailing commas between items, not after last
- `parentOption` inline object format
- String escaping (quotes, backslashes)
- Optional fields omitted when null

**`CoverageAnalyzerTests`** — Report accuracy:
- Correct documented/total counts and percentages
- Empty codebase handling
- Classes with no public methods (`n/a`)
- Uses cached parse results (no redundant Roslyn parsing)

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Syntax-only parsing (no semantic model) | We only read attributes and signatures — no need to resolve types or compile |
| Single-pass walker | Each file is parsed once; results cached on `SourceModule.Parsed` and reused by options, capabilities, and coverage |
| `SortedDictionary` for categories | Alphabetical output ordering without a separate sort step |
| Explicit `\n` line endings | `StringBuilder.AppendLine` uses `Environment.NewLine` (`\r\n` on Windows) — forced `\n` for cross-platform consistency |
| No `--json` input mode | The Python tool supports JSON for CI pipelines; this tool is directory-scan only, keeping it simpler |
| Hardcoded `SpecCategories` map | Same approach as the Python tool — categories are a small, stable set |

## Differences from the Python Extractor

| | Python | Roslyn (this tool) |
|---|---|---|
| Parsing | Regex patterns | Roslyn AST walker |
| Runtime | Python 3.6+ | .NET 8+ |
| JSON input | Supported (`--json`) | Not supported |
| Dependencies | None (stdlib) | Microsoft.CodeAnalysis.CSharp |
| Tests | 30+ unittest classes | 53 xUnit tests |
| Parse caching | N/A (regex is cheap) | Single parse per file, reused everywhere |

Both tools produce identical output for the same input.
