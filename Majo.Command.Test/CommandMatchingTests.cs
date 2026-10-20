using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies literal matching, typed arguments, and command matching errors.
/// </summary>
public sealed class CommandMatchingTests
{
    /// <summary>
    /// Verifies that literal matching ignores case without changing parameter text.
    /// </summary>
    [Fact]
    public void LiteralMatching_IsCaseInsensitiveAndPreservesArgumentCase()
    {
        var match = TestCommandTrees.Create().Match("SERVICE CREATE MyService CapabilityA PermissionB");

        Assert.True(match.Success);
        Assert.Equal("MyService", match.Get<string>("serviceId"));
        Assert.Equal("CapabilityA", match.Get<string>("capability"));
        Assert.Equal("PermissionB", match.Get<string>("permission"));
    }

    /// <summary>
    /// Verifies that typed arguments are parsed between literal command segments.
    /// </summary>
    [Fact]
    public void TypedArgument_CanAppearBeforeNestedLiterals()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("account", account => account.Argument<long>("qq", qq => qq.Literal("name", name =>
            name.Literal("set", set => set.Argument("value", value => value.Executes((_, _) => Task.CompletedTask))))));

        var match = builder.Build().Match("ACCOUNT 1234567 NAME SET Alice");

        Assert.True(match.Success);
        Assert.Equal(1234567L, match.Get<long>("qq"));
        Assert.Equal("Alice", match.Get<string>("value"));
    }

    /// <summary>
    /// Verifies that a literal branch is preferred when an argument branch also accepts the token.
    /// </summary>
    [Fact]
    public void LiteralBranch_TakesPrecedenceOverArgumentBranch()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("status", node => node.Executes((_, _) => Task.CompletedTask));
        builder.Argument("value", node => node.Executes((_, _) => Task.CompletedTask));
        var tree = builder.Build();

        var literal = tree.Match("STATUS");
        var argument = tree.Match("other");

        Assert.True(literal.Success);
        Assert.False(literal.TryGet<string>("value", out _));
        Assert.True(argument.Success);
        Assert.Equal("other", argument.Get<string>("value"));
    }

    /// <summary>
    /// Verifies that completely blank command input is rejected.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  \t  ")]
    public void EmptyInput_ReturnsSpecificError(string input)
    {
        var match = TestCommandTrees.Create().Match(input);

        Assert.False(match.Success);
        Assert.Equal(CommandError.EmptyInput, match.Error);
        Assert.NotNull(match.ErrorMessage);
    }

    /// <summary>
    /// Verifies that unknown literal commands produce the expected error.
    /// </summary>
    [Fact]
    public void UnknownLiteral_ReturnsUnknownCommand()
    {
        var match = TestCommandTrees.Create().Match("something create item");

        Assert.Equal(CommandError.UnknownCommand, match.Error);
    }

    /// <summary>
    /// Verifies that inputs ending at an incomplete command branch are rejected.
    /// </summary>
    [Theory]
    [InlineData("account")]
    [InlineData("account create")]
    [InlineData("account create 1234567")]
    [InlineData("service create")]
    public void IncompleteCommand_ReturnsSpecificError(string input)
    {
        var match = TestCommandTrees.Create().Match(input);

        Assert.Equal(CommandError.IncompleteCommand, match.Error);
    }

    /// <summary>
    /// Verifies that an invalid value for a typed parameter does not succeed.
    /// </summary>
    [Fact]
    public void InvalidTypedValue_ReturnsInvalidArgument()
    {
        var match = TestCommandTrees.Create().Match("account create not-a-number Password1");

        Assert.Equal(CommandError.InvalidArgument, match.Error);
        Assert.Contains("qq", match.ErrorMessage);
    }

    /// <summary>
    /// Verifies that extra tokens are rejected after a complete command.
    /// </summary>
    [Fact]
    public void UnexpectedTrailingToken_ReturnsUnexpectedArgument()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 Password1 manual extra");

        Assert.Equal(CommandError.UnexpectedArgument, match.Error);
    }

    /// <summary>
    /// Verifies that failed matches do not expose partially parsed arguments.
    /// </summary>
    [Fact]
    public void FailedMatch_DoesNotReturnPartialArguments()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567");

        Assert.False(match.Success);
        Assert.False(match.TryGet<long>("qq", out _));
    }
}
