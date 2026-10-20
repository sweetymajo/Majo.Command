<div align="center">

# Majo.Command

**A lightweight, typed command-tree library for .NET.**

Define commands with a fluent builder, parse positional arguments, execute asynchronous handlers, and generate help from a single command tree.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

**English** · [简体中文](./README.zh-CN.md)

</div>

---

## Overview

`Majo.Command` provides command registration, parsing, and execution for .NET applications. Build a command tree from literal keywords and typed positional arguments, then reuse it to process commands from a console, chat application, or other input source.

The API focuses on a simple workflow: **register commands, build the tree, and execute input with a context**.

## Highlights

- Fluent command registration with `Literal(...)`, `Argument(...)`, and `Executes(...)`
- Case-insensitive command keywords and typed positional arguments
- Built-in parsing for types implementing `ISpanParsable<T>`
- Custom argument parsers and regular-expression validation
- Quoted arguments, including text with spaces, and trailing arguments that consume the remaining input
- Asynchronous command handlers with cancellation-token support
- Execution contexts with configurable output callbacks
- Structured matching results and typed argument access
- Automatic help generation with compact optional-argument notation
- Frozen command trees that can be shared across independent requests

## Requirements

- **.NET 10**

## Quick Start

Register a command, build the tree, and execute it:

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

Example output:

```text
[Information] [Greeting] Hello world
say <message> [tag] - Prints a message.
```

The command also accepts `say "Hello world"`, using `Command` as the default tag.

## Registering Commands

### Literals and typed arguments

`Literal(...)` adds a command keyword. `Argument(...)` adds a string argument, while `Argument<T>(...)` parses a specified type using `ISpanParsable<T>` and invariant culture.

For example, a command can combine multiple keywords with a numeric parameter:

```csharp
builder.Literal("user", user =>
{
    user.Literal("show", show =>
    {
        show.Argument<long>("id", id =>
        {
            id.Executes((call, ct) =>
            {
                long userId = call.Get<long>("id");
                call.Context.Output(OutputLevel.Information, $"User ID: {userId}", "User");
                return Task.CompletedTask;
            }, "Shows a user ID.");
        });
    });
});
```

Register all branches before calling `Build()`. Literal names are matched without regard to case. Each branch can contain multiple literal children and one positional argument child; literal matches take priority.

### Optional trailing arguments

A command can end at any node where `Executes(...)` is registered. Register the same handler at successive argument nodes to accept both shorter and longer command forms:

```text
say "Hello world"
say "Hello world" Greeting
```

The corresponding help entry presents the trailing argument as optional: `say <message> [tag]`.

### Custom parsing and validation

Use `CommandValueParser<T>` to parse a custom type. For string arguments, pass a `Regex` to validate the complete value:

```csharp
using System.Text.RegularExpressions;

builder.Literal("lookup", lookup =>
{
    lookup.Argument("code", code =>
    {
        code.Executes((call, ct) =>
        {
            call.Context.Output(OutputLevel.Information, call.Get<string>("code"), "Lookup");
            return Task.CompletedTask;
        });
    }, regex: new Regex("[A-Z]{3}[0-9]{2}"));
});
```

For a final free-text argument, set `consumeRemaining: true` to capture the remaining input as a single value.

## Matching and Execution

`Match(...)` parses a command and returns its arguments and matching status:

```csharp
CommandMatch<CommandContext> match = tree.Match("say hello");

if (match.Success)
    Console.WriteLine(match.Get<string>("message"));
else
    Console.Error.WriteLine($"{match.Error}: {match.ErrorMessage}");
```

`Get<T>(name)` reads a parsed argument by name. `TryGet<T>(name, out value)` is convenient for arguments that may be omitted.

Use `ExecuteAsync(input, context, cancellationToken)` to match input and invoke its registered asynchronous handler. The returned `CommandMatch<TContext>` reports the matching outcome. Handler exceptions propagate to the caller, where they can be handled according to the application.

Matching failures are classified by `CommandError`, including unknown commands, invalid arguments, incomplete commands, unexpected arguments, and invalid syntax.

## Help Generation

`GetHelp()` generates usage information from registered handlers and their descriptions:

```csharp
tree.GetHelp();
tree.GetHelp("user");
```

The optional filter selects commands by their literal keyword path. Consecutive executable argument nodes are presented in a compact form, such as:

```text
say <message> [tag] - Prints a message.
```

To expose this information through a `help` command, register a handler that calls `GetHelp()`.

## Execution Context

A `CommandContext` carries an output callback that receives an `OutputLevel`, message text, and tag:

```csharp
var context = new CommandContext((level, text, tag) =>
{
    Console.WriteLine($"[{level}] [{tag}] {text}");
});
```

Applications can connect this callback to their logging or messaging infrastructure. A custom context derived from `CommandContext` can also carry application-specific data for handlers.

Once built, a command tree can be reused across requests. Each match has its own parsed argument values and can be executed with a separate context, making the tree suitable for applications that process multiple independent commands.

## License

See [LICENSE.txt](./LICENSE.txt).

---

<div align="center">

Built as a focused command component for the Majo project family.

[简体中文](./README.zh-CN.md)

</div>
