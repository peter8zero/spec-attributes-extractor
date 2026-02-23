using SpecOptionExtractor;

// --- Argument parsing ---
string? inputPath = null;
string outputPath = "./code-options.js";
string capabilitiesOutputPath = "./code-capabilities.js";
bool preview = false;
bool coverage = false;
bool verbose = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-o":
        case "--output":
            if (i + 1 >= args.Length) { Console.Error.WriteLine("Error: -o/--output requires a value."); return 1; }
            outputPath = args[++i];
            break;
        case "-c":
        case "--capabilities-output":
            if (i + 1 >= args.Length) { Console.Error.WriteLine("Error: -c/--capabilities-output requires a value."); return 1; }
            capabilitiesOutputPath = args[++i];
            break;
        case "--preview":
            preview = true;
            break;
        case "--coverage":
            coverage = true;
            break;
        case "-v":
        case "--verbose":
            verbose = true;
            break;
        case "-h":
        case "--help":
            PrintUsage();
            return 0;
        default:
            if (args[i].StartsWith("-"))
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                PrintUsage();
                return 1;
            }
            inputPath = args[i];
            break;
    }
}

if (inputPath == null)
{
    Console.Error.WriteLine("Error: input directory is required.");
    PrintUsage();
    return 1;
}

if (!Directory.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: {inputPath} is not a directory.");
    return 1;
}

// --- Load modules ---
if (verbose)
    Console.WriteLine($"Scanning {inputPath} for .cs files...");

var modules = LoadModulesFromDir(inputPath, verbose);

if (verbose)
    Console.WriteLine($"Loaded {modules.Count} .cs files from {inputPath}");

// --- Parse all modules once (cached on each SourceModule) ---
foreach (var module in modules)
{
    if (!string.IsNullOrEmpty(module.Code))
        AttributeParser.GetOrParse(module);
}

// --- Process options ---
if (verbose)
    Console.WriteLine("\n--- Spec Options ---");

var optionCategories = ProcessOptions(modules, verbose);
int totalOptions = optionCategories.Values.Sum(list => list.Count);

if (verbose)
{
    Console.WriteLine($"\nExtracted {totalOptions} spec options across {optionCategories.Count} categories");
    foreach (var (cat, opts) in optionCategories)
        Console.WriteLine($"  {cat}: {opts.Count} options");
}

var optionsJs = JsGenerator.GenerateOptionsJs(optionCategories);
File.WriteAllText(outputPath, optionsJs);

if (verbose)
    Console.WriteLine($"Wrote {outputPath}");

// --- Process capabilities ---
if (verbose)
    Console.WriteLine("\n--- Spec Capabilities ---");

var capCategories = ProcessCapabilities(modules, verbose);
int totalCaps = capCategories.Values.Sum(list => list.Count);

if (verbose)
{
    Console.WriteLine($"\nExtracted {totalCaps} spec capabilities across {capCategories.Count} categories");
    foreach (var (cat, caps) in capCategories)
        Console.WriteLine($"  {cat}: {caps.Count} capabilities");
}

var capsJs = JsGenerator.GenerateCapabilitiesJs(capCategories);
File.WriteAllText(capabilitiesOutputPath, capsJs);

if (verbose)
    Console.WriteLine($"Wrote {capabilitiesOutputPath}");

// --- Preview ---
if (preview)
{
    if (verbose) Console.WriteLine();
    PrintPreview(optionCategories, capCategories);
}

// --- Coverage ---
if (coverage)
{
    if (verbose) Console.WriteLine();
    Console.Write(CoverageAnalyzer.GenerateReport(modules));
}

// --- Default summary ---
if (!verbose && !coverage && !preview)
{
    Console.WriteLine($"Extracted {totalOptions} spec options -> {outputPath}");
    Console.WriteLine($"Extracted {totalCaps} spec capabilities -> {capabilitiesOutputPath}");
}

return 0;

// --- Helper methods ---

