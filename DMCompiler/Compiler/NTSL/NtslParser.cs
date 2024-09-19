using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.Compiler.NTSL;

public class NtslParser(NtslLexer lexer) : Parser<Token>(lexer) {
    private Location CurrentLoc => Current().Location;

    private readonly HashSet<string> _usedVars = new();

    public NtslFile File() {
        var loc = CurrentLoc;
        var statements = new List<DMASTStatement>();

        _usedVars.Clear();
        while (Current().Type is not (TokenType.NTSL_EndFile or TokenType.EndOfFile)) {
            var procDef = ProcDefinition();
            if (procDef != null) {
                statements.Add(procDef);
                continue;
            }

            DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, $"Unexpected token {Current().Type}");
            break;
        }

        if (Current().Type != TokenType.NTSL_EndFile)
            DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected end of NTSL script");

        return new NtslFile(loc, statements, [.._usedVars]);
    }

    private DMASTProcDefinition? ProcDefinition() {
        if (Check(TokenType.NTSL_Def, out var defToken)) {
            if (!Check(TokenType.NTSL_Identifier, out var nameToken)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected an identifier to name the function");
                return null;
            }

            if (!Check(TokenType.NTSL_LeftParenthesis)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected a '(' for function arguments");
                return null;
            }

            if (!Check(TokenType.NTSL_RightParenthesis)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected a ')' to end function arguments");
                return null;
            }

            var bodyLoc = CurrentLoc;
            var statements = CodeBlock();
            if (statements == null) {
                DMCompiler.Emit(WarningCode.MissingBody, CurrentLoc, "Expected a function body");
                return null;
            }

            return new DMASTProcDefinition(defToken.Location, new DreamPath("/proc/" + nameToken.Text), [], new(bodyLoc, statements, null), null);
        }

        return null;
    }

    private DMASTProcStatement[]? CodeBlock() {
        if (!Check(TokenType.NTSL_LeftCurlyBracket))
            return null;

        List<DMASTProcStatement> statements = new();
        DMASTProcStatement? statement = Statement();
        while (statement != null) {
            statements.Add(statement);

            if (!Check(TokenType.NTSL_Semicolon)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected ';' to end statement");
                break;
            }

            statement = Statement();
        }

        if (!Check(TokenType.NTSL_RightCurlyBracket))
            DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected '}' to end code block");

        return statements.ToArray();
    }

    private DMASTProcStatement? Statement() {
        if (Check(TokenType.NTSL_Return, out var returnToken)) {
            var value = Expression();
            if (value == null) {
                DMCompiler.Emit(WarningCode.MissingExpression, CurrentLoc, "Expected a value to return");
                value = new DMASTInvalidExpression(CurrentLoc);
            }

            return new DMASTProcStatementReturn(returnToken.Location, value);
        }

        var expression = Expression();
        if (expression != null) {
            return new DMASTProcStatementExpression(expression.Location, expression);
        }

        return null;
    }

    private DMASTExpression? Expression() {
        return ExpressionAssign();
    }

    private DMASTExpression? ExpressionAssign() {
        var expression = ExpressionPrimary();

        if (expression != null && Check(TokenType.NTSL_Equals)) {
            var assignTo = ExpressionAssign();
            if (assignTo == null) {
                DMCompiler.Emit(WarningCode.BadExpression, CurrentLoc, "Expected a value to assign");
                return expression;
            }

            return new DMASTAssign(expression.Location, expression, assignTo);
        }

        return expression;
    }

    private DMASTExpression? ExpressionPrimary() {
        if (Check(TokenType.NTSL_String, out var stringToken))
            return new DMASTConstantString(stringToken.Location, stringToken.ValueAsString());
        if (Check(TokenType.NTSL_Number, out var numberToken))
            return new DMASTConstantFloat(numberToken.Location, (float)numberToken.Value!);

        if (Check(TokenType.NTSL_VarIdentifierPrefix, out var prefixToken)) {
            if (!Check(TokenType.NTSL_Identifier, out var varIdentifierToken)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected a var identifier");
                return new DMASTInvalidExpression(prefixToken.Location);
            }

            _usedVars.Add(varIdentifierToken.Text);
            return new DMASTIdentifier(prefixToken.Location, varIdentifierToken.Text);
        }

        if (Check(TokenType.NTSL_Identifier, out var identifierToken)) { // Identifier without a '$' prefix, refers to a function
            if (!Check(TokenType.NTSL_LeftParenthesis)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected function call arguments");
                return null;
            }

            List<DMASTCallParameter> arguments = new();
            DMASTExpression? argument = ExpressionPrimary();
            while (argument != null) {
                arguments.Add(new(argument.Location, argument));

                if (!Check(TokenType.NTSL_Comma))
                    break;

                argument = ExpressionPrimary();
            }

            if (!Check(TokenType.NTSL_RightParenthesis)) {
                DMCompiler.Emit(WarningCode.BadToken, CurrentLoc, "Expected ')' to end function call arguments");
                return null;
            }

            switch (identifierToken.Text) {
                case "vector":
                    return new DMASTList(identifierToken.Location, arguments.ToArray());
                case "at":
                    if (arguments.Count is not (2 or 3)) {
                        DMCompiler.Emit(WarningCode.BadExpression, identifierToken.Location,
                            "at() required 2 or 3 arguments");
                        return new DMASTInvalidExpression(identifierToken.Location);
                    }

                    var vector = arguments[0];
                    var index = arguments[1];
                    var deref = new DMASTDereference(identifierToken.Location, vector.Value, [
                        new DMASTDereference.IndexOperation {
                            Index = index.Value,
                            Location = index.Value.Location,
                            Safe = false
                        }
                    ]);

                    // Assigning the index
                    if (arguments.Count == 3) {
                        return new DMASTAssign(identifierToken.Location, deref, arguments[2].Value);
                    }

                    return deref;
                default:
                    return new DMASTProcCall(
                        identifierToken.Location,
                        new DMASTCallableProcIdentifier(identifierToken.Location, identifierToken.Text),
                        arguments.ToArray()
                    );
            }
        }

        return null;
    }
}

public sealed class NtslFile(Location loc, List<DMASTStatement> statements, HashSet<string> usedVars) : DMASTStatement(loc) {
    public readonly List<DMASTStatement> Statements = statements;
    public readonly HashSet<string> UsedVars = usedVars;
}
