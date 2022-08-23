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
        /// <summary>
        /// Handles parsing of Tokens of type <see cref="TokenType.DM_String"/>.<br/>
        /// (Shunted into a helper because this is a quite long and arduous block of code)
        /// </summary>
        /// <returns>Either a <see cref="DMASTConstantString"/> or a <see cref="DMASTStringFormat"/>.</returns>
        private DMASTExpression ExpressionFromString(Token constantToken)
        {
            string tokenValue = (string)constantToken.Value;
            StringBuilder stringBuilder = new StringBuilder(tokenValue.Length);
            List<DMASTExpression>? interpolationValues = null;
            Advance();

            int bracketNesting = 0;
            StringBuilder? insideBrackets = null;
            StringFormatTypes currentInterpolationType = StringFormatTypes.Stringify;
            for (int i = 0; i < tokenValue.Length; i++)
            {
                char c = tokenValue[i];


                if (bracketNesting > 0)
                {
                    insideBrackets?.Append(c); // should never be null
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
                                if (insideBracketsText != String.Empty)
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
                                    }
                                    catch (CompileErrorException e)
                                    {
                                        Errors.Add(e.Error);
                                    }

                                    if (expressionParser.Warnings.Count > 0) Warnings.AddRange(expressionParser.Warnings);
                                    interpolationValues.Add(expression);
                                }
                                else
                                {
                                    interpolationValues.Add(null);
                                }

                                stringBuilder.Append(StringFormatCharacter);
                                stringBuilder.Append((char)currentInterpolationType);

                                currentInterpolationType = StringFormatTypes.Stringify;
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
                                //TODO: Many of these require [] before the macro instead of after. They should verify that there is one.
                                switch (escapeSequence)
                                {
                                    case "proper":
                                    case "improper":
                                        if (stringBuilder.Length != 0)
                                        {
                                            Error($"Escape sequence \"\\{escapeSequence}\" must come at the beginning of the string");
                                        }

                                        skipSpaces = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append(escapeSequence == "proper" ? (char)StringFormatTypes.Proper : (char)StringFormatTypes.Improper);
                                        break;

                                    case "ref":
                                        currentInterpolationType = StringFormatTypes.Ref; break;

                                    case "The":
                                        skipSpaces = true;
                                        currentInterpolationType = StringFormatTypes.UpperDefiniteArticle;
                                        break;
                                    case "the":
                                        skipSpaces = true;
                                        currentInterpolationType = StringFormatTypes.LowerDefiniteArticle;
                                        break;

                                    case "A":
                                    case "An":
                                        unimplemented = true;
                                        currentInterpolationType = StringFormatTypes.UpperIndefiniteArticle;
                                        break;
                                    case "a":
                                    case "an":
                                        unimplemented = true;
                                        currentInterpolationType = StringFormatTypes.LowerIndefiniteArticle;
                                        break;

                                    case "He":
                                    case "She":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.UpperSubjectPronoun);
                                        break;
                                    case "he":
                                    case "she":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.LowerSubjectPronoun);
                                        break;

                                    case "His":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.UpperPossessiveAdjective);
                                        break;
                                    case "his":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.LowerPossessiveAdjective);
                                        break;

                                    case "him":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.ObjectPronoun);
                                        break;

                                    case "himself":
                                    case "herself":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.ReflexivePronoun);
                                        break;

                                    case "Hers":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.UpperPossessivePronoun);
                                        break;
                                    case "hers":
                                        unimplemented = true;
                                        stringBuilder.Append(StringFormatCharacter);
                                        stringBuilder.Append((char)StringFormatTypes.LowerPossessivePronoun);
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
                                        else if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence))
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
                return new DMASTConstantString(constantToken.Location, stringValue);
            }
            else
            {
                return new DMASTStringFormat(constantToken.Location, stringValue, interpolationValues.ToArray());
            }
        }
    }
}
