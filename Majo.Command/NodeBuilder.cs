using System.Collections.Frozen;

namespace Majo.Command;

/// <summary>
/// Builds a mutable command grammar node.
/// </summary>
/// <param name="name">The node name.</param>
/// <param name="description">The optional node description shown in help text.</param>
/// <typeparam name="TContext">The command execution context type.</typeparam>
internal sealed class NodeBuilder<TContext>(string name, string? description = null) where TContext : CommandContext
{
    /// <summary>
    /// The node name.
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// The optional node description shown in help text.
    /// </summary>
    public string? Description { get; set; } = description;
    
    /// <summary>
    /// The literal child branches.
    /// </summary>
    public Dictionary<string, NodeBuilder<TContext>> Literals { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// The optional argument branch.
    /// </summary>
    public NodeBuilder<TContext>? Argument { get; set; }
    
    /// <summary>
    /// The argument parser.
    /// </summary>
    public IArgumentParser? Parser { get; set; }
    
    /// <summary>
    /// Whether this argument consumes all remaining input instead of a single token.
    /// </summary>
    public bool ConsumeRemaining { get; set; }
    
    /// <summary>
    /// Whether this node has already been frozen.
    /// </summary>
    public bool IsFrozen { get; private set; }
    
    /// <summary>
    /// The handler invoked when this node is matched as a complete command.
    /// </summary>
    public Func<CommandInvocation<TContext>, CancellationToken, Task>? Action { get; set; }
    
    /// <summary>
    /// Freezes this node and its descendants into command nodes.
    /// </summary>
    /// <returns>The frozen command node.</returns>
    /// <exception cref="InvalidOperationException">This node has already been frozen.</exception>
    public CommandNode<TContext> Freeze()
    {
        if (IsFrozen) throw new InvalidOperationException("Already frozen.");
        IsFrozen = true;
        var children = Literals.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Freeze(),
            StringComparer.OrdinalIgnoreCase);

        return new CommandNode<TContext>(Name, Description, children, Argument?.Freeze(), 
            Parser, ConsumeRemaining, Action);
    }
}