static List<SourceModule> LoadModulesFromDir(string dirPath, bool verbose)
{
    var modules = new List<SourceModule>();
    dirPath = Path.GetFullPath(dirPath);

    foreach (var filePath in Directory.EnumerateFiles(dirPath, "*.cs", SearchOption.AllDirectories).OrderBy(f => f))
    {
        var relPath = Path.GetRelativePath(dirPath, filePath);
        var parts = relPath.Split(Path.DirectorySeparatorChar);
        var scheme = parts.Length > 1 ? parts[0] : "";
        var moduleName = Path.GetFileNameWithoutExtension(filePath);
        var code = File.ReadAllText(filePath);
        var mtime = File.GetLastWriteTime(filePath);
        var lastModified = mtime.ToString("yyyy-MM-ddTHH:mm:ss");

        modules.Add(new SourceModule
        {
            ModuleName = moduleName,
            Scheme = scheme,
            Code = code,
            LastModified = lastModified,
        });

        if (verbose)
            Console.WriteLine($"  READ {relPath} (scheme={scheme})");
    }

    return modules;
}

static SortedDictionary<string, List<SpecOption>> ProcessOptions(List<SourceModule> modules, bool verbose)
{
    var categories = new SortedDictionary<string, List<SpecOption>>();

    foreach (var module in modules)
    {
        if (string.IsNullOrEmpty(module.Code))
        {
            if (verbose) Console.WriteLine($"  SKIP {module.ModuleName}: no code");
            continue;
        }

        var result = AttributeParser.GetOrParse(module);
        if (result.Options.Count == 0)
        {
            if (verbose) Console.WriteLine($"  SKIP {module.ModuleName}: no [SpecOption] attribute");
            continue;
        }

        var parsed = result.Options[0];
        var fields = parsed.Fields;

        var category = fields["Category"].ToLower();
        if (category == "builder")
            category = "builders";

        var lastModified = module.LastModified;
        if (lastModified.Contains('T'))
            lastModified = lastModified.Split('T')[0];

        var codeClass = parsed.ClassName ?? module.ModuleName;

        var option = new SpecOption
        {
            Id = AttributeParser.ToSnakeId(codeClass),
            Name = fields["Name"],
            Description = fields.GetValueOrDefault("Description", ""),
            CodeClass = codeClass,
            Scheme = module.Scheme,
            LastModified = lastModified,
        };

        if (fields.ContainsKey("WhyItMatters"))
            option.WhyItMatters = fields["WhyItMatters"];

        if (!categories.ContainsKey(category))
            categories[category] = new List<SpecOption>();
        categories[category].Add(option);

        if (verbose)
            Console.WriteLine($"  FOUND {module.ModuleName} -> {category}/{option.Id}");
    }

    return categories;
}

