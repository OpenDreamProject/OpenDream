using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DMCompiler.Compiler.DM;

namespace DMCompiler.Compiler.DMPreprocessor;

/// <summary>
/// The master class for handling DM preprocessing.
/// This is an <see cref="IEnumerable"/>, and is usually accessed via its <see cref="Token"/> output in a for-loop.
/// </summary>
public sealed class DMPreprocessor : IEnumerable<Token> {
    public readonly List<string> IncludedMaps = new(8);
    public string? IncludedInterface;

    //Every include pushes a new lexer that gets popped once the included file is finished
    private readonly Stack<DMPreprocessorLexer> _lexerStack =  new(8); // Capacity Note: TG peaks at 4 at time of writing

    private readonly Stack<Token> _bufferedWhitespace = new();
    private bool _currentLineContainsNonWhitespace = false;
    private bool _canUseDirective = true;
    private readonly HashSet<string> _includedFiles = new(5120); // Capacity Note: TG peaks at 4860 at time of writing
    private readonly Stack<Token> _unprocessedTokens = new(8192); // Capacity Note: TG peaks at 6802 at time of writing
    private readonly bool _enableDirectives;
    private readonly Dictionary<string, DMMacro> _defines = new(12288) { // Capacity Note: TG peaks at 9827 at time of writing. Current value is arbitrarily 4096 * 3.
        { "__LINE__", new DMMacroLine() },
        { "__FILE__", new DMMacroFile() },
        { "DM_VERSION", new DMMacroVersion() },
        { "DM_BUILD", new DMMacroBuild() }
    };
    /// <summary>
    /// This stores previous evaluations of if-directives that have yet to find their #endif.<br/>
    /// We do this so that we can A.) Detect whether an #else or #endif is valid and B.) Remember what to do when we find that #else.
    /// A null value indicates the last directive found was an #else that's waiting for an #endif.
    /// </summary>
    private readonly Stack<bool?> _lastIfEvaluations = new(16);
    private Location _lastSeenIf = Location.Unknown; // used by the errors emitted for when the above var isn't empty at exit

    private static readonly TokenType[] DirectiveTypes = {
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
        TokenType.DM_Preproc_EndIf,
        TokenType.DM_Preproc_Pragma
    };

    public DMPreprocessor(bool enableDirectives) {
        _enableDirectives = enableDirectives;
    }

