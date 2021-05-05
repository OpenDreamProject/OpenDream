using OpenDreamShared.Compiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace DMCompiler.Preprocessor {
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

        public virtual List<Token> Expand(List<List<Token>> parameters) {
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

                            expandedTokens.Add(new Token(TokenType.DM_Preproc_String, "\"", null, 0, 0, null));
                            foreach (Token parameterToken in parameter) {
                                expandedTokens.Add(new Token(TokenType.DM_Preproc_String, null, parameterToken.Text, 0, 0, null));
                            }
                            expandedTokens.Add(new Token(TokenType.DM_Preproc_String, "\"", null, 0, 0, null));
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
}
