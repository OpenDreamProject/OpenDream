using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;
using ScopeMode = DMCompiler.DM.Builders.DMExpressionBuilder.ScopeMode;

namespace DMCompiler.DM;

internal partial class DMCodeTree {
    public abstract class VarNode : INode {
        public UnknownReference? LastError;

        protected bool IsFirstPass => (LastError == null);

        public abstract void TryDefineVar(DMCompiler compiler);

        protected bool TryBuildValue(DMCompiler compiler, DMASTExpression ast, DreamPath? inferredType, DMObject dmObject, DMProc? proc,
            ScopeMode scope, [NotNullWhen(true)] out DMExpression? value) {
            try {
                compiler.DMExpressionBuilder.CurrentScopeMode = scope;

                value = compiler.DMExpressionBuilder.CreateIgnoreUnknownReference(dmObject, proc, ast, inferredType);
                if (value is UnknownReference unknownRef) {
                    LastError = unknownRef;
                    value = null;
                    return false;
                }

                return true;
            } finally {
                compiler.DMExpressionBuilder.CurrentScopeMode = ScopeMode.Normal;
            }
        }

        protected void SetVariableValue(DMCompiler compiler, DMObject dmObject, DMVariable variable, DMExpression value, bool isOverride) {
            // Typechecking
            if (!variable.ValType.MatchesType(compiler, value.ValType) && !variable.ValType.IsUnimplemented) {
                if (value is Null && !isOverride) {
                    compiler.Emit(WarningCode.ImplicitNullType, value.Location, $"{dmObject.Path}.{variable.Name}: Variable is null but not explicitly typed as nullable, append \"|null\" to \"as\". Implicitly treating as nullable.");
                    variable.ValType |= DMValueType.Null;
                } else {
                    compiler.Emit(WarningCode.InvalidVarType, value.Location, $"{dmObject.Path}.{variable.Name}: Invalid var value type {value.ValType}, expected {variable.ValType}");
                }
            }

            if (value.TryAsConstant(compiler, out var constant)) {
                variable.Value = constant;
                return;
            } else if (variable.IsConst) {
                compiler.Emit(WarningCode.HardConstContext, value.Location, "Value of const var must be a constant");
                return;
            }

            if (!IsValidRightHandSide(compiler, dmObject, value)) {
                compiler.Emit(WarningCode.BadExpression, value.Location,
                    $"Invalid initial value for \"{variable.Name}\"");
                return;
            }

            var initLoc = value.Location;
            var field = new Field(initLoc, variable, variable.ValType);
            var assign = new Assignment(initLoc, field, value);

            variable.Value = new Null(Location.Internal);
            dmObject.InitializationProcExpressions.Add(assign);
        }

        /// <returns>Whether the given value can be used as an instance variable's initial value</returns>
        private bool IsValidRightHandSide(DMCompiler compiler, DMObject dmObject, DMExpression value) {
            return value switch {
                //TODO: A better way of handling procs evaluated at compile time
                ProcCall procCall => procCall.GetTargetProc(compiler, dmObject).Proc?.Name switch {
                    "generator" => true,
                    "matrix" => true,
                    "icon" => true,
                    "file" => true,
                    "sound" => true,
                    "nameof" => true,
                    _ => false
                },

                List => true,
                DimensionalList => true,
                NewList => true,
                NewPath => true,
                Rgb => true,
                // TODO: Check for circular reference loops here
                // (Note that we do accidentally support global-field access somewhat when it gets const-folded by TryAsConstant before we get here)
                GlobalField => false,
                _ => false
            };
        }
    }

    private class ObjectVarNode(DreamPath owner, DMASTObjectVarDefinition varDef) : VarNode {
        private string VarName => varDef.Name;
        private bool IsStatic => varDef.IsStatic;

        private bool _defined;

        public override void TryDefineVar(DMCompiler compiler) {
            if (_defined)
                return;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return;

            if (AlreadyExists(compiler, dmObject)) {
                _defined = true;
                WaitingNodes.Remove(this);
                return;
            }

            if (IsStatic) {
                HandleGlobalVar(compiler, dmObject);
            } else {
                HandleInstanceVar(compiler, dmObject);
            }
        }

