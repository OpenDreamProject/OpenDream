
using System;
using System.Collections;
using System.Collections.Generic;

namespace DMCompiler.Compiler.Experimental {

    public class DMPreprocessorLexer : IEnumerator<PreprocessorToken> {
        public TextProducer _tp;

        PreprocessorToken _current = null;
        public PreprocessorToken Current { get { return _current; } }
        object IEnumerator.Current {
            get { return Current; }
        }

        public bool MoveNext() {
            _current = Advance();
            return true;
        }
        public void Reset() { throw new Exception("not implemented"); }
        public void Dispose() { }

        public DMPreprocessorLexer() {
            _tp = new TextProducer();
        }
        public void Include(SourceText srctext) {
            _tp.Include(srctext);
        }
        public void SavePosition() {
            _tp.SavePosition();
        }
        public void RestorePosition() {
            _tp.RestorePosition();
        }
        public void AcceptPosition() {
            _tp.AcceptPosition();
        }

        public bool CheckText(PreprocessorToken token, TokenType ty, string s) {
            if (token.Type == ty && token.Text == s) {
                return true;
            }
            return false;
        }

        public string ReadLine() {
            int start = _tp.CurrentPosition();
            while (_tp.Peek(0) != '\n') {
                _tp.Advance(1);
            }
            return _tp.GetString(start, _tp.CurrentPosition() - 1);
        }

        public PreprocessorToken AcceptToken(TokenType ty, int n) {
            if (n == 0) {
                throw new Exception("no progress");
            }
            PreprocessorToken token = new PreprocessorToken(ty, _tp.GetString(n), loc: current_location);
            token.WhitespaceOnly = whitespace_only_line;
            if (_tp.Peek(0) != ' ' && _tp.Peek(0) != '\t') {
                whitespace_only_line = false;
            }
            _tp.Advance(n);
            return token;
        }

        bool whitespace_only_line;
        SourceLocation current_location;

