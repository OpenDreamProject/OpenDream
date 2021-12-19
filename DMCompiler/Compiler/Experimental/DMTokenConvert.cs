using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Original = OpenDreamShared.Compiler;
using OTT = OpenDreamShared.Compiler.TokenType;

namespace DMCompiler.Compiler.Experimental {

    public class PreprocessorTokenConvert : DM.IDMLexer {
        private IEnumerator<PreprocessorToken> _tokens;
        PreprocessorToken _currentToken;

        Queue<Original.Token> _pendingTokenQueue = new();

        Original.Token CreateToken(OTT ty, string text = null, object value = null, PreprocessorToken parent = null) {
            string path;
            int line, column;
            if (parent == null) {
                parent = _currentToken;
            }
            path = parent?.Location?.Source.FullPath ?? "";
            line = parent?.Location?.Line ?? 0;
            column = parent?.Location?.Column ?? 0;
            text = text ?? parent?.Text ?? "";
            value = value ?? parent?.Value ?? null;
            return new Original.Token(ty, text, new Original.Location(path, line, column), value);
        }

        public PreprocessorTokenConvert(IEnumerator<PreprocessorToken> tokens) {
            _tokens = tokens; tokens.MoveNext(); _currentToken = tokens.Current;
        }

        private int _bracketNesting = 0;

        public int BracketNesting { get { return _bracketNesting; } set { _bracketNesting = value; } }
        private Stack<int> _indentationStack = new(new int[] { 0 });

        public int CurrentIndentation() {
            return _indentationStack.Peek();
        }
        private int CheckIndentation() {
            int indentationLevel = 0;
            while (_currentToken.Type == TokenType.Whitespace) {
                indentationLevel++;
                Advance();
            }
            return indentationLevel;
        }

        class EndOfLexException : Exception { }
        PreprocessorToken Advance() {
            _tokens.MoveNext();
            _currentToken = _tokens.Current;
            if (_currentToken == null) {
                throw new EndOfLexException();
            }
            return _currentToken;
        }

        public Original.Token GetNextToken() {
            if (_pendingTokenQueue.Count > 0)
                return _pendingTokenQueue.Dequeue();

            try {
                Original.Token nextToken = ParseNextToken();
                while (nextToken.Type == OTT.Skip) nextToken = ParseNextToken();
                if (_pendingTokenQueue.Count > 0) {
                    _pendingTokenQueue.Enqueue(nextToken);
                    return _pendingTokenQueue.Dequeue();
                }
                else {
                    return nextToken;
                }
            }
            catch (EndOfLexException) {
                DrainIndent();
                _pendingTokenQueue.Enqueue(CreateToken(OTT.Newline, ""));
                _pendingTokenQueue.Enqueue(CreateToken(OTT.EndOfFile, ""));
                return GetNextToken();
            }
        }

        void DrainIndent() {
            while (_indentationStack.Peek() > 0) {
                _indentationStack.Pop();
                _pendingTokenQueue.Enqueue(CreateToken(OTT.DM_Dedent, "\r"));
            }
        }

        // >= 0 still collecting on line
        // -1 already drained for this line
        int current_indent = 0;
        bool pending_newline = false;

