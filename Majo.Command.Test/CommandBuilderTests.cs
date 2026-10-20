using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies command registration constraints and the one-time freeze operation.
/// </summary>
public sealed class CommandBuilderTests
{
    /// <summary>
    /// Verifies that the same case-insensitive literal can be configured in more than one registration call.
    /// </summary>
    [Fact]
    public void RepeatedLiteralName_ReusesExistingBranch()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("account", account => account.Literal("create", create =>
            create.Executes((_, _) => Task.CompletedTask)));
        builder.Literal("ACCOUNT", account => account.Literal("delete", delete =>
            delete.Executes((_, _) => Task.CompletedTask)));
        var tree = builder.Build();

        Assert.True(tree.Match("account create").Success);
        Assert.True(tree.Match("account delete").Success);
    }

    /// <summary>
    /// Verifies that a builder cannot be used to build the tree a second time.
    /// </summary>
    [Fact]
    public void Build_RejectsSecondInvocation()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("ping", ping => ping.Executes((_, _) => Task.CompletedTask));
        _ = builder.Build();

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    /// <summary>
    /// Verifies that previously captured branch builders reject modification after a freeze.
    /// </summary>
    [Fact]
    public void FrozenBranch_RejectsFurtherRegistration()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        CommandBranchBuilder<CommandContext>? captured = null;
        builder.Literal("ping", ping =>
        {
            captured = ping;
            ping.Executes((_, _) => Task.CompletedTask);
        });
        _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() => captured!.Literal("another", _ => { }));
        Assert.Throws<InvalidOperationException>(() => captured!.Executes((_, _) => Task.CompletedTask));
    }

    /// <summary>
    /// Verifies that a node cannot register two execution handlers.
    /// </summary>
    [Fact]
    public void DuplicateHandler_IsRejected()
    {
        CommandTreeBuilder<CommandContext> builder = new();

        Assert.Throws<InvalidOperationException>(() => builder.Literal("ping", ping =>
        {
            ping.Executes((_, _) => Task.CompletedTask);
            ping.Executes((_, _) => Task.CompletedTask);
        }));
    }

    /// <summary>
    /// Verifies that a node cannot register multiple argument branches.
    /// </summary>
    [Fact]
    public void MultipleArgumentBranches_AreRejected()
    {
        CommandTreeBuilder<CommandContext> builder = new();

        Assert.Throws<InvalidOperationException>(() => builder.Literal("item", item =>
        {
            item.Argument("id", id => id.Executes((_, _) => Task.CompletedTask));
            item.Argument("name", name => name.Executes((_, _) => Task.CompletedTask));
        }));
    }

    /// <summary>
    /// Verifies that literal names cannot contain whitespace.
    /// </summary>
    [Fact]
    public void LiteralName_WithWhitespaceIsRejected()
    {
        CommandTreeBuilder<CommandContext> builder = new();

        Assert.Throws<ArgumentException>(() => builder.Literal("two words", _ => { }));
    }

    /// <summary>
    /// Verifies that remainder arguments cannot contain additional child commands.
    /// </summary>
    [Fact]
    public void ConsumeRemainingArgument_RejectsChildNodes()
    {
        CommandTreeBuilder<CommandContext> builder = new();

        Assert.Throws<InvalidOperationException>(() => builder.Literal("help", help => help.Argument("path", path =>
            path.Literal("unexpected", _ => { }), consumeRemaining: true)));
    }
}
