using PromptEffectivenessEvaluator.Core.Models;

namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class ContextSelector
{
    private static readonly string[] AllowedExtensions = [".cs", ".csproj", ".sln", ".md", ".json", ".config", ".yml", ".yaml", ".java", ".js", ".ts", ".html", ".css", ".xml"];

    public IReadOnlyList<string> SelectCandidateFiles(string projectPath, string prompt, PromptIntent intent, PromptScope scope, int maxFiles = 8)
    {
        if (!Directory.Exists(projectPath)) return Array.Empty<string>();

        var terms = ExtractTerms(prompt);
        var all = Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
            .Where(f => !IsIgnored(f))
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .Where(f => f.Length < 200_000)
            .Select(f => new ScoredFile(f.FullName, Score(projectPath, f.FullName, terms, intent, scope)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path.Length)
            .Take(maxFiles)
            .Select(x => Path.GetRelativePath(projectPath, x.Path))
            .ToList();

        return all;
    }

    public string BuildContextPreview(string projectPath, IReadOnlyList<string> relativeFiles, int maxCharsPerFile = 1200)
    {
        if (relativeFiles.Count == 0) return "{{CONTEXT}}";
        var chunks = new List<string>();
        foreach (var relative in relativeFiles)
        {
            var full = Path.Combine(projectPath, relative);
            if (!File.Exists(full)) continue;
            var text = File.ReadAllText(full);
            if (text.Length > maxCharsPerFile)
                text = text[..maxCharsPerFile] + Environment.NewLine + "... [trecho truncado pelo avaliador]";
            chunks.Add($"--- FILE: {relative} ---{Environment.NewLine}{text}");
        }
        return string.Join(Environment.NewLine + Environment.NewLine, chunks);
    }

    private static int Score(string root, string file, HashSet<string> terms, PromptIntent intent, PromptScope scope)
    {
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/').ToLowerInvariant();
        var name = Path.GetFileName(file).ToLowerInvariant();
        var ext = Path.GetExtension(file).ToLowerInvariant();
        var score = 0;

        foreach (var term in terms)
        {
            if (relative.Contains(term)) score += 20;
            if (name.Contains(term)) score += 30;
        }

        if (name is "readme.md") score += scope is PromptScope.RepositoryOverview or PromptScope.DocumentationExtraction ? 35 : 8;
        if (ext is ".csproj" or ".sln" or ".xml" or ".json" or ".yml" or ".yaml") score += 12;
        if (relative.Contains("test") || relative.Contains("tests")) score += intent == PromptIntent.Testing ? 40 : 5;
        if (relative.Contains("controller")) score += intent is PromptIntent.ExplainCode or PromptIntent.Documentation or PromptIntent.Security ? 25 : 5;
        if (relative.Contains("service")) score += 20;
        if (relative.Contains("dto") || relative.Contains("model")) score += scope == PromptScope.DocumentationExtraction ? 30 : 10;
        if (scope == PromptScope.RepositoryOverview && (ext == ".md" || ext == ".csproj" || name == "pom.xml" || name == "package.json")) score += 20;
        if (scope == PromptScope.LocalQuestion && ext == ".md") score -= 5;
        return Math.Max(score, 0);
    }

    private static HashSet<string> ExtractTerms(string prompt)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "como", "para", "isso", "esse", "essa", "com", "dos", "das", "que", "uma", "por", "the", "and", "what", "this", "that", "melhorar", "explique" };

        return prompt.Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '?', '!'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length >= 4 && !stop.Contains(x))
            .Take(12)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsIgnored(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/bin/") || p.Contains("/obj/") || p.Contains("/node_modules/") || p.Contains("/.git/") || p.Contains("/target/") || p.Contains("/.vs/");
    }

    private sealed record ScoredFile(string Path, int Score);
}
