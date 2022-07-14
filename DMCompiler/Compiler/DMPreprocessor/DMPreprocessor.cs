using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenDreamShared.Compiler;

namespace DMCompiler.Compiler.DMPreprocessor {
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

        private static readonly TokenType[] DirectiveTypes =
        {
            TokenType.DM_Preproc_Include,
            TokenType.DM_Preproc_Define,
            TokenType.DM_Preproc_Undefine,
            TokenType.DM_Preproc_If,
            TokenType.DM_Preproc_Ifdef,
            TokenType.DM_Preproc_Ifndef,
            TokenType.DM_Else,
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
                    case TokenType.DM_Preproc_Else:
                        HandleElseDirective(token);
                        break;
                    case TokenType.DM_Preproc_Warning:
                    case TokenType.DM_Preproc_Error:
                        HandleErrorOrWarningDirective(token);
                        break;
                    case TokenType.DM_Preproc_EndIf:
                        break;

                    case TokenType.DM_Preproc_Identifier:
                        _currentLineWhitespaceOnly = false;

                        if (!_defines.TryGetValue(token.Text, out DMMacro macro)) {
                            yield return token;
                            break;
                        }

                        List<List<Token>> parameters = null;
                        if (macro.HasParameters() && !TryGetMacroParameters(out parameters)) {
                            yield return token;
                            break;
                        }

                        List<Token> expandedTokens = macro.Expand(token, parameters);
                        expandedTokens.Reverse();

                        foreach (Token expandedToken in expandedTokens) {
                            expandedToken.Location = token.Location;

                            // These tokens are pushed so that nested macros get processed
                            PushToken(expandedToken);
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
            string file = Path.Combine(Path.GetDirectoryName(currentLexer.SourceName), (string)includedFileToken.Value);
            string directory = currentLexer.IncludeDirectory;

            IncludeFile(directory, file, includedFrom: includeToken.Location);
        }

        private void HandleDefineDirective(Token defineToken) {
            if (!VerifyDirectiveUsage(defineToken))
                return;

            Token defineIdentifier = GetNextToken(true);
            if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier)
                throw new Exception("Invalid define identifier");
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

        private void HandleIfDirective(Token ifToken) {
            if (!VerifyDirectiveUsage(ifToken))
                return;

            //TODO Implement #if properly
            SkipIfBody();
            DMCompiler.UnimplementedWarning(ifToken.Location, "#if is not implemented");
        }

        private void HandleIfDefDirective(Token ifDefToken) {
            if (!VerifyDirectiveUsage(ifDefToken))
                return;

            Token define = GetNextToken(true);
            if (define.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(ifDefToken.Location, "Expected a define identifier"));
                return;
            }

            if (!_defines.ContainsKey(define.Text)) {
                SkipIfBody();
            }
        }

        private void HandleIfNDefDirective(Token ifNDefToken) {
            if (!VerifyDirectiveUsage(ifNDefToken))
                return;

            Token define = GetNextToken(true);
            if (define.Type != TokenType.DM_Preproc_Identifier) {
                DMCompiler.Error(new CompilerError(ifNDefToken.Location, "Expected a define identifier"));
                return;
            }

            if (_defines.ContainsKey(define.Text)) {
                SkipIfBody();
            }
        }

        private void HandleElseDirective(Token elseToken) {
            if (!VerifyDirectiveUsage(elseToken))
                return;

            //If #else is encountered outside of SkipIfBody, it needs skipped
            SkipIfBody();
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
            if (_unprocessedTokens.Count > 0) {
                Token nextToken = _unprocessedTokens.Pop();

                if (ignoreWhitespace && nextToken.Type == TokenType.DM_Preproc_Whitespace) {
                    nextToken = GetNextToken(true);
                }

                return nextToken;
            } else {
                return ignoreWhitespace ? _lexerStack.Peek().GetNextTokenIgnoringWhitespace() : _lexerStack.Peek().GetNextToken();
            }
        }

        private void SkipIfBody() {
            int ifdefCount = 1;

            Token token;
            while ((token = GetNextToken()).Type != TokenType.EndOfFile) {
                if (token.Type == TokenType.DM_Preproc_If || token.Type == TokenType.DM_Preproc_Ifdef || token.Type == TokenType.DM_Preproc_Ifndef) {
                    ifdefCount++;
                } else if (token.Type == TokenType.DM_Preproc_EndIf) {
                    ifdefCount--;
                }

                if (ifdefCount == 0 || (token.Type == TokenType.DM_Preproc_Else && ifdefCount == 1)) {
                    break;
                }
            }
        }

        private bool TryGetMacroParameters(out List<List<Token>> parameters) {
            Token leftParenToken = GetNextToken();

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
