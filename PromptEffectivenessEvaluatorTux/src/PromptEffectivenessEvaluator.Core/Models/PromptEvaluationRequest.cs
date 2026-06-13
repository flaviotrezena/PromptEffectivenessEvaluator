namespace PromptEffectivenessEvaluator.Core.Models;

public sealed class PromptEvaluationRequest
{
    public required string ProjectPath { get; init; }
    public required string Prompt { get; init; }
    public int InputBudgetTokens { get; init; } = 2500;
    public int ExpectedOutputTokens { get; init; } = 1000;
}
