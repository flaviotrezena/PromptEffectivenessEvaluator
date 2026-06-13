using System.Text;
using PromptEffectivenessEvaluator.Core.Models;

namespace PromptEffectivenessEvaluator.Core.Services;

public sealed class PromptGenerator
{
    public string Generate(PromptEvaluationRequest request, StackDetectionResult stack, PromptIntent intent, PromptScope scope, IReadOnlyList<string> candidateFiles, string context, int estimatedInputTokens)
    {
        var specialization = stack.Confidence >= 85 ? stack.Specialization : "{{SPECIALIZATION}}";
        var objective = ObjectiveFor(intent, scope);
        var question = string.IsNullOrWhiteSpace(request.Prompt) ? "{{QUESTION}}" : request.Prompt.Trim();
        var projectName = TryGetProjectName(request.ProjectPath);

        var sb = new StringBuilder();
        sb.AppendLine($"Você é um engenheiro de software sênior especializado em {specialization}.");
        sb.AppendLine();
        sb.AppendLine("OBJETIVO:");
        sb.AppendLine(objective);
        sb.AppendLine();
        sb.AppendLine("PROJETO:");
        sb.AppendLine(projectName);
        sb.AppendLine();
        sb.AppendLine("PERGUNTA DO ENGENHEIRO:");
        sb.AppendLine(question);
        sb.AppendLine();
        sb.AppendLine("REGRAS DE CONFIANÇA:");
        sb.AppendLine("1. Priorize código-fonte, configurações e testes sobre documentação Markdown.");
        sb.AppendLine("2. Se documentação e código divergirem, aponte explicitamente a divergência.");
        sb.AppendLine("3. Não assuma que arquivos .md estão atualizados.");
        sb.AppendLine("4. Não invente informações ausentes; indique lacunas quando necessário.");
        sb.AppendLine();
        sb.AppendLine("ARQUIVOS CANDIDATOS DETECTADOS:");
        if (candidateFiles.Count == 0)
            sb.AppendLine("- {{CANDIDATE_FILES}}");
        else
            foreach (var file in candidateFiles) sb.AppendLine($"- {file}");
        sb.AppendLine();
        sb.AppendLine("FORMATO ESPERADO DA RESPOSTA:");
        foreach (var line in ResponseFormatFor(intent, scope)) sb.AppendLine(line);
        sb.AppendLine("6. INFORMAÇÕES DE ORÇAMENTO");
        sb.AppendLine($"   - Input estimado do prompt: {estimatedInputTokens} tokens");
        sb.AppendLine($"   - Output esperado: {request.ExpectedOutputTokens} tokens");
        sb.AppendLine($"   - Budget total estimado: {estimatedInputTokens + request.ExpectedOutputTokens} tokens");
        sb.AppendLine("7. EFICIÊNCIA DO CONTEXTO");
        sb.AppendLine("   - Avalie se o contexto fornecido foi suficiente para responder.");
        sb.AppendLine("   - Indique quais partes do contexto foram mais relevantes.");
        sb.AppendLine("   - Identifique informações redundantes ou que poderiam ser removidas.");
        sb.AppendLine("   - Indique informações ausentes que melhorariam a resposta.");
        sb.AppendLine("   - Avalie qualitativamente se o uso de tokens foi eficiente.");
        sb.AppendLine();
        sb.AppendLine("CONTEXTO SELECIONADO:");
        sb.AppendLine(context);
        return sb.ToString();
    }

    private static string ObjectiveFor(PromptIntent intent, PromptScope scope)
    {
        if (scope == PromptScope.PullRequestReview)
            return "Avaliar mudanças de Pull Request com base em diff, arquivos alterados e evidências explícitas. Se o diff/PR não estiver no contexto, informe a lacuna.";
        return intent switch
        {
            PromptIntent.Testing => "Melhorar a efetividade dos testes com menor alteração segura possível.",
            PromptIntent.BugInvestigation => "Investigar causas prováveis do problema e indicar evidências necessárias antes de propor código.",
            PromptIntent.Refactoring => "Sugerir refatorações pequenas, seguras e justificadas por evidência no código.",
            PromptIntent.ArchitectureReview => "Avaliar arquitetura, responsabilidades e acoplamentos com base no contexto fornecido.",
            PromptIntent.Performance => "Avaliar possíveis gargalos de performance com base em evidências no código/contexto.",
            PromptIntent.Security => "Avaliar riscos de segurança com base em evidências no código/configuração.",
            PromptIntent.Documentation => "Extrair documentação técnica objetiva baseada no código atual.",
            PromptIntent.ExplainCode => "Explicar o funcionamento do código ou projeto com base no contexto selecionado.",
            _ => "{{OBJECTIVE}}"
        };
    }

    private static IReadOnlyList<string> ResponseFormatFor(PromptIntent intent, PromptScope scope)
    {
        if (scope == PromptScope.DocumentationExtraction)
        {
            return new[]
            {
                "1. Resumo executivo",
                "2. Componentes e responsabilidades",
                "3. Fluxo de dados",
                "4. Evidências por arquivo",
                "5. Lacunas, riscos ou divergências"
            };
        }

        if (intent == PromptIntent.Testing)
        {
            return new[]
            {
                "1. Diagnóstico da cobertura atual",
                "2. Cenários de teste ausentes",
                "3. Priorização por risco",
                "4. Arquivos/testes a alterar",
                "5. Código somente se for necessário"
            };
        }

        return new[]
        {
            "1. Diagnóstico objetivo",
            "2. Evidências encontradas no código/contexto",
            "3. Riscos ou divergências identificadas",
            "4. Próximas ações sugeridas",
            "5. Código somente se for necessário"
        };
    }

    private static string TryGetProjectName(string path)
    {
        var name = new DirectoryInfo(path).Name;
        return string.IsNullOrWhiteSpace(name) ? "{{PROJECT_NAME}}" : name;
    }
}
