using System.Text;

namespace Majo.Command;

/// <summary>
/// Reads whitespace-delimited or double-quoted tokens from command input.
/// </summary>
/// <param name="input">The original command input.</param>
internal ref struct CommandTokenReader(ReadOnlySpan<char> input)
{
    /// <summary>
    /// The original command input.
    /// </summary>
    private readonly ReadOnlySpan<char> _input = input;
    
    /// <summary>
    /// The current offset in the input.
    /// </summary>
    private int _position = 0;
    
    /// <summary>
    /// Gets whether only whitespace or no input remains, without advancing the reader.
    /// </summary>
    public bool AtEnd
    {
        get
        {
            var i = _position;
            while (i < _input.Length && char.IsWhiteSpace(_input[i])) i++;
            return i == _input.Length;
        }
    }
    
    /// <summary>
    /// Moves the reader to the end of the input.
    /// </summary>
    public void Finish() => _position = _input.Length;
    
    /// <summary>
    /// Returns the remaining input from the specified position with leading and trailing whitespace removed.
    /// </summary>
    /// <param name="tokenStart">The starting offset in the original input.</param>
    /// <returns>A trimmed slice of the original input starting at <paramref name="tokenStart"/>.</returns>
    public ReadOnlySpan<char> RemainingFrom(int tokenStart) => _input[tokenStart..].Trim();
    
    /// <summary>
    /// Attempts to read the next token, removing surrounding quotes and decoding supported escapes.
    /// </summary>
    /// <param name="token">The token text, with surrounding quotes removed and supported escapes decoded.</param>
    /// <param name="decoded">The allocated decoded string when escapes were processed; otherwise, null.</param>
    /// <param name="error">A syntax error message, or null when no syntax error occurred.</param>
    /// <param name="tokenStart">The token's starting offset in the original input, including any opening quote.</param>
    /// <returns><see langword="true"/> if a token was read; otherwise, <see langword="false"/> at the end of input or on a syntax error.</returns>
    /// <remarks>Inside double quotes, only escaped double quotes and escaped backslashes are decoded.</remarks>
    public bool TryRead(out ReadOnlySpan<char> token, out string? decoded, out string? error, out int tokenStart)
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            _position++;

        tokenStart = _position;
        decoded = null;
        error = null;
        token = default;
        if (_position == _input.Length) return false;

        if (_input[_position] != '"')
        {
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]))
                _position++;

            token = _input[tokenStart.._position];
            return true;
        }

        // Treat a quoted value as one token even when it contains whitespace.
        _position++;
        var contentStart = _position;
        // Allocate a decoding buffer only when a quoted token contains a supported escape.
        StringBuilder? builder = null;
        var segmentStart = contentStart;
        while (_position < _input.Length)
        {
            var c = _input[_position];
            if (c == '"')
            {
                if (builder is not null)
                {
                    builder.Append(_input[segmentStart.._position]);
                    decoded = builder.ToString();
                }
                else
                {
                    token = _input[contentStart.._position];
                }

                _position++;
                if (_position < _input.Length && !char.IsWhiteSpace(_input[_position]))
                {
                    error = "A closing quote must be followed by whitespace.";
                    return false;
                }

                if (decoded is not null) token = decoded.AsSpan();
                return true;
            }

            if (c == '\\' && _position + 1 < _input.Length && 
                (_input[_position + 1] == '"' || _input[_position + 1] == '\\'))
            {
                builder ??= new StringBuilder();
                builder.Append(_input[segmentStart.._position]);
                builder.Append(_input[_position + 1]);
                _position += 2;
                segmentStart = _position;
            }
            else
            {
                _position++;
            }
        }

        error = "Unterminated quoted argument.";
        return false;
    }
}