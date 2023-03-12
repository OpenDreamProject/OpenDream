using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    static class DMObjectBuilder {
        /// <summary>
        /// In DM, the definition of a base class may occur way after the definition of (perhaps numerous) derived classes. <br/>
        /// At the time that we first evaluate the derived class, we do not know some important information, like the implicit type of certain things.<br/>
        /// This event allows for delaying variable override evaluation until after we know what it is. </summary>
        /// <remarks>
        /// Further, it just so happens to also act as a container that can enumerate all objects which were expecting a definition but never got one.
        /// </remarks>
        public static event EventHandler<DMVariable> VarDefined; // Fires if we define a new variable.
        /// <summary>
        /// A running collection of types that we need to find a definition for. <br/>
        /// Comes up most especially when a var/some/type/x is defined before we know what /some/type even is. <br/>
        /// The Location value can just be the first instance of this definition being promised, for errors' sake.
        /// </summary>
        public static Dictionary<DreamPath, Location> AwaitedObjectDefinitions = new();

        /// <summary>
        /// Similiar to <see cref="AwaitedObjectDefinitions"/> except for procs that are overridden before they are defined.
        /// </summary>
        public static Dictionary<string, DMObject> AwaitedProcDefinitions = new();

        public static void Reset() {
            DMObjectTree.Reset(); // Blank the object tree
            VarDefined = null;
            AwaitedObjectDefinitions.Clear(); // Need to do this since this static is re-used during unit testing :^)
            AwaitedProcDefinitions.Clear();
        }
        public static void BuildObjectTree(DMASTFile astFile) {
            Reset();
            ProcessFile(astFile); // generate it

            if (VarDefined != null) // This means some listeners are remaining, which means that variables were overridden but not defined! Bad!
            {
                foreach(var method in VarDefined.GetInvocationList()) // For every object listening
                {
                    object? obj = method.Target;
                    DMObject? realObj = (DMObject?)obj;
                    if(realObj != null)
                    {
                        Robust.Shared.Utility.DebugTools.Assert(realObj.danglingOverrides is not null);
                        foreach(DMASTObjectVarOverride varOverride in realObj.danglingOverrides)
                        {
                            DMCompiler.Emit(WarningCode.DanglingOverride, varOverride.Location, $"Cannot override undefined var {varOverride.VarName}");
                        }
                    }
                }
            }

            //Lets check in on all the types we found promised before and see if we actually got them. :^)
            //Note: Dynamically removing types as we find them would've made the compiler 4x slower, per some tests I did.
            //      It's much more performant to do it this way.
            foreach(var pair in AwaitedObjectDefinitions) {
                if(!DMObjectTree.TryGetTypeId(pair.Key,out var _))
                    DMCompiler.Emit(WarningCode.DanglingVarType, pair.Value, $"Definition for path {pair.Key} not found");
            }

            //And now all the proc definitions we were promised
            foreach (var pair in AwaitedProcDefinitions) {
                if (pair.Value.IsRoot) { // Have to do this since DMObjectTree is what holds global procs, not the root DMObject, interestingly enough
                    if(!DMObjectTree.SeenGlobalProcDefinition.Contains(pair.Key)) { // If we didn't see a definition for it :(
                        int ID = DMObjectTree.GlobalProcs[pair.Key];
                        DMProc proc = DMObjectTree.AllProcs[ID];
                        DMCompiler.Emit(WarningCode.DanglingOverride, proc.Location, $"Definition for global proc {pair.Key} not found");
                    }
                    continue;
                }
                if (!pair.Value.HasProcDefined(pair.Key)) {
                    DMProc proc = DMObjectTree.AllProcs[pair.Value.GetProcs(pair.Key)[0]];
                    DMCompiler.Emit(WarningCode.DanglingOverride, proc.Location, $"Definition for proc {pair.Key} on type {pair.Value.Path} not found");
                }
            }

            // TODO Nuke this pass
            // (Note that VarDefined's lazy evaluation behaviour is dependent on happening BEFORE the the initialization proc statements are emitted)
            foreach (DMObject dmObject in DMObjectTree.AllObjects) {
                dmObject.CreateInitializationProc();
            }

            foreach (DMProc proc in DMObjectTree.AllProcs)
                proc.Compile();

            DMObjectTree.CreateGlobalInitProc();
        }

        private static void ProcessFile(DMASTFile file) {
            ProcessBlockInner(file.BlockInner, DMObjectTree.Root);
        }

        private static void ProcessBlockInner(DMASTBlockInner blockInner, DMObject currentObject) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                try {
                    ProcessStatement(statement, currentObject);
                } catch (CompileErrorException e) {
                    DMCompiler.Emit(e.Error);
                }
            }
        }

        private static void ProcessStatement(DMASTStatement statement, DMObject currentObject) {
            switch (statement) {
                case DMASTObjectDefinition objectDefinition: ProcessObjectDefinition(objectDefinition); break;

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
                default: throw new CompileAbortException(statement.Location, "Invalid object statement");
            }
        }

        private static void ProcessObjectDefinition(DMASTObjectDefinition objectDefinition) {

            DMCompiler.VerbosePrint($"Generating {objectDefinition.Path}");
            DMObject newCurrentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock, newCurrentObject);
        }

        private static void ProcessVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMVariable variable;
            DMObject varObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);
            //DMObjects store two bundles of variables; the statics in GlobalVariables and the non-statics in Variables.
            //Lets check if we're duplicating a definition, first.
            if (varObject.HasGlobalVariable(varDefinition.Name))
            {
                DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, $"Duplicate definition of static var \"{varDefinition.Name}\"");
                variable = varObject.GetGlobalVariable(varDefinition.Name);
            }
            else if (varObject.HasLocalVariable(varDefinition.Name))
            {
                if(!DoesDefineSnowflakeVars(varDefinition, varObject))
                    DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, $"Duplicate definition of var \"{varDefinition.Name}\"");
                variable = varObject.GetVariable(varDefinition.Name);
            }
            //TODO: Fix this else-if chaining once _currentObject is refactored out of DMObjectBuilder.
            else if (varDefinition.IsStatic) { // static

                //make sure this static doesn't already exist first
                if(DoesOverrideGlobalVars(varDefinition)) // Some snowflake behaviour for global.vars
                {
                    DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, "Duplicate definition of global.vars");
                    //We can't salvage any part of this definition, since global.vars doesn't technically even exist, so lets just return
                    return;
                }
                //otherwise create
                variable = varObject.CreateGlobalVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsConst, varDefinition.ValType);
            } else { // not static
                variable = new DMVariable(varDefinition.Type, varDefinition.Name, false, varDefinition.IsConst,varDefinition.ValType);
                varObject.Variables[variable.Name] = variable;
            }

            //Check if this var definition implies a type we don't know about yet
            if(variable.Type != null && !DMObjectTree.TryGetTypeId(variable.Type.Value,out var _)) {
                AwaitedObjectDefinitions.TryAdd(variable.Type.Value, varDefinition.Location);
            }

            try {
                SetVariableValue(varObject, ref variable, varDefinition.Value);
                VarDefined?.Invoke(varObject, variable); // FIXME: God there HAS to be a better way of doing this
            } catch (CompileErrorException e) {
                DMCompiler.Emit(e.Error);
            }
        }

        private static void ProcessVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject varObject = DMObjectTree.GetDMObject(varOverride.ObjectPath);

            try
            {
                switch (varOverride.VarName) // Keep in mind that anything here, by default, affects all objects, even those who don't inherit from /datum
                {
                    case "parent_type":
                    {
                        DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                        if (parentType == null) throw new CompileErrorException(varOverride.Location, "Expected a constant path");
                        varObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
                        return;
                    }
                    case "tag":
                    {
                        if(varObject.IsSubtypeOf(DreamPath.Datum))
                        {
                            throw new CompileErrorException(varOverride.Location, "var \"tag\" cannot be set to a value at compile-time");
                        }
                        break;
                    }
                }
                DMVariable variable;
                if (varObject.HasLocalVariable(varOverride.VarName))
                {
                    variable = varObject.GetVariable(varOverride.VarName);
                }
                else if (varObject.HasGlobalVariable(varOverride.VarName))
                {
                    variable = varObject.GetGlobalVariable(varOverride.VarName);
                    DMCompiler.Emit(WarningCode.StaticOverride, varOverride.Location, $"var \"{varOverride.VarName}\" cannot be overridden - it is a global var");
                }
                else // So this is an awkward point where we have to be a little bit silly.
                {
                    //So, this override cannot be emitted until we know what the heck the DMVariable is supposed to be.
                    //To do that, we are now going to cache this var override and wait for our parent class to be defined, so we can know wtf it is and resume!
                    if(varObject.Path == DreamPath.Root) // As per DM, we should just error out if a root global var is overwritten before it is defined.
                    {
                        DMCompiler.Emit(WarningCode.ItemDoesntExist, varOverride.Location, $"var \"{varOverride.VarName}\" is not declared");
                        return; // don't do the fancy event stuff if we're root
                    }
                    varObject.WaitForLateVarDefinition(varOverride);
                    return;
                }
                OverrideVariableValue(varObject, ref variable, varOverride.Value);
                varObject.VariableOverrides[variable.Name] = variable;
            } catch (CompileErrorException e) {
                DMCompiler.Emit(e.Error);
            }
        }

        private static void ProcessProcDefinition(DMASTProcDefinition procDefinition, DMObject currentObject) {
            string procName = procDefinition.Name;
            try {
                DMObject dmObject = DMObjectTree.GetDMObject(currentObject.Path.Combine(procDefinition.ObjectPath));
                bool hasProc = dmObject.HasProc(procName); // Trying to avoid calling this several times since it's recursive and maybe slow
                if (!procDefinition.IsOverride && hasProc) { // If this is a define and we already had a proc somehow
                    if(!dmObject.HasProcNoInheritence(procName)) { // If we're inheriting this proc (so making a new define for it at our level is stupid)
                        DMCompiler.Emit(WarningCode.DuplicateProcDefinition, procDefinition.Location, $"Type {dmObject.Path} already inherits a proc named \"{procName}\" and cannot redefine it");
                        return; // TODO: Maybe fallthrough since this error is a little pedantic?
                    }
                    //Otherwise, it's ok
                }
                /*
                    So the way that BYOND handles the distinction between definitions and overrides on the same type is kinda strange.
                    There is NO visible dominance that one has over the other, except that the last one found is the first definition invoked when called.

                    The only grammatical purpose the /proc/ phrase in one of the procs does,
                    is to mark that this type should be the first one in its inheritence to have that proc defined.
                    Nothing else.
                */
                if (procDefinition.IsOverride && !hasProc) // If an override for this proc was found before its definition
                    AwaitedProcDefinitions.TryAdd(procName, dmObject); // Remember to check that we eventually found a definition, later :)

                DMProc proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);

                if (procDefinition.ObjectPath == DreamPath.Root) {
                    if(procDefinition.IsOverride) {
                        DMCompiler.Emit(WarningCode.InvalidOverride, procDefinition.Location, $"Global procs cannot be overridden - '{procDefinition.Name}' override will be ignored");
                        //Continue processing the proc anyhoo, just don't add it.
                    } else {
                        if (!DMObjectTree.SeenGlobalProcDefinition.Add(procName)) { // Add() is equivalent to Dictionary's TryAdd() for some reason
                            DMCompiler.Emit(WarningCode.DuplicateProcDefinition, procDefinition.Location, $"Global proc {procDefinition.Name} is already defined");
                            //Again, even though this is likely an error, process the statements anyways.
                        } else {
                            DMObjectTree.AddGlobalProc(proc.Name, proc.Id);
                        }
                    }
                } else {
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
                    dmObject.AddVerb(proc);
                }
            } catch (CompileErrorException e) {
                DMCompiler.Emit(e.Error);
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
        private static bool DoesOverrideGlobalVars(DMASTObjectVarDefinition varDefinition)
        {
            if (varDefinition == null) return false;
            return varDefinition.IsStatic && varDefinition.Name == "vars" && varDefinition.ObjectPath == DreamPath.Root;
        }

        /// <summary>
        /// A snowflake helper proc which allows for ignoring variable duplication in the specific case that /world or /client are inheriting from /datum,<br/>
        /// which would normally throw an error since all of these classes have their own var/vars definition.
        /// </summary>
        private static bool DoesDefineSnowflakeVars(DMASTObjectVarDefinition varDefinition, DMObject varObject) {
            if (DMCompiler.Settings.NoStandard == false)
                if (varDefinition != null)
                    if (varDefinition.Name == "vars")
                        if (varDefinition.ObjectPath == DreamPath.World || varDefinition.ObjectPath == DreamPath.Client)
                            if (varObject.IsSubtypeOf(DreamPath.Datum))
                                return true;
            return false;
        }

        /// <summary>
        /// A filter proc above <see cref="SetVariableValue"/> <br/>
        /// which checks first to see if overriding this thing's value is valid (as in the case of const and <see cref="DMValueType.CompiletimeReadonly"/>)
        /// </summary>
        /// <remarks>
        /// This is bizarrely public instead of private because DMObject ends up calling it to handle late var definitions, and there's no 'friend' keyword in C#.
        /// </remarks>
        public static void OverrideVariableValue(DMObject currentObject, ref DMVariable variable, DMASTExpression value)
        {
            if(variable.IsConst)
            {
                DMCompiler.Emit(WarningCode.WriteToConstant, value.Location, $"Var {variable.Name} is const and cannot be modified");
                return;
            }
            if((variable.ValType & DMValueType.CompiletimeReadonly) == DMValueType.CompiletimeReadonly)
            {
                DMCompiler.Emit(WarningCode.WriteToConstant, value.Location, $"Var {variable.Name} is a native read-only value which cannot be modified");
            }
            SetVariableValue(currentObject, ref variable, value);
        }

        /// <summary>
        /// Handles setting a variable to a value (when called by itself, this assumes the statement is a declaration and not a re-assignment)
        /// </summary>
        /// <param name="variable">This parameter may be modified if a new variable had to be instantiated in the case of an override.</param>
        /// <exception cref="CompileErrorException"></exception>
        private static void SetVariableValue(DMObject currentObject, ref DMVariable variable, DMASTExpression value) {
            DMVisitorExpression._scopeMode = variable.IsGlobal ? "static" : "normal";
            DMExpression expression = DMExpression.Create(currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, variable.Type);
            DMVisitorExpression._scopeMode = "normal";

            if (expression.TryAsConstant(out var constant)) {
                variable = variable.WriteToValue(constant);
                return;
            }

            if (variable.IsConst) {
                throw new CompileErrorException(value.Location, "Value of const var must be a constant");
            }

            if (!IsValidRighthandSide(currentObject, variable, expression)) {
                throw new CompileErrorException(value.Location, $"Invalid initial value for \"{variable.Name}\"");
            }

            variable = variable.WriteToValue(new Expressions.Null(Location.Internal));
            EmitInitializationAssign(currentObject, variable, expression);
        }

        /// <param name="expression">This expression should have already been processed by TryAsConstant.</param>
        /// <returns>true if the expression given can be used to initialize the given variable. false if not.</returns>
        private static bool IsValidRighthandSide(DMObject currentObject, DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) // Have to back out early like this because if we are a static set by a ProcCall, it might be underdefined right now (and so error in the switch)
                return true;
            return expression switch {
                //TODO: A better way of handling procs evaluated at compile time
                Expressions.ProcCall procCall => procCall.GetTargetProc(currentObject).Proc?.Name switch {
                    "rgb" => true,
                    "generator" => true,
                    "matrix" => true,
                    "icon" => true,
                    "file" => true,
                    "sound" => true,
                    _ => false
                },

                Expressions.List => true,
                Expressions.NewList => true,
                Expressions.NewPath => true,
                // TODO: Check for circular reference loops here
                // (Note that we do accidentally support global-field access somewhat when it gets const-folded by TryAsConstant before we get here)
                Expressions.GlobalField => false,
                _ => false
            };
        }

        private static void EmitInitializationAssign(DMObject currentObject, DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) {
                int? globalId = currentObject.GetGlobalVariableId(variable.Name);
                if (globalId == null) throw new CompileAbortException(expression?.Location ?? Location.Unknown, $"Invalid global {currentObject.Path}.{variable.Name}");

                DMObjectTree.AddGlobalInitAssign(currentObject, globalId.Value, expression);
            } else {
                var initLoc = expression.Location;
                Expressions.Field field = new Expressions.Field(initLoc, variable);
                Expressions.Assignment assign = new Expressions.Assignment(initLoc, field, expression);

                currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
