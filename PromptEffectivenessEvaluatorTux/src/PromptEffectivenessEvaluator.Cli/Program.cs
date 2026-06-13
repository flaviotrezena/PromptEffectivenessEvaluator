using PromptEffectivenessEvaluator.Core.Models;
using PromptEffectivenessEvaluator.Core.Services;
using Spectre.Console;

namespace PromptEffectivenessEvaluator.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            var options = CliOptions.Parse(args);
            var request = options.Question is null
                ? ReadInteractiveRequest(options)
                : new PromptEvaluationRequest
                {
                    ProjectPath = options.Path ?? Directory.GetCurrentDirectory(),
                    Prompt = options.Question,
                    InputBudgetTokens = options.Budget,
                    ExpectedOutputTokens = options.Output
                };

            var engine = new PromptEffectivenessEngine();
            PromptEvaluationResult? result = null;

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("[yellow]Analisando efetividade do prompt...[/]", _ =>
                {
                    result = engine.Evaluate(request);
                });

            if (result is null)
                throw new InvalidOperationException("A análise não retornou resultado.");

            RenderResult(request, result);

            if (!options.NoCopy)
            {
                if (ClipboardService.TryCopy(result.OptimizedPrompt, out var error))
                    AnsiConsole.MarkupLine("[green]✓ Prompt otimizado copiado para a área de transferência.[/]");
                else
                    AnsiConsole.MarkupLine($"[yellow]⚠ Não foi possível copiar automaticamente:[/] {Markup.Escape(error)}");
            }

            if (!string.IsNullOrWhiteSpace(options.OutFile))
            {
                File.WriteAllText(options.OutFile, result.OptimizedPrompt);
                AnsiConsole.MarkupLine($"[green]✓ Prompt salvo em:[/] {Markup.Escape(options.OutFile)}");
            }

            if (options.Question is null)
                ShowPostActions(result);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }

    private static PromptEvaluationRequest ReadInteractiveRequest(CliOptions options)
    {
        var projectPath = options.Path ?? Directory.GetCurrentDirectory();
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("PEE").Centered().Color(Color.Cyan1));
        AnsiConsole.Write(new Rule("[bold]Prompt Effectiveness Evaluator[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Projeto detectado:[/]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(projectPath)}[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Usar esta pasta como raiz do projeto?", true))
        {
            projectPath = AnsiConsole.Ask<string>("Informe a [green]pasta do projeto[/]:");
        }

        var question = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Qual prompt/pergunta você quer avaliar?[/]")
                .PromptStyle("cyan")
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("Informe uma pergunta.")
                    : ValidationResult.Success()));

        var budgetLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Orçamento de input[/]")
                .AddChoices("Econômico (~1500)", "Balanceado (~2500)", "Profundo (~5000)"));

        var budget = budgetLabel.StartsWith("Econômico") ? 1500 : budgetLabel.StartsWith("Profundo") ? 5000 : 2500;
        var output = budgetLabel.StartsWith("Profundo") ? 1500 : 1000;

        return new PromptEvaluationRequest
        {
            ProjectPath = projectPath,
            Prompt = question,
            InputBudgetTokens = budget,
            ExpectedOutputTokens = output
        };
    }

    private static void RenderResult(PromptEvaluationRequest request, PromptEvaluationResult result)
    {
        AnsiConsole.WriteLine();
        var color = result.Score >= 80 ? "green" : result.Score >= 60 ? "yellow" : "red";
        AnsiConsole.Write(new Panel($"[bold {color}]{result.Score}/100[/]")
            .Header("Prompt Score")
            .Border(BoxBorder.Rounded)
            .Padding(2, 1));

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Métricas[/]");
        table.AddColumn("Critério");
        table.AddColumn("Score");
        table.AddRow("Clareza", ScoreText(result.ClarityScore));
        table.AddRow("Completude", ScoreText(result.CompletenessScore));
        table.AddRow("Eficiência de contexto", ScoreText(result.ContextEfficiencyScore));
        table.AddRow("Consistência", ScoreText(result.ConsistencyScore));
        table.AddRow("Budget", ScoreText(result.BudgetScore));
        AnsiConsole.Write(table);

        var meta = new Table().Border(TableBorder.Simple).Title("[bold]Diagnóstico local[/]");
        meta.AddColumn("Item");
        meta.AddColumn("Valor");
        meta.AddRow("Stack", $"{result.Stack} ({result.StackConfidence}% confiança)");
        meta.AddRow("Intenção", result.Intent.ToString());
        meta.AddRow("Escopo", result.Scope.ToString());
        meta.AddRow("Input estimado", $"{result.EstimatedInputTokens} tokens");
        meta.AddRow("Output esperado", $"{result.ExpectedOutputTokens} tokens");
        AnsiConsole.Write(meta);

        RenderList("Pontos fortes", result.Strengths, "green");
        RenderList("Problemas encontrados", result.Issues, "yellow");
        RenderList("Sugestões", result.Suggestions, "cyan");
        RenderList("Campos que exigem revisão humana", result.HumanReviewFields, "red");
        RenderList("Arquivos candidatos", result.CandidateFiles, "grey");

        AnsiConsole.Write(new Rule("[bold]Prompt otimizado[/]").RuleStyle("grey"));
        AnsiConsole.Write(new Panel(Markup.Escape(result.OptimizedPrompt))
            .Border(BoxBorder.Rounded)
            .Expand()
            .Padding(1, 1));
    }

    private static void RenderList(string title, IReadOnlyList<string> items, string color)
    {
        if (items.Count == 0) return;
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(title)}:[/]");
        foreach (var item in items)
            AnsiConsole.MarkupLine($"[{color}]•[/] {Markup.Escape(item)}");
        AnsiConsole.WriteLine();
    }

    private static string ScoreText(int score)
    {
        var color = score >= 80 ? "green" : score >= 60 ? "yellow" : "red";
        return $"[{color}]{score}/100[/]";
    }

    private static void ShowPostActions(PromptEvaluationResult result)
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("O que deseja fazer agora?")
                .AddChoices("Copiar novamente", "Salvar em prompt-otimizado.txt", "Sair"));

        if (action == "Copiar novamente")
        {
            ClipboardService.TryCopy(result.OptimizedPrompt, out _);
            AnsiConsole.MarkupLine("[green]✓ Copiado.[/]");
        }
        else if (action == "Salvar em prompt-otimizado.txt")
        {
            File.WriteAllText("prompt-otimizado.txt", result.OptimizedPrompt);
            AnsiConsole.MarkupLine("[green]✓ Arquivo salvo.[/]");
        }
    }

    private static void PrintHelp()
    {
        AnsiConsole.MarkupLine("[bold]Prompt Effectiveness Evaluator[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Uso interativo:");
        AnsiConsole.MarkupLine("  [green]pee[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Uso direto:");
        AnsiConsole.MarkupLine("  [green]pee[/] \"Como melhorar os testes do fluxo de cancelamento?\"");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Opções:");
        AnsiConsole.MarkupLine("  --path <dir>      Pasta do projeto. Padrão: pasta atual");
        AnsiConsole.MarkupLine("  --budget <n>      Orçamento de input. Padrão: 2500");
        AnsiConsole.MarkupLine("  --output <n>      Output esperado. Padrão: 1000");
        AnsiConsole.MarkupLine("  --no-copy         Não copia automaticamente");
        AnsiConsole.MarkupLine("  --out <file>      Salva o prompt otimizado");
    }
}

