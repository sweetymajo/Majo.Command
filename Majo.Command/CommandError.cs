namespace Majo.Command;

/// <summary>
/// Specifies errors that can occur while matching command input.
/// </summary>
public enum CommandError
{
    /// <summary>
    /// The command matched successfully.
    /// </summary>
    None,
    
    /// <summary>
    /// The input contains no command tokens.
    /// </summary>
    EmptyInput,
    
    /// <summary>
    /// The input does not match any available literal branch.
    /// </summary>
    UnknownCommand,
    
    /// <summary>
    /// An argument parser rejected the supplied value.
    /// </summary>
    InvalidArgument,
    
    /// <summary>
    /// The input ends at a node that has no command handler.
    /// </summary>
    IncompleteCommand,
    
    /// <summary>
    /// The input contains a token where no further branches are available.
    /// </summary>
    UnexpectedArgument,
    
    /// <summary>
    /// The input contains invalid quoting or token syntax.
    /// </summary>
    InvalidSyntax
}