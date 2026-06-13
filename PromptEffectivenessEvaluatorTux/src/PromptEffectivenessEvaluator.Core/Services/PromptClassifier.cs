using PromptEffectivenessEvaluator.Core.Models;

namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class PromptClassifier
{
    public PromptIntent DetectIntent(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        if (ContainsAny(p, "teste", "test", "xunit", "nunit", "coverage", "cobertura")) return PromptIntent.Testing;
        if (ContainsAny(p, "bug", "erro", "falha", "exception", "não funciona", "nao funciona")) return PromptIntent.BugInvestigation;
        if (ContainsAny(p, "refator", "refactor", "clean code", "melhorar código", "melhorar codigo")) return PromptIntent.Refactoring;
        if (ContainsAny(p, "arquitetura", "architecture", "camada", "design", "acoplamento")) return PromptIntent.ArchitectureReview;
        if (ContainsAny(p, "performance", "latência", "latencia", "lento", "throughput")) return PromptIntent.Performance;
        if (ContainsAny(p, "segurança", "security", "vulnerabilidade", "auth", "token", "secret")) return PromptIntent.Security;
        if (ContainsAny(p, "document", "readme", "relatório", "relatorio", "descreva")) return PromptIntent.Documentation;
        if (ContainsAny(p, "explique", "como funciona", "o que faz", "entender")) return PromptIntent.ExplainCode;
        return PromptIntent.Unknown;
    }

    public PromptScope DetectScope(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        if (ContainsAny(p, "pr", "pull request", "merge request", "diff")) return PromptScope.PullRequestReview;
        if (ContainsAny(p, "projeto inteiro", "repositório", "repositorio", "arquitetura do projeto", "visão geral", "visao geral")) return PromptScope.RepositoryOverview;
        if (ContainsAny(p, "documentar", "relatório técnico", "relatorio tecnico", "endpoints", "dtos")) return PromptScope.DocumentationExtraction;
        if (ContainsAny(p, "módulo", "modulo", "componente", "serviço", "servico")) return PromptScope.ModuleAnalysis;
        return PromptScope.LocalQuestion;
    }

    private static bool ContainsAny(string text, params string[] terms) => terms.Any(text.Contains);
}
