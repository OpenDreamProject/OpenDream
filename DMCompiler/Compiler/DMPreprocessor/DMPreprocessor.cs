using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Compiler.DM;
using DMCompiler.DM;
using OpenDreamShared.Compiler;
using Robust.Shared.Utility;

namespace DMCompiler.Compiler.DMPreprocessor {

    /// <summary>
    /// The master class for handling DM preprocessing.
    /// This is an <see cref="IEnumerable"/>, and is usually accessed via its <see cref="Token"/> output in a for-loop.
    /// </summary>
    public class DMPreprocessor : IEnumerable<Token> {
        public List<string> IncludedMaps = new();
        public string IncludedInterface;

        //Every include pushes a new lexer that gets popped once the included file is finished
        private Stack<DMPreprocessorLexer> _lexerStack =  new();

        private HashSet<string> _includedFiles = new();
        private Stack<Token> _unprocessedTokens = new();
        private bool _currentLineWhitespaceOnly = true;
        private bool _enableDirectives;
        private Dictionary<string, DMMacro> _defines = new() {
            { "__LINE__", new DMMacroLine() },
            { "__FILE__", new DMMacroFile() }
        };
        /// <summary>
        /// This stores previous evaluations of if-directives that have yet to find their #endif.<br/>
        /// We do this so that we can A.) Detect whether an #else or #endif is valid and B.) Remember what to do when we find that #else.
        /// A null value indicates the last directive found was an #else that's waiting for an #endif.
        /// </summary>
        private Stack<bool?> _lastIfEvaluations = new(); 
        private Location _lastSeenIf = Location.Unknown; // used by the errors emitted for when the above two vars are non-zero at exit
        private Token _lastToken; // The last token emitted via GetNextToken().

        private static readonly TokenType[] DirectiveTypes =
        {
            TokenType.DM_Preproc_Include,
            TokenType.DM_Preproc_Define,
            TokenType.DM_Preproc_Undefine,
            TokenType.DM_Preproc_If,
            TokenType.DM_Preproc_Ifdef,
            TokenType.DM_Preproc_Ifndef,
            TokenType.DM_Preproc_Elif,
            TokenType.DM_Preproc_Else,
            TokenType.DM_Preproc_Warning,
            TokenType.DM_Preproc_Error,
            TokenType.DM_Preproc_EndIf
        };

        public DMPreprocessor(bool enableDirectives) {
            _enableDirectives = enableDirectives;
        }

