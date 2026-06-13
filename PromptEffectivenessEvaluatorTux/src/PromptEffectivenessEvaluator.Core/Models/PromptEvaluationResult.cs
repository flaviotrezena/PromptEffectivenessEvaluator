namespace PromptEffectivenessEvaluator.Core.Models;

public sealed class PromptEvaluationResult
{
    public int Score { get; init; }
    public int ClarityScore { get; init; }
    public int CompletenessScore { get; init; }
    public int ContextEfficiencyScore { get; init; }
    public int ConsistencyScore { get; init; }
    public int BudgetScore { get; init; }
    public StackType Stack { get; init; }
    public int StackConfidence { get; init; }
    public PromptIntent Intent { get; init; }
    public PromptScope Scope { get; init; }
    public int EstimatedInputTokens { get; init; }
    public int ExpectedOutputTokens { get; init; }
    public string OptimizedPrompt { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateFiles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> HumanReviewFields { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}
