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
        public void VisitProcStatementSet(DMASTProcStatementSet statementSet);
        public void VisitProcStatementIf(DMASTProcStatementIf statementIf);
        public void VisitProcStatementForList(DMASTProcStatementForList statementForList);
        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop);
        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile);
        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch);
        public void VisitProcDefinition(DMASTProcDefinition procDefinition);
        public void VisitIdentifier(DMASTIdentifier identifier);
        public void VisitConstantInteger(DMASTConstantInteger constant);
        public void VisitConstantFloat(DMASTConstantFloat constant);
        public void VisitConstantString(DMASTConstantString constant);
        public void VisitConstantResource(DMASTConstantResource constant);
        public void VisitConstantNull(DMASTConstantNull constant);
        public void VisitConstantPath(DMASTConstantPath constant);
        public void VisitAssign(DMASTAssign assign);
        public void VisitNewPath(DMASTNewPath newPath);
        public void VisitNewDereference(DMASTNewDereference newDereference);
        public void VisitExpressionNot(DMASTExpressionNot not);
        public void VisitExpressionNegate(DMASTExpressionNegate negate);
        public void VisitEqual(DMASTEqual equal);
        public void VisitNotEqual(DMASTNotEqual notEqual);
        public void VisitLessThan(DMASTLessThan lessThan);
        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual);
        public void VisitGreaterThan(DMASTGreaterThan greaterThan);
        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual);
        public void VisitMultiply(DMASTMultiply multiply);
        public void VisitDivide(DMASTDivide divide);
        public void VisitModulus(DMASTModulus modulus);
        public void VisitAdd(DMASTAdd add);
        public void VisitSubtract(DMASTSubtract subtract);
        public void VisitTernary(DMASTTernary ternary);
        public void VisitAppend(DMASTAppend append);
        public void VisitMask(DMASTMask mask);
        public void VisitOr(DMASTOr or);
        public void VisitAnd(DMASTAnd and);
        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd);
        public void VisitBinaryOr(DMASTBinaryOr binaryOr);
        public void VisitBinaryNot(DMASTBinaryNot binaryNot);
        public void VisitLeftShift(DMASTLeftShift leftShift);
        public void VisitRightShift(DMASTRightShift rightShift);
        public void VisitIn(DMASTExpressionIn expressionIn);
        public void VisitListIndex(DMASTListIndex listIndex);
        public void VisitProcCall(DMASTProcCall procCall);
        public void VisitCallParameter(DMASTCallParameter callParameter);
        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter);
        public void VisitCallableIdentifier(DMASTCallableIdentifier identifier);
        public void VisitCallableDereference(DMASTCallableDereference dereference);
        public void VisitCallableSuper(DMASTCallableSuper super);
        public void VisitCallableSelf(DMASTCallableSelf self);
    }

    interface DMASTNode : ASTNode<DMASTVisitor> {

    }

    interface DMASTStatement : DMASTNode {

    }

    interface DMASTProcStatement : DMASTNode {

    }

    interface DMASTExpression : DMASTNode {

    }

    interface DMASTExpressionConstant : DMASTExpression {

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

    class DMASTProcStatementSet : DMASTProcStatement {
        public string Property;
        public DMASTExpression Value;

        public DMASTProcStatementSet(string property, DMASTExpression value) {
            Property = property;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSet(this);
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

    class DMASTProcStatementWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementWhile(DMASTExpression conditional, DMASTProcBlockInner body) {
            Conditional = conditional;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementWhile(this);
        }
    }

    class DMASTProcStatementSwitch : DMASTProcStatement {
        public class SwitchCase {
            public DMASTProcBlockInner Body;

            protected SwitchCase(DMASTProcBlockInner body) {
                Body = body;
            }
        }

        public class SwitchCaseDefault : SwitchCase {
            public SwitchCaseDefault(DMASTProcBlockInner body) : base(body) { }
        }

        public class SwitchCaseValue : SwitchCase {
            public DMASTExpressionConstant Value;

            public SwitchCaseValue(DMASTExpressionConstant value, DMASTProcBlockInner body) : base(body) {
                Value = value;
            }
        }

        public DMASTExpression Value;
        public SwitchCase[] Cases;

        public DMASTProcStatementSwitch(DMASTExpression value, SwitchCase[] cases) {
            Value = value;
            Cases = cases;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSwitch(this);
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

    class DMASTConstantInteger : DMASTExpressionConstant {
        public int Value;

        public DMASTConstantInteger(int value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantInteger(this);
        }
    }

    class DMASTConstantFloat : DMASTExpressionConstant {
        public float Value;

        public DMASTConstantFloat(float value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantFloat(this);
        }
    }

    class DMASTConstantString : DMASTExpressionConstant {
        public string Value;

        public DMASTConstantString(string value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantString(this);
        }
    }

    class DMASTConstantResource : DMASTExpressionConstant {
        public string Path;

        public DMASTConstantResource(string path) {
            Path = path;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantResource(this);
        }
    }

    class DMASTConstantNull : DMASTExpressionConstant {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantNull(this);
        }
    }

    class DMASTConstantPath : DMASTExpressionConstant {
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

    class DMASTEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitEqual(this);
        }
    }

    class DMASTNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTNotEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitNotEqual(this);
        }
    }

    class DMASTLessThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThan(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThan(this);
        }
    }

    class DMASTLessThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThanOrEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThanOrEqual(this);
        }
    }

    class DMASTGreaterThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThan(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThan(this);
        }
    }

    class DMASTGreaterThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThanOrEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThanOrEqual(this);
        }
    }

    class DMASTMultiply : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiply(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitMultiply(this);
        }
    }

    class DMASTDivide : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivide(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitDivide(this);
        }
    }

    class DMASTModulus : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulus(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitModulus(this);
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

    class DMASTTernary : DMASTExpression {
        public DMASTExpression A, B, C;

        public DMASTTernary(DMASTExpression a, DMASTExpression b, DMASTExpression c) {
            A = a;
            B = b;
            C = c;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitTernary(this);
        }
    }

    class DMASTAppend : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAppend(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitAppend(this);
        }
    }

    class DMASTMask : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMask(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitMask(this);
        }
    }

    class DMASTOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTOr(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitOr(this);
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

    class DMASTBinaryOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryOr(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryOr(this);
        }
    }

    class DMASTBinaryNot : DMASTExpression {
        public DMASTExpression Value;

        public DMASTBinaryNot(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryNot(this);
        }
    }

    class DMASTLeftShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShift(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitLeftShift(this);
        }
    }

    class DMASTRightShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShift(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitRightShift(this);
        }
    }

    class DMASTExpressionIn : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression List;

        public DMASTExpressionIn(DMASTExpression value, DMASTExpression list) {
            Value = value;
            List = list;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitIn(this);
        }
    }

    class DMASTListIndex : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Index;

        public DMASTListIndex(DMASTExpression expression, DMASTExpression index) {
            Expression = expression;
            Index = index;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitListIndex(this);
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

    class DMASTCallParameter : DMASTNode {
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

    class DMASTDefinitionParameter : DMASTNode{
        public enum ParameterType {
            Anything,
            Text
        }

        public DMASTPath Path;
        public DMASTExpression Value;
        public ParameterType Type;

        public DMASTDefinitionParameter(DMASTPath path, DMASTExpression value, ParameterType type) {
            Path = path;
            Value = value;
            Type = type;
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

    class DMASTCallableSelf : DMASTCallable {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSelf(this);
        }
    }
}
