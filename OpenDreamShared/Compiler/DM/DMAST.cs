using System;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamShared.Compiler.DM {
    public interface DMASTVisitor : ASTVisitor {
        public void VisitFile(DMASTFile file) { throw new NotImplementedException(); }
        public void VisitBlockInner(DMASTBlockInner block) { throw new NotImplementedException(); }
        public void VisitProcBlockInner(DMASTProcBlockInner procBlock) { throw new NotImplementedException(); }
        public void VisitObjectDefinition(DMASTObjectDefinition statement) { throw new NotImplementedException(); }
        public void VisitPath(DMASTPath path) { throw new NotImplementedException(); }
        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) { throw new NotImplementedException(); }
        public void VisitObjectVarOverride(DMASTObjectVarOverride objectVarOverride) { throw new NotImplementedException(); }
        public void VisitProcStatementExpression(DMASTProcStatementExpression statementExpression) { throw new NotImplementedException(); }
        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) { throw new NotImplementedException(); }
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
        public void VisitProcStatementForRange(DMASTProcStatementForRange statementForRange) { throw new NotImplementedException(); }
        public void VisitProcStatementForLoop(DMASTProcStatementForLoop statementForLoop) { throw new NotImplementedException(); }
        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) { throw new NotImplementedException(); }
        public void VisitProcStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) { throw new NotImplementedException(); }
        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) { throw new NotImplementedException(); }
        public void VisitProcStatementBrowse(DMASTProcStatementBrowse statementBrowse) { throw new NotImplementedException(); }
        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) { throw new NotImplementedException(); }
        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) { throw new NotImplementedException(); }
        public void VisitProcDefinition(DMASTProcDefinition procDefinition) { throw new NotImplementedException(); }
        public void VisitIdentifier(DMASTIdentifier identifier) { throw new NotImplementedException(); }
        public void VisitConstantInteger(DMASTConstantInteger constant) { throw new NotImplementedException(); }
        public void VisitConstantFloat(DMASTConstantFloat constant) { throw new NotImplementedException(); }
        public void VisitConstantString(DMASTConstantString constant) { throw new NotImplementedException(); }
        public void VisitConstantResource(DMASTConstantResource constant) { throw new NotImplementedException(); }
        public void VisitConstantNull(DMASTConstantNull constant) { throw new NotImplementedException(); }
        public void VisitConstantPath(DMASTConstantPath constant) { throw new NotImplementedException(); }
        public void VisitSwitchCaseRange(DMASTSwitchCaseRange switchCaseRange) { throw new NotImplementedException(); }
        public void VisitStringFormat(DMASTStringFormat stringFormat) { throw new NotImplementedException(); }
        public void VisitList(DMASTList list) { throw new NotImplementedException(); }
        public void VisitNewList(DMASTNewList newList) { throw new NotImplementedException(); }
        public void VisitInput(DMASTInput input) { throw new NotImplementedException(); }
        public void VisitInitial(DMASTInitial initial) { throw new NotImplementedException(); }
        public void VisitIsSaved(DMASTIsSaved isSaved) { throw new NotImplementedException(); }
        public void VisitIsType(DMASTIsType isType) { throw new NotImplementedException(); }
        public void VisitImplicitIsType(DMASTImplicitIsType isType) { throw new NotImplementedException(); }
        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) { throw new NotImplementedException(); }
        public void VisitLocate(DMASTLocate locate) { throw new NotImplementedException(); }
        public void VisitCall(DMASTCall call) { throw new NotImplementedException(); }
        public void VisitAssign(DMASTAssign assign) { throw new NotImplementedException(); }
        public void VisitNewPath(DMASTNewPath newPath) { throw new NotImplementedException(); }
        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) { throw new NotImplementedException(); }
        public void VisitNewDereference(DMASTNewDereference newDereference) { throw new NotImplementedException(); }
        public void VisitNewInferred(DMASTNewInferred newInferred) { throw new NotImplementedException(); }
        public void VisitNot(DMASTNot not) { throw new NotImplementedException(); }
        public void VisitNegate(DMASTNegate negate) { throw new NotImplementedException(); }
        public void VisitEqual(DMASTEqual equal) { throw new NotImplementedException(); }
        public void VisitNotEqual(DMASTNotEqual notEqual) { throw new NotImplementedException(); }
        public void VisitLessThan(DMASTLessThan lessThan) { throw new NotImplementedException(); }
        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) { throw new NotImplementedException(); }
        public void VisitGreaterThan(DMASTGreaterThan greaterThan) { throw new NotImplementedException(); }
        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) { throw new NotImplementedException(); }
        public void VisitMultiply(DMASTMultiply multiply) { throw new NotImplementedException(); }
        public void VisitDivide(DMASTDivide divide) { throw new NotImplementedException(); }
        public void VisitModulus(DMASTModulus modulus) { throw new NotImplementedException(); }
        public void VisitPower(DMASTPower power) { throw new NotImplementedException(); }
        public void VisitAdd(DMASTAdd add) { throw new NotImplementedException(); }
        public void VisitSubtract(DMASTSubtract subtract) { throw new CompileErrorException("DMASTSubstract"); }
        public void VisitPreIncrement(DMASTPreIncrement preIncrement) { throw new NotImplementedException(); }
        public void VisitPreDecrement(DMASTPreDecrement preDecrement) { throw new NotImplementedException(); }
        public void VisitPostIncrement(DMASTPostIncrement postIncrement) { throw new NotImplementedException(); }
        public void VisitPostDecrement(DMASTPostDecrement postDecrement) { throw new NotImplementedException(); }
        public void VisitTernary(DMASTTernary ternary) { throw new NotImplementedException(); }
        public void VisitAppend(DMASTAppend append) { throw new NotImplementedException(); }
        public void VisitRemove(DMASTRemove remove) { throw new NotImplementedException(); }
        public void VisitCombine(DMASTCombine combine) { throw new NotImplementedException(); }
        public void VisitMask(DMASTMask mask) { throw new NotImplementedException(); }
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
        public void VisitListIndex(DMASTListIndex listIndex) { throw new NotImplementedException(); }
        public void VisitProcCall(DMASTProcCall procCall) { throw new NotImplementedException(); }
        public void VisitCallParameter(DMASTCallParameter callParameter) { throw new NotImplementedException(); }
        public void VisitDefinitionParameter(DMASTDefinitionParameter definitionParameter) { throw new NotImplementedException(); }
        public void VisitDereference(DMASTDereference dereference) { throw new NotImplementedException(); }
        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) { throw new NotImplementedException(); }
        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) { throw new NotImplementedException(); }
        public void VisitCallableSuper(DMASTCallableSuper super) { throw new NotImplementedException(); }
        public void VisitCallableSelf(DMASTCallableSelf self) { throw new NotImplementedException(); }
    }

    public interface DMASTNode : ASTNode<DMASTVisitor> {

    }

    public interface DMASTStatement : DMASTNode {

    }

    public interface DMASTProcStatement : DMASTNode {

    }

    public interface DMASTExpression : DMASTNode {

    }

    public interface DMASTExpressionConstant : DMASTExpression {

    }

    public interface DMASTCallable : DMASTExpression {

    }

    public class DMASTFile : DMASTNode {
        public DMASTBlockInner BlockInner;

        public DMASTFile(DMASTBlockInner blockInner) {
            BlockInner = blockInner;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitFile(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(DMASTStatement[] statements) {
            Statements = statements;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitBlockInner(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcBlockInner : DMASTNode {
        public DMASTProcStatement[] Statements;

        public DMASTProcBlockInner(DMASTProcStatement[] statements) {
            Statements = statements;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcBlockInner(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTObjectDefinition : DMASTStatement {
        public DreamPath Path;
        public DMASTBlockInner InnerBlock;

        public DMASTObjectDefinition(DreamPath path, DMASTBlockInner innerBlock) {
            Path = path;
            InnerBlock = innerBlock;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitObjectDefinition(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcDefinition : DMASTStatement {
        public DreamPath? ObjectPath;
        public string Name;
        public bool IsOverride = false;
        public bool IsVerb = false;
        public DMASTDefinitionParameter[] Parameters;
        public DMASTProcBlockInner Body;

        public DMASTProcDefinition(DreamPath path, DMASTDefinitionParameter[] parameters, DMASTProcBlockInner body) {
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

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcDefinition(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPath : DMASTNode {
        public DreamPath Path;

        public DMASTPath(DreamPath path) {
            Path = path;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPath(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTObjectVarDefinition : DMASTStatement {
        public DreamPath ObjectPath;
        public DreamPath? Type;
        public string Name;
        public DMASTExpression Value;
        public bool IsGlobal = false;

        public DMASTObjectVarDefinition(DreamPath path, DMASTExpression value) {
            int globalElementIndex = path.FindElement("global");
            if (globalElementIndex != -1) path = path.RemoveElement(globalElementIndex);

            int varElementIndex = path.FindElement("var");
            if (varElementIndex == -1) throw new Exception("Var definition's path (" + path + ") did not contain a var element");

            DreamPath varPath = path.FromElements(varElementIndex + 1, -1);

            ObjectPath = path.FromElements(0, varElementIndex);
            Type = (varPath.Elements.Length > 1) ? varPath.FromElements(0, -2) : null;
            IsGlobal = globalElementIndex != -1 || ObjectPath.Equals(DreamPath.Root);
            Name = varPath.LastElement;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitObjectVarDefinition(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTObjectVarOverride : DMASTStatement {
        public DreamPath ObjectPath;
        public string VarName;
        public DMASTExpression Value;

        public DMASTObjectVarOverride(DreamPath path, DMASTExpression value) {
            ObjectPath = path.FromElements(0, -2);
            VarName = path.LastElement;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitObjectVarOverride(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementExpression(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementVarDeclaration : DMASTProcStatement {
        public DreamPath? Type;
        public string Name;
        public DMASTExpression Value;

        public DMASTProcStatementVarDeclaration(DMASTPath path, DMASTExpression value) {
            int varElementIndex = path.Path.FindElement("var");
            DreamPath typePath = path.Path.FromElements(varElementIndex + 1, -2);

            Type = (typePath.Elements.Length > 0) ? typePath : null;
            Name = path.Path.LastElement;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementVarDeclaration(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementReturn(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementReturn(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementBreak : DMASTProcStatement {
        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementBreak(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementContinue : DMASTProcStatement {
        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementContinue(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementGoto : DMASTProcStatement {
        public DMASTIdentifier Label;

        public DMASTProcStatementGoto(DMASTIdentifier label) {
            Label = label;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementGoto(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementLabel : DMASTProcStatement {
        public string Name;

        public DMASTProcStatementLabel(string name) {
            Name = name;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementLabel(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementDel(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementSet : DMASTProcStatement {
        public string Attribute;
        public DMASTExpression Value;

        public DMASTProcStatementSet(string attribute, DMASTExpression value) {
            Attribute = attribute;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementSet(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementSpawn : DMASTProcStatement {
        public DMASTExpression Delay;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementSpawn(DMASTExpression delay, DMASTProcBlockInner body) {
            Delay = delay;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementSpawn(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementIf : DMASTProcStatement {
        public DMASTExpression Condition;
        public DMASTProcBlockInner Body;
        public DMASTProcBlockInner ElseBody;

        public DMASTProcStatementIf(DMASTExpression condition, DMASTProcBlockInner body, DMASTProcBlockInner elseBody = null) {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementIf(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementFor : DMASTProcStatement {
        public DMASTProcStatement Initializer;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementFor(DMASTProcStatement initializer, DMASTProcBlockInner body) {
            Initializer = initializer;
            Body = body;
        }

        public virtual void Visit(DMASTVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    public class DMASTProcStatementForStandard : DMASTProcStatementFor {
        public DMASTExpression Comparator, Incrementor;

        public DMASTProcStatementForStandard(DMASTProcStatement initializer, DMASTExpression comparator, DMASTExpression incrementor, DMASTProcBlockInner body) : base(initializer, body) {
            Comparator = comparator;
            Incrementor = incrementor;
        }

        public override void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementForStandard(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementForList : DMASTProcStatementFor {
        public DMASTIdentifier Variable;
        public DMASTExpression List;

        public DMASTProcStatementForList(DMASTProcStatement initializer, DMASTIdentifier variable, DMASTExpression list, DMASTProcBlockInner body) : base(initializer, body) {
            Variable = variable;
            List = list;
        }

        public override void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementForList(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementForRange : DMASTProcStatementFor {
        public DMASTIdentifier Variable;
        public DMASTExpression RangeStart, RangeEnd, Step;

        public DMASTProcStatementForRange(DMASTProcStatement initializer, DMASTIdentifier variable, DMASTExpression rangeStart, DMASTExpression rangeEnd, DMASTExpression step, DMASTProcBlockInner body) : base(initializer, body) {
            Variable = variable;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            Step = step;
        }

        public override void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementForRange(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementForLoop : DMASTProcStatement {
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
            try {
                visitor.VisitProcStatementForLoop(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementWhile(DMASTExpression conditional, DMASTProcBlockInner body) {
            Conditional = conditional;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementWhile(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementDoWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementDoWhile(DMASTExpression conditional, DMASTProcBlockInner body) {
            Conditional = conditional;
            Body = body;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementDoWhile(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
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

        public DMASTProcStatementSwitch(DMASTExpression value, SwitchCase[] cases) {
            Value = value;
            Cases = cases;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementSwitch(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementBrowse : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Body;
        public DMASTExpression Options;

        public DMASTProcStatementBrowse(DMASTExpression receiver, DMASTExpression body, DMASTExpression options) {
            Receiver = receiver;
            Body = body;
            Options = options;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementBrowse(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementBrowseResource : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression File;
        public DMASTExpression Filename;

        public DMASTProcStatementBrowseResource(DMASTExpression receiver, DMASTExpression file, DMASTExpression filename) {
            Receiver = receiver;
            File = file;
            Filename = filename;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementBrowseResource(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcStatementOutputControl : DMASTProcStatement {
        public DMASTExpression Receiver;
        public DMASTExpression Message;
        public DMASTExpression Control;

        public DMASTProcStatementOutputControl(DMASTExpression receiver, DMASTExpression message, DMASTExpression control) {
            Receiver = receiver;
            Message = message;
            Control = control;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcStatementOutputControl(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTIdentifier : DMASTExpression {
        public string Identifier;

        public DMASTIdentifier(string identifier) {
            Identifier = identifier;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitIdentifier(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantInteger : DMASTExpressionConstant {
        public int Value;

        public DMASTConstantInteger(int value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantInteger(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantFloat : DMASTExpressionConstant {
        public float Value;

        public DMASTConstantFloat(float value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantFloat(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantString : DMASTExpressionConstant {
        public string Value;

        public DMASTConstantString(string value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantString(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantResource : DMASTExpressionConstant {
        public string Path;

        public DMASTConstantResource(string path) {
            Path = path;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantResource(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantNull : DMASTExpressionConstant {
        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantNull(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTConstantPath : DMASTExpressionConstant {
        public DMASTPath Value;

        public DMASTConstantPath(DMASTPath value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitConstantPath(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTSwitchCaseRange : DMASTExpression {
        public DMASTExpression RangeStart, RangeEnd;

        public DMASTSwitchCaseRange(DMASTExpression rangeStart, DMASTExpression rangeEnd) {
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitSwitchCaseRange(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTStringFormat : DMASTExpression {
        public string Value;
        public DMASTExpression[] InterpolatedValues;

        public DMASTStringFormat(string value, DMASTExpression[] interpolatedValues) {
            Value = value;
            InterpolatedValues = interpolatedValues;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitStringFormat(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTList : DMASTExpression {
        public DMASTCallParameter[] Values;

        public DMASTList(DMASTCallParameter[] values) {
            Values = values;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitList(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNewList : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTNewList(DMASTCallParameter[] parameters) {
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNewList(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTInput : DMASTExpression {
        public DMASTCallParameter[] Parameters;
        public DMValueType Types;
        public DMASTExpression List;

        public DMASTInput(DMASTCallParameter[] parameters, DMValueType types, DMASTExpression list) {
            Parameters = parameters;
            Types = types;
            List = list;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitInput(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTInitial : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTInitial(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitInitial(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTIsSaved : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTIsSaved(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitIsSaved(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTIsType : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression Type;

        public DMASTIsType(DMASTExpression value, DMASTExpression type) {
            Value = value;
            Type = type;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitIsType(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTImplicitIsType : DMASTExpression {
        public DMASTExpression Value;

        public DMASTImplicitIsType(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitImplicitIsType(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLocateCoordinates : DMASTExpression {
        public DMASTExpression X, Y, Z;

        public DMASTLocateCoordinates(DMASTExpression x, DMASTExpression y, DMASTExpression z) {
            X = x;
            Y = y;
            Z = z;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLocateCoordinates(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLocate : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Container;

        public DMASTLocate(DMASTExpression expression, DMASTExpression container) {
            Expression = expression;
            Container = container;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLocate(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCall : DMASTExpression {
        public DMASTCallParameter[] CallParameters, ProcParameters;

        public DMASTCall(DMASTCallParameter[] callParameters, DMASTCallParameter[] procParameters) {
            CallParameters = callParameters;
            ProcParameters = procParameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCall(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTAssign : DMASTExpression {
        public DMASTExpression Expression, Value;

        public DMASTAssign(DMASTExpression expression, DMASTExpression value) {
            Expression = expression;
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNewPath : DMASTExpression {
        public DMASTPath Path;
        public DMASTCallParameter[] Parameters;

        public DMASTNewPath(DMASTPath path, DMASTCallParameter[] parameters) {
            Path = path;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNewPath(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNewIdentifier : DMASTExpression {
        public DMASTIdentifier Identifier;
        public DMASTCallParameter[] Parameters;

        public DMASTNewIdentifier(DMASTIdentifier identifier, DMASTCallParameter[] parameters) {
            Identifier = identifier;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNewIdentifier(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNewDereference : DMASTExpression {
        public DMASTDereference Dereference;
        public DMASTCallParameter[] Parameters;

        public DMASTNewDereference(DMASTDereference dereference, DMASTCallParameter[] parameters) {
            Dereference = dereference;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNewDereference(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNewInferred : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTNewInferred(DMASTCallParameter[] parameters) {
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNewInferred(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNot : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNot(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNot(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNegate : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTNegate(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNegate(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitEqual(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTNotEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTNotEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitNotEqual(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLessThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThan(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLessThan(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLessThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLessThanOrEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLessThanOrEqual(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTGreaterThan : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThan(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitGreaterThan(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTGreaterThanOrEqual : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTGreaterThanOrEqual(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitGreaterThanOrEqual(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTMultiply : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiply(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitMultiply(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTDivide : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivide(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitDivide(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTModulus : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulus(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitModulus(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPower : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTPower(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPower(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTAdd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAdd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitAdd(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTSubtract : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTSubtract(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitSubtract(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPreIncrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPreIncrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPreIncrement(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPreDecrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPreDecrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPreDecrement(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPostIncrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostIncrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPostIncrement(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTPostDecrement : DMASTExpression {
        public DMASTExpression Expression;

        public DMASTPostDecrement(DMASTExpression expression) {
            Expression = expression;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitPostDecrement(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTTernary : DMASTExpression {
        public DMASTExpression A, B, C;

        public DMASTTernary(DMASTExpression a, DMASTExpression b, DMASTExpression c) {
            A = a;
            B = b;
            C = c;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitTernary(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTAppend : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAppend(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitAppend(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTRemove : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRemove(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitRemove(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCombine : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTCombine(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCombine(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTMask : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMask(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitMask(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTMultiplyAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTMultiplyAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitMultiplyAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTDivideAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTDivideAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitDivideAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLeftShiftAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShiftAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLeftShiftAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTRightShiftAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShiftAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitRightShiftAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTXorAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTXorAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitXorAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTModulusAssign : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTModulusAssign(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitModulusAssign(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTOr(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitOr(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTAnd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitAnd(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTBinaryAnd : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryAnd(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitBinaryAnd(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTBinaryXor : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryXor(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitBinaryXor(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTBinaryOr : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTBinaryOr(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitBinaryOr(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTBinaryNot : DMASTExpression {
        public DMASTExpression Value;

        public DMASTBinaryNot(DMASTExpression value) {
            Value = value;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitBinaryNot(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTLeftShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTLeftShift(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitLeftShift(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTRightShift : DMASTExpression {
        public DMASTExpression A, B;

        public DMASTRightShift(DMASTExpression a, DMASTExpression b) {
            A = a;
            B = b;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitRightShift(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTExpressionIn : DMASTExpression {
        public DMASTExpression Value;
        public DMASTExpression List;

        public DMASTExpressionIn(DMASTExpression value, DMASTExpression list) {
            Value = value;
            List = list;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitIn(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTListIndex : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Index;

        public DMASTListIndex(DMASTExpression expression, DMASTExpression index) {
            Expression = expression;
            Index = index;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitListIndex(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTProcCall : DMASTExpression {
        public DMASTCallable Callable;
        public DMASTCallParameter[] Parameters;

        public DMASTProcCall(DMASTCallable callable, DMASTCallParameter[] parameters) {
            Callable = callable;
            Parameters = parameters;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitProcCall(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCallParameter : DMASTNode {
        public DMASTExpression Value;
        public string Name;

        public DMASTCallParameter(DMASTExpression value, string name = null) {
            Value = value;
            Name = name;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCallParameter(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTDefinitionParameter : DMASTNode {
        public DreamPath? ObjectType;
        public string Name;
        public DMASTExpression Value;
        public DMValueType Type;
        public DMASTExpression PossibleValues;

        public DMASTDefinitionParameter(DMASTPath astPath, DMASTExpression value, DMValueType type, DMASTExpression possibleValues) {
            DreamPath path = astPath.Path;

            int varElementIndex = path.FindElement("var");
            if (varElementIndex != -1) path = path.RemoveElement(varElementIndex);

            ObjectType = (path.Elements.Length > 1) ? path.FromElements(0, -2) : null;
            Name = path.LastElement;
            Value = value;
            Type = type;
            PossibleValues = possibleValues;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitDefinitionParameter(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTDereference : DMASTCallable {
        public enum DereferenceType {
            Direct,
            Search,
        }

        public struct Dereference {
            public DereferenceType Type;
            public bool Conditional;
            public string Property;

            public Dereference(DereferenceType type, bool conditional, string property) {
                Type = type;
                Conditional = conditional;
                Property = property;
            }
        }

        public DMASTExpression Expression;
        public Dereference[] Dereferences;

        public DMASTDereference(DMASTExpression expression, Dereference[] dereferences) {
            Expression = expression;
            Dereferences = dereferences;
        }

        public virtual void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitDereference(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTDereferenceProc : DMASTDereference, DMASTCallable {
        public DMASTDereferenceProc(DMASTExpression expression, Dereference[] dereferences) : base(expression, dereferences) { }

        public override void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitDereferenceProc(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCallableProcIdentifier : DMASTCallable {
        public string Identifier;

        public DMASTCallableProcIdentifier(string identifier) {
            Identifier = identifier;
        }

        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCallableProcIdentifier(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCallableSuper : DMASTCallable {
        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCallableSuper(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }

    public class DMASTCallableSelf : DMASTCallable {
        public void Visit(DMASTVisitor visitor) {
            try {
                visitor.VisitCallableSelf(this);
            } catch (CompileErrorException exception) {
                visitor.HandleCompileErrorException(exception);
            }
        }
    }
}
