using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies that applications can extend command contexts without changing the command library.
/// </summary>
public sealed class CommandContextTests
{
    /// <summary>
    /// Verifies that a handler can access a caller-specific context and route output through its callback.
    /// </summary>
    [Fact]
    public async Task GenericContext_PreservesCallerSpecificInformation()
    {
        CommandTreeBuilder<TestOriginContext> builder = new();
        builder.Literal("who", who => who.Executes((call, _) =>
        {
            call.Context.Output(OutputLevel.Information, call.Context.Origin, "Origin");
            return Task.CompletedTask;
        }));
        var output = new List<string>();
        TestOriginContext context = new("group-123", (level, text, tag) => output.Add($"{level}|{tag}|{text}"));

        var match = await builder.Build().ExecuteAsync("who", context, TestContext.Current.CancellationToken);

        Assert.True(match.Success);
        Assert.Equal("Information|Origin|group-123", Assert.Single(output));
    }
}