    public IEnumerator<Token> GetEnumerator() {
        while (_lexerStack.Count > 0) {
            Token token = GetNextToken();

            switch (token.Type) {
                case TokenType.DM_Preproc_Whitespace:
                    if (_currentLineContainsNonWhitespace) {
                        yield return token;
                        break;
                    }

                    _bufferedWhitespace.Push(token);
                    break;
                case TokenType.EndOfFile:
                    _lexerStack.Pop();
                    break;
                case TokenType.Newline:
                    _canUseDirective = true;

                    if (!_currentLineContainsNonWhitespace) {
                        _bufferedWhitespace.Clear();
                        break;
                    }

                    // All buffered whitespace should have been written out by this point
                    if (_bufferedWhitespace.Count > 0) {
                        throw new InvalidOperationException();
                    }

                    _currentLineContainsNonWhitespace = false;
                    yield return token;
                    break;
                case TokenType.DM_Preproc_LineSplice:
                    do {
                        token = GetNextToken(true);
                    } while (token.Type == TokenType.Newline);

                    // Preprocessor directives can be used once we reach a line-splice
                    _canUseDirective = true;
                    PushToken(token);
                    break;

                case TokenType.DM_Preproc_Include:
                    if (!_currentLineContainsNonWhitespace) {
                        _bufferedWhitespace.Clear();
                    }
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
                        DMCompiler.Emit(WarningCode.BadDirective, token.Location, "Unexpected #else");
                    if (wasTruthy.Value)
                        SkipIfBody(true);
                    else
                        _lastIfEvaluations.Push((bool?)null);
                    break;
                case TokenType.DM_Preproc_Warning:
                case TokenType.DM_Preproc_Error:
                    HandleErrorOrWarningDirective(token);
                    break;
                case TokenType.DM_Preproc_Pragma:
                    HandlePragmaDirective(token);
                    break;
                case TokenType.DM_Preproc_EndIf:
                    if (!_lastIfEvaluations.TryPop(out var _))
                        DMCompiler.Emit(WarningCode.BadDirective, token.Location, "Unexpected #endif");
                    break;
                case TokenType.DM_Preproc_Identifier: {
                    if (TryMacro(token)) {
                        break;
                    }

                    // Otherwise treat it like any other normal token
                    goto case TokenType.DM_Preproc_Number;
                }
                case TokenType.DM_Preproc_Punctuator:
                case TokenType.DM_Preproc_Number:
                case TokenType.DM_Preproc_StringBegin:
                case TokenType.DM_Preproc_StringMiddle:
                case TokenType.DM_Preproc_StringEnd:
                case TokenType.DM_Preproc_ConstantString:
                case TokenType.DM_Preproc_Punctuator_Comma:
                case TokenType.DM_Preproc_Punctuator_Period:
                case TokenType.DM_Preproc_Punctuator_Colon:
                case TokenType.DM_Preproc_Punctuator_Question:
                case TokenType.DM_Preproc_Punctuator_LeftParenthesis:
                case TokenType.DM_Preproc_Punctuator_LeftBracket:
                case TokenType.DM_Preproc_Punctuator_RightBracket:
                case TokenType.DM_Preproc_Punctuator_Semicolon:
                case TokenType.DM_Preproc_Punctuator_RightParenthesis: {
                    while (_bufferedWhitespace.TryPop(out var whitespace)) {
                        yield return whitespace;
                    }

                    _currentLineContainsNonWhitespace = true;
                    _canUseDirective = (token.Type == TokenType.DM_Preproc_Punctuator_Semicolon);

                    yield return token;
                    break;
                }

                case TokenType.Error:
                    DMCompiler.Emit(WarningCode.ErrorDirective, token.Location, (string)token.Value);
                    break;

                default:
                    DMCompiler.Emit(WarningCode.BadToken, token.Location,
                        $"Invalid token encountered while preprocessing: {token.PrintableText} ({token.Type})");
                    break;
            }
        }
        if(_lastIfEvaluations.Any())
            DMCompiler.Emit(WarningCode.BadDirective, _lastSeenIf, $"Missing {_lastIfEvaluations.Count} #endif directive{(_lastIfEvaluations.Count != 1 ? 's' : "")}");
        DMCompiler.CheckAllPragmasWereSet();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void DefineMacro(string key, string value) {
        var lexer = new DMPreprocessorLexer(null, "<command line>", value);
        var list = new List<Token>();

        while (lexer.NextToken() is { Type: not TokenType.EndOfFile } token) {
            list.Add(token);
        }

        _defines.Add(key, new DMMacro(null, list));
    }

    // NB: Pushes files to a stack, so call in reverse order if you are
    // including multiple files.
    public void IncludeFile(string includeDir, string file, Location? includedFrom = null) {
        string filePath = Path.Combine(includeDir, file);
        filePath = filePath.Replace('\\', Path.DirectorySeparatorChar);

        if (_includedFiles.Contains(filePath)) {
            DMCompiler.Emit(WarningCode.FileAlreadyIncluded, includedFrom ?? Location.Internal, $"File \"{filePath}\" was already included");
            return;
        }

        if (!File.Exists(filePath)) {
            DMCompiler.Emit(WarningCode.MissingIncludedFile, includedFrom ?? Location.Internal, $"Could not find included file \"{filePath}\"");
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
                    if(IncludedInterface == filePath) {
                        DMCompiler.Emit(WarningCode.FileAlreadyIncluded, includedFrom ?? Location.Internal, $"Interface \"{filePath}\" was already included");
                        break;
                    }

                    DMCompiler.Emit(WarningCode.InvalidInclusion, includedFrom ?? Location.Internal, $"Attempted to include a second interface file ({filePath}) while one was already included ({IncludedInterface})");
                    break;
                }

                IncludedInterface = filePath;
                break;
            case ".dms":
                // Webclient interface file. Probably never gonna be supported.
                DMCompiler.UnimplementedWarning(includedFrom ?? Location.Internal, "DMS files are not supported");
                break;
            default:
                PreprocessFile(includeDir, file);
                break;
        }
    }

