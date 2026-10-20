using System.Diagnostics.CodeAnalysis;
// ReSharper disable MemberCanBePrivate.Global

namespace Majo.Command;

/// <summary>
/// Represents the result of matching command input, including parsed arguments or a matching error.
/// </summary>
/// <typeparam name="TContext">The command execution context type.</typeparam>
public class CommandMatch<TContext> where TContext : CommandContext
{
    /// <summary>
    /// The matched command node, or null if matching failed.
    /// </summary>
    private readonly CommandNode<TContext>? _node;
    
    /// <summary>
    /// The original command input.
    /// </summary>
    private readonly string _input;
    
    /// <summary>
    /// The parsed command argument values.
    /// </summary>
    private readonly CommandArgumentValue[] _values;
    
    /// <summary>
    /// Gets the error encountered while matching the command.
    /// </summary>
    public CommandError Error { get; }
    
    /// <summary>
    /// Gets the matching error message, or null when matching succeeded.
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Gets whether command matching succeeded.
    /// </summary>
    /// <remarks>A successful match does not mean that the command handler has executed successfully.</remarks>
    public bool Success => Error == CommandError.None;
    
    /// <summary>
    /// Creates a command match with its parsed values or error details.
    /// </summary>
    /// <param name="node">The command node.</param>
    /// <param name="input">The original command input.</param>
    /// <param name="values">The parsed command argument values.</param>
    /// <param name="error">The error encountered while matching the command.</param>
    /// <param name="message">The error message from command matching, if any.</param>
    internal CommandMatch(CommandNode<TContext>? node, string input, CommandArgumentValue[] values, CommandError error, 
        string? message)
    {
        _node = node;
        _input = input;
        _values = values;
        Error = error;
        ErrorMessage = message;
    }
    
    /// <summary>
    /// Retrieves a parsed argument by name.
    /// </summary>
    /// <param name="name">The argument name, compared without regard to case.</param>
    /// <typeparam name="T">The argument value type.</typeparam>
    /// <returns>The argument value as <typeparamref name="T"/>.</returns>
    /// <exception cref="KeyNotFoundException">No argument with the specified name was parsed.</exception>
    /// <exception cref="InvalidCastException">The argument value is not compatible with <typeparamref name="T"/>.</exception>
    public T Get<T>(string name)
    {
        foreach (var item in _values)
            if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                return item.Value is T value ? value :
                    throw new InvalidCastException($"Argument '{name}' is not a {typeof(T).Name}.");

        throw new KeyNotFoundException($"Argument '{name}' was not parsed.");
    }
    
    /// <summary>
    /// Attempts to retrieve a parsed argument by name.
    /// </summary>
    /// <param name="name">The argument name, compared without regard to case.</param>
    /// <param name="value">The argument value if found; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <typeparam name="T">The argument value type.</typeparam>
    /// <returns><see langword="true"/> if the argument was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidCastException">The argument exists, but its value is not compatible with <typeparamref name="T"/>.</exception>
    public bool TryGet<T>(string name, [MaybeNullWhen(false)] out T value)
    {
        foreach (var item in _values)
        {
            if (!string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (item.Value is T parsed)
            {
                value = parsed;
                return true;
            }

            throw new InvalidCastException($"Argument '{name}' is not a {typeof(T).Name}.");
        }

        value = default!;
        return false;
    }
    
    /// <summary>
    /// Executes the handler for this successful match.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="ct">The cancellation token passed to the command handler.</param>
    /// <returns>A task representing the handler execution.</returns>
    /// <exception cref="ArgumentNullException">The execution context is null.</exception>
    /// <exception cref="InvalidOperationException">Matching failed or the matched node has no handler.</exception>
    public async Task ExecuteAsync(TContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Success || _node?.Action is null)
            throw new InvalidOperationException(ErrorMessage ?? "This command is not executable.");

        await _node.Action(new CommandInvocation<TContext>(context, this, _input), ct).ConfigureAwait(false);
    }
}