using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DMPreprocessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {

        /// <summary>
        /// A special override of Error() since, for DMParser, we know we are in a compilation context and can make use of error codes.
        /// </summary>
        /// <remarks>
        /// Should only be called AFTER <see cref="DMCompiler"/> has built up its list of pragma configurations.
        /// </remarks>
        /// <returns> True if this will raise an error, false if not. You can use this return value to help improve error emission around this (depending on how permissive we're being)</returns>
        protected bool Error(WarningCode code, string message) {
            ErrorLevel level = DMCompiler.CodeToLevel(code);
            if (Emissions.Count < MAX_EMISSIONS_RECORDED)
                Emissions.Add(new CompilerEmission(level, code, Current().Location, message));
            return level == ErrorLevel.Error;
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
            //A missing right parenthesis has to subtract 1 from the lexer's bracket nesting counter
            //To keep indentation working correctly
            if (!Check(TokenType.DM_RightParenthesis)) {
                ((DMLexer)_lexer).BracketNesting--;
                Error("Expected ')'");
            }
        }

        private void ConsumeRightBracket() {
            //Similar to ConsumeRightParenthesis()
            if (!Check(TokenType.DM_RightBracket)) {
                ((DMLexer)_lexer).BracketNesting--;
                Error("Expected ']'");
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
        /// Handles parsing of Tokens of type <see cref="TokenType.DM_String"/>.<br/>
        /// (Shunted into a helper because this is a quite long and arduous block of code)
        /// </summary>
        /// <returns>Either a <see cref="DMASTConstantString"/> or a <see cref="DMASTStringFormat"/>.</returns>
        private DMASTExpression ExpressionFromString(Token constantToken) {
            string tokenValue = (string)constantToken.Value;
            StringBuilder stringBuilder = new StringBuilder(tokenValue.Length); // The actual text (but includes special codepoints for macros and markers for where interps go)
            List<DMASTExpression?>? interpolationValues = null;
            Advance();

            int bracketNesting = 0;
            StringBuilder? insideBrackets = null;
            StringFormatEncoder.FormatSuffix currentInterpolationType = StringFormatEncoder.InterpolationDefault;
            string usedPrefixMacro = null; // A string holding the name of the last prefix macro (\the, \a etc.) used, for error presentation poipoises
            bool hasSeenNonRefInterpolation = false;
            for (int i = 0; i < tokenValue.Length; i++) {
                char c = tokenValue[i];

                if (bracketNesting > 0) {
                    insideBrackets!.Append(c); // should never be null
                }

                switch (c) {
                    case '[':
                        bracketNesting++;
                        insideBrackets ??= new StringBuilder(tokenValue.Length - stringBuilder.Length);
                        interpolationValues ??= new List<DMASTExpression?>(1);
                        break;
                    case ']' when bracketNesting > 0: {
                        bracketNesting--;

                        if (bracketNesting == 0) { //End of expression
                            insideBrackets.Remove(insideBrackets.Length - 1, 1); //Remove the ending bracket

                            string insideBracketsText = insideBrackets?.ToString();
                            if (!String.IsNullOrWhiteSpace(insideBracketsText)) {
                                DMPreprocessorLexer preprocLexer = new DMPreprocessorLexer(null, constantToken.Location.SourceFile, insideBracketsText);
                                List<Token> preprocTokens = new();
                                Token preprocToken;
                                do {
                                    preprocToken = preprocLexer.GetNextToken();
                                    preprocToken.Location = constantToken.Location;
                                    preprocTokens.Add(preprocToken);
                                } while (preprocToken.Type != TokenType.EndOfFile);

                                DMLexer expressionLexer = new DMLexer(constantToken.Location.SourceFile, preprocTokens);
                                DMParser expressionParser = new DMParser(expressionLexer);

                                DMASTExpression? expression = null;
                                try {
                                    expressionParser.Whitespace(true);
                                    expression = expressionParser.Expression();
                                    if (expression == null) Error("Expected an expression");
                                    if (expressionParser.Current().Type != TokenType.EndOfFile) Error("Expected end of embedded statement");
                                } catch (CompileErrorException e) {
                                    Emissions.Add(e.Error);
                                }

                                if (expressionParser.Emissions.Count > 0) Emissions.AddRange(expressionParser.Emissions);
                                interpolationValues.Add(expression);
                            } else {
                                interpolationValues.Add(null);
                            }
                            hasSeenNonRefInterpolation = hasSeenNonRefInterpolation || currentInterpolationType != StringFormatEncoder.FormatSuffix.ReferenceOfValue;
                            stringBuilder.Append(StringFormatEncoder.Encode(currentInterpolationType));

                            currentInterpolationType = StringFormatEncoder.InterpolationDefault;
                            insideBrackets.Clear();
                        }

                        break;
                    }
                    case '\\' when bracketNesting == 0: {
                        string escapeSequence = String.Empty;

                        if (i == tokenValue.Length) {
                            Error("Invalid escape sequence");
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
                                Error($"Invalid Unicode macro \"\\{c}{utfCode}\"");
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
                                    if (stringBuilder.Length != 0)
                                    {
                                        Error($"Escape sequence \"\\{escapeSequence}\" must come at the beginning of the string");
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
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperSubjectPronoun));
                                    break;
                                case "he":
                                case "she":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerSubjectPronoun));
                                    break;

                                case "His":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "His")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessiveAdjective));
                                    break;
                                case "his":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "his")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessiveAdjective));
                                    break;

                                case "Him": // BYOND errors here but lets be nice!
                                    Warning("\"\\Him\" is not an available text macro. Coercing macro into \"\\him\"");
                                    goto case "him"; // Fallthrough!
                                case "him":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "him")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ObjectPronoun));
                                    break;

                                case "Her":
                                case "her":
                                    Error("\"Her\" is a grammatically ambiguous pronoun. Use \\him or \\his instead");
                                    break;

                                case "himself":
                                case "herself":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, escapeSequence)) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ReflexivePronoun));
                                    break;

                                case "Hers":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "Hers")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessivePronoun));
                                    break;
                                case "hers":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "hers")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessivePronoun));
                                    break;
                                //Plurals, ordinals, etc
                                //(things that hug, as a suffix, the [] that they reference)
                                case "s":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "s")) break;
                                    stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.PluralSuffix));
                                    break;
                                case "th":
                                    if (CheckInterpolation(constantToken.Location, hasSeenNonRefInterpolation, interpolationValues, "th")) break;
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
                                        Error($"Invalid escape sequence \"\\{escapeSequence}\"");
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
                                    Error("Invalid escape sequence \"\\" + escapeSequence + "\"");
                                    break;
                            }
                        }

                        break;
                    }
                    default: {
                        if (bracketNesting == 0) {
                            stringBuilder.Append(c);
                        }

                        break;
                    }
                }
            }

            if (bracketNesting > 0) Error("Expected ']'");

            string stringValue = stringBuilder.ToString();
            if (interpolationValues is null) {
                if (usedPrefixMacro != null) // FIXME: \the should not compiletime here, instead becoming a tab character followed by "he", when in parity mode
                    DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, constantToken.Location,
                        $"Macro \"\\{usedPrefixMacro}\" requires interpolated expression");
                return new DMASTConstantString(constantToken.Location, stringValue);
            }

            if(currentInterpolationType != StringFormatEncoder.InterpolationDefault) { // this implies a prefix tried to modify a [] that never ended up existing after it
                DMCompiler.Emit(WarningCode.MissingInterpolatedExpression, constantToken.Location,
                    $"Macro \"\\{usedPrefixMacro}\" must precede an interpolated expression");
            }

            return new DMASTStringFormat(constantToken.Location, stringValue, interpolationValues.ToArray());
        }
    }
}
