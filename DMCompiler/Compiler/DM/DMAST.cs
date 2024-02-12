using System;
using System.Collections.Generic;
using System.Linq;
using DMCompiler.DM;

namespace DMCompiler.Compiler.DM {
    public interface DMASTVisitor {
        public void VisitFile(DMASTFile file) {
            throw new NotImplementedException();
        }

        public void VisitBlockInner(DMASTBlockInner block) {
            throw new NotImplementedException();
        }

        public void VisitProcBlockInner(DMASTProcBlockInner procBlock) {
            throw new NotImplementedException();
        }

        public void VisitObjectDefinition(DMASTObjectDefinition statement) {
            throw new NotImplementedException();
        }

        public void VisitPath(DMASTPath path) {
            throw new NotImplementedException();
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) {
            throw new NotImplementedException();
        }

        public void VisitMultipleObjectVarDefinitions(DMASTMultipleObjectVarDefinitions multipleObjectVarDefinitions) {
            throw new NotImplementedException();
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride objectVarOverride) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statementReturn) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementGoto(DMASTProcStatementGoto statementGoto) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementLabel(DMASTProcStatementLabel statementLabel) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementIf(DMASTProcStatementIf statementIf) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementFor(DMASTProcStatementFor statementFor) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementInfLoop(DMASTProcStatementInfLoop statementInfLoop) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementBrowse(DMASTProcStatementBrowse statementBrowse) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementFtp(DMASTProcStatementFtp statementFtp) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementOutput(DMASTProcStatementOutput statementOutput) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementInput(DMASTProcStatementInput statementInput) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementTryCatch(DMASTProcStatementTryCatch statementTryCatch) {
            throw new NotImplementedException();
        }

        public void VisitProcStatementThrow(DMASTProcStatementThrow statementThrow) {
            throw new NotImplementedException();
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            throw new NotImplementedException();
        }

        public void VisitVoid(DMASTVoid voidNode) {
            throw new NotImplementedException();
        }

        public void VisitIdentifier(DMASTIdentifier identifier) {
            throw new NotImplementedException();
        }

