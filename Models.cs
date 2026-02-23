namespace SpecOptionExtractor;

public class SpecOption
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public string? WhyItMatters { get; set; }
    public required string CodeClass { get; set; }
    public string Scheme { get; set; } = "";
    public string LastModified { get; set; } = "";
}

public class SpecCapability
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public string? WhyItMatters { get; set; }
    public string? MethodName { get; set; }
    public string? ReturnType { get; set; }
    public string? Parameters { get; set; }
    public ParentOptionRef? ParentOption { get; set; }
    public required string CodeClass { get; set; }
    public string Scheme { get; set; } = "";
    public string LastModified { get; set; } = "";
}

public class ParentOptionRef
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

/// <summary>
/// Raw parsed attribute data before processing into SpecOption/SpecCapability.
/// </summary>
public class ParsedAttribute
{
    public Dictionary<string, string> Fields { get; set; } = new();
    public string? ClassName { get; set; }
    public string? MethodName { get; set; }
    public string? ReturnType { get; set; }
    public string? Parameters { get; set; }
    /// <summary>
    /// For capabilities: reference to the parent class's [SpecOption] if present.
    /// </summary>
    public ParsedParentRef? ParentOption { get; set; }
}

/// <summary>
/// Parent option reference stored on a parsed capability attribute.
/// </summary>
public class ParsedParentRef
{
    public required string ClassName { get; set; }
    public required string Name { get; set; }
}

/// <summary>
/// Complete parse result for a single source file. Cached to avoid re-parsing.
/// </summary>
public class ParseResult
{
    public List<ParsedAttribute> Options { get; set; } = new();
    public List<ParsedAttribute> Capabilities { get; set; } = new();
    /// <summary>
    /// Public method counts per class name (excludes constructors).
    /// </summary>
    public Dictionary<string, int> PublicMethodCounts { get; set; } = new();
}

/// <summary>
/// Represents a loaded C# source module (mirrors the Python module dict).
/// </summary>
public class SourceModule
{
    public required string ModuleName { get; set; }
    public string Scheme { get; set; } = "";
    public required string Code { get; set; }
    public string LastModified { get; set; } = "";
    /// <summary>
    /// Cached parse result. Populated on first parse, reused thereafter.
    /// </summary>
    public ParseResult? Parsed { get; set; }
}
