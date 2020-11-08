using System;
using DMCompiler.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    interface DMASTVisitor {
        public void VisitFile(DMASTFile file) { throw new NotImplementedException(); }
        public void VisitBlockInner(DMASTBlockInner block) { throw new NotImplementedException(); }
        public void VisitProcBlockInner(DMASTProcBlockInner procBlock) { throw new NotImplementedException(); }
        public void VisitObjectDefinition(DMASTObjectDefinition statement) { throw new NotImplementedException(); }
        public void VisitPath(DMASTPath path) { throw new NotImplementedException(); }
        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) { throw new NotImplementedException(); }
        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) { throw new NotImplementedException(); }
        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) { throw new NotImplementedException(); }
        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn) { throw new NotImplementedException(); }
        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) { throw new NotImplementedException(); }
        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) { throw new NotImplementedException(); }
        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) { throw new NotImplementedException(); }
        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) { throw new NotImplementedException(); }
        public void VisitProcStatementSpawn(DMASTProcStatementSpawn statementSpawn) { throw new NotImplementedException(); }
        public void VisitProcStatementIf(DMASTProcStatementIf statementIf) { throw new NotImplementedException(); }
        public void VisitProcStatementForList(DMASTProcStatementForList statementForList) { throw new NotImplementedException(); }
        public void VisitProcStatementForNumberRange(DMASTProcStatementForNumberRange statementForNumberRange) { throw new NotImplementedException(); }
        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop) { throw new NotImplementedException(); }
        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) { throw new NotImplementedException(); }
        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) { throw new NotImplementedException(); }
        public void VisitProcDefinition(DMASTProcDefinition procDefinition) { throw new NotImplementedException(); }
        public void VisitIdentifier(DMASTIdentifier identifier) { throw new NotImplementedException(); }
        public void VisitConstantInteger(DMASTConstantInteger constant) { throw new NotImplementedException(); }
        public void VisitConstantFloat(DMASTConstantFloat constant) { throw new NotImplementedException(); }
        public void VisitConstantString(DMASTConstantString constant) { throw new NotImplementedException(); }
        public void VisitConstantResource(DMASTConstantResource constant) { throw new NotImplementedException(); }
        public void VisitConstantNull(DMASTConstantNull constant) { throw new NotImplementedException(); }
        public void VisitConstantPath(DMASTConstantPath constant) { throw new NotImplementedException(); }
        public void VisitCall(DMASTCall call) { throw new NotImplementedException(); }
        public void VisitAssign(DMASTAssign assign) { throw new NotImplementedException(); }
        public void VisitNewPath(DMASTNewPath newPath) { throw new NotImplementedException(); }
        public void VisitNewDereference(DMASTNewDereference newDereference) { throw new NotImplementedException(); }
        public void VisitNewInferred(DMASTNewInferred newInferred) { throw new NotImplementedException(); }
        public void VisitExpressionNot(DMASTExpressionNot not) { throw new NotImplementedException(); }
        public void VisitExpressionNegate(DMASTExpressionNegate negate) { throw new NotImplementedException(); }
        public void VisitEqual(DMASTEqual equal) { throw new NotImplementedException(); }
        public void VisitNotEqual(DMASTNotEqual notEqual) { throw new NotImplementedException(); }
        public void VisitLessThan(DMASTLessThan lessThan) { throw new NotImplementedException(); }
        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) { throw new NotImplementedException(); }
        public void VisitGreaterThan(DMASTGreaterThan greaterThan) { throw new NotImplementedException(); }
        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) { throw new NotImplementedException(); }
        public void VisitMultiply(DMASTMultiply multiply) { throw new NotImplementedException(); }
        public void VisitDivide(DMASTDivide divide) { throw new NotImplementedException(); }
        public void VisitModulus(DMASTModulus modulus) { throw new NotImplementedException(); }
        public void VisitAdd(DMASTAdd add) { throw new NotImplementedException(); }
        public void VisitSubtract(DMASTSubtract subtract) { throw new NotImplementedException(); }
        public void VisitPostIncrement(DMASTPostIncrement postIncrement) { throw new NotImplementedException(); }
        public void VisitPostDecrement(DMASTPostDecrement postDecrement) { throw new NotImplementedException(); }
        public void VisitTernary(DMASTTernary ternary) { throw new NotImplementedException(); }
        public void VisitAppend(DMASTAppend append) { throw new NotImplementedException(); }
        public void VisitMask(DMASTMask mask) { throw new NotImplementedException(); }
        public void VisitOr(DMASTOr or) { throw new NotImplementedException(); }
        public void VisitAnd(DMASTAnd and) { throw new NotImplementedException(); }
        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) { throw new NotImplementedException(); }
        public void VisitBinaryOr(DMASTBinaryOr binaryOr) { throw new NotImplementedException(); }
        public void VisitBinaryNot(DMASTBinaryNot binaryNot) { throw new NotImplementedException(); }
        public void VisitLeftShift(DMASTLeftShift leftShift) { throw new NotImplementedException(); }
        public void VisitRightShift(DMASTRightShift rightShift) { throw new NotImplementedException(); }
        public void VisitIn(DMASTExpressionIn expressionIn) { throw new NotImplementedException(); }
        public void VisitListIndex(DMASTListIndex listIndex) { throw new NotImplementedException(); }
        public void VisitProcCall(DMASTProcCall procCall) { throw new NotImplementedException(); }
        public void VisitCallParameter(DMASTCallParameter callParameter) { throw new NotImplementedException(); }
        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter) { throw new NotImplementedException(); }
        public void VisitCallableIdentifier(DMASTCallableIdentifier identifier) { throw new NotImplementedException(); }
        public void VisitCallableDereference(DMASTCallableDereference dereference) { throw new NotImplementedException(); }
        public void VisitCallableDereferenceSearch(DMASTCallableDereferenceSearch dereferenceSearch) { throw new NotImplementedException(); }
        public void VisitCallableSuper(DMASTCallableSuper super) { throw new NotImplementedException(); }
        public void VisitCallableSelf(DMASTCallableSelf self) { throw new NotImplementedException(); }
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
            visitor.VisitObjectDefinition(this);
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
        public DreamPath Path;

        public DMASTPath(DreamPath.PathType pathType, string[] pathElements) {
            Path = new DreamPath(pathType, pathElements);
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitPath(this);
        }
    }

    class DMASTObjectVarDefinition : DMASTStatement {
        public DMASTPath Type;
        public string Name;
        public DMASTExpression Value;

        public DMASTObjectVarDefinition(DMASTPath path, DMASTExpression value) {
            int varElementIndex = path.Path.FindElement("var");
            string[] typeElements = path.Path.GetElements(varElementIndex + 1);

            Type = (typeElements.Length > 0) ? new DMASTPath(path.Path.Type, typeElements) : null;
            Name = path.Path.LastElement;
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
            int varElementIndex = path.Path.FindElement("var");
            string[] typeElements = path.Path.GetElements(varElementIndex + 1);

            Type = (typeElements.Length > 0) ? new DMASTPath(path.Path.Type, typeElements) : null;
            Name = path.Path.LastElement;
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

    class DMASTProcStatementContinue : DMASTProcStatement {
        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementContinue(this);
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

    class DMASTProcStatementSpawn : DMASTProcStatement {
        public DMASTExpression Time;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementSpawn(DMASTExpression time, DMASTProcBlockInner body) {
            Time = time;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSpawn(this);
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

    class DMASTProcStatementForNumberRange : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration VariableDeclaration;
        public DMASTCallable Variable;
        public DMASTExpression RangeBegin, RangeEnd;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementForNumberRange(DMASTProcStatementVarDeclaration variableDeclaration, DMASTCallable variable, DMASTExpression rangeBegin, DMASTExpression rangeEnd, DMASTProcBlockInner body) {
            VariableDeclaration = variableDeclaration;
            Variable = variable;
            RangeBegin = rangeBegin;
            RangeEnd = rangeEnd;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForNumberRange(this);
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

    class DMASTCall : DMASTExpression {
        public DMASTCallParameter[] CallParameters, ProcParameters;

        public DMASTCall(DMASTCallParameter[] callParameters, DMASTCallParameter[] procParameters) {
            CallParameters = callParameters;
            ProcParameters = procParameters;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitCall(this);
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

    class DMASTNewInferred : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTNewInferred(DMASTCallParameter[] parameters) {
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitNewInferred(this);
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

    class DMASTPostIncrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostIncrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitPostIncrement(this);
        }
    }

    class DMASTPostDecrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostDecrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            visitor.VisitPostDecrement(this);
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
            Default = 0x0,
            Anything = 0x1,
            Text = 0x2,
            Obj = 0x4,
            Mob = 0x8,
            Turf = 0x10
        }

        public DMASTPath Path;
        public DMASTExpression Value;
        public ParameterType Type;
        public DMASTExpression PossibleValues;

        public DMASTDefinitionParameter(DMASTPath path, DMASTExpression value, ParameterType type, DMASTExpression possibleValues) {
            Path = path;
            Value = value;
            Type = type;
            PossibleValues = possibleValues;
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

        public virtual void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableDereference(this);
        }
    }

    class DMASTCallableDereferenceSearch : DMASTCallableDereference {
        public DMASTCallableDereferenceSearch(DMASTExpression left, DMASTExpression right) : base(left, right) {

        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableDereferenceSearch(this);
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
