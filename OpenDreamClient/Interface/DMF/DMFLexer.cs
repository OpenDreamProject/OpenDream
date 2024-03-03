using System.Text;

namespace OpenDreamClient.Interface.DMF;

public sealed class DMFLexer(string source) {
    public enum TokenType {
        Error,

        EndOfFile,
        Newline,
        Period,
        Semicolon,
        Equals,
        Value,
        Elem,
        Macro,
        Menu,
        Window,
        Attribute,
        Ternary,
        Colon,
        Lookup,
    }

    public struct Token(TokenType type, string text) {
        public TokenType Type = type;
        public string Text = text;

        public Token(TokenType type, char textChar) : this(type, textChar.ToString()) { }
    }

    private int _currentSourceIndex;

    /// <summary>
    /// Whether we're parsing an attribute name or attribute value
    /// </summary>
    private bool _parsingAttributeName = true;

    private bool AtEndOfSource => _currentSourceIndex >= source.Length;

    public Token NextToken() {
        char c = GetCurrent();

        while (c is ' ' or '\r' or '\t') // Skip these
            c = Advance();

        switch (c) {
            case '\0':
                return new(TokenType.EndOfFile, c);
            case '\n':
                Advance();
                _parsingAttributeName = true;
                return new(TokenType.Newline, c);
            case '.':
                Advance();
                _parsingAttributeName = true; // Still parsing an attribute name, the last one was actually an element name!
                return new(TokenType.Period, c);
            case ';':
                Advance();
                _parsingAttributeName = true;
                return new(TokenType.Semicolon, c);
            case '=':
                Advance();
                _parsingAttributeName = false;
                return new(TokenType.Equals, c);
            case '\'': // TODO: Single-quoted values probably refer to resources and shouldn't be treated as strings
            case '"': {
                StringBuilder textBuilder = new StringBuilder(c.ToString());

                while (Advance() != c && !AtEndOfSource) {
                    if (GetCurrent() == '\\') {
                        Advance();

                        switch (GetCurrent()) {
                            case '"':
                            case '\\': textBuilder.Append(GetCurrent()); break;
                            case 't': textBuilder.Append('\t'); break;
                            case 'n': textBuilder.Append('\n'); break;
                            default: throw new Exception($"Invalid escape sequence '\\{GetCurrent()}'");
                        }
                    } else {
                        textBuilder.Append(GetCurrent());
                    }
                }
                if (GetCurrent() != c) throw new Exception($"Expected '{c}' got '{GetCurrent()}'");
                textBuilder.Append(c);
                Advance();

                string text = textBuilder.ToString();

                // Strings are treated the same un-quoted values except they can use escape codes
                return new(TokenType.Value, text.Substring(1, text.Length - 2));
            }
            case '?':{
                Advance();
                return new(TokenType.Ternary, c);
            }
            case ':':{
                Advance();
                return new(TokenType.Colon, c);
            }
            case '[': {
                Advance();
                if(GetCurrent() != '[') //must be [[
                    throw new Exception("Expected '['");

                StringBuilder textBuilder = new StringBuilder(c.ToString());

                while (Advance() != ']' && !AtEndOfSource) {
                    textBuilder.Append(GetCurrent());
                }
                if (GetCurrent() != ']') throw new Exception("Expected ']'");
                Advance();

                return new(TokenType.Lookup, textBuilder.ToString());
            }
            default: {
                if (!char.IsAscii(c)) {
                    Advance();
                    return new(TokenType.Error, $"Invalid character: {c.ToString()}");
                }

                string text = c.ToString();

                while (!char.IsWhiteSpace(Advance()) && GetCurrent() is not ';' and not '=' and not '.' and not '?' and not ':' && !AtEndOfSource)
                    text += GetCurrent();

                TokenType tokenType;
                if (_parsingAttributeName) {
                    tokenType = text switch {
                        "elem" => TokenType.Elem,
                        "macro" => TokenType.Macro,
                        "menu" => TokenType.Menu,
                        "window" => TokenType.Window,
                        _ => TokenType.Attribute
                    };

                    _parsingAttributeName = false;
                } else {
                    tokenType = TokenType.Value;
                    _parsingAttributeName = true;
                }

                return new(tokenType, text);
            }
        }
    }

    private char GetCurrent() {
        return !AtEndOfSource ? source[_currentSourceIndex] : '\0';
    }

    private char Advance() {
        _currentSourceIndex++;
        return GetCurrent();
    }
}
