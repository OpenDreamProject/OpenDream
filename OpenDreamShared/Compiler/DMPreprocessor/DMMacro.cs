using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Compiler.DMPreprocessor {
    class DMMacro {
        private List<string> _parameters;
        private List<Token> _tokens;
        private string _overflowParameter = null;
        private int _overflowParameterIndex;

        public DMMacro(List<string> parameters, List<Token> tokens) {
            _parameters = parameters;
            _tokens = tokens;

            if (_parameters != null) {
                for (int i = 0; i < _parameters.Count; i++) {
                    string parameter = _parameters[i];

                    if (parameter.EndsWith("...")) {
                        _overflowParameter = parameter.Substring(0, parameter.Length - 3);
                        _overflowParameterIndex = i;

                        break;
                    }
                }
            }
        }

        public bool HasParameters() {
            return _parameters != null;
        }

        public virtual List<Token> Expand(Token replacing, List<List<Token>> parameters) {
            if (parameters == null && HasParameters()) throw new ArgumentException("This macro requires parameters");

            List<Token> expandedTokens = new();
            foreach (Token token in _tokens) {
                if (HasParameters()) {
                    string parameterName = (token.Type == TokenType.DM_Preproc_TokenConcat || token.Type == TokenType.DM_Preproc_ParameterStringify) ? (string)token.Value : token.Text;
                    int parameterIndex = _parameters.IndexOf(parameterName);

                    if (parameterIndex != -1 && parameters.Count > parameterIndex) {
                        List<Token> parameter = parameters[parameterIndex];

                        if (token.Type == TokenType.DM_Preproc_ParameterStringify) {
                            StringBuilder tokenTextBuilder = new StringBuilder();

                            tokenTextBuilder.Append('"');
                            foreach (Token parameterToken in parameter) {
                                tokenTextBuilder.Append(parameterToken.Text);
                            }
                            tokenTextBuilder.Append('"');

                            string tokenText = tokenTextBuilder.ToString();
                            expandedTokens.Add(new Token(TokenType.DM_Preproc_ConstantString, tokenText, null, 0, 0, tokenText.Substring(1, tokenText.Length - 2)));
                        } else {
                            foreach (Token parameterToken in parameter) {
                                expandedTokens.Add(parameterToken);
                            }
                        }
                    } else if (_overflowParameter != null && parameterName == _overflowParameter) {
                        for (int i = _overflowParameterIndex; i < parameters.Count; i++) {
                            foreach (Token parameterToken in parameters[i]) {
                                expandedTokens.Add(parameterToken);
                            }

                            expandedTokens.Add(new Token(TokenType.DM_Preproc_Punctuator_Comma, ",", null, 0, 0, null));
                        }
                    } else {
                        expandedTokens.Add(token);
                    }
                } else {
                    expandedTokens.Add(token);
                }
            }

            return expandedTokens;
        }
    }

    class DMMacroLine : DMMacro {
        public DMMacroLine() : base(null, null) { }

        public override List<Token> Expand(Token replacing, List<List<Token>> parameters) {
            return new() {
                new Token(TokenType.DM_Preproc_Number, replacing.Line.ToString(), replacing.SourceFile, replacing.Line, replacing.Column, null)
            };
        }
    }

    class DMMacroFile : DMMacro {
        public DMMacroFile() : base(null, null) { }

        public override List<Token> Expand(Token replacing, List<List<Token>> parameters) {
            return new() {
                new Token(TokenType.DM_Preproc_ConstantString, $"\"{replacing.SourceFile}\"", replacing.SourceFile, replacing.Line, replacing.Column, replacing.SourceFile)
            };
        }
    }
}
