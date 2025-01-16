using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.DM.Builders;

// Takes in an AST node and attempts to fold what mathematical expressions it can
// TODO: Constant folding should instead be done by either expression or bytecode generation
public class DMASTFolder {
    public void FoldAst(DMASTNode? ast) {
        if (ast == null)
            return;

        switch (ast) {
            case DMASTFile file: FoldAst(file.BlockInner); break;
            case DMASTObjectDefinition { InnerBlock: not null } objectDef: FoldAst(objectDef.InnerBlock); break;
            case DMASTObjectVarDefinition objectVarDef: objectVarDef.Value = FoldExpression(objectVarDef.Value); break;
            case DMASTObjectVarOverride objectVarOverride: objectVarOverride.Value = FoldExpression(objectVarOverride.Value); break;
            case DMASTProcStatementExpression procExpr: procExpr.Expression = FoldExpression(procExpr.Expression); break;
            case DMASTProcStatementReturn procRet: procRet.Value = FoldExpression(procRet.Value); break;
            case DMASTProcStatementDel procDel: procDel.Value = FoldExpression(procDel.Value); break;
            case DMASTProcStatementThrow procThrow: procThrow.Value = FoldExpression(procThrow.Value); break;
            case DMASTProcStatementVarDeclaration procVarDecl: procVarDecl.Value = FoldExpression(procVarDecl.Value); break;
            case DMASTMultipleObjectVarDefinitions objectVarDefs:
                foreach (DMASTObjectVarDefinition varDefinition in objectVarDefs.VarDefinitions) {
                    FoldAst(varDefinition);
                }

                break;
            case DMASTAggregate<DMASTProcStatementVarDeclaration> procVarDecls:
                foreach (DMASTProcStatementVarDeclaration varDefinition in procVarDecls.Statements) {
                    FoldAst(varDefinition);
                }

                break;
            case DMASTBlockInner blockInner:
                foreach (DMASTStatement statement in blockInner.Statements) {
                    FoldAst(statement);
                }

                break;
            case DMASTProcBlockInner procBlockInner:
                foreach (DMASTProcStatement statement in procBlockInner.Statements) {
                    FoldAst(statement);
                }

                break;
            case DMASTProcDefinition procDef:
                foreach (DMASTDefinitionParameter parameter in procDef.Parameters) {
                    parameter.Value = FoldExpression(parameter.Value);
                }

                FoldAst(procDef.Body);
                break;
            case DMASTProcStatementIf statementIf:
                statementIf.Condition = FoldExpression(statementIf.Condition);
                FoldAst(statementIf.Body);
                FoldAst(statementIf.ElseBody);

                break;
            case DMASTProcStatementFor statementFor:
                statementFor.Expression1 = FoldExpression(statementFor.Expression1);
                statementFor.Expression2 = FoldExpression(statementFor.Expression2);
                statementFor.Expression3 = FoldExpression(statementFor.Expression3);
                FoldAst(statementFor.Body);

                break;
            case DMASTProcStatementWhile statementWhile:
                statementWhile.Conditional = FoldExpression(statementWhile.Conditional);
                FoldAst(statementWhile.Body);

                break;
            case DMASTProcStatementDoWhile statementDoWhile:
                statementDoWhile.Conditional = FoldExpression(statementDoWhile.Conditional);
                FoldAst(statementDoWhile.Body);

                break;
            case DMASTProcStatementInfLoop statementInfLoop:
                FoldAst(statementInfLoop.Body);

                break;
            case DMASTProcStatementSwitch statementSwitch:
                statementSwitch.Value = FoldExpression(statementSwitch.Value);

                foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                    if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                        for (var i = 0; i < switchCaseValues.Values.Length; i++) {
                            switchCaseValues.Values[i] = FoldExpression(switchCaseValues.Values[i]);
                        }
                    }

                    FoldAst(switchCase.Body);
                }

                break;
            case DMASTProcStatementSpawn statementSpawn:
                statementSpawn.Delay = FoldExpression(statementSpawn.Delay);
                FoldAst(statementSpawn.Body);

                break;
            case DMASTProcStatementBrowse statementBrowse:
                statementBrowse.Receiver = FoldExpression(statementBrowse.Receiver);
                statementBrowse.Body = FoldExpression(statementBrowse.Body);
                statementBrowse.Options = FoldExpression(statementBrowse.Options);

                break;
            case DMASTProcStatementBrowseResource statementBrowseResource:
                statementBrowseResource.Receiver = FoldExpression(statementBrowseResource.Receiver);
                statementBrowseResource.File = FoldExpression(statementBrowseResource.File);
                statementBrowseResource.Filename = FoldExpression(statementBrowseResource.Filename);