        public override string ToString() {
            return varDef.IsStatic ? $"var/static/{VarName}" : $"var/{VarName}";
        }

        private void HandleGlobalVar(DMCompiler compiler, DMObject dmObject) {
            var scope = IsFirstPass ? ScopeMode.FirstPassStatic : ScopeMode.Static;
            if (!TryBuildValue(compiler, varDef.Value, varDef.Type, dmObject, GlobalInitProc, scope, out var value))
                return;

            int globalId = compiler.DMObjectTree.CreateGlobal(out DMVariable global, varDef.Type, VarName, varDef.IsConst,
                varDef.ValType);

            dmObject.AddGlobalVariable(global, globalId);
            _defined = true;
            WaitingNodes.Remove(this);

            if (value.TryAsConstant(compiler, out var constant)) {
                global.Value = constant;
                return;
            } else if (!global.IsConst) {
                // Starts out as null, gets initialized by the global init proc
                global.Value = new Null(Location.Internal);
            } else {
                compiler.Emit(WarningCode.HardConstContext, value.Location, "Constant initializer required");
            }

            // Initialize its value in the global init proc
            compiler.VerbosePrint($"Adding {dmObject.Path}/var/static/{global.Name} to global init on pass {_currentPass}");
            GlobalInitProc.DebugSource(value.Location);
            value.EmitPushValue(compiler, dmObject, GlobalInitProc);
            GlobalInitProc.Assign(DMReference.CreateGlobal(globalId));
        }

        private void HandleInstanceVar(DMCompiler compiler, DMObject dmObject) {
            if (!TryBuildValue(compiler, varDef.Value, varDef.Type, dmObject, null, ScopeMode.Normal, out var value))
                return;

            var variable = new DMVariable(varDef.Type, VarName, false, varDef.IsConst, varDef.IsTmp, varDef.ValType);
            dmObject.AddVariable(variable);
            _defined = true;
            WaitingNodes.Remove(this);

            SetVariableValue(compiler, dmObject, variable, value, false);
        }

        private bool AlreadyExists(DMCompiler compiler, DMObject dmObject) {
            // "type" and "tag" can only be defined in DMStandard
            if (VarName is "type" or "tag" && !varDef.Location.InDMStandard) {
                compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                    $"Cannot redefine built-in var \"{VarName}\"");
                return true;
            }

