using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies handler invocation, output routing, and execution-time failures.
/// </summary>
public sealed class CommandExecutionTests
{
    /// <summary>
    /// Verifies that service commands use empty values for omitted optional parameters.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ServiceWithoutOptionalsProducesExpectedOutput()
    {
        var outputs = new List<(OutputLevel Level, string Text, string Tag)>();
        var context = TestCommandTrees.CreateContext(outputs);

        var match = await TestCommandTrees.Create().ExecuteAsync("service create test", context,
            TestContext.Current.CancellationToken);

        Assert.True(match.Success);
        Assert.Equal((OutputLevel.Information, "test||", "Service"), Assert.Single(outputs));
    }

    /// <summary>
    /// Verifies that a provided optional source is passed through the execution context.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AccountWithOptionalSourceProducesExpectedOutput()
    {
        var outputs = new List<(OutputLevel Level, string Text, string Tag)>();
        var context = TestCommandTrees.CreateContext(outputs);

        var match = await TestCommandTrees.Create().ExecuteAsync("account create 1234567 Password1 imported", context,
            TestContext.Current.CancellationToken);

        Assert.True(match.Success);
        Assert.Equal((OutputLevel.Information, "1234567|Password1|imported", "Account"), Assert.Single(outputs));
    }

    /// <summary>
    /// Verifies that failed command matching does not invoke a handler or send output.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_UnmatchedInputDoesNotInvokeHandler()
    {
        var outputs = new List<(OutputLevel Level, string Text, string Tag)>();
        var context = TestCommandTrees.CreateContext(outputs);

        var match = await TestCommandTrees.Create().ExecuteAsync("missing command", context,
            TestContext.Current.CancellationToken);

        Assert.False(match.Success);
        Assert.Empty(outputs);
    }

    /// <summary>
    /// Verifies that handlers receive the original command text and the requested output level and tag.
    /// </summary>
    [Fact]
    public async Task Handler_ReceivesOriginalInputAndOutputCallback()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("echo", echo => echo.Executes((call, _) =>
        {
            call.Context.Output(OutputLevel.Error, call.Input, "Echo");
            return Task.CompletedTask;
        }));
        var outputs = new List<(OutputLevel Level, string Text, string Tag)>();

        await builder.Build().ExecuteAsync("EcHo", TestCommandTrees.CreateContext(outputs),
            TestContext.Current.CancellationToken);

        Assert.Equal((OutputLevel.Error, "EcHo", "Echo"), Assert.Single(outputs));
    }

    /// <summary>
    /// Verifies that a handler receives the cancellation token supplied by its caller.
    /// </summary>
    [Fact]
    public async Task Handler_ReceivesCancellationToken()
    {
        CancellationToken received = CancellationToken.None;
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("check", node => node.Executes((_, token) =>
        {
            received = token;
            return Task.CompletedTask;
        }));
        using CancellationTokenSource cts = new();

        await builder.Build().ExecuteAsync("check", new CommandContext((_, _, _) => { }), cts.Token);

        Assert.Equal(cts.Token, received);
    }

    /// <summary>
    /// Verifies that matching a command does not execute it until ExecuteAsync is called.
    /// </summary>
    [Fact]
    public async Task Match_AndExecuteAsyncAreSeparateOperations()
    {
        int count = 0;
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("increment", node => node.Executes((_, _) =>
        {
            count++;
            return Task.CompletedTask;
        }));
        var match = builder.Build().Match("increment");

        Assert.True(match.Success);
        Assert.Equal(0, count);
        await match.ExecuteAsync(new CommandContext((_, _, _) => { }), TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    /// <summary>
    /// Verifies that handler exceptions remain visible to the invoking application.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PropagatesHandlerExceptions()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("fail", node => node.Executes((_, _) =>
            Task.FromException(new InvalidOperationException("Handler failure"))));
        var tree = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => tree.ExecuteAsync(
            "fail", new CommandContext((_, _, _) => { }), TestContext.Current.CancellationToken));

        Assert.Equal("Handler failure", exception.Message);
    }

    /// <summary>
    /// Verifies that an unsuccessful match cannot execute a handler.
    /// </summary>
    [Fact]
    public async Task FailedMatch_CannotExecute()
    {
        var match = TestCommandTrees.Create().Match("invalid-command");

        await Assert.ThrowsAsync<InvalidOperationException>(() => match.ExecuteAsync(
            new CommandContext((_, _, _) => { }), TestContext.Current.CancellationToken));
    }
}
