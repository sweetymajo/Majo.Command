namespace Majo.Command;

/// <summary>
/// Parses argument text into a value without exposing its concrete type.
/// </summary>
internal interface IArgumentParser
{
    /// <summary>
    /// Attempts to parse the input into an argument value.
    /// </summary>
    /// <param name="input">The argument text to parse.</param>
    /// <param name="value">The parsed argument value when parsing succeeds.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    bool TryParse(ReadOnlySpan<char> input, out object? value);
}