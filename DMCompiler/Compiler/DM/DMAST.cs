using System;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.Compiler.DM {
    public abstract class DMASTNode {
        public DMASTNode(Location location) {
            Location = location;
        }

        public readonly Location Location;
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

    public interface DMASTUnary {
        public DMASTExpression Expression { get; set; }
    }

    public interface DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }
    }

    public class DMASTFile : DMASTNode {
        public DMASTBlockInner BlockInner;

        public DMASTFile(Location location, DMASTBlockInner blockInner)
            : base(location)
        {
            BlockInner = blockInner;
        }
    }

    public class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(Location location, DMASTStatement[] statements)
            : base(location)
        {
            Statements = statements;
        }
    }

    public class DMASTProcBlockInner : DMASTNode {
        public DMASTProcStatement[] Statements;

        public DMASTProcBlockInner(Location location, DMASTProcStatement[] statements)
            : base(location)
        {
            Statements = statements;
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
    }

    //TODO: This can probably be replaced with a DreamPath nullable
    public class DMASTPath : DMASTNode {
        public DreamPath Path;

        public DMASTPath(Location location, DreamPath path) : base(location)
        {
            Path = path;
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
    }

    public class DMASTMultipleObjectVarDefinitions : DMASTStatement {
        public DMASTObjectVarDefinition[] VarDefinitions;

        public DMASTMultipleObjectVarDefinitions(Location location, DMASTObjectVarDefinition[] varDefinitions) : base(location) {
            VarDefinitions = varDefinitions;
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
    }

    public class DMASTProcStatementExpression : DMASTProcStatement {
        public DMASTExpression Expression;

        public DMASTProcStatementExpression(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
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
    }

    public class DMASTProcStatementMultipleVarDeclarations : DMASTProcStatement {
        public DMASTProcStatementVarDeclaration[] VarDeclarations;

        public DMASTProcStatementMultipleVarDeclarations(Location location, DMASTProcStatementVarDeclaration[] varDeclarations) : base(location) {
            VarDeclarations = varDeclarations;
        }
    }

    public class DMASTProcStatementReturn : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementReturn(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }
    }

    public class DMASTProcStatementBreak : DMASTProcStatement
    {
        public DMASTIdentifier Label;

        public DMASTProcStatementBreak(Location location, DMASTIdentifier label = null) : base(location)
        {
            Label = label;
        }
    }

    public class DMASTProcStatementContinue : DMASTProcStatement {
        public DMASTIdentifier Label;

        public DMASTProcStatementContinue(Location location, DMASTIdentifier label = null) : base(location)
        {
            Label = label;
        }
    }

    public class DMASTProcStatementGoto : DMASTProcStatement {
        public DMASTIdentifier Label;

        public DMASTProcStatementGoto(Location location, DMASTIdentifier label) : base(location) {
            Label = label;
        }
    }

    public class DMASTProcStatementLabel : DMASTProcStatement {
        public string Name;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementLabel(Location location, string name, DMASTProcBlockInner body) : base(location) {
            Name = name;
            Body = body;
        }
    }

    public class DMASTProcStatementDel : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementDel(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }
    }

    public class DMASTProcStatementSet : DMASTProcStatement {
        public string Attribute;
        public DMASTExpression Value;

        public DMASTProcStatementSet(Location location, string attribute, DMASTExpression value) : base(location) {
            Attribute = attribute;
            Value = value;
        }
    }

    public class DMASTProcStatementSpawn : DMASTProcStatement {
        public DMASTExpression Delay;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementSpawn(Location location, DMASTExpression delay, DMASTProcBlockInner body) : base(location) {
            Delay = delay;
            Body = body;
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
    }

    public class DMASTProcStatementFor : DMASTProcStatement {
        public DMASTProcStatement Initializer;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementFor(Location location, DMASTProcStatement initializer, DMASTProcBlockInner body) : base(location) {
            Initializer = initializer;
            Body = body;
        }
    }

    public class DMASTProcStatementForStandard : DMASTProcStatementFor {
        public DMASTExpression Comparator, Incrementor;

        public DMASTProcStatementForStandard(Location location, DMASTProcStatement initializer, DMASTExpression comparator, DMASTExpression incrementor, DMASTProcBlockInner body) : base(location, initializer, body) {
            Comparator = comparator;
            Incrementor = incrementor;
        }
    }

    public class DMASTProcStatementForList : DMASTProcStatementFor {
        public DMASTIdentifier Variable;
        public DMASTExpression List;

        public DMASTProcStatementForList(Location location, DMASTProcStatement initializer, DMASTIdentifier variable, DMASTExpression list, DMASTProcBlockInner body) : base(location, initializer, body) {
            Variable = variable;
            List = list;
        }
    }

    // for(var/client/C) & similar
    public class DMASTProcStatementForType : DMASTProcStatementFor {
        public DMASTIdentifier Variable;

        public DMASTProcStatementForType(Location location, DMASTProcStatement initializer, DMASTIdentifier variable, DMASTProcBlockInner body) : base(location, initializer, body) {
            Variable = variable;
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
    }

    public class DMASTProcStatementInfLoop : DMASTProcStatement{
        public DMASTProcBlockInner Body;

        public DMASTProcStatementInfLoop(Location location, DMASTProcBlockInner body) : base(location){
            Body = body;
        }
    }

    public class DMASTProcStatementWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) : base(location) {
            Conditional = conditional;
            Body = body;
        }
    }

    public class DMASTProcStatementDoWhile : DMASTProcStatement {
        public DMASTExpression Conditional;
        public DMASTProcBlockInner Body;

        public DMASTProcStatementDoWhile(Location location, DMASTExpression conditional, DMASTProcBlockInner body) : base(location) {
            Conditional = conditional;
            Body = body;
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
    }

    public class DMASTProcStatementThrow : DMASTProcStatement {
        public DMASTExpression Value;

        public DMASTProcStatementThrow(Location location, DMASTExpression value) : base(location) {
            Value = value;
        }
    }

    public class DMASTIdentifier : DMASTExpression {
        public string Identifier;

        public DMASTIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }
    }

    public class DMASTGlobalIdentifier : DMASTExpression {
        public string Identifier;

        public DMASTGlobalIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }
    }

    public class DMASTConstantInteger : DMASTExpressionConstant {
        public int Value;

        public DMASTConstantInteger(Location location, int value) : base(location) {
            Value = value;
        }
    }

    public class DMASTConstantFloat : DMASTExpressionConstant {
        public float Value;

        public DMASTConstantFloat(Location location, float value) : base(location) {
            Value = value;
        }
    }

    public class DMASTConstantString : DMASTExpressionConstant {
        public string Value;

        public DMASTConstantString(Location location, string value) : base(location) {
            Value = value;
        }
    }

    public class DMASTConstantResource : DMASTExpressionConstant {
        public string Path;

        public DMASTConstantResource(Location location, string path) : base(location) {
            Path = path;
        }
    }

    public class DMASTConstantNull : DMASTExpressionConstant {
        public DMASTConstantNull(Location location)
            : base(location)
        {}
    }

    public class DMASTConstantPath : DMASTExpressionConstant {
        public DMASTPath Value;

        public DMASTConstantPath(Location location, DMASTPath value) : base(location) {
            Value = value;
        }
    }

    public class DMASTUpwardPathSearch : DMASTExpressionConstant {
        public DMASTExpressionConstant Path;
        public DMASTPath Search;

        public DMASTUpwardPathSearch(Location location, DMASTExpressionConstant path, DMASTPath search) : base(location) {
            Path = path;
            Search = search;
        }
    }

    public class DMASTSwitchCaseRange : DMASTExpression {
        public DMASTExpression RangeStart, RangeEnd;

        public DMASTSwitchCaseRange(Location location, DMASTExpression rangeStart, DMASTExpression rangeEnd) : base(location) {
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }
    }

    public class DMASTStringFormat : DMASTExpression {
        public string Value;
        public DMASTExpression[] InterpolatedValues;

        public DMASTStringFormat(Location location, string value, DMASTExpression[] interpolatedValues) : base(location) {
            Value = value;
            InterpolatedValues = interpolatedValues;
        }
    }

    public class DMASTList : DMASTExpression {
        public DMASTCallParameter[] Values;

        public DMASTList(Location location, DMASTCallParameter[] values) : base(location) {
            Values = values;
        }
    }

    public class DMASTAddText : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTAddText(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }
    }

    public class DMASTNewList : DMASTExpression
    {
        public DMASTCallParameter[] Parameters;

        public DMASTNewList(Location location, DMASTCallParameter[] parameters) : base(location)
        {
            Parameters = parameters;
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
    }

    public class DMASTInitial : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTInitial(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }
    }

    public class DMASTIsSaved : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTIsSaved(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }
    }

    public class DMASTIsType : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTIsType(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTImplicitIsType : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTImplicitIsType(Location location, DMASTExpression expression) : base(location) {
            Expression = expression;
        }
    }

    public class DMASTLocateCoordinates : DMASTExpression {
        public DMASTExpression X, Y, Z;

        public DMASTLocateCoordinates(Location location, DMASTExpression x, DMASTExpression y, DMASTExpression z) : base(location) {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class DMASTLocate : DMASTExpression {
        public DMASTExpression Expression;
        public DMASTExpression Container;

        public DMASTLocate(Location location, DMASTExpression expression, DMASTExpression container) : base(location) {
            Expression = expression;
            Container = container;
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
    }

    public class DMASTCall : DMASTExpression {
        public DMASTCallParameter[] CallParameters, ProcParameters;

        public DMASTCall(Location location, DMASTCallParameter[] callParameters, DMASTCallParameter[] procParameters) : base(location) {
            CallParameters = callParameters;
            ProcParameters = procParameters;
        }
    }

    public class DMASTAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTNewPath : DMASTExpression {
        public DMASTPath Path;
        public DMASTCallParameter[] Parameters;

        public DMASTNewPath(Location location, DMASTPath path, DMASTCallParameter[] parameters) : base(location) {
            Path = path;
            Parameters = parameters;
        }
    }

    public class DMASTNewIdentifier : DMASTExpression {
        public DMASTIdentifier Identifier;
        public DMASTCallParameter[] Parameters;

        public DMASTNewIdentifier(Location location, DMASTIdentifier identifier, DMASTCallParameter[] parameters) : base(location) {
            Identifier = identifier;
            Parameters = parameters;
        }
    }

    public class DMASTNewDereference : DMASTExpression {
        public DMASTDereference Dereference;
        public DMASTCallParameter[] Parameters;

        public DMASTNewDereference(Location location, DMASTDereference dereference, DMASTCallParameter[] parameters) : base(location) {
            Dereference = dereference;
            Parameters = parameters;
        }
    }

    public class DMASTNewListIndex : DMASTExpression {
            public DMASTListIndex ListIdx;
            public DMASTCallParameter[] Parameters;

            public DMASTNewListIndex(Location location, DMASTListIndex listIdx, DMASTCallParameter[] parameters) : base(location) {
                ListIdx = listIdx;
                Parameters = parameters;
            }
        }

    public class DMASTNewInferred : DMASTExpression {
        public DMASTCallParameter[] Parameters;

        public DMASTNewInferred(Location location, DMASTCallParameter[] parameters) : base(location) {
            Parameters = parameters;
        }
    }

    public class DMASTNot : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTNot(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTNegate : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTNegate(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTEqual : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTEqual(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTNotEqual : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTNotEqual(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTEquivalent : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTEquivalent(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTNotEquivalent : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTNotEquivalent(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTLessThan : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLessThan(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTLessThanOrEqual : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLessThanOrEqual(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTGreaterThan : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTGreaterThan(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTGreaterThanOrEqual : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTGreaterThanOrEqual(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTMultiply : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTMultiply(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTDivide : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTDivide(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTModulus : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTModulus(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTPower : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTPower(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTAdd : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTAdd(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTSubtract : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTSubtract(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTPreIncrement : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTPreIncrement(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTPreDecrement : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTPreDecrement(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTPostIncrement : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTPostIncrement(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTPostDecrement : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTPostDecrement(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTTernary : DMASTExpression {
        public DMASTExpression A, B, C;

        public DMASTTernary(Location location, DMASTExpression a, DMASTExpression b, DMASTExpression c) : base(location) {
            A = a;
            B = b;
            C = c;
        }
    }

    public class DMASTAppend : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTAppend(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTRemove : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTRemove(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTCombine : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTCombine(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTMask : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTMask(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTLogicalAndAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLogicalAndAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTLogicalOrAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLogicalOrAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTMultiplyAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTMultiplyAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTDivideAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTDivideAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTLeftShiftAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLeftShiftAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTRightShiftAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTRightShiftAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTXorAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTXorAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTModulusAssign : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTModulusAssign(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTOr : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTOr(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTAnd : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTAnd(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTBinaryAnd : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTBinaryAnd(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTBinaryXor : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTBinaryXor(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTBinaryOr : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTBinaryOr(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTBinaryNot : DMASTExpression, DMASTUnary {
        public DMASTExpression Expression { get; set; }

        public DMASTBinaryNot(Location location, DMASTExpression a) : base(location) {
            Expression = a;
        }
    }

    public class DMASTLeftShift : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTLeftShift(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTRightShift : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTRightShift(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    public class DMASTExpressionIn : DMASTExpression, DMASTBinary {
        public DMASTExpression LHS { get; set; }
        public DMASTExpression RHS { get; set; }

        public DMASTExpressionIn(Location location, DMASTExpression lhs, DMASTExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
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
    }

    public class DMASTProcCall : DMASTExpression {
        public DMASTCallable Callable;
        public DMASTCallParameter[] Parameters;

        public DMASTProcCall(Location location, DMASTCallable callable, DMASTCallParameter[] parameters) : base(location) {
            Callable = callable;
            Parameters = parameters;
        }
    }

    public class DMASTCallParameter : DMASTNode {
        public DMASTExpression Value;
        public string Name;

        public DMASTCallParameter(Location location, DMASTExpression value, string name = null) : base(location) {
            Value = value;
            Name = name;
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
    }

    public class DMASTDereferenceProc : DMASTDereference {
        public DMASTDereferenceProc(Location location, DMASTExpression expression, string property, DereferenceType type, bool conditional) : base(location, expression, property, type, conditional) { }
    }

    public class DMASTCallableProcIdentifier : DMASTExpression, DMASTCallable {
        public string Identifier;

        public DMASTCallableProcIdentifier(Location location, string identifier) : base(location) {
            Identifier = identifier;
        }
    }

    public class DMASTCallableSuper : DMASTExpression, DMASTCallable {
        public DMASTCallableSuper(Location location) : base(location){}
    }

    public class DMASTCallableSelf : DMASTExpression, DMASTCallable {
        public DMASTCallableSelf(Location location) : base(location){}
    }

    public class DMASTCallableGlobalProc : DMASTExpression, DMASTCallable {
        public string Identifier;

        public DMASTCallableGlobalProc(Location location, string identifier) : base(location)
        {
            Identifier = identifier;
        }
    }
}
