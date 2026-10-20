using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies the public parsing behavior of the command token reader through command matching.
/// </summary>
public sealed class CommandTokenTests
{
    /// <summary>
    /// Verifies that quoted values preserve internal spaces while removing surrounding quotes.
    /// </summary>
    [Fact]
    public void QuotedArgument_PreservesSpaces()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 \"Password With Spaces\"");

        Assert.True(match.Success);
        Assert.Equal("Password With Spaces", match.Get<string>("password"));
    }

    /// <summary>
    /// Verifies that escaped quotation marks are decoded inside quoted input.
    /// </summary>
    [Fact]
    public void EscapedQuotes_AreDecoded()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 \"say \\\"hello\\\"\"");

        Assert.True(match.Success);
        Assert.Equal("say \"hello\"", match.Get<string>("password"));
    }

    /// <summary>
    /// Verifies that escaped backslashes are decoded inside quoted input.
    /// </summary>
    [Fact]
    public void EscapedBackslashes_AreDecoded()
    {
        var match = TestCommandTrees.Create().Match(@"account create 1234567 ""C:\\Temp""");

        Assert.True(match.Success);
        Assert.Equal(@"C:\Temp", match.Get<string>("password"));
    }

    /// <summary>
    /// Verifies that a closing quote must be followed by whitespace or the end of input.
    /// </summary>
    [Fact]
    public void ClosingQuoteFollowedByText_IsInvalidSyntax()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 \"hello\"world");

        Assert.Equal(CommandError.InvalidSyntax, match.Error);
    }

    /// <summary>
    /// Verifies that an unterminated quoted value returns a syntax error.
    /// </summary>
    [Fact]
    public void UnterminatedQuote_IsInvalidSyntax()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 \"hello");

        Assert.Equal(CommandError.InvalidSyntax, match.Error);
    }

    /// <summary>
    /// Verifies that excess whitespace between parameters is ignored.
    /// </summary>
    [Fact]
    public void WhitespaceBetweenTokens_IsIgnored()
    {
        var match = TestCommandTrees.Create().Match("  account   create\t1234567   Password1  ");

        Assert.True(match.Success);
        Assert.Equal(1234567L, match.Get<long>("qq"));
        Assert.Equal("Password1", match.Get<string>("password"));
    }
}
