using System.Linq;
using DMCompiler.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    interface DMASTVisitor {
        public void VisitFile(DMASTFile file);
        public void VisitBlockInner(DMASTBlockInner block);
        public void VisitProcBlockInner(DMASTProcBlockInner procBlock);
        public void VisitObject(DMASTObjectDefinition statement);
        public void VisitPath(DMASTPath path);
        public void VisitPathElement(DMASTPathElement pathElement);
        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition);
        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression);
        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration);
        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn);
        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak);
        public void VisitProcStatementDel(DMASTProcStatementDel statementDel);
        public void VisitProcStatementIf(DMASTProcStatementIf statementIf);
        public void VisitProcStatementForList(DMASTProcStatementForList statementForList);
        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop);
        public void VisitProcDefinition(DMASTProcDefinition procDefinition);
        public void VisitIdentifier(DMASTIdentifier identifier);
        public void VisitConstantInteger(DMASTConstantInteger constant);
        public void VisitConstantFloat(DMASTConstantFloat constant);
        public void VisitConstantString(DMASTConstantString constant);
        public void VisitConstantNull(DMASTConstantNull constant);
        public void VisitConstantPath(DMASTConstantPath constant);
        public void VisitAssign(DMASTAssign assign);
        public void VisitNewPath(DMASTNewPath newPath);
        public void VisitNewDereference(DMASTNewDereference newDereference);
        public void VisitExpressionNot(DMASTExpressionNot not);
        public void VisitExpressionNegate(DMASTExpressionNegate negate);
        public void VisitComparisonEqual(DMASTComparisonEqual comparison);
        public void VisitComparisonNotEqual(DMASTComparisonNotEqual comparison);
        public void VisitAdd(DMASTAdd add);
        public void VisitSubtract(DMASTSubtract subtract);
        public void VisitAnd(DMASTAnd and);
        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd);
        public void VisitProcCall(DMASTProcCall procCall);
        public void VisitCallParameter(DMASTCallParameter callParameter);
        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter);
        public void VisitCallableIdentifier(DMASTCallableIdentifier identifier);
        public void VisitCallableDereference(DMASTCallableDereference dereference);
        public void VisitCallableSuper(DMASTCallableSuper super);
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

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitFile(this);
        }
    }

    class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(DMASTStatement[] statements) {
            Statements = statements;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitBlockInner(this);
        }
    }

    class DMASTProcBlockInner : DMASTNode {
        public DMASTProcStatement[] Statements;

        public DMASTProcBlockInner(DMASTProcStatement[] statements) {
            Statements = statements;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcBlockInner(this);
        }
    }

    class DMASTObjectDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTBlockInner InnerBlock;

        public DMASTObjectDefinition(DMASTPath path, DMASTBlockInner innerBlock) {
            Path = path;
            InnerBlock = innerBlock;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitObject(this);
        }
    }

    class DMASTProcDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTDefinitionParameter[] Parameters;
        public DMASTProcBlockInner Body;

        public DMASTProcDefinition(DMASTPath path, DMASTDefinitionParameter[] parameters, DMASTProcBlockInner body) {
            Path = path;
            Parameters = parameters;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcDefinition(this);
        }
    }

    class DMASTPath : DMASTNode {
        public DreamPath.PathType PathType;
        public DMASTPathElement[] PathElements;

        public DMASTPath(DreamPath.PathType pathType, DMASTPathElement[] pathElements) {
            PathType = pathType;
            PathElements = pathElements;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitPath(this);
        }
    }

    class DMASTPathElement : DMASTNode {
        public string Element;

        public DMASTPathElement(string element) {
            Element = element;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitPathElement(this);
        }
    }

    class DMASTObjectVarDefinition : DMASTStatement {
        public DMASTPath Path;
        public DMASTExpression Value;

        public DMASTObjectVarDefinition(DMASTPath path, DMASTExpression value) {
            Path = path;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectVarDefinition(this);
        }
    }

    class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementExpression(this);
        }
    }

    class DMASTProcStatementVarDeclaration : DMASTProcStatement {
        public DMASTPath Type;
        public string Name;
        public DMASTExpression Value;

        public DMASTProcStatementVarDeclaration(DMASTPath path, DMASTExpression value) {
            DMASTPathElement[] typeElements = path.PathElements.Take(path.PathElements.Length - 1).ToArray();

            Type = (typeElements.Length > 0) ? new DMASTPath(path.PathType, typeElements) : null;
            Name = path.PathElements[path.PathElements.Length - 1].Element;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementVarDeclaration(this);
        }
    }

    class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementReturn(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementReturn(this);
        }
    }

    class DMASTProcStatementBreak : DMASTProcStatement {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBreak(this);
        }
    }

    class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementDel(this);
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

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementIf(this);
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

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForList(this);
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

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForLoop(this);
        }
    }

    class DMASTIdentifier : DMASTExpression {
        public DMASTCallable Identifier;

        public DMASTIdentifier(DMASTCallable identifier) {
            Identifier = identifier;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitIdentifier(this);
        }
    }

    class DMASTConstantInteger : DMASTExpression {
        public int Value;

        public DMASTConstantInteger(int value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantInteger(this);
        }
    }

    class DMASTConstantFloat : DMASTExpression {
        public float Value;

        public DMASTConstantFloat(float value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantFloat(this);
        }
    }

    class DMASTConstantString : DMASTExpression {
        public string Value;

        public DMASTConstantString(string value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantString(this);
        }
    }

    class DMASTConstantNull : DMASTExpression {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantNull(this);
        }
    }

    class DMASTConstantPath : DMASTExpression {
        public DMASTPath Value;

        public DMASTConstantPath(DMASTPath value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantPath(this);
        }
    }

    class DMASTAssign : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssign(DMASTExpression expression, DMASTExpression value) {
            Expression = expression;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitAssign(this);
        }
    }

    class DMASTNewPath : DMASTExpression {
        public DMASTPath Path;
        public DMASTCallParameter[] Parameters;

        public DMASTNewPath(DMASTPath path, DMASTCallParameter[] parameters) {
            Path = path;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitNewPath(this);
        }
    }

    class DMASTNewDereference : DMASTExpression {
        public DMASTCallableDereference Dereference;
        public DMASTCallParameter[] Parameters;

        public DMASTNewDereference(DMASTCallableDereference dereference, DMASTCallParameter[] parameters) {
            Dereference = dereference;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitNewDereference(this);
        }
    }

    class DMASTExpressionNot : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTExpressionNot(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitExpressionNot(this);
        }
    }

    class DMASTExpressionNegate : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTExpressionNegate(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitExpressionNegate(this);
        }
    }

    class DMASTComparisonEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTComparisonEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitComparisonEqual(this);
        }
    }

    class DMASTComparisonNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTComparisonNotEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitComparisonNotEqual(this);
        }
    }

    class DMASTAdd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAdd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitAdd(this);
        }
    }

    class DMASTSubtract : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTSubtract(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitSubtract(this);
        }
    }

    class DMASTAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAnd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitAnd(this);
        }
    }

    class DMASTBinaryAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryAnd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryAnd(this);
        }
    }

    class DMASTProcCall : DMASTExpression {
        public DMASTCallable Callable;
        public DMASTCallParameter[] Parameters;

        public DMASTProcCall(DMASTCallable callable, DMASTCallParameter[] parameters) {
            Callable = callable;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcCall(this);
        }
    }

    class DMASTCallParameter : DMASTExpression {
        public DMASTExpression Value;
        public string Name;

        public DMASTCallParameter(DMASTExpression value, string name = null) {
            Value = value;
            Name = name;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCallParameter(this);
        }
    }

    class DMASTDefinitionParameter : DMASTExpression {
        public DMASTPath Path;
        public DMASTExpression Value;

        public DMASTDefinitionParameter(DMASTPath path, DMASTExpression value = null) {
            Path = path;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitDefinitionParameter(this);
        }
    }

    class DMASTCallableIdentifier : DMASTCallable {
        public string Identifier;

        public DMASTCallableIdentifier(string identifier) {
            Identifier = identifier;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableIdentifier(this);
        }
    }

    class DMASTCallableDereference : DMASTCallable {
        public DMASTExpression Left, Right;

        public DMASTCallableDereference(DMASTExpression left, DMASTExpression right) {
            Left = left;
            Right = right;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableDereference(this);
        }
    }
    
    class DMASTCallableSuper : DMASTCallable {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSuper(this);
        }
    }
}
