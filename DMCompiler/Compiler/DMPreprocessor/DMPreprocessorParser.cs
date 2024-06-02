using DMCompiler.Compiler.DM;
using System;
using System.Collections.Generic;
using System.IO;

namespace DMCompiler.Compiler.DMPreprocessor;

/*
 * NOTE: Some operations are (as in BYOND) not implemented for preproc expressions.
 * This includes ternary, bitwise, and assignment.
Grammar: (As usual in extended BNF, {A} means 0 or more As, and [A] means an optional A. Brace characters are escaped with single-quotes.)
Expression ::= AndExp ['||' AndExp]
AndExp ::= CompExp ['&&' CompExp]
CompExp ::= LtGtExp [('==' | '!=') LtGtExp]
LtGtExp ::= AddSubExp [(‘<’ | ‘<=’ | ‘>’ | ‘>=’ | ‘==’ | ‘!=’) AddSubExp]
AddSubExp ::= MultExp [('+' | '-') MultExp]
MultExp ::= PowerExp [('*' | '/' | '%') PowerExp]
PowerExp ::= UnaryExp [** PowerExp]
UnaryExp ::= {!}SignExp
SignExp ::= [-]Primary
Primary ::= '(' Expression ')' | DefinedExp | Constant
DefinedExp ::= 'defined(' MacroIdentifier ')'
Constant ::= Integer | Float | Null
*/

/// <summary>
/// An extremely simple parser that acts on a sliver of tokens that have been DM-lexed for evaluation in a preprocessor directive, <br/>
/// held separate from DMParser because of slightly different behaviour, far simpler implementation, and (<see langword="TODO"/>) possible statelessness.
/// </summary>
internal static class DMPreprocessorParser {
    private static List<Token>? _tokens;
    private static Dictionary<string, DMMacro>? _defines;
    private static int _tokenIndex;
    private static readonly float DegenerateValue = 0.0f;

    /// <returns>A float, because that is the only possible thing a well-formed preproc expression can evaluate to.</returns>
    public static float? ExpressionFromTokens(List<Token> input, Dictionary<string, DMMacro> defines) {
        _tokens = input;
        _defines = defines;
        var ret = Expression();
        _tokens = null;
        _defines = null;
        _tokenIndex = 0;
        return ret;
    }

    private static void Advance() {
        ++_tokenIndex;
    }

    private static Token Current() {
        if (_tokenIndex >= _tokens!.Count)
            return new Token(TokenType.EndOfFile, "\0", Location.Unknown, null);

        return _tokens[_tokenIndex];
    }

    private static bool Check(TokenType type) {
        if (Current().Type == type) {
            Advance();
            return true;
        }

        return false;
    }

    private static bool Check(TokenType[] types) {
        foreach (TokenType type in types) {
            if (Current().Type == type) {
                Advance();
                return true;
            }
        }

        return false;
    }

    private static void Error(string msg) {
        DMCompiler.Emit(WarningCode.BadDirective, Current().Location, msg);
    }

    private static float? Expression() {
        return ExpressionOr();
    }

    private static float? ExpressionOr() {
        float? a = ExpressionAnd();
        if (a is null) return a;

        while (Check(TokenType.DM_BarBar)) {
            float? b = ExpressionAnd();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            if (a != 0f || b != 0f)
                a = 1.0f;
            else
                a = 0.0f;
        }

        return a;
    }

    private static float? ExpressionAnd() {
        float? a = ExpressionComparison();
        if (a is null) return a;

        while (Check(TokenType.DM_AndAnd)) {
            float? b = ExpressionComparison();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            if (a != 0f && b != 0f)
                a = 1.0f;
            else
                a = 0.0f;
        }

        return a;
    }

    private static float? ExpressionComparison() {
        float? a = ExpressionComparisonLtGt();
        if (a is null) return a;
        for (Token token = Current(); Check(DMParser.ComparisonTypes); token = Current()) {
            float? b = ExpressionComparisonLtGt();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            switch (token.Type) {
                case TokenType.DM_EqualsEquals:
                    a = (a == b ? 1.0f : 0.0f);
                    break;
                case TokenType.DM_ExclamationEquals:
                    a = (a != b ? 1.0f : 0.0f);
                    break;
                case TokenType.DM_TildeEquals:
                    Error("'~=' is not valid in preprocessor expressions");
                    break;
                case TokenType.DM_TildeExclamation:
                    Error("'~!' is not valid in preprocessor expressions");
                    break;
            }
        }

        return a;
    }

    private static float? ExpressionComparisonLtGt() {
        float? a = ExpressionAdditionSubtraction();
        if (a is null) return a;
        for (Token token = Current(); Check(DMParser.LtGtComparisonTypes); token = Current()) {
            float? b = ExpressionAdditionSubtraction();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            switch (token.Type) {
                case TokenType.DM_LessThan:
                    a = (a < b ? 1.0f : 0.0f);
                    break;
                case TokenType.DM_LessThanEquals:
                    a = (a <= b ? 1.0f : 0.0f);
                    break;
                case TokenType.DM_GreaterThan:
                    a = (a > b ? 1.0f : 0.0f);
                    break;
                case TokenType.DM_GreaterThanEquals:
                    a = (a >= b ? 1.0f : 0.0f);
                    break;
            }
        }

        return a;
    }

