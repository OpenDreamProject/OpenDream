using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler.Experimental {
    class DMMacro {
        protected List<string> _parameters;
        private List<PreprocessorToken> _tokens;
        private string _overflowParameter = null;
        private int _overflowParameterIndex;

        public DMMacro(List<string> parameters, List<PreprocessorToken> tokens) {
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
            if (_parameters == null) { return false; }
            return _parameters.Count != 0;
        }

        public List<PreprocessorToken> Substitute(PreprocessorToken replacing, List<PreprocessorToken> tokens, List<List<PreprocessorToken>> parameters) {
            List<PreprocessorToken> expandedTokens = new();
            int ctok = 0;
            while (ctok < tokens.Count) {
                var token = tokens[ctok];
                if (token.Type == TokenType.Identifier) {
                    string parameterName = token.Text;
                    int parameterIndex = _parameters.IndexOf(parameterName);
                    if (parameterIndex != -1 && parameters.Count > parameterIndex) {
                        var parameter = parameters[parameterIndex];
                        foreach (var parameterToken in parameter) {
                            expandedTokens.Add(parameterToken);
                        }
                    }
                    else if (_overflowParameter != null && parameterName == _overflowParameter) {
                        for (int i = _overflowParameterIndex; i < parameters.Count; i++) {
                            foreach (var parameterToken in parameters[i]) {
                                expandedTokens.Add(parameterToken);
                            }
                            expandedTokens.Add(new PreprocessorToken(TokenType.Symbol, ",", loc: replacing.Location));
                        }
                    }
                    else {
                        expandedTokens.Add(token);
                    }
                    ctok += 1;
                }
                else if (token.Type == TokenType.String) {
                    var new_token = new PreprocessorToken(TokenType.String, token.Text, token.Value, token.WhitespaceOnly, token.Location);
                    if (new_token.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                        new_token.Value = info.Copy();
                        info = new_token.Value as StringTokenInfo;
                        info.nestedTokenInfo.Tokens = Substitute(replacing, info.nestedTokenInfo.Tokens, parameters);
                    }
                    if (token.Value is NestedTokenInfo nti && nti.Tokens != null) {
                        new_token.Value = nti.Copy();
                        nti = new_token.Value as NestedTokenInfo;
                        nti.Tokens = Substitute(replacing, nti.Tokens, parameters);
                    }
                    expandedTokens.Add(new_token);
                    ctok += 1;
                }
                else if (token.IsSymbol("#")) {
                    System.Text.StringBuilder stringified = new();
                    if (ctok + 1 > tokens.Count) {
                        throw new Exception("Macro # has no parameter");
                    }
                    PreprocessorToken stringifyToken = tokens[ctok + 1];
                    string parameterName = stringifyToken.Text;
                    int parameterIndex = _parameters.IndexOf(parameterName);
                    if (parameterIndex != -1 && parameters.Count > parameterIndex) {
                        var parameter = parameters[parameterIndex];
                        foreach (var parameterToken in parameter) {
                            stringified.Append(parameterToken.Text);
                        }
                    }
                    expandedTokens.Add(new PreprocessorToken(TokenType.String, stringified.ToString(), loc: replacing.Location));
                    ctok += 2;
                }
                else {
                    expandedTokens.Add(token);
                    ctok += 1;
                }
            }
            var new_tokens = new List<PreprocessorToken>();
            foreach (var etoken in expandedTokens) {
                var new_token = new PreprocessorToken(etoken.Type, etoken.Text, etoken.Value, etoken.WhitespaceOnly, loc: replacing.Location);
                new_token.ExpandEligible = etoken.ExpandEligible;
                new_tokens.Add(new_token);
            }
            return new_tokens;
        }

        public virtual List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            //Console.WriteLine(replacing.Text);
            //Console.WriteLine($"----------------------------------------------substitute: {replacing} \n{PreprocessorToken.PrintTokens(_tokens)}");
            //if (parameters != null) {
            //    foreach (var parameter in parameters) {
            //        Console.WriteLine($"param: \n{PreprocessorToken.PrintTokens(parameter)}");
            //    }
            //}
            List<PreprocessorToken> expandedTokens = new();
            //Console.WriteLine(replacing.Text + " | " + parameters + " | " + parameters?.Count);
            if (parameters == null || parameters?.Count == 0 && HasParameters()) {
                expandedTokens.Add(replacing);
                replacing.ExpandEligible = false;
            }
            else if (parameters != null && parameters.Count != 0 && !HasParameters()) {
                expandedTokens.Add(replacing);
                replacing.ExpandEligible = false;
            }
            else {
                expandedTokens = Substitute(replacing, _tokens, parameters);
            }
            //Console.WriteLine($"----------------------------------------------result: \n{PreprocessorToken.PrintTokens(expandedTokens)}");
            var new_tokens = new List<PreprocessorToken>();
            foreach (var etoken in expandedTokens) {
                var new_token = new PreprocessorToken(etoken.Type, etoken.Text, etoken.Value, etoken.WhitespaceOnly, loc: replacing.Location);
                new_token.ExpandEligible = etoken.ExpandEligible;
                new_tokens.Add(new_token);
            }
            return new_tokens;
        }
    }

    class DMMacroLine : DMMacro {
        public DMMacroLine() : base(null, null) { }

        public override List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            return new() {
                new PreprocessorToken(TokenType.Numeric, replacing.Location.Line.ToString(), loc: replacing.Location)
            };
        }
    }

    class DMMacroFile : DMMacro {
        public DMMacroFile() : base(null, null) { }

        public override List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            string path = replacing.Location.Source.IncludePath;
            return new() {
                new PreprocessorToken(TokenType.String, path, loc: replacing.Location)
            };
        }
    }

    class DMDefinedMacro : DMMacro {
        private Dictionary<string, DMMacro> _defines;
        public DMDefinedMacro(Dictionary<string, DMMacro> defines) : base(null, null) {
            _defines = defines;
            _parameters = new List<String> { "identifier" };
        }
        public override List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            string result;
            string defined_ident = parameters[0][0].Text;
            if (_defines.ContainsKey(defined_ident)) {
                result = "1";
            }
            else {
                result = "0";
            }
            //Console.WriteLine(defined_ident + " | " + result);
            return new List<PreprocessorToken> { new PreprocessorToken(TokenType.Numeric, result, loc: replacing.Location) };
        }
    }
}
