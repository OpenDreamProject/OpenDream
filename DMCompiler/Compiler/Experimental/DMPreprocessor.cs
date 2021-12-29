using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Original = DMCompiler.Compiler;
using DMCompiler.DM.Visitors;
using System.IO;

namespace DMCompiler.Compiler.Experimental {
    public class DMPreprocessor : IEnumerable<PreprocessorToken> {

        DMPreprocessorLexer lexer;

        class TokenState {
            public PreprocessorToken current;
        }

        TokenState _state = new();
        bool allow_directives = true;
        bool end_of_source = false;
        bool enable_expand_debug = false;

        Queue<SourceText> outer_sources = new();
        Stack<SourceText> inner_sources = new();
        SourceText current_source = null;

        class TokenSource {
            public IEnumerator<PreprocessorToken> tokens;
            public Queue<PreprocessorToken> unprocessedTokens;

            public TokenSource(IEnumerator<PreprocessorToken> toks) {
                tokens = toks;
                unprocessedTokens = new();
            }
        }
        Stack<TokenSource> token_source_stack = new();
        TokenSource current_tokens;

        // Values for this stack:
        // 1: currently including the true condition
        // 0: true condition not found yet
        // -1: the true condition was already included
        Stack<int> ifStack = new();

        // true if the else has triggered
        Stack<bool> elseStack = new();
        int skippedIfs = 0;

