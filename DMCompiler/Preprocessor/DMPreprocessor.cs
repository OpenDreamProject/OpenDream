using OpenDreamShared.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DMCompiler.Preprocessor {
    class DMPreprocessor {
        //Every include pushes a new lexer that gets popped once the included file is finished
        private Stack<DMPreprocessorLexer> _lexerStack =  new();

        Stack<Token> _unprocessedTokens = new();
        private StringBuilder _result = new StringBuilder();
        private StringBuilder _currentLine = new StringBuilder();
        private bool _isCurrentLineWhitespaceOnly = true;
        private Dictionary<string, DMMacro> _defines = new();

        public void IncludeFile(string includePath, string filePath) {
            string source = File.ReadAllText(Path.Combine(includePath, filePath));
            source = source.Replace("\r\n", "\n");
            source = Regex.Replace(source, @"\\\n", String.Empty); //Combine all lines ending with a backslash
            source += '\n';

            _lexerStack.Push(new DMPreprocessorLexer(source));

            Token token = GetNextToken();
            while (token.Type != TokenType.EndOfFile) {
                switch (token.Type) {
                    case TokenType.DM_Preproc_Include: {
                        Token includedFile = GetNextToken(true);
                        if (includedFile.Type != TokenType.DM_Preproc_ConstantString) throw new Exception("\"" + includedFile.Text + "\" is not a valid include path");
                        string includedFilePath = (string)includedFile.Value;

                        if (includedFilePath.EndsWith(".dm")) IncludeFile(includePath, (string)includedFile.Value);
                        break;
                    }
                    case TokenType.DM_Preproc_Define: {
                        Token defineIdentifier = GetNextToken(true);
                        if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier) throw new Exception("Invalid define identifier");
                        List<string> parameters = null;
                        List<Token> defineTokens = new();

                        Token defineToken = GetNextToken();
                        if (defineToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) {
                            parameters = new List<string>();

                            Token parameterToken;
                            do {
                                parameterToken = GetNextToken(true);
                                if (parameterToken.Type != TokenType.DM_Preproc_Identifier) throw new Exception("Expected a macro parameter");

                                string parameterName = parameterToken.Text;
                                
                                parameterToken = GetNextToken(true);
                                if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Period) {
                                    parameterToken = GetNextToken();
                                    if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_Period) throw new Exception("Expected a second period");
                                    parameterToken = GetNextToken();
                                    if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_Period) throw new Exception("Expected a third period");

                                    parameters.Add(parameterName + "...");

                                    parameterToken = GetNextToken(true);
                                } else {
                                    parameters.Add(parameterName);
                                }
                            } while (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Comma);

                            if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_RightParenthesis) throw new Exception("Missing ')' in macro definition");
                            defineToken = GetNextToken(true);
                        } else if (defineToken.Type == TokenType.DM_Preproc_Whitespace) {
                            defineToken = GetNextToken(true);
                        }

                        while (defineToken.Type != TokenType.Newline && defineToken.Type != TokenType.EndOfFile) {
                            defineTokens.Add(defineToken);

                            defineToken = GetNextToken();
                        }

                        _defines.Add(defineIdentifier.Text, new DMMacro(parameters, defineTokens));
                        break;
                    }
                    case TokenType.DM_Preproc_Undefine: {
                        Token defineIdentifier = GetNextToken(true);
                        if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier) throw new Exception("Invalid define identifier");

                        _defines.Remove(defineIdentifier.Text);
                        break;
                    }
                    case TokenType.DM_Preproc_Identifier: {
                        if (_defines.TryGetValue(token.Text, out DMMacro macro)) {
                            List<Token> expandedTokens = ProcessMacro(macro);

                            //Put the tokens at the beginning of the macro on the top of the stack
                            //Can't use a queue because macros within a macro have to be processed before the rest of the macro
                            expandedTokens.Reverse();

                            foreach (Token expandedToken in expandedTokens) {
                                _unprocessedTokens.Push(expandedToken);
                            }
                        } else {
                            _isCurrentLineWhitespaceOnly = false;

                            _currentLine.Append(token.Text);
                        }

                        break;
                    }
                    case TokenType.Newline: {
                        if (!_isCurrentLineWhitespaceOnly) {
                            _result.Append(_currentLine);
                            _result.Append('\n');

                            _isCurrentLineWhitespaceOnly = true;
                        }

                        _currentLine = new StringBuilder();
                        break;
                    }
                    case TokenType.DM_Preproc_Number:
                    case TokenType.DM_Preproc_String:
                    case TokenType.DM_Preproc_ConstantString:
                    case TokenType.DM_Preproc_Punctuator:
                    case TokenType.DM_Preproc_Punctuator_Comma:
                    case TokenType.DM_Preproc_Punctuator_Period:
                    case TokenType.DM_Preproc_Punctuator_LeftParenthesis:
                    case TokenType.DM_Preproc_Punctuator_LeftBracket:
                    case TokenType.DM_Preproc_Punctuator_RightBracket:
                    case TokenType.DM_Preproc_Punctuator_RightParenthesis: _isCurrentLineWhitespaceOnly = false; _currentLine.Append(token.Text); break;
                    case TokenType.DM_Preproc_Whitespace: _currentLine.Append(token.Text); break;
                    default: throw new Exception("Invalid token '" + token.Text + "'");
                }

                token = GetNextToken();
            }

            _lexerStack.Pop();
        }

        public string GetResult() {
            return _result.ToString();
        }

        private Token GetNextToken(bool ignoreWhitespace = false) {
            if (_unprocessedTokens.Count > 0) {
                Token nextToken = _unprocessedTokens.Pop();

                if (ignoreWhitespace) {
                    while (nextToken.Type == TokenType.DM_Preproc_Whitespace) nextToken = _unprocessedTokens.Pop();
                }

                return nextToken;
            } else {
                return ignoreWhitespace ? _lexerStack.Peek().GetNextTokenIgnoringWhitespace() : _lexerStack.Peek().GetNextToken();
            }
        }

        private List<Token> ProcessMacro(DMMacro macro) {
            List<List<Token>> parameters = null;

            if (macro.HasParameters()) {
                parameters = GetMacroParameters();
            }

            return macro.Expand(parameters);
        }

        private List<List<Token>> GetMacroParameters() {
            Token leftParenToken = GetNextToken();

            if (leftParenToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) {
                List<List<Token>> parameters = new();
                List<Token> currentParameter = new();

                Token parameterToken = GetNextToken(true);
                int parenthesisNesting = 0;
                while (!(parenthesisNesting == 0 && parameterToken.Type == TokenType.DM_Preproc_Punctuator_RightParenthesis) &&
                        parameterToken.Type != TokenType.EndOfFile) {
                    if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_Comma && parenthesisNesting == 0) {
                        parameters.Add(currentParameter);
                        currentParameter = new List<Token>();

                        parameterToken = GetNextToken(true);
                    } else {
                        if (parameterToken.Type != TokenType.Newline) currentParameter.Add(parameterToken);

                        if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) parenthesisNesting++;
                        else if (parameterToken.Type == TokenType.DM_Preproc_Punctuator_RightParenthesis) parenthesisNesting--;

                        parameterToken = GetNextToken();
                    }
                }

                parameters.Add(currentParameter);
                if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_RightParenthesis) throw new Exception("Missing ')' in macro call");

                return parameters;
            }

            return null;
        }
    }
}
