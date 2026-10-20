namespace Majo.Command.Test;

/// <summary>
/// Represents an application-specific context carrying caller information alongside command output.
/// </summary>
/// <param name="origin">The origin of the command request.</param>
/// <param name="output">The callback receiving command output.</param>
internal sealed class TestOriginContext(string origin, Action<OutputLevel, string, string> output)
    : CommandContext(output)
{
    /// <summary>
    /// Gets the origin of the command request.
    /// </summary>
    internal string Origin { get; } = origin;
}