        public IEnumerator<PreprocessorToken> GetEnumerator() {
            PreprocessorToken t;
            do {
                t = ProducerNext();
                yield return t;
            } while (t != null);
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private Dictionary<string, DMMacro> _defines = new() {
            { "__LINE__", new DMMacroLine() },
            { "__FILE__", new DMMacroFile() }
        };

        public DMPreprocessor() {
            lexer = new DMPreprocessorLexer();
        }

        public void ConsumerNext() {
            if (current_tokens.unprocessedTokens.Count > 0) {
                _state.current = current_tokens.unprocessedTokens.Dequeue();
                end_of_source = current_tokens.unprocessedTokens.Count == 0 && current_tokens.tokens.Current == null;
                return;
            }
            current_tokens.tokens.MoveNext();
            UpdateState();
        }

        public string PrintState() {
            System.Text.StringBuilder sb = new();
            sb.Append("--------------");
            sb.Append(current_source.FullPath + "\n");
            sb.Append($"token source stack: {token_source_stack.Count}, outer_sources: {outer_sources.Count}, inner_sources: {inner_sources.Count}");
            return sb.ToString();
        }

        public void IncludeOuter(SourceText srctext) {
            if (current_source == null) {
                IncludeInner(srctext);
            }
            else {
                outer_sources.Enqueue(srctext);
            }
            //Console.WriteLine("IncludeOuter " + PrintState());
        }

        protected void UpdateState() {
            _state.current = current_tokens.tokens.Current;
            end_of_source = current_tokens.tokens.Current == null;
            if (_state.current != null && current_source != null) {
                // this should match the path printed in PrintState for debugging purposes
                //if (current_source.FullPath == "/home/vagrant/dream/storage/repos/SS13/goonstation/code/WorkInProgress/ObjectProperties.dm") 
                //    Console.WriteLine("state: " + _state.current.ToString() + " " + _state.current.WhitespaceOnly);
            }
        }

        protected void IncludeInner(SourceText srctext) {
            lexer.Include(srctext);
            if (current_source == null) {
                PushSource(lexer);
            }
            if (current_source != null) {
                inner_sources.Push(current_source);
            }
            current_source = srctext;
            //Console.WriteLine("IncludeInner " + PrintState());
        }

        public void PushSource(IEnumerator<PreprocessorToken> token_source) {
            token_source_stack.Push(new TokenSource(token_source));
            token_source.MoveNext();
            current_tokens = token_source_stack.Peek();
            UpdateState();
        }
        public void PopSource() {
            token_source_stack.Pop();
            current_tokens = token_source_stack.Peek();
            UpdateState();
        }
        public PreprocessorToken ProducerNext(bool sourceRestricted = false) {
            PreprocessorToken found_token = null;
            while (found_token == null) {
                PreprocessorToken start_token = _state.current;
                // sourceRestricted means we cant leave our current token source i.e. localized macro expansion
                if (end_of_source && sourceRestricted) {
                    return null;
                }
                if (end_of_source) {
                    PopSource();
                    continue;
                }
                if (_state.current.Type == TokenType.EndOfFile) {
                    if (inner_sources.Count == 0) {
                        if (outer_sources.Count == 0) {
                            return null;
                        }
                        current_source = null;
                        SourceText next_include = outer_sources.Dequeue();
                        IncludeInner(next_include);
                    }
                    else {
                        current_source = inner_sources.Pop();
                        ConsumerNext();
                    }
                    continue;
                }
                if (_state.current.Type == TokenType.Symbol) {
                    if (_state.current.Text == "#" && _state.current.WhitespaceOnly && allow_directives) {
                        ConsumerNext(); SkipWhitespace();
                        if (_state.current.Type != TokenType.Identifier) {
                            throw new Exception("Expected identifier for directive: " + _state.current.ToString());
                        }
                        if (_state.current.Text == "include") {
                            if (SkippingIfBody()) { lexer.ReadLine(); continue; }
                            ConsumerNext(); SkipWhitespace();
                            PreprocessorToken includeParameter = _state.current;
                            if (_state.current.Type == TokenType.String) {
                                ConsumerNext(); SkipWhitespace();
                                if (_state.current.Type != TokenType.Newline) {
                                    throw new Exception("Newline must end include");
                                }

                                var source_text = new SourceText(current_source.RootDir, includeParameter.Text);
                                switch (Path.GetExtension(source_text.FullPath)) {
                                    case ".dmm": {
                                            // TODO IncludedMaps
                                            continue;
                                        }
                                    case ".dmf": {
                                            // TODO IncludedInterface
                                            continue;
                                        }
                                    case ".dms": {
                                            continue;
                                        }
                                    default: {
                                            IncludeInner(source_text);
                                            continue;
                                        }
                                }
                            }
                            else {
                                throw new Exception("Constant string required as include argument");
                            }
                        }
                        else if (_state.current.Text == "warn") {
                            if (SkippingIfBody()) { lexer.ReadLine(); continue; }
                            string s = lexer.ReadLine();
                            ConsumerNext();
                            Console.WriteLine("warning: " + s);
                        }
                        else if (_state.current.Text == "error") {
                            if (SkippingIfBody()) { lexer.ReadLine(); continue; }
                            string s = lexer.ReadLine();
                            ConsumerNext();
                            Console.WriteLine("error: " + s);
                        }
                        else if (_state.current.Text == "define") {
                            if (SkippingIfBody()) { lexer.ReadLine(); continue; }
                            ConsumerNext(); SkipWhitespace();
                            PreprocessorToken defineIdentifier = _state.current;
                            if (defineIdentifier.Type != TokenType.Identifier) { throw new Exception("Invalid define identifier"); }
                            ConsumerNext();
                            PreprocessorToken defineToken = _state.current;
                            List<string> parameters = new();
                            if (defineToken.IsSymbol("(")) {
                                PreprocessorToken parameterToken;
                                do {
                                    ConsumerNext();
                                    SkipWhitespace();
                                    parameterToken = _state.current;
                                    ConsumerNext();
                                    SkipWhitespace();
                                    if (parameterToken.Type == TokenType.Identifier) {
                                        if (_state.current.IsSymbol("...")) {
                                            parameters.Add(parameterToken.Text + _state.current.Text);
                                            ConsumerNext();
                                            SkipWhitespace();
                                        }
                                        else {
                                            parameters.Add(parameterToken.Text);
                                        }
                                    }
                                    else if (parameterToken.IsSymbol("...")) {
                                        parameters.Add(parameterToken.Text);
                                    }
                                    else {
                                        throw new Exception("bad argument in macro definition: " + defineIdentifier.ToString());
                                    }
                                } while (_state.current.IsSymbol(","));

                                SkipWhitespace();
                                if (!_state.current.IsSymbol(")")) {
                                    throw new Exception("Missing ')' in macro definition: " + defineIdentifier.ToString());
                                }
                                ConsumerNext();
                                SkipWhitespace();
                            }
                            else if (defineToken.Type == TokenType.Whitespace) {
                                SkipWhitespace();
                            }
                            List<PreprocessorToken> defineTokens = ReadLine();
                            //Console.WriteLine(PreprocessorToken.PrintTokens(defineTokens));
                            _defines[defineIdentifier.Text] = new DMMacro(parameters, defineTokens);
                        }
                        else if (_state.current.Text == "undef") {
                            if (SkippingIfBody()) { lexer.ReadLine(); continue; }
                            ConsumerNext(); SkipWhitespace();
                            PreprocessorToken defineIdentifier = _state.current;
                            if (defineIdentifier.Type != TokenType.Identifier) { throw new Exception("Invalid define identifier"); }
                            ConsumerNext();
                            _defines.Remove(defineIdentifier.Text);
                        }
                        else if (_state.current.Text == "if") {
                            if (SkippingIfBody()) { skippedIfs += 1; lexer.ReadLine(); continue; }
                            ConsumerNext(); SkipWhitespace();
                            int result = ProcessIfDirective();
                            ifStack.Push(result);
                            elseStack.Push(false);
                        }
                        else if (_state.current.Text == "elif") {
                            if (!ElseEligible()) {
                                throw new Exception("invalid use of #elif");
                            }
                            if (!ElifEligible()) {
                                var current_mode = ifStack.Peek();
                                if (current_mode == 1) { ifStack.Pop(); ifStack.Push(-1); }
                                lexer.ReadLine();
                                continue;
                            }
                            ConsumerNext(); SkipWhitespace();
                            int result = ProcessIfDirective();
                            ifStack.Push(result); elseStack.Push(elseStack.Peek());
                        }
                        else if (_state.current.Text == "else") {
                            if (!ElseEligible()) {
                                throw new Exception("invalid use of #else");
                            }
                            var current_mode = ifStack.Peek();
                            if (current_mode == 1) { ifStack.Pop(); ifStack.Push(-1); }
                            if (current_mode == 0) { ifStack.Pop(); ifStack.Push(1); }
                            elseStack.Pop(); elseStack.Push(true);
                            ConsumerNext(); SkipWhitespace();
                        }
                        else if (_state.current.Text == "endif") {
                            ConsumerNext(); SkipWhitespace();
                            if (skippedIfs > 0) { skippedIfs--; }
                            else { ifStack.Pop(); elseStack.Pop(); }
                        }
                        else if (_state.current.Text == "ifdef" || _state.current.Text == "ifndef") {
                            var def_flag = _state.current.Text;
                            if (SkippingIfBody()) { skippedIfs += 1; lexer.ReadLine(); continue; }
                            ConsumerNext(); SkipWhitespace();
                            if (_state.current.Type != TokenType.Identifier) { throw new Exception("expected identifier in #ifdef"); }
                            int if_val = -42;
                            if (_defines.ContainsKey(_state.current.Text)) {
                                if (def_flag == "ifdef") { if_val = 1; }
                                if (def_flag == "ifndef") { if_val = 0; }
                            }
                            else {
                                if (def_flag == "ifdef") { if_val = 0; }
                                if (def_flag == "ifndef") { if_val = 1; }
                            }
                            ifStack.Push(if_val); elseStack.Push(false);
                            ConsumerNext(); SkipWhitespace();
                        }
                        else {
                            throw new Exception("unknown directive " + _state.current.Text);
                        }
                        continue;
                    }
                    found_token = _state.current;
                    ConsumerNext();
                }
                else if (_state.current.Type == TokenType.Identifier) {
                    var expand_result = IdentifierExpand(_state.current);
                    if (expand_result != null) {
                        expand_result = FullExpand(expand_result);
                        PushSource(expand_result.GetEnumerator());
                    }
                    else {
                        found_token = _state.current;
                        ConsumerNext();
                    }
                }
                else if (_state.current.Type == TokenType.Whitespace) {
                    found_token = _state.current;
                    ConsumerNext();
                }
                else if (_state.current.Type == TokenType.Newline) {
                    found_token = _state.current;
                    ConsumerNext();
                }
                else if (_state.current.Type == TokenType.String) {
                    if (_state.current.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                        info.nestedTokenInfo.Tokens = FullExpand(info.nestedTokenInfo.Tokens);
                    }
                    if (_state.current.Value is NestedTokenInfo nti && nti.Tokens != null) {
                        nti.Tokens = FullExpand(nti.Tokens);
                    }
                    found_token = _state.current;
                    ConsumerNext();
                }
                else if (_state.current.Type == TokenType.Numeric) {
                    found_token = _state.current;
                    ConsumerNext();
                }
                else {
                    throw new Exception("unknown token " + _state.current.Type + " " + _state.current.Text);
                }
                if (SkippingIfBody()) {
                    found_token = null;
                }
            }
            if (found_token == null) {
                throw new Exception("null token unexpected");
            }
            return found_token;
        }

        int ProcessIfDirective() {
            List<PreprocessorToken> ifTokens = ReadLine();
            //Console.WriteLine(PreprocessorToken.PrintTokens(ifTokens));

            _defines["defined"] = new DMDefinedMacro(_defines);
            ifTokens = FullExpand(ifTokens);
            _defines.Remove("defined");
            ifTokens.Add(new PreprocessorToken(TokenType.Newline, loc: ifTokens[ifTokens.Count - 1].Location));

            var converter = new PreprocessorTokenConvert(ifTokens.GetEnumerator());
            DM.DMParser parser = new DM.DMParser(converter, true);
            DM.DMASTExpression expr = parser.Expression();
            //new Original.DM.DMAST.DMASTNodePrinter().Print(expr, Console.Out);

            DMASTSimplifier simplify = new();
            simplify.SimplifyExpression(ref expr);
            //new Original.DM.DMAST.DMASTNodePrinter().Print(expr, Console.Out);

            var intexpr = expr as DM.DMASTConstantInteger;
            if (intexpr == null) {
                throw new Exception("Expected integer result in #if macro evaluation");
            }
            //Console.WriteLine(intexpr.Value);
            return intexpr.Value;
        }

        bool SkippingIfBody() {
            if (ifStack.Count > 0 && (ifStack.Peek() == 0 || ifStack.Peek() == -1)) {
                return true;
            }
            return false;
        }

        bool ElifEligible() {
            if (ifStack.Count > 0 && ifStack.Peek() == 1) {
                return false;
            }
            return true;
        }
        bool ElseEligible() {
            if (ifStack.Count > 0 && elseStack.Peek() == false) {
                return true;
            }
            return false;
        }
        void SkipWhitespace() {
            while (_state.current.Type == TokenType.Whitespace) {
                ConsumerNext();
            }
        }

        List<PreprocessorToken> PeekNestedDirective() {
            List<PreprocessorToken> peeked_tokens = new();
            if (_state.current.IsSymbol("#")) {
                peeked_tokens.Add(_state.current);
                ConsumerNext();
                while (_state.current.Type == TokenType.Whitespace) {
                    peeked_tokens.Add(_state.current);
                    ConsumerNext();
                }
                if (_state.current.IsIdentifier("define")) {
                    peeked_tokens.Add(_state.current);
                    ConsumerNext();
                    foreach (var token in peeked_tokens) {
                        token_source_stack.Peek().unprocessedTokens.Enqueue(token);
                    }
                    return null;
                }
            }
            return peeked_tokens;
        }

        List<PreprocessorToken> ReadLine() {
            List<PreprocessorToken> tokens = new();
            while (_state.current.Type != TokenType.Newline && _state.current.Type != TokenType.EndOfFile) {
                if (PeekNestedDirective() == null) {
                    return tokens;
                }
                tokens.Add(_state.current);
                ConsumerNext();
            }
            return tokens;
        }

        public List<PreprocessorToken> FullExpand(List<PreprocessorToken> tokens) {
            bool did_expand = true;
            while (did_expand) {
                PushSource(tokens.GetEnumerator());
                (did_expand, tokens) = Expand();
                PopSource();
                //                bool has_held = false;
                //                foreach (var held_token in tokens) {
                //                    if (!held_token.ExpandEligible) {
                //                        held_token.ExpandEligible = true;
                //                        has_held = true;
                //                    }
                //                }
                //                if (has_held) {
                //                    did_expand = true;
                //                }
            }
            //enable_expand_debug = true;
            if (enable_expand_debug) {
                Console.WriteLine($"FullExpand result1----------------------------- \n{PreprocessorToken.PrintTokens(tokens)}");
            }
            List<PreprocessorToken> directiveResult = new();
            PushSource(tokens.GetEnumerator());
            PreprocessorToken pt = null;
            do {
                pt = ProducerNext(sourceRestricted: true);
                if (pt != null) {
                    directiveResult.Add(pt);
                }
            } while (pt != null);
            PopSource();

            tokens = ConcatenateAll(directiveResult);
            if (enable_expand_debug) {
                Console.WriteLine($"FullExpand result2------------------------------ \n{PreprocessorToken.PrintTokens(tokens)}");
            }
            enable_expand_debug = false;
            // TODO hack to prevent indentation from being screwed up, but this could be resolved at the TextProducer level instead
            var start_pos = SkipWhitespace(tokens, 0);
            tokens = tokens.Skip(start_pos).ToList();
            return tokens;
        }

        public (bool, List<PreprocessorToken>) Expand() {
            List<PreprocessorToken> result = new();
            bool did_expand = false;
            var had_expand = false;
            while (!end_of_source) {
                PreprocessorToken t = _state.current;
                if (t.Type == TokenType.Identifier) {
                    List<PreprocessorToken> nestedExpand = IdentifierExpand(t);
                    if (nestedExpand != null) {
                        did_expand = true;
                        result.AddRange(nestedExpand);
                    }
                    else { result.Add(t); ConsumerNext(); }
                }
                else if (t.Type == TokenType.String) {
                    if (t.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                        info.nestedTokenInfo.Tokens = FullExpand(info.nestedTokenInfo.Tokens);
                    }
                    if (t.Value is NestedTokenInfo nti && nti.Tokens != null) {
                        nti.Tokens = FullExpand(nti.Tokens);
                    }
                    result.Add(t); ConsumerNext();
                }
                else { result.Add(t); ConsumerNext(); }
            }
            return (did_expand, result);
        }

        int SkipWhitespace(List<PreprocessorToken> tokens, int start) {
            int ctok = start;
            while (ctok < tokens.Count) {
                if (tokens[ctok].Type != TokenType.Whitespace) { return ctok; }
                ctok++;
            }
            return ctok;
        }

        public List<PreprocessorToken> Concatenate(PreprocessorToken token_l, PreprocessorToken token_r) {
            List<PreprocessorToken> result = new();
            bool simple_concat = false;
            token_l.ExpandEligible = false;
            token_r.ExpandEligible = false;
            if (token_l.Type == TokenType.Identifier) {
                if (token_r.Type == TokenType.Identifier) {
                    result.Add(new PreprocessorToken(TokenType.Identifier, token_l.Text + token_r.Text, loc: token_l.Location));
                }
                else if (token_r.Type == TokenType.Symbol) {
                    simple_concat = true;
                }
                else if (token_r.Type == TokenType.Numeric) {
                    result.Add(new PreprocessorToken(TokenType.Identifier, token_l.Text + token_r.Text, loc: token_l.Location));
                }
                else {
                    throw new Exception($"Invalid use of ##: {token_l} ## {token_r}");
                }
            }
            else if (token_l.Type == TokenType.Symbol) {
                simple_concat = true;
            }
            else if (token_l.Type == TokenType.Numeric) {
                simple_concat = true;
            }
            else {
                throw new Exception($"Invalid use of ##: {token_l} ## {token_r}");
            }
            if (simple_concat) {
                result.Add(token_l);
                result.Add(token_r);
            }
            return result;
        }
        public List<PreprocessorToken> ConcatenateAll(List<PreprocessorToken> tokens) {
            int ctok = 0;
            List<PreprocessorToken> expandedTokens = new();
            PreprocessorToken previous_token = null;
            PreprocessorToken has_whitespace = null;
            while (ctok < tokens.Count) {
                PreprocessorToken current_token = tokens[ctok];
                //Console.Write(ctok + " ");
                if (tokens[ctok].IsSymbol("##")) {
                    // TODO This may need an extra scan where unjoined ##'s are cleared 
                    if (previous_token == null) {
                        if (ctok + 1 < tokens.Count) {
                            //tokens[ctok + 1].ExpandEligible = false;
                        }
                        ctok += 1;
                        continue;
                    }
                    int token_r_pos = SkipWhitespace(tokens, ctok + 1);
                    if (!(token_r_pos < tokens.Count)) {
                        ctok += 1;
                        previous_token.ExpandEligible = false;
                        continue;
                    }
                    PreprocessorToken token_l = previous_token;
                    PreprocessorToken token_r = tokens[token_r_pos];
                    previous_token = null;
                    List<PreprocessorToken> concat_tokens = Concatenate(token_l, token_r);
                    if (concat_tokens.Count == 1) {
                        if (previous_token != null) {
                            expandedTokens.Add(previous_token);
                        }
                        if (has_whitespace != null) {
                            expandedTokens.Add(has_whitespace);
                            has_whitespace = null;
                        }
                        previous_token = concat_tokens[0];
                    }
                    else if (concat_tokens.Count == 2) {
                        if (previous_token != null) {
                            expandedTokens.Add(previous_token);
                        }
                        expandedTokens.Add(concat_tokens[0]);
                        if (has_whitespace != null) {
                            expandedTokens.Add(has_whitespace);
                            has_whitespace = null;
                        }
                        previous_token = concat_tokens[1];
                    }
                    //Console.WriteLine($"skipped {token_r_pos - ctok + 1}");
                    ctok += token_r_pos - ctok + 1;
                }
                else if (tokens[ctok].IsSymbol("#")) {
                    if (!(ctok + 1 < tokens.Count)) {
                        throw new Exception("# invalid usage");
                    }
                    if (previous_token != null) {
                        expandedTokens.Add(previous_token);
                    }
                    if (has_whitespace != null) {
                        expandedTokens.Add(has_whitespace);
                        has_whitespace = null;
                    }
                    previous_token = new PreprocessorToken(TokenType.String, tokens[ctok + 1].Text, loc: tokens[ctok].Location);
                    ctok += 2;
                }
                else if (tokens[ctok].Type == TokenType.Whitespace) {
                    has_whitespace = current_token;
                    ctok += 1;
                }
                else {
                    if (previous_token != null) {
                        expandedTokens.Add(previous_token);
                    }
                    if (has_whitespace != null) {
                        expandedTokens.Add(has_whitespace);
                        has_whitespace = null;
                    }
                    previous_token = current_token;
                    ctok += 1;
                }
            }

            if (previous_token != null) {
                expandedTokens.Add(previous_token);
            }
            if (has_whitespace != null) {
                expandedTokens.Add(has_whitespace);
            }
            return expandedTokens;
        }

        public List<PreprocessorToken> IdentifierExpand(PreprocessorToken defined_token) {
            if (!defined_token.ExpandEligible) { return null; }
            List<PreprocessorToken> result = new();
            if (_defines.TryGetValue(defined_token.Text, out DMMacro macro)) {
                List<List<PreprocessorToken>> parameters = null;
                ConsumerNext();
                if (macro.HasParameters()) {
                    parameters = GetMacroApplyParameters();
                }
                List<List<PreprocessorToken>> expanded_parameters = new();
                if (parameters != null) {
                    foreach (var parameter in parameters) {
                        if (enable_expand_debug) {
                            expanded_parameters.Add(FullExpand(parameter));
                            enable_expand_debug = true;
                        }
                        else {
                            expanded_parameters.Add(FullExpand(parameter));
                        }
                    }
                }
                result = macro.Expand(defined_token, expanded_parameters);
                //Console.WriteLine($"id result------------------------------ \n{PreprocessorToken.PrintTokens(result)}");
                return result;
            }
            else {
                return null;
            }
        }

        // TODO need to make sure this doesnt read past the current source
        List<List<PreprocessorToken>> GetMacroApplyParameters() {
            if (_state.current == null || !_state.current.IsSymbol("(")) {
                return null;
            }
            ConsumerNext();

            List<List<PreprocessorToken>> parameters = new();
            List<PreprocessorToken> currentParameter = new();
            PreprocessorToken parameterToken = _state.current;
            int parenthesisNesting = 0;
            //Console.WriteLine("params");
            while (parenthesisNesting != 0 || !parameterToken.IsSymbol(")")) {
                if (parenthesisNesting == 0 && parameterToken.IsSymbol(",")) {
                    parameters.Add(currentParameter);
                    currentParameter = new List<PreprocessorToken>();
                }
                else {
                    if (parameterToken.Type != TokenType.Newline) currentParameter.Add(parameterToken);
                    if (parameterToken.IsSymbol("(")) { parenthesisNesting++; }
                    else if (parameterToken.IsSymbol(")")) { parenthesisNesting--; }
                }
                //Console.Write(_state.current.Text);
                ConsumerNext();
                if (parameterToken.IsSymbol(")")) {
                    //Console.WriteLine("\n" + parenthesisNesting);
                }
                parameterToken = _state.current;
            }
            //Console.WriteLine();
            ConsumerNext();
            parameters.Add(currentParameter);
            return parameters;
        }
    }

}
