using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenDreamShared.Compiler.DM {
    public class DMLexer : Lexer<Token> {
        public static List<string> ValidEscapeSequences = new List<string>() {
            "t", "n",
            "[", "]",
            "\\", "\"", "'",
            "<", ">",

            "icon",
            "Roman", "roman",
            "The", "the",
            "A", "a", "An", "an",
            "th",
            "s",
            "He", "he",
            "She", "she",
            "him",
            "himself", "herself",
            "His", "his",
            "Hers", "hers",
            "icon",
            "ref",
            "improper", "proper",
            "red", "blue", "green", "black",
            "..."
            //TODO: ASCII/Unicode values
        };

        public static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>() {
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
            { "spawn", TokenType.DM_Spawn },
            { "list", TokenType.DM_List },
            { "newlist", TokenType.DM_NewList },
            { "goto", TokenType.DM_Goto },
            { "step", TokenType.DM_Step }
        };

        private bool _checkingIndentation = true;
        private int bracketNesting = 0;
        private Stack<int> _indentationStack = new Stack<int>(new int[] { 0 });

        public DMLexer(string sourceName, List<Token> source) : base(sourceName, source) {
            Advance();
        }

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
                    if (_checkingIndentation) {
                        Advance();

                        int currentIndentationLevel = _indentationStack.Peek();
                        int indentationLevel = CheckIndentation();
                        if (indentationLevel > currentIndentationLevel) {
                            _indentationStack.Push(indentationLevel);

                            _pendingTokenQueue.Enqueue(preprocToken);
                            token = CreateToken(TokenType.DM_Indent, '\t');
                        } else if (indentationLevel < currentIndentationLevel) {
                            _pendingTokenQueue.Enqueue(preprocToken);
                            do {
                                _indentationStack.Pop();
                                _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Dedent, '\r'));
                            } while (indentationLevel < _indentationStack.Peek());

                            if (indentationLevel == _indentationStack.Peek()) {
                                token = _pendingTokenQueue.Dequeue();
                            } else {
                                token = CreateToken(TokenType.Error, null, "Invalid indentation");
                            }
                        } else {
                            token = preprocToken;
                        }
                    } else {
                        token = preprocToken;
                    }
                } else {
                    switch (preprocToken.Type) {
                        case TokenType.DM_Preproc_Whitespace: {
                            while (Advance().Type == TokenType.DM_Preproc_Whitespace && !AtEndOfSource) {
                            }

                            token = CreateToken(TokenType.DM_Whitespace, preprocToken.Text);
                            break;
                        }
                        case TokenType.DM_Preproc_Punctuator_LeftParenthesis: bracketNesting++; Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, preprocToken.Text); break;
                        case TokenType.DM_Preproc_Punctuator_RightParenthesis: bracketNesting--; Advance(); token = CreateToken(TokenType.DM_RightParenthesis, preprocToken.Text); break;
                        case TokenType.DM_Preproc_Punctuator_LeftBracket: bracketNesting++; Advance(); token = CreateToken(TokenType.DM_LeftBracket, preprocToken.Text); break;
                        case TokenType.DM_Preproc_Punctuator_RightBracket: bracketNesting--; Advance(); token = CreateToken(TokenType.DM_RightBracket, preprocToken.Text); break;
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
                        case TokenType.DM_Preproc_Punctuator: {
                            Advance();

                            string c = preprocToken.Text;
                            switch (c) {
                                case ";": token = CreateToken(TokenType.DM_Semicolon, c); break;
                                case "{": token = CreateToken(TokenType.DM_LeftCurlyBracket, c); break;
                                case "}": {
                                    _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_RightCurlyBracket, c));
                                    token = CreateToken(TokenType.Newline, '\n');

                                    break;
                                }
                                case "/": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_SlashEquals, "/=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Slash, c);
                                    }

                                    break;
                                }
                                case "=": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_EqualsEquals, "==");
                                    } else {
                                        token = CreateToken(TokenType.DM_Equals, c);
                                    }

                                    break;
                                }
                                case "!": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_ExclamationEquals, "!=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Exclamation, c);
                                    }

                                    break;
                                }
                                case "^": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_XorEquals, "^=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Xor, c);
                                    }

                                    break;
                                }
                                case "%": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_ModulusEquals, "%=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Modulus, c);
                                    }

                                    break;
                                }
                                case "~": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_TildeEquals, "~=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Tilde, c);
                                    }

                                    break;
                                }
                                case "&": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "&") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_AndAnd, "&&");
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_AndEquals, "&=");
                                    } else {
                                        token = CreateToken(TokenType.DM_And, c);
                                    }

                                    break;
                                }
                                case "+": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "+") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_PlusPlus, "++");
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_PlusEquals, "+=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Plus, c);
                                    }

                                    break;
                                }
                                case "-": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "-") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_MinusMinus, "--");
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_MinusEquals, "-=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Minus, c);
                                    }

                                    break;
                                }
                                case "*": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "*") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_StarStar, "**");
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_StarEquals, "*=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Star, c);
                                    }

                                    break;
                                }
                                case "|": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "|") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_BarBar, "||");
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_BarEquals, "|=");
                                    } else {
                                        token = CreateToken(TokenType.DM_Bar, c);
                                    }

                                    break;
                                }
                                case "<": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "<") {
                                        current = Advance();

                                        if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                            Advance();

                                            token = CreateToken(TokenType.DM_LeftShiftEquals, "<<=");
                                        } else {
                                            token = CreateToken(TokenType.DM_LeftShift, "<<");
                                        }
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_LessThanEquals, "<=");
                                    } else {
                                        token = CreateToken(TokenType.DM_LessThan, c);
                                    }

                                    break;
                                }
                                case ">": {
                                    Token current = GetCurrent();

                                    if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == ">") {
                                        current = Advance();

                                        if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                            Advance();

                                            token = CreateToken(TokenType.DM_RightShiftEquals, ">>=");
                                        } else {
                                            token = CreateToken(TokenType.DM_RightShift, ">>");
                                        }
                                    } else if (current.Type == TokenType.DM_Preproc_Punctuator && current.Text == "=") {
                                        Advance();

                                        token = CreateToken(TokenType.DM_GreaterThanEquals, ">=");
                                    } else {
                                        token = CreateToken(TokenType.DM_GreaterThan, c);
                                    }

                                    break;
                                }
                                default: throw new Exception("Invalid punctuator token '" + c + "'");
                            }

                            break;
                        }
                        case TokenType.DM_Preproc_ConstantString: {
                            string tokenText = preprocToken.Text;
                            switch (preprocToken.Text[0]) {
                                case '"':
                                case '{': token = CreateToken(TokenType.DM_String, tokenText, preprocToken.Value); break;
                                case '\'': token = CreateToken(TokenType.DM_Resource, tokenText, preprocToken.Value); break;
                                case '@': token = CreateToken(TokenType.DM_RawString, tokenText, preprocToken.Value); break;
                                default: token = CreateToken(TokenType.Error, tokenText, "Invalid string"); break;
                            }

                            Advance();
                            break;
                        }
                        case TokenType.DM_Preproc_String: {
                            string tokenText = preprocToken.Text;

                            string stringStart = null, stringEnd = null;
                            switch (preprocToken.Text[0]) {
                                case '"': stringStart = "\""; stringEnd = "\""; break;
                                case '{': stringStart = "{\""; stringEnd = "\"}"; break;
                            }

                            if (stringEnd != null) {
                                StringBuilder stringTextBuilder = new StringBuilder(tokenText);

                                int stringNesting = 1;
                                while (!AtEndOfSource) {
                                    Token stringToken = Advance();

                                    stringTextBuilder.Append(stringToken.Text);
                                    if (stringToken.Type == TokenType.DM_Preproc_String) {
                                        if (stringToken.Text.StartsWith(stringStart)) {
                                            stringNesting++;
                                        } else if (stringToken.Text.EndsWith(stringEnd)) {
                                            stringNesting--;

                                            if (stringNesting == 0) break;
                                        }
                                    }
                                }

                                string stringText = stringTextBuilder.ToString();
                                string stringValue = stringText.Substring(stringStart.Length, stringText.Length - stringStart.Length - stringEnd.Length);
                                token = CreateToken(TokenType.DM_String, stringText, stringValue);
                            } else {
                                token = CreateToken(TokenType.Error, tokenText, "Invalid string");
                            }

                            Advance();
                            break;
                        }
                        case TokenType.DM_Preproc_Identifier: {
                            StringBuilder identifierTextBuilder = new StringBuilder();

                            do { //Preprocessor macros might end up making one identifier out of multiple tokens
                                identifierTextBuilder.Append(GetCurrent().Text);
                            } while (Advance().Type == TokenType.DM_Preproc_Identifier && !AtEndOfSource);

                            string identifierText = identifierTextBuilder.ToString();
                            if (Keywords.TryGetValue(identifierText, out TokenType keywordType)) {
                                token = CreateToken(keywordType, identifierText);
                            } else {
                                token = CreateToken(TokenType.DM_Identifier, identifierText);
                            }

                            break;
                        }
                        case TokenType.DM_Preproc_Number: {
                            Advance();

                            string text = preprocToken.Text;
                            if (text == "1.#INF") {
                                token = CreateToken(TokenType.DM_Float, text, Single.PositiveInfinity);
                            } else if (text.StartsWith("0x") && Int32.TryParse(text.Substring(2), NumberStyles.HexNumber, null, out int intValue)) {
                                token = CreateToken(TokenType.DM_Integer, text, intValue);
                            } else if (Int32.TryParse(text, out intValue)) {
                                token = CreateToken(TokenType.DM_Integer, text, intValue);
                            } else if (Single.TryParse(text, out float floatValue)) {
                                token = CreateToken(TokenType.DM_Float, text, floatValue);
                            } else {
                                token = CreateToken(TokenType.Error, text, "Invalid number");
                            }

                            break;
                        }
                        case TokenType.EndOfFile:
                        case TokenType.Error:
                        case TokenType.Warning: token = preprocToken; Advance(); break;
                        default: token = CreateToken(TokenType.Error, preprocToken.Text, "Invalid token"); break;
                    }
                }
            }

            return token;
        }

        protected override Token Advance() {
            Token current = base.Advance();

            //Skip any newlines when inside brackets
            if (bracketNesting != 0) {
                while (current.Type == TokenType.Newline) current = Advance();
            }

            SourceName = current.SourceFile;
            CurrentLine = current.Line;
            CurrentColumn = current.Column;
            return current;
        }

        private int CheckIndentation() {
            int indentationLevel = 0;

            while (GetCurrent().Type == TokenType.DM_Preproc_Whitespace) {
                indentationLevel++;

                Advance();
            }

            return indentationLevel;
        }
    }
}
