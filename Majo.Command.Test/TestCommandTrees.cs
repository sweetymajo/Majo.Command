namespace Majo.Command.Test;

/// <summary>
/// Creates in-memory command trees that reproduce the service and account command structures used by real consumers.
/// </summary>
internal static class TestCommandTrees
{
    /// <summary>
    /// Creates a frozen command tree with optional trailing arguments, typed parameters, and help commands.
    /// </summary>
    /// <returns>A command tree that has no external service or database dependencies.</returns>
    internal static CommandTree<CommandContext> Create()
    {
        CommandTreeBuilder<CommandContext> builder = new();

        builder.Literal("service", service => service.Literal("create", create => create.Argument("serviceId", id =>
        {
            id.Executes(CreateServiceAsync, "Create a service with optional capabilities and permissions");
            id.Argument("capability", capability =>
            {
                capability.Executes(CreateServiceAsync);
                capability.Argument("permission", permission => permission.Executes(CreateServiceAsync));
            });
        })));

        builder.Literal("account", account => account.Literal("create", create => create.Argument<long>("qq", qq =>
        {
            qq.Argument("password", password =>
            {
                password.Executes(CreateAccountAsync, "Create an account with an optional source");
                password.Argument("createSource", source => source.Executes(CreateAccountAsync));
            });
        })));

        builder.Literal("help", help =>
        {
            help.Executes((_, _) => Task.CompletedTask, "Show command help");
            help.Argument("path", path => path.Executes((_, _) => Task.CompletedTask), consumeRemaining: true);
        });

        return builder.Build();
    }

    /// <summary>
    /// Creates an execution context that records output in an in-memory collection.
    /// </summary>
    /// <param name="output">The collection that receives command output.</param>
    /// <returns>A context that has no dependency on a terminal or logger.</returns>
    internal static CommandContext CreateContext(List<(OutputLevel Level, string Text, string Tag)> output)
        => new((level, text, tag) => output.Add((level, text, tag)));

    /// <summary>
    /// Formats a service creation request without invoking a real service.
    /// </summary>
    /// <param name="call">The invocation containing parsed arguments and the caller's output context.</param>
    /// <param name="ct">The cancellation token passed to the handler.</param>
    /// <returns>A completed task.</returns>
    private static Task CreateServiceAsync(CommandInvocation<CommandContext> call, CancellationToken ct)
    {
        string serviceId = call.Get<string>("serviceId");
        call.TryGet<string>("capability", out var capability);
        call.TryGet<string>("permission", out var permission);
        call.Context.Output(OutputLevel.Information, $"{serviceId}|{capability ?? ""}|{permission ?? ""}", "Service");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Formats an account creation request without invoking a real service.
    /// </summary>
    /// <param name="call">The invocation containing parsed arguments and the caller's output context.</param>
    /// <param name="ct">The cancellation token passed to the handler.</param>
    /// <returns>A completed task.</returns>
    private static Task CreateAccountAsync(CommandInvocation<CommandContext> call, CancellationToken ct)
    {
        long qq = call.Get<long>("qq");
        string password = call.Get<string>("password");
        call.TryGet<string>("createSource", out var source);
        call.Context.Output(OutputLevel.Information, $"{qq}|{password}|{source ?? "manual"}", "Account");
        return Task.CompletedTask;
    }
}
