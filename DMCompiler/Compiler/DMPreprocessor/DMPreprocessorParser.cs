using DMCompiler.Compiler.DM;
using DMCompiler.DM;
using OpenDreamShared.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMCompiler.Compiler.DMPreprocessor {
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
    Primary ::= '(' Expression ')' | Constant
    Constant ::= Integer | Float | Null
    */

    /// <summary>
    /// An extremely simple parser that acts on Preproc tokens, <br/>
    /// held separate from DMParser because of slightly different behaviour and far simpler implementation.
    /// </summary>
    internal static class DMPreprocessorParser {
        private static List<Token> _tokens;
        private static int _tokenIndex = 0;

        public static DMExpression ExpressionFromTokens(List<Token> input) {
            _tokens = input;
            return DMExpression.Create(null, null, Expression());
        }

        private static void Advance() {
            ++_tokenIndex;
        }
        private static Token? Current() {
            if (_tokenIndex >= _tokens.Count())
                return null;
            return _tokens[_tokenIndex];
        }
        private static bool Check(TokenType type) {
            if (Current()?.Type == type) {
                Advance();
                return true;
            }
            return false;
        }
        private static bool Check(TokenType[] types) {
            foreach (TokenType type in types) {
                if (Current()?.Type == type) {
                    Advance();
                    return true;
                }
            }
            return false;
        }
        private static void Error(string msg) {
            DMCompiler.Error(new CompilerError(Current().Location, msg));
        }

        private static DMASTExpression Expression() {
            return ExpressionOr();
        }
        private static DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();
            if (a != null) {
                var loc = Current()?.Location;
                while (Check(TokenType.DM_BarBar)) {
                    DMASTExpression b = ExpressionAnd();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTOr(loc.Value, a, b);
                }
            }
            return a;
        }
        public static DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionComparison();
            if (a != null) {
                var loc = Current()?.Location;
                while (Check(TokenType.DM_AndAnd)) {
                    DMASTExpression b = ExpressionComparison();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTAnd(loc.Value, a, b);
                }
            }
            return a;
        }
        public static DMASTExpression ExpressionComparison() {
            DMASTExpression a = ExpressionComparisonLtGt();

            if (a != null) {
                Token token = Current();
                while (Check(DMParser.ComparisonTypes)) {
                    DMASTExpression b = ExpressionComparisonLtGt();
                    if (b == null) Error("Expected an expression to compare to");
                    switch (token.Type) {
                        case TokenType.DM_EqualsEquals: a = new DMASTEqual(token.Location, a, b); break;
                        case TokenType.DM_ExclamationEquals: a = new DMASTNotEqual(token.Location, a, b); break;
                        case TokenType.DM_TildeEquals: a = new DMASTEquivalent(token.Location, a, b); break;
                        case TokenType.DM_TildeExclamation: a = new DMASTNotEquivalent(token.Location, a, b); break;
                    }
                    token = Current();
                }
            }

            return a;
        }
        public static DMASTExpression ExpressionComparisonLtGt() {
            DMASTExpression a = ExpressionAdditionSubtraction();

            if (a != null) {
                Token token = Current();
                while (Check(DMParser.LtGtComparisonTypes)) {
                    DMASTExpression b = ExpressionAdditionSubtraction();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_LessThan: a = new DMASTLessThan(token.Location, a, b); break;
                        case TokenType.DM_LessThanEquals: a = new DMASTLessThanOrEqual(token.Location, a, b); break;
                        case TokenType.DM_GreaterThan: a = new DMASTGreaterThan(token.Location, a, b); break;
                        case TokenType.DM_GreaterThanEquals: a = new DMASTGreaterThanOrEqual(token.Location, a, b); break;
                    }
                    token = Current();
                }
            }

            return a;
        }
        public static DMASTExpression ExpressionAdditionSubtraction() {
            DMASTExpression a = ExpressionMultiplicationDivisionModulus();

            if (a != null) {
                Token token = Current();
                while (Check(DMParser.PlusMinusTypes)) {
                    DMASTExpression b = ExpressionMultiplicationDivisionModulus();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Plus: a = new DMASTAdd(token.Location, a, b); break;
                        case TokenType.DM_Minus: a = new DMASTSubtract(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }
        public static DMASTExpression ExpressionMultiplicationDivisionModulus() {
            DMASTExpression a = ExpressionPower();

            if (a != null) {
                Token token = Current();
                while (Check(DMParser.MulDivModTypes)) {
                    DMASTExpression b = ExpressionPower();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Star: a = new DMASTMultiply(token.Location, a, b); break;
                        case TokenType.DM_Slash: a = new DMASTDivide(token.Location, a, b); break;
                        case TokenType.DM_Modulus: a = new DMASTModulus(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }
        public static DMASTExpression ExpressionPower() {
            DMASTExpression a = ExpressionUnary();

            if (a != null) {
                var loc = Current()?.Location;
                while (Check(TokenType.DM_StarStar)) {
                    DMASTExpression b = ExpressionUnary();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTPower(loc.Value, a, b);
                }
            }

            return a;
        }
        public static DMASTExpression ExpressionUnary() {
            var loc = Current()?.Location;
            if (Check(TokenType.DM_Exclamation)) {
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTNot(loc.Value, expression);
            }
            return ExpressionSign();
        }
        public static DMASTExpression ExpressionSign() {
            Token token = Current();

            if (Check(DMParser.PlusMinusTypes)) {
                DMASTExpression expression = ExpressionSign();

                if (expression == null) Error("Expected an expression");
                if (token?.Type == TokenType.DM_Minus) {
                    switch (expression) {
                        case DMASTConstantInteger integer: {
                            int value = integer.Value;

                            return new DMASTConstantInteger(token.Location, -value);
                        }
                        case DMASTConstantFloat constantFloat: {
                            float value = constantFloat.Value;

                            return new DMASTConstantFloat(token.Location, -value);
                        }
                        default:
                            return new DMASTNegate(token.Location, expression);
                    }
                } else {
                    return expression;
                }
            }

            return ExpressionPrimary();
        }
        public static DMASTExpression ExpressionPrimary() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                DMASTExpression inner = Expression();
                if (!Check(TokenType.DM_RightParenthesis)) {
                    Error("Expected ')' to close expression");
                }
                return inner;
            }

            return Constant();
        }
        public static DMASTExpression Constant() {
            Token constantToken = Current();

            switch (constantToken.Type) {
                case TokenType.DM_Integer: Advance(); return new DMASTConstantInteger(constantToken.Location, (int)constantToken.Value);
                case TokenType.DM_Float: Advance(); return new DMASTConstantFloat(constantToken.Location, (float)constantToken.Value);
                    
                default: {
                    Error($"Token not accepted in preprocessor expression: {constantToken.PrintableText}");
                    return new DMASTConstantInteger(constantToken.Location, 0); // Lets just be false, I guess.
                }
            }
        }
    }
}
