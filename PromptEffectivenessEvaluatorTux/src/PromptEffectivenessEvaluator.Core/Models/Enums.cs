namespace PromptEffectivenessEvaluator.Core.Models;

public enum StackType
{
    Unknown,
    DotNet,
    JavaSpringBoot,
    NodeJavaScript,
    Python,
    Mixed
}

public enum PromptIntent
{
    Unknown,
    ExplainCode,
    Testing,
    BugInvestigation,
    Refactoring,
    ArchitectureReview,
    Performance,
    Security,
    Documentation
}

public enum PromptScope
{
    Unknown,
    LocalQuestion,
    ModuleAnalysis,
    RepositoryOverview,
    PullRequestReview,
    DocumentationExtraction
}
