using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.Compiler.DM;

public partial class DMParser {
    /// <summary>
    /// If the expression is null, emit an error and set it to a new <see cref="DMASTInvalidExpression" />
    /// </summary>
    /// <param name="expression">The expression that must have a value</param>
    /// <param name="errorMessage">Message to error with if the expression is null</param>
    protected void RequireExpression([NotNull] ref DMASTExpression? expression, string? errorMessage = null) {
        if (expression != null)
            return;

        Emit(WarningCode.MissingExpression, CurrentLoc, errorMessage ?? "Expected an expression");
        expression = new DMASTInvalidExpression(CurrentLoc);
    }

    protected bool PeekDelimiter() {
        return Current().Type == TokenType.Newline || Current().Type == TokenType.DM_Semicolon;
    }

    protected void LocateNextStatement() {
        while (!PeekDelimiter() && Current().Type != TokenType.DM_Dedent) {
            Advance();

            if (Current().Type == TokenType.EndOfFile) {
                break;
            }
        }
    }

    protected void LocateNextTopLevel() {
        do {
            LocateNextStatement();

            Delimiter();
            while (Current().Type == TokenType.DM_Dedent) {
                Advance();
            }

            if (Current().Type == TokenType.EndOfFile) break;
        } while (((DMLexer)_lexer).CurrentIndentation() != 0);

        Delimiter();
    }

    private void ConsumeRightParenthesis() {
        // A missing right parenthesis has to subtract 1 from the lexer's bracket nesting counter
        // To keep indentation working correctly
        if (!Check(TokenType.DM_RightParenthesis)) {
            ((DMLexer)_lexer).BracketNesting--;
            Emit(WarningCode.BadToken, "Expected ')'");
        }
    }

    private void ConsumeRightBracket() {
        // Similar to ConsumeRightParenthesis()
        if (!Check(TokenType.DM_RightBracket)) {
            ((DMLexer)_lexer).BracketNesting--;
            Emit(WarningCode.BadToken, "Expected ']'");
        }
    }

    /// <summary> Small helper function for <see cref="ExpressionFromString"/>, for macros that require a preceding expression in the string.</summary>
    /// <returns><see langword="true"/> if error occurs.</returns>
    private bool CheckInterpolation(Location loc, bool hasSeenNonRefInterpolation, List<DMASTExpression?>? interpolationValues, string mack) {
        if (interpolationValues == null || interpolationValues.Count == 0) {
            DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, loc, $"Macro \"\\{mack}\" requires preceding interpolated expression");
            return true;
        }

