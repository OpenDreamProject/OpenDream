using System;
using System.Collections.Generic;
using System.Text;

namespace DMCompiler.DM.Visitors {
    class DMVisitorPrint : DMASTVisitor {
        private int _indentLevel = 0;

        private void PrintIndents() {
            for (int i = 0; i < _indentLevel; i++) {
                Console.Write("\t");
            }
        }

        public void VisitAdd(DMASTAdd add) {
            Console.Write("(");
            add.A.Visit(this);
            Console.Write(" + ");
            add.B.Visit(this);
            Console.Write(")");
        }

        public void VisitAnd(DMASTAnd and) {
            Console.Write("(");
            and.A.Visit(this);
            Console.Write(" && ");
            and.B.Visit(this);
            Console.Write(")");
        }

        public void VisitAssign(DMASTAssign assign) {
            assign.Expression.Visit(this);
            Console.Write(" = ");
            assign.Value.Visit(this);
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            Console.Write("(");
            binaryAnd.A.Visit(this);
            Console.Write(" & ");
            binaryAnd.B.Visit(this);
            Console.Write(")");
        }

        public void VisitBlockInner(DMASTBlockInner block) {
            for (int i = 0; i < block.Statements.Length; i++) {
                PrintIndents();
                block.Statements[i].Visit(this);

                if (i < block.Statements.Length - 1) Console.Write("\n");
            }
        }

        public void VisitCallableDereference(DMASTCallableDereference dereference) {
            dereference.Left.Visit(this);
            Console.Write(".");
            dereference.Right.Visit(this);
        }

        public void VisitCallableIdentifier(DMASTCallableIdentifier identifier) {
            Console.Write(identifier.Identifier);
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            Console.Write("..");
        }

        public void VisitCallParameter(DMASTCallParameter callParameter) {
            if (callParameter.Name != null) {
                Console.Write(callParameter.Name + " = ");
            }

            callParameter.Value.Visit(this);
        }

        public void VisitComparisonEqual(DMASTComparisonEqual comparison) {
            Console.Write("(");
            comparison.A.Visit(this);
            Console.Write(" == ");
            comparison.B.Visit(this);
            Console.Write(")");
        }

        public void VisitComparisonNotEqual(DMASTComparisonNotEqual comparison) {
            Console.Write("(");
            comparison.A.Visit(this);
            Console.Write(" != ");
            comparison.B.Visit(this);
            Console.Write(")");
        }

        public void VisitConstantFloat(DMASTConstantFloat constant) {
            Console.Write(constant.Value.ToString());
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            Console.Write(constant.Value.ToString());
        }

        public void VisitConstantNull(DMASTConstantNull constant) {
            Console.Write("null");
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            constant.Value.Visit(this);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            Console.Write("\"" + constant.Value.ToString() + "\"");
        }

        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter) {
            definitionParameter.Path.Visit(this);

            if (definitionParameter.Value != null) {
                Console.Write(" = ");
                definitionParameter.Value.Visit(this);
            }
        }

        public void VisitExpressionNegate(DMASTExpressionNegate negate) {
            Console.Write("-");
            negate.Expression.Visit(this);
        }

        public void VisitExpressionNot(DMASTExpressionNot not) {
            Console.Write("!");
            not.Expression.Visit(this);
        }

        public void VisitFile(DMASTFile file) {
            file.BlockInner.Visit(this);
            Console.Write("\n");
        }

        public void VisitIdentifier(DMASTIdentifier identifier) {
            throw new NotImplementedException();
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            throw new NotImplementedException();
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            Console.Write("new ");
            newPath.Path.Visit(this);

            Console.Write("(");
            for (int i = 0; i < newPath.Parameters.Length; i++) {
                newPath.Parameters[i].Visit(this);

                if (i < newPath.Parameters.Length - 1) Console.Write(", ");
            }
            Console.Write(")");
        }

