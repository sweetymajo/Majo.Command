using System.Collections.Frozen;

namespace Majo.Command;

/// <summary>
/// Represents a command grammar node with frozen literal branches and an optional argument branch.
/// </summary>
/// <param name="name">The node name.</param>
/// <param name="description">The optional node description shown in help text.</param>
/// <param name="literals">The frozen, case-insensitive lookup of literal child branches.</param>
/// <param name="argument">The optional argument branch.</param>
/// <param name="parser">The value parser for an argument node, or null for a literal node.</param>
/// <param name="consumeRemaining">Whether this argument consumes all remaining input instead of a single token.</param>
/// <param name="action">The handler invoked when this node is matched as a complete command.</param>
/// <typeparam name="TContext">The command execution context type.</typeparam>
internal class CommandNode<TContext>(string name, string? description, 
    FrozenDictionary<string, CommandNode<TContext>> literals, CommandNode<TContext>? argument, IArgumentParser? parser, 
    bool consumeRemaining, Func<CommandInvocation<TContext>, CancellationToken, Task>? action)
    where TContext : CommandContext
{
    /// <summary>
    /// The node name.
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// The optional node description shown in help text.
    /// </summary>
    public string? Description { get; } = description;
    
    /// <summary>
    /// The literal child branches.
    /// </summary>
    public FrozenDictionary<string, CommandNode<TContext>> Literals { get; } = literals;
    
    /// <summary>
    /// The optional argument branch.
    /// </summary>
    public CommandNode<TContext>? Argument { get; } = argument;
    
    /// <summary>
    /// The argument parser.
    /// </summary>
    public IArgumentParser? Parser { get; } = parser;
    
    /// <summary>
    /// Whether this argument consumes all remaining input instead of a single token.
    /// </summary>
    public bool ConsumeRemaining { get; } = consumeRemaining;
    
    /// <summary>
    /// The handler invoked when this node is matched as a complete command.
    /// </summary>
    public Func<CommandInvocation<TContext>, CancellationToken, Task>? Action { get; } = action;
    
    /// <summary>
    /// Attempts to find a literal child using an ordinal, case-insensitive comparison.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="node">The literal child if found; otherwise, null.</param>
    /// <returns><see langword="true"/> if a matching literal child was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetLiteral(ReadOnlySpan<char> name, out CommandNode<TContext>? node)
        => Literals.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out node);
}