        public PreprocessorToken Advance() {
            while (true) {
                current_location = _tp.CurrentLocation();

                //Console.Write($"{_tp.Peek(0)}");
                if (_tp.Peek(-1) == '\n' || _tp.Peek(-1) == null) {
                    whitespace_only_line = true;
                }

                char? c = _tp.Peek(0);
                switch (c) {
                    case null: _tp.Advance(1); return new PreprocessorToken(TokenType.EndOfFile, loc: current_location);
                    case ' ': return AcceptToken(TokenType.Whitespace, 1);
                    case '\t': return AcceptToken(TokenType.Whitespace, 1);
                    case '\n': return AcceptToken(TokenType.Newline, 1);
                    case '.': return ReadDots();
                    case '[':
                    case ']':
                    case ';':
                    case ':':
                    case ',':
                    case '(':
                    case ')': return AcceptToken(TokenType.Symbol, 1);
                    case '>': {
                            switch (_tp.Peek(1)) {
                                case '>': {
                                        if (_tp.Peek(2) == '=') {
                                            return AcceptToken(TokenType.Symbol, 3);
                                        }
                                        else {
                                            return AcceptToken(TokenType.Symbol, 2);
                                        }
                                    }
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '?': {
                            switch (_tp.Peek(1)) {
                                case '.': return AcceptToken(TokenType.Symbol, 2);
                                case ':': return AcceptToken(TokenType.Symbol, 2);
                                case '[': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '<': {
                            switch (_tp.Peek(1)) {
                                case '<': {
                                        if (_tp.Peek(2) == '=') {
                                            return AcceptToken(TokenType.Symbol, 3);
                                        }
                                        else {
                                            return AcceptToken(TokenType.Symbol, 2);
                                        }
                                    }
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '|': {
                            switch (_tp.Peek(1)) {
                                case '|': return AcceptToken(TokenType.Symbol, 2);
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '*': {
                            switch (_tp.Peek(1)) {
                                case '*': return AcceptToken(TokenType.Symbol, 2);
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '+': {
                            switch (_tp.Peek(1)) {
                                case '+': return AcceptToken(TokenType.Symbol, 2);
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '-': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                case '-': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '&': {
                            switch (_tp.Peek(1)) {
                                case '&': return AcceptToken(TokenType.Symbol, 2);
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '~': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                case '!': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '%': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '^': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '!': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '=': {
                            switch (_tp.Peek(1)) {
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '\\': {
                            switch (_tp.Peek(1)) {
                                case '\n': _tp.Advance(2); Splice(); continue;
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '/': {
                            switch (_tp.Peek(1)) {
                                case '/': _tp.Advance(2); SkipSingleComment(); return new PreprocessorToken(TokenType.Whitespace, " ", loc: current_location);
                                case '*': _tp.Advance(2); SkipMultiComment(); return new PreprocessorToken(TokenType.Whitespace, " ", loc: current_location);
                                case '=': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '@': {
                            _tp.Advance(1); return LexRawString();
                        }
                    case '\'':
                    case '"': {
                            return LexString(false);
                        }
                    case '{': {
                            if (_tp.Peek(1) == '"') {
                                return LexString(true);
                            }
                            else {
                                return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    case '}': {
                            return AcceptToken(TokenType.Symbol, 1);
                        }
                    case '#': {
                            switch (_tp.Peek(1)) {
                                case '#': return AcceptToken(TokenType.Symbol, 2);
                                default: return AcceptToken(TokenType.Symbol, 1);
                            }
                        }
                    default: {
                            char cc = (char)c;
                            if (_tp.IsIdentifierStart(cc)) {
                                return ReadIdentifier();
                            }
                            else if (_tp.IsNumeric(cc)) {
                                return ReadNumeric();
                            }
                            else {
                                throw new Exception("Unknown Preprocessing character " + c);
                            }
                        }
                }
            }
            throw new Exception("Advance broke loop");
        }

        public PreprocessorToken ReadIdentifier() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c is char cc && _tp.IsIdentifier(cc)) {
                    n += 1;
                    continue;
                }
                break;
            }
            return AcceptToken(TokenType.Identifier, n);
        }

        public PreprocessorToken ReadNumeric() {
            int n = 0;
            string error = null;
            while (true) {
                char? c = _tp.Peek(n);
                if (c == '#') {
                    if (_tp.Match("INF", n + 1)) {
                        n += 4;
                        break;
                    }
                    error = "Invalid # modifier in numeric literal";
                    break;
                }
                if (c == 'e' || c == 'E') {
                    c = _tp.Peek(n + 1);
                    if (c == '-' || c == '+') {
                        n += 2;
                        continue;
                    }
                    else {
                        n += 1;
                        continue;
                    }
                }
                else if (c is char cc && _tp.IsHex(cc) || c == '.' || c == 'p' || c == 'P') {
                    n += 1;
                    continue;
                }
                else {
                    break;
                }
            }
            if (error != null) {
                throw new Exception("Error in numeric literal: " + error);
            }
            return AcceptToken(TokenType.Numeric, n);
        }

        public PreprocessorToken ReadDots() {
            int n = 0;
            while (_tp.Peek(n) == '.') {
                n += 1;
            }
            return AcceptToken(TokenType.Symbol, n);
        }

        public void Splice() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c == ' ' || c == '\t' || c == '\n') {
                    n += 1;
                    continue;
                }
                break;
            }
            whitespace_only_line = true;
            _tp.Advance(n);
        }
        public void SkipMultiComment() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                bool isStar = c == '*';

                if (isStar && _tp.Peek(n + 1) == '/') {
                    n += 2;
                    break;
                }
                else if (c == null) throw new Exception("Expected \"*/\" to end multiline comment");
                else { n += 1; }
            }
            _tp.Advance(n);
        }
        public void SkipSingleComment() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c != '\n') {
                    n += 1;
                    continue;
                }
                whitespace_only_line = true;
                break;
            }
            _tp.Advance(n);
        }

        public PreprocessorToken LexRawString() {
            char? delimiter = null;
            string text_delimiter = null;
            bool is_long = false;
            if (_tp.Peek(0) == '{') {
                if (_tp.Peek(1) == '"') {
                    delimiter = '"';
                    is_long = true;
                    if (_tp.Peek(2) == '\n') {
                        _tp.Advance(3);
                    }
                    else {
                        _tp.Advance(2);
                    }
                }
            }
            else if (_tp.Peek(0) == '(') {
                _tp.Advance(1);
                int delim_n = 0;
                while (_tp.Peek(delim_n) != ')') {
                    delim_n += 1;
                }
                text_delimiter = _tp.GetString(delim_n);
                _tp.Advance(delim_n + 1);
            }
            else {
                delimiter = _tp.Peek(0);
                _tp.Advance(1);
            }
            int start = _tp.CurrentPosition();
            int end = start;
            bool last_n = false;
            while (true) {
                char? c = _tp.Peek(0);
                if (text_delimiter != null && _tp.Match(text_delimiter)) {
                    _tp.Advance(text_delimiter.Length);
                    break;
                }
                else if (c == null) {
                    throw new Exception("EOF found in string constant");
                }
                else if (c == delimiter) {
                    if (is_long) {
                        if (_tp.Peek(1) == '}') {
                            end = _tp.CurrentPosition() - 1;
                            _tp.Advance(2);
                            break;
                        }
                        _tp.Advance(1);
                        continue;
                    }
                    end = _tp.CurrentPosition();
                    _tp.Advance(1);
                    break;
                }
                else if (c == '\n' && !is_long) { throw new Exception("a line break cannot end a simple raw string"); }
                else if (c == '\n') { last_n = true; _tp.Advance(1); }
                else {
                    last_n = false;
                    _tp.Advance(1);
                }
            }
            StringTokenInfo info = new();
            info.isLong = is_long;
            info.isRaw = true;
            info.delimiter = text_delimiter != null ? text_delimiter : delimiter?.ToString();
            PreprocessorToken token = new PreprocessorToken(TokenType.String, _tp.GetString(start, end - (last_n ? 1 : 0)), info, loc: current_location);
            return token;
        }

        public PreprocessorToken LexString(bool isLong) {
            char? long_delim = null;
            char? delimiter;
            if (isLong) { long_delim = '}'; delimiter = _tp.Peek(1); _tp.Advance(2); }
            else { delimiter = _tp.Peek(0); _tp.Advance(1); }
            List<PreprocessorToken> nestedTokens = new();
            int full_start = _tp.CurrentPosition();
            int start = _tp.CurrentPosition();
            int end;
            while (true) {
                char? c = _tp.Peek(0);
                if (c == '[') {
                    end = _tp.CurrentPosition();
                    nestedTokens.Add(new PreprocessorToken(TokenType.String, _tp.GetString(start, end), loc: current_location));
                    _tp.Advance(1);
                    var exprTokens = LexNestedExpression();
                    nestedTokens.Add(new PreprocessorToken(TokenType.String, "", new NestedTokenInfo("stringify", exprTokens), loc: current_location));
                    start = _tp.CurrentPosition();
                }
                else if (c == null) {
                    throw new Exception("EOF in string");
                }
                else if (c == '\\' && _tp.Peek(1) == '\n') {
                    end = _tp.CurrentPosition();
                    nestedTokens.Add(new PreprocessorToken(TokenType.String, _tp.GetString(start, end), loc: current_location));
                    _tp.Advance(2); Splice();
                    start = _tp.CurrentPosition();
                }
                else if (c == '\n' && !isLong) {
                    throw new Exception("Newline in string");
                }
                else if (c == '\\') {
                    if (_tp.Match("ref[", 1)) {
                        end = _tp.CurrentPosition();
                        nestedTokens.Add(new PreprocessorToken(TokenType.String, _tp.GetString(start, end), loc: current_location));
                        _tp.Advance(5);
                        var exprTokens = LexNestedExpression();
                        nestedTokens.Add(new PreprocessorToken(TokenType.String, "", new NestedTokenInfo("ref", exprTokens), loc: current_location));
                        start = _tp.CurrentPosition();
                    }
                    else {
                        _tp.Advance(2);
                    }
                }
                else if (long_delim != null && _tp.Peek(0) == delimiter && _tp.Peek(1) == long_delim) {
                    end = _tp.CurrentPosition();
                    _tp.Advance(2);
                    break;
                }
                else if (long_delim == null && c == delimiter) {
                    end = _tp.CurrentPosition();
                    _tp.Advance(1);
                    break;
                }
                else { _tp.Advance(1); }
            }
            StringTokenInfo info = new();
            info.isLong = isLong;
            info.isRaw = false;
            info.delimiter = (long_delim != null ? long_delim.ToString() : "") + (delimiter != null ? delimiter.ToString() : "");
            if (nestedTokens.Count > 0) {
                nestedTokens.Add(new PreprocessorToken(TokenType.String, _tp.GetString(start, end), loc: current_location));
                bool is_interp = false;
                foreach (var token in nestedTokens) {
                    if (token.Value != null) { is_interp = true; }
                }
                if (is_interp) {
                    info.nestedTokenInfo = new NestedTokenInfo("root", nestedTokens);
                }
                else {
                    System.Text.StringBuilder sb = new();
                    foreach (var token in nestedTokens) {
                        sb.Append(token.Text);
                    }
                    return new PreprocessorToken(TokenType.String, sb.ToString(), info, loc: current_location);
                }
            }
            return new PreprocessorToken(TokenType.String, _tp.GetString(full_start, end), info, loc: current_location);

            List<PreprocessorToken> LexNestedExpression() {
                List<PreprocessorToken> exprTokens = new();
                PreprocessorToken exprToken = Advance();
                int bracketNesting = 0;
                while (!(bracketNesting == 0 && exprToken.IsSymbol("]")) && _tp.Peek(0) != null) {
                    exprTokens.Add(exprToken);
                    if (exprToken.IsSymbol("[")) bracketNesting++;
                    if (exprToken.IsSymbol("]")) bracketNesting--;
                    exprToken = Advance();
                }
                if (!exprToken.IsSymbol("]")) throw new Exception("Expected ']' to end expression");
                return exprTokens;
            }

        }
    }
}