        public void VisitObject(DMASTObjectDefinition statement) {
            Console.Write("ObjectDefinition(");
            statement.Path.Visit(this);
            Console.Write(")");

            if (statement.InnerBlock != null) {
                Console.Write("\n");

                _indentLevel++;
                statement.InnerBlock.Visit(this);
                _indentLevel--;
            }
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) {
            Console.Write("VarDefinition(");
            objectVarDefinition.Path.Visit(this);
            Console.Write(" = ");
            objectVarDefinition.Value.Visit(this);
            Console.Write(")");
        }

        public void VisitPath(DMASTPath path) {
            switch (path.PathType) {
                case OpenDreamShared.Dream.DreamPath.PathType.Absolute: Console.Write("/"); break;
                case OpenDreamShared.Dream.DreamPath.PathType.DownwardSearch: Console.Write(":"); break;
                case OpenDreamShared.Dream.DreamPath.PathType.UpwardSearch: Console.Write("."); break;
            }

            for (int i = 0; i < path.PathElements.Length; i++) {
                path.PathElements[i].Visit(this);
                
                if (i < path.PathElements.Length - 1) Console.Write("/");
            }
        }

        public void VisitPathElement(DMASTPathElement pathElement) {
            Console.Write(pathElement.Element);
        }

        public void VisitProcBlockInner(DMASTProcBlockInner procBlock) {
            for (int i = 0; i < procBlock.Statements.Length; i++) {
                PrintIndents();
                procBlock.Statements[i].Visit(this);
                
                if (i < procBlock.Statements.Length - 1) Console.Write("\n");
            }
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            procCall.Callable.Visit(this);

            Console.Write("(");
            for (int i = 0; i < procCall.Parameters.Length; i++) {
                procCall.Parameters[i].Visit(this);

                if (i < procCall.Parameters.Length - 1) Console.Write(", ");
            }
            Console.Write(")");
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            Console.Write("ProcDefinition(");
            procDefinition.Path.Visit(this);

            Console.Write("(");
            for (int i = 0; i < procDefinition.Parameters.Length; i++) {
                procDefinition.Parameters[i].Visit(this);

                if (i < procDefinition.Parameters.Length - 1) Console.Write(", ");
            }
            Console.Write("))");

            if (procDefinition.Body != null) {
                Console.Write("\n");

                _indentLevel++;
                procDefinition.Body.Visit(this);
                _indentLevel--;
            }
        }

        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) {
            Console.Write("break");
        }

        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) {
            Console.Write("del ");
            statementDel.Value.Visit(this);
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) {
            statementExpression.Expression.Visit(this);
        }

        public void VisitProcStatementForList(DMASTProcStatementForList statementForList) {
            Console.Write("for (");

            if (statementForList.VariableDeclaration != null) {
                statementForList.VariableDeclaration.Visit(this);
            } else {
                statementForList.Variable.Visit(this);
            }

            Console.Write(" in ");
            statementForList.List.Visit(this);
            Console.Write(")\n");

            _indentLevel++;
            statementForList.Body.Visit(this);
            _indentLevel--;
        }

        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementIf(DMASTProcStatementIf statementIf) {
            Console.Write("if ");
            statementIf.Condition.Visit(this);
            Console.Write("\n");
            
            _indentLevel++;
            statementIf.Body.Visit(this);
            _indentLevel--;

            if (statementIf.ElseBody != null) {
                Console.Write("\n");
                PrintIndents();
                Console.Write("else\n");

                _indentLevel++;
                statementIf.ElseBody.Visit(this);
                _indentLevel--;
            }
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn) {
            Console.Write("return");

            if (statementReturn.Value != null) {
                Console.Write(" ");
                statementReturn.Value.Visit(this);
            }
        }

        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            Console.Write("var");
            if (varDeclaration.Type != null) varDeclaration.Type.Visit(this);
            Console.Write("/" + varDeclaration.Name);

            if (varDeclaration.Value != null) {
                Console.Write(" = ");
                varDeclaration.Value.Visit(this);
            }
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            Console.Write("(");
            subtract.A.Visit(this);
            Console.Write(" - ");
            subtract.B.Visit(this);
            Console.Write(")");
        }
    }
}