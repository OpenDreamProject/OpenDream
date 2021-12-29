using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {

        public static bool ExperimentalPreproc = false;
        public DMASTExpression ExperimentalConstant() {
            Token constantToken = Current();

            switch (constantToken.Type) {
                case TokenType.DM_Integer: Advance(); return new DMASTConstantInteger(constantToken.Location, (int)constantToken.Value);
                case TokenType.DM_Float: Advance(); return new DMASTConstantFloat(constantToken.Location, (float)constantToken.Value);
                case TokenType.DM_Resource: Advance(); return new DMASTConstantResource(constantToken.Location, constantToken.Text);
                case TokenType.DM_Null: Advance(); return new DMASTConstantNull(constantToken.Location);
                case TokenType.DM_RawString: Advance(); return new DMASTConstantString(constantToken.Location, constantToken.Text);
                case TokenType.DM_String: {
                        DMASTExpression expr;
                        if (constantToken.Value is Experimental.StringTokenInfo sti && sti.nestedTokenInfo != null) {
                            StringBuilder format_string = new();
                            List<DMASTExpression> interp_exprs = new();
                            foreach (var token in sti.nestedTokenInfo.Tokens) {
                                if (token.Type == Experimental.TokenType.String) {
                                    var nti = token.Value as Experimental.NestedTokenInfo;
                                    if (nti != null) {
                                        format_string.Append((char)0xFF);
                                        switch (nti.Type) {
                                            case "stringify": format_string.Append((char)StringFormatTypes.Stringify); break;
                                            case "ref": format_string.Append((char)StringFormatTypes.Ref); break;
                                            default: throw new Exception("Unknown NestedTokenInfo type");
                                        }
                                        // TODO this newline should be fixed in the parser's Expression function
                                        nti.Tokens.Add(new Experimental.PreprocessorToken(Experimental.TokenType.Newline));
                                        var lexer = new Experimental.PreprocessorTokenConvert(nti.Tokens.GetEnumerator());
                                        interp_exprs.Add(new DMParser(lexer, false).Expression());
                                    }
                                    else {
                                        format_string.Append(token.Text);
                                    }
                                }
                                else {
                                    throw new Exception("invalid nested token in String");
                                }
                            }
                            expr = new DMASTStringFormat(constantToken.Location, format_string.ToString(), interp_exprs.ToArray());
                        }
                        else {
                            expr = new DMASTConstantString(constantToken.Location, constantToken.Text);
                        }
                        Advance(); return expr;
                    }
                default: return null;
            }
        }
    }
}
