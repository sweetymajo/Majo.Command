namespace Majo.Command;

/// <summary>
/// Specifies the severity or verbosity of command output.
/// </summary>
public enum OutputLevel
{
    /// <summary>
    /// Detailed diagnostic output.
    /// </summary>
    Verbose,
    
    /// <summary>
    /// Output intended for debugging.
    /// </summary>
    Debug,
    
    /// <summary>
    /// General information about command execution.
    /// </summary>
    Information,
    
    /// <summary>
    /// A potential issue that may require attention.
    /// </summary>
    Warning,
    
    /// <summary>
    /// An error encountered during command execution.
    /// </summary>
    Error,
    
    /// <summary>
    /// A critical error that prevents normal operation.
    /// </summary>
    Fatal
}