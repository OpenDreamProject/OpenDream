using System.Globalization;
using System.Text;

namespace DMCompiler.Compiler.DM;

public sealed class DMLexer : TokenLexer {
    public static readonly List<string> ValidEscapeSequences = [
        "icon",
        "Roman", "roman",
        "The", "the",
        "A", "a",
        "An", "an",
        "th",
        "s",
        "He", "he",
        "She", "she",
        "himself", "herself",
        "him", "Him",
        "His", "his",
        "Hers", "hers",
        "icon",
        "improper", "proper",
        "red", "blue", "green", "black", "yellow", "navy", "teal", "cyan",
        "bold", "b",
        "italic",
        "..."
    ];

    private static readonly StringBuilder TokenTextBuilder = new();

    private static readonly List<TokenType> ValidIdentifierComponents = [
        TokenType.DM_Preproc_Identifier,
        TokenType.DM_Preproc_Number
    ];

    // NOTE: .NET still needs you to pass the capacity size to generate the most optimal code, so update it when you change these values
    private static readonly Dictionary<string, TokenType> Keywords = new(25) {
        { "null", TokenType.DM_Null },
        { "break", TokenType.DM_Break },
        { "continue", TokenType.DM_Continue },
        { "if", TokenType.DM_If },
        { "else", TokenType.DM_Else },
        { "for", TokenType.DM_For },
        { "switch", TokenType.DM_Switch },
        { "while", TokenType.DM_While },
        { "do", TokenType.DM_Do },
        { "var", TokenType.DM_Var },
        { "proc", TokenType.DM_Proc },
        { "new", TokenType.DM_New },
        { "del", TokenType.DM_Del },
        { "return", TokenType.DM_Return },
        { "in", TokenType.DM_In },
        { "to", TokenType.DM_To },
        { "as", TokenType.DM_As },
        { "set", TokenType.DM_Set },
        { "call", TokenType.DM_Call },
        { "call_ext", TokenType.DM_Call},
        { "spawn", TokenType.DM_Spawn },
        { "goto", TokenType.DM_Goto },
        { "step", TokenType.DM_Step },
        { "try", TokenType.DM_Try },
        { "catch", TokenType.DM_Catch },
        { "throw", TokenType.DM_Throw }
    };

    public int BracketNesting;

    private readonly Stack<int> _indentationStack = new(new[] { 0 });

    /// <param name="source">The enumerable list of tokens output by <see cref="DMPreprocessor.DMPreprocessorLexer"/>.</param>
    public DMLexer(string sourceName, IEnumerable<Token> source) : base(sourceName, source) { }

    protected override Token ParseNextToken() {
        Token token;

        if (AtEndOfSource) {
            while (_indentationStack.Peek() > 0) {
                _indentationStack.Pop();
                _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Dedent, '\r'));
            }

