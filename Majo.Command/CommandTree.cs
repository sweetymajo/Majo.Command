// ReSharper disable MemberCanBePrivate.Global
namespace Majo.Command;

/// <summary>
/// Represents a frozen command tree used to match, execute, and describe commands.
/// </summary>
/// <typeparam name="TContext">The command execution context type.</typeparam>
public sealed class CommandTree<TContext> where TContext : CommandContext
{
    /// <summary>
    /// The root command node.
    /// </summary>
    private readonly CommandNode<TContext> _root;
    
    /// <summary>
    /// Creates a command tree from a frozen root node.
    /// </summary>
    /// <param name="root">The root command node.</param>
    internal CommandTree(CommandNode<TContext> root) => _root = root;

    /// <summary>
    /// Matches input against the command tree.
    /// </summary>
    /// <param name="input">The original command input.</param>
    /// <returns>The result of matching command input.</returns>
    public CommandMatch<TContext> Match(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var reader = new CommandTokenReader(input.AsSpan());
        var current = _root;
        List<CommandArgumentValue>? values = null;
        var foundAny = false;

        while (true)
        {
            if (!reader.TryRead(out var token, out _,
                out var syntaxError, out var tokenStart))
            {
                if (syntaxError is not null)
                    return Fail(CommandError.InvalidSyntax, syntaxError);

                if (!foundAny)
                    return Fail(CommandError.EmptyInput, "A command is required.");

                if (current.Action is null)
                    return Fail(CommandError.IncompleteCommand, "The command is incomplete.");

                return new CommandMatch<TContext>(current, input,
                    values?.ToArray() ?? [], CommandError.None, null);
            }

            foundAny = true;
            // Literal branches take precedence over the argument branch; matching does not backtrack.
            if (current.TryGetLiteral(token, out var literal))
            {
                current = literal!;
                continue;
            }

            var argument = current.Argument;
            if (argument is null)
            {
                var kind = current.Literals.Count > 0 ? CommandError.UnknownCommand :
                    CommandError.UnexpectedArgument;
                return Fail(kind, $"Unexpected token '{token.ToString()}'.");
            }

            if (argument.ConsumeRemaining)
            {
                // Keep a lone token decoded; otherwise preserve the trimmed, unprocessed remainder.
                var wholeRemainder = reader.RemainingFrom(tokenStart);
                token = reader.AtEnd ? token : wholeRemainder;
                reader.Finish();
            }

            if (!argument.Parser!.TryParse(token, out var value))
                return Fail(CommandError.InvalidArgument,
                    $"Invalid value for '{argument.Name}': '{token.ToString()}'.");

            (values ??= []).Add(new CommandArgumentValue(argument.Name, value));
            current = argument;
        }

        // Failed matches expose no partially parsed argument values.
        CommandMatch<TContext> Fail(CommandError error, string message)
            => new(null, input, [], error, message);
    }

    /// <summary>
    /// Matches the input and invokes the corresponding handler if matching succeeds.
    /// </summary>
    /// <param name="input">The original command input.</param>
    /// <param name="context">The command execution context.</param>
    /// <param name="ct">The cancellation token passed to the command handler.</param>
    /// <returns>The result of matching command input.</returns>
    public async Task<CommandMatch<TContext>> ExecuteAsync(
        string input, TContext context, CancellationToken ct = default)
    {
        var match = Match(input);
        if (match.Success)
            await match.ExecuteAsync(context, ct).ConfigureAwait(false);
        return match;
    }

    /// <summary>
    /// Returns help text listing executable command usages and their descriptions.
    /// </summary>
    /// <param name="literalPath">An optional space-separated literal path used to filter the commands.</param>
    /// <returns>Command help entries, or a message indicating that no commands matched.</returns>
    /// <remarks>The filter matches literal segments only, without regard to case; argument names are excluded.</remarks>
    public string GetHelp(string? literalPath = null)
    {
        var filter = literalPath?.Split(' ', 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var lines = new List<string>();
        Visit(_root, "", []);
        return lines.Count == 0 ? "No commands found." : string.Join(Environment.NewLine, lines);


        void Visit(CommandNode<TContext> node, string usage, string[] literalSegments)
        {
            if (node.Action is not null && PrefixMatches(literalSegments, filter))
            {
                string? optionalSuffix = GetOptionalSuffix(node);
                string displayUsage = optionalSuffix is null
                    ? usage
                    : Add(usage, optionalSuffix);

                lines.Add(string.IsNullOrEmpty(node.Description)
                    ? displayUsage
                    : $"{displayUsage} - {node.Description}");

                // The remaining nodes are included in this help entry, so skip their individual entries.
                if (optionalSuffix is not null)
                    return;
            }

            foreach (var pair in node.Literals.OrderBy(
                         x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var child = pair.Value;
                var segment = child.Name;
                Visit(child, Add(usage, segment), [..literalSegments, segment]);
            }

            if (node.Argument is { } argument)
                Visit(argument, Add(usage, $"<{argument.Name}>"), literalSegments);
        }
        
        static string? GetOptionalSuffix(CommandNode<TContext> node)
        {
            if (node.Literals.Count > 0 || node.Argument is null)
                return null;

            var segments = new List<string>();

            // Each stopping point has a handler, so the remaining argument chain can be shown as optional.
            while (node.Argument is { } argument)
            {
                // Only collapse consecutive executable argument nodes with no literal branches.
                if (argument.Action is null || argument.Literals.Count > 0)
                    return null;

                segments.Add($"[{argument.Name}]");
                node = argument;
            }

            return string.Join(" ", segments);
        }
        
        static string Add(string prefix, string segment)
            => prefix.Length == 0 ? segment : $"{prefix} {segment}";

        static bool PrefixMatches(string[] literals, string[] prefix)
        {
            if (prefix.Length > literals.Length) return false;
            for (var i = 0; i < prefix.Length; i++)
                if (!string.Equals(literals[i], prefix[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            return true;
        }
    }
}