        if(!hasSeenNonRefInterpolation) { // More elaborate error for a more elaborate situation
            DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, loc, $"Macro \"\\{mack}\" requires preceding interpolated expression that is not a reference");
            return true;
        }

        return false;
    }

    private bool TryConvertUtfCodeToString(ReadOnlySpan<char> input, ref StringBuilder stringBuilder) {
        int utf32Code;
        if (!int.TryParse(input, style: System.Globalization.NumberStyles.HexNumber, provider: null, out utf32Code)) {
            return false;
        }
        stringBuilder.Append(char.ConvertFromUtf32(utf32Code));
        return true;
    }

    /// <summary>
    /// Handles parsing of Tokens of type <see cref="TokenType.DM_ConstantString"/> or a series of tokens starting with <see cref="TokenType.DM_StringBegin"/>.<br/>
    /// (Shunted into a helper because this is a quite long and arduous block of code)
    /// </summary>
    /// <returns>Either a <see cref="DMASTConstantString"/> or a <see cref="DMASTStringFormat"/>.</returns>
    private DMASTExpression ExpressionFromString() {
        // The actual text (but includes special codepoints for macros and markers for where interps go)
        StringBuilder stringBuilder = new();
        List<DMASTExpression?>? interpolationValues = null;
        StringFormatEncoder.FormatSuffix currentInterpolationType = StringFormatEncoder.InterpolationDefault;
        string? usedPrefixMacro = null; // A string holding the name of the last prefix macro (\the, \a etc.) used, for error presentation poipoises
        bool hasSeenNonRefInterpolation = false;
        var tokenLoc = Current().Location;

        while (true) {
            Token currentToken = Current();
            Advance();

            string tokenValue = currentToken.ValueAsString();

            // If an interpolation comes after this, ignore the last character (always '[')
            int iterateLength = currentToken.Type is TokenType.DM_StringBegin or TokenType.DM_StringMiddle
                ? tokenValue.Length - 1
                : tokenValue.Length;

            // If an interpolation came before this, ignore the first character (always ']')
            int iterateBegin = currentToken.Type is TokenType.DM_StringMiddle or TokenType.DM_StringEnd ? 1 : 0;

            stringBuilder.EnsureCapacity(stringBuilder.Length + iterateLength - iterateBegin);

            for (int i = iterateBegin; i < iterateLength; i++) {
                char c = tokenValue[i];

                switch (c) {
                    case '\\': {
                        string escapeSequence = string.Empty;

                        if (i == tokenValue.Length - 1) {
                            Emit(WarningCode.BadToken, "Invalid escape sequence");
                            break;
                        }

                        c = tokenValue[++i];

                        int? utfCodeDigitsExpected = null;
                        switch (c) {
                            case 'x':
                                utfCodeDigitsExpected = 2; break;
                            case 'u':
                                utfCodeDigitsExpected = 4; break;
                            case 'U':
                                utfCodeDigitsExpected = 6; break;
                        }

                        if (utfCodeDigitsExpected.HasValue) {
                            i++;
                            int utfCodeLength = Math.Min(utfCodeDigitsExpected.Value, tokenValue.Length - i);
                            var utfCode = tokenValue.AsSpan(i, utfCodeLength);
                            if (utfCodeLength < utfCodeDigitsExpected.Value || !TryConvertUtfCodeToString(utfCode, ref stringBuilder)) {
                                Emit(WarningCode.BadToken, $"Invalid Unicode macro \"\\{c}{utfCode}\"");
                                break;
                            }

                            i += utfCodeLength - 1; // -1, cause we have i++ in the current 'for' expression
                        } else if (char.IsLetter(c)) {
                            while (i < tokenValue.Length && char.IsLetter(tokenValue[i])) {
                                escapeSequence += tokenValue[i++];
                            }
                            i--;

                            bool skipSpaces = false;
                            bool consumeSpaceCharacter = false;
                            switch (escapeSequence) {
                                case "Proper": // Users can have a little case-insensitivity, as a treat
                                case "Improper":
                                    Warning($"Escape sequence \"\\{escapeSequence}\" should not be capitalized. Coercing macro to \"\\{escapeSequence.ToLower()}");
                                    escapeSequence = escapeSequence.ToLower();
                                    goto case "proper"; // Fallthrough!
                                case "proper":
                                case "improper":
                                    if (stringBuilder.Length != 0) {
                                        Emit(WarningCode.BadToken, $"Escape sequence \"\\{escapeSequence}\" must come at the beginning of the string");
                                        break;
                                    }

                                    skipSpaces = true;
                                    if(escapeSequence == "proper")
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.Proper));
                                    else
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.Improper));
                                    break;
                                case "roman":
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerRoman));
                                    break;
                                case "Roman":
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperRoman));
                                    break;

                                case "ref":
                                    // usedPrefixMacro = true; -- while ref is indeed a prefix macro, it DOES NOT ERROR if it fails to find what it's supposed to /ref.
                                    // TODO: Actually care about this when we add --noparity
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.ReferenceOfValue; break;

                                case "The":
                                    usedPrefixMacro = "The";
                                    consumeSpaceCharacter = true;
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperDefiniteArticle));
                                    break;
                                case "the":
                                    usedPrefixMacro = "the";
                                    consumeSpaceCharacter = true;
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerDefiniteArticle));
                                    break;

                                case "A":
                                case "An":
                                    usedPrefixMacro = escapeSequence;
                                    consumeSpaceCharacter = true;
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperIndefiniteArticle));
                                    break;
                                case "a":
                                case "an":
                                    usedPrefixMacro = escapeSequence;
                                    consumeSpaceCharacter = true;
                                    currentInterpolationType = StringFormatEncoder.FormatSuffix.StringifyNoArticle;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerIndefiniteArticle));
                                    break;

                                case "He":
                                case "She":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperSubjectPronoun));
                                    break;
                                case "he":
                                case "she":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerSubjectPronoun));
                                    break;

                                case "His":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "His")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessiveAdjective));
                                    break;
                                case "his":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "his")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessiveAdjective));
                                    break;

                                case "Him": // BYOND errors here but lets be nice!
                                    Warning("\"\\Him\" is not an available text macro. Coercing macro into \"\\him\"");
                                    goto case "him"; // Fallthrough!
                                case "him":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "him")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ObjectPronoun));
                                    break;

                                case "Her":
                                case "her":
                                    Emit(WarningCode.BadToken, "\"Her\" is a grammatically ambiguous pronoun. Use \\him or \\his instead");
                                    break;

                                case "himself":
                                case "herself":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ReflexivePronoun));
                                    break;

                                case "Hers":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "Hers")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessivePronoun));
                                    break;
                                case "hers":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "hers")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessivePronoun));
                                    break;
                                //Plurals, ordinals, etc
                                //(things that hug, as a suffix, the [] that they reference)
                                case "s":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "s")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.PluralSuffix));
                                    break;
                                case "th":
                                    if (CheckInterpolation(tokenLoc, hasSeenNonRefInterpolation, interpolationValues, "th")) break;
                                    // TODO: this should error if not DIRECTLY after an expression ([]\s vs []AA\s)
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.OrdinalIndicator));
                                    break;
                                default:
                                    if (escapeSequence.StartsWith("n")) {
                                        stringBuilder.Append('\n');
                                        stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                    } else if (escapeSequence.StartsWith("t")) {
                                        stringBuilder.Append('\t');
                                        stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                    } else if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence)) { // This only exists to allow unimplements to fallthrough w/o a direct error
                                        Emit(WarningCode.BadToken, $"Invalid escape sequence \"\\{escapeSequence}\"");
                                    }

                                    break;
                            }

                            if (skipSpaces) {
                                // Note that some macros in BYOND require a single/zero space between them and the []
                                // This doesn't replicate that
                                while (i < tokenValue.Length - 1 && tokenValue[i + 1] == ' ') i++;
                            }

                            if(consumeSpaceCharacter) {
                                if (i < tokenValue.Length - 1 && tokenValue[i + 1] == ' ') i++;
                            }
                        }
                        else {
                            escapeSequence += c;
                            switch (escapeSequence) {
                                case "[":
                                case "]":
                                case "<":
                                case ">":
                                case "\"":
                                case "'":
                                case "\\":
                                case " ":
                                case ".":
                                    stringBuilder.Append(escapeSequence);
                                    break;
                                default: //Unimplemented escape sequence
                                    Emit(WarningCode.BadToken, "Invalid escape sequence \"\\" + escapeSequence + "\"");
                                    break;
                            }
                        }

                        break;
                    }
                    default: {
                        stringBuilder.Append(c);
                        break;
                    }
                }
            }

            // We've parsed the text of this piece of string, what happens next depends on what token this was
            switch (currentToken.Type) {
                case TokenType.DM_ConstantString: // Constant singular piece of string, return here
                    if (usedPrefixMacro != null) // FIXME: \the should not compiletime here, instead becoming a tab character followed by "he", when in parity mode
                        DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, tokenLoc,
                            $"Macro \"\\{usedPrefixMacro}\" requires interpolated expression");

                    return new DMASTConstantString(currentToken.Location, stringBuilder.ToString());
                case TokenType.DM_StringBegin:
                case TokenType.DM_StringMiddle: // An interpolation is coming up, collect the expression
                    interpolationValues ??= new(1);

                    Whitespace();
                    if (Current().Type is TokenType.DM_StringMiddle or TokenType.DM_StringEnd) { // Empty interpolation
                        interpolationValues.Add(null);
                    } else {
                        var interpolatedExpression = Expression();
                        if (interpolatedExpression == null)
                            DMCompiler.Emit(WarningCode.MissingExpression, Current().Location,
                                "Expected an embedded expression");

                        // The next token should be the next piece of the string, error if not
                        if (Current().Type is not TokenType.DM_StringMiddle and not TokenType.DM_StringEnd) {
                            DMCompiler.Emit(WarningCode.BadExpression, Current().Location,
                                "Expected end of the embedded expression");

                            while (Current().Type is not TokenType.DM_StringMiddle and not TokenType.DM_StringEnd
                                   and not TokenType.EndOfFile) {
                                Advance();
                            }
                        }
                        interpolationValues.Add(interpolatedExpression);
                    }

                    hasSeenNonRefInterpolation |= currentInterpolationType != StringFormatEncoder.FormatSuffix.ReferenceOfValue;
                    stringBuilder.Append(StringFormatEncoder.Encode(currentInterpolationType));
                    currentInterpolationType = StringFormatEncoder.InterpolationDefault;
                    break;
                case TokenType.DM_StringEnd: // End of a string with interpolated values, return here
                    if(currentInterpolationType != StringFormatEncoder.InterpolationDefault) { // this implies a prefix tried to modify a [] that never ended up existing after it
                        DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, tokenLoc,
                            $"Macro \"\\{usedPrefixMacro}\" must precede an interpolated expression");
                    }

                    return new DMASTStringFormat(tokenLoc, stringBuilder.ToString(), interpolationValues!.ToArray());
            }
        }
    }
}
