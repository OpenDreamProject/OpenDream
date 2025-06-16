using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Builders;
using DMCompiler.DM.Expressions;
using ScopeMode = DMCompiler.DM.Builders.DMExpressionBuilder.ScopeMode;

namespace DMCompiler.DM;

internal partial class DMCodeTree {
    public abstract class VarNode : INode {
        public UnknownReference? LastError;

        protected bool IsFirstPass => (LastError == null);

        public abstract bool TryDefineVar(DMCompiler compiler, int pass);

        protected bool TryBuildValue(ExpressionContext ctx, DMASTExpression ast, DreamPath? inferredType,
            ScopeMode scope, [NotNullWhen(true)] out DMExpression? value) {
            var exprBuilder = new DMExpressionBuilder(ctx, scope);

            value = exprBuilder.CreateIgnoreUnknownReference(ast, inferredType);
            if (value is UnknownReference unknownRef) {
                LastError = unknownRef;
                value = null;
                return false;
            }

            return true;
        }

        protected void SetVariableValue(DMCompiler compiler, DMObject dmObject, DMVariable variable, DMExpression value, bool isOverride) {
            // Typechecking
            if (!variable.ValType.MatchesType(compiler, value.ValType) && !variable.ValType.IsUnimplemented && !variable.ValType.IsUnsupported && !variable.ValType.Type.HasFlag(DMValueType.NoConstFold)) {
                if (value is Null && !isOverride) {
                    compiler.Emit(WarningCode.ImplicitNullType, value.Location, $"{dmObject.Path}.{variable.Name}: Variable is null but not explicitly typed as nullable, append \"|null\" to \"as\". Implicitly treating as nullable.");
                    variable.ValType |= DMValueType.Null;
                } else {
                    compiler.Emit(WarningCode.InvalidVarType, value.Location, $"{dmObject.Path}.{variable.Name}: Invalid var value type {value.ValType}, expected {variable.ValType}");
                }
            }

            if (value.TryAsConstant(compiler, out var constant)) {
                variable.Value = constant;

                // We want to continue with putting this in the init proc if a base type initializes it to another value
                if (!isOverride || !dmObject.IsRuntimeInitialized(variable.Name)) {
                    return;
                }
            } else if (variable.IsConst) {
                compiler.Emit(WarningCode.HardConstContext, value.Location, "Value of const var must be a constant");
                return;
            } else if (!IsValidRightHandSide(compiler, dmObject, value)) {
                compiler.Emit(WarningCode.BadExpression, value.Location,
                    $"Invalid initial value for \"{variable.Name}\"");
                return;
            }

            var initLoc = value.Location;
            var field = new Field(initLoc, variable, variable.ValType);
            var assign = new Assignment(initLoc, field, value);

            variable.Value = new Null(Location.Internal);
            dmObject.InitializationProcAssignments.Add((variable.Name, assign));
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
                    "filter" => true,
                    _ => false
                },

                AList => true,
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

        public override bool TryDefineVar(DMCompiler compiler, int pass) {
            if (_defined)
                return true;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return false;

            if (CheckCantDefine(compiler, dmObject)) {
                _defined = true;
                return true;
            }

            if (IsStatic) {
                return HandleGlobalVar(compiler, dmObject, pass);
            } else {
                return HandleInstanceVar(compiler, dmObject);
            }
        }

        public override string ToString() {
            return varDef.IsStatic ? $"var/static/{VarName}" : $"var/{VarName}";
        }