        public void VisitGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier) {
            throw new NotImplementedException();
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            throw new NotImplementedException();
        }

        public void VisitConstantFloat(DMASTConstantFloat constant) {
            throw new NotImplementedException();
        }

        public void VisitConstantString(DMASTConstantString constant) {
            throw new NotImplementedException();
        }

        public void VisitConstantResource(DMASTConstantResource constant) {
            throw new NotImplementedException();
        }

        public void VisitConstantNull(DMASTConstantNull constant) {
            throw new NotImplementedException();
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            throw new NotImplementedException();
        }

        public void VisitUpwardPathSearch(DMASTUpwardPathSearch upwardPathSearch) {
            throw new NotImplementedException();
        }

        public void VisitSwitchCaseRange(DMASTSwitchCaseRange switchCaseRange) {
            throw new NotImplementedException();
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            throw new NotImplementedException();
        }

        public void VisitList(DMASTList list) {
            throw new NotImplementedException();
        }

        public void VisitDimensionalList(DMASTDimensionalList list) {
            throw new NotImplementedException();
        }

        public void VisitNewList(DMASTNewList newList) {
            throw new NotImplementedException();
        }

        public void VisitAddText(DMASTAddText input) {
            throw new NotImplementedException();
        }

        public void VisitProb(DMASTProb prob) {
            throw new NotImplementedException();
        }

        public void VisitInput(DMASTInput input) {
            throw new NotImplementedException();
        }

        public void VisitInitial(DMASTInitial initial) {
            throw new NotImplementedException();
        }

        public void VisitNameof(DMASTNameof nameof) {
            throw new NotImplementedException();
        }

        public void VisitIsSaved(DMASTIsSaved isSaved) {
            throw new NotImplementedException();
        }

        public void VisitIsType(DMASTIsType isType) {
            throw new NotImplementedException();
        }

        public void VisitIsNull(DMASTIsNull isNull) {
            throw new NotImplementedException();
        }

        public void VisitLength(DMASTLength length) {
            throw new NotImplementedException();
        }

        public void VisitGetStep(DMASTGetStep getStep) {
            throw new NotImplementedException();
        }

        public void VisitGetDir(DMASTGetDir getDir) {
            throw new NotImplementedException();
        }

        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            throw new NotImplementedException();
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            throw new NotImplementedException();
        }

        public void VisitLocate(DMASTLocate locate) {
            throw new NotImplementedException();
        }

        public void VisitGradient(DMASTGradient gradient) {
            throw new NotImplementedException();
        }

        public void VisitPick(DMASTPick pick) {
            throw new NotImplementedException();
        }
        public void VisitSin(DMASTSin sin) {
            throw new NotImplementedException();
        }
        public void VisitCos(DMASTCos cos) {
            throw new NotImplementedException();
        }
        public void VisitTan(DMASTTan tan) {
            throw new NotImplementedException();
        }
        public void VisitArcsin(DMASTArcsin asin) {
            throw new NotImplementedException();
        }
        public void VisitArccos(DMASTArccos acos) {
            throw new NotImplementedException();
        }
        public void VisitArctan(DMASTArctan atan) {
            throw new NotImplementedException();
        }
        public void VisitArctan2(DMASTArctan2 atan) {
            throw new NotImplementedException();
        }
        public void VisitSqrt(DMASTSqrt sqrt) {
            throw new NotImplementedException();
        }
        public void VisitLog(DMASTLog log) {
            throw new NotImplementedException();
        }
        public void VisitAbs(DMASTAbs abs) {
            throw new NotImplementedException();
        }
        public void VisitCall(DMASTCall call) {
            throw new NotImplementedException();
        }

        public void VisitAssign(DMASTAssign assign) {
            throw new NotImplementedException();
        }

        public void VisitAssignInto(DMASTAssignInto assign) {
            throw new NotImplementedException();
        }

        public void VisitVarDeclExpression(DMASTVarDeclExpression vardecl) {
            throw new NotImplementedException();
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            throw new NotImplementedException();
        }

        public void VisitNewExpr(DMASTNewExpr newExpr) {
            throw new NotImplementedException();
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            throw new NotImplementedException();
        }

        public void VisitNot(DMASTNot not) {
            throw new NotImplementedException();
        }

        public void VisitNegate(DMASTNegate negate) {
            throw new NotImplementedException();
        }

        public void VisitEqual(DMASTEqual equal) {
            throw new NotImplementedException();
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            throw new NotImplementedException();
        }

        public void VisitEquivalent(DMASTEquivalent equivalent) {
            throw new NotImplementedException();
        }

        public void VisitNotEquivalent(DMASTNotEquivalent notEquivalent) {
            throw new NotImplementedException();
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            throw new NotImplementedException();
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            throw new NotImplementedException();
        }

        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            throw new NotImplementedException();
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            throw new NotImplementedException();
        }

        public void VisitMultiply(DMASTMultiply multiply) {
            throw new NotImplementedException();
        }

        public void VisitDivide(DMASTDivide divide) {
            throw new NotImplementedException();
        }

        public void VisitModulus(DMASTModulus modulus) {
            throw new NotImplementedException();
        }

        public void VisitModulusModulus(DMASTModulusModulus modulusModulus) {
            throw new NotImplementedException();
        }

        public void VisitPower(DMASTPower power) {
            throw new NotImplementedException();
        }

        public void VisitAdd(DMASTAdd add) {
            throw new NotImplementedException();
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            throw new NotImplementedException();
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            throw new NotImplementedException();
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            throw new NotImplementedException();
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            throw new NotImplementedException();
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            throw new NotImplementedException();
        }

        public void VisitTernary(DMASTTernary ternary) {
            throw new NotImplementedException();
        }

        public void VisitAppend(DMASTAppend append) {
            throw new NotImplementedException();
        }

        public void VisitRemove(DMASTRemove remove) {
            throw new NotImplementedException();
        }

        public void VisitCombine(DMASTCombine combine) {
            throw new NotImplementedException();
        }

        public void VisitMask(DMASTMask mask) {
            throw new NotImplementedException();
        }

        public void VisitLogicalAndAssign(DMASTLogicalAndAssign landAssign) {
            throw new NotImplementedException();
        }

        public void VisitLogicalOrAssign(DMASTLogicalOrAssign lorAssign) {
            throw new NotImplementedException();
        }

        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) {
            throw new NotImplementedException();
        }

        public void VisitDivideAssign(DMASTDivideAssign divideAssign) {
            throw new NotImplementedException();
        }

        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) {
            throw new NotImplementedException();
        }

        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) {
            throw new NotImplementedException();
        }

        public void VisitXorAssign(DMASTXorAssign xorAssign) {
            throw new NotImplementedException();
        }

        public void VisitModulusAssign(DMASTModulusAssign modulusAssign) {
            throw new NotImplementedException();
        }

        public void VisitModulusModulusAssign(DMASTModulusModulusAssign modulusModulusAssign) {
            throw new NotImplementedException();
        }

        public void VisitOr(DMASTOr or) {
            throw new NotImplementedException();
        }

        public void VisitAnd(DMASTAnd and) {
            throw new NotImplementedException();
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            throw new NotImplementedException();
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            throw new NotImplementedException();
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            throw new NotImplementedException();
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            throw new NotImplementedException();
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            throw new NotImplementedException();
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            throw new NotImplementedException();
        }

        public void VisitIn(DMASTExpressionIn expressionIn) {
            throw new NotImplementedException();
        }

        public void VisitInRange(DMASTExpressionInRange expressionInRange) {
            throw new NotImplementedException();
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            throw new NotImplementedException();
        }

        public void VisitCallParameter(DMASTCallParameter callParameter) {
            throw new NotImplementedException();
        }

        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter) {
            throw new NotImplementedException();
        }

        public void VisitDereference(DMASTDereference deref) {
            throw new NotImplementedException();
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
            throw new NotImplementedException();
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            throw new NotImplementedException();
        }

        public void VisitCallableSelf(DMASTCallableSelf self) {
            throw new NotImplementedException();
        }

        public void VisitCallableGlobalProc(DMASTCallableGlobalProc globalIdentifier) {
            throw new NotImplementedException();
        }
    }

    public abstract class DMASTNode(Location location) {
        public readonly Location Location = location;

        public abstract void Visit(DMASTVisitor visitor);
    }

    public abstract class DMASTStatement : DMASTNode {
        protected DMASTStatement(Location location) : base(location) {
        }
    }

    public abstract class DMASTProcStatement : DMASTNode {
        protected DMASTProcStatement(Location location)
            : base(location) {
        }

        /// <returns>
        /// Returns true if this statement is either T or an aggregation of T (stored by an <see cref="DMASTAggregate{T}"/> instance). False otherwise.
        /// </returns>
        public bool IsAggregateOr<T>() where T : DMASTProcStatement {
            return (this is T or DMASTAggregate<T>);
        }
    }

    public abstract class DMASTExpression : DMASTNode {
        protected DMASTExpression(Location location) : base(location) {
        }

        public virtual IEnumerable<DMASTExpression> Leaves() {
            yield break;
        }

        /// <summary>
        /// If this is a <see cref="DMASTExpressionWrapped"/>, returns the expression inside.
        /// Returns this expression if not.
        /// </summary>
        public virtual DMASTExpression GetUnwrapped() {
            return this;
        }
    }

    public abstract class DMASTExpressionConstant : DMASTExpression {
        protected DMASTExpressionConstant(Location location) : base(location) {
        }
    }

    public interface DMASTCallable {
    }

    public sealed class DMASTFile : DMASTNode {
        public readonly DMASTBlockInner BlockInner;

        public DMASTFile(Location location, DMASTBlockInner blockInner) : base(location) {
            BlockInner = blockInner;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitFile(this);
        }
    }

    public sealed class DMASTBlockInner : DMASTNode {
        public readonly DMASTStatement[] Statements;

        public DMASTBlockInner(Location location, DMASTStatement[] statements) : base(location) {
            Statements = statements;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBlockInner(this);
        }
    }

    public sealed class DMASTProcBlockInner : DMASTNode {
        public readonly DMASTProcStatement[] Statements;

        /// <remarks>
        /// SetStatements is held separately because all set statements need to be, to borrow cursed JS terms, "hoisted" to the top of the block, before anything else.<br/>
        /// This isn't SPECIFICALLY a <see cref="DMASTProcStatementSet"/> array because some of these may be DMASTAggregate instances.
        /// </remarks>
        public readonly DMASTProcStatement[] SetStatements;

        /// <summary> Initializes an empty block. </summary>
        public DMASTProcBlockInner(Location location) : base(location) {
            Statements = Array.Empty<DMASTProcStatement>();
            SetStatements = Array.Empty<DMASTProcStatement>();
        }

        /// <summary> Initializes a block with only one statement (which may be a <see cref="DMASTProcStatementSet"/> :o) </summary>
        public DMASTProcBlockInner(Location location, DMASTProcStatement statement) : base(location) {
            if (statement.IsAggregateOr<DMASTProcStatementSet>()) {
                // If this is a Set statement or a set of Set statements
                Statements = Array.Empty<DMASTProcStatement>();
                SetStatements = new[] { statement };
            } else {
                Statements = new[] { statement };
                SetStatements = Array.Empty<DMASTProcStatement>();
            }
        }

        public DMASTProcBlockInner(Location location, DMASTProcStatement[] statements,
            DMASTProcStatement[]? setStatements)
            : base(location) {
            Statements = statements;
            SetStatements = setStatements ?? Array.Empty<DMASTProcStatement>();
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcBlockInner(this);
        }
    }

    public sealed class DMASTObjectDefinition : DMASTStatement {
        /// <summary> Unlike other Path variables stored by AST nodes, this path is guaranteed to be the real, absolute path of this object definition block. <br/>
        /// That includes any inherited pathing from being tabbed into a different, base definition.
        /// </summary>
        public DreamPath Path;

        public readonly DMASTBlockInner? InnerBlock;

        public DMASTObjectDefinition(Location location, DreamPath path, DMASTBlockInner? innerBlock) : base(location) {
            Path = path;
            InnerBlock = innerBlock;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectDefinition(this);
        }
    }

    /// <remarks> Also includes proc overrides; see the <see cref="IsOverride"/> member. Verbs too.</remarks>
    public sealed class DMASTProcDefinition : DMASTStatement {
        public readonly DreamPath ObjectPath;
        public readonly string Name;
        public readonly bool IsOverride;
        public readonly bool IsVerb;
        public readonly DMASTDefinitionParameter[] Parameters;
        public readonly DMASTProcBlockInner? Body;

        public DMASTProcDefinition(Location location, DreamPath path, DMASTDefinitionParameter[] parameters,
            DMASTProcBlockInner? body) : base(location) {
            int procElementIndex = path.FindElement("proc");

            if (procElementIndex == -1) {
                procElementIndex = path.FindElement("verb");

                if (procElementIndex != -1) IsVerb = true;
                else IsOverride = true;
            }

            if (procElementIndex != -1) path = path.RemoveElement(procElementIndex);

            ObjectPath = (path.Elements.Length > 1) ? path.FromElements(0, -2) : DreamPath.Root;
            Name = path.LastElement;
            Parameters = parameters;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcDefinition(this);
        }
    }

    //TODO: This can probably be replaced with a DreamPath nullable
    public sealed class DMASTPath : DMASTNode {
        public DreamPath Path;
        public bool IsOperator = false;

        public DMASTPath(Location location, DreamPath path, bool operatorFlag = false) : base(location) {
            Path = path;
            IsOperator = operatorFlag;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPath(this);
        }
    }

    public sealed class DMASTObjectVarDefinition : DMASTStatement {
        /// <summary>The path of the object that we are a property of.</summary>
        public DreamPath ObjectPath => _varDecl.ObjectPath;

        /// <summary>The actual type of the variable itself.</summary>
        public DreamPath? Type => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath;

        public string Name => _varDecl.VarName;
        public DMASTExpression Value;

        private readonly ObjVarDeclInfo _varDecl;

        public bool IsStatic => _varDecl.IsStatic;

        public bool IsGlobal =>
            _varDecl.IsStatic; // TODO: Standardize our phrasing in the codebase. Are we calling these Statics or Globals?

        public bool IsConst => _varDecl.IsConst;
        public bool IsTmp => _varDecl.IsTmp;

        public readonly DMValueType ValType;

        public DMASTObjectVarDefinition(Location location, DreamPath path, DMASTExpression value,
            DMValueType valType = DMValueType.Anything) : base(location) {
            _varDecl = new ObjVarDeclInfo(path);
            Value = value;
            ValType = valType;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitObjectVarDefinition(this);
        }
    }

    public sealed class DMASTMultipleObjectVarDefinitions : DMASTStatement {
        public readonly DMASTObjectVarDefinition[] VarDefinitions;

        public DMASTMultipleObjectVarDefinitions(Location location, DMASTObjectVarDefinition[] varDefinitions) :
            base(location) {
            VarDefinitions = varDefinitions;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultipleObjectVarDefinitions(this);
        }
    }

    public sealed class DMASTObjectVarOverride : DMASTStatement {
        public readonly DreamPath ObjectPath;
        public readonly string VarName;
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

    public sealed class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementExpression(this);
        }
    }

    public sealed class DMASTProcStatementVarDeclaration : DMASTProcStatement {
        public DMASTExpression? Value;

        public DreamPath? Type => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath;
        public string Name => _varDecl.VarName;
        public bool IsGlobal => _varDecl.IsStatic;
        public bool IsConst => _varDecl.IsConst;

        private readonly ProcVarDeclInfo _varDecl;

        public DMASTProcStatementVarDeclaration(Location location, DMASTPath path, DMASTExpression? value) :
            base(location) {
            _varDecl = new ProcVarDeclInfo(path.Path);
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementVarDeclaration(this);
        }
    }

    /// <summary>
    /// A kinda-abstract class that represents several statements that were created in unison by one "super-statement" <br/>
    /// Such as, a var declaration that actually declares several vars at once (which in our parser must become "one" statement, hence this thing)
    /// </summary>
    /// <typeparam name="T">The DMASTProcStatement-derived class that this AST node holds.</typeparam>
    public sealed class DMASTAggregate<T> : DMASTProcStatement where T : DMASTProcStatement {
        // Gotta be honest? I like this "where" syntax better than C++20 concepts
        public T[] Statements { get; }

        public DMASTAggregate(Location location, T[] statements) : base(location) {
            Statements = statements;
        }

        public override void Visit(DMASTVisitor visitor) {
            foreach (T statement in Statements)
                statement.Visit(visitor);
        }
    }

    public sealed class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression? Value;

        public DMASTProcStatementReturn(Location location, DMASTExpression? value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementReturn(this);
        }
    }

    public sealed class DMASTProcStatementBreak : DMASTProcStatement {
        public readonly DMASTIdentifier? Label;

        public DMASTProcStatementBreak(Location location, DMASTIdentifier? label = null) : base(location) {
            Label = label;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBreak(this);
        }
    }

    public sealed class DMASTProcStatementContinue : DMASTProcStatement {
        public readonly DMASTIdentifier? Label;

        public DMASTProcStatementContinue(Location location, DMASTIdentifier? label = null) : base(location) {
            Label = label;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementContinue(this);
        }
    }

    public sealed class DMASTProcStatementGoto : DMASTProcStatement {
        public readonly DMASTIdentifier Label;

        public DMASTProcStatementGoto(Location location, DMASTIdentifier label) : base(location) {
            Label = label;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementGoto(this);
        }
    }

    public sealed class DMASTProcStatementLabel : DMASTProcStatement {
        public readonly string Name;
        public readonly DMASTProcBlockInner? Body;

        public DMASTProcStatementLabel(Location location, string name, DMASTProcBlockInner? body) : base(location) {
            Name = name;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementLabel(this);
        }
    }

    public sealed class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementDel(this);
        }
    }

    public sealed class DMASTProcStatementSet : DMASTProcStatement {
        public readonly string Attribute;
        public readonly DMASTExpression Value;
        public readonly bool WasInKeyword; // Marks whether this was a "set x in y" expression, or a "set x = y" one

        public DMASTProcStatementSet(Location location, string attribute, DMASTExpression value, bool wasInKeyword) :
            base(location) {
            Attribute = attribute;
            Value = value;
            WasInKeyword = wasInKeyword;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSet(this);
        }
    }

    public sealed class DMASTProcStatementSpawn : DMASTProcStatement {
        public DMASTExpression Delay;
        public readonly DMASTProcBlockInner Body;

        public DMASTProcStatementSpawn(Location location, DMASTExpression delay, DMASTProcBlockInner body) :
            base(location) {
            Delay = delay;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSpawn(this);
        }
    }

    public sealed class DMASTProcStatementIf : DMASTProcStatement {
        public DMASTExpression Condition;
        public readonly DMASTProcBlockInner Body;
        public readonly DMASTProcBlockInner? ElseBody;

        public DMASTProcStatementIf(Location location, DMASTExpression condition, DMASTProcBlockInner body,
            DMASTProcBlockInner? elseBody = null) : base(location) {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementIf(this);
        }
    }

    public sealed class DMASTProcStatementFor : DMASTProcStatement {
        public DMASTExpression? Expression1, Expression2, Expression3;
        public DMValueType? DMTypes;
        public readonly DMASTProcBlockInner Body;

        public DMASTProcStatementFor(Location location, DMASTExpression? expr1, DMASTExpression? expr2,
            DMASTExpression? expr3, DMValueType? dmTypes, DMASTProcBlockInner body) : base(location) {
            Expression1 = expr1;
            Expression2 = expr2;
            Expression3 = expr3;
            DMTypes = dmTypes;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementFor(this);
        }
    }

    public sealed class DMASTProcStatementInfLoop : DMASTProcStatement {
        public readonly DMASTProcBlockInner Body;

        public DMASTProcStatementInfLoop(Location location, DMASTProcBlockInner body) : base(location) {
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementInfLoop(this);
        }
    }

    public sealed class DMASTProcStatementWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public readonly DMASTProcBlockInner Body;

        public DMASTProcStatementWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) :
            base(location) {
            Conditional = conditional;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementWhile(this);
        }
    }

    public sealed class DMASTProcStatementDoWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public readonly DMASTProcBlockInner Body;

        public DMASTProcStatementDoWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) :
            base(location) {
            Conditional = conditional;
            Body = body;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementDoWhile(this);
        }
    }

    public sealed class DMASTProcStatementSwitch : DMASTProcStatement {
        public class SwitchCase {
            public readonly DMASTProcBlockInner Body;

            protected SwitchCase(DMASTProcBlockInner body) {
                Body = body;
            }
        }

        public sealed class SwitchCaseDefault : SwitchCase {
            public SwitchCaseDefault(DMASTProcBlockInner body) : base(body) {
            }
        }

        public sealed class SwitchCaseValues : SwitchCase {
            public readonly DMASTExpression[] Values;

            public SwitchCaseValues(DMASTExpression[] values, DMASTProcBlockInner body) : base(body) {
                Values = values;
            }
        }

        public DMASTExpression Value;
        public readonly SwitchCase[] Cases;

        public DMASTProcStatementSwitch(Location location, DMASTExpression value, SwitchCase[] cases) : base(location) {
            Value = value;
            Cases = cases;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementSwitch(this);
        }
    }

    public sealed class DMASTProcStatementBrowse : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Body;
        public DMASTExpression Options;

        public DMASTProcStatementBrowse(Location location, DMASTExpression receiver, DMASTExpression body,
            DMASTExpression options) : base(location) {
            Receiver = receiver;
            Body = body;
            Options = options;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBrowse(this);
        }
    }

    public sealed class DMASTProcStatementBrowseResource : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression File;
        public DMASTExpression Filename;

        public DMASTProcStatementBrowseResource(Location location, DMASTExpression receiver, DMASTExpression file,
            DMASTExpression filename) : base(location) {
            Receiver = receiver;
            File = file;
            Filename = filename;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementBrowseResource(this);
        }
    }

    public sealed class DMASTProcStatementOutputControl : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Message;
        public DMASTExpression Control;

        public DMASTProcStatementOutputControl(Location location, DMASTExpression receiver, DMASTExpression message,
            DMASTExpression control) : base(location) {
            Receiver = receiver;
            Message = message;
            Control = control;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementOutputControl(this);
        }
    }

    public sealed class DMASTProcStatementFtp : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression File;
        public DMASTExpression Name;

        public DMASTProcStatementFtp(Location location, DMASTExpression receiver, DMASTExpression file, DMASTExpression name) : base(location) {
            Receiver = receiver;
            File = file;
            Name = name;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementFtp(this);
        }
    }

    public sealed class DMASTProcStatementOutput : DMASTProcStatement {
        public DMASTExpression A, B;

        public DMASTProcStatementOutput(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementOutput(this);
        }
    }

    public sealed class DMASTProcStatementInput : DMASTProcStatement {
        public DMASTExpression A, B;

        public DMASTProcStatementInput(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementInput(this);
        }
    }

    public sealed class DMASTProcStatementTryCatch : DMASTProcStatement {
        public readonly DMASTProcBlockInner TryBody;
        public readonly DMASTProcBlockInner? CatchBody;
        public readonly DMASTProcStatement? CatchParameter;

        public DMASTProcStatementTryCatch(Location location, DMASTProcBlockInner tryBody,
            DMASTProcBlockInner? catchBody, DMASTProcStatement? catchParameter) : base(location) {
            TryBody = tryBody;
            CatchBody = catchBody;
            CatchParameter = catchParameter;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementTryCatch(this);
        }
    }

    public sealed class DMASTProcStatementThrow : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementThrow(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcStatementThrow(this);
        }
    }

    public sealed class DMASTVoid : DMASTExpression {
        public DMASTVoid(Location location) : base(location) {
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitVoid(this);
        }
    }

    public sealed class DMASTIdentifier : DMASTExpression {
        public readonly string Identifier;

        public DMASTIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIdentifier(this);
        }
    }

    public sealed class DMASTGlobalIdentifier : DMASTExpression {
        public readonly string Identifier;

        public DMASTGlobalIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGlobalIdentifier(this);
        }
    }

    public sealed class DMASTConstantInteger : DMASTExpressionConstant {
        public readonly int Value;

        public DMASTConstantInteger(Location location, int value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantInteger(this);
        }
    }

    public sealed class DMASTConstantFloat : DMASTExpressionConstant {
        public readonly float Value;

        public DMASTConstantFloat(Location location, float value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantFloat(this);
        }
    }

    public sealed class DMASTConstantString : DMASTExpressionConstant {
        public readonly string Value;

        public DMASTConstantString(Location location, string value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantString(this);
        }
    }

    public sealed class DMASTConstantResource : DMASTExpressionConstant {
        public readonly string Path;

        public DMASTConstantResource(Location location, string path) : base(location) {
            Path = path;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantResource(this);
        }
    }

    public sealed class DMASTConstantNull : DMASTExpressionConstant {
        public DMASTConstantNull(Location location)
            : base(location) {
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantNull(this);
        }
    }

    public sealed class DMASTConstantPath : DMASTExpressionConstant {
        public readonly DMASTPath Value;

        public DMASTConstantPath(Location location, DMASTPath value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitConstantPath(this);
        }
    }

    public sealed class DMASTUpwardPathSearch : DMASTExpressionConstant {
        public readonly DMASTExpressionConstant Path;
        public readonly DMASTPath Search;

        public DMASTUpwardPathSearch(Location location, DMASTExpressionConstant path, DMASTPath search) :
            base(location) {
            Path = path;
            Search = search;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitUpwardPathSearch(this);
        }
    }

    public sealed class DMASTSwitchCaseRange : DMASTExpression {
        public DMASTExpression RangeStart, RangeEnd;

        public DMASTSwitchCaseRange(Location location, DMASTExpression rangeStart, DMASTExpression rangeEnd) :
            base(location) {
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSwitchCaseRange(this);
        }
    }

    public sealed class DMASTStringFormat : DMASTExpression {
        public readonly string Value;
        public readonly DMASTExpression?[] InterpolatedValues;

        public DMASTStringFormat(Location location, string value, DMASTExpression?[] interpolatedValues) :
            base(location) {
            Value = value;
            InterpolatedValues = interpolatedValues;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitStringFormat(this);
        }
    }

    public sealed class DMASTList : DMASTExpression {
        public readonly DMASTCallParameter[] Values;

        public DMASTList(Location location, DMASTCallParameter[] values) : base(location) {
            Values = values;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitList(this);
        }

        public bool AllValuesConstant() {
            return Values.All(
                value => (value is {
                    Key: DMASTExpressionConstant,
                    Value: DMASTExpressionConstant
                })
                ||
                (value is {
                    Key: DMASTExpressionConstant,
                    Value: DMASTList valueList
                } && valueList.AllValuesConstant())
            );
        }
    }

    /// <summary>
    /// Represents the value of a var defined as <code>var/list/L[1][2][3]</code>
    /// </summary>
    public sealed class DMASTDimensionalList : DMASTExpression {
        public readonly List<DMASTExpression> Sizes;

        public DMASTDimensionalList(Location location, List<DMASTExpression> sizes) : base(location) {
            Sizes = sizes;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDimensionalList(this);
        }
    }

    public sealed class DMASTAddText : DMASTExpression {
        public readonly DMASTCallParameter[] Parameters;

        public DMASTAddText(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAddText(this);
        }
    }

    public sealed class DMASTProb : DMASTExpression {
        public readonly DMASTExpression P;

        public DMASTProb(Location location, DMASTExpression p) : base(location) {
            P = p;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProb(this);
        }
    }

    public sealed class DMASTNewList : DMASTExpression {
        public readonly DMASTCallParameter[] Parameters;

        public DMASTNewList(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewList(this);
        }
    }

    public sealed class DMASTInput : DMASTExpression {
        public readonly DMASTCallParameter[] Parameters;
        public DMValueType? Types;
        public readonly DMASTExpression? List;

        public DMASTInput(Location location, DMASTCallParameter[] parameters, DMValueType? types,
            DMASTExpression? list) : base(location) {
            Parameters = parameters;
            Types = types;
            List = list;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInput(this);
        }
    }

    public sealed class DMASTInitial : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTInitial(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInitial(this);
        }
    }

    public sealed class DMASTNameof : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNameof(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNameof(this);
        }
    }

    public sealed class DMASTIsSaved : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTIsSaved(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIsSaved(this);
        }
    }

    public sealed class DMASTIsType : DMASTExpression {
        public readonly DMASTExpression Value;
        public readonly DMASTExpression Type;

        public DMASTIsType(Location location, DMASTExpression value, DMASTExpression type) : base(location) {
            Value = value;
            Type = type;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIsType(this);
        }
    }

    public sealed class DMASTIsNull : DMASTExpression {
        public readonly DMASTExpression Value;

        public DMASTIsNull(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIsNull(this);
        }
    }

    public sealed class DMASTLength : DMASTExpression {
        public readonly DMASTExpression Value;

        public DMASTLength(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLength(this);
        }
    }

    public sealed class DMASTGetStep : DMASTExpression {
        public readonly DMASTExpression Ref;
        public readonly DMASTExpression Dir;

        public DMASTGetStep(Location location, DMASTExpression refValue, DMASTExpression dir) : base(location) {
            Ref = refValue;
            Dir = dir;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGetStep(this);
        }
    }

    public sealed class DMASTGetDir : DMASTExpression {
        public readonly DMASTExpression Loc1;
        public readonly DMASTExpression Loc2;

        public DMASTGetDir(Location location, DMASTExpression loc1, DMASTExpression loc2) : base(location) {
            Loc1 = loc1;
            Loc2 = loc2;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGetDir(this);
        }
    }

    public sealed class DMASTImplicitIsType : DMASTExpression {
        public readonly DMASTExpression Value;

        public DMASTImplicitIsType(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitImplicitIsType(this);
        }
    }

    public sealed class DMASTLocateCoordinates : DMASTExpression {
        public readonly DMASTExpression X, Y, Z;

        public DMASTLocateCoordinates(Location location, DMASTExpression x, DMASTExpression y, DMASTExpression z) :
            base(location) {
            X = x;
            Y = y;
            Z = z;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLocateCoordinates(this);
        }
    }

    public sealed class DMASTLocate : DMASTExpression {
        public readonly DMASTExpression? Expression;
        public readonly DMASTExpression? Container;

        public DMASTLocate(Location location, DMASTExpression? expression, DMASTExpression? container) :
            base(location) {
            Expression = expression;
            Container = container;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLocate(this);
        }
    }

    public sealed class DMASTGradient : DMASTExpression {
        public readonly DMASTCallParameter[] Parameters;

        public DMASTGradient(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGradient(this);
        }
    }

    public sealed class DMASTPick : DMASTExpression {
        public struct PickValue {
            public readonly DMASTExpression? Weight;
            public readonly DMASTExpression Value;

            public PickValue(DMASTExpression? weight, DMASTExpression value) {
                Weight = weight;
                Value = value;
            }
        }

        public readonly PickValue[] Values;

        public DMASTPick(Location location, PickValue[] values) : base(location) {
            Values = values;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPick(this);
        }
    }

    public class DMASTSin : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTSin(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSin(this);
        }
    }

    public class DMASTCos : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTCos(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCos(this);
        }
    }

    public class DMASTTan : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTTan(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitTan(this);
        }
    }

    public class DMASTArcsin : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTArcsin(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitArcsin(this);
        }
    }

    public class DMASTArccos : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTArccos(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitArccos(this);
        }
    }

    public class DMASTArctan : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTArctan(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitArctan(this);
        }
    }

    public class DMASTArctan2 : DMASTExpression {
        public DMASTExpression XExpression;
        public DMASTExpression YExpression;

        public DMASTArctan2(Location location, DMASTExpression xExpression, DMASTExpression yExpression) : base(location) {
            XExpression = xExpression;
            YExpression = yExpression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitArctan2(this);
        }
    }

    public class DMASTSqrt : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTSqrt(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSqrt(this);
        }
    }

    public class DMASTLog : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression? BaseExpression;

        public DMASTLog(Location location, DMASTExpression expression, DMASTExpression? baseExpression) : base(location) {
            Expression = expression;
            BaseExpression = baseExpression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLog(this);
        }
    }

    public class DMASTAbs : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTAbs(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAbs(this);
        }
    }
    public sealed class DMASTCall : DMASTExpression {
        public readonly DMASTCallParameter[] CallParameters, ProcParameters;

        public DMASTCall(Location location, DMASTCallParameter[] callParameters, DMASTCallParameter[] procParameters) :
            base(location) {
            CallParameters = callParameters;
            ProcParameters = procParameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCall(this);
        }
    }

    public sealed class DMASTAssign : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssign(Location location, DMASTExpression expression, DMASTExpression value) : base(location) {
            Expression = expression;
            Value = value;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
            yield return Value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAssign(this);
        }
    }

    public class DMASTAssignInto : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssignInto(Location location, DMASTExpression expression, DMASTExpression value) : base(location) {
            Expression = expression;
            Value = value;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
            yield return Value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAssignInto(this);
        }
    }

    public class DMASTVarDeclExpression : DMASTExpression {
        public DMASTPath DeclPath;
        public DMASTVarDeclExpression(Location location, DMASTPath path) : base(location) {
            DeclPath = path;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitVarDeclExpression(this);
        }
    }

    public sealed class DMASTNewPath : DMASTExpression {
        public readonly DMASTConstantPath Path;
        public readonly DMASTCallParameter[] Parameters;

        public DMASTNewPath(Location location, DMASTConstantPath path, DMASTCallParameter[] parameters) : base(location) {
            Path = path;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewPath(this);
        }
    }

    public sealed class DMASTNewExpr : DMASTExpression {
        public DMASTExpression Expression;
        public readonly DMASTCallParameter[] Parameters;

        public DMASTNewExpr(Location location, DMASTExpression expression, DMASTCallParameter[] parameters) :
            base(location) {
            Expression = expression;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewExpr(this);
        }
    }

    public sealed class DMASTNewInferred : DMASTExpression {
        public readonly DMASTCallParameter[] Parameters;

        public DMASTNewInferred(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNewInferred(this);
        }
    }

    public sealed class DMASTNot : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNot(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNot(this);
        }
    }

    public sealed class DMASTNegate : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNegate(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNegate(this);
        }
    }

    public sealed class DMASTEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitEqual(this);
        }
    }

    public sealed class DMASTNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTNotEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNotEqual(this);
        }
    }

    public sealed class DMASTEquivalent : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTEquivalent(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitEquivalent(this);
        }
    }

    public sealed class DMASTNotEquivalent : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTNotEquivalent(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitNotEquivalent(this);
        }
    }

    public sealed class DMASTLessThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThan(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThan(this);
        }
    }

    public sealed class DMASTLessThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLessThanOrEqual(this);
        }
    }

    public sealed class DMASTGreaterThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThan(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThan(this);
        }
    }

    public sealed class DMASTGreaterThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitGreaterThanOrEqual(this);
        }
    }

    public sealed class DMASTMultiply : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiply(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultiply(this);
        }
    }

    public sealed class DMASTDivide : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivide(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDivide(this);
        }
    }

    public sealed class DMASTModulus : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulus(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulus(this);
        }
    }

    public sealed class DMASTModulusModulus : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTModulusModulus(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulusModulus(this);
        }
    }

    public sealed class DMASTPower : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTPower(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPower(this);
        }
    }

    public sealed class DMASTAdd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAdd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAdd(this);
        }
    }

    public sealed class DMASTSubtract : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTSubtract(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitSubtract(this);
        }
    }

    public sealed class DMASTPreIncrement : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTPreIncrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPreIncrement(this);
        }
    }

    public sealed class DMASTPreDecrement : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTPreDecrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPreDecrement(this);
        }
    }

    public sealed class DMASTPostIncrement : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTPostIncrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPostIncrement(this);
        }
    }

    public sealed class DMASTPostDecrement : DMASTExpression {
        public readonly DMASTExpression Expression;

        public DMASTPostDecrement(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitPostDecrement(this);
        }
    }

    public sealed class DMASTTernary : DMASTExpression {
        public readonly DMASTExpression A, B, C;

        public DMASTTernary(Location location, DMASTExpression a, DMASTExpression b, DMASTExpression c) :
            base(location) {
            A = a;
            B = b;
            C = c;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
            yield return C;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitTernary(this);
        }
    }

    public sealed class DMASTAppend : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAppend(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAppend(this);
        }
    }

    public sealed class DMASTRemove : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRemove(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRemove(this);
        }
    }

    public sealed class DMASTCombine : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTCombine(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCombine(this);
        }
    }

    public sealed class DMASTMask : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMask(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMask(this);
        }
    }

    public sealed class DMASTLogicalAndAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTLogicalAndAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLogicalAndAssign(this);
        }
    }

    public sealed class DMASTLogicalOrAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTLogicalOrAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLogicalOrAssign(this);
        }
    }

    public sealed class DMASTMultiplyAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTMultiplyAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitMultiplyAssign(this);
        }
    }

    public sealed class DMASTDivideAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTDivideAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDivideAssign(this);
        }
    }

    public sealed class DMASTLeftShiftAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTLeftShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLeftShiftAssign(this);
        }
    }

    public sealed class DMASTRightShiftAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTRightShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRightShiftAssign(this);
        }
    }

    public sealed class DMASTXorAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTXorAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitXorAssign(this);
        }
    }

    public sealed class DMASTModulusAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTModulusAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulusAssign(this);
        }
    }

    public sealed class DMASTModulusModulusAssign : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTModulusModulusAssign(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitModulusModulusAssign(this);
        }
    }

    public sealed class DMASTOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTOr(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitOr(this);
        }
    }

    public sealed class DMASTAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAnd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitAnd(this);
        }
    }

    public sealed class DMASTBinaryAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryAnd(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryAnd(this);
        }
    }

    public sealed class DMASTBinaryXor : DMASTExpression {
        public readonly DMASTExpression A, B;

        public DMASTBinaryXor(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryXor(this);
        }
    }

    public sealed class DMASTBinaryOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryOr(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryOr(this);
        }
    }

    public sealed class DMASTBinaryNot : DMASTExpression {
        public DMASTExpression Value;

        public DMASTBinaryNot(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Value;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitBinaryNot(this);
        }
    }

    public sealed class DMASTLeftShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShift(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitLeftShift(this);
        }
    }

    public sealed class DMASTRightShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShift(Location location, DMASTExpression a, DMASTExpression b) : base(location) {
            A = a;
            B = b;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return A;
            yield return B;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitRightShift(this);
        }
    }

    /// <summary>
    /// An expression wrapped around parentheses
    /// <code>(1 + 1)</code>
    /// </summary>
    public sealed class DMASTExpressionWrapped : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTExpressionWrapped(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }

        public override void Visit(DMASTVisitor visitor) {
            Expression.Visit(visitor);
        }

        public override DMASTExpression GetUnwrapped() {
            DMASTExpression expr = Expression;
            while (expr is DMASTExpressionWrapped wrapped)
                expr = wrapped.Expression;

            return expr;
        }
    }

    public sealed class DMASTExpressionIn : DMASTExpression {
        public readonly DMASTExpression Value;
        public readonly DMASTExpression List;

        public DMASTExpressionIn(Location location, DMASTExpression value, DMASTExpression list) : base(location) {
            Value = value;
            List = list;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Value;
            yield return List;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitIn(this);
        }
    }

    public sealed class DMASTExpressionInRange : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression StartRange;
        public DMASTExpression EndRange;
        public readonly DMASTExpression? Step;

        public DMASTExpressionInRange(Location location, DMASTExpression value, DMASTExpression startRange,
            DMASTExpression endRange, DMASTExpression? step = null) : base(location) {
            Value = value;
            StartRange = startRange;
            EndRange = endRange;
            Step = step;
        }

        public override IEnumerable<DMASTExpression> Leaves() {
            yield return Value;
            yield return StartRange;
            yield return EndRange;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitInRange(this);
        }
    }

    public sealed class DMASTProcCall : DMASTExpression {
        public readonly DMASTCallable Callable;
        public readonly DMASTCallParameter[] Parameters;

        public DMASTProcCall(Location location, DMASTCallable callable, DMASTCallParameter[] parameters) :
            base(location) {
            Callable = callable;
            Parameters = parameters;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitProcCall(this);
        }
    }

    public sealed class DMASTCallParameter : DMASTNode {
        public DMASTExpression Value;
        public readonly DMASTExpression? Key;

        public DMASTCallParameter(Location location, DMASTExpression value, DMASTExpression? key = null) :
            base(location) {
            Value = value;
            Key = key;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallParameter(this);
        }
    }

    public sealed class DMASTDefinitionParameter : DMASTNode {
        public DreamPath? ObjectType => _paramDecl.IsList ? DreamPath.List : _paramDecl.TypePath;
        public string Name => _paramDecl.VarName;
        public DMASTExpression? Value;
        public readonly DMValueType Type;
        public DMASTExpression PossibleValues;

        private readonly ProcParameterDeclInfo _paramDecl;

        public DMASTDefinitionParameter(Location location, DMASTPath astPath, DMASTExpression? value, DMValueType type,
            DMASTExpression possibleValues) : base(location) {
            _paramDecl = new ProcParameterDeclInfo(astPath.Path);

            Value = value;
            Type = type;
            PossibleValues = possibleValues;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDefinitionParameter(this);
        }
    }


    public sealed class DMASTDereference : DMASTExpression {
        public abstract class Operation {
            /// <summary>
            /// The location of the operation.
            /// </summary>
            public required Location Location;
            /// <summary>
            /// Whether we should short circuit if the expression we are accessing is null.
            /// </summary>
            public required bool Safe; // x?.y, x?.y() etc
        }

        public abstract class NamedOperation : Operation {
            /// <summary>
            /// Name of the identifier.
            /// </summary>
            public required string Identifier;
            /// <summary>
            /// Whether we should check if the variable exists or not.
            /// </summary>
            public required bool NoSearch; // x:y, x:y()
        }

        public sealed class FieldOperation : NamedOperation;

        public sealed class IndexOperation : Operation {
            /// <summary>
            /// The index expression that we use to index this expression (constant or otherwise).
            /// </summary>
            public required DMASTExpression Index; // x[y], x?[y]
        }

        public sealed class CallOperation : NamedOperation {
            /// <summary>
            /// The parameters that we call this proc with.
            /// </summary>
            public required DMASTCallParameter[] Parameters; // x.y(),
        }

        public DMASTExpression Expression;

        // Always contains at least one operation
        public Operation[] Operations;

        public DMASTDereference(Location location, DMASTExpression expression, Operation[] operations) :
            base(location) {
            Expression = expression;
            Operations = operations;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitDereference(this);
        }
    }

    public sealed class DMASTCallableProcIdentifier : DMASTExpression, DMASTCallable {
        public readonly string Identifier;

        public DMASTCallableProcIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableProcIdentifier(this);
        }
    }

    public sealed class DMASTCallableSuper : DMASTExpression, DMASTCallable {
        public DMASTCallableSuper(Location location) : base(location) {
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSuper(this);
        }
    }

    public sealed class DMASTCallableSelf : DMASTExpression, DMASTCallable {
        public DMASTCallableSelf(Location location) : base(location) {
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableSelf(this);
        }
    }

    public sealed class DMASTCallableGlobalProc : DMASTExpression, DMASTCallable {
        public readonly string Identifier;

        public DMASTCallableGlobalProc(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }

        public override void Visit(DMASTVisitor visitor) {
            visitor.VisitCallableGlobalProc(this);
        }
    }
}
