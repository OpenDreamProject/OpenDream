using OpenDreamShared.Dream;
using System.Linq;

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
        public object VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration);
        public object VisitProcStatementReturn(DMASTProcStatementReturn statementReturn);
        public object VisitProcStatementBreak(DMASTProcStatementBreak statementBreak);
        public object VisitProcStatementDel(DMASTProcStatementDel statementDel);
        public object VisitProcStatementIf(DMASTProcStatementIf statementIf);
        public object VisitProcStatementForList(DMASTProcStatementForList statementForList);
        public object VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop);
        public object VisitProcDefinition(DMASTProcDefinition procDefinition);
        public object VisitIdentifier(DMASTIdentifier identifier);
        public object VisitConstantInteger(DMASTConstantInteger constant);
        public object VisitConstantFloat(DMASTConstantFloat constant);
        public object VisitConstantString(DMASTConstantString constant);
        public object VisitConstantNull(DMASTConstantNull constant);
        public object VisitConstantPath(DMASTConstantPath constant);
        public object VisitAssign(DMASTAssign assign);
        public object VisitNewPath(DMASTNewPath newPath);
        public object VisitNewDereference(DMASTNewDereference newDereference);
        public object VisitExpressionNot(DMASTExpressionNot not);
        public object VisitExpressionNegate(DMASTExpressionNegate negate);
        public object VisitComparisonEqual(DMASTComparisonEqual comparison);
        public object VisitComparisonNotEqual(DMASTComparisonNotEqual comparison);
        public object VisitAnd(DMASTAnd and);
        public object VisitProcCall(DMASTProcCall procCall);
        public object VisitCallParameter(DMASTCallParameter callParameter);
        public object VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter);
        public object VisitCallableIdentifier(DMASTCallableIdentifier identifier);
        public object VisitCallableDereference(DMASTCallableDereference dereference);
        public object VisitCallableSuper(DMASTCallableSuper super);
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

    class DMASTProcStatementVarDeclaration : DMASTProcStatement {
        public DMASTPath Type;
        public string Name;
        public DMASTExpression Value;

        public DMASTProcStatementVarDeclaration(DMASTPath path, DMASTExpression value) {
            DMASTPathElement[] typeElements = path.PathElements.Take(path.PathElements.Length - 1).ToArray();

            Type = new DMASTPath(path.PathType, typeElements);
            Name = path.PathElements[path.PathElements.Length - 1].Element;
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementVarDeclaration(this);
        }
    }

    class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementReturn(DMASTExpression value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementReturn(this);
        }
    }

    class DMASTProcStatementBreak : DMASTProcStatement {
        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementBreak(this);
        }
    }

    class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(DMASTExpression value) {
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementDel(this);
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

    class DMASTProcStatementForList : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration VariableDeclaration;
        public DMASTCallable Variable;
        public DMASTExpression List;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementForList(DMASTProcStatementVarDeclaration variableDeclaration, DMASTCallable variable, DMASTExpression list, DMASTProcBlockInner body) {
            VariableDeclaration = variableDeclaration;
            Variable = variable;
            List = list;
            Body = body;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementForList(this);
        }
    }

    class DMASTProcStatementForLoop : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration VariableDeclaration;
        public DMASTCallable Variable;
        public DMASTExpression Condition, Incrementer;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementForLoop(DMASTProcStatementVarDeclaration variableDeclaration, DMASTCallable variable, DMASTExpression condition, DMASTExpression incrementer, DMASTProcBlockInner body) {
            VariableDeclaration = variableDeclaration;
            Variable = variable;
            Condition = condition;
            Incrementer = incrementer;
            Body = body;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitProcStatementForLoop(this);
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

    class DMASTAssign : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssign(DMASTExpression expression, DMASTExpression value) {
            Expression = expression;
            Value = value;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitAssign(this);
        }
    }

    class DMASTNewPath : DMASTExpression {
        public DMASTPath Path;
        public DMASTCallParameter[] Parameters;

        public DMASTNewPath(DMASTPath path, DMASTCallParameter[] parameters) {
            Path = path;
            Parameters = parameters;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitNewPath(this);
        }
    }

    class DMASTNewDereference : DMASTExpression {
        public DMASTCallableDereference Dereference;
        public DMASTCallParameter[] Parameters;

        public DMASTNewDereference(DMASTCallableDereference dereference, DMASTCallParameter[] parameters) {
            Dereference = dereference;
            Parameters = parameters;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitNewDereference(this);
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

    class DMASTComparisonEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTComparisonEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitComparisonEqual(this);
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

    class DMASTAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAnd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitAnd(this);
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
        public DMASTExpression Left, Right;

        public DMASTCallableDereference(DMASTExpression left, DMASTExpression right) {
            Left = left;
            Right = right;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitCallableDereference(this);
        }
    }
    
    class DMASTCallableSuper : DMASTCallable {
        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitCallableSuper(this);
        }
    }
}