        public IEnumerator<Token> GetEnumerator() {
            while (_lexerStack.Count > 0) {
                Token token = GetNextToken();

                switch (token.Type) {
                    case TokenType.DM_Preproc_Whitespace:
                        Token afterWhitespace = GetNextToken();
                        if (_currentLineWhitespaceOnly) {
                            if (afterWhitespace.Type == TokenType.Newline)
                                break; //Ignore lines containing only whitespace

                            if (DirectiveTypes.Contains(afterWhitespace.Type)) {
                                PushToken(afterWhitespace);
                                break;
                            }
                        }

                        yield return token;
                        PushToken(afterWhitespace);
                        break;
                    case TokenType.EndOfFile:
                        _lexerStack.Pop();
                        break;
                    case TokenType.Newline:
                        if (_currentLineWhitespaceOnly)
                            break;

                        _currentLineWhitespaceOnly = true;
                        yield return token;
                        break;
                    case TokenType.DM_Preproc_LineSplice:
                        do {
                            token = GetNextToken(true);
                        } while (token.Type == TokenType.Newline);

                        PushToken(token);
                        break;

                    case TokenType.DM_Preproc_Include:
                        HandleIncludeDirective(token);
                        break;
                    case TokenType.DM_Preproc_Define:
                        HandleDefineDirective(token);
                        break;
                    case TokenType.DM_Preproc_Undefine:
                        HandleUndefineDirective(token);
                        break;
                    case TokenType.DM_Preproc_If:
                        HandleIfDirective(token);
                        break;
                    case TokenType.DM_Preproc_Ifdef:
                        HandleIfDefDirective(token);
                        break;
                    case TokenType.DM_Preproc_Ifndef:
                        HandleIfNDefDirective(token);
                        break;
                    case TokenType.DM_Preproc_Elif:
                        HandleElifDirective(token);
                        break;
                    case TokenType.DM_Preproc_Else:
                        if (!_lastIfEvaluations.TryPop(out bool? wasTruthy) || wasTruthy is null)
                            DMCompiler.Error(new CompilerError(token.Location, "Unexpected #else"));
                        if (!wasTruthy.HasValue || wasTruthy.Value)
                            SkipIfBody(true);
                        else
                            _lastIfEvaluations.Push((bool?)null);
                        break;
                    case TokenType.DM_Preproc_Warning:
                    case TokenType.DM_Preproc_Error:
                        HandleErrorOrWarningDirective(token);
                        break;
                    case TokenType.DM_Preproc_EndIf:
                        if (!_lastIfEvaluations.TryPop(out var _))
                            DMCompiler.Error(new CompilerError(token.Location, "Unexpected #endif"));
                        break;
                    case TokenType.DM_Preproc_Identifier:
                        _currentLineWhitespaceOnly = false;
                        if(!TryMacro(token)) {
                            yield return token;
                        }
                        break;
                    case TokenType.DM_Preproc_Number:
                    case TokenType.DM_Preproc_String:
                    case TokenType.DM_Preproc_ConstantString:
                    case TokenType.DM_Preproc_Punctuator:
                    case TokenType.DM_Preproc_Punctuator_Comma:
                    case TokenType.DM_Preproc_Punctuator_Period:
                    case TokenType.DM_Preproc_Punctuator_Colon:
                    case TokenType.DM_Preproc_Punctuator_Question:
                    case TokenType.DM_Preproc_Punctuator_LeftParenthesis:
                    case TokenType.DM_Preproc_Punctuator_LeftBracket:
                    case TokenType.DM_Preproc_Punctuator_RightBracket:
                    case TokenType.DM_Preproc_Punctuator_RightParenthesis:
                        _currentLineWhitespaceOnly = false;
                        yield return token;
                        break;

                    case TokenType.Error:
                        DMCompiler.Error(new CompilerError(token.Location, (string)token.Value));
                        break;

                    default:
                        DMCompiler.Error(new CompilerError(token.Location, $"Invalid token encountered while preprocessing: {token}"));
                        break;
                }
            }
            if(_lastIfEvaluations.Any())
                DMCompiler.Error(new CompilerError(_lastSeenIf, $"Missing {_lastIfEvaluations.Count} #endif directive{(_lastIfEvaluations.Count != 1 ? 's' : "")}"));
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void IncludeFiles(IEnumerable<string> files) {
            foreach (string file in files) {
                string includeDir = Path.GetDirectoryName(file);
                string fileName = Path.GetFileName(file);

                IncludeFile(includeDir, fileName);
            }
        }

        public void IncludeFile(string includeDir, string file, Location? includedFrom = null) {
            string filePath = Path.Combine(includeDir, file);
            filePath = filePath.Replace('\\', Path.DirectorySeparatorChar);

            if (_includedFiles.Contains(filePath)) {
                DMCompiler.Warning(new CompilerWarning(includedFrom ?? Location.Internal, $"File \"{filePath}\" was already included"));
                return;
            }

            if (!File.Exists(filePath)) {
                DMCompiler.Error(new CompilerError(includedFrom ?? Location.Internal, $"Could not find included file \"{filePath}\""));
                return;
            }

            DMCompiler.VerbosePrint($"Including {file}");
            _includedFiles.Add(filePath);

            switch (Path.GetExtension(filePath)) {
                case ".dmp":
                case ".dmm":
                    IncludedMaps.Add(filePath);
                    break;
                case ".dmf":
                    if (IncludedInterface != null) {
                        DMCompiler.Error(new CompilerError(includedFrom ?? Location.Internal, $"Attempted to include a second interface file ({filePath}) while one was already included ({IncludedInterface})"));
                        break;
                    }

                    IncludedInterface = filePath;
                    break;
                case ".dms":
                    // Webclient interface file. Probably never gonna be supported.
                    DMCompiler.Warning(new CompilerWarning(includedFrom ?? Location.Internal, "DMS files are not supported"));
                    break;
                default:
                    PreprocessFile(includeDir, file);
                    break;
            }
        }

        public void PreprocessFile(string includeDir, string file) {
            string filePath = Path.Combine(includeDir, file).Replace('\\', Path.DirectorySeparatorChar);
            string source = File.ReadAllText(filePath);
            source = source.Replace("\r\n", "\n");
            source += '\n';

            _lexerStack.Push(new DMPreprocessorLexer(includeDir, file, source));
        }

        private bool VerifyDirectiveUsage(Token token) {
            if (!_enableDirectives) {
                DMCompiler.Error(new CompilerError(token.Location, "Cannot use a preprocessor directive here"));
                return false;
            }

            if (!_currentLineWhitespaceOnly) {
                DMCompiler.Error(new CompilerError(token.Location, "There can only be whitespace before a preprocessor directive"));
                return false;
            }

            return true;
        }

        private void HandleIncludeDirective(Token includeToken) {
            if (!VerifyDirectiveUsage(includeToken))
                return;

            Token includedFileToken = GetNextToken(true);
            if (includedFileToken.Type != TokenType.DM_Preproc_ConstantString) {
                DMCompiler.Error(new CompilerError(includeToken.Location, $"\"{includedFileToken.Text}\" is not a valid include path"));
                return;
            }

            DMPreprocessorLexer currentLexer = _lexerStack.Peek();
            string file = Path.Combine(Path.GetDirectoryName(currentLexer.SourceName.Replace('\\', Path.DirectorySeparatorChar)), (string)includedFileToken.Value);
            string directory = currentLexer.IncludeDirectory;

            IncludeFile(directory, file, includedFrom: includeToken.Location);
        }

        private void HandleDefineDirective(Token defineToken) {
            if (!VerifyDirectiveUsage(defineToken))
                return;

            Token defineIdentifier = GetNextToken(true);
            if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(defineIdentifier.Location, "Unexpected token, identifier expected for #define directive"));
                GetLineOfTokens(); // consume what's on this line and leave
                return;
            }
            if(defineIdentifier.Text == "defined") {
                DMCompiler.Error(new CompilerError(defineIdentifier.Location, "Reserved keywrod 'defined' used as macro name"));
            }

