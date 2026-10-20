namespace Majo.Command;

/// <summary>
/// Adapts a typed argument parser to the object-based parser interface.
/// </summary>
/// <param name="parser">The typed parser used to read argument values.</param>
/// <typeparam name="T">The parsed value type.</typeparam>
internal sealed class ArgumentParser<T>(CommandValueParser<T> parser) : IArgumentParser
{
    /// <summary>
    /// Attempts to parse the input into an argument value.
    /// </summary>
    /// <param name="input">The argument text to parse.</param>
    /// <param name="value">The parsed value on success; otherwise, null.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public bool TryParse(ReadOnlySpan<char> input, out object? value)
    {
        if (parser(input, out var result))
        {
            value = result;
            return true;
        }
        value = null;
        return false;
    }
}