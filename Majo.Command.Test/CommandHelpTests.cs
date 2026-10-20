using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies help filtering, descriptions, and compact optional-argument usage formatting.
/// </summary>
public sealed class CommandHelpTests
{
    /// <summary>
    /// Verifies that help lists all commands and merges consecutive optional argument nodes.
    /// </summary>
    [Fact]
    public void GetHelp_CollapsesOptionalArgumentChains()
    {
        var help = TestCommandTrees.Create().GetHelp();
        string[] lines = help.Split(Environment.NewLine);

        Assert.Equal(3, lines.Length);
        Assert.Equal("account create <qq> <password> [createSource] - Create an account with an optional source",
            lines[0]);
        Assert.Equal("help [path] - Show command help", lines[1]);
        Assert.Equal("service create <serviceId> [capability] [permission] - " +
            "Create a service with optional capabilities and permissions", lines[2]);
    }

    /// <summary>
    /// Verifies that help can be filtered by case-insensitive literal paths.
    /// </summary>
    [Fact]
    public void GetHelp_FiltersLiteralPathsWithoutCaseSensitivity()
    {
        var help = TestCommandTrees.Create().GetHelp("SERVICE CREATE");

        Assert.Equal("service create <serviceId> [capability] [permission] - " +
            "Create a service with optional capabilities and permissions", help);
    }

    /// <summary>
    /// Verifies that help returns a clear message for an unknown literal path.
    /// </summary>
    [Fact]
    public void GetHelp_UnknownPathReturnsNoCommandsFound()
    {
        Assert.Equal("No commands found.", TestCommandTrees.Create().GetHelp("missing"));
    }

    /// <summary>
    /// Verifies that help does not merge an executable argument chain that has a literal branch.
    /// </summary>
    [Fact]
    public void GetHelp_PreservesLiteralBranchesWhenOptionalArgumentsExist()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("item", item => item.Argument("id", id =>
        {
            id.Executes((_, _) => Task.CompletedTask, "Show item");
            id.Literal("delete", delete => delete.Executes((_, _) => Task.CompletedTask, "Delete item"));
            id.Argument("detail", detail => detail.Executes((_, _) => Task.CompletedTask, "Show detail"));
        }));

        string help = builder.Build().GetHelp();

        Assert.Contains("item <id> - Show item", help);
        Assert.Contains("item <id> delete - Delete item", help);
        Assert.Contains("item <id> <detail> - Show detail", help);
        Assert.DoesNotContain("item <id> [detail]", help);
    }
}