            List<string> parameters = null;
            List<Token> macroTokens = new(1);

            Token macroToken = GetNextToken();
            if (macroToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) {
                parameters = new List<string>(1);

                Token parameterToken;
                do {
                    parameterToken = GetNextToken(true);
                    bool unnamed = parameterToken.Type == TokenType.DM_Preproc_Punctuator_Period;
                    if (!unnamed && parameterToken.Type != TokenType.DM_Preproc_Identifier) {
                        DMCompiler.Error(new CompilerError(parameterToken.Location, "Expected a macro parameter"));
                        return;
                    }

                    string parameterName = unnamed ? "" : parameterToken.Text;

                    parameterToken = GetNextToken(true);
                    if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Period) {
                        if (!unnamed) parameterToken = GetNextToken();
                        if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_Period) throw new Exception("Expected a second period");
                        parameterToken = GetNextToken();
                        if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_Period) throw new Exception("Expected a third period");

                        parameters.Add(parameterName + "...");

                        parameterToken = GetNextToken(true);
                        if (unnamed) break;
                    } else {
                        if (unnamed) throw new Exception("Expected a second period");
                        parameters.Add(parameterName);
                    }
                } while (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Comma);

                if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_RightParenthesis) throw new Exception("Missing ')' in macro definition");
                macroToken = GetNextToken(true);
            } else if (macroToken.Type == TokenType.DM_Preproc_Whitespace) {
                macroToken = GetNextToken();
            }

            while (macroToken.Type != TokenType.Newline && macroToken.Type != TokenType.EndOfFile) {
                //Note that line splices behave differently inside macros than outside
                //Outside, a line splice will remove all empty lines that come after it
                //Inside, only one line is spliced
                if (macroToken.Type == TokenType.DM_Preproc_LineSplice) {
                    macroToken = GetNextToken(true);
                } else {
                    macroTokens.Add(macroToken);
                    macroToken = GetNextToken();
                }
            }

            if (macroTokens.Count > 0 && macroTokens[^1].Type == TokenType.DM_Preproc_Whitespace) {
                //Remove trailing whitespace
                macroTokens.RemoveAt(macroTokens.Count - 1);
            }

            _defines[defineIdentifier.Text] = new DMMacro(parameters, macroTokens);
            PushToken(macroToken);
        }

        private void HandleUndefineDirective(Token undefToken) {
            if (!VerifyDirectiveUsage(undefToken))
                return;

            Token defineIdentifier = GetNextToken(true);
            if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(defineIdentifier.Location, "Invalid macro identifier"));
                return;
            } else if (!_defines.ContainsKey(defineIdentifier.Text)) {
                DMCompiler.Error(new CompilerError(defineIdentifier.Location, $"No macro named \"{defineIdentifier.PrintableText}\""));
                return;
            }

            _defines.Remove(defineIdentifier.Text);
        }

        /// <summary>
        /// Reads in <see cref="Token"/>s to a List until it reads a grammatical line of them,
        /// handling newlines, macros, the preproc's wonky line-splicing.
        /// These are DM tokens, not DM_Preproc ones!!
        /// </summary>
        private List<Token> GetLineOfTokens()
        {
            List<Token> tokens = new List<Token>();
            bool tryIdentifiersAsMacros = true;
            for(Token token = GetNextToken(true); token.Type != TokenType.Newline; token = GetNextToken(true)) {
                switch(token.Type) {
                    case TokenType.DM_Preproc_LineSplice:
                        continue;
                    case TokenType.DM_Preproc_Identifier:
                        if(token.Text == "defined") // need to be careful here to prevent macros in defined() expressions from being clobbered
                            tryIdentifiersAsMacros = false;
                        else if (tryIdentifiersAsMacros && TryMacro(token)) // feeding any novel macro tokens back into the pipeline here
                            continue;
                        goto default; // Fallthrough!
                    case TokenType.DM_Preproc_Punctuator_RightParenthesis:
                        tryIdentifiersAsMacros = true; // may be ending a defined() sequence
                        goto default; // Fallthrough!
                    default:
                        tokens.Add(token);
                        break;
                }
            }
            tokens.Add(new Token(TokenType.Newline, "\n", Location.Unknown, null));
            DMLexer lexer = new(_lexerStack.Peek().SourceName,tokens);
            List<Token> newTokens = new List<Token>();
            for(Token token = lexer.GetNextToken(); !lexer.AtEndOfSource; token = lexer.GetNextToken()) {
                newTokens.Add(token);
            }
            return newTokens;
        }

        /// <summary>If this <see cref="TokenType.DM_Preproc_Identifier"/> Token is a macro, pushes all of its tokens onto the queue.</summary>
        /// <returns>true if the Token ended up meaning a macro sequence.</returns>
        private bool TryMacro(Token token) {
            DebugTools.Assert(token.Type == TokenType.DM_Preproc_Identifier); // Check this before passing anything to this function.
            if (!_defines.TryGetValue(token.Text, out DMMacro macro)) {
                return false;
            }

            List<List<Token>> parameters = null;
            if (macro.HasParameters() && !TryGetMacroParameters(out parameters)) {
                return false;
            }

            List<Token> expandedTokens = macro.Expand(token, parameters);
            expandedTokens.Reverse();

            foreach (Token expandedToken in expandedTokens) {
                expandedToken.Location = token.Location;

                // These tokens are pushed so that nested macros get processed
                PushToken(expandedToken);
            }
            return true;
        }

        /// <summary>
        /// Tiny helper that handles what to do when an #if or #ifdef or whatever is not well-formed. <br/>
        /// Doing this stuff helps make the compiler errors a bit clearer.
        /// </summary>
        private void HandleDegenerateIf() {
            _lastIfEvaluations.Push(false); // Doing this to help reduce weird #else errors
            SkipIfBody();
        }

        /// <remarks> NOTE: This is called by <see cref="HandleElifDirective(Token)"/> once it finishes doing its else-y behaviour. </remarks>
        private void HandleIfDirective(Token ifToken) {
            _lastSeenIf = ifToken.Location;
            if (!VerifyDirectiveUsage(ifToken))
                return;
                
            var tokens = GetLineOfTokens();
            if (!tokens.Any()) { // If empty
                DMCompiler.Error(new CompilerError(ifToken.Location, "Expression expected for #if"));
                HandleDegenerateIf();
                return;
            }
            float? expr = DMPreprocessorParser.ExpressionFromTokens(tokens, _defines);
            if(expr is null) {
                DMCompiler.Error(new CompilerError(ifToken.Location, "Expression is invalid"));
                HandleDegenerateIf();
                return;
            }
            bool result = expr != 0.0f;
            _lastIfEvaluations.Push(result);
            if (!result)
                SkipIfBody();
        }

        private void HandleIfDefDirective(Token ifDefToken) {
            _lastSeenIf = ifDefToken.Location;
            if (!VerifyDirectiveUsage(ifDefToken))
                return;

            Token define = GetNextToken(true);
            if (define.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(ifDefToken.Location, "Expected a define identifier"));
                HandleDegenerateIf();
                return;
            }
            bool result = _defines.ContainsKey(define.Text);
            if (!result) {
                SkipIfBody();
            }
            _lastIfEvaluations.Push(result);
        }

        private void HandleIfNDefDirective(Token ifNDefToken) {
            _lastSeenIf = ifNDefToken.Location;
            if (!VerifyDirectiveUsage(ifNDefToken))
                return;

            Token define = GetNextToken(true);
            if (define.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(ifNDefToken.Location, "Expected a define identifier"));
                HandleDegenerateIf();
                return;
            }

            bool result = _defines.ContainsKey(define.Text);
            if (result) {
                SkipIfBody();
            }
            _lastIfEvaluations.Push(!result);
        }

        private void HandleElifDirective(Token elifToken) {
            if (!_lastIfEvaluations.TryPeek(out bool? wasTruthy))
                DMCompiler.Error(new CompilerError(elifToken.Location, "Unexpected #elif"));
            if (wasTruthy is null) {
                DMCompiler.Error(new CompilerError(elifToken.Location, "Directive #elif cannot appear after #else in its flow control"));
                SkipIfBody();
            } else if (wasTruthy.Value)
                SkipIfBody();
            else {
                _lastIfEvaluations.Pop(); // There's a new evaluation in town
                HandleIfDirective(elifToken);
            }
        }

        private void HandleErrorOrWarningDirective(Token token) {
            if (!VerifyDirectiveUsage(token))
                return;

            StringBuilder messageBuilder = new StringBuilder();

            Token messageToken = GetNextToken(true);
            while (messageToken.Type != TokenType.EndOfFile) {
                if (messageToken.Type == TokenType.Newline) break;

                messageBuilder.Append(messageToken.Text);
                messageToken = GetNextToken();
            }

            string message = messageBuilder.ToString();
            if (token.Type == TokenType.DM_Preproc_Error) {
                DMCompiler.Error(new CompilerError(token.Location, message));
            } else {
                DMCompiler.Warning(new CompilerWarning(token.Location, message));
            }
        }

        private void PushToken(Token token) {
            _unprocessedTokens.Push(token);
        }

        private Token GetNextToken(bool ignoreWhitespace = false) {
            if (_unprocessedTokens.TryPop(out Token nextToken)) {
                if (ignoreWhitespace && nextToken.Type == TokenType.DM_Preproc_Whitespace) { // This doesn't need to be a loop since whitespace tokens should never occur next to each other
                    nextToken = GetNextToken(true);
                }
                _lastToken = nextToken;
                return nextToken;
            } else {
                _lastToken = ignoreWhitespace ? _lexerStack.Peek().GetNextTokenIgnoringWhitespace() : _lexerStack.Peek().GetNextToken();
                return _lastToken;
            }
        }

        /// <summary>Also used by #else sometimes to skip its body.</summary>
        /// <remarks>WARNING: DOES NOT CONSUME any #elif, #else, or #endif it finds.</remarks>
        /// <param name="noElseAllowed">This is used so that #else can operate this function while also forbidding it having its own #elses.</param>
        /// <returns>true if it stopped on an #else, false if it stopped in an #endif.</returns>
        private bool SkipIfBody(bool calledByElseDirective = false) {
            int ifStack = 1; // how many "ifs" deep we seem to be. We end when we reach 0.
            for (Token token = GetNextToken(true); token.Type != TokenType.EndOfFile; token = GetNextToken(true)) {
                switch (token.Type) {
                    case TokenType.DM_Preproc_If:
                    case TokenType.DM_Preproc_Ifdef:
                    case TokenType.DM_Preproc_Ifndef:
                        ifStack++;
                        break;
                    case TokenType.DM_Preproc_EndIf:
                        ifStack--;
                        break;
                    case TokenType.DM_Preproc_Else:
                    case TokenType.DM_Preproc_Elif:
                        if (ifStack != 1)
                            break;
                        if (calledByElseDirective)
                            DMCompiler.Error(new CompilerError(token.Location, $"Unexpected {token.PrintableText} directive"));
                        _unprocessedTokens.Push(token); // Push it back onto the stack so we can interpret this later
                        return true;
                    default:
                        continue; // Don't need to do the ifStack check since it has not changed as a result of this token
                }
                if (ifStack == 0) {
                    if(!calledByElseDirective)
                        _unprocessedTokens.Push(token); // Push it back onto the stack so we can interpret the entry in _lastIfEvaluations correctly.
                    return false;
                }
            }
            DMCompiler.Error(new CompilerError(Location.Unknown, "Missing #endif directive"));
            return false;
        }

        private bool TryGetMacroParameters(out List<List<Token>> parameters) {
            Token leftParenToken = GetNextToken(true);

            if (leftParenToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) {
                parameters = new();
                List<Token> currentParameter = new();

                Token parameterToken = GetNextToken(true);
                while (parameterToken.Type == TokenType.Newline) { // Skip newlines after the left parenthesis
                    parameterToken = GetNextToken(true);
                }

                int parenthesisNesting = 0;
                while (!(parenthesisNesting == 0 && parameterToken.Type == TokenType.DM_Preproc_Punctuator_RightParenthesis) &&
                        parameterToken.Type != TokenType.EndOfFile) {
                    if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Comma && parenthesisNesting == 0) {
                        parameters.Add(currentParameter);
                        currentParameter = new List<Token>();

                        parameterToken = GetNextToken(true);
                    } else {
                        currentParameter.Add(parameterToken);

                        if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) parenthesisNesting++;
                        else if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_RightParenthesis) parenthesisNesting--;

                        parameterToken = GetNextToken();
                    }
                }

                parameters.Add(currentParameter);
                if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_RightParenthesis) {
                    DMCompiler.Error(new CompilerError(leftParenToken.Location, "Missing ')' in macro call"));

                    return false;
                }

                return true;
            }

            PushToken(leftParenToken);
            parameters = null;
            return false;
        }
    }
}
