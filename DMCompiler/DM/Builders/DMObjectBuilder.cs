using System.Collections.Generic;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;

namespace DMCompiler.DM.Builders;

internal static class DMObjectBuilder {
    private static readonly List<(DMObject, DMASTObjectVarDefinition)> VarDefinitions = new();
    private static readonly List<(DMObject, DMASTObjectVarOverride)> VarOverrides = new();
    private static readonly List<(DMObject?, DMASTProcDefinition)> ProcDefinitions = new();
    private static readonly List<(DMObject DMObject, DMASTObjectVarDefinition VarDecl)> StaticObjectVars = new();
    private static readonly List<(DMObject DMObject, DMProc Proc, int Id, DMASTProcStatementVarDeclaration VarDecl)> StaticProcVars = new();

    private static int _firstProcGlobal = -1;

    public static void Reset() {
        DMObjectTree.Reset(); // Blank the object tree
        DMExpressionBuilder.ScopeOperatorEnabled = false;

        VarDefinitions.Clear();
        VarOverrides.Clear();
        ProcDefinitions.Clear();
        StaticObjectVars.Clear();
        StaticProcVars.Clear();

        _firstProcGlobal = -1;
    }

    public static void BuildObjectTree(DMASTFile astFile) {
        Reset();

        // Step 1: Define every type in the code. Collect proc and var declarations for later.
        //          Also handles parent_type
        ProcessFile(astFile);

        // Step 2: Define every proc and proc override (but do not compile!)
        //          Collect static vars inside procs for later.
        foreach (var procDef in ProcDefinitions) {
            ProcessProcDefinition(procDef.Item2, procDef.Item1);
        }

        // Step 3: Create static vars
        List<(DMObject, DMASTObjectVarDefinition, UnknownIdentifierException e)> lateVarDefs = new();
        List<(DMObject, DMProc, DMASTProcStatementVarDeclaration, int, UnknownIdentifierException e)> lateProcVarDefs = new();
        for (int i = 0; i <= StaticObjectVars.Count; i++) {
            // Static vars are initialized in code-order, except proc statics are all lumped together
            if (i == _firstProcGlobal) {
                foreach (var procStatic in StaticProcVars) {
                    if (procStatic.VarDecl.Value == null)
                        continue;

                    try {
                        DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.FirstPassStatic;
                        DMExpression expression = DMExpression.Create(procStatic.DMObject, procStatic.Proc,
                            procStatic.VarDecl.Value, procStatic.VarDecl.Type);

                        DMObjectTree.AddGlobalInitAssign(procStatic.Id, expression);
                    } catch (UnknownIdentifierException e) {
                        // For step 6
                        lateProcVarDefs.Add((procStatic.DMObject, procStatic.Proc, procStatic.VarDecl, procStatic.Id, e));
                    } finally {
                        DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Normal;
                    }
                }
            }

            if (i == StaticObjectVars.Count)
                break;

            var objectStatic = StaticObjectVars[i];

            try {
                ProcessVarDefinition(objectStatic.DMObject, objectStatic.VarDecl);
            } catch (UnknownIdentifierException e) {
                lateVarDefs.Add((objectStatic.DMObject, objectStatic.VarDecl, e)); // For step 6
            }
        }

        // Step 4: Define non-static vars
        foreach (var varDef in VarDefinitions) {
            try {
                ProcessVarDefinition(varDef.Item1, varDef.Item2);
            } catch (UnknownIdentifierException e) {
                lateVarDefs.Add((varDef.Item1, varDef.Item2, e)); // For step 6
            }
        }

        // Step 5: Apply var overrides
        List<(DMObject, DMASTObjectVarOverride, UnknownIdentifierException e)> lateOverrides = new();
        foreach (var varOverride in VarOverrides) {
            try {
                ProcessVarOverride(varOverride.Item1, varOverride.Item2);
            } catch (UnknownIdentifierException e) {
                lateOverrides.Add((varOverride.Item1, varOverride.Item2, e)); // For step 7
            }
        }

        // Step 6: Attempt to resolve all vars that referenced other not-yet-existing or overridden vars
        DMExpressionBuilder.ScopeOperatorEnabled = true;
        ProcessLateVarDefs(lateVarDefs, lateProcVarDefs, lateOverrides);

        // The vars these reference were never found, emit their errors
        foreach (var lateVarDef in lateVarDefs) {
            DMCompiler.Emit(WarningCode.ItemDoesntExist, lateVarDef.Item3.Location,
                $"Unknown identifier \"{lateVarDef.Item3.Identifier}\"");
        }

        foreach (var lateVarDef in lateProcVarDefs) {
            DMCompiler.Emit(WarningCode.ItemDoesntExist, lateVarDef.Item3.Location,
                $"Unknown identifier \"{lateVarDef.Item5.Identifier}\"");
        }

        // Step 7: Create each types' initialization proc (initializes vars that aren't constants)
        foreach (DMObject dmObject in DMObjectTree.AllObjects) {
            dmObject.CreateInitializationProc();
        }

        // Step 8: Compile every proc
        foreach (DMProc proc in DMObjectTree.AllProcs)
            proc.Compile();

        // Step 9: Create & Compile the global init proc (initializes global vars)
        DMObjectTree.CreateGlobalInitProc();
    }

