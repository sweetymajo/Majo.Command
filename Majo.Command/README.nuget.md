# Majo.Command

A lightweight, typed command-tree library for .NET.

`Majo.Command` provides declarative command registration, positional argument parsing, asynchronous execution, and help generation. A single command tree can serve commands from console applications, chat services, and other input sources.

## Installation

```bash
dotnet add package Majo.Command
```

**Target framework:** .NET 10.

## Features

- Fluent registration of command keywords and typed positional arguments
- Case-insensitive literal matching
- Built-in and custom argument parsers, with optional regex validation
- Quoted arguments and trailing free-text arguments
- Asynchronous handlers with cancellation-token support
- Execution contexts with customizable output callbacks
- Typed argument access and structured matching results
- Automatic help generation with optional-argument notation
- Frozen command trees reusable across independent requests

## Quick Start

```csharp
using Majo.Command;

var builder = new CommandTreeBuilder<CommandContext>();

builder.Literal("say", say =>
{
    say.Argument("message", message =>
    {
        message.Executes(PrintAsync, "Prints a message.");
        message.Argument("tag", tag => tag.Executes(PrintAsync));
    });
});

var tree = builder.Build();
var context = new CommandContext((level, text, tag) => Console.WriteLine($"[{level}] [{tag}] {text}"));

var result = await tree.ExecuteAsync("say \"Hello world\" Greeting", context);
if (!result.Success)
    Console.Error.WriteLine(result.ErrorMessage);

Console.WriteLine(tree.GetHelp("say"));

static Task PrintAsync(CommandInvocation<CommandContext> call, CancellationToken ct)
{
    string message = call.Get<string>("message");
    string tag = call.TryGet<string>("tag", out var suppliedTag) ? suppliedTag : "Command";
    call.Context.Output(OutputLevel.Information, message, tag);
    return Task.CompletedTask;
}
```

The generated help entry is:

```text
say <message> [tag] - Prints a message.
```

## Command Registration

Use `Literal(...)` for command keywords and `Argument(...)` for string parameters. `Argument<T>(...)` parses values implementing `ISpanParsable<T>` with invariant culture.

For specialized formats, use `CommandValueParser<T>` or supply a `Regex` when registering a string argument. Set `consumeRemaining: true` on a final argument to capture the rest of the input.

Register `Executes(...)` at each point where a command can finish. This also lets help display consecutive trailing arguments in brackets, such as `[tag]`.

## Matching and Help

- `tree.Match(input)` returns matching status and parsed argument values.
- `tree.ExecuteAsync(input, context, ct)` matches and executes the registered handler.
- `match.Success`, `match.Error`, and `match.ErrorMessage` describe the matching result.
- `Get<T>(name)` and `TryGet<T>(name, out value)` retrieve parsed arguments.
- `tree.GetHelp()` lists executable commands; `tree.GetHelp("say")` filters by literal path.

## Execution Context

`CommandContext` accepts an output callback receiving `OutputLevel`, message text, and tag. Connect the callback to your application's output destination, such as a logger or messaging service.

For additional request-specific data, derive a custom context from `CommandContext` and use it as the type parameter to `CommandTreeBuilder<TContext>`.

Build the tree once and reuse it across requests. Each command match has its own argument values and can be executed with a separate context.
