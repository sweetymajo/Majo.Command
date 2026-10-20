namespace Majo.Command;

/// <summary>
/// Represents the caller-provided command execution environment.
/// </summary>
/// <param name="output">The callback receiving the output level, text, and tag, in that order.</param>
/// <remarks>The callback controls whether output is written immediately or buffered by the caller.</remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class CommandContext(Action<OutputLevel, string, string> output)
{
    /// <summary>
    /// Sends command output to the caller-provided callback.
    /// </summary>
    /// <param name="level">The output level.</param>
    /// <param name="text">The output text.</param>
    /// <param name="tag">The output tag.</param>
    public void Output(OutputLevel level, string text, string tag) => output(level, text, tag);
}