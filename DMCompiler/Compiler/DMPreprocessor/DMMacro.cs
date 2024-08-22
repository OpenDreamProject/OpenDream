using System;
using System.Collections.Generic;
using System.Text;

namespace DMCompiler.Compiler.DMPreprocessor;

internal class DMMacro {
    private readonly List<string>? _parameters;
    private readonly List<Token>? _tokens;
    private readonly string? _overflowParameter;
    private readonly int _overflowParameterIndex;

    public DMMacro(List<string>? parameters, List<Token>? tokens) {
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

        if (_tokens != null) {
            // Concat tokens cause any whitespace directly before them to be ignored
            for (int i = 1; i < _tokens.Count; i++) {
                Token token = _tokens[i];
                Token lastToken = _tokens[i - 1];

                if (token.Type == TokenType.DM_Preproc_TokenConcat &&
                    lastToken.Type == TokenType.DM_Preproc_Whitespace) {
                    _tokens.RemoveAt(--i);
                }
            }
        }
    }

    public bool HasParameters() {
        return _parameters != null;
    }

    /// <summary>
    /// Takes given parameters and creates a list of tokens representing the expanded macro
    /// </summary>
    /// <param name="replacing">The identifier being replaced with this macro</param>
    /// <param name="parameters">Parameters for macro expansion. Null if none given.</param>
    /// <returns>A list of tokens replacing the identifier</returns>
    /// <exception cref="ArgumentException">Thrown if no parameters were given but are required</exception>
    // TODO: Convert this to an IEnumerator<Token>? Could cut down on allocations.
    public virtual List<Token>? Expand(Token replacing, List<List<Token>>? parameters) {
        if (_tokens == null)
            return null;

        // If this macro has no parameters then we can just return our list of tokens
        if (!HasParameters())
            return _tokens;

        // If we have parameters but weren't given any, throw an exception
        if (parameters == null)
            throw new ArgumentException("This macro requires parameters");

        List<Token> expandedTokens = new(_tokens.Count);
        foreach (Token token in _tokens) {
            string parameterName =
                token.Type is TokenType.DM_Preproc_TokenConcat or TokenType.DM_Preproc_ParameterStringify
                    ? token.ValueAsString()
                    : token.Text;
            int parameterIndex = _parameters!.IndexOf(parameterName);

            if (parameterIndex != -1 && parameters.Count > parameterIndex) {
                List<Token> parameter = parameters[parameterIndex];

                if (token.Type == TokenType.DM_Preproc_ParameterStringify) {
                    StringBuilder tokenTextBuilder = new StringBuilder();

                    // Use a raw string. Use '#' because that can't appear in an expression.
                    // this does mean, however, that a '#' inside the string will make the preprocessor dump produce invalid code
                    tokenTextBuilder.Append("@#");
                    foreach (Token parameterToken in parameter) {
                        tokenTextBuilder.Append(parameterToken.Text);
                    }

                    tokenTextBuilder.Append('#');

                    string tokenText = tokenTextBuilder.ToString();
                    expandedTokens.Add(new Token(TokenType.DM_Preproc_ConstantString, tokenText,
                        Location.Unknown, tokenText.Substring(2, tokenText.Length - 3)));
                } else {
                    foreach (var parameterToken in parameter) {
                        switch (parameterToken.Type) {
                            case TokenType.DM_Preproc_Identifier:
                            case TokenType.DM_Preproc_Number:
                                if (expandedTokens.Count == 0)
                                    goto default;

                                // If the last token was an identifier, we need to combine the two
                                var lastToken = expandedTokens[^1];
                                if (lastToken.Type != TokenType.DM_Preproc_Identifier)
                                    goto default;

                                // A new identifier made up of the last one and this identifier/number
                                expandedTokens[^1] = new Token(TokenType.DM_Preproc_Identifier,
                                    lastToken.Text + parameterToken.Text, lastToken.Location, null);
                                break;
                            default:
                                expandedTokens.Add(parameterToken);
                                break;
                        }
                    }
                }
            } else if (_overflowParameter != null && parameterName == _overflowParameter) {
                for (int i = _overflowParameterIndex; i < parameters.Count; i++) {
                    expandedTokens.AddRange(parameters[i]);

                    if(i < parameters.Count-1)
                        expandedTokens.Add(new Token(TokenType.DM_Preproc_Punctuator_Comma, ",", Location.Unknown, null));
                }
            } else {
                if (token.Type == TokenType.DM_Preproc_ParameterStringify) {
                    expandedTokens.Add(new Token(TokenType.DM_Preproc_ConstantString, $"@#{parameterName}#",
                        Location.Unknown, parameterName));
                } else if (token.Type == TokenType.DM_Preproc_TokenConcat) {
                    expandedTokens.Add(new Token(TokenType.DM_Preproc_Identifier, parameterName,
                        Location.Unknown, null));
                } else {
                    expandedTokens.Add(token);
                }
            }
        }

        return expandedTokens;
    }
}

// __LINE__
internal sealed class DMMacroLine() : DMMacro(null, null) {
    public override List<Token> Expand(Token replacing, List<List<Token>>? parameters) {
        var line = replacing.Location.Line;
        if (line == null)
            throw new ArgumentException($"Token {replacing} does not have a line number", nameof(replacing));

        return [
            new Token(TokenType.DM_Preproc_Number, line.Value.ToString(), replacing.Location, null)
        ];
    }
}

// __FILE__
internal sealed class DMMacroFile() : DMMacro(null, null) {
    public override List<Token> Expand(Token replacing, List<List<Token>>? parameters) {
        string path = replacing.Location.SourceFile.Replace(@"\", @"\\"); //Escape any backwards slashes

        return [
            new Token(TokenType.DM_Preproc_ConstantString, $"\"{path}\"", replacing.Location, path)
        ];
    }
}

// DM_VERSION
internal sealed class DMMacroVersion() : DMMacro(null, null) {
    public override List<Token> Expand(Token replacing, List<List<Token>>? parameters) {
        return [
            new Token(TokenType.DM_Preproc_Number, DMCompiler.Settings.DMVersion, replacing.Location, null)
        ];
    }
}

// DM_BUILD
internal sealed class DMMacroBuild() : DMMacro(null, null) {
    public override List<Token> Expand(Token replacing, List<List<Token>>? parameters) {
        return [
            new Token(TokenType.DM_Preproc_Number, DMCompiler.Settings.DMBuild, replacing.Location, null)
        ];
    }
}