internal sealed class CliOptions
{
    public string? Question { get; init; }
    public string? Path { get; init; }
    public int Budget { get; init; } = 2500;
    public int Output { get; init; } = 1000;
    public bool NoCopy { get; init; }
    public string? OutFile { get; init; }

    public static CliOptions Parse(string[] args)
    {
        string? question = null;
        string? path = null;
        var budget = 2500;
        var output = 1000;
        var noCopy = false;
        string? outFile = null;
        var questionParts = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--path": path = args[++i]; break;
                case "--budget": budget = int.Parse(args[++i]); break;
                case "--output": output = int.Parse(args[++i]); break;
                case "--no-copy": noCopy = true; break;
                case "--out": outFile = args[++i]; break;
                default: questionParts.Add(arg); break;
            }
        }

        if (questionParts.Count > 0) question = string.Join(' ', questionParts);
        return new CliOptions { Question = question, Path = path, Budget = budget, Output = output, NoCopy = noCopy, OutFile = outFile };
    }
}

internal static class ClipboardService
{
    public static bool TryCopy(string text, out string error)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return RunClipboardCommand("cmd.exe", "/c clip", text, out error);
            if (OperatingSystem.IsMacOS())
                return RunClipboardCommand("pbcopy", string.Empty, text, out error);
            if (RunClipboardCommand("xclip", "-selection clipboard", text, out error))
                return true;
            return RunClipboardCommand("xsel", "--clipboard --input", text, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool RunClipboardCommand(string fileName, string arguments, string input, out string error)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.StandardInput.Write(input);
            process.StandardInput.Close();
            process.WaitForExit(3000);
            error = process.StandardError.ReadToEnd();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