        private bool HandleGlobalVar(DMCompiler compiler, DMObject dmObject, int pass) {
            var scope = IsFirstPass ? ScopeMode.FirstPassStatic : ScopeMode.Static;
            if (!TryBuildValue(new(compiler, dmObject, compiler.GlobalInitProc), varDef.Value, varDef.Type, scope, out var value))
                return false;

            int globalId = compiler.DMObjectTree.CreateGlobal(out DMVariable global, varDef.Type, VarName, varDef.IsConst,
                varDef.IsFinal, varDef.ValType);

            dmObject.AddGlobalVariable(global, globalId);
            _defined = true;

            if (value.TryAsConstant(compiler, out var constant)) {
                global.Value = constant;
                return true;
            } else if (!global.IsConst) {
                // Starts out as null, gets initialized by the global init proc
                global.Value = new Null(Location.Internal);
            } else {
                compiler.Emit(WarningCode.HardConstContext, value.Location, "Constant initializer required");
            }

            // Initialize its value in the global init proc
            compiler.VerbosePrint($"Adding {dmObject.Path}/var/static/{global.Name} to global init on pass {pass}");
            compiler.GlobalInitProc.DebugSource(value.Location);
            value.EmitPushValue(new(compiler, dmObject, compiler.GlobalInitProc));
            compiler.GlobalInitProc.Assign(DMReference.CreateGlobal(globalId));
            return true;
        }

        private bool HandleInstanceVar(DMCompiler compiler, DMObject dmObject) {
            if (!TryBuildValue(new(compiler, dmObject, null), varDef.Value, varDef.Type, ScopeMode.Normal, out var value))
                return false;

            var variable = new DMVariable(varDef.Type, VarName, false, varDef.IsConst, varDef.IsFinal, varDef.IsTmp, varDef.ValType);
            dmObject.AddVariable(variable);
            _defined = true;

            SetVariableValue(compiler, dmObject, variable, value, false);
            return true;
        }

        private bool CheckCantDefine(DMCompiler compiler, DMObject dmObject) {
            if (!compiler.Settings.NoStandard) {
                var inStandard = varDef.Location.InDMStandard;

                // "type" and "tag" can only be defined in DMStandard
                if (VarName is "type" or "tag" && !inStandard) {
                    compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                        $"Cannot redefine built-in var \"{VarName}\"");
                    return true;
                }

                // Vars on /world, /list, and /alist can only be defined in DMStandard
                if ((dmObject.Path == DreamPath.World || dmObject.Path == DreamPath.List || dmObject.Path == DreamPath.AList) && !inStandard) {
                    compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                        $"Cannot define a var on type {dmObject.Path}");
                    return true;
                }
            }