static SortedDictionary<string, List<SpecCapability>> ProcessCapabilities(List<SourceModule> modules, bool verbose)
{
    var categories = new SortedDictionary<string, List<SpecCapability>>();

    foreach (var module in modules)
    {
        if (string.IsNullOrEmpty(module.Code))
        {
            if (verbose) Console.WriteLine($"  SKIP {module.ModuleName}: no code");
            continue;
        }

        var result = AttributeParser.GetOrParse(module);
        if (result.Capabilities.Count == 0)
        {
            if (verbose) Console.WriteLine($"  SKIP {module.ModuleName}: no [SpecCapability] attribute");
            continue;
        }

        // Determine class name from first capability's class
        var codeClass = result.Capabilities[0].ClassName ?? module.ModuleName;

        var lastModified = module.LastModified;
        if (lastModified.Contains('T'))
            lastModified = lastModified.Split('T')[0];

        foreach (var parsed in result.Capabilities)
        {
            var fields = parsed.Fields;
            var category = fields["Category"].ToLower();

            var cap = new SpecCapability
            {
                Id = AttributeParser.ToSnakeId(parsed.MethodName ?? fields["Name"]),
                Name = fields["Name"],
                Description = fields.GetValueOrDefault("Description", ""),
                CodeClass = parsed.ClassName ?? codeClass,
                Scheme = module.Scheme,
                LastModified = lastModified,
            };

            if (fields.ContainsKey("WhyItMatters"))
                cap.WhyItMatters = fields["WhyItMatters"];

            if (parsed.MethodName != null)
                cap.MethodName = parsed.MethodName;
            if (parsed.ReturnType != null)
                cap.ReturnType = parsed.ReturnType;
            if (parsed.Parameters != null)
                cap.Parameters = parsed.Parameters;

            // Link to parent option via dedicated property
            if (parsed.ParentOption != null)
            {
                cap.ParentOption = new ParentOptionRef
                {
                    Id = AttributeParser.ToSnakeId(parsed.ParentOption.ClassName),
                    Name = parsed.ParentOption.Name,
                };
            }

            if (!categories.ContainsKey(category))
                categories[category] = new List<SpecCapability>();
            categories[category].Add(cap);

            if (verbose)
                Console.WriteLine($"  FOUND capability {module.ModuleName} -> {category}/{cap.Id}");
        }
    }

    return categories;
}

static void PrintPreview(SortedDictionary<string, List<SpecOption>> optionCats, SortedDictionary<string, List<SpecCapability>> capCats)
{
    int totalOptions = optionCats.Values.Sum(l => l.Count);
    Console.WriteLine($"Options ({totalOptions}):");
    if (totalOptions == 0)
        Console.WriteLine("  (none)");
    foreach (var (cat, opts) in optionCats)
    {
        var items = string.Join(", ", opts.Select(o => $"{o.Name} ({o.CodeClass})"));
        Console.WriteLine($"  {FormatCategory(cat),-14} {items}");
    }

    Console.WriteLine();

    int totalCaps = capCats.Values.Sum(l => l.Count);
    Console.WriteLine($"Capabilities ({totalCaps}):");
    if (totalCaps == 0)
        Console.WriteLine("  (none)");
    foreach (var (cat, caps) in capCats)
    {
        var parented = new Dictionary<string, List<string>>();
        var standalone = new List<string>();
        foreach (var c in caps)
        {
            if (c.ParentOption != null)
            {
                if (!parented.ContainsKey(c.ParentOption.Name))
                    parented[c.ParentOption.Name] = new List<string>();
                parented[c.ParentOption.Name].Add(c.Name);
            }
            else
            {
                standalone.Add(c.Name);
            }
        }

        var parts = new List<string>();
        foreach (var (parentName, capNames) in parented)
            parts.Add($"{string.Join(", ", capNames)}  -> parent: {parentName}");
        if (standalone.Count > 0)
            parts.Add($"{string.Join(", ", standalone)}  (standalone)");

        Console.WriteLine($"  {FormatCategory(cat),-14} {string.Join("; ", parts)}");
    }

    Console.WriteLine();
}

static string FormatCategory(string key)
{
    var known = new HashSet<string> { "gmp", "dc", "pcls", "erf", "lrf", "afr", "cetv" };
    var words = key.Replace('_', ' ').Split(' ');
    return string.Join(" ", words.Select(w =>
    {
        if (string.IsNullOrEmpty(w)) return w;
        return known.Contains(w.ToLower())
            ? w.ToUpper()
            : char.ToUpper(w[0]) + w[1..];
    }));
}

static void PrintUsage()
{
    Console.WriteLine(@"Usage: dotnet run -- <directory> [options]

Arguments:
  <directory>                         Directory of .cs files to scan

Options:
  -o, --output <path>                 Output file for options (default: ./code-options.js)
  -c, --capabilities-output <path>    Output file for capabilities (default: ./code-capabilities.js)
  --preview                           Print summary to stdout
  --coverage                          Print coverage report
  -v, --verbose                       Diagnostic output
  -h, --help                          Show this help");
}