    private static void ProcessFile(DMASTFile file) {
        ProcessBlockInner(file.BlockInner, DMObjectTree.Root);
    }

    private static void ProcessBlockInner(DMASTBlockInner blockInner, DMObject? currentObject) {
        foreach (DMASTStatement statement in blockInner.Statements) {
            ProcessStatement(statement, currentObject);
        }
    }

    public static void ProcessStatement(DMASTStatement statement, DMObject? currentObject = null) {
        switch (statement) {
            case DMASTObjectDefinition objectDefinition:
                ProcessObjectDefinition(objectDefinition);
                break;

            case DMASTObjectVarDefinition varDefinition:
                var dmObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath)!;

                if (varDefinition.IsGlobal) {
                    // var/static/list/L[1][2][3] and list() both come first in global init order
                    if (varDefinition.Value is DMASTDimensionalList ||
                        (varDefinition.Value is DMASTList list && list.AllValuesConstant()))
                        StaticObjectVars.Insert(0, (dmObject, varDefinition));
                    else
                        StaticObjectVars.Add((dmObject, varDefinition));
                } else {
                    VarDefinitions.Add((dmObject, varDefinition));
                }

                break;
            case DMASTObjectVarOverride varOverride:
                // parent_type is treated as part of the object definition rather than an actual var override
                if (varOverride.VarName == "parent_type") {
                    if (varOverride.Value is not DMASTConstantPath parentTypePath) {
                        DMCompiler.Emit(WarningCode.BadExpression, varOverride.Location, "Expected a constant path");
                        break; // Ignore it
                    }

                    var parentType = DMObjectTree.GetDMObject(parentTypePath.Value.Path);

                    DMObjectTree.GetDMObject(varOverride.ObjectPath)!.Parent = parentType;
                    break;
                }

                VarOverrides.Add((DMObjectTree.GetDMObject(varOverride.ObjectPath)!, varOverride));
                break;
            case DMASTProcDefinition procDefinition:
                if (procDefinition.Body != null) {
                    foreach (var stmt in GetStatements(procDefinition.Body)) {
                        // TODO multiple var definitions.
                        if (stmt is DMASTProcStatementVarDeclaration { IsGlobal: true }) {
                            if (_firstProcGlobal == -1)
                                _firstProcGlobal = StaticObjectVars.Count;
                            break;
                        }
                    }
                }

                ProcDefinitions.Add((currentObject, procDefinition));
                break;
            case DMASTMultipleObjectVarDefinitions multipleVarDefinitions: {
                foreach (DMASTObjectVarDefinition varDefinition in multipleVarDefinitions.VarDefinitions) {
                    VarDefinitions.Add((DMObjectTree.GetDMObject(varDefinition.ObjectPath)!, varDefinition));
                }

                break;
            }
            default:
                DMCompiler.ForcedError(statement.Location, $"Invalid object statement {statement.GetType()}");
                break;
        }
    }

    private static void ProcessObjectDefinition(DMASTObjectDefinition objectDefinition) {
        DMCompiler.VerbosePrint($"Generating {objectDefinition.Path}");

        DMObject? newCurrentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
        if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock, newCurrentObject);
    }

    private static void ProcessVarDefinition(DMObject? varObject, DMASTObjectVarDefinition? varDefinition) {
        DMVariable? variable = null;

        //DMObjects store two bundles of variables; the statics in GlobalVariables and the non-statics in Variables.
        //Lets check if we're duplicating a definition, first.
        if (varObject.HasGlobalVariable(varDefinition.Name)) {
            DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, $"Duplicate definition of static var \"{varDefinition.Name}\"");
            variable = varObject.GetGlobalVariable(varDefinition.Name);
        } else if (varObject.HasLocalVariable(varDefinition.Name)) {
            if(!DoesDefineSnowflakeVars(varDefinition, varObject))
                DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, $"Duplicate definition of var \"{varDefinition.Name}\"");
            variable = varObject.GetVariable(varDefinition.Name);
        } else if (varDefinition.IsStatic && DoesOverrideGlobalVars(varDefinition)) { // static TODO: Fix this else-if chaining once _currentObject is refactored out of DMObjectBuilder.
            DMCompiler.Emit(WarningCode.DuplicateVariable, varDefinition.Location, "Duplicate definition of global.vars");
            //We can't salvage any part of this definition, since global.vars doesn't technically even exist, so lets just return
            return;
        }

        DMExpression expression;
        try {
            if (varDefinition.IsGlobal)
                DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Static; // FirstPassStatic is not used for object vars

            // TODO: no, bad. instance field declarations should have a proc assigned to them.
            expression = DMExpression.Create(varObject, varDefinition.IsGlobal ? DMObjectTree.GlobalInitProc : null,
                varDefinition.Value, varDefinition.Type);
        } finally {
            DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Normal;
        }

        if (variable is null) {
            if (varDefinition.IsStatic) {
                variable = varObject.CreateGlobalVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsConst, varDefinition.ValType);
            } else {
                variable = new DMVariable(varDefinition.Type, varDefinition.Name, false, varDefinition.IsConst, varDefinition.IsTmp, varDefinition.ValType);
                varObject.Variables[variable.Name] = variable;
                if(varDefinition.IsConst){
                    varObject.ConstVariables ??= new HashSet<string>();
                    varObject.ConstVariables.Add(varDefinition.Name);
                }
                if(varDefinition.IsTmp){
                    varObject.TmpVariables ??= new HashSet<string>();
                    varObject.TmpVariables.Add(varDefinition.Name);
                }
            }
        }

        // TODO: why are we passing the variable ref? we aren't using it after this
        SetVariableValue(varObject, ref variable, varDefinition.Location, expression);
    }

    private static void ProcessVarOverride(DMObject? varObject, DMASTObjectVarOverride? varOverride) {
        switch (varOverride.VarName) {
            // Keep in mind that anything here, by default, affects all objects, even those who don't inherit from /datum
            case "tag": {
                if (varObject.IsSubtypeOf(DreamPath.Datum)) {
                    DMCompiler.Emit(WarningCode.BadExpression, varOverride.Location,
                        "var \"tag\" cannot be set to a value at compile-time");
                }

                break;
            }
        }

        DMVariable? variable;
        if (varObject.HasLocalVariable(varOverride.VarName)) {
            variable = varObject.GetVariable(varOverride.VarName);
        } else if (varObject.HasGlobalVariable(varOverride.VarName)) {
            variable = varObject.GetGlobalVariable(varOverride.VarName);
            DMCompiler.Emit(WarningCode.StaticOverride, varOverride.Location, $"var \"{varOverride.VarName}\" cannot be overridden - it is a global var");
        } else {
            throw new UnknownIdentifierException(varOverride.Location, varOverride.VarName);
        }

        OverrideVariableValue(varObject, ref variable, varOverride.Value);
        varObject.VariableOverrides[variable.Name] = variable;
    }

    private static void ProcessProcDefinition(DMASTProcDefinition procDefinition, DMObject? currentObject) {
        string procName = procDefinition.Name;
        DMObject dmObject = DMObjectTree.GetDMObject(currentObject.Path.Combine(procDefinition.ObjectPath));
        bool hasProc = dmObject.HasProc(procName); // Trying to avoid calling this several times since it's recursive and maybe slow
        if (!procDefinition.IsOverride && hasProc) { // If this is a define and we already had a proc somehow
            if(!dmObject.HasProcNoInheritance(procName)) { // If we're inheriting this proc (so making a new define for it at our level is stupid)
                DMCompiler.Emit(WarningCode.DuplicateProcDefinition, procDefinition.Location, $"Type {dmObject.Path} already inherits a proc named \"{procName}\" and cannot redefine it");
                return; // TODO: Maybe fallthrough since this error is a little pedantic?
            }
            //Otherwise, it's ok
        }

        DMProc proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);
        proc.IsVerb = procDefinition.IsVerb;

        if (procDefinition.ObjectPath == DreamPath.Root) {
            if(procDefinition.IsOverride) {
                DMCompiler.Emit(WarningCode.InvalidOverride, procDefinition.Location, $"Global procs cannot be overridden - '{procDefinition.Name}' override will be ignored");
                //Continue processing the proc anyhoo, just don't add it.
            } else {
                if (!DMObjectTree.SeenGlobalProcDefinition.Add(procName)) { // Add() is equivalent to Dictionary's TryAdd() for some reason
                    DMCompiler.Emit(WarningCode.DuplicateProcDefinition, procDefinition.Location, $"Global proc {procDefinition.Name} is already defined");
                    //Again, even though this is likely an error, process the statements anyways.
                } else {
                    DMObjectTree.AddGlobalProc(proc);
                }
            }
        } else {
            dmObject.AddProc(procName, proc);
        }

        if (procDefinition.Body != null) {
            foreach (var stmt in GetStatements(procDefinition.Body)) {
                // TODO multiple var definitions.
                if (stmt is DMASTProcStatementVarDeclaration varDeclaration && varDeclaration.IsGlobal) {
                    DMVariable variable = proc.CreateGlobalVariable(varDeclaration.Type, varDeclaration.Name, varDeclaration.IsConst, out var globalId);
                    variable.Value = new Expressions.Null(varDeclaration.Location);

                    StaticProcVars.Add((dmObject, proc, globalId, varDeclaration));
                }
            }
        }

        if (procDefinition.IsVerb && (dmObject.IsSubtypeOf(DreamPath.Atom) || dmObject.IsSubtypeOf(DreamPath.Client)) && !DMCompiler.Settings.NoStandard) {
            dmObject.AddVerb(proc);
        }
    }

    // TODO: Remove this entirely
    public static IEnumerable<DMASTProcStatement> GetStatements(DMASTProcBlockInner block) {
        foreach (var stmt in block.Statements) {
            yield return stmt;
            List<DMASTProcBlockInner?> recurse;
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
    private static bool DoesOverrideGlobalVars(DMASTObjectVarDefinition varDefinition) {
        return varDefinition.IsStatic && varDefinition.Name == "vars" && varDefinition.ObjectPath == DreamPath.Root;
    }

    /// <summary>
    /// A snowflake helper proc which allows for ignoring variable duplication in the specific case that /world or /client are inheriting from /datum,<br/>
    /// which would normally throw an error since all of these classes have their own var/vars definition.
    /// </summary>
    private static bool DoesDefineSnowflakeVars(DMASTObjectVarDefinition varDefinition, DMObject varObject) {
        if (DMCompiler.Settings.NoStandard == false)
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
    private static void OverrideVariableValue(DMObject currentObject, ref DMVariable variable,
        DMASTExpression value) {
        if (variable.IsConst) {
            DMCompiler.Emit(WarningCode.WriteToConstant, value.Location,
                $"Var {variable.Name} is const and cannot be modified");
            return;
        }

        if (variable.ValType.IsCompileTimeReadOnly) {
            DMCompiler.Emit(WarningCode.WriteToConstant, value.Location,
                $"Var {variable.Name} is a native read-only value which cannot be modified");
        }

        try {
            if (variable.IsGlobal)
                DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Static;

            DMExpression expression = DMExpression.Create(currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, variable.Type);

            SetVariableValue(currentObject, ref variable, value.Location, expression, true);
        } finally {
            DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Normal;
        }
    }

    private static void SetVariableValue(DMObject currentObject, ref DMVariable variable, Location location, DMExpression expression, bool isOverride = false) {
        // Typechecking
        if (!variable.ValType.MatchesType(expression.ValType) && !variable.ValType.IsUnimplemented) {
            if (expression is Null && !isOverride) {
                DMCompiler.Emit(WarningCode.ImplicitNullType, expression.Location, $"{currentObject.Path.ToString()}.{variable.Name}: Variable is null but not explicitly typed as nullable, append \"|null\" to \"as\". Implicitly treating as nullable.");
                variable.ValType |= DMValueType.Null;
            } else {
                DMCompiler.Emit(WarningCode.InvalidVarType, expression.Location, $"{currentObject.Path.ToString()}.{variable.Name}: Invalid var value type {expression.ValType}, expected {variable.ValType}");
            }
        }

        if (expression.TryAsConstant(out var constant)) {
            variable = variable.WriteToValue(constant);
            return;
        }

        if (variable.IsConst) {
            DMCompiler.Emit(WarningCode.HardConstContext, location, "Value of const var must be a constant");
            return;
        }

        if (!IsValidRighthandSide(currentObject, variable, expression)) {
            DMCompiler.Emit(WarningCode.BadExpression,
                location,
                $"Invalid initial value for \"{variable.Name}\"");
            return;
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
                "generator" => true,
                "matrix" => true,
                "icon" => true,
                "file" => true,
                "sound" => true,
                "nameof" => true,
                _ => false
            },

            Expressions.List => true,
            Expressions.DimensionalList => true,
            Expressions.NewList => true,
            Expressions.NewPath => true,
            Expressions.Rgb => true,
            // TODO: Check for circular reference loops here
            // (Note that we do accidentally support global-field access somewhat when it gets const-folded by TryAsConstant before we get here)
            Expressions.GlobalField => false,
            _ => false
        };
    }

    private static void EmitInitializationAssign(DMObject currentObject, DMVariable variable, DMExpression expression) {
        if (variable.IsGlobal) {
            int? globalId = currentObject.GetGlobalVariableId(variable.Name);
            if (globalId == null) {
                DMCompiler.Emit(WarningCode.BadExpression, expression?.Location ?? Location.Unknown,
                    $"Invalid global {currentObject.Path}.{variable.Name}");
                return;
            }

            DMObjectTree.AddGlobalInitAssign(globalId.Value, expression);
        } else {
            var initLoc = expression.Location;
            var field = new Field(initLoc, variable, variable.ValType);
            var assign = new Assignment(initLoc, field, expression);

            currentObject.InitializationProcExpressions.Add(assign);
        }
    }

    private static void ProcessLateVarDefs(List<(DMObject, DMASTObjectVarDefinition, UnknownIdentifierException e)> lateVarDefs, List<(DMObject, DMProc, DMASTProcStatementVarDeclaration, int, UnknownIdentifierException e)> lateProcVarDefs, List<(DMObject, DMASTObjectVarOverride, UnknownIdentifierException e)> lateOverrides) {
        int lastLateVarDefCount;
        do {
            lastLateVarDefCount = lateVarDefs.Count + lateProcVarDefs.Count + lateOverrides.Count;

            // Vars outside of procs
            for (int i = 0; i < lateVarDefs.Count; i++) {
                var varDef = lateVarDefs[i];

                try {
                    ProcessVarDefinition(varDef.Item1, varDef.Item2);

                    // Success! Remove this one from the list
                    lateVarDefs.RemoveAt(i--);
                } catch (UnknownIdentifierException) {
                    // Keep it in the list, try again after the rest have been processed
                }
            }

            // Static vars inside procs
            for (int i = 0; i < lateProcVarDefs.Count; i++) {
                var varDef = lateProcVarDefs[i];
                var varDecl = varDef.Item3;

                try {
                    DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Static;
                    DMExpression expression =
                        DMExpression.Create(varDef.Item1, varDef.Item2, varDecl.Value!, varDecl.Type);

                    DMObjectTree.AddGlobalInitAssign(varDef.Item4, expression);

                    // Success! Remove this one from the list
                    lateProcVarDefs.RemoveAt(i--);
                } catch (UnknownIdentifierException) {
                    // Keep it in the list, try again after the rest have been processed
                } finally {
                    DMExpressionBuilder.CurrentScopeMode = DMExpressionBuilder.ScopeMode.Normal;
                }
            }

            // Overrides
            for (int i = 0; i < lateOverrides.Count; i++) {
                try {
                    ProcessVarOverride(lateOverrides[i].Item1, lateOverrides[i].Item2);

                    // Success! Remove this one from the list
                    lateOverrides.RemoveAt(i--);
                } catch (UnknownIdentifierException) {
                    // Keep it in the list, try again after the rest have been processed
                }
            }
        } while ((lateVarDefs.Count + lateProcVarDefs.Count + lateOverrides.Count) != lastLateVarDefCount); // As long as the lists are getting smaller, keep trying
    }
}
