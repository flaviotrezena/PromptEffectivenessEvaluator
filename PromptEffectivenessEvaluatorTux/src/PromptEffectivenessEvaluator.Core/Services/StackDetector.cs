using PromptEffectivenessEvaluator.Core.Models;

namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class StackDetectionResult
{
    public StackType Stack { get; init; }
    public int Confidence { get; init; }
    public string Specialization => Stack switch
    {
        StackType.DotNet => "C# / .NET",
        StackType.JavaSpringBoot => "Java / Spring Boot",
        StackType.NodeJavaScript => "JavaScript / Node.js",
        StackType.Python => "Python",
        StackType.Mixed => "arquiteturas multi-stack",
        _ => "{{SPECIALIZATION}}"
    };
}

public sealed class StackDetector
{
    public StackDetectionResult Detect(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            return new StackDetectionResult { Stack = StackType.Unknown, Confidence = 0 };

        var files = Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
            .Where(f => !IsIgnored(f))
            .Take(5000)
            .ToList();

        var hasCsproj = files.Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        var hasSln = files.Any(f => f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));
        var hasPom = files.Any(f => Path.GetFileName(f).Equals("pom.xml", StringComparison.OrdinalIgnoreCase));
        var hasGradle = files.Any(f => Path.GetFileName(f).StartsWith("build.gradle", StringComparison.OrdinalIgnoreCase));
        var hasPackage = files.Any(f => Path.GetFileName(f).Equals("package.json", StringComparison.OrdinalIgnoreCase));
        var hasPy = files.Any(f => f.EndsWith(".py", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase));

        var stackCount = new[] { hasCsproj || hasSln, hasPom || hasGradle, hasPackage, hasPy }.Count(x => x);
        if (stackCount > 1)
            return new StackDetectionResult { Stack = StackType.Mixed, Confidence = 75 };
        if (hasCsproj || hasSln)
            return new StackDetectionResult { Stack = StackType.DotNet, Confidence = 98 };
        if (hasPom || hasGradle)
            return new StackDetectionResult { Stack = StackType.JavaSpringBoot, Confidence = 95 };
        if (hasPackage)
            return new StackDetectionResult { Stack = StackType.NodeJavaScript, Confidence = 90 };
        if (hasPy)
            return new StackDetectionResult { Stack = StackType.Python, Confidence = 85 };

        return new StackDetectionResult { Stack = StackType.Unknown, Confidence = 0 };
    }

    private static bool IsIgnored(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/bin/") || p.Contains("/obj/") || p.Contains("/node_modules/") || p.Contains("/.git/") || p.Contains("/target/");
    }
}
