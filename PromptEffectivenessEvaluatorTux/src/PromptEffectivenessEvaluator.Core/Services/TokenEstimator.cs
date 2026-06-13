namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class TokenEstimator
{
    public int Estimate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
    }
}
