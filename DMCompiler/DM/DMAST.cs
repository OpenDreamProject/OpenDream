using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    interface DMASTVisitor {
        public object VisitFile(DMASTFile file);
        public object VisitBlockInner(DMASTBlockInner block);
        public object VisitProcBlockInner(DMASTProcBlockInner procBlock);
        public object VisitObject(DMASTObjectDefinition statement);
        public object VisitPath(DMASTPath path);
        public object VisitPathElement(DMASTPathElement pathElement);
        public object VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition);
        public object VisitProcStatementExpression(DMASTProcStatementExpression statementExpression);
        public object VisitProcStatementAssign(DMASTProcStatementAssign statementAssign);
        public object VisitProcStatementIf(DMASTProcStatementIf statementIf);
        public object VisitProcDefinition(DMASTProcDefinition procDefinition);
        public object VisitIdentifier(DMASTIdentifier identifier);
        public object VisitConstantInteger(DMASTConstantInteger constant);
        public object VisitConstantFloat(DMASTConstantFloat constant);
        public object VisitConstantString(DMASTConstantString constant);
        public object VisitConstantNull(DMASTConstantNull constant);
        public object VisitConstantPath(DMASTConstantPath constant);
        public object VisitExpressionNot(DMASTExpressionNot not);
        public object VisitExpressionNegate(DMASTExpressionNegate negate);
        public object VisitComparisonNotEqual(DMASTComparisonNotEqual comparison);
        public object VisitProcCall(DMASTProcCall procCall);
        public object VisitCallParameter(DMASTCallParameter callParameter);
        public object VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter);
        public object VisitCallableIdentifier(DMASTCallableIdentifier identifier);
        public object VisitCallableDereference(DMASTCallableDereference dereference);
    }

    interface DMASTNode : ASTNode<DMASTVisitor> {
        
    }

    interface DMASTStatement : DMASTNode {

    }

    interface DMASTProcStatement : DMASTNode {

    }

    interface DMASTExpression : DMASTNode {

    }

    interface DMASTCallable : DMASTExpression {

    }

    class DMASTFile : DMASTNode {
        public DMASTBlockInner BlockInner;

        public DMASTFile(DMASTBlockInner blockInner) {
            BlockInner = blockInner;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitFile(this);
        }
    }

    class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(DMASTStatement[] statements) {
            Statements = statements;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitBlockInner(this);
        }
    }

    class DMASTProcBlockInner : DMASTNode {
        public DMASTProcStatement[] Statements;

        public DMASTProcBlockInner(DMASTProcStatement[] statements) {
            Statements = statements;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcBlockInner(this);
        }
    }

    class DMASTObjectDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTBlockInner InnerBlock;

        public DMASTObjectDefinition(DMASTPath path, DMASTBlockInner innerBlock) {
            Path = path;
            InnerBlock = innerBlock;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitObject(this);
        }
    }

    class DMASTProcDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTDefinitionParameter[] Parameters;

        public DMASTProcDefinition(DMASTPath path, DMASTDefinitionParameter[] parameters) {
            Path = path;
            Parameters = parameters;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcDefinition(this);
        }
    }

    class DMASTPath : DMASTNode {
        public DreamPath.PathType PathType;
        public DMASTPathElement[] PathElements;

        public DMASTPath(DreamPath.PathType pathType, DMASTPathElement[] pathElements) {
            PathType = pathType;
            PathElements = pathElements;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitPath(this);
        }
    }

    class DMASTPathElement : DMASTNode {
        public string Element;

        public DMASTPathElement(string element) {
            Element = element;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitPathElement(this);
        }
    }

    class DMASTObjectVarDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTExpression Value;

        public DMASTObjectVarDefinition(DMASTPath path, DMASTExpression value) {
            Path = path;
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitObjectVarDefinition(this);
        }
    }

    class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(DMASTExpression expression) {
            Expression = expression;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementExpression(this);
        }
    }

    class DMASTProcStatementAssign : DMASTProcStatement {
        public DMASTExpression Expression, Value;

        public DMASTProcStatementAssign(DMASTExpression expression, DMASTExpression value) {
            Expression = expression;
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementAssign(this);
        }
    }

    class DMASTProcStatementIf : DMASTProcStatement {
        public DMASTExpression Condition;
        public DMASTProcBlockInner Body;
        public DMASTProcBlockInner ElseBody;

        public DMASTProcStatementIf(DMASTExpression condition, DMASTProcBlockInner body, DMASTProcBlockInner elseBody = null) {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementIf(this);
        }
    }

    class DMASTIdentifier : DMASTExpression {
        public DMASTCallable Identifier;

        public DMASTIdentifier(DMASTCallable identifier) {
            Identifier = identifier;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitIdentifier(this);
        }
    }

    class DMASTConstantInteger : DMASTExpression {
        public int Value;

        public DMASTConstantInteger(int value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitConstantInteger(this);
        }
    }

    class DMASTConstantFloat : DMASTExpression {
        public float Value;

        public DMASTConstantFloat(float value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitConstantFloat(this);
        }
    }

    class DMASTConstantString : DMASTExpression {
        public string Value;

        public DMASTConstantString(string value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitConstantString(this);
        }
    }

    class DMASTConstantNull : DMASTExpression {
        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitConstantNull(this);
        }
    }

    class DMASTConstantPath : DMASTExpression {
        public DMASTPath Value;

        public DMASTConstantPath(DMASTPath value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitConstantPath(this);
        }
    }

    class DMASTExpressionNot : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTExpressionNot(DMASTExpression expression) {
            Expression = expression;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitExpressionNot(this);
        }
    }

    class DMASTExpressionNegate : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTExpressionNegate(DMASTExpression expression) {
            Expression = expression;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitExpressionNegate(this);
        }
    }

    class DMASTComparisonNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTComparisonNotEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitComparisonNotEqual(this);
        }
    }

    class DMASTProcCall : DMASTExpression {
        public DMASTCallable Callable;
        public DMASTCallParameter[] Parameters;

        public DMASTProcCall(DMASTCallable callable, DMASTCallParameter[] parameters) {
            Callable = callable;
            Parameters = parameters;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcCall(this);
        }
    }

    class DMASTCallParameter : DMASTExpression {
        public DMASTExpression Value;
        public string Name;

        public DMASTCallParameter(DMASTExpression value, string name = null) {
            Value = value;
            Name = name;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitCallParameter(this);
        }
    }

    class DMASTDefinitionParameter : DMASTExpression {
        public DMASTPath Path;
        public DMASTExpression Value;

        public DMASTDefinitionParameter(DMASTPath path, DMASTExpression value = null) {
            Path = path;
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitDefinitionParameter(this);
        }
    }

    class DMASTCallableIdentifier : DMASTCallable {
        public string Identifier;

        public DMASTCallableIdentifier(string identifier) {
            Identifier = identifier;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitCallableIdentifier(this);
        }
    }

    class DMASTCallableDereference : DMASTCallable {
        DMASTExpression Left, Right;

        public DMASTCallableDereference(DMASTExpression left, DMASTExpression right) {
            Left = left;
            Right = right;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitCallableDereference(this);
        }
    }
}
