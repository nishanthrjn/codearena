// Placeholder for LanguageRegistry.cs
namespace CodeArena.Runner;

public record LanguageConfig(
    string Image,          // Docker image for the runner sandbox
    string FileExtension,
    string? CompileCommand, // null if interpreted
    string RunCommand,
    string FileName        // source file name inside container workspace
);

public static class LanguageRegistry
{
    private static readonly Dictionary<string, LanguageConfig> _configs = new()
    {
        ["csharp"] = new LanguageConfig(
            Image: "codearena-runner-csharp:latest",
            FileExtension: ".cs",
            CompileCommand: "dotnet-script compile /workspace/solution.cs",
            RunCommand: "dotnet-script /workspace/solution.cs",
            FileName: "solution.cs"
        ),
        ["python"] = new LanguageConfig(
            Image: "codearena-runner-python:latest",
            FileExtension: ".py",
            CompileCommand: null,
            RunCommand: "python3 /workspace/solution.py",
            FileName: "solution.py"
        ),
        ["javascript"] = new LanguageConfig(
            Image: "codearena-runner-node:latest",
            FileExtension: ".js",
            CompileCommand: null,
            RunCommand: "node /workspace/solution.js",
            FileName: "solution.js"
        ),
        ["c"] = new LanguageConfig(
            Image: "codearena-runner-c-cpp:latest",
            FileExtension: ".c",
            CompileCommand: "gcc -O2 -o /workspace/a.out /workspace/solution.c",
            RunCommand: "/workspace/a.out",
            FileName: "solution.c"
        ),
        ["cpp"] = new LanguageConfig(
            Image: "codearena-runner-c-cpp:latest",
            FileExtension: ".cpp",
            CompileCommand: "g++ -O2 -std=c++17 -o /workspace/a.out /workspace/solution.cpp",
            RunCommand: "/workspace/a.out",
            FileName: "solution.cpp"
        )
    };

    public static LanguageConfig Get(string language)
    {
        if (_configs.TryGetValue(language, out var cfg)) return cfg;
        throw new ArgumentException($"Unsupported language: {language}");
    }
}