using System.Text;
using OpenDreamShared.Compiler;

namespace OpenDreamClient.Interface.DMF;

public sealed class DMFLexer : TextLexer {
    /// <summary>
    /// Whether we're parsing an attribute name or attribute value
    /// </summary>
    private bool _parsingAttributeName = true;

    public DMFLexer(string sourceName, string source) : base(sourceName, source) { }

    protected override Token ParseNextToken() {
        Token token = base.ParseNextToken();

        if (token == null) {
            char c = GetCurrent();

            switch (c) {
                case ' ':
                case '\t': {
                    Advance();
                    token = CreateToken(TokenType.Skip, c);
                    break;
                }
                case '.':
                    if (_parsingAttributeName == false) goto default;
                    Advance();
                    token = CreateToken(TokenType.DMF_Period, c);
                    _parsingAttributeName = true; // Still parsing an attribute name, the last one was actually an element name!
                    break;
                case ';':
                    Advance();
                    token = CreateToken(TokenType.DMF_Semicolon, c);
                    _parsingAttributeName = true;
                    break;
                case '=':
                    Advance();
                    token = CreateToken(TokenType.DMF_Equals, c);
                    _parsingAttributeName = false;
                    break;
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
                    if (GetCurrent() != c) throw new Exception($"Expected '{c}'");
                    textBuilder.Append(c);
                    Advance();

                    string text = textBuilder.ToString();

                    // Strings are treated the same un-quoted values except they can use escape codes
                    token = CreateToken(TokenType.DMF_Value, text.Substring(1, text.Length - 2));
                    break;
                }
                default: {
                    if (!char.IsAscii(c)) {
                        token = CreateToken(TokenType.Error, $"Invalid character: {c.ToString()}");
                        Advance();
                        break;
                    }

                    string text = c.ToString();

                    while (!char.IsWhiteSpace(Advance()) && GetCurrent() is not ';' and not '=' and not '.' && !AtEndOfSource) text += GetCurrent();

                    TokenType tokenType;
                    if (_parsingAttributeName) {
                        tokenType = text switch {
                            "elem" => TokenType.DMF_Elem,
                            "macro" => TokenType.DMF_Macro,
                            "menu" => TokenType.DMF_Menu,
                            "window" => TokenType.DMF_Window,
                            _ => TokenType.DMF_Attribute
                        };

                        _parsingAttributeName = false;
                    } else {
                        tokenType = TokenType.DMF_Value;
                        _parsingAttributeName = true;
                    }

                    token = CreateToken(tokenType, text);
                    break;
                }
            }
        } else if (token.Type == TokenType.Newline) {
            _parsingAttributeName = true;
        }

        return token;
    }
}