                break;
            case DMASTProcStatementOutputControl statementOutputControl:
                statementOutputControl.Receiver = FoldExpression(statementOutputControl.Receiver);
                statementOutputControl.Message = FoldExpression(statementOutputControl.Message);
                statementOutputControl.Control = FoldExpression(statementOutputControl.Control);

                break;
            case DMASTProcStatementTryCatch tryCatch:
                FoldAst(tryCatch.TryBody);
                FoldAst(tryCatch.CatchBody);

                break;
        }
    }

    private DMASTExpression FoldExpression(DMASTExpression? expression) {
        if (expression is DMASTUnary unary) {
            unary.Value = FoldExpression(unary.Value);
        } else if (expression is DMASTBinary binary) {
            binary.LHS = FoldExpression(binary.LHS);
            binary.RHS = FoldExpression(binary.RHS);
        }

        switch (expression) {
            case DMASTExpressionInRange inRange:
                inRange.Value = FoldExpression(inRange.Value);
                inRange.StartRange = FoldExpression(inRange.StartRange);
                inRange.EndRange = FoldExpression(inRange.EndRange);
                break;
            case DMASTSwitchCaseRange switchCaseRange:
                switchCaseRange.RangeStart = FoldExpression(switchCaseRange.RangeStart);
                switchCaseRange.RangeEnd = FoldExpression(switchCaseRange.RangeEnd);
                break;
            case DMASTList list:
                foreach (DMASTCallParameter parameter in list.Values) {
                    parameter.Value = FoldExpression(parameter.Value);
                }

                break;
            case DMASTAddText addText:
                foreach (DMASTCallParameter parameter in addText.Parameters) {
                    parameter.Value = FoldExpression(parameter.Value);
                }

                break;
            case DMASTNewPath newPath:
                if (newPath.Parameters != null) {
                    foreach (DMASTCallParameter parameter in newPath.Parameters) {
                        parameter.Value = FoldExpression(parameter.Value);
                    }
                }

                break;
            case DMASTNewExpr newExpr:
                if (newExpr.Parameters != null) {
                    foreach (DMASTCallParameter parameter in newExpr.Parameters) {
                        parameter.Value = FoldExpression(parameter.Value);
                    }
                }

                break;
            case DMASTProcCall procCall:
                foreach (DMASTCallParameter parameter in procCall.Parameters) {
                    parameter.Value = FoldExpression(parameter.Value);
                }

                break;
            case DMASTStringFormat stringFormat:
                for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                    stringFormat.InterpolatedValues[i] = FoldExpression(stringFormat.InterpolatedValues[i]);
                }

                break;

            #region Math

            case DMASTNegate negate:
                switch (negate.Value) {
                    case DMASTConstantInteger exprInteger: negate.Value = new DMASTConstantInteger(expression.Location, -exprInteger.Value); break;
                    case DMASTConstantFloat exprFloat: negate.Value = new DMASTConstantFloat(expression.Location, -exprFloat.Value); break;
                }

                break;
            case DMASTNot not:
                switch (not.Value) {
                    case DMASTConstantInteger exprInteger: not.Value = new DMASTConstantInteger(expression.Location, (exprInteger.Value != 0) ? 1 : 0); break;
                    case DMASTConstantFloat exprFloat: not.Value = new DMASTConstantInteger(expression.Location, (exprFloat.Value != 0) ? 1 : 0); break;
                }

                break;
            case DMASTOr or: {
                bool? simpleTruth = SimpleTruth(or.LHS);

                if (simpleTruth == true) {
                    return or.LHS;
                } else if (simpleTruth == false) {
                    return or.RHS;
                }

                break;
            }
            case DMASTAnd and: {
                bool? simpleTruth = SimpleTruth(and.LHS);

                if (simpleTruth == false) {
                    return and.LHS;
                } else if (simpleTruth == true) {
                    return and.RHS;
                }

                break;
            }

            #endregion Math

            default:
                break;
        }

        return expression;
    }

    private static bool? SimpleTruth(DMASTExpression expr) {
        switch (expr) {
            case DMASTConstantInteger e: return e.Value != 0;
            case DMASTConstantFloat e: return e.Value != 0;
            case DMASTConstantString e: return e.Value.Length != 0;
            case DMASTConstantNull: return false;
            case DMASTConstantPath: return true;
            case DMASTConstantResource: return true;
            default: return null;
        }
    }
}
