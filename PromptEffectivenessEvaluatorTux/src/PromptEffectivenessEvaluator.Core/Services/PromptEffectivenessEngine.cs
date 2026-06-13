using PromptEffectivenessEvaluator.Core.Models;

namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class PromptEffectivenessEngine
{
    private readonly StackDetector _stackDetector = new();
    private readonly PromptClassifier _classifier = new();
    private readonly ContextSelector _contextSelector = new();
    private readonly TokenEstimator _tokenEstimator = new();
    private readonly PromptGenerator _promptGenerator = new();

    public PromptEvaluationResult Evaluate(PromptEvaluationRequest request)
    {
        var stack = _stackDetector.Detect(request.ProjectPath);
        var intent = _classifier.DetectIntent(request.Prompt);
        var scope = _classifier.DetectScope(request.Prompt);
        var candidateFiles = _contextSelector.SelectCandidateFiles(request.ProjectPath, request.Prompt, intent, scope);
        var context = _contextSelector.BuildContextPreview(request.ProjectPath, candidateFiles);

        var draftInput = request.Prompt + Environment.NewLine + context;
        var estimatedInput = _tokenEstimator.Estimate(draftInput);
        var optimizedPrompt = _promptGenerator.Generate(request, stack, intent, scope, candidateFiles, context, estimatedInput);
        estimatedInput = _tokenEstimator.Estimate(optimizedPrompt);
        optimizedPrompt = _promptGenerator.Generate(request, stack, intent, scope, candidateFiles, context, estimatedInput);

        var clarity = ScoreClarity(request.Prompt);
        var completeness = ScoreCompleteness(request.Prompt, scope, candidateFiles);
        var efficiency = ScoreEfficiency(estimatedInput, request.InputBudgetTokens, candidateFiles.Count);
        var consistency = stack.Stack == StackType.Unknown ? 65 : 95;
        var budget = ScoreBudget(estimatedInput, request.InputBudgetTokens, request.ExpectedOutputTokens);
        var score = (int)Math.Round((clarity * 0.25) + (completeness * 0.20) + (efficiency * 0.25) + (consistency * 0.15) + (budget * 0.15));

        var reviewFields = new List<string>();
        if (stack.Confidence < 85) reviewFields.Add("{{SPECIALIZATION}}");
        if (intent == PromptIntent.Unknown) reviewFields.Add("{{OBJECTIVE}}");
        if (scope == PromptScope.PullRequestReview && !LooksLikePullRequestContext(request.Prompt)) reviewFields.Add("{{PR_NUMBER_OR_DIFF}}");
        if (candidateFiles.Count == 0) reviewFields.Add("{{CANDIDATE_FILES}}");

        var strengths = new List<string>();
        if (clarity >= 80) strengths.Add("Pergunta suficientemente clara.");
        if (stack.Confidence >= 85) strengths.Add($"Stack detectada com boa confiança: {stack.Specialization}.");
        if (candidateFiles.Count > 0) strengths.Add($"{candidateFiles.Count} arquivos candidatos detectados.");

        var issues = new List<string>();
        if (clarity < 70) issues.Add("Pergunta genérica; especifique o resultado desejado.");
        if (stack.Confidence < 85) issues.Add("Stack/especialização não detectada com confiança suficiente.");
        if (scope == PromptScope.PullRequestReview && !LooksLikePullRequestContext(request.Prompt)) issues.Add("A pergunta menciona PR, mas não contém número, URL ou diff.");
        if (estimatedInput > request.InputBudgetTokens) issues.Add("Input estimado excede o orçamento configurado.");

        var suggestions = new List<string>();
        if (clarity < 70) suggestions.Add("Reescreva a pergunta indicando ação, alvo e critério de sucesso.");
        if (candidateFiles.Count == 0) suggestions.Add("Execute na raiz do projeto ou adicione arquivos candidatos manualmente.");
        if (estimatedInput > request.InputBudgetTokens) suggestions.Add("Reduza contexto ou aumente o orçamento de input.");
        if (request.ExpectedOutputTokens < 800) suggestions.Add("Considere aumentar output esperado para permitir resposta mais útil.");

        return new PromptEvaluationResult
        {
            Score = score,
            ClarityScore = clarity,
            CompletenessScore = completeness,
            ContextEfficiencyScore = efficiency,
            ConsistencyScore = consistency,
            BudgetScore = budget,
            Stack = stack.Stack,
            StackConfidence = stack.Confidence,
            Intent = intent,
            Scope = scope,
            EstimatedInputTokens = estimatedInput,
            ExpectedOutputTokens = request.ExpectedOutputTokens,
            OptimizedPrompt = optimizedPrompt,
            CandidateFiles = candidateFiles,
            HumanReviewFields = reviewFields,
            Strengths = strengths,
            Issues = issues,
            Suggestions = suggestions
        };
    }

    private static int ScoreClarity(string prompt)
    {
        var len = prompt.Trim().Length;
        var score = 50;
        if (len >= 40) score += 20;
        if (prompt.Contains('?')) score += 10;
        if (HasAny(prompt, "explique", "melhorar", "avaliar", "investigar", "corrigir", "testes", "arquitetura")) score += 20;
        if (len < 15) score -= 30;
        return Clamp(score);
    }

    private static int ScoreCompleteness(string prompt, PromptScope scope, IReadOnlyList<string> files)
    {
        var score = 60;
        if (files.Count > 0) score += 20;
        if (scope == PromptScope.PullRequestReview && !LooksLikePullRequestContext(prompt)) score -= 50;
        if (HasAny(prompt, "não", "nao", "restrição", "restricao", "sem alterar", "objetivo")) score += 10;
        return Clamp(score);
    }

    private static int ScoreEfficiency(int estimatedInput, int budget, int fileCount)
    {
        var score = 100;
        if (estimatedInput > budget) score -= 35;
        if (fileCount > 10) score -= 20;
        if (fileCount == 0) score -= 20;
        if (estimatedInput < 400) score -= 10;
        return Clamp(score);
    }

    private static int ScoreBudget(int estimatedInput, int inputBudget, int output)
    {
        var score = 80;
        if (estimatedInput <= inputBudget) score += 15;
        else score -= Math.Min(50, (estimatedInput - inputBudget) / 50);
        if (output < 600) score -= 15;
        if (output >= 1000) score += 5;
        return Clamp(score);
    }

    private static bool LooksLikePullRequestContext(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        return p.Contains("/pull/") || p.Contains("pr #") || p.Contains("pr#") || p.Contains("pull request #") || p.Contains("diff") || p.Contains("arquivo alterado");
    }

    private static bool HasAny(string text, params string[] terms) => terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));
}