        public void ResolveIndendation() {
            //Console.WriteLine(" resolve " + current_indent + " " + _indentationStack.Peek());
            if (BracketNesting == 0) {
                int currentIndentationLevel = _indentationStack.Peek();
                int indentationLevel = current_indent;
                if (indentationLevel > currentIndentationLevel) {
                    _indentationStack.Push(indentationLevel);
                    if (pending_newline) {
                        _pendingTokenQueue.Enqueue(CreateToken(OTT.Newline, "\n", _currentToken));
                        pending_newline = false;
                    }
                    _pendingTokenQueue.Enqueue(CreateToken(OTT.DM_Indent, "\t"));
                }
                else if (indentationLevel < currentIndentationLevel) {
                    if (!_indentationStack.Contains(indentationLevel)) {
                        _pendingTokenQueue.Enqueue(CreateToken(OTT.Error, value: "Invalid indentation"));
                    }
                    do {
                        _indentationStack.Pop();
                        _pendingTokenQueue.Enqueue(CreateToken(OTT.DM_Dedent, "\r"));
                    } while (indentationLevel < _indentationStack.Peek());
                    if (pending_newline) {
                        _pendingTokenQueue.Enqueue(CreateToken(OTT.Newline, "\n", _currentToken));
                        pending_newline = false;
                    }
                }
                else {
                    if (pending_newline) {
                        _pendingTokenQueue.Enqueue(CreateToken(OTT.Newline, "\n", _currentToken));
                        pending_newline = false;
                    }
                }
            }
            else {
                if (pending_newline) {
                    _pendingTokenQueue.Enqueue(CreateToken(OTT.Newline, "\n", _currentToken));
                    pending_newline = false;
                }
            }
        }
        public Original.Token ParseNextToken() {
            Original.Token found_token;
            PreprocessorToken original_token = _currentToken;
            if (_currentToken == null) {
                DrainIndent();
                _pendingTokenQueue.Enqueue(CreateToken(OTT.EndOfFile));
                return GetNextToken();
            }
            //Console.WriteLine("_currentToken " + _currentToken.ToString());
            if (_currentToken.Type == TokenType.EndOfFile) {
                DrainIndent();
                Advance();
                return CreateToken(OTT.Skip, "");
            }
            else if (_currentToken.Type == TokenType.Newline) {
                Advance();
                if (current_indent != -1) {
                    current_indent = 0;
                    return ParseNextToken();
                }
                current_indent = 0;
                pending_newline = true;
                return ParseNextToken();
            }
            else if (_currentToken.Type == TokenType.Whitespace && current_indent > -1) {
                current_indent += 1;
                Advance();
                return ParseNextToken();
            }
            else {
                if (current_indent > -1) {
                    ResolveIndendation();
                    current_indent = -1;
                }
                switch (_currentToken.Type) {
                    case TokenType.Whitespace: {
                            do {
                                Advance();
                            } while (_currentToken != null && _currentToken.Type == TokenType.Whitespace && _currentToken.Type != TokenType.EndOfFile);
                            found_token = CreateToken(OTT.DM_Whitespace, " ", parent: original_token);
                            break;
                        }
                    case TokenType.Symbol: {
                            found_token = ProcessSymbol(_currentToken);
                            Advance();
                            if (found_token.Type == OTT.DM_RightCurlyBracket) {
                                _pendingTokenQueue.Enqueue(found_token);
                                found_token = CreateToken(OTT.Newline, "\n");
                            }
                            break;
                        }
                    case TokenType.String: {
                            if (_currentToken.Value is StringTokenInfo s) {
                                if (s.isRaw) {
                                    found_token = CreateToken(OTT.DM_RawString, parent: _currentToken);
                                }
                                else if (!s.isLong && s.delimiter == "'") {
                                    found_token = CreateToken(OTT.DM_Resource, parent: _currentToken);
                                }
                                else {
                                    _currentToken.Text = EscapeString(_currentToken.Text);
                                    s.Transform(EscapeString);
                                    found_token = CreateToken(OTT.DM_String, parent: _currentToken);
                                }
                            }
                            else {
                                found_token = CreateToken(OTT.DM_String, parent: _currentToken);
                            }
                            Advance();
                            break;
                        }
                    case TokenType.Identifier: {
                            string text = _currentToken.Text;
                            if (DM.DMLexer.Keywords.TryGetValue(text, out OTT keywordType)) {
                                found_token = CreateToken(keywordType, text);
                            }
                            else {
                                found_token = CreateToken(OTT.DM_Identifier, text);
                            }
                            Advance();
                            break;
                        }
                    case TokenType.Numeric: {
                            string text = _currentToken.Text;
                            // TODO assign a match to the other Single states
                            if (text == "1.#INF") {
                                found_token = CreateToken(OTT.DM_Float, text, value: Single.PositiveInfinity);
                            }
                            else if (text.StartsWith("0x") && Int32.TryParse(text.Substring(2), NumberStyles.HexNumber, null, out int intValue)) {
                                found_token = CreateToken(OTT.DM_Integer, text, value: intValue);
                            }
                            else if (Int32.TryParse(text, out intValue)) {
                                found_token = CreateToken(OTT.DM_Integer, text, intValue);
                            }
                            else if (Single.TryParse(text, out float floatValue)) {
                                found_token = CreateToken(OTT.DM_Float, text, floatValue);
                            }
                            else {
                                found_token = CreateToken(OTT.Error, text, value: "Invalid number");
                            }
                            Advance();
                            break;
                        }
                    case TokenType.EndOfFile: found_token = CreateToken(OTT.Newline, parent: original_token); Advance(); break;
                    default: throw new Exception("Invalid token " + _currentToken.ToString());
                }
            }
            return found_token;
        }