            token = CreateToken(TokenType.EndOfFile, '\0');
        } else {
            Token preprocToken = GetCurrent();

            if (preprocToken.Type == TokenType.Newline) {
                Advance();

                if (BracketNesting == 0) { //Don't parse indentation when inside brackets/parentheses
                    int currentIndentationLevel = _indentationStack.Peek();
                    int indentationLevel = CheckIndentation();

                    if (indentationLevel > currentIndentationLevel) {
                        _indentationStack.Push(indentationLevel);

                        _pendingTokenQueue.Enqueue(preprocToken);
                        token = CreateToken(TokenType.DM_Indent, '\t');
                    } else if (indentationLevel < currentIndentationLevel) {
                        if (_indentationStack.Contains(indentationLevel)) {
                            token = preprocToken;
                        } else {
                            _pendingTokenQueue.Enqueue(preprocToken);
                            token = CreateToken(TokenType.Error, string.Empty, "Invalid indentation");
                        }

                        do {
                            _indentationStack.Pop();
                            _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Dedent, '\r'));
                        } while (indentationLevel < _indentationStack.Peek());
                    } else {
                        token = preprocToken;
                    }
                } else {
                    token = preprocToken;
                }
            } else {
                switch (preprocToken.Type) {
                    case TokenType.DM_Preproc_Whitespace: Advance(); token = CreateToken(TokenType.DM_Whitespace, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_LeftParenthesis: BracketNesting++; Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_RightParenthesis: BracketNesting = Math.Max(BracketNesting - 1, 0); Advance(); token = CreateToken(TokenType.DM_RightParenthesis, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_LeftBracket: BracketNesting++; Advance(); token = CreateToken(TokenType.DM_LeftBracket, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_RightBracket: BracketNesting = Math.Max(BracketNesting - 1, 0); Advance(); token = CreateToken(TokenType.DM_RightBracket, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_Comma: Advance(); token = CreateToken(TokenType.DM_Comma, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_Colon: Advance(); token = CreateToken(TokenType.DM_Colon, preprocToken.Text); break;
                    case TokenType.DM_Preproc_Punctuator_Question:
                        switch (Advance().Type) {
                            case TokenType.DM_Preproc_Punctuator_Period:
                                token = CreateToken(TokenType.DM_QuestionPeriod, "?.");
                                Advance();
                                break;

                            case TokenType.DM_Preproc_Punctuator_Colon:
                                token = CreateToken(TokenType.DM_QuestionColon, "?:");
                                Advance();
                                break;

                            case TokenType.DM_Preproc_Punctuator_LeftBracket:
                                token = CreateToken(TokenType.DM_QuestionLeftBracket, "?[");
                                BracketNesting++;
                                Advance();
                                break;

                            default:
                                token = CreateToken(TokenType.DM_Question, "?");
                                break;
                        }
                        break;
                    case TokenType.DM_Preproc_Punctuator_Period:
                        switch (Advance().Type) {
                            case TokenType.DM_Preproc_Punctuator_Period:
                                if (Advance().Type == TokenType.DM_Preproc_Punctuator_Period) {
                                    token = CreateToken(TokenType.DM_IndeterminateArgs, "...");

                                    Advance();
                                } else {
                                    token = CreateToken(TokenType.DM_SuperProc, "..");
                                }

                                break;

                            default:
                                token = CreateToken(TokenType.DM_Period, ".");
                                break;
                        }
                        break;
                    case TokenType.DM_Preproc_Punctuator_Semicolon: {
                        Advance();
                        token = CreateToken(TokenType.DM_Semicolon, ";");
                        break;
                    }
                    case TokenType.DM_Preproc_Punctuator: {
                        Advance();

                        string c = preprocToken.Text;
                        switch (c) {
                            case "{": token = CreateToken(TokenType.DM_LeftCurlyBracket, c); break;
                            case "}": {
                                _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_RightCurlyBracket, c));
                                token = CreateToken(TokenType.Newline, '\n');

                                break;
                            }
                            case "/": token = CreateToken(TokenType.DM_Slash, c); break;
                            case "/=": token = CreateToken(TokenType.DM_SlashEquals, c); break;
                            case "=": token = CreateToken(TokenType.DM_Equals, c); break;
                            case "==": token = CreateToken(TokenType.DM_EqualsEquals, c); break;
                            case "!": token = CreateToken(TokenType.DM_Exclamation, c); break;
                            case "<>": // This is syntactically equivalent to the below so why not the same token
                            case "!=": token = CreateToken(TokenType.DM_ExclamationEquals, c); break;
                            case "^": token = CreateToken(TokenType.DM_Xor, c); break;
                            case "^=": token = CreateToken(TokenType.DM_XorEquals, c); break;
                            case "%": token = CreateToken(TokenType.DM_Modulus, c); break;
                            case "%=": token = CreateToken(TokenType.DM_ModulusEquals, c); break;
                            case "%%": token = CreateToken(TokenType.DM_ModulusModulus, c); break;
                            case "%%=": token = CreateToken(TokenType.DM_ModulusModulusEquals, c); break;
                            case "~": token = CreateToken(TokenType.DM_Tilde, c); break;
                            case "~=": token = CreateToken(TokenType.DM_TildeEquals, c); break;
                            case "~!": token = CreateToken(TokenType.DM_TildeExclamation, c); break;
                            case "&": token = CreateToken(TokenType.DM_And, c); break;
                            case "&&": token = CreateToken(TokenType.DM_AndAnd, c); break;
                            case "&&=": token = CreateToken(TokenType.DM_AndAndEquals, c); break;
                            case "&=": token = CreateToken(TokenType.DM_AndEquals, c); break;
                            case "+": token = CreateToken(TokenType.DM_Plus, c); break;
                            case "++": token = CreateToken(TokenType.DM_PlusPlus, c); break;
                            case "+=": token = CreateToken(TokenType.DM_PlusEquals, c); break;
                            case "-": token = CreateToken(TokenType.DM_Minus, c); break;
                            case "--": token = CreateToken(TokenType.DM_MinusMinus, c); break;
                            case "-=": token = CreateToken(TokenType.DM_MinusEquals, c); break;
                            case "*": token = CreateToken(TokenType.DM_Star, c); break;
                            case "**": token = CreateToken(TokenType.DM_StarStar, c); break;
                            case "*=": token = CreateToken(TokenType.DM_StarEquals, c); break;
                            case "|": token = CreateToken(TokenType.DM_Bar, c); break;
                            case "||": token = CreateToken(TokenType.DM_BarBar, c); break;
                            case "||=": token = CreateToken(TokenType.DM_BarBarEquals, c); break;
                            case "|=": token = CreateToken(TokenType.DM_BarEquals, c); break;
                            case "<": token = CreateToken(TokenType.DM_LessThan, c); break;
                            case "<<": token = CreateToken(TokenType.DM_LeftShift, c); break;
                            case "<=": token = CreateToken(TokenType.DM_LessThanEquals, c); break;
                            case "<<=": token = CreateToken(TokenType.DM_LeftShiftEquals, c); break;
                            case ">": token = CreateToken(TokenType.DM_GreaterThan, c); break;
                            case ">>": token = CreateToken(TokenType.DM_RightShift, c); break;
                            case ">=": token = CreateToken(TokenType.DM_GreaterThanEquals, c); break;
                            case ">>=": token = CreateToken(TokenType.DM_RightShiftEquals, c); break;
                            case ":=": token = CreateToken(TokenType.DM_AssignInto, c); break;
                            case "[]": token = CreateToken(TokenType.DM_DoubleSquareBracket, c); break;
                            case "[]=": token = CreateToken(TokenType.DM_DoubleSquareBracketEquals, c); break;
                            case "::": token = CreateToken(TokenType.DM_DoubleColon, c); break;
                            default: token = CreateToken(TokenType.Error, c, $"Invalid punctuator token '{c}'"); break;
                        }

                        break;
                    }
                    case TokenType.DM_Preproc_ConstantString: {
                        string tokenText = preprocToken.Text;
                        switch (preprocToken.Text[0]) {
                            case '"':
                            case '{': token = CreateToken(TokenType.DM_ConstantString, tokenText, preprocToken.Value); break;
                            case '\'': token = CreateToken(TokenType.DM_Resource, tokenText, preprocToken.Value); break;
                            case '@': token = CreateToken(TokenType.DM_RawString, tokenText, preprocToken.Value); break;
                            default: token = CreateToken(TokenType.Error, tokenText, "Invalid string"); break;
                        }

                        Advance();
                        break;
                    }
                    case TokenType.DM_Preproc_StringBegin:
                        token = CreateToken(TokenType.DM_StringBegin, preprocToken.Text, preprocToken.Value);
                        Advance();
                        break;
                    case TokenType.DM_Preproc_StringMiddle:
                        token = CreateToken(TokenType.DM_StringMiddle, preprocToken.Text, preprocToken.Value);
                        Advance();
                        break;
                    case TokenType.DM_Preproc_StringEnd:
                        token = CreateToken(TokenType.DM_StringEnd, preprocToken.Text, preprocToken.Value);
                        Advance();
                        break;
                    case TokenType.DM_Preproc_Identifier: {
                        TokenTextBuilder.Clear();

                        //An identifier can end up being made out of multiple tokens
                        //This is caused by escaped identifiers
                        do {
                            TokenTextBuilder.Append(GetCurrent().Text);
                        } while (ValidIdentifierComponents.Contains(Advance().Type) && !AtEndOfSource);

                        var identifierText = TokenTextBuilder.ToString();
                        var tokenType = Keywords.GetValueOrDefault(identifierText, TokenType.DM_Identifier);

                        token = CreateToken(tokenType, identifierText);
                        break;
                    }
                    case TokenType.DM_Preproc_Number: {
                        Advance();

                        string text = preprocToken.Text;
                        if (text is "1.#INF" or "1#INF") {
                            token = CreateToken(TokenType.DM_Float, text, float.PositiveInfinity);
                        } else if (text is "1.#IND" or "1#IND") {
                            token = CreateToken(TokenType.DM_Float, text, float.NaN);
                        } else if (text.StartsWith("0x") && int.TryParse(text.Substring(2), NumberStyles.HexNumber, null, out int intValue)) {
                            token = CreateToken(TokenType.DM_Integer, text, intValue);
                        } else if (int.TryParse(text, out intValue)) {
                            token = CreateToken(TokenType.DM_Integer, text, intValue);
                        } else if (float.TryParse(text, out float floatValue)) {
                            token = CreateToken(TokenType.DM_Float, text, floatValue);
                        } else {
                            token = CreateToken(TokenType.Error, text, "Invalid number");
                        }

                        break;
                    }
                    case TokenType.NTSL_StartFile:
                    case TokenType.NTSL_EndFile:
                    case TokenType.EndOfFile: token = preprocToken; Advance(); break;
                    default: token = CreateToken(TokenType.Error, preprocToken.Text, "Invalid token"); break;
                }
            }
        }

        return token;
    }

    public int CurrentIndentation() {
        return _indentationStack.Peek();
    }

    private int CheckIndentation() {
        int indentationLevel = 0;

        Token current = GetCurrent();
        if (current.Type == TokenType.DM_Preproc_Whitespace) {
            indentationLevel = current.Text.Length;

            Advance();
        }

        return indentationLevel;
    }
}
