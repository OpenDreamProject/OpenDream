namespace DMCompiler {
    enum TokenType {
        //Base lexer
        Error,
        Newline,
        EndOfFile,
        Unknown,
        Skip, //Internally skipped by the lexer

        //DM
        DM_And,
        DM_AndAnd,
        DM_AndEquals,
        DM_Break,
        DM_Comma,
        DM_Dedent,
        DM_Del,
        DM_Equals,
        DM_EqualsEquals,
        DM_Exclamation,
        DM_ExclamationEquals,
        DM_Float,
        DM_For,
        DM_Identifier,
        DM_If,
        DM_In,
        DM_Indent,
        DM_Integer,
        DM_LeftParenthesis,
        DM_Minus,
        DM_MinusEquals,
        DM_New,
        DM_Null,
        DM_Period,
        DM_Plus,
        DM_PlusEquals,
        DM_Proc,
        DM_Resource,
        DM_Return,
        DM_RightParenthesis,
        DM_Slash,
        DM_SlashEquals,
        DM_String,
        DM_SuperProc,
        DM_Switch,
        DM_Var,
        DM_While
    }

    struct Token {
        public TokenType Type;
        public string Text;
        public int Line, Column;
        public object Value;

        public Token(TokenType type, string text, int line, int column, object value) {
            Type = type;
            Text = text;
            Line = line;
            Column = column;
            Value = value;
        }
    }
}
