using System.Globalization;
using System.Text.RegularExpressions;
// ReSharper disable MemberCanBePrivate.Global

namespace Majo.Command;

/// <summary>
/// Configures literal branches, argument branches, and command handlers.
/// </summary>
/// <remarks>Each node supports at most one argument branch; an argument that consumes the remaining input must be terminal.</remarks>
/// <typeparam name="TContext">The command execution context type.</typeparam>
public class CommandBranchBuilder<TContext> where TContext : CommandContext
{
    /// <summary>
    /// Gets the mutable node configured by this branch builder.
    /// </summary>
    internal NodeBuilder<TContext> Node { get; }
    
    /// <summary>
    /// Creates a branch builder for the supplied node.
    /// </summary>
    /// <param name="node">The mutable node to configure.</param>
    internal CommandBranchBuilder(NodeBuilder<TContext> node) => Node = node;

    /// <summary>
    /// Adds or reuses a literal branch and configures it.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="configure">The callback that configures the literal branch.</param>
    /// <param name="description">The optional node description shown in help text.</param>
    /// <returns>The builder for the literal branch.</returns>
    /// <remarks>Literal names are case-insensitive; repeated names reuse the existing branch.</remarks>
    public CommandBranchBuilder<TContext> Literal(string name, Action<CommandBranchBuilder<TContext>> configure,
        string? description = null)
    {
        EnsureNotFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("A literal must be a single token.", nameof(name));
        if (Node.ConsumeRemaining)
            throw new InvalidOperationException("A remainder argument cannot have child nodes.");

        if (!Node.Literals.TryGetValue(name, out var child))
        {
            child = new NodeBuilder<TContext>(name, description);
            Node.Literals.Add(name, child);
        }
        else if (description is not null)
        {
            child.Description ??= description;
        }

        var branch = new CommandBranchBuilder<TContext>(child);
        configure(branch);
        return branch;
    }
    
    /// <summary>
    /// Adds and configures an argument branch parsed with invariant culture.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="configure">The callback that configures the argument branch.</param>
    /// <param name="consumeRemaining">Whether this argument consumes all remaining input instead of a single token.</param>
    /// <param name="description">The optional argument description shown in help text.</param>
    /// <typeparam name="T">The argument value type.</typeparam>
    /// <returns>The builder for the argument branch.</returns>
    public CommandBranchBuilder<TContext> Argument<T>(string name, Action<CommandBranchBuilder<TContext>> configure,
        bool consumeRemaining = false, string? description = null) where T : ISpanParsable<T>
        => Argument(name, (ReadOnlySpan<char> input, out T value) =>
            {
                var success = T.TryParse(
                    input,
                    CultureInfo.InvariantCulture,
                    out var parsed);

                value = parsed!;
                return success;
            }, configure, consumeRemaining, description);

    /// <summary>
    /// Adds and configures an argument branch parsed by the supplied delegate.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="parser">The argument parser.</param>
    /// <param name="configure">The callback that configures the argument branch.</param>
    /// <param name="consumeRemaining">Whether this argument consumes all remaining input instead of a single token.</param>
    /// <param name="description">The optional argument description shown in help text.</param>
    /// <typeparam name="T">The argument value type.</typeparam>
    /// <returns>The builder for the argument branch.</returns>
    public CommandBranchBuilder<TContext> Argument<T>(string name, CommandValueParser<T> parser,
        Action<CommandBranchBuilder<TContext>> configure, bool consumeRemaining = false, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        return AddArgument(name, new ArgumentParser<T>(parser), configure, consumeRemaining, description);
    }

    /// <summary>
    /// Adds and configures a string argument branch with optional full-value regex validation.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="configure">The callback that configures the argument branch.</param>
    /// <param name="regex">An optional regular expression whose first match must span the entire argument.</param>
    /// <param name="consumeRemaining">Whether this argument consumes all remaining input instead of a single token.</param>
    /// <param name="description">The optional argument description shown in help text.</param>
    /// <returns>The builder for the string argument branch.</returns>
    public CommandBranchBuilder<TContext> Argument(string name, Action<CommandBranchBuilder<TContext>> configure,
        Regex? regex = null, bool consumeRemaining = false, string? description = null)
    {
        // Require the first regex match to span the entire argument.
        bool ParseString(ReadOnlySpan<char> input, out string value)
        {
            if (regex is not null)
            {
                var matches = regex.EnumerateMatches(input);
                if (!matches.MoveNext() || matches.Current.Index != 0 || matches.Current.Length != input.Length)
                {
                    value = string.Empty;
                    return false;
                }
            }

            value = input.ToString();
            return true;
        }

        return Argument<string>(name, ParseString, configure, consumeRemaining, description);
    }

    /// <summary>
    /// Registers the handler for this command node.
    /// </summary>
    /// <param name="action">The asynchronous handler to invoke for this command.</param>
    /// <param name="description">The optional command description shown in help text.</param>
    /// <returns>This branch builder.</returns>
    public CommandBranchBuilder<TContext> Executes(Func<CommandInvocation<TContext>, CancellationToken, Task> action,
        string? description = null)
    {
        EnsureNotFrozen();
        ArgumentNullException.ThrowIfNull(action);
        if (Node.Action is not null)
            throw new InvalidOperationException($"The command node '{Node.Name}' already has a handler.");

        Node.Action = action;
        Node.Description ??= description;
        return this;
    }

    /// <summary>
    /// Throws if this branch has already been frozen.
    /// </summary>
    private void EnsureNotFrozen()
    {
        if (Node.IsFrozen)
            throw new InvalidOperationException("The command tree has already been frozen.");
    }

    /// <summary>
    /// Adds and configures the node's argument branch.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="parser">The argument parser.</param>
    /// <param name="configure">The callback that configures the argument branch.</param>
    /// <param name="consumeRemaining">Whether this argument consumes all remaining input instead of a single token.</param>
    /// <param name="description">The optional argument description shown in help text.</param>
    /// <returns>The builder for the argument branch.</returns>
    private CommandBranchBuilder<TContext> AddArgument(string name, IArgumentParser parser,
        Action<CommandBranchBuilder<TContext>> configure, bool consumeRemaining, string? description)
    {
        EnsureNotFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);
        if (Node.ConsumeRemaining)
            throw new InvalidOperationException("A remainder argument cannot have child nodes.");
        if (Node.Argument is not null)
            throw new InvalidOperationException($"The node '{Node.Name}' already has an argument branch.");

        var child = new NodeBuilder<TContext>(name, description)
        {
            Parser = parser,
            ConsumeRemaining = consumeRemaining
        };
        Node.Argument = child;
        var branch = new CommandBranchBuilder<TContext>(child);
        configure(branch);
        if (consumeRemaining && (child.Literals.Count > 0 || child.Argument is not null))
            throw new InvalidOperationException("A remainder argument must be the last grammar node.");
        return branch;
    }
}