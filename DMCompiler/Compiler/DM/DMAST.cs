using System;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.Compiler.DM {
    public interface DMASTVisitor : ASTVisitor {
        public void VisitFile(DMASTFile file) { throw new NotImplementedException(); }
        public void VisitBlockInner(DMASTBlockInner block) { throw new NotImplementedException(); }
        public void VisitProcBlockInner(DMASTProcBlockInner procBlock) { throw new NotImplementedException(); }
        public void VisitObjectDefinition(DMASTObjectDefinition statement) { throw new NotImplementedException(); }
        public void VisitPath(DMASTPath path) { throw new NotImplementedException(); }
        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) { throw new NotImplementedException(); }
        public void VisitMultipleObjectVarDefinitions(DMASTMultipleObjectVarDefinitions multipleObjectVarDefinitions) { throw new NotImplementedException(); }
        public void VisitObjectVarOverride(DMASTObjectVarOverride objectVarOverride) { throw new NotImplementedException(); }
        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) { throw new NotImplementedException(); }
        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) { throw new NotImplementedException(); }
        public void VisitProcStatementMultipleVarDeclarations(DMASTProcStatementMultipleVarDeclarations multipleVarDeclarations) { throw new NotImplementedException(); }
        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn) { throw new NotImplementedException(); }
        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) { throw new NotImplementedException(); }
        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) { throw new NotImplementedException(); }
        public void VisitProcStatementGoto(DMASTProcStatementGoto statementGoto) { throw new NotImplementedException(); }
        public void VisitProcStatementLabel(DMASTProcStatementLabel statementLabel) { throw new NotImplementedException(); }
        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) { throw new NotImplementedException(); }
        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) { throw new NotImplementedException(); }
        public void VisitProcStatementSpawn(DMASTProcStatementSpawn statementSpawn) { throw new NotImplementedException(); }
        public void VisitProcStatementIf(DMASTProcStatementIf statementIf) { throw new NotImplementedException(); }
        public void VisitProcStatementForStandard(DMASTProcStatementForStandard statementForStandard) { throw new NotImplementedException(); }
        public void VisitProcStatementForList(DMASTProcStatementForList statementForList) { throw new NotImplementedException(); }
        public void VisitProcStatementForType(DMASTProcStatementForType statementForType) { throw new NotImplementedException(); }
        public void VisitProcStatementForRange(DMASTProcStatementForRange statementForRange) { throw new NotImplementedException(); }
        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop) { throw new NotImplementedException(); }
        public void VisitProcStatementInfLoop(DMASTProcStatementInfLoop statementInfLoop) {throw new NotImplementedException(); }
        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) { throw new NotImplementedException(); }
        public void VisitProcStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) { throw new NotImplementedException(); }
        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) { throw new NotImplementedException(); }
        public void VisitProcStatementBrowse(DMASTProcStatementBrowse statementBrowse) { throw new NotImplementedException(); }
        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) { throw new NotImplementedException(); }
        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) { throw new NotImplementedException(); }
        public void VisitProcStatementTryCatch(DMASTProcStatementTryCatch statementTryCatch) { throw new NotImplementedException(); }
        public void VisitProcStatementThrow(DMASTProcStatementThrow statementThrow) { throw new NotImplementedException(); }
        public void VisitProcDefinition(DMASTProcDefinition procDefinition) { throw new NotImplementedException(); }
        public void VisitIdentifier(DMASTIdentifier identifier) { throw new NotImplementedException(); }
        public void VisitGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier) { throw new NotImplementedException(); }
        public void VisitConstantInteger(DMASTConstantInteger constant) { throw new NotImplementedException(); }
        public void VisitConstantFloat(DMASTConstantFloat constant) { throw new NotImplementedException(); }
        public void VisitConstantString(DMASTConstantString constant) { throw new NotImplementedException(); }
        public void VisitConstantResource(DMASTConstantResource constant) { throw new NotImplementedException(); }
        public void VisitConstantNull(DMASTConstantNull constant) { throw new NotImplementedException(); }
        public void VisitConstantPath(DMASTConstantPath constant) { throw new NotImplementedException(); }
        public void VisitUpwardPathSearch(DMASTUpwardPathSearch upwardPathSearch) { throw new NotImplementedException(); }
        public void VisitSwitchCaseRange(DMASTSwitchCaseRange switchCaseRange) { throw new NotImplementedException(); }
        public void VisitStringFormat(DMASTStringFormat stringFormat) { throw new NotImplementedException(); }
        public void VisitList(DMASTList list) { throw new NotImplementedException(); }
        public void VisitNewList(DMASTNewList newList) { throw new NotImplementedException(); }
        public void VisitAddText(DMASTAddText input) { throw new NotImplementedException(); }
        public void VisitInput(DMASTInput input) { throw new NotImplementedException(); }
        public void VisitInitial(DMASTInitial initial) { throw new NotImplementedException(); }
        public void VisitIsSaved(DMASTIsSaved isSaved) { throw new NotImplementedException(); }
        public void VisitIsType(DMASTIsType isType) { throw new NotImplementedException(); }
        public void VisitImplicitIsType(DMASTImplicitIsType isType) { throw new NotImplementedException(); }
        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) { throw new NotImplementedException(); }
        public void VisitLocate(DMASTLocate locate) { throw new NotImplementedException(); }
        public void VisitPick(DMASTPick pick) { throw new NotImplementedException(); }
        public void VisitCall(DMASTCall call) { throw new NotImplementedException(); }
        public void VisitAssign(DMASTAssign assign) { throw new NotImplementedException(); }
        public void VisitNewPath(DMASTNewPath newPath) { throw new NotImplementedException(); }
        public void VisitNewMultidimensionalList(DMASTNewMultidimensionalList newMultidimensionalList) { throw new NotImplementedException(); }
        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) { throw new NotImplementedException(); }
        public void VisitNewDereference(DMASTNewDereference newDereference) { throw new NotImplementedException(); }
        public void VisitNewListIndex(DMASTNewListIndex newListIndex) { throw new NotImplementedException(); }
        public void VisitNewInferred(DMASTNewInferred newInferred) { throw new NotImplementedException(); }
        public void VisitNot(DMASTNot not) { throw new NotImplementedException(); }
        public void VisitNegate(DMASTNegate negate) { throw new NotImplementedException(); }
        public void VisitEqual(DMASTEqual equal) { throw new NotImplementedException(); }
        public void VisitNotEqual(DMASTNotEqual notEqual) { throw new NotImplementedException(); }
        public void VisitEquivalent(DMASTEquivalent equivalent) { throw new NotImplementedException(); }
        public void VisitNotEquivalent(DMASTNotEquivalent notEquivalent) { throw new NotImplementedException(); }
        public void VisitLessThan(DMASTLessThan lessThan) { throw new NotImplementedException(); }
        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) { throw new NotImplementedException(); }
        public void VisitGreaterThan(DMASTGreaterThan greaterThan) { throw new NotImplementedException(); }
        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) { throw new NotImplementedException(); }
        public void VisitMultiply(DMASTMultiply multiply) { throw new NotImplementedException(); }
        public void VisitDivide(DMASTDivide divide) { throw new NotImplementedException(); }
        public void VisitModulus(DMASTModulus modulus) { throw new NotImplementedException(); }
        public void VisitPower(DMASTPower power) { throw new NotImplementedException(); }
        public void VisitAdd(DMASTAdd add) { throw new NotImplementedException(); }
        public void VisitSubtract(DMASTSubtract subtract) { throw new NotImplementedException(); }
        public void VisitPreIncrement(DMASTPreIncrement preIncrement) { throw new NotImplementedException(); }
        public void VisitPreDecrement(DMASTPreDecrement preDecrement) { throw new NotImplementedException(); }
        public void VisitPostIncrement(DMASTPostIncrement postIncrement) { throw new NotImplementedException(); }
        public void VisitPostDecrement(DMASTPostDecrement postDecrement) { throw new NotImplementedException(); }
        public void VisitTernary(DMASTTernary ternary) { throw new NotImplementedException(); }
        public void VisitAppend(DMASTAppend append) { throw new NotImplementedException(); }
        public void VisitRemove(DMASTRemove remove) { throw new NotImplementedException(); }
        public void VisitCombine(DMASTCombine combine) { throw new NotImplementedException(); }
        public void VisitMask(DMASTMask mask) { throw new NotImplementedException(); }
        public void VisitLogicalAndAssign(DMASTLogicalAndAssign landAssign) { throw new NotImplementedException(); }
        public void VisitLogicalOrAssign(DMASTLogicalOrAssign lorAssign) { throw new NotImplementedException(); }
        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) { throw new NotImplementedException(); }
        public void VisitDivideAssign(DMASTDivideAssign divideAssign) { throw new NotImplementedException(); }
        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) { throw new NotImplementedException(); }
        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) { throw new NotImplementedException(); }
        public void VisitXorAssign(DMASTXorAssign xorAssign) { throw new NotImplementedException(); }
        public void VisitModulusAssign(DMASTModulusAssign modulusAssign) { throw new NotImplementedException(); }
        public void VisitOr(DMASTOr or) { throw new NotImplementedException(); }
        public void VisitAnd(DMASTAnd and) { throw new NotImplementedException(); }
        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) { throw new NotImplementedException(); }
        public void VisitBinaryXor(DMASTBinaryXor binaryXor) { throw new NotImplementedException(); }
        public void VisitBinaryOr(DMASTBinaryOr binaryOr) { throw new NotImplementedException(); }
        public void VisitBinaryNot(DMASTBinaryNot binaryNot) { throw new NotImplementedException(); }
        public void VisitLeftShift(DMASTLeftShift leftShift) { throw new NotImplementedException(); }
        public void VisitRightShift(DMASTRightShift rightShift) { throw new NotImplementedException(); }
        public void VisitIn(DMASTExpressionIn expressionIn) { throw new NotImplementedException(); }
        public void VisitInRange(DMASTExpressionInRange expressionInRange) { throw new NotImplementedException(); }
        public void VisitListIndex(DMASTListIndex listIndex) { throw new NotImplementedException(); }
        public void VisitProcCall(DMASTProcCall procCall) { throw new NotImplementedException(); }
        public void VisitCallParameter(DMASTCallParameter callParameter) { throw new NotImplementedException(); }
        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter) { throw new NotImplementedException(); }
        public void VisitDereference(DMASTDereference dereference) { throw new NotImplementedException(); }
        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) { throw new NotImplementedException(); }
        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) { throw new NotImplementedException(); }
        public void VisitCallableSuper(DMASTCallableSuper super) { throw new NotImplementedException(); }
        public void VisitCallableSelf(DMASTCallableSelf self) { throw new NotImplementedException(); }
        public void VisitCallableGlobalProc(DMASTCallableGlobalProc globalIdentifier) { throw new NotImplementedException(); }

    }

    public abstract class DMASTNode : ASTNode<DMASTVisitor> {
        public DMASTNode(Location location) {
            Location = location;
        }

        public readonly Location Location;

        public abstract void Visit(DMASTVisitor visitor);
    }

    public abstract class DMASTStatement : DMASTNode {
        public DMASTStatement(Location location)
            : base(location)
        {}
    }

    public abstract class DMASTProcStatement : DMASTNode {
        public DMASTProcStatement(Location location)
            : base(location)
        {}
    }

    public abstract class DMASTExpression : DMASTNode {
        public DMASTExpression(Location location)
            : base(location)
        {}
    }

    public abstract class DMASTExpressionConstant : DMASTExpression {
        public DMASTExpressionConstant(Location location)
            : base(location)
        {
        }
    }

    public interface DMASTCallable {
    }

    public class DMASTFile : DMASTNode {
        public DMASTBlockInner BlockInner;

        public DMASTFile(Location location, DMASTBlockInner blockInner)
            : base(location)
        {
            BlockInner = blockInner;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitFile(this);
        }
    }

    public class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(Location location, DMASTStatement[] statements)
            : base(location)
        {
            Statements = statements;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBlockInner(this);
        }
    }

    public class DMASTProcBlockInner : DMASTNode {
        public DMASTProcStatement[] Statements;

        public DMASTProcBlockInner(Location location, DMASTProcStatement[] statements)
            : base(location)
        {
            Statements = statements;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcBlockInner(this);
        }
    }

    public class DMASTObjectDefinition : DMASTStatement {
        public DreamPath Path;
        public DMASTBlockInner InnerBlock;

        public DMASTObjectDefinition(Location location, DreamPath path, DMASTBlockInner innerBlock) : base(location)
        {
            Path = path;
            InnerBlock = innerBlock;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectDefinition(this);
        }
    }

    public class DMASTProcDefinition : DMASTStatement {
        public DreamPath? ObjectPath;
        public string Name;
        public bool IsOverride = false;
        public bool IsVerb = false;
        public DMASTDefinitionParameter[] Parameters;
        public DMASTProcBlockInner Body;

        public DMASTProcDefinition(Location location, DreamPath path, DMASTDefinitionParameter[] parameters, DMASTProcBlockInner body) : base(location)
        {
            int procElementIndex = path.FindElement("proc");

            if (procElementIndex == -1) {
                procElementIndex = path.FindElement("verb");

                if (procElementIndex != -1) IsVerb = true;
                else IsOverride = true;
            }

            if (procElementIndex != -1) path = path.RemoveElement(procElementIndex);

            ObjectPath = (path.Elements.Length > 1) ? path.FromElements(0, -2) : null;
            Name = path.LastElement;
            Parameters = parameters;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcDefinition(this);
        }
    }

    //TODO: This can probably be replaced with a DreamPath nullable
    public class DMASTPath : DMASTNode {
        public DreamPath Path;

        public DMASTPath(Location location, DreamPath path) : base(location)
        {
            Path = path;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPath(this);
        }
    }

    public class DMASTObjectVarDefinition : DMASTStatement {
        public DreamPath ObjectPath { get => _varDecl.ObjectPath; }
        public DreamPath? Type { get => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath; }
        public string Name { get => _varDecl.VarName; }
        public DMASTExpression Value;

        private ObjVarDeclInfo _varDecl;

        public bool IsStatic { get => _varDecl.IsStatic; }
        public bool IsToplevel { get => _varDecl.IsToplevel; }
        public bool IsGlobal { get => _varDecl.IsStatic || _varDecl.IsToplevel; }
        public bool IsConst { get => _varDecl.IsConst; }
        public bool IsTmp { get => _varDecl.IsTmp; }

        public DMValueType ValType;

        public DMASTObjectVarDefinition(Location location, DreamPath path, DMASTExpression value, DMValueType valType = DMValueType.Anything) : base(location)
        {
            _varDecl = new ObjVarDeclInfo(path);
            Value = value;
            ValType = valType;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectVarDefinition(this);
        }
    }

    public class DMASTMultipleObjectVarDefinitions : DMASTStatement {
        public DMASTObjectVarDefinition[] VarDefinitions;

        public DMASTMultipleObjectVarDefinitions(Location location, DMASTObjectVarDefinition[] varDefinitions) : base(location) {
            VarDefinitions = varDefinitions;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultipleObjectVarDefinitions(this);
        }
    }

    public class DMASTObjectVarOverride : DMASTStatement {
        public DreamPath ObjectPath;
        public string VarName;
        public DMASTExpression Value;

        public DMASTObjectVarOverride(Location location, DreamPath path, DMASTExpression value) : base(location) {
            ObjectPath = path.FromElements(0, -2);
            VarName = path.LastElement;
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectVarOverride(this);
        }
    }

    public class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementExpression(this);
        }
    }

    public class DMASTProcStatementVarDeclaration : DMASTProcStatement {
        public DreamPath? Type { get => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath; }
        public string Name { get => _varDecl.VarName; }
        public DMASTExpression Value;
        private ProcVarDeclInfo _varDecl;

        public bool IsGlobal { get => _varDecl.IsStatic; }
        public bool IsConst { get => _varDecl.IsConst; }

        public DMASTProcStatementVarDeclaration(Location location, DMASTPath path, DMASTExpression value) : base(location)
        {
            _varDecl = new ProcVarDeclInfo(path.Path);
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementVarDeclaration(this);
        }
    }

    public class DMASTProcStatementMultipleVarDeclarations : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration[] VarDeclarations;

        public DMASTProcStatementMultipleVarDeclarations(Location location, DMASTProcStatementVarDeclaration[] varDeclarations) : base(location) {
            VarDeclarations = varDeclarations;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementMultipleVarDeclarations(this);
        }
    }

    public class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementReturn(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementReturn(this);
        }
    }

    public class DMASTProcStatementBreak : DMASTProcStatement
    {
        public DMASTIdentifier Label;

        public DMASTProcStatementBreak(Location location, DMASTIdentifier label = null) : base(location)
        {
            Label = label;
        }
        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBreak(this);
        }
    }

    public class DMASTProcStatementContinue : DMASTProcStatement {
        public DMASTIdentifier Label;

        public DMASTProcStatementContinue(Location location, DMASTIdentifier label = null) : base(location)
        {
            Label = label;
        }
        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementContinue(this);
        }
    }

    public class DMASTProcStatementGoto : DMASTProcStatement {
        public DMASTIdentifier Label;

        public DMASTProcStatementGoto(Location location, DMASTIdentifier label) : base(location) {
            Label = label;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementGoto(this);
        }
    }

    public class DMASTProcStatementLabel : DMASTProcStatement {
        public string Name;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementLabel(Location location, string name, DMASTProcBlockInner body) : base(location) {
            Name = name;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementLabel(this);
        }
    }

    public class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementDel(this);
        }
    }

    public class DMASTProcStatementSet : DMASTProcStatement {
        public string Attribute;
        public DMASTExpression Value;

        public DMASTProcStatementSet(Location location, string attribute, DMASTExpression value) : base(location) {
            Attribute = attribute;
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSet(this);
        }
    }

    public class DMASTProcStatementSpawn : DMASTProcStatement {
        public DMASTExpression Delay;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementSpawn(Location location, DMASTExpression delay, DMASTProcBlockInner body) : base(location) {
            Delay = delay;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSpawn(this);
        }
    }

    public class DMASTProcStatementIf : DMASTProcStatement {
        public DMASTExpression Condition;
        public DMASTProcBlockInner Body;
        public DMASTProcBlockInner ElseBody;

        public DMASTProcStatementIf(Location location, DMASTExpression condition, DMASTProcBlockInner body, DMASTProcBlockInner elseBody = null) : base(location) {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementIf(this);
        }
    }

    public class DMASTProcStatementFor : DMASTProcStatement {
        public DMASTProcStatement Initializer;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementFor(Location location, DMASTProcStatement initializer, DMASTProcBlockInner body) : base(location) {
            Initializer = initializer;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    public class DMASTProcStatementForStandard : DMASTProcStatementFor {
        public DMASTExpression Comparator, Incrementor;

        public DMASTProcStatementForStandard(Location location, DMASTProcStatement initializer, DMASTExpression comparator, DMASTExpression incrementor, DMASTProcBlockInner body) : base(location, initializer, body) {
            Comparator = comparator;
            Incrementor = incrementor;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForStandard(this);
        }
    }

    public class DMASTProcStatementForList : DMASTProcStatementFor {
        public DMASTIdentifier Variable;
        public DMASTExpression List;

        public DMASTProcStatementForList(Location location, DMASTProcStatement initializer, DMASTIdentifier variable, DMASTExpression list, DMASTProcBlockInner body) : base(location, initializer, body) {
            Variable = variable;
            List = list;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForList(this);
        }
    }

    // for(var/client/C) & similar
    public class DMASTProcStatementForType : DMASTProcStatementFor {
        public DMASTIdentifier Variable;

        public DMASTProcStatementForType(Location location, DMASTProcStatement initializer, DMASTIdentifier variable, DMASTProcBlockInner body) : base(location, initializer, body) {
            Variable = variable;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForType(this);
        }
    }

    public class DMASTProcStatementForRange : DMASTProcStatementFor {
        public DMASTIdentifier Variable;
        public DMASTExpression RangeStart, RangeEnd, Step;

        public DMASTProcStatementForRange(Location location, DMASTProcStatement initializer, DMASTIdentifier variable, DMASTExpression rangeStart, DMASTExpression rangeEnd, DMASTExpression step, DMASTProcBlockInner body) : base(location, initializer, body) {
            Variable = variable;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            Step = step;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForRange(this);
        }
    }

    public class DMASTProcStatementForLoop : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration VariableDeclaration;
        public DMASTCallable Variable;
        public DMASTExpression Condition, Incrementer;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementForLoop(Location location, DMASTProcStatementVarDeclaration variableDeclaration, DMASTCallable variable, DMASTExpression condition, DMASTExpression incrementer, DMASTProcBlockInner body) : base(location) {
            VariableDeclaration = variableDeclaration;
            Variable = variable;
            Condition = condition;
            Incrementer = incrementer;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementForLoop(this);
        }
    }

    public class DMASTProcStatementInfLoop : DMASTProcStatement{
        public DMASTProcBlockInner Body;

        public DMASTProcStatementInfLoop(Location location, DMASTProcBlockInner body) : base(location){
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor){
            visitor.VisitProcStatementInfLoop(this);
        }
    }

    public class DMASTProcStatementWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) : base(location) {
            Conditional = conditional;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementWhile(this);
        }
    }

    public class DMASTProcStatementDoWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementDoWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) : base(location) {
            Conditional = conditional;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementDoWhile(this);
        }
    }

    public class DMASTProcStatementSwitch : DMASTProcStatement {
        public class SwitchCase {
            public DMASTProcBlockInner Body;

            protected SwitchCase(DMASTProcBlockInner body) {
                Body = body;
            }
        }

        public class SwitchCaseDefault : SwitchCase {
            public SwitchCaseDefault(DMASTProcBlockInner body) : base(body) { }
        }

        public class SwitchCaseValues : SwitchCase {
            public DMASTExpression[] Values;

            public SwitchCaseValues(DMASTExpression[] values, DMASTProcBlockInner body) : base(body) {
                Values = values;
            }
        }

        public DMASTExpression Value;
        public SwitchCase[] Cases;

        public DMASTProcStatementSwitch(Location location, DMASTExpression value, SwitchCase[] cases) : base(location) {
            Value = value;
            Cases = cases;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSwitch(this);
        }
    }

    public class DMASTProcStatementBrowse : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Body;
        public DMASTExpression Options;

        public DMASTProcStatementBrowse(Location location, DMASTExpression receiver, DMASTExpression body, DMASTExpression options) : base(location) {
            Receiver = receiver;
            Body = body;
            Options = options;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBrowse(this);
        }
    }

    public class DMASTProcStatementBrowseResource : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression File;
        public DMASTExpression Filename;

        public DMASTProcStatementBrowseResource(Location location, DMASTExpression receiver, DMASTExpression file, DMASTExpression filename) : base(location) {
            Receiver = receiver;
            File = file;
            Filename = filename;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBrowseResource(this);
        }
    }

    public class DMASTProcStatementOutputControl : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Message;
        public DMASTExpression Control;

        public DMASTProcStatementOutputControl(Location location, DMASTExpression receiver, DMASTExpression message, DMASTExpression control) : base(location) {
            Receiver = receiver;
            Message = message;
            Control = control;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementOutputControl(this);
        }
    }

    public class DMASTProcStatementTryCatch : DMASTProcStatement {
        public DMASTProcBlockInner TryBody;
        public DMASTProcBlockInner CatchBody;
        public DMASTProcStatement CatchParameter;
        public DMASTProcStatementTryCatch(Location location, DMASTProcBlockInner tryBody, DMASTProcBlockInner catchBody, DMASTProcStatement catchParameter) : base(location)
        {
            TryBody = tryBody;
            CatchBody = catchBody;
            CatchParameter = catchParameter;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementTryCatch(this);
        }
    }

    public class DMASTProcStatementThrow : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementThrow(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementThrow(this);
        }
    }

    public class DMASTIdentifier : DMASTExpression {
        public string Identifier;

        public DMASTIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIdentifier(this);
        }
    }

    public class DMASTGlobalIdentifier : DMASTExpression {
        public string Identifier;

        public DMASTGlobalIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGlobalIdentifier(this);
        }
    }

    public class DMASTConstantInteger : DMASTExpressionConstant {
        public int Value;

        public DMASTConstantInteger(Location location, int value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantInteger(this);
        }
    }

    public class DMASTConstantFloat : DMASTExpressionConstant {
        public float Value;

        public DMASTConstantFloat(Location location, float value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantFloat(this);
        }
    }

    public class DMASTConstantString : DMASTExpressionConstant {
        public string Value;

        public DMASTConstantString(Location location, string value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantString(this);
        }
    }

    public class DMASTConstantResource : DMASTExpressionConstant {
        public string Path;

        public DMASTConstantResource(Location location, string path) : base(location) {
            Path = path;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantResource(this);
        }
    }

    public class DMASTConstantNull : DMASTExpressionConstant {
        public DMASTConstantNull(Location location)
            : base(location)
        {}
        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantNull(this);
        }
    }

    public class DMASTConstantPath : DMASTExpressionConstant {
        public DMASTPath Value;

        public DMASTConstantPath(Location location, DMASTPath value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantPath(this);
        }
    }

    public class DMASTUpwardPathSearch : DMASTExpressionConstant {
        public DMASTExpressionConstant Path;
        public DMASTPath Search;

        public DMASTUpwardPathSearch(Location location, DMASTExpressionConstant path, DMASTPath search) : base(location) {
            Path = path;
            Search = search;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitUpwardPathSearch(this);
        }
    }

    public class DMASTSwitchCaseRange : DMASTExpression {
        public DMASTExpression RangeStart, RangeEnd;

        public DMASTSwitchCaseRange(Location location, DMASTExpression rangeStart, DMASTExpression rangeEnd) : base(location) {
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSwitchCaseRange(this);
        }
    }

    public class DMASTStringFormat : DMASTExpression {
        public string Value;
        public DMASTExpression[] InterpolatedValues;

        public DMASTStringFormat(Location location, string value, DMASTExpression[] interpolatedValues) : base(location) {
            Value = value;
            InterpolatedValues = interpolatedValues;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitStringFormat(this);
        }
    }

    public class DMASTList : DMASTExpression {
        public DMASTCallParameter[] Values;

        public DMASTList(Location location, DMASTCallParameter[] values) : base(location) {
            Values = values;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitList(this);
        }
    }

    public class DMASTAddText : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTAddText(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAddText(this);
        }
    }

    public class DMASTNewList : DMASTExpression
    {
        public DMASTCallParameter[] Parameters;

        public DMASTNewList(Location location, DMASTCallParameter[] parameters) : base(location)
        {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor)
        {
            visitor.VisitNewList(this);
        }
    }

    public class DMASTInput : DMASTExpression {
        public DMASTCallParameter[] Parameters;
        public DMValueType Types;
        public DMASTExpression List;

        public DMASTInput(Location location, DMASTCallParameter[] parameters, DMValueType types, DMASTExpression list) : base(location) {
            Parameters = parameters;
            Types = types;
            List = list;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInput(this);
        }
    }

    public class DMASTInitial : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTInitial(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInitial(this);
        }
    }

    public class DMASTIsSaved : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTIsSaved(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIsSaved(this);
        }
    }

    public class DMASTIsType : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression Type;

        public DMASTIsType(Location location, DMASTExpression value, DMASTExpression type) : base(location) {
            Value = value;
            Type = type;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIsType(this);
        }
    }

    public class DMASTImplicitIsType : DMASTExpression {
        public DMASTExpression Value;

        public DMASTImplicitIsType(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitImplicitIsType(this);
        }
    }

    public class DMASTLocateCoordinates : DMASTExpression {
        public DMASTExpression X, Y, Z;

        public DMASTLocateCoordinates(Location location, DMASTExpression x, DMASTExpression y, DMASTExpression z) : base(location) {
            X = x;
            Y = y;
            Z = z;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLocateCoordinates(this);
        }
    }

    public class DMASTLocate : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Container;

        public DMASTLocate(Location location, DMASTExpression expression, DMASTExpression container) : base(location) {
            Expression = expression;
            Container = container;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLocate(this);
        }
    }

    public class DMASTPick : DMASTExpression {
        public struct PickValue {
            public DMASTExpression Weight;
            public DMASTExpression Value;

            public PickValue(DMASTExpression weight, DMASTExpression value) {
                Weight = weight;
                Value = value;
            }
        }

        public PickValue[] Values;

        public DMASTPick(Location location, PickValue[] values) : base(location) {
            Values = values;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPick(this);
        }
    }

    public class DMASTCall : DMASTExpression {
        public DMASTCallParameter[] CallParameters, ProcParameters;

        public DMASTCall(Location location, DMASTCallParameter[] callParameters, DMASTCallParameter[] procParameters) : base(location) {
            CallParameters = callParameters;
            ProcParameters = procParameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCall(this);
        }
    }

    public class DMASTAssign : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssign(Location location, DMASTExpression expression, DMASTExpression value) : base(location) {
            Expression = expression;
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAssign(this);
        }
    }

    public class DMASTNewPath : DMASTExpression {
        public DMASTPath Path;
        public DMASTCallParameter[] Parameters;

        public DMASTNewPath(Location location, DMASTPath path, DMASTCallParameter[] parameters) : base(location) {
            Path = path;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewPath(this);
        }
    }

    public class DMASTNewMultidimensionalList : DMASTExpression {
        public DMASTExpression[] Dimensions;

        public DMASTNewMultidimensionalList(Location location, DMASTExpression[] dimensions) : base(location) {
            Dimensions = dimensions;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewMultidimensionalList(this);
        }
    }

    public class DMASTNewIdentifier : DMASTExpression {
        public DMASTIdentifier Identifier;
        public DMASTCallParameter[] Parameters;

        public DMASTNewIdentifier(Location location, DMASTIdentifier identifier, DMASTCallParameter[] parameters) : base(location) {
            Identifier = identifier;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewIdentifier(this);
        }
    }

    public class DMASTNewDereference : DMASTExpression {
        public DMASTDereference Dereference;
        public DMASTCallParameter[] Parameters;

        public DMASTNewDereference(Location location, DMASTDereference dereference, DMASTCallParameter[] parameters) : base(location) {
            Dereference = dereference;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewDereference(this);
        }
    }

    public class DMASTNewListIndex : DMASTExpression {
            public DMASTListIndex ListIdx;
            public DMASTCallParameter[] Parameters;

            public DMASTNewListIndex(Location location, DMASTListIndex listIdx, DMASTCallParameter[] parameters) : base(location) {
                ListIdx = listIdx;
                Parameters = parameters;
            }

            public override void Visit(DMASTVisitor visitor) {
                visitor.VisitNewListIndex(this);
            }
        }

    public class DMASTNewInferred : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTNewInferred(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewInferred(this);
        }
    }

    public class DMASTNot : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNot(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNot(this);
        }
    }

    public class DMASTNegate : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNegate(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNegate(this);
        }
    }

    public class DMASTEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitEqual(this);
        }
    }

    public class DMASTNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTNotEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNotEqual(this);
        }
    }

    public class DMASTEquivalent : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTEquivalent(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitEquivalent(this);
        }
    }

    public class DMASTNotEquivalent : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTNotEquivalent(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNotEquivalent(this);
        }
    }

    public class DMASTLessThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThan(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThan(this);
        }
    }

    public class DMASTLessThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThanOrEqual(this);
        }
    }

    public class DMASTGreaterThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThan(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThan(this);
        }
    }

    public class DMASTGreaterThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThanOrEqual(this);
        }
    }

    public class DMASTMultiply : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiply(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultiply(this);
        }
    }

    public class DMASTDivide : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivide(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDivide(this);
        }
    }

    public class DMASTModulus : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulus(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulus(this);
        }
    }

    public class DMASTPower : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTPower(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPower(this);
        }
    }

    public class DMASTAdd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAdd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAdd(this);
        }
    }

    public class DMASTSubtract : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTSubtract(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSubtract(this);
        }
    }

    public class DMASTPreIncrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPreIncrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPreIncrement(this);
        }
    }

    public class DMASTPreDecrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPreDecrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPreDecrement(this);
        }
    }

    public class DMASTPostIncrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostIncrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPostIncrement(this);
        }
    }

    public class DMASTPostDecrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostDecrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPostDecrement(this);
        }
    }

    public class DMASTTernary : DMASTExpression {
        public DMASTExpression A, B, C;

        public DMASTTernary(Location location, DMASTExpression a, DMASTExpression b, DMASTExpression c) : base(location) {
            A = a;
            B = b;
            C = c;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitTernary(this);
        }
    }

    public class DMASTAppend : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAppend(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAppend(this);
        }
    }

    public class DMASTRemove : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRemove(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRemove(this);
        }
    }

    public class DMASTCombine : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTCombine(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCombine(this);
        }
    }

    public class DMASTMask : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMask(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMask(this);
        }
    }

    public class DMASTLogicalAndAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLogicalAndAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLogicalAndAssign(this);
        }
    }

    public class DMASTLogicalOrAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLogicalOrAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLogicalOrAssign(this);
        }
    }

    public class DMASTMultiplyAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiplyAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultiplyAssign(this);
        }
    }

    public class DMASTDivideAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivideAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDivideAssign(this);
        }
    }

    public class DMASTLeftShiftAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLeftShiftAssign(this);
        }
    }

    public class DMASTRightShiftAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRightShiftAssign(this);
        }
    }

    public class DMASTXorAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTXorAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitXorAssign(this);
        }
    }

    public class DMASTModulusAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulusAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulusAssign(this);
        }
    }

    public class DMASTOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTOr(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitOr(this);
        }
    }

    public class DMASTAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAnd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAnd(this);
        }
    }

    public class DMASTBinaryAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryAnd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryAnd(this);
        }
    }

    public class DMASTBinaryXor : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryXor(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryXor(this);
        }
    }

    public class DMASTBinaryOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryOr(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryOr(this);
        }
    }

    public class DMASTBinaryNot : DMASTExpression {
        public DMASTExpression Value;

        public DMASTBinaryNot(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryNot(this);
        }
    }

    public class DMASTLeftShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShift(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLeftShift(this);
        }
    }

    public class DMASTRightShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShift(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRightShift(this);
        }
    }

    public class DMASTExpressionIn : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression List;

        public DMASTExpressionIn(Location location, DMASTExpression value, DMASTExpression list) : base(location) {
            Value = value;
            List = list;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIn(this);
        }
    }

    public class DMASTExpressionInRange : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression StartRange;
        public DMASTExpression EndRange;

        public DMASTExpressionInRange(Location location, DMASTExpression value, DMASTExpression startRange, DMASTExpression endRange) : base(location) {
            Value = value;
            StartRange = startRange;
            EndRange = endRange;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInRange(this);
        }
    }

    public class DMASTListIndex : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Index;
        public bool Conditional;

        public DMASTListIndex(Location location, DMASTExpression expression, DMASTExpression index, bool conditional) : base(location) {
            Expression = expression;
            Index = index;
            Conditional = conditional;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitListIndex(this);
        }
    }

    public class DMASTProcCall : DMASTExpression {
        public DMASTCallable Callable;
        public DMASTCallParameter[] Parameters;

        public DMASTProcCall(Location location, DMASTCallable callable, DMASTCallParameter[] parameters) : base(location) {
            Callable = callable;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcCall(this);
        }
    }

    public class DMASTCallParameter : DMASTNode {
        public DMASTExpression Value;
        public string Name;

        public DMASTCallParameter(Location location, DMASTExpression value, string name = null) : base(location) {
            Value = value;
            Name = name;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallParameter(this);
        }
    }

    public class DMASTDefinitionParameter : DMASTNode {
        public DreamPath? ObjectType { get => _paramDecl.IsList ? DreamPath.List : _paramDecl.TypePath; }
        public string Name { get => _paramDecl.VarName; }
        public DMASTExpression Value;
        public DMValueType Type;
        public DMASTExpression PossibleValues;

        private ProcParameterDeclInfo _paramDecl;

        public DMASTDefinitionParameter(Location location, DMASTPath astPath, DMASTExpression value, DMValueType type, DMASTExpression possibleValues) : base(location) {
            _paramDecl = new ProcParameterDeclInfo(astPath.Path);

            Value = value;
            Type = type;
            PossibleValues = possibleValues;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDefinitionParameter(this);
        }
    }

    public class DMASTDereference : DMASTExpression, DMASTCallable {
        public enum DereferenceType {
            Direct,
            Search,
        }

        public DMASTExpression Expression;
        public string Property;
        public DereferenceType Type;
        public bool Conditional;

        public DMASTDereference(Location location, DMASTExpression expression, string property, DereferenceType type, bool conditional) : base(location) {
            Expression = expression;
            Property = property;
            Type = type;
            Conditional = conditional;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDereference(this);
        }
    }

    public class DMASTDereferenceProc : DMASTDereference {
        public DMASTDereferenceProc(Location location, DMASTExpression expression, string property, DereferenceType type, bool conditional) : base(location, expression, property, type, conditional) { }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDereferenceProc(this);
        }
    }

    public class DMASTCallableProcIdentifier : DMASTExpression, DMASTCallable {
        public string Identifier;

        public DMASTCallableProcIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableProcIdentifier(this);
        }
    }

    public class DMASTCallableSuper : DMASTExpression, DMASTCallable {
        public DMASTCallableSuper(Location location) : base(location){}
        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSuper(this);
        }
    }

    public class DMASTCallableSelf : DMASTExpression, DMASTCallable {
        public DMASTCallableSelf(Location location) : base(location){}
        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSelf(this);
        }
    }

    public class DMASTCallableGlobalProc : DMASTExpression, DMASTCallable {
        public string Identifier;

        public DMASTCallableGlobalProc(Location location, string identifier) : base(location)
        {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor)
        {
            visitor.VisitCallableGlobalProc(this);
        }
    }
}
