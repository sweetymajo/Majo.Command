namespace Majo.Command;

/// <summary>
/// Builds a command tree and freezes its configuration.
/// </summary>
/// <typeparam name="TContext">The command execution context type.</typeparam>
public sealed class CommandTreeBuilder<TContext> : CommandBranchBuilder<TContext> where TContext : CommandContext
{
    /// <summary>
    /// Whether this builder has already produced a command tree.
    /// </summary>
    private bool _built;
    
    /// <summary>
    /// Creates an empty command tree builder.
    /// </summary>
    public CommandTreeBuilder() : base(new NodeBuilder<TContext>(string.Empty)) { }

    /// <summary>
    /// Builds the command tree and freezes all branches.
    /// </summary>
    /// <returns>The frozen command tree.</returns>
    /// <remarks>This is a one-time operation; retained branch builders cannot modify the tree afterward.</remarks>
    /// <exception cref="InvalidOperationException">This builder has already been built.</exception>
    public CommandTree<TContext> Build()
    {
        if (_built)
            throw new InvalidOperationException("This builder has already been built.");

        _built = true;
        return new CommandTree<TContext>(Node.Freeze());
    }
}