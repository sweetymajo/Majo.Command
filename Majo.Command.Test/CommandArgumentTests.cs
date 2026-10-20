using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Majo.Command.Test;

/// <summary>
/// Verifies optional, typed, custom-parsed, and regular-expression arguments.
/// </summary>
public sealed class CommandArgumentTests
{
    /// <summary>
    /// Verifies that a required parameter is available and absent optional parameters are reported as missing.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsFalseForOmittedOptionalParameters()
    {
        var match = TestCommandTrees.Create().Match("service create example");

        Assert.True(match.Success);
        Assert.Equal("example", match.Get<string>("serviceId"));
        Assert.False(match.TryGet<string>("capability", out var capability));
        Assert.Null(capability);
        Assert.False(match.TryGet<string>("permission", out _));
    }

    /// <summary>
    /// Verifies that optional parameters can be supplied independently by extending the command path.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsProvidedOptionalParameters()
    {
        var match = TestCommandTrees.Create().Match("service create example FeatureA RoleB");

        Assert.True(match.TryGet<string>("CAPABILITY", out var capability));
        Assert.Equal("FeatureA", capability);
        Assert.Equal("RoleB", match.Get<string>("permission"));
    }

    /// <summary>
    /// Verifies that a missing required argument raises an explicit lookup error.
    /// </summary>
    [Fact]
    public void Get_MissingArgumentThrows()
    {
        var match = TestCommandTrees.Create().Match("service create example");

        Assert.Throws<KeyNotFoundException>(() => match.Get<string>("capability"));
    }

    /// <summary>
    /// Verifies that an incorrect requested argument type is not treated as an omitted argument.
    /// </summary>
    [Fact]
    public void TryGet_WrongTypeThrows()
    {
        var match = TestCommandTrees.Create().Match("account create 1234567 Password1");

        Assert.Throws<InvalidCastException>(() => match.TryGet<string>("qq", out _));
        Assert.Throws<InvalidCastException>(() => match.Get<string>("qq"));
    }

    /// <summary>
    /// Verifies that a custom Span-based parser converts a hexadecimal value.
    /// </summary>
    [Fact]
    public void CustomParser_ConvertsInput()
    {
        CommandTreeBuilder<CommandContext> builder = new();
        builder.Literal("hex", hex => hex.Argument<int>("value", ParseHex,
            value => value.Executes((_, _) => Task.CompletedTask)));
        var tree = builder.Build();

        Assert.Equal(255, tree.Match("hex FF").Get<int>("value"));
        Assert.Equal(CommandError.InvalidArgument, tree.Match("hex ZZ").Error);

        static bool ParseHex(ReadOnlySpan<char> input, out int value)
            => int.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Verifies that regular-expression validation accepts a complete match only.
    /// </summary>
    [Theory]
    [InlineData("code AB123", true)]
    [InlineData("code A123", false)]
    [InlineData("code AB123suffix", false)]
    [InlineData("code xxAB123", false)]
    public void RegexArgument_ValidatesEntireToken(string input, bool expected)
    {
        CommandTreeBuilder<CommandContext> builder = new();
        Regex regex = new(@"[A-Z]{2}\d{3}", RegexOptions.CultureInvariant);
        builder.Literal("code", code => code.Argument("value", value =>
            value.Executes((_, _) => Task.CompletedTask), regex: regex));

        var match = builder.Build().Match(input);

        Assert.Equal(expected, match.Success);
        if (!expected) Assert.Equal(CommandError.InvalidArgument, match.Error);
    }

    /// <summary>
    /// Verifies that a remainder argument consumes a multi-word literal path.
    /// </summary>
    [Fact]
    public void ConsumeRemaining_CombinesUnquotedTrailingTokens()
    {
        var match = TestCommandTrees.Create().Match("help service create");

        Assert.True(match.Success);
        Assert.Equal("service create", match.Get<string>("path"));
    }

    /// <summary>
    /// Verifies that a quoted, last-position remainder is returned without surrounding quotes.
    /// </summary>
    [Fact]
    public void ConsumeRemaining_DecodesLoneQuotedToken()
    {
        var match = TestCommandTrees.Create().Match("help \"service create\"");

        Assert.True(match.Success);
        Assert.Equal("service create", match.Get<string>("path"));
    }
}
