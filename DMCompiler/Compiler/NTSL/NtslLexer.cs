namespace DMCompiler.Compiler.NTSL;

public sealed class NtslLexer(string sourceName, IEnumerable<Token> source) : TokenLexer(sourceName, source) {
    private static readonly Dictionary<string, TokenType> Keywords = new() {
        { "def", TokenType.NTSL_Def },
        { "return", TokenType.NTSL_Return }
    };

    protected override Token ParseNextToken() {
        Token token;

        if (AtEndOfSource) {
            token = CreateToken(TokenType.EndOfFile, '\0');
        } else {
            Token preprocToken = GetCurrent();

            switch (preprocToken.Type) {
                case TokenType.Newline:
                case TokenType.DM_Preproc_Whitespace:
                    Advance();
                    token = CreateToken(TokenType.Skip, ' '); // Not whitespace-sensitive
                    break;

                case TokenType.DM_Preproc_Punctuator_LeftParenthesis: Advance(); token = CreateToken(TokenType.NTSL_LeftParenthesis, preprocToken.Text); break;
                case TokenType.DM_Preproc_Punctuator_RightParenthesis: Advance(); token = CreateToken(TokenType.NTSL_RightParenthesis, preprocToken.Text); break;
                case TokenType.DM_Preproc_Punctuator_Comma: Advance(); token = CreateToken(TokenType.NTSL_Comma, ","); break;
                case TokenType.DM_Preproc_Punctuator_Semicolon: Advance(); token = CreateToken(TokenType.NTSL_Semicolon, ";"); break;
                case TokenType.DM_Preproc_ConstantString: Advance(); token = CreateToken(TokenType.NTSL_String, preprocToken.Text, preprocToken.Value); break;
                case TokenType.DM_Preproc_Punctuator: {
                    Advance();

                    string c = preprocToken.Text;
                    switch (c) {
                        case "{": token = CreateToken(TokenType.NTSL_LeftCurlyBracket, c); break;
                        case "}": token = CreateToken(TokenType.NTSL_RightCurlyBracket, c); break;
                        case "$": token = CreateToken(TokenType.NTSL_VarIdentifierPrefix, c); break;
                        case "=": token = CreateToken(TokenType.NTSL_Equals, c); break;
                        default: token = CreateToken(TokenType.Error, c, $"Invalid punctuator token '{c}'"); break;
                    }

                    break;
                }
                case TokenType.DM_Preproc_Identifier: {
                    var identifierText = preprocToken.Text;
                    var tokenType = Keywords.GetValueOrDefault(identifierText, TokenType.NTSL_Identifier);

                    Advance();
                    token = CreateToken(tokenType, identifierText);
                    break;
                }
                case TokenType.DM_Preproc_Number: {
                    Advance();

                    token = float.TryParse(preprocToken.Text, out float floatValue) ?
                        CreateToken(TokenType.NTSL_Number, preprocToken.Text, floatValue) :
                        CreateToken(TokenType.Error, preprocToken.Text, "Invalid number");
                    break;
                }
                case TokenType.NTSL_EndFile:
                case TokenType.EndOfFile: token = preprocToken; Advance(); break;
                default: token = CreateToken(TokenType.Error, preprocToken.Text, $"Invalid token {preprocToken.Type}");
                    Advance(); break;
            }
        }

        return token;
    }
}
