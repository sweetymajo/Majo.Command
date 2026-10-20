namespace Majo.Command;

/// <summary>
/// Parses command argument text into a value of the specified type.
/// </summary>
/// <param name="input">The argument text to parse.</param>
/// <param name="value">The parsed value when parsing succeeds.</param>
/// <typeparam name="T">The parsed value type.</typeparam>
/// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
public delegate bool CommandValueParser<T>(ReadOnlySpan<char> input, out T value);