        public string EscapeString(string s) {
            if (s == null) { return s; }

            int write_pos = 0;
            int read_pos = 0;
            char[] cs = s.ToCharArray();
            for (read_pos = 0; read_pos < s.Length;) {
                char c = s[read_pos++];
                if (c == '\\') {
                    if (read_pos >= s.Length) {
                        break;
                    }
                    char c2 = s[read_pos++];
                    // TODO b here is a bug in the \black escape
                    if (c2 == 'n') {
                        cs[write_pos++] = '\n';
                    }
                    else if (c2 == 't') {
                        cs[write_pos++] = '\t';
                    }
                    else if (c2 == '\\') {
                        cs[write_pos++] = '\\';
                    }
                    else if (c2 == '"') {
                        cs[write_pos++] = '\"';
                    }
                    else if (c2 == '\'') {
                        cs[write_pos++] = '\'';
                    }
                    else if (c2 == '[') {
                        cs[write_pos++] = '[';
                    }
                    else if (c2 == ']') {
                        cs[write_pos++] = ']';
                    }
                    else {
                        cs[write_pos++] = c;
                        cs[write_pos++] = c2;
                    }
                }
                else {
                    cs[write_pos++] = c;
                }
            }
            return new string(cs.Take<char>(write_pos).ToArray());
        }
        public static Dictionary<string, OTT> simpleSymbolMap = new() {
            { "(", OTT.DM_LeftParenthesis },
            { ")", OTT.DM_RightParenthesis },
            { "[", OTT.DM_LeftBracket },
            { "]", OTT.DM_RightBracket },
            { "{", OTT.DM_LeftCurlyBracket },
            { "}", OTT.DM_RightCurlyBracket },
            { ",", OTT.DM_Comma },
            { ":", OTT.DM_Colon },
            { ";", OTT.DM_Semicolon },
            { "?[", OTT.DM_QuestionLeftBracket },
            { "?.", OTT.DM_QuestionPeriod },
            { "?:", OTT.DM_QuestionColon },
            { "?", OTT.DM_Question },
            { ".......", OTT.DM_SevenDots },
            { "......", OTT.DM_SixDots },
            { "...", OTT.DM_IndeterminateArgs },
            { "..", OTT.DM_SuperProc },
            { ".", OTT.DM_Period },
            { "/", OTT.DM_Slash },
            { "\\", OTT.DM_Backslash },
            { "/=", OTT.DM_SlashEquals },
            { "=", OTT.DM_Equals },
            { "==", OTT.DM_EqualsEquals },
            { "!", OTT.DM_Exclamation },
            { "!=", OTT.DM_ExclamationEquals },
            { "^", OTT.DM_Xor },
            { "^=", OTT.DM_XorEquals },
            { "%", OTT.DM_Modulus },
            { "%=", OTT.DM_ModulusEquals },
            { "~", OTT.DM_Tilde },
            { "~=", OTT.DM_TildeEquals },
            { "~!", OTT.DM_TildeExclamation },
            { "&", OTT.DM_And },
            { "&&", OTT.DM_AndAnd },
            { "&=", OTT.DM_AndEquals },
            { "+", OTT.DM_Plus },
            { "++", OTT.DM_PlusPlus },
            { "+=", OTT.DM_PlusEquals },
            { "-", OTT.DM_Minus },
            { "--", OTT.DM_MinusMinus },
            { "-=", OTT.DM_MinusEquals },
            { "*", OTT.DM_Star },
            { "**", OTT.DM_StarStar },
            { "*=", OTT.DM_StarEquals },
            { "|", OTT.DM_Bar },
            { "||", OTT.DM_BarBar },
            { "|=", OTT.DM_BarEquals },
            { "<", OTT.DM_LessThan },
            { "<<", OTT.DM_LeftShift },
            { "<=", OTT.DM_LessThanEquals },
            { "<<=", OTT.DM_LeftShiftEquals },
            { ">", OTT.DM_GreaterThan },
            { ">>", OTT.DM_RightShift },
            { ">=", OTT.DM_GreaterThanEquals },
            { ">>=", OTT.DM_RightShiftEquals }
        };

        Original.Token AcceptSimpleToken(PreprocessorToken t) {
            return CreateToken(simpleSymbolMap[t.Text], t.Text);
        }
        Original.Token ProcessSymbol(PreprocessorToken token) {
            switch (token.Text) {
                case "[":
                case "(": BracketNesting++; break;
                case "]":
                case ")": BracketNesting = Math.Max(BracketNesting - 1, 0); break;
            }
            return AcceptSimpleToken(token);
        }
    }
}
