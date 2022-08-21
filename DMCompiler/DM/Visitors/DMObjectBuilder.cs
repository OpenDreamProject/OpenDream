using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMObjectBuilder {
        public void BuildObjectTree(DMASTFile astFile) {
            DMObjectTree.Reset();
            ProcessFile(astFile);

            // TODO Nuke this pass
            foreach (DMObject dmObject in DMObjectTree.AllObjects) {
                dmObject.CreateInitializationProc();
            }

            foreach (DMProc proc in DMObjectTree.AllProcs)
                proc.Compile();

            DMObjectTree.CreateGlobalInitProc();
        }

        private void ProcessFile(DMASTFile file) {
            ProcessBlockInner(file.BlockInner, DMObjectTree.Root);
        }

        private void ProcessBlockInner(DMASTBlockInner blockInner, DMObject currentObject) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                try {
                    ProcessStatement(statement, ref currentObject);
                } catch (CompileErrorException e) {
                    DMCompiler.Error(e.Error);
                }
            }
        }

        private void ProcessStatement(DMASTStatement statement, ref DMObject currentObject) {
            switch (statement) {
                case DMASTObjectDefinition objectDefinition: ProcessObjectDefinition(objectDefinition, ref currentObject); break;

                //The above are the only cases where the currentObject could be set to a novel, new() value.
                //The rest can just have it be passed as mutable ref like normal.
                case DMASTObjectVarDefinition varDefinition: ProcessVarDefinition(varDefinition); break;
                case DMASTObjectVarOverride varOverride: ProcessVarOverride(varOverride); break;
                case DMASTProcDefinition procDefinition: ProcessProcDefinition(procDefinition, currentObject); break;
                case DMASTMultipleObjectVarDefinitions multipleVarDefinitions: {
                    foreach (DMASTObjectVarDefinition varDefinition in multipleVarDefinitions.VarDefinitions) {
                        ProcessVarDefinition(varDefinition);
                    }

                    break;
                }
                default: throw new ArgumentException("Invalid object statement");
            }
        }

        private void ProcessObjectDefinition(DMASTObjectDefinition objectDefinition, ref DMObject currentObject) {

            DMCompiler.VerbosePrint($"Generating {objectDefinition.Path}");
            currentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock, currentObject);
        }

        private void ProcessVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMVariable variable;
            DMObject varObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);
            //DMObjects store two bundles of variables; the statics in GlobalVariables and the non-statics in Variables.
            //Lets check if we're duplicating a definition, first.
            if (varObject.HasGlobalVariable(varDefinition.Name))
            {
                DMCompiler.Error(new CompilerError(varDefinition.Location, $"Duplicate definition of static var \"{varDefinition.Name}\""));
                variable = varObject.GetGlobalVariable(varDefinition.Name);
            }
            else if (varObject.HasLocalVariable(varDefinition.Name))
            {
                DMCompiler.Error(new CompilerError(varDefinition.Location, $"Duplicate definition of var \"{varDefinition.Name}\""));
                variable = varObject.GetVariable(varDefinition.Name);
            }
            //TODO: Fix this else-if chaining once _currentObject is refactored out of DMObjectBuilder.
            else if (varDefinition.IsStatic) { // static

                //make sure this static doesn't already exist first
                if(DoesOverrideGlobalVars(varDefinition)) // Some snowflake behaviour for global.vars
                {
                    DMCompiler.Error(new CompilerError(varDefinition.Location, "Duplicate definition of global.vars"));
                    //We can't salvage any part of this definition, since global.vars doesn't technically even exist, so lets just return
                    return;
                }
                //otherwise create
                variable = varObject.CreateGlobalVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsConst);
            } else { // not static
                variable = new DMVariable(varDefinition.Type, varDefinition.Name, false, varDefinition.IsConst);
                varObject.Variables[variable.Name] = variable;
            }

            try {
                SetVariableValue(varObject, variable, varDefinition.Value, varDefinition.ValType);
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }
        }

        private void ProcessVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject varObject = DMObjectTree.GetDMObject(varOverride.ObjectPath);

            try
            {
                switch (varOverride.VarName)
                {
                    case "parent_type":
                    {
                        DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                        if (parentType == null) throw new CompileErrorException(varOverride.Location, "Expected a constant path");
                            varObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
                        return;
                    }
                    case "tag":
                        DMCompiler.Error(new CompilerError(varOverride.Location, "tag: may not be set at compile-time"));
                        return;
                }
                DMVariable variable;
                if (varObject.HasLocalVariable(varOverride.VarName))
                {
                    variable = varObject.GetVariable(varOverride.VarName);
                }
                else // Shouldn't happen, ideally
                {
                    DMCompiler.Warning(new CompilerWarning(varOverride.Location, $"Override of var {varOverride.VarName} found before variable declaration. This isn't supposed to happen!"));
                    variable = new DMVariable(null, varOverride.VarName, false, false);
                }
                OverrideVariableValue(varObject, variable, varOverride.Value);
                varObject.VariableOverrides[variable.Name] = variable;
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }
        }

        private void ProcessProcDefinition(DMASTProcDefinition procDefinition, DMObject currentObject) {
            string procName = procDefinition.Name;
            DMObject dmObject = currentObject; // Default value if we can't discern its object

            try {
                if (procDefinition.ObjectPath.HasValue) {
                    dmObject = DMObjectTree.GetDMObject(currentObject.Path.Combine(procDefinition.ObjectPath.Value));
                }

                if (!procDefinition.IsOverride && dmObject.HasProc(procName)) {
                    throw new CompileErrorException(procDefinition.Location, $"Type {dmObject.Path} already has a proc named \"{procName}\"");
                }

                DMProc proc;

                if (procDefinition.ObjectPath == null) {
                    if (DMObjectTree.TryGetGlobalProc(procDefinition.Name, out _)) {
                        throw new CompileErrorException(new CompilerError(procDefinition.Location, $"proc {procDefinition.Name} is already defined in global scope"));
                    }

                    proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);
                    DMObjectTree.AddGlobalProc(proc.Name, proc.Id);
                } else {
                    proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);
                    dmObject.AddProc(procName, proc);
                }

                if (procDefinition.Body != null) {
                    foreach (var stmt in GetStatements(procDefinition.Body)) {
                        // TODO multiple var definitions.
                        if (stmt is DMASTProcStatementVarDeclaration varDeclaration && varDeclaration.IsGlobal) {
                            DMVariable variable = proc.CreateGlobalVariable(varDeclaration.Type, varDeclaration.Name, varDeclaration.IsConst);
                            variable.Value = new Expressions.Null(varDeclaration.Location);

                            if (varDeclaration.Value != null) {
                                DMVisitorExpression._scopeMode = "static";
                                DMExpression expression = DMExpression.Create(dmObject, proc, varDeclaration.Value, varDeclaration.Type);
                                DMVisitorExpression._scopeMode = "normal";
                                DMObjectTree.AddGlobalInitAssign(dmObject, proc.GetGlobalVariableId(varDeclaration.Name).Value, expression);
                            }
                        }
                    }
                }

                if (procDefinition.IsVerb && (dmObject.IsSubtypeOf(DreamPath.Atom) || dmObject.IsSubtypeOf(DreamPath.Client)) && !DMCompiler.Settings.NoStandard) {
                    Expressions.Field field = new Expressions.Field(Location.Unknown, dmObject.GetVariable("verbs"));
                    DreamPath procPath = new DreamPath(".proc/" + procName);
                    Expressions.Append append = new Expressions.Append(Location.Unknown, field, new Expressions.Path(Location.Unknown, procPath));

                    dmObject.InitializationProcExpressions.Add(append);
                }
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }
        }

        // TODO Move this to an appropriate location
        static public IEnumerable<DMASTProcStatement> GetStatements(DMASTProcBlockInner block) {
            foreach (var stmt in block.Statements) {
                yield return stmt;
                List<DMASTProcBlockInner> recurse;
                switch (stmt) {
                    case DMASTProcStatementSpawn ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementIf ps: recurse = new() { ps.Body, ps.ElseBody }; break;
                    case DMASTProcStatementFor ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementWhile ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementDoWhile ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementInfLoop ps: recurse = new() { ps.Body }; break;
                    // TODO Good luck if you declare a static var inside a switch
                    case DMASTProcStatementSwitch ps: {
                        recurse = new();
                        foreach (var swcase in ps.Cases) {
                            recurse.Add(swcase.Body);
                        }
                        break;
                    }
                    case DMASTProcStatementTryCatch ps: recurse = new() { ps.TryBody, ps.CatchBody }; break;
                    default: recurse = new(); break;
                }
                foreach (var subblock in recurse) {
                    if (subblock == null) { continue; }
                    foreach (var substmt in GetStatements(subblock)) {
                        yield return substmt;
                    }
                }
            }

        }

        /// <summary>
        /// A snowflake helper proc which determines whether the given definition would be a duplication definition of global.vars.<br/>
        /// It exists because global.vars is not a "real" global but rather a construct indirectly implemented via PushGlobals et al.
        /// </summary>
        private bool DoesOverrideGlobalVars(DMASTObjectVarDefinition varDefinition)
        {
            if (varDefinition == null) return false;
            return varDefinition.IsStatic && varDefinition.Name == "vars" && varDefinition.ObjectPath == DreamPath.Root;
        }

        /// <summary>
        /// A filter proc above <see cref="SetVariableValue"/> <br/>
        /// which checks first to see if overriding this thing's value is valid (as in the case of const and <see cref="DMValueType.CompiletimeReadonly"/>)
        /// </summary>
        private void OverrideVariableValue(DMObject currentObject, DMVariable variable, DMASTExpression value, DMValueType valType = DMValueType.Anything)
        {
            if(variable.IsConst)
            {
                DMCompiler.Error(new CompilerError(value.Location, $"Var {variable.Name} is const and cannot be modified"));
                return;
            }
            if((valType & DMValueType.CompiletimeReadonly) == DMValueType.CompiletimeReadonly)
            {
                DMCompiler.Error(new CompilerError(value.Location, $"Var {variable.Name} is a native read-only value which cannot be modified"));
            }
            SetVariableValue(currentObject, variable, value, valType);
        }

        /// <summary>
        /// Handles setting a variable to a value (when called by itself, this assumes the statement is a declaration and not a re-assignment)
        /// </summary>
        /// <exception cref="CompileErrorException"></exception>
        private void SetVariableValue(DMObject currentObject, DMVariable variable, DMASTExpression value, DMValueType valType = DMValueType.Anything) {
            DMVisitorExpression._scopeMode = variable.IsGlobal ? "static" : "normal";
            DMExpression expression = DMExpression.Create(currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, variable.Type);
            DMVisitorExpression._scopeMode = "normal";
            expression.ValType = valType;

            if (expression.TryAsConstant(out var constant)) {
                variable.Value = constant;
                return;
            }

            if (variable.IsConst) {
                throw new CompileErrorException(value.Location, "Value of const var must be a constant");
            }

            //Whether this should be initialized at runtime
            bool isValid = expression switch {
                //TODO: A better way of handling procs evaluated at compile time
                Expressions.ProcCall procCall => procCall.GetTargetProc(currentObject).Proc?.Name switch {
                    "rgb" => true,
                    "generator" => true,
                    "matrix" => true,
                    "icon" => true,
                    "file" => true,
                    "sound" => true,
                    _ => variable.IsGlobal
                },

                Expressions.List => true,
                Expressions.NewList => true,
                Expressions.NewPath => true,
                Expressions.GlobalField => variable.IsGlobal, // Global set to another global
                Expressions.StringFormat => variable.IsGlobal,
                Expressions.Add => variable.IsGlobal,
                Expressions.Subtract => variable.IsGlobal,
                _ => false
            };

            if (isValid) {
                variable.Value = new Expressions.Null(Location.Internal);
                EmitInitializationAssign(currentObject, variable, expression);
            } else {
                throw new CompileErrorException(value.Location, $"Invalid initial value for \"{variable.Name}\"");
            }
        }

        private void EmitInitializationAssign(DMObject currentObject, DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) {
                int? globalId = currentObject.GetGlobalVariableId(variable.Name);
                if (globalId == null) throw new Exception($"Invalid global {currentObject.Path}.{variable.Name}");

                DMObjectTree.AddGlobalInitAssign(currentObject, globalId.Value, expression);
            } else {
                Expressions.Field field = new Expressions.Field(Location.Unknown, variable);
                Expressions.Assignment assign = new Expressions.Assignment(Location.Unknown, field, expression);

                currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