            //DMObjects store two bundles of variables; the statics in GlobalVariables and the non-statics in Variables.
            if (dmObject.HasGlobalVariable(VarName)) {
                compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                    $"Duplicate definition of static var \"{VarName}\"");
                return true;
            } else if (dmObject.HasLocalVariable(VarName)) {
                if (!varDef.Location.InDMStandard) { // Duplicate instance vars are not an error in DMStandard
                    var variable = dmObject.GetVariable(VarName);
                    if(variable!.Value is not null)
                        compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                        $"Duplicate definition of var \"{VarName}\". Previous definition at {variable.Value.Location}");
                    else
                        compiler.Emit(WarningCode.InvalidVarDefinition, varDef.Location,
                        $"Duplicate definition of var \"{VarName}\"");
                }

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

        public override bool TryDefineVar(DMCompiler compiler, int pass) {
            if (_finished)
                return true;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return false;

            DMVariable? variable = null;
            if (dmObject.HasLocalVariable(VarName)) {
                variable = dmObject.GetVariable(VarName);
            } else if (dmObject.HasGlobalVariable(VarName)) {
                compiler.Emit(WarningCode.StaticOverride, varOverride.Location,
                    $"var \"{VarName}\" cannot be overridden - it is a global var");
                _finished = true;
                return true;
            }

            if (variable == null) {
                return false;
            } else if (variable.IsConst) {
                compiler.Emit(WarningCode.WriteToConstant, varOverride.Location,
                    $"Var \"{VarName}\" is const and cannot be modified");
                _finished = true;
                return true;
            } else if (variable.IsFinal) {
                compiler.Emit(WarningCode.FinalOverride, varOverride.Location,
                    $"Var \"{VarName}\" is final and cannot be modified");
                _finished = true;
                return true;
            } else if (variable.ValType.IsCompileTimeReadOnly) {
                compiler.Emit(WarningCode.WriteToConstant, varOverride.Location,
                    $"Var \"{VarName}\" is a native read-only value which cannot be modified");
                _finished = true;
                return true;
            }

            variable = new DMVariable(variable);

            if (!TryBuildValue(new(compiler, dmObject, null), varOverride.Value, variable.Type, ScopeMode.Normal, out var value))
                return false;

            if (VarName == "tag" && dmObject.IsSubtypeOf(DreamPath.Datum) && !compiler.Settings.NoStandard)
                compiler.Emit(WarningCode.InvalidOverride, varOverride.Location,
                    "var \"tag\" cannot be set to a value at compile-time");

            dmObject.VariableOverrides[variable.Name] = variable;
            _finished = true;

            SetVariableValue(compiler, dmObject, variable, value, true);
            return true;
        }

        public override string ToString() {
            return $"{varOverride.VarName} {{override}}";
        }
    }

    private class ProcGlobalVarNode(DreamPath owner, DMProc proc, DMASTProcStatementVarDeclaration varDecl) : VarNode {
        private bool _defined;

        public override bool TryDefineVar(DMCompiler compiler, int pass) {
            if (_defined)
                return true;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return false;

            DMExpression? value = null;
            if (varDecl.Value != null) {
                var scope = IsFirstPass ? ScopeMode.FirstPassStatic : ScopeMode.Static;
                if (!TryBuildValue(new(compiler, dmObject, proc), varDecl.Value, varDecl.Type, scope, out value))
                    return false;
            }

            int globalId = compiler.DMObjectTree.CreateGlobal(out DMVariable global, varDecl.Type, varDecl.Name, varDecl.IsConst,
                false, varDecl.ValType);

            global.Value = new Null(Location.Internal);
            proc.AddGlobalVariable(global, globalId);
            _defined = true;

            if (value != null) {
                // Initialize its value in the global init proc
                compiler.VerbosePrint($"Adding {dmObject.Path}/proc/{proc.Name}/var/static/{global.Name} to global init on pass {pass}");
                compiler.GlobalInitProc.DebugSource(value.Location);
                value.EmitPushValue(new(compiler, dmObject, compiler.GlobalInitProc));
                compiler.GlobalInitProc.Assign(DMReference.CreateGlobal(globalId));
            }

            return true;
        }

        public override string ToString() {
            return $"var/static/{varDecl.Name}";
        }
    }

    public void AddObjectVar(DreamPath owner, DMASTObjectVarDefinition varDef) {
        var node = GetDMObjectNode(owner);
        var varNode = new ObjectVarNode(owner, varDef);

        node.Children.Add(varNode);
        _waitingNodes.Add(varNode);
    }

    public void AddObjectVarOverride(DreamPath owner, DMASTObjectVarOverride varOverride) {
        var node = GetDMObjectNode(owner);

        // parent_type is not an actual var override, and must be applied as soon as the object is created
        if (varOverride.VarName == "parent_type") {
            if (_parentTypes.ContainsKey(owner)) {
                _compiler.Emit(WarningCode.InvalidOverride, varOverride.Location,
                    $"{owner} already has its parent_type set. This override is ignored.");
                return;
            }

            if (varOverride.Value is not DMASTConstantPath parentType) {
                _compiler.Emit(WarningCode.BadExpression, varOverride.Value.Location, "Expected a constant path");
                return;
            }

            _parentTypes.Add(owner, parentType.Value.Path);
            return;
        }

        var varNode = new ObjectVarOverrideNode(owner, varOverride);
        node.Children.Add(varNode);
        _waitingNodes.Add(varNode);
    }
}
