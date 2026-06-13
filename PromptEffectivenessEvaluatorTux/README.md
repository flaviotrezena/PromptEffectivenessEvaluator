# Prompt Effectiveness Evaluator (TUX)

Ferramenta de terminal para avaliar a efetividade de prompts técnicos antes de enviá-los para uma IA, com foco em uso mais eficiente de tokens.

## Objetivo

Avaliar:

- clareza do prompt;
- completude;
- eficiência do contexto;
- consistência entre prompt e stack detectada;
- orçamento de tokens;
- campos que precisam de revisão humana.

A ferramenta usa lógica local por padrão. Ela não chama IA externa.

## Interface TUX

A execução sem argumentos abre uma experiência interativa usando `Spectre.Console`:

```powershell
dotnet run --project src/PromptEffectivenessEvaluator.Cli
```

Fluxo:

1. detecta a pasta atual como projeto;
2. pergunta se deseja usar essa pasta;
3. coleta a pergunta/prompt do engenheiro;
4. permite escolher orçamento de input;
5. mostra score e diagnóstico;
6. gera prompt otimizado;
7. copia automaticamente para a área de transferência.

## Execução direta

```powershell
dotnet run --project src/PromptEffectivenessEvaluator.Cli -- "Como melhorar os testes do fluxo de cancelamento?"
```

Com pasta específica:

```powershell
dotnet run --project src/PromptEffectivenessEvaluator.Cli -- --path "C:\repos\ProjetoX" "Como melhorar os testes?"
```

Salvar em arquivo:

```powershell
dotnet run --project src/PromptEffectivenessEvaluator.Cli -- "Explique a arquitetura" --out prompt.txt
```

Não copiar automaticamente:

```powershell
dotnet run --project src/PromptEffectivenessEvaluator.Cli -- "Explique o fluxo" --no-copy
```

## Opções

```txt
--path <dir>      Pasta do projeto. Padrão: pasta atual
--budget <n>      Orçamento de input. Padrão: 2500
--output <n>      Output esperado. Padrão: 1000
--no-copy         Não copia automaticamente
--out <file>      Salva o prompt otimizado
--help            Ajuda
```

## Princípios

- Nunca gerar `Desconhecido` no prompt final.
- Se a confiança for baixa, usar placeholders como `{{SPECIALIZATION}}`.
- Código/configuração/testes têm prioridade sobre Markdown.
- Não invocar IA externa se a lógica local resolver com confiança suficiente.
- O prompt otimizado inclui informações de orçamento e pede avaliação qualitativa da eficiência do contexto.

## Estrutura

```txt
PromptEffectivenessEvaluator.sln
src/
  PromptEffectivenessEvaluator.Core/
    Models/
    Services/
  PromptEffectivenessEvaluator.Cli/
    Program.cs
```

## Build

```powershell
dotnet restore
dotnet build
```

