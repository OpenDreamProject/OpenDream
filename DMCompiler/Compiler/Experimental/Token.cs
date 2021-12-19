
using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler.Experimental {
    public enum TokenType : byte {
        Info,
        EndOfFile,
        Whitespace,
        Newline,
        Symbol,
        String,
        Identifier,
        Numeric,
    }

    class NestedTokenInfo {
        public string Type;
        public List<PreprocessorToken> Tokens;

        public NestedTokenInfo(string type, List<PreprocessorToken> tokens) {
            Type = type;
            Tokens = tokens;
        }

        public NestedTokenInfo Copy() {
            return new NestedTokenInfo(Type, Tokens);
        }

        public void Transform(Func<string, string> fn) {
            foreach (var token in Tokens) {
                if (token.Type == TokenType.String) {
                    token.Text = fn(token.Text);
                }
                if (token.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                    info.Transform(fn);
                }
                if (token.Value is NestedTokenInfo nti && nti.Tokens != null) {
                    nti.Transform(fn);
                }
            }
        }
    }

    class StringTokenInfo {
        public string delimiter;
        public bool isRaw;
        public bool isLong;
        public NestedTokenInfo nestedTokenInfo;

        public StringTokenInfo Copy() {
            var sti = new StringTokenInfo();
            sti.delimiter = delimiter;
            sti.isRaw = isRaw;
            sti.isLong = isLong;
            sti.nestedTokenInfo = nestedTokenInfo.Copy();
            return sti;
        }

        public void Transform(Func<string, string> fn) {
            nestedTokenInfo?.Transform(fn);
        }
    }

    public partial class PreprocessorToken {
        public TokenType Type;
        public object Value;

        public string Text;
        public SourceLocation Location;

        public bool ExpandEligible = true;
        public bool WhitespaceOnly = false;

        public PreprocessorToken(TokenType ty, string text = null, object value = null, bool whitespace_only = false, SourceLocation loc = null) {
            Type = ty;
            Text = text;
            Value = value;
            WhitespaceOnly = whitespace_only;
            if (loc == null) {
                Location = new SourceLocation();
                Location.Source = new SourceText("", "");
            }
            else {
                Location = loc;
            }
        }

        public DMToken DMToken() {
            return DMToken(Type);
        }
        public DMToken DMToken(TokenType ty) {
            return new DMToken(ty, Value, this);
        }

        public bool IsSymbol(string sym) {
            if (Type != TokenType.Symbol) { return false; }
            if (Text != sym) { return false; }
            return true;
        }
        public bool IsIdentifier(string id) {
            if (Type != TokenType.Identifier) { return false; }
            if (Text != id) { return false; }
            return true;
        }
        public override string ToString() {
            return $"{Type} {Text} {Value}";
        }

        public static string PrintTokens(IEnumerable<PreprocessorToken> tokens) {
            System.Text.StringBuilder sb = new();
            foreach (var token in tokens) {
                sb.Append(token.ToString() + " | ");
            }
            sb.Append("");
            return sb.ToString();
        }
    }

    public partial class DMToken {
        public TokenType Type;
        public object Value;
        public PreprocessorToken Parent;

        public DMToken(TokenType ty, object value, PreprocessorToken parent) {
            Type = ty;
            Value = value;
            Parent = parent;
        }
        public override string ToString() {
            return $"{Type} {Parent.Text} {Value}";
        }

    }
}
