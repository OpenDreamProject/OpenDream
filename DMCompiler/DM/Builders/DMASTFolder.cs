using System;
using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.DM.Visitors;

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
            case DMASTLeftShift leftShift: {
                if (leftShift is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, lhsInt.Value << rhsInt.Value);
                }

                break;
            }
            case DMASTRightShift rightShift: {
                if (rightShift is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, lhsInt.Value >> rhsInt.Value);
                }

                break;
            }
            case DMASTBinaryAnd binaryAnd: {
                if (binaryAnd is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, lhsInt.Value & rhsInt.Value);
                }

                break;
            }
            case DMASTBinaryOr binaryOr: {
                if (binaryOr is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, lhsInt.Value | rhsInt.Value);
                }

                break;
            }
            case DMASTBinaryNot binaryNot: {
                if (binaryNot.Value is DMASTConstantInteger exprInt) {
                    return new DMASTConstantInteger(expression.Location, (~exprInt.Value) & 0xFFFFFF);
                }

                break;
            }
            case DMASTAdd add: {
                DMASTConstantInteger? lhsInt = add.LHS as DMASTConstantInteger;
                DMASTConstantFloat? lhsFloat = add.LHS as DMASTConstantFloat;
                DMASTConstantString? lhsString = add.LHS as DMASTConstantString;
                DMASTConstantInteger? rhsInt = add.RHS as DMASTConstantInteger;
                DMASTConstantFloat? rhsFloat = add.RHS as DMASTConstantFloat;
                DMASTConstantString? rhsString = add.RHS as DMASTConstantString;

                if (lhsInt != null && rhsInt != null) return new DMASTConstantInteger(expression.Location, lhsInt.Value + rhsInt.Value);
                if (lhsInt != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsInt.Value + rhsFloat.Value);
                if (lhsFloat != null && rhsInt != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value + rhsInt.Value);
                if (lhsFloat != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value + rhsFloat.Value);
                if (lhsString != null && rhsString != null) return new DMASTConstantString(expression.Location, lhsString.Value + rhsString.Value);

                break;
            }
            case DMASTSubtract subtract: {
                DMASTConstantInteger? lhsInt = subtract.LHS as DMASTConstantInteger;
                DMASTConstantFloat? lhsFloat = subtract.LHS as DMASTConstantFloat;
                DMASTConstantInteger? rhsInt = subtract.RHS as DMASTConstantInteger;
                DMASTConstantFloat? rhsFloat = subtract.RHS as DMASTConstantFloat;

                if (lhsInt != null && rhsInt != null) return new DMASTConstantInteger(expression.Location, lhsInt.Value - rhsInt.Value);
                if (lhsInt != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsInt.Value - rhsFloat.Value);
                if (lhsFloat != null && rhsInt != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value - rhsInt.Value);
                if (lhsFloat != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value - rhsFloat.Value);

                break;
            }
            case DMASTMultiply multiply: {
                DMASTConstantInteger? lhsInt = multiply.LHS as DMASTConstantInteger;
                DMASTConstantFloat? lhsFloat = multiply.LHS as DMASTConstantFloat;
                DMASTConstantInteger? rhsInt = multiply.RHS as DMASTConstantInteger;
                DMASTConstantFloat? rhsFloat = multiply.RHS as DMASTConstantFloat;

                if (lhsInt != null && rhsInt != null) return new DMASTConstantInteger(expression.Location, lhsInt.Value * rhsInt.Value);
                if (lhsInt != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsInt.Value * rhsFloat.Value);
                if (lhsFloat != null && rhsInt != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value * rhsInt.Value);
                if (lhsFloat != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value * rhsFloat.Value);

                break;
            }
            case DMASTDivide divide: {
                DMASTConstantInteger? lhsInt = divide.LHS as DMASTConstantInteger;
                DMASTConstantFloat? lhsFloat = divide.LHS as DMASTConstantFloat;
                DMASTConstantInteger? rhsInt = divide.RHS as DMASTConstantInteger;
                DMASTConstantFloat? rhsFloat = divide.RHS as DMASTConstantFloat;

                if (lhsInt != null && rhsInt != null) return new DMASTConstantFloat(expression.Location, lhsInt.Value / rhsInt.Value);
                if (lhsInt != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsInt.Value / rhsFloat.Value);
                if (lhsFloat != null && rhsInt != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value / rhsInt.Value);
                if (lhsFloat != null && rhsFloat != null) return new DMASTConstantFloat(expression.Location, lhsFloat.Value / rhsFloat.Value);

                break;
            }
            case DMASTModulus modulus: {
                if (modulus is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, lhsInt.Value % rhsInt.Value);
                }

                break;
            }
            case DMASTPower power: {
                if (power is { LHS: DMASTConstantInteger lhsInt, RHS: DMASTConstantInteger rhsInt }) {
                    return new DMASTConstantInteger(expression.Location, (int)Math.Pow(lhsInt.Value, rhsInt.Value));
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