    private static float? ExpressionAdditionSubtraction() {
        float? a = ExpressionMultiplicationDivisionModulus();
        if (a is null) return a;
        for (Token token = Current(); Check(DMParser.PlusMinusTypes); token = Current()) {
            float? b = ExpressionMultiplicationDivisionModulus();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            switch (token.Type) {
                case TokenType.DM_Plus:
                    a += b;
                    break;
                case TokenType.DM_Minus:
                    a -= b;
                    break;
            }
        }

        return a;
    }

    private static float? ExpressionMultiplicationDivisionModulus() {
        float? a = ExpressionPower();
        if (a is null) return a;
        for (Token token = Current(); Check(DMParser.MulDivModTypes); token = Current()) {
            float? b = ExpressionPower();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            switch (token.Type) {
                case TokenType.DM_Star:
                    a *= b;
                    break;
                case TokenType.DM_Slash:
                    a /= b;
                    break;
                case TokenType.DM_Modulus:
                    a %= b;
                    break;
            }
        }

        return a;
    }

    private static float? ExpressionPower() {
        float? a = ExpressionUnary();
        if (a is null) return a;

        while (Check(TokenType.DM_StarStar)) {
            float? b = ExpressionPower();
            if (b is null) {
                Error("Expected a second value");
                break;
            }

            a = MathF.Pow(a.Value, b.Value);
        }

        return a;
    }

    private static float? ExpressionUnary() {
        if (Check(TokenType.DM_Exclamation)) {
            float? expression = ExpressionUnary();
            if (expression == null) {
                Error("Expected an expression");
                return null;
            }

            return expression == 0.0f ? 1.0f : 0.0f;
        }

        return ExpressionSign();
    }

    private static float? ExpressionSign() {
        Token token = Current();

        if (Check(DMParser.PlusMinusTypes)) {
            float? expression = ExpressionSign();

            if (expression is null) {
                Error("Expected an expression");
                return null;
            }

            if (token.Type == TokenType.DM_Minus)
                expression = -expression;
            return expression;
        }

        return ExpressionPrimary();
    }

    private static float? ExpressionPrimary() {
        Token token = Current();
        switch (token.Type) {
            case TokenType.DM_LeftParenthesis:
                Advance();
                float? inner = Expression();
                if (!Check(TokenType.DM_RightParenthesis)) {
                    Error("Expected ')' to close expression");
                }

                return inner;
            case TokenType.DM_Identifier:
                if (token.Text == "defined") {
                    Advance();
                    if (!Check(TokenType.DM_LeftParenthesis)) {
                        Error("Expected '(' to begin defined() expression");
                        return DegenerateValue;
                    }

                    Token definedInner = Current();

                    if (definedInner.Type != TokenType.DM_Identifier) {
                        Error($"Unexpected token {definedInner.PrintableText} - identifier expected");
                        return DegenerateValue;
                    }

                    Advance();
                    if (!Check(TokenType.DM_RightParenthesis)) {
                        DMCompiler.Emit(WarningCode.DefinedMissingParen, token.Location,
                            "Expected ')' to end defined() expression");
                        //Electing to not return a degenerate value here since "defined(x" actually isn't an ambiguous grammar; we can figure out what they meant.
                    }

                    return _defines!.ContainsKey(definedInner.Text) ? 1.0f : 0.0f;
                } else if (token.Text == "fexists") {
                    Advance();
                    if (!Check(TokenType.DM_LeftParenthesis)) {
                        Error("Expected '(' to begin fexists() expression");
                        return DegenerateValue;
                    }

                    Token fexistsInner = Current();

                    if (fexistsInner.Type != TokenType.DM_ConstantString) {
                        Error($"Unexpected token {fexistsInner.PrintableText} - file path expected");
                        return DegenerateValue;
                    }

                    Advance();
                    if (!Check(TokenType.DM_RightParenthesis)) {
                        DMCompiler.Emit(WarningCode.DefinedMissingParen, token.Location,
                            "Expected ')' to end fexists() expression");
                    }

                    var filePath = Path.GetRelativePath(".", fexistsInner.Value!.ToString().Replace('\\', '/'));

                    var outputDir = Path.GetDirectoryName(DMCompiler.Settings.Files?[0]) ?? "/";
                    if (string.IsNullOrEmpty(outputDir))
                        outputDir = "./";

                    filePath = Path.Combine(outputDir, filePath);

                    return File.Exists(filePath) ? 1.0f : 0.0f;
                }

                Error($"Unexpected identifier {token.PrintableText} in preprocessor expression");
                return DegenerateValue;
            default:
                return Constant();
        }
    }

    private static float? Constant() {
        Token constantToken = Current();

        switch (constantToken.Type) {
            case TokenType.DM_Integer:
                Advance();
                return (float)((int)constantToken.Value);
            case TokenType.DM_Float:
                Advance();
                return (float)constantToken.Value;
            case TokenType.DM_ConstantString: {
                Advance();
                Error("Strings are not valid in preprocessor expressions. Did you mean to use a define() here?");
                return DegenerateValue;
            }

            default: {
                Error($"Token not accepted in preprocessor expression: {constantToken.PrintableText}");
                return DegenerateValue;
            }
        }
    }
}
