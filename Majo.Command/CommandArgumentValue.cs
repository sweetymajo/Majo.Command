namespace Majo.Command;

/// <summary>
/// Stores a parsed argument name and its value.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Value">The argument value.</param>
internal readonly record struct CommandArgumentValue(string Name, object? Value);