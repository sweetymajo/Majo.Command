using System.Diagnostics.CodeAnalysis;

namespace Majo.Command;

/// <summary>
/// Provides a command handler with its execution context, original input, and typed arguments.
/// </summary>
/// <param name="context">The command execution context.</param>
/// <param name="match">The successful match containing the parsed argument values.</param>
/// <param name="input">The original command input.</param>
/// <typeparam name="TContext">The command execution context type.</typeparam>
public sealed class CommandInvocation<TContext>(TContext context, CommandMatch<TContext> match, string input) 
    where TContext : CommandContext
{
    /// <summary>
    /// Gets the caller-provided command execution context.
    /// </summary>
    public TContext Context { get; } = context;
    
    /// <summary>
    /// Gets the original, unmodified command input.
    /// </summary>
    public string Input { get; } = input;
    
    /// <summary>
    /// Retrieves a parsed argument by name.
    /// </summary>
    /// <param name="name">The argument name, compared without regard to case.</param>
    /// <typeparam name="T">The expected argument value type.</typeparam>
    /// <returns>The argument value as <typeparamref name="T"/>.</returns>
    /// <exception cref="KeyNotFoundException">No argument with the specified name was parsed.</exception>
    /// <exception cref="InvalidCastException">The argument value is not compatible with <typeparamref name="T"/>.</exception>
    public T Get<T>(string name) => match.Get<T>(name);
    
    /// <summary>
    /// Attempts to retrieve a parsed argument by name.
    /// </summary>
    /// <param name="name">The argument name, compared without regard to case.</param>
    /// <param name="value">The argument value if found; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <typeparam name="T">The expected argument value type.</typeparam>
    /// <returns><see langword="true"/> if the argument was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidCastException">The argument exists, but its value is not compatible with <typeparamref name="T"/>.</exception>
    public bool TryGet<T>(string name, [MaybeNullWhen(false)] out T value) => match.TryGet(name, out value);
}