    public void PreprocessFile(string includeDir, string file) {
        file = file.Replace('\\', '/');

        _lexerStack.Push(new DMPreprocessorLexer(includeDir, file));
    }

    private bool VerifyDirectiveUsage(Token token) {
        if (!_enableDirectives) {
            DMCompiler.Emit(WarningCode.MisplacedDirective, token.Location, "Cannot use a preprocessor directive here");
            return false;
        }

        if (!_canUseDirective) {
            DMCompiler.Emit(WarningCode.MisplacedDirective, token.Location, "There can only be whitespace before a preprocessor directive");
            return false;
        }

        return true;
    }

    private void HandleIncludeDirective(Token includeToken) {
        if (!VerifyDirectiveUsage(includeToken))
            return;

        Token includedFileToken = GetNextToken(true);
        if (includedFileToken.Type != TokenType.DM_Preproc_ConstantString) {
            DMCompiler.Emit(WarningCode.InvalidInclusion, includeToken.Location, $"\"{includedFileToken.Text}\" is not a valid include path");
            return;
        }

        DMPreprocessorLexer currentLexer = _lexerStack.Peek();
        string file = Path.Combine(Path.GetDirectoryName(currentLexer.File.Replace('\\', Path.DirectorySeparatorChar)), (string)includedFileToken.Value);
        string directory = currentLexer.IncludeDirectory;

        IncludeFile(directory, file, includedFrom: includeToken.Location);
    }

    private void HandleDefineDirective(Token defineToken) {
        if (!VerifyDirectiveUsage(defineToken))
            return;

        Token defineIdentifier = GetNextToken(true);
        if (defineIdentifier.Type != TokenType.DM_Preproc_Identifier) {
            DMCompiler.Emit(WarningCode.BadDirective, defineIdentifier.Location, "Unexpected token, identifier expected for #define directive");
            GetLineOfTokens(); // consume what's on this line and leave
            return;
        }

        // #define FILE_DIR is a little special
        // Every define will add to a list of directories to check for resource files
        if (defineIdentifier.Text == "FILE_DIR") {
            Token dirToken = GetNextToken(true);
            string? dirTokenValue = dirToken.Type switch {
                TokenType.DM_Preproc_ConstantString => (string?)dirToken.Value,
                TokenType.DM_Preproc_Punctuator_Period => ".",
                _ => null
            };

            if (dirTokenValue is null) {
                DMCompiler.Emit(WarningCode.BadDirective, dirToken.Location, $"\"{dirToken.Text}\" is not a valid directory");
                return;
            }

            DMPreprocessorLexer currentLexer = _lexerStack.Peek();
            string dir = Path.Combine(currentLexer.IncludeDirectory, dirTokenValue);
            DMCompiler.AddResourceDirectory(dir);

            // In BYOND it goes on to set the FILE_DIR macro's value to the added directory
            // I don't see any reason to do that
            return;
        } else if (defineIdentifier.Text == "defined") {
            DMCompiler.Emit(WarningCode.SoftReservedKeyword, defineIdentifier.Location, "Reserved keyword 'defined' cannot be used as macro name");
        }

        List<string> parameters = null;
        List<Token> macroTokens = new(1);

        Token macroToken = GetNextToken();
        if (macroToken.Type == TokenType.DM_Preproc_Punctuator_LeftParenthesis) { // We're a macro function!
            parameters = new List<string>(1);
            //Read in the parameters
            bool canConsumeComma = false;
            bool foundVariadic = false;
            while(true) {
                var parameterToken = GetNextToken(true);
                switch(parameterToken.Type) {
                    case TokenType.DM_Preproc_Identifier:
                        canConsumeComma = true;
                        if (foundVariadic) {
                            DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, $"Variadic argument '{parameters.Last()}' must be the last argument");
                            foundVariadic = false; // Reduces error spam if there's several arguments after it
                            continue;
                        }
                        if(Check(TokenType.DM_Preproc_Punctuator_Period)) { // Check for a variadic
                            if (!Check(TokenType.DM_Preproc_Punctuator_Period) || !Check(TokenType.DM_Preproc_Punctuator_Period)) {
                                DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, $"Invalid macro parameter, '{parameterToken.Text}...' expected");
                            }
                            parameters.Add($"{parameterToken.Text}...");
                            foundVariadic = true;
                            // Consciously not setting canConsumeComma to false here. Users can have a little dangling comma, as a treat :o)
                            continue;
                        }
                        parameters.Add(parameterToken.Text);

                        continue;
                    case TokenType.DM_Preproc_Punctuator_Period: // One of those "..." things, maybe?
                        if (!Check(TokenType.DM_Preproc_Punctuator_Period) || !Check(TokenType.DM_Preproc_Punctuator_Period)) {
                            DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, "Invalid macro parameter, '...' expected");
                        }
                        canConsumeComma = true;
                        if (foundVariadic) { // Placed here so we properly consume this bogus '...' parameter if need be
                            DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, $"Variadic argument '{parameters.Last()}' must be the last argument");
                            foundVariadic = false; // Reduces error spam if there's several arguments after it
                            continue;
                        }
                        parameters.Add($"...");
                        continue;
                    case TokenType.DM_Preproc_Punctuator_Comma:
                        if(!canConsumeComma)
                            DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, "Unexpected ',' in macro parameter list");
                        canConsumeComma = false;
                        continue;
                    case TokenType.DM_Preproc_Punctuator_RightParenthesis:
                        break;
                    case TokenType.EndOfFile:
                        DMCompiler.Emit(WarningCode.BadDirective, macroToken.Location, "Missing ')' in macro definition"); // Location points to the left paren!
                        PushToken(parameterToken);
                        break;
                    default:
                        DMCompiler.Emit(WarningCode.BadDirective, parameterToken.Location, "Expected a macro parameter");
                        return;
                }
                break; // If the switch gets here, the loop ends.
            }
            macroToken = GetNextToken(true);
        } else if (macroToken.Type == TokenType.DM_Preproc_Whitespace) { // Whitespace between the identifier and a left-paren turns it into a non-function macro.
            macroToken = GetNextToken();
        }

        while (macroToken.Type != TokenType.Newline && macroToken.Type != TokenType.EndOfFile) {
            // A line splice followed by another new line will end the macro without inserting the line splice
            if (macroToken.Type == TokenType.DM_Preproc_LineSplice) {
                var nextToken = GetNextToken(true);

                // If the next token is another newline, immediately stop adding new tokens
                if (nextToken.Type == TokenType.Newline) {
                    break;
                }
                macroTokens.Add(macroToken);
                macroToken = nextToken;
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
            DMCompiler.Emit(WarningCode.BadDirective, defineIdentifier.Location, "Invalid macro identifier");
            return;
        } else if (!_defines.ContainsKey(defineIdentifier.Text)) {
            DMCompiler.Emit(WarningCode.UndefineMissingDirective, defineIdentifier.Location, $"No macro named \"{defineIdentifier.PrintableText}\"");
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
        DMLexer lexer = new(_lexerStack.Peek().File, tokens);
        List<Token> newTokens = new List<Token>();
        for(Token token = lexer.GetNextToken(); !lexer.AtEndOfSource; token = lexer.GetNextToken()) {
            newTokens.Add(token);
        }
        return newTokens;
    }

    /// <summary>If this <see cref="TokenType.DM_Preproc_Identifier"/> Token is a macro, pushes all of its tokens onto the queue.</summary>
    /// <returns>true if the Token ended up meaning a macro sequence.</returns>
    private bool TryMacro(Token token) {
        if (token.Type != TokenType.DM_Preproc_Identifier) // Check this before passing anything to this function.
            throw new ArgumentException("Given token must be a DM_Preproc_Identifier", nameof(token));

        if (!_defines.TryGetValue(token.Text, out DMMacro? macro)) {
            return false;
        }

        List<List<Token>>? parameters = null;
        if (macro.HasParameters() && !TryGetMacroParameters(out parameters)) {
            return false;
        }

        List<Token> expandedTokens = macro.Expand(token, parameters);
        for (int i = expandedTokens.Count - 1; i >= 0; i--) {
            Token expandedToken = expandedTokens[i];
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
            DMCompiler.Emit(WarningCode.BadDirective, ifToken.Location, "Expression expected for #if");
            HandleDegenerateIf();
            return;
        }
        float? expr = DMPreprocessorParser.ExpressionFromTokens(tokens, _defines);
        if(expr is null) {
            DMCompiler.Emit(WarningCode.BadDirective, ifToken.Location, "Expression is invalid");
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
            DMCompiler.Emit(WarningCode.BadDirective, ifDefToken.Location, "Expected a define identifier");
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
            DMCompiler.Emit(WarningCode.BadDirective, ifNDefToken.Location, "Expected a define identifier");
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
            DMCompiler.Emit(WarningCode.BadDirective, elifToken.Location, "Unexpected #elif");
        if (wasTruthy is null) {
            DMCompiler.Emit(WarningCode.BadDirective, elifToken.Location, "Directive #elif cannot appear after #else in its flow control");
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

        DMCompiler.Emit(
            token.Type == TokenType.DM_Preproc_Error ? WarningCode.ErrorDirective : WarningCode.WarningDirective,
            token.Location, token.Text);
    }

    private void PushToken(Token token) {
        _unprocessedTokens.Push(token);
    }

    /// <remarks>
    /// WARNING: Do not call this with the <see langword="true"/> argument <br/>
    /// unless you are completely sure that the clobbered whitespace will NEVER have any grammatical significance <br/>
    /// neither here in the preprocessor, nor in any other parsing pass! <br/><br/>
    /// If whitespace may be important later, use <see cref="CheckForTokenIgnoringWhitespace(TokenType, out Token)"/>.
    /// </remarks>
    private Token GetNextToken(bool ignoreWhitespace = false) {
        if (_unprocessedTokens.TryPop(out Token nextToken)) {
            if (ignoreWhitespace && nextToken.Type == TokenType.DM_Preproc_Whitespace) { // This doesn't need to be a loop since whitespace tokens should never occur next to each other
                nextToken = GetNextToken(true);
            }

            return nextToken;
        } else {
            return _lexerStack.Peek().NextToken(ignoreWhitespace);
        }
    }

    private void HandlePragmaDirective(Token pragmaDirective) {
        Token warningNameToken = GetNextToken(true);
        WarningCode warningCode;
        switch(warningNameToken.Type) {
            case TokenType.DM_Preproc_Identifier: {
                if (!Enum.TryParse(warningNameToken.Text, out warningCode)) {
                    DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Warning '{warningNameToken.PrintableText}' does not exist");
                    GetLineOfTokens(); // consume what's on this line and leave
                    return;
                }

                break;
            }
            case TokenType.DM_Preproc_Number: {
                if (!int.TryParse(warningNameToken.Text, out var intValue)) {
                    DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Warning OD{warningNameToken.PrintableText} does not exist");
                    GetLineOfTokens();
                    return;
                }

                warningCode = (WarningCode)intValue;
                break;
            }
            default: {
                DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Invalid warning identifier '{warningNameToken.PrintableText}'");
                GetLineOfTokens();
                return;
            }
        }

        if((int)warningCode < 1000) {
            DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Warning OD{(int)warningCode:d4} cannot be set - it must always be an error");
            GetLineOfTokens();
            return;
        }

        Token warningTypeToken = GetNextToken(true);
        if (warningTypeToken.Type != TokenType.DM_Preproc_Identifier) {
            DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Warnings can only be set to disabled, notice, warning, or error");
            return;
        }
        switch(warningTypeToken.Text.ToLower()) {
            case "disabled":
            case "disable":
                DMCompiler.SetPragma(warningCode, ErrorLevel.Disabled);
                break;
            case "notice":
            case "pedantic":
            case "info":
                DMCompiler.SetPragma(warningCode, ErrorLevel.Notice);
                break;
            case "warning":
            case "warn":
                DMCompiler.SetPragma(warningCode, ErrorLevel.Warning);
                break;
            case "error":
            case "err":
                DMCompiler.SetPragma(warningCode, ErrorLevel.Error);
                break;
            default:
                DMCompiler.Emit(WarningCode.BadDirective, warningNameToken.Location, $"Warnings can only be set to disabled, notice, warning, or error");
                return;
        }
    }

    private bool Check(TokenType tokenType) {
        Token received = GetNextToken();
        if(received.Type == tokenType)
            return true;
        PushToken(received);
        return false;
    }

    /// <summary>
    /// The alternative to <see cref="GetNextToken(bool)"/> if you don't know whether you'll consume the whitespace or not.
    /// </summary>
    private bool CheckForTokenIgnoringWhitespace(TokenType type, [NotNullWhen(true)] out Token? result) {
        Token firstToken = GetNextToken();
        if (firstToken.Type == TokenType.DM_Preproc_Whitespace) { // This doesn't need to be a loop since whitespace tokens should never occur next to each other
            Token secondToken = GetNextToken();
            if (secondToken.Type != type) { //Rollback!
                PushToken(secondToken);
                PushToken(firstToken);
                result = null;
                return false;
            }
            result = secondToken;
            return true;
        } else if (firstToken.Type == type) {
            result = firstToken;
            return true;
        } else {
            PushToken(firstToken);
            result = null;
            return false;
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
                        DMCompiler.Emit(WarningCode.BadDirective, token.Location, $"Unexpected {token.PrintableText} directive");
                    _unprocessedTokens.Push(token); // Push it back onto the stack so we can interpret this later
                    return true;
                default:
                    continue; // Don't need to do the ifStack check since it has not changed as a result of this token
            }
            if (ifStack == 0) {
                if (!calledByElseDirective) {
                    _unprocessedTokens.Push(token); // Push it back onto the stack so we can interpret the entry in _lastIfEvaluations correctly.
                }

                return false;
            }
        }
        DMCompiler.Emit(WarningCode.BadDirective, Location.Unknown, "Missing #endif directive");
        return false;
    }

    private bool TryGetMacroParameters(out List<List<Token>>? parameters) {
        if (!CheckForTokenIgnoringWhitespace(TokenType.DM_Preproc_Punctuator_LeftParenthesis, out var leftParenToken)) {
            parameters = null;
            return false;
        }

        parameters = new();
        List<Token> currentParameter = new();

        Token parameterToken = GetNextToken(true);
        while (parameterToken.Type == TokenType.Newline) { // Skip newlines after the left parenthesis
            parameterToken = GetNextToken(true);
        }

        int parenthesisNesting = 1;
        while(true) {
            switch (parameterToken.Type) {
                case TokenType.DM_Preproc_Punctuator_Comma when parenthesisNesting == 1:
                    parameters.Add(currentParameter);
                    currentParameter = new List<Token>();
                    parameterToken = GetNextToken(true);
                    while(parameterToken.Type == TokenType.Newline) {
                        currentParameter.Add(new Token(TokenType.DM_Preproc_LineSplice, "", parameterToken.Location, null));
                        parameterToken = GetNextToken(true);
                    }
                    continue;
                case TokenType.DM_Preproc_Punctuator_LeftParenthesis:
                    parenthesisNesting++;
                    currentParameter.Add(parameterToken);
                    parameterToken = GetNextToken();
                    continue;
                case TokenType.DM_Preproc_Punctuator_RightParenthesis:
                    parenthesisNesting--;
                    if (parenthesisNesting == 0) // if that's our paren
                        break; // break out
                    //otherwise, add it as another token for this parameter
                    currentParameter.Add(parameterToken);
                    parameterToken = GetNextToken();
                    continue;
                case TokenType.EndOfFile:
                    PushToken(parameterToken);
                    break;
                default:
                    currentParameter.Add(parameterToken);
                    parameterToken = GetNextToken();
                    continue;
            }
            break; // If it manages to escape the switch, the loop breaks
        }

        parameters.Add(currentParameter);
        if (parameterToken.Type != TokenType.DM_Preproc_Punctuator_RightParenthesis) {
            DMCompiler.Emit(WarningCode.BadDirective, leftParenToken.Value.Location, "Missing ')' in macro call");

            return false;
        }

        return true;
    }
}
