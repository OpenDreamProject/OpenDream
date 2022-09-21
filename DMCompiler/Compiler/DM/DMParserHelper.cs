using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DMPreprocessor;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {
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
        private bool CheckInterpolation(List<DMASTExpression>? interpolationValues, string mack)
        {
            if (interpolationValues == null || interpolationValues.Count == 0)
            {
                Error($"Macro \"\\{mack}\" requires preceding interpolated expression");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles parsing of Tokens of type <see cref="TokenType.DM_String"/>.<br/>
        /// (Shunted into a helper because this is a quite long and arduous block of code)
        /// </summary>
        /// <returns>Either a <see cref="DMASTConstantString"/> or a <see cref="DMASTStringFormat"/>.</returns>
        private DMASTExpression ExpressionFromString(Token constantToken)
        {
            string tokenValue = (string)constantToken.Value;
            StringBuilder stringBuilder = new StringBuilder(tokenValue.Length); // The actual text (but includes special codepoints for macros and markers for where interps go)
            List<DMASTExpression>? interpolationValues = null;
            Advance();

            int bracketNesting = 0;
            StringBuilder? insideBrackets = null;
            StringFormatEncoder.FormatSuffix currentInterpolationType = StringFormatEncoder.InterpolationDefault;
            string usedPrefixMacro = null; // A string holding the name of the last prefix macro (\the, \a etc.) used, for error presentation poipoises
            for (int i = 0; i < tokenValue.Length; i++)
            {
                char c = tokenValue[i];


                if (bracketNesting > 0)
                {
                    insideBrackets!.Append(c); // should never be null
                }

                switch (c)
                {
                    case '[':
                        bracketNesting++;
                        insideBrackets ??= new StringBuilder(tokenValue.Length - stringBuilder.Length);
                        interpolationValues ??= new List<DMASTExpression>(1);
                        break;
                    case ']' when bracketNesting > 0:
                        {
                            bracketNesting--;

                            if (bracketNesting == 0)
                            { //End of expression
                                insideBrackets.Remove(insideBrackets.Length - 1, 1); //Remove the ending bracket

                                string insideBracketsText = insideBrackets?.ToString();
                                if (!String.IsNullOrWhiteSpace(insideBracketsText))
                                {
                                    DMPreprocessorLexer preprocLexer = new DMPreprocessorLexer(null, constantToken.Location.SourceFile, insideBracketsText);
                                    List<Token> preprocTokens = new();
                                    Token preprocToken;
                                    do
                                    {
                                        preprocToken = preprocLexer.GetNextToken();
                                        preprocToken.Location = constantToken.Location;
                                        preprocTokens.Add(preprocToken);
                                    } while (preprocToken.Type != TokenType.EndOfFile);

                                    DMLexer expressionLexer = new DMLexer(constantToken.Location.SourceFile, preprocTokens);
                                    DMParser expressionParser = new DMParser(expressionLexer, _unimplementedWarnings);

                                    DMASTExpression expression = null;
                                    try
                                    {
                                        expressionParser.Whitespace(true);
                                        expression = expressionParser.Expression();
                                        if (expression == null) Error("Expected an expression");
                                        if (expressionParser.Current().Type != TokenType.EndOfFile) Error("Expected end of embedded statement");
                                    }
                                    catch (CompileErrorException e)
                                    {
                                        Errors.Add(e.Error);
                                    }

                                    if (expressionParser.Errors.Count > 0) Errors.AddRange(expressionParser.Errors);
                                    if (expressionParser.Warnings.Count > 0) Warnings.AddRange(expressionParser.Warnings);
                                    interpolationValues.Add(expression);
                                }
                                else
                                {
                                    interpolationValues.Add(null);
                                }
                                stringBuilder.Append(StringFormatEncoder.Encode(currentInterpolationType));

                                currentInterpolationType = StringFormatEncoder.InterpolationDefault;
                                insideBrackets.Clear();
                            }

                            break;
                        }
                    case '\\' when bracketNesting == 0:
                        {
                            string escapeSequence = String.Empty;

                            if (i == tokenValue.Length)
                            {
                                Error("Invalid escape sequence");
                            }
                            c = tokenValue[++i];

                            if (char.IsLetter(c))
                            {
                                while (i < tokenValue.Length && char.IsLetter(tokenValue[i]))
                                {
                                    escapeSequence += tokenValue[i++];
                                }
                                i--;

                                bool unimplemented = false;
                                bool skipSpaces = false;
                                bool consumeSpaceCharacter = false;
                                switch (escapeSequence)
                                {
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
                                        if (CheckInterpolation(interpolationValues, escapeSequence)) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperSubjectPronoun));
                                        break;
                                    case "he":
                                    case "she":
                                        if (CheckInterpolation(interpolationValues, escapeSequence)) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerSubjectPronoun));
                                        break;

                                    case "His":
                                        if (CheckInterpolation(interpolationValues, "His")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessiveAdjective));
                                        break;
                                    case "his":
                                        if (CheckInterpolation(interpolationValues, "his")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessiveAdjective));
                                        break;

                                    case "Him": // BYOND errors here but lets be nice!
                                        Warning("\"\\Him\" is not an available text macro. Coercing macro into \"\\him\"");
                                        goto case "him"; // Fallthrough!
                                    case "him":
                                        if (CheckInterpolation(interpolationValues, "him")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ObjectPronoun));
                                        break;

                                    case "Her":
                                    case "her":
                                        Error("\"Her\" is a grammatically ambiguous pronoun. Use \\him or \\his instead");
                                        break;

                                    case "himself":
                                    case "herself":
                                        if (CheckInterpolation(interpolationValues, escapeSequence)) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.ReflexivePronoun));
                                        break;

                                    case "Hers":
                                        if (CheckInterpolation(interpolationValues, "Hers")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.UpperPossessivePronoun));
                                        break;
                                    case "hers":
                                        if (CheckInterpolation(interpolationValues, "hers")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.LowerPossessivePronoun));
                                        break;
                                    //Plurals, ordinals, etc
                                    //(things that hug, as a suffix, the [] that they reference)
                                    case "s":
                                        unimplemented = true;
                                        if (CheckInterpolation(interpolationValues, "s")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.PluralSuffix));
                                        break;
                                    case "th":
                                        unimplemented = true;
                                        if (CheckInterpolation(interpolationValues, "th")) break;
                                        stringBuilder.Append(StringFormatEncoder.Encode(StringFormatEncoder.FormatSuffix.OrdinalIndicator));
                                        break;
                                    default:
                                        if (escapeSequence.StartsWith("n"))
                                        {
                                            stringBuilder.Append('\n');
                                            stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                        }
                                        else if (escapeSequence.StartsWith("t"))
                                        {
                                            stringBuilder.Append('\t');
                                            stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                        }
                                        else if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence)) // This only exists to allow unimplements to fallthrough w/o a direct error
                                        {
                                            Error($"Invalid escape sequence \"\\{escapeSequence}\"");
                                        }

                                        break;
                                }

                                if (unimplemented)
                                {
                                    DMCompiler.UnimplementedWarning(constantToken.Location, $"Unimplemented escape sequence \"{escapeSequence}\"");
                                }
                                if (skipSpaces)
                                {
                                    // Note that some macros in BYOND require a single/zero space between them and the []
                                    // This doesn't replicate that
                                    while (i < tokenValue.Length - 1 && tokenValue[i + 1] == ' ') i++;
                                }
                                if(consumeSpaceCharacter)
                                {
                                    if (i < tokenValue.Length - 1 && tokenValue[i + 1] == ' ') i++;
                                }
                            }
                            else
                            {
                                escapeSequence += c;
                                switch (escapeSequence)
                                {
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
                    default:
                        {
                            if (bracketNesting == 0)
                            {
                                stringBuilder.Append(c);
                            }

                            break;
                        }
                }
            }

            if (bracketNesting > 0) Error("Expected ']'");

            string stringValue = stringBuilder.ToString();
            if (interpolationValues is null)
            {
                if (usedPrefixMacro != null) // FIXME: \the should not compiletime here, instead becoming a tab character followed by "he", when in parity mode
                    Error($"Macro \"\\{usedPrefixMacro}\" requires interpolated expression");
                return new DMASTConstantString(constantToken.Location, stringValue);
            }
            if(currentInterpolationType != StringFormatEncoder.InterpolationDefault) // this implies a prefix tried to modify a [] that never ended up existing after it
            {
                Error($"Macro \"\\{usedPrefixMacro}\" must precede an interpolated expression");
            }
            return new DMASTStringFormat(constantToken.Location, stringValue, interpolationValues.ToArray());
        }
    }
}