            //DMObjects store two bundles of variables; the statics in GlobalVariables and the non-statics in Variables.
            if (dmObject.HasGlobalVariable(VarName)) {
                compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                    $"Duplicate definition of static var \"{VarName}\"");
                return true;
            } else if (dmObject.HasLocalVariable(VarName)) {
                if (!varDef.Location.InDMStandard) // Duplicate instance vars are not an error in DMStandard
                    compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                        $"Duplicate definition of var \"{VarName}\"");
                return true;
            } else if (IsStatic && VarName == "vars" && dmObject == compiler.DMObjectTree.Root) {
                compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location, "Duplicate definition of global.vars");
                return true;
            }

            return false;
        }
    }

    private class ObjectVarOverrideNode(DreamPath owner, DMASTObjectVarOverride varOverride) : VarNode {
        private string VarName => varOverride.VarName;

        private bool _finished;

        public override void TryDefineVar(DMCompiler compiler) {
            if (_finished)
                return;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return;

            DMVariable? variable = null;
            if (dmObject.HasLocalVariable(VarName)) {
                variable = dmObject.GetVariable(VarName);
            } else if (dmObject.HasGlobalVariable(VarName)) {
                compiler.Emit(WarningCode.StaticOverride, varOverride.Location,
                    $"var \"{VarName}\" cannot be overridden - it is a global var");
                _finished = true;
                WaitingNodes.Remove(this);
                return;
            }

            if (variable == null) {
                return;
            } else if (variable.IsConst) {
                compiler.Emit(WarningCode.WriteToConstant, varOverride.Location,
                    $"Var \"{VarName}\" is const and cannot be modified");
                _finished = true;
                WaitingNodes.Remove(this);
                return;
            } else if (variable.ValType.IsCompileTimeReadOnly) {
                compiler.Emit(WarningCode.WriteToConstant, varOverride.Location,
                    $"Var \"{VarName}\" is a native read-only value which cannot be modified");
                _finished = true;
                WaitingNodes.Remove(this);
                return;
            }

            variable = new DMVariable(variable);

            if (!TryBuildValue(compiler, varOverride.Value, variable.Type, dmObject, null, ScopeMode.Normal, out var value))
                return;

            if (VarName == "tag" && dmObject.IsSubtypeOf(DreamPath.Datum) && !compiler.Settings.NoStandard)
                compiler.Emit(WarningCode.InvalidOverride, varOverride.Location,
                    "var \"tag\" cannot be set to a value at compile-time");

            dmObject.VariableOverrides[variable.Name] = variable;
            _finished = true;
            WaitingNodes.Remove(this);

            try {
                SetVariableValue(compiler, dmObject, variable, value, true);
            } finally {
                compiler.DMExpressionBuilder.CurrentScopeMode = ScopeMode.Normal;
            }
        }

        public override string ToString() {
            return $"{varOverride.VarName} {{override}}";
        }
    }

    private class ProcGlobalVarNode(DreamPath owner, DMProc proc, DMASTProcStatementVarDeclaration varDecl) : VarNode {
        private bool _defined;

        public override void TryDefineVar(DMCompiler compiler) {
            if (_defined)
                return;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return;

            DMExpression? value = null;
            if (varDecl.Value != null) {
                var scope = IsFirstPass ? ScopeMode.FirstPassStatic : ScopeMode.Static;
                if (!TryBuildValue(compiler, varDecl.Value, varDecl.Type, dmObject, proc, scope, out value))
                    return;
            }

            int globalId = compiler.DMObjectTree.CreateGlobal(out DMVariable global, varDecl.Type, varDecl.Name, varDecl.IsConst,
                varDecl.ValType);

            global.Value = new Null(Location.Internal);
            proc.AddGlobalVariable(global, globalId);
            _defined = true;
            WaitingNodes.Remove(this);

            if (value != null) {
                // Initialize its value in the global init proc
                compiler.VerbosePrint($"Adding {dmObject.Path}/proc/{proc.Name}/var/static/{global.Name} to global init on pass {_currentPass}");
                GlobalInitProc.DebugSource(value.Location);
                value.EmitPushValue(compiler, dmObject, GlobalInitProc);
                GlobalInitProc.Assign(DMReference.CreateGlobal(globalId));
            }
        }

        public override string ToString() {
            return $"var/static/{varDecl.Name}";
        }
    }

    public void AddObjectVar(DreamPath owner, DMASTObjectVarDefinition varDef) {
        var node = GetDMObjectNode(owner);
        var varNode = new ObjectVarNode(owner, varDef);

        node.Children.Add(varNode);
        WaitingNodes.Add(varNode);
    }

    public void AddObjectVarOverride(DreamPath owner, DMASTObjectVarOverride varOverride) {
        var node = GetDMObjectNode(owner);

        // parent_type is not an actual var override, and must be applied as soon as the object is created
        if (varOverride.VarName == "parent_type") {
            if (ParentTypes.ContainsKey(owner)) {
                compiler.Emit(WarningCode.InvalidOverride, varOverride.Location,
                    $"{owner} already has its parent_type set. This override is ignored.");
                return;
            }

            if (varOverride.Value is not DMASTConstantPath parentType) {
                compiler.Emit(WarningCode.BadExpression, varOverride.Value.Location, "Expected a constant path");
                return;
            }

            ParentTypes.Add(owner, parentType.Value.Path);
            return;
        }

        var varNode = new ObjectVarOverrideNode(owner, varOverride);
        node.Children.Add(varNode);
        WaitingNodes.Add(varNode);
    }
}
