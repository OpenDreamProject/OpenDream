using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler.Experimental {
    class DMMacro {
        DMPreprocessor _pp;
        protected List<string> _parameters;
        private List<PreprocessorToken> _body;
        private string _overflowParameter = null;
        private int _overflowParameterIndex;

        public DMMacro(DMPreprocessor pp, List<string> parameters, List<PreprocessorToken> tokens) {
            _pp = pp;
            _parameters = parameters;
            _body = tokens;

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

            int ctok = 0;
            if (_body != null) {
                while (ctok < _body.Count) {
                    var token = _body[ctok];
                    if (token.IsSymbol("#") || token.IsSymbol("##")) {
                        var btok = ctok - 1;
                        while (btok >= 0 && _body[btok].Type == TokenType.Whitespace) { btok--; }
                        var ftok = ctok + 1;
                        while (ftok < _body.Count && _body[ftok].Type == TokenType.Whitespace) { ftok++; }
                        if (btok >= 0) { _body[btok].PrescanExpandEligible = false; }
                        if (ftok < _body.Count) { _body[ftok].PrescanExpandEligible = false; }
                    }
                    ctok += 1;
                }
            }
        }
        public bool HasParameters() {
            if (_parameters == null) { return false; }
            return _parameters.Count != 0;
        }

        public int NumParameters() {
            return _parameters.Count;
        }

        public bool VarParameters() {
            return _overflowParameter != null;
        }
        public List<PreprocessorToken> Substitute(PreprocessorToken replacing, List<PreprocessorToken> body, List<List<PreprocessorToken>> parameterValues) {
            List<PreprocessorToken> expandedTokens = new();
            int ctok = 0;
            while (ctok < body.Count) {
                var token = body[ctok];
                if (token.Type == TokenType.Identifier) {
                    string parameterName = token.Text;
                    int parameterIndex = _parameters.IndexOf(parameterName);
                    // This is a parameter subsitution
                    if (parameterIndex != -1 && parameterValues?.Count > parameterIndex) {
                        var parameter = parameterValues[parameterIndex];
                        List<PreprocessorToken> parameterResult;
                        if (token.PrescanExpandEligible) {
                            parameterResult = _pp.FullExpand(parameter);
                        } else {
                            parameterResult = parameter;
                        }
                        foreach (var parameterToken in parameterResult) {
                            var new_token = new PreprocessorToken(parameterToken);
                            expandedTokens.Add(new_token);
                        }
                    }
                    // This is overflow parameter substitution
                    else if (_overflowParameter != null && parameterName == _overflowParameter) {
                        for (int i = _overflowParameterIndex; i < parameterValues.Count; i++) {
                            List<PreprocessorToken> parameterResult;
                            if (token.PrescanExpandEligible) {
                                parameterResult = _pp.FullExpand(parameterValues[i]);
                            } else {
                                parameterResult = parameterValues[i];
                            }
                            foreach (var parameterToken in parameterResult) {
                                var new_token = new PreprocessorToken(parameterToken);
                                expandedTokens.Add(new_token);
                            }
                            expandedTokens.Add(new PreprocessorToken(TokenType.Symbol, ",", loc: replacing.Location));
                        }
                    }
                    else {
                        var new_token = new PreprocessorToken(token);
                        expandedTokens.Add(new_token);
                    }
                    ctok += 1;
                }
                else if (token.Type == TokenType.String) {
                    var new_token = new PreprocessorToken(token);
                    if (new_token.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                        new_token.Value = info.Copy();
                        info = new_token.Value as StringTokenInfo;
                        info.nestedTokenInfo.Tokens = Substitute(replacing, info.nestedTokenInfo.Tokens, parameterValues);
                    }
                    if (token.Value is NestedTokenInfo nti && nti.Tokens != null) {
                        new_token.Value = nti.Copy();
                        nti = new_token.Value as NestedTokenInfo;
                        nti.Tokens = Substitute(replacing, nti.Tokens, parameterValues);
                    }
                    expandedTokens.Add(new PreprocessorToken(new_token));
                    ctok += 1;
                }
                else if (token.IsSymbol("#")) {
                    System.Text.StringBuilder stringified = new();
                    if (ctok + 1 >= body.Count) {
                        throw new Exception("Macro # has no parameter");
                    }
                    PreprocessorToken stringifyToken = body[ctok + 1];
                    string parameterName = stringifyToken.Text;
                    int parameterIndex = _parameters.IndexOf(parameterName);
                    if (parameterIndex != -1 && parameterValues.Count > parameterIndex) {
                        var parameter = parameterValues[parameterIndex];
                        foreach (var parameterToken in parameter) {
                            stringified.Append(parameterToken.Text);
                        }
                    }
                    expandedTokens.Add(new PreprocessorToken(TokenType.String, stringified.ToString(), loc: replacing.Location));
                    ctok += 2;
                }
                else {
                    expandedTokens.Add(new PreprocessorToken(token));
                    ctok += 1;
                }
            }
            return expandedTokens;
        }

        public virtual List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameterValues) {
            if (replacing.expandLevel > 256) {
                throw new Exception("Macro expansion level exceeds 256");
            }
            if (_pp.enable_expand_debug) {
                Console.WriteLine($"---substitute: {replacing.Text} with {PreprocessorToken.PrintTokens(_body)} ({parameterValues?.Count})");
                if (parameterValues != null) {
                    foreach (var parameter in parameterValues) {
                        Console.WriteLine($"param: {PreprocessorToken.PrintTokens(parameter)}");
                    }
                }
            }
            var expandedTokens = Substitute(replacing, _body, parameterValues);
            if (_pp.enable_expand_debug) {
                Console.WriteLine($"---result: {PreprocessorToken.PrintTokens(expandedTokens)}");
            }
            foreach (var etoken in expandedTokens) {
                etoken.expandLevel = replacing.expandLevel + 1;
            }
            return expandedTokens;
        }
    }

    class DMMacroLine : DMMacro {
        public DMMacroLine() : base(null, null, null) { }

        public override List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            return new() {
                new PreprocessorToken(TokenType.Numeric, replacing.Location.Line.ToString(), loc: replacing.Location)
            };
        }
    }

    class DMMacroFile : DMMacro {
        public DMMacroFile() : base(null, null, null) { }

        public override List<PreprocessorToken> Expand(PreprocessorToken replacing, List<List<PreprocessorToken>> parameters) {
            string path = replacing.Location.Source.IncludePath;
            return new() {
                new PreprocessorToken(TokenType.String, path, loc: replacing.Location)
            };
        }
    }

    class DMDefinedMacro : DMMacro {
        private Dictionary<string, DMMacro> _defines;
        public DMDefinedMacro(Dictionary<string, DMMacro> defines) : base(null, null, null) {
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
