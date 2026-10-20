using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies that multiple requests can execute against the same frozen command tree without sharing request state.
/// </summary>
public sealed class CommandConcurrencyTests
{
    /// <summary>
    /// Verifies that independent contexts receive only their own output during concurrent command execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConcurrentRequestsKeepParametersAndOutputIsolated()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("request", request => request.Argument<int>("id", id => id.Executes(async (call, _) =>
        {
            int requestId = call.Get<int>("id");
            await Task.Yield();
            call.Context.Output(OutputLevel.Information, requestId.ToString(), "Request");
        })));
        var tree = builder.Build();

        var requests = Enumerable.Range(0, 64).Select(async id =>
        {
            var output = new List<(OutputLevel Level, string Text, string Tag)>();
            var context = TestCommandTrees.CreateContext(output);
            var match = await tree.ExecuteAsync($"request {id}", context, TestContext.Current.CancellationToken);
            Assert.True(match.Success);
            Assert.Equal(id, match.Get<int>("id"));
            Assert.Equal((OutputLevel.Information, id.ToString(), "Request"), Assert.Single(output));
        });

        await Task.WhenAll(requests);
    }
}
