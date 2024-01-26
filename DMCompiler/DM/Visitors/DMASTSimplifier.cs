using DMCompiler.Compiler.DM;
using DMCompiler.DM.Expressions;
using System;

namespace DMCompiler.DM.Visitors {
    public sealed class DMASTSimplifier : DMASTVisitor {
        public void SimplifyAST(DMASTNode ast) {
            ast.Visit(this);
        }

        public void VisitFile(DMASTFile dmFile) {
            dmFile.BlockInner.Visit(this);
        }

        #region Objects
        public void VisitObjectDefinition(DMASTObjectDefinition statement) {
            statement.InnerBlock?.Visit(this);
        }

        public void VisitBlockInner(DMASTBlockInner blockInner) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                statement.Visit(this);
            }
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) {
            SimplifyExpression(ref objectVarDefinition.Value);
        }

        public void VisitMultipleObjectVarDefinitions(DMASTMultipleObjectVarDefinitions multipleObjectVarDefinitions) {
            foreach (DMASTObjectVarDefinition varDefinition in multipleObjectVarDefinitions.VarDefinitions) {
                varDefinition.Visit(this);
            }
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride objectVarOverride) {
            SimplifyExpression(ref objectVarOverride.Value);
        }
        #endregion Objects

        #region Procs
        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                SimplifyExpression(ref parameter.Value);
            }

            procDefinition.Body?.Visit(this);
        }

        public void VisitProcBlockInner(DMASTProcBlockInner procBlockInner) {
            foreach (DMASTProcStatement statement in procBlockInner.Statements) {
                statement.Visit(this);
            }
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) {
            SimplifyExpression(ref statementExpression.Expression);
        }

        public void VisitProcStatementIf(DMASTProcStatementIf statementIf) {
            SimplifyExpression(ref statementIf.Condition);

            statementIf.Body?.Visit(this);
            statementIf.ElseBody?.Visit(this);
        }

        public void VisitProcStatementFor(DMASTProcStatementFor statementFor) {
            if (statementFor.Expression1 != null) SimplifyExpression(ref statementFor.Expression1);
            if (statementFor.Expression2 != null) SimplifyExpression(ref statementFor.Expression2);
            if (statementFor.Expression3 != null) SimplifyExpression(ref statementFor.Expression3);
            statementFor.Body?.Visit(this);
        }

        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) {
            SimplifyExpression(ref statementWhile.Conditional);

            statementWhile.Body?.Visit(this);
        }

        public void VisitProcStatementInfLoop(DMASTProcStatementInfLoop statementInfLoop){
            statementInfLoop.Body?.Visit(this);
        }

        public void VisitProcStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) {
            SimplifyExpression(ref statementDoWhile.Conditional);

            statementDoWhile.Body?.Visit(this);
        }

        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            SimplifyExpression(ref statementSwitch.Value);

            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                    for (var i = 0; i < switchCaseValues.Values.Length; i++) {
                        SimplifyExpression(ref switchCaseValues.Values[i]);
                    }
                }
                switchCase.Body?.Visit(this);
            }
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn) {
            SimplifyExpression(ref statementReturn.Value);
        }

        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) {

        }

        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) {

        }

        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) {

        }

        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) {
            SimplifyExpression(ref statementDel.Value);
        }

        public void VisitProcStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            SimplifyExpression(ref statementSpawn.Delay);
            statementSpawn.Body.Visit(this);
        }

        public void VisitProcStatementGoto(DMASTProcStatementGoto statementGoto) {

        }

        public void VisitProcStatementLabel(DMASTProcStatementLabel statementLabel) {

        }

        public void VisitProcStatementBrowse(DMASTProcStatementBrowse statementBrowse) {
            SimplifyExpression(ref statementBrowse.Receiver);
            SimplifyExpression(ref statementBrowse.Body);
            SimplifyExpression(ref statementBrowse.Options);
        }

        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            SimplifyExpression(ref statementBrowseResource.Receiver);
            SimplifyExpression(ref statementBrowseResource.File);
            SimplifyExpression(ref statementBrowseResource.Filename);
        }

        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            SimplifyExpression(ref statementOutputControl.Receiver);
            SimplifyExpression(ref statementOutputControl.Message);
            SimplifyExpression(ref statementOutputControl.Control);
        }

        public void VisitProcStatementOutput(DMASTProcStatementOutput statementOutput) {
            SimplifyExpression(ref statementOutput.A);
            SimplifyExpression(ref statementOutput.B);
        }

        public void VisitProcStatementFtp(DMASTProcStatementFtp statementFtp) {
            SimplifyExpression(ref statementFtp.Receiver);
            SimplifyExpression(ref statementFtp.File);
            SimplifyExpression(ref statementFtp.Name);
        }

        public void VisitProcStatementInput(DMASTProcStatementInput statementInput) {
            SimplifyExpression(ref statementInput.A);
            SimplifyExpression(ref statementInput.B);
        }

        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            SimplifyExpression(ref varDeclaration.Value);
        }

        public void VisitProcStatementTryCatch(DMASTProcStatementTryCatch tryCatch) {
            tryCatch.TryBody.Visit(this);
            tryCatch.CatchBody?.Visit(this);
        }

        public void VisitProcStatementThrow(DMASTProcStatementThrow statementThrow) {
            SimplifyExpression(ref statementThrow.Value);
        }
        #endregion Procs

        private void SimplifyExpression(ref DMASTExpression expression) {
            if (expression == null || expression is DMASTExpressionConstant || expression is DMASTCallable) return;

            if (expression is DMASTExpressionWrapped wrapped) {
                SimplifyExpression(ref wrapped.Expression);
                return;
            }

            #region Comparators
            DMASTEqual equal = expression as DMASTEqual;
            if (equal != null) {
                SimplifyExpression(ref equal.A);
                SimplifyExpression(ref equal.B);

                return;
            }

            DMASTNotEqual notEqual = expression as DMASTNotEqual;
            if (notEqual != null) {
                SimplifyExpression(ref notEqual.A);
                SimplifyExpression(ref notEqual.B);

                return;
            }

            DMASTLessThan lessThan = expression as DMASTLessThan;
            if (lessThan != null) {
                SimplifyExpression(ref lessThan.A);
                SimplifyExpression(ref lessThan.B);

                return;
            }

            DMASTLessThanOrEqual lessThanOrEqual = expression as DMASTLessThanOrEqual;
            if (lessThanOrEqual != null) {
                SimplifyExpression(ref lessThanOrEqual.A);
                SimplifyExpression(ref lessThanOrEqual.B);

                return;
            }

            DMASTGreaterThan greaterThan = expression as DMASTGreaterThan;
            if (greaterThan != null) {
                SimplifyExpression(ref greaterThan.A);
                SimplifyExpression(ref greaterThan.B);

                return;
            }

            DMASTGreaterThanOrEqual greaterThanOrEqual = expression as DMASTGreaterThanOrEqual;
            if (greaterThanOrEqual != null) {
                SimplifyExpression(ref greaterThanOrEqual.A);
                SimplifyExpression(ref greaterThanOrEqual.B);

                return;
            }

            DMASTExpressionInRange inRange = expression as DMASTExpressionInRange;
            if (inRange != null) {
                SimplifyExpression(ref inRange.Value);
                SimplifyExpression(ref inRange.StartRange);
                SimplifyExpression(ref inRange.EndRange);

                return;
            }
            #endregion Comparators

            #region Math
            DMASTNegate negate = expression as DMASTNegate;
            if (negate != null) {
                SimplifyExpression(ref negate.Expression);
                if (negate.Expression is not DMASTExpressionConstant) return;

                switch (negate.Expression) {
                    case DMASTConstantInteger exprInteger: expression = new DMASTConstantInteger(expression.Location, -exprInteger.Value); break;
                    case DMASTConstantFloat exprFloat: expression = new DMASTConstantFloat(expression.Location, -exprFloat.Value); break;
                }

                return;
            }

            DMASTNot not = expression as DMASTNot;
            if (not != null) {
                SimplifyExpression(ref not.Expression);
                if (not.Expression is not DMASTExpressionConstant) return;

                DMASTConstantInteger exprInteger = not.Expression as DMASTConstantInteger;
                DMASTConstantFloat exprFloat = not.Expression as DMASTConstantFloat;

                if (exprInteger != null) expression = new DMASTConstantInteger(expression.Location, (exprInteger.Value != 0) ? 1 : 0);
                else if (exprFloat != null) expression = new DMASTConstantFloat(expression.Location, (exprFloat.Value != 0) ? 1 : 0);

                return;
            }

            DMASTOr or = expression as DMASTOr;
            if (or != null) {
                SimplifyExpression(ref or.A);
                SimplifyExpression(ref or.B);
                if (SimpleTruth(or.A) == true) {
                    expression = or.A;
                    return;
                }
                if (or.A is not DMASTExpressionConstant || or.B is not DMASTExpressionConstant) return;
                DMASTConstantInteger aInteger = or.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = or.B as DMASTConstantInteger;
                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, ((aInteger.Value != 0) || (bInteger.Value != 0)) ? bInteger.Value : 0);

                return;
            }

            DMASTAnd and = expression as DMASTAnd;
            if (and != null) {
                SimplifyExpression(ref and.A);
                SimplifyExpression(ref and.B);
                if (SimpleTruth(and.A) == false) {
                    expression = and.A;
                    return;
                }
                if (and.A is not DMASTExpressionConstant || and.B is not DMASTExpressionConstant) return;
                DMASTConstantInteger aInteger = and.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = and.B as DMASTConstantInteger;
                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, ((aInteger.Value != 0) && (bInteger.Value != 0)) ? bInteger.Value : 0);

                return;
            }

            DMASTLeftShift leftShift = expression as DMASTLeftShift;
            if (leftShift != null) {
                SimplifyExpression(ref leftShift.A);
                SimplifyExpression(ref leftShift.B);
                if (leftShift.A is not DMASTExpressionConstant || leftShift.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = leftShift.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = leftShift.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value << bInteger.Value);

                return;
            }

            DMASTRightShift rightShift = expression as DMASTRightShift;
            if (rightShift != null) {
                SimplifyExpression(ref rightShift.A);
                SimplifyExpression(ref rightShift.B);
                if (rightShift.A is not DMASTExpressionConstant || rightShift.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = rightShift.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = rightShift.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value >> bInteger.Value);

                return;
            }

            DMASTBinaryAnd binaryAnd = expression as DMASTBinaryAnd;
            if (binaryAnd != null) {
                SimplifyExpression(ref binaryAnd.A);
                SimplifyExpression(ref binaryAnd.B);
                if (binaryAnd.A is not DMASTExpressionConstant || binaryAnd.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = binaryAnd.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = binaryAnd.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value & bInteger.Value);

                return;
            }

            DMASTBinaryOr binaryOr = expression as DMASTBinaryOr;
            if (binaryOr != null) {
                SimplifyExpression(ref binaryOr.A);
                SimplifyExpression(ref binaryOr.B);
                if (binaryOr.A is not DMASTExpressionConstant || binaryOr.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = binaryOr.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = binaryOr.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value | bInteger.Value);

                return;
            }

            DMASTBinaryNot binaryNot = expression as DMASTBinaryNot;
            if (binaryNot != null) {
                SimplifyExpression(ref binaryNot.Value);
                if (binaryNot.Value is not DMASTExpressionConstant) return;

                DMASTConstantInteger valueInteger = binaryNot.Value as DMASTConstantInteger;

                if (valueInteger != null) expression = new DMASTConstantInteger(expression.Location, (~valueInteger.Value) & 0xFFFFFF);

                return;
            }

            DMASTAdd add = expression as DMASTAdd;
            if (add != null) {
                SimplifyExpression(ref add.A);
                SimplifyExpression(ref add.B);
                if (add.A is not DMASTExpressionConstant || add.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = add.A as DMASTConstantInteger;
                DMASTConstantFloat aFloat = add.A as DMASTConstantFloat;
                DMASTConstantString aString = add.A as DMASTConstantString;
                DMASTConstantInteger bInteger = add.B as DMASTConstantInteger;
                DMASTConstantFloat bFloat = add.B as DMASTConstantFloat;
                DMASTConstantString bString = add.B as DMASTConstantString;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value + bInteger.Value);
                else if (aInteger != null && bFloat != null) expression = new DMASTConstantFloat(expression.Location, aInteger.Value + bFloat.Value);
                else if (aFloat != null && bInteger != null) expression = new DMASTConstantFloat(expression.Location, aFloat.Value + bInteger.Value);
                else if (aFloat != null && bFloat != null) expression = new DMASTConstantFloat(expression.Location, aFloat.Value + bFloat.Value);
                else if (aString != null && bString != null) expression = new DMASTConstantString(expression.Location, aString.Value + bString.Value);

                return;
            }

            DMASTSubtract subtract = expression as DMASTSubtract;
            if (subtract != null) {
                SimplifyExpression(ref subtract.A);
                SimplifyExpression(ref subtract.B);
                if (subtract.A is not DMASTExpressionConstant || subtract.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = subtract.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = subtract.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value - bInteger.Value);

                return;
            }

            DMASTMultiply multiply = expression as DMASTMultiply;
            if (multiply != null) {
                SimplifyExpression(ref multiply.A);
                SimplifyExpression(ref multiply.B);
                if (multiply.A is not DMASTExpressionConstant || multiply.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = multiply.A as DMASTConstantInteger;
                DMASTConstantFloat aFloat = multiply.A as DMASTConstantFloat;
                DMASTConstantInteger bInteger = multiply.B as DMASTConstantInteger;
                DMASTConstantFloat bFloat = multiply.B as DMASTConstantFloat;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value * bInteger.Value);
                else if (aInteger != null && bFloat != null) expression = new DMASTConstantFloat(expression.Location, aInteger.Value * bFloat.Value);
                else if (aFloat != null && bInteger != null) expression = new DMASTConstantFloat(expression.Location, aFloat.Value * bInteger.Value);
                else if (aFloat != null && bFloat != null) expression = new DMASTConstantFloat(expression.Location, aFloat.Value * bFloat.Value);

                return;
            }

            DMASTDivide divide = expression as DMASTDivide;
            if (divide != null) {
                SimplifyExpression(ref divide.A);
                SimplifyExpression(ref divide.B);
                if (divide.A is not DMASTExpressionConstant || divide.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = divide.A as DMASTConstantInteger;
                DMASTConstantFloat aFloat = divide.A as DMASTConstantFloat;
                DMASTConstantInteger bInteger = divide.B as DMASTConstantInteger;
                DMASTConstantFloat bFloat = divide.B as DMASTConstantFloat;

                if (aInteger != null && bInteger != null) {
                    if (aInteger.Value % bInteger.Value == 0) expression = new DMASTConstantFloat(expression.Location, aInteger.Value / bInteger.Value);
                    else expression = new DMASTConstantFloat(expression.Location, (float)aInteger.Value / (float)bInteger.Value);
                } else if (aFloat != null && bInteger != null) {
                    expression = new DMASTConstantFloat(expression.Location, aFloat.Value / bInteger.Value);
                } else if (aFloat != null && bFloat != null) {
                    expression = new DMASTConstantFloat(expression.Location, aFloat.Value / bFloat.Value);
                }

                return;
            }

            DMASTModulus modulus = expression as DMASTModulus;
            if (modulus != null) {
                SimplifyExpression(ref modulus.A);
                SimplifyExpression(ref modulus.B);
                if (modulus.A is not DMASTExpressionConstant || modulus.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = modulus.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = modulus.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, aInteger.Value % bInteger.Value);

                return;
            }

            DMASTPower power = expression as DMASTPower;
            if (power != null) {
                SimplifyExpression(ref power.A);
                SimplifyExpression(ref power.B);
                if (power.A is not DMASTExpressionConstant || power.B is not DMASTExpressionConstant) return;

                DMASTConstantInteger aInteger = power.A as DMASTConstantInteger;
                DMASTConstantInteger bInteger = power.B as DMASTConstantInteger;

                if (aInteger != null && bInteger != null) expression = new DMASTConstantInteger(expression.Location, (int)MathF.Pow(aInteger.Value, bInteger.Value));

                return;
            }

            DMASTAppend append = expression as DMASTAppend;
            if (append != null) {
                SimplifyExpression(ref append.A);
                SimplifyExpression(ref append.B);

                return;
            }

            DMASTRemove remove = expression as DMASTRemove;
            if (remove != null) {
                SimplifyExpression(ref remove.A);
                SimplifyExpression(ref remove.B);

                return;
            }

            DMASTCombine combine = expression as DMASTCombine;
            if (combine != null) {
                SimplifyExpression(ref combine.A);
                SimplifyExpression(ref combine.B);

                return;
            }

            DMASTMask mask = expression as DMASTMask;
            if (mask != null) {
                SimplifyExpression(ref mask.A);
                SimplifyExpression(ref mask.B);

                return;
            }
            #endregion Math

            #region Others
            DMASTList list = expression as DMASTList;
            if (list != null) {
                foreach (DMASTCallParameter parameter in list.Values) {
                    SimplifyExpression(ref parameter.Value);
                }

                return;
            }

            DMASTAddText addtext = expression as DMASTAddText;
            if(addtext != null) {
                foreach (DMASTCallParameter parameter in addtext.Parameters)
                {
                    SimplifyExpression(ref parameter.Value);
                }

                return;
            }

            DMASTNewPath newPath = expression as DMASTNewPath;
            if (newPath != null) {
                if (newPath.Parameters != null) {
                    foreach (DMASTCallParameter parameter in newPath.Parameters) {
                        SimplifyExpression(ref parameter.Value);
                    }
                }

                return;
            }

            DMASTNewExpr newExpr = expression as DMASTNewExpr;
            if (newExpr != null) {
                SimplifyExpression(ref newExpr.Expression);

                if (newExpr.Parameters != null) {
                    foreach (DMASTCallParameter parameter in newExpr.Parameters) {
                        SimplifyExpression(ref parameter.Value);
                    }
                }
            }

            if (expression is DMASTDereference deref) {
                SimplifyExpression(ref deref.Expression);

                foreach (var operation in deref.Operations) {
                    switch (operation) {
                        case DMASTDereference.IndexOperation indexOperation:
                            SimplifyExpression(ref indexOperation.Index);
                            break;
                        case DMASTDereference.CallOperation callOperation:
                            foreach (var param in callOperation.Parameters) {
                                SimplifyExpression(ref param.Value);
                            }
                            break;
                    }
                }
            }

            DMASTProcCall procCall = expression as DMASTProcCall;
            if (procCall != null) {
                foreach (DMASTCallParameter parameter in procCall.Parameters) {
                    SimplifyExpression(ref parameter.Value);
                }

                return;
            }

            DMASTAssign assign = expression as DMASTAssign;
            if (assign != null) {
                SimplifyExpression(ref assign.Expression);
                SimplifyExpression(ref assign.Value);

                return;
            }

            DMASTStringFormat stringFormat = expression as DMASTStringFormat;
            if (stringFormat != null) {
                for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                    DMASTExpression simplifiedValue = stringFormat.InterpolatedValues[i];

                    SimplifyExpression(ref simplifiedValue);
                    stringFormat.InterpolatedValues[i] = simplifiedValue;
                }

                return;
            }
            if (expression is DMASTSwitchCaseRange switchCaseRange) {
                SimplifyExpression(ref switchCaseRange.RangeStart);
                SimplifyExpression(ref switchCaseRange.RangeEnd);
                return;
            }
            #endregion Others
        }

        bool? SimpleTruth(DMASTExpression expr) {
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
}
