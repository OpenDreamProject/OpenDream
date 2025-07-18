using DMCompiler.Bytecode;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DMCompiler.DM.Expressions;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Builders;
using DMCompiler.Json;
using DMCompiler.Optimizer;
using VerbSrcEnum = DMCompiler.DM.VerbSrc;

namespace DMCompiler.DM;

internal sealed class DMProc {
    public class LocalVariable(string name, int id, bool isParameter, DreamPath? type, DMComplexValueType? explicitValueType) {
        public readonly string Name = name;
        public readonly int Id = id;
        public readonly bool IsParameter = isParameter;
        public DreamPath? Type = type;

        /// <summary>
        /// The explicit <see cref="DMValueType"/> for this variable
        /// <code>var/parameter as mob</code>
        /// </summary>
        public DMComplexValueType? ExplicitValueType = explicitValueType;
    }

    public sealed class LocalConstVariable(string name, int id, DreamPath? type, Constant value)
        : LocalVariable(name, id, false, type, value.ValType) {
        public readonly Constant Value = value;
    }

    public class CodeLabel {
        private static int _idCounter;
        public readonly long AnnotatedByteOffset;
        public readonly int Id;
        public readonly string Name;

        public string LabelName => $"{Name}_{Id}_codelabel";

        public CodeLabel(string name, long offset) {
            Id = _idCounter++;
            Name = name;
            AnnotatedByteOffset = offset;
        }
    }

    internal struct CodeLabelReference(string identifier, string placeholder, Location location, DMProcScope scope) {
        public readonly string Identifier = identifier;
        public readonly string Placeholder = placeholder;
        public readonly Location Location = location;
        public readonly DMProcScope Scope = scope;
    }

    internal class DMProcScope {
        public readonly Dictionary<string, LocalVariable> LocalVariables = new();
        public readonly Dictionary<string, CodeLabel> LocalCodeLabels = new();
        public readonly DMProcScope? ParentScope;

        public DMProcScope() { }

        public DMProcScope(DMProcScope? parentScope) {
            ParentScope = parentScope;
        }
    }

    public string Name => _astDefinition?.Name ?? "<init>";
    public bool IsVerb => _astDefinition?.IsVerb ?? false;
    public bool IsFinal => _astDefinition?.IsFinal ?? false;
    public readonly List<string> Parameters = new();
    public Location Location;
    public ProcAttributes Attributes;
    public string? UnsupportedReason;
    public readonly int Id;
    public readonly Dictionary<string, int> GlobalVariables = new();

    public VerbSrc? VerbSrc;
    public string? VerbName;
    public string? VerbCategory = string.Empty;
    public string? VerbDesc;
    public sbyte Invisibility;

    private readonly DMCompiler _compiler;
    private readonly DMObject _dmObject;
    private readonly DMASTProcDefinition? _astDefinition;
    private readonly Stack<CodeLabelReference> _pendingLabelReferences = new();
    private Stack<string>? _loopStack;
    private readonly Stack<DMProcScope> _scopes = new();
    private readonly Dictionary<string, LocalVariable> _parameters = new();
    private int _labelIdCounter;
    private int _enumeratorIdCounter;

    private readonly List<string> _localVariableNames = new();
    private int _localVariableIdCounter;

    private readonly List<SourceInfoJson> _sourceInfo = new();
    private string? _lastSourceFile;

    public bool TypeChecked => !ReturnTypes.IsAnything;
    public DMComplexValueType? RawReturnTypes => _astDefinition?.ReturnTypes;
    public DMComplexValueType ReturnTypes => _dmObject.GetProcReturnTypes(Name) ?? DMValueType.Anything;

    public long Position => AnnotatedBytecode.Position;
    public readonly AnnotatedByteCodeWriter AnnotatedBytecode;

    private Location _writerLocation;

    /// <summary>
    /// BYOND currently has a ridiculous behaviour, where, <br/>
    /// sometimes when a set statement has a right-hand side that is non-constant, <br/>
    /// no error is emitted and instead its value is just, whatever the last well-evaluated set statement's value was. <br/>
    /// This behaviour is nonsense but for harsh parity we sometimes may need to carry it out to hold up a codebase; <br/>
    /// Yogstation (at time of writing) actually errors on OD if we don't implement this.
    /// </summary>
    // Starts null; marks that we've never seen one before and should just error like normal people.
    private static Constant? _previousSetStatementValue;

    public DMProc(DMCompiler compiler, int id, DMObject dmObject, DMASTProcDefinition? astDefinition) {
        AnnotatedBytecode = new(compiler);
        _compiler = compiler;
        Id = id;
        _dmObject = dmObject;
        _astDefinition = astDefinition;
        if (_astDefinition?.IsOverride ?? false) Attributes |= ProcAttributes.IsOverride; // init procs don't have AST definitions
        Location = astDefinition?.Location ?? Location.Unknown;
        _scopes.Push(new DMProcScope());

        if (_astDefinition is not null) {
            foreach (var parameter in _astDefinition!.Parameters) {
                AddParameter(parameter.Name, parameter.Location, parameter.Type, parameter.ObjectType);
            }

            foreach (var statement in _astDefinition!.Body?.SetStatements ?? Array.Empty<DMASTProcStatementSet>()) {
                if (statement is DMASTAggregate<DMASTProcStatementSet> setAggregate) {
                    foreach (var setStatement in setAggregate.Statements) {
                        ProcessSetStatement(setStatement);
                    }
                } else if (statement is DMASTProcStatementSet setStatement) {
                    ProcessSetStatement(setStatement);
                }
            }
        }
    }

    private int AllocLocalVariable(string name) {
        _localVariableNames.Add(name);
        WriteLocalVariable(name);
        return _localVariableIdCounter++;
    }

    private void DeallocLocalVariables(int amount) {
        if (amount > 0) {
            WriteLocalVariableDealloc(amount);
            _localVariableIdCounter -= amount;
        }
    }

    public void Compile() {
        _compiler.VerbosePrint($"Compiling proc {_dmObject.Path.ToString()}.{Name}()");

        if (_astDefinition is not null) { // It's null for initialization procs
            new DMProcBuilder(_compiler, _dmObject, this).ProcessProcDefinition(_astDefinition);
        }
    }

    public void ValidateReturnType(DMExpression expr) {
        var type = expr.ValType;
        var returnTypes = _dmObject.GetProcReturnTypes(Name)!.Value;
        if ((returnTypes.Type & (DMValueType.Color | DMValueType.File | DMValueType.Message)) != 0) {
            _compiler.Emit(WarningCode.UnsupportedTypeCheck, expr.Location, "color, message, and file return types are currently unsupported.");
            return;
        }

        var splitter = _astDefinition?.IsOverride ?? false ? "/" : "/proc/";
        // We couldn't determine the expression's return type for whatever reason
        if (type.IsAnything) {
            if (_compiler.Settings.SkipAnythingTypecheck)
                return;

            switch (expr) {
                case ProcCall:
                    _compiler.Emit(WarningCode.InvalidReturnType, expr.Location, $"{_dmObject.Path.ToString()}.{Name}(): Called proc does not have a return type set, expected {ReturnTypes}.");
                    break;
                case Local:
                    _compiler.Emit(WarningCode.InvalidReturnType, expr.Location, $"{_dmObject.Path.ToString()}.{Name}(): Cannot determine return type of non-constant expression, expected {ReturnTypes}. Consider making this variable constant or adding an explicit \"as {ReturnTypes}\"");
                    break;
                default:
                    _compiler.Emit(WarningCode.InvalidReturnType, expr.Location, $"{_dmObject.Path.ToString()}.{Name}(): Cannot determine return type of expression \"{expr}\", expected {ReturnTypes}. Consider reporting this as a bug on OpenDream's GitHub.");
                    break;
            }
        } else if (!ReturnTypes.MatchesType(_compiler, type)) { // We could determine the return types but they don't match
            _compiler.Emit(WarningCode.InvalidReturnType, expr.Location, $"{_dmObject.Path.ToString()}{splitter}{Name}(): Invalid return type {type}, expected {ReturnTypes}");
        }
    }

    public ProcDefinitionJson GetJsonRepresentation() {
        var serializer = new AnnotatedBytecodeSerializer(_compiler);

        _compiler.BytecodeOptimizer.Optimize(AnnotatedBytecode.GetAnnotatedBytecode());

        List<ProcArgumentJson>? arguments = null;
        if (_parameters.Count > 0) {
            arguments = new List<ProcArgumentJson>(_parameters.Count);

            foreach (var parameter in _parameters.Values) {
                if (parameter.ExplicitValueType is not { } argumentType) {
                    // If no "as" was used then we assume its type based on the type hint
                    if (parameter.Type is not { } typePath) {
                        argumentType = DMValueType.Anything;
                    } else {
                        _compiler.DMObjectTree.TryGetDMObject(typePath, out var type);
                        argumentType = type?.GetDMValueType() ?? DMValueType.Anything;
                    }
                }

                arguments.Add(new ProcArgumentJson {
                    Name = parameter.Name,
                    Type = argumentType.Type
                });
            }
        }

        return new ProcDefinitionJson {
            OwningTypeId = _dmObject.Id,
            Name = Name,
            Attributes = Attributes,
            MaxStackSize = AnnotatedBytecode.GetMaxStackSize(),
            Bytecode = serializer.Serialize(AnnotatedBytecode.GetAnnotatedBytecode()),
            Arguments = arguments,
            SourceInfo = serializer.SourceInfo,
            Locals = (_localVariableNames.Count > 0) ? serializer.GetLocalVariablesJson() : null,

            IsVerb = IsVerb,
            VerbSrc = VerbSrc,
            VerbName = VerbName,
            VerbDesc = VerbDesc,
            Invisibility = Invisibility,

            // Normally VerbCategory is "" by default and null to hide it, but we invert those during (de)serialization to reduce JSON size
            VerbCategory = VerbCategory switch {
                "" => null,
                null => string.Empty,
                _ => VerbCategory
            }
        };
    }

    public void WaitFor(bool waitFor) {
        if (waitFor) {
            // "waitfor" is true by default
            Attributes &= ~ProcAttributes.DisableWaitfor;
        } else {
            Attributes |= ProcAttributes.DisableWaitfor;
        }
    }

    public void AddGlobalVariable(DMVariable global, int id) {
        GlobalVariables[global.Name] = id;
    }

    public int? GetGlobalVariableId(string name) {
        if (GlobalVariables.TryGetValue(name, out int id)) {
            return id;
        }

        return null;
    }

    public void AddParameter(string name, Location location, DMComplexValueType? valueType, DreamPath? type) {
        if (_parameters.ContainsKey(name)) {
            _compiler.Emit(WarningCode.DuplicateVariable, location, $"Duplicate argument \"{name}\"");
        } else {
            // TODO: Emit a warning when shadowing a builtin, such as "caller"
            Parameters.Add(name);
            _parameters.Add(name, new LocalVariable(name, _parameters.Count, true, type, valueType));
        }
    }

    public void ProcessSetStatement(DMASTProcStatementSet statementSet) {
        var attribute = statementSet.Attribute.ToLower();

        if(attribute == "src") {
            // TODO: Would be much better if the parser was just more strict with the expression
            switch (statementSet.Value) {
                case DMASTIdentifier {Identifier: "usr"}:
                    VerbSrc = statementSet.WasInKeyword ? VerbSrcEnum.InUsr : VerbSrcEnum.Usr;
                    break;
                case DMASTDereference {Expression: DMASTIdentifier{Identifier: "usr"}, Operations: var operations}:
                    if (operations is not [DMASTDereference.FieldOperation {Identifier: var deref}])
                        goto default;

                    if (deref == "contents") {
                        VerbSrc = VerbSrcEnum.InUsr;
                    }  else if (deref == "loc") {
                        VerbSrc = VerbSrcEnum.UsrLoc;
                        _compiler.UnimplementedWarning(statementSet.Location,
                            "'set src = usr.loc' is unimplemented");
                    } else if (deref == "group") {
                        VerbSrc = VerbSrcEnum.UsrGroup;
                        _compiler.UnimplementedWarning(statementSet.Location,
                            "'set src = usr.group' is unimplemented");
                    } else {
                        goto default;
                    }

                    break;
                case DMASTIdentifier {Identifier: "world"}:
                    VerbSrc = statementSet.WasInKeyword ? VerbSrcEnum.InWorld : VerbSrcEnum.World;
                    if (statementSet.WasInKeyword)
                        _compiler.UnimplementedWarning(statementSet.Location,
                            "'set src = world.contents' is unimplemented");
                    else
                        _compiler.UnimplementedWarning(statementSet.Location,
                            "'set src = world' is unimplemented");
                    break;
                case DMASTDereference {Expression: DMASTIdentifier{Identifier: "world"}, Operations: var operations}:
                    if (operations is not [DMASTDereference.FieldOperation {Identifier: "contents"}])
                        goto default;

                    VerbSrc = VerbSrcEnum.InWorld;
                    _compiler.UnimplementedWarning(statementSet.Location,
                        "'set src = world.contents' is unimplemented");
                    break;
                case DMASTProcCall {Callable: DMASTCallableProcIdentifier {Identifier: { } viewType and ("view" or "oview")}}:
                    // TODO: Ranges
                    if (statementSet.WasInKeyword)
                        VerbSrc = viewType == "view" ? VerbSrcEnum.InView : VerbSrcEnum.InOView;
                    else
                        VerbSrc = viewType == "view" ? VerbSrcEnum.View : VerbSrcEnum.OView;
                    break;
                // range() and orange() are undocumented, but they work
                case DMASTProcCall {Callable: DMASTCallableProcIdentifier {Identifier: { } viewType and ("range" or "orange")}}:
                    // TODO: Ranges
                    if (statementSet.WasInKeyword)
                        VerbSrc = viewType == "range" ? VerbSrcEnum.InRange : VerbSrcEnum.InORange;
                    else
                        VerbSrc = viewType == "range" ? VerbSrcEnum.Range : VerbSrcEnum.ORange;
                    break;
                default:
                    _compiler.Emit(WarningCode.BadExpression, statementSet.Value.Location, "Invalid verb src");
                    break;
            }

            return;
        }

        var exprBuilder = new DMExpressionBuilder(new ExpressionContext(_compiler, _dmObject, this));
        if (!exprBuilder.TryConstant(statementSet.Value, out var constant)) { // If this set statement's rhs is not constant
            bool didError = _compiler.Emit(WarningCode.InvalidSetStatement, statementSet.Location, $"'{attribute}' attribute should be a constant");
            if (didError) // if this is an error
                return; // don't do the cursed thing

            constant = _previousSetStatementValue;
        } else {
            _previousSetStatementValue = constant;
        }

        // oh no.
        if (constant is null) {
            _compiler.Emit(WarningCode.BadExpression, statementSet.Location, $"'{attribute}' attribute must be a constant");
            return;
        }

        // Check if it was 'set x in y' or whatever
        // (which is illegal for everything except setting src to something)
        if (statementSet.WasInKeyword) {
            _compiler.Emit(WarningCode.BadToken, statementSet.Location, "Use of 'in' keyword is illegal here. Did you mean '='?");
            //fallthrough into normal behaviour because this error is kinda pedantic
        }

        switch (statementSet.Attribute.ToLower()) {
            case "waitfor": {
                WaitFor(constant.IsTruthy());
                break;
            }
            case "opendream_unimplemented": {
                if (constant.IsTruthy())
                    Attributes |= ProcAttributes.Unimplemented;
                else
                    Attributes &= ~ProcAttributes.Unimplemented;
                break;
            }
            case "opendream_unsupported":
                if (constant is not Expressions.String unsupportedStr) {
                    _compiler.Emit(WarningCode.BadExpression, constant.Location, "opendream_unsupported attribute must be a string");
                    break;
                }

                UnsupportedReason = unsupportedStr.Value;
                break;
            case "hidden":
                if (constant.IsTruthy())
                    Attributes |= ProcAttributes.Hidden;
                else
                    Attributes &= ~ProcAttributes.Hidden;
                break;
            case "popup_menu":
                if (constant.IsTruthy()) // The default is to show it so we flag it if it's hidden
                    Attributes &= ~ProcAttributes.HidePopupMenu;
                else
                    Attributes |= ProcAttributes.HidePopupMenu;
                break;
            case "instant":
                if (constant.IsTruthy())
                    Attributes |= ProcAttributes.Instant;
                else
                    Attributes &= ~ProcAttributes.Instant;

                _compiler.UnimplementedWarning(statementSet.Location, "set instant is not implemented");
                break;
            case "background":
                if (constant.IsTruthy())
                    Attributes |= ProcAttributes.Background;
                else
                    Attributes &= ~ProcAttributes.Background;
                break;
            case "name":
                if (constant is not Expressions.String nameStr) {
                    _compiler.Emit(WarningCode.BadExpression, constant.Location, "name attribute must be a string");
                    break;
                }

                VerbName = nameStr.Value;
                break;
            case "category":
                if (constant is Expressions.String str) {
                    VerbCategory = str.Value;
                } else if (constant is Null) {
                    VerbCategory = null;
                } else {
                    _compiler.Emit(WarningCode.BadExpression, constant.Location,
                        "category attribute must be a string or null");
                }

                break;
            case "desc":
                // TODO: verb.desc is supposed to be printed when you type the verb name and press F1. Check the ref for details.
                if (constant is not Expressions.String descStr) {
                    _compiler.Emit(WarningCode.BadExpression, constant.Location, "desc attribute must be a string");
                    break;
                }

                VerbDesc = descStr.Value;
                break;
            case "invisibility":
                // The ref says 0-101 for atoms and 0-100 for verbs
                // BYOND doesn't clamp the actual var value but it does seem to treat out-of-range values as their extreme
                if (constant is not Number invisNum) {
                    _compiler.Emit(WarningCode.BadExpression, constant.Location, "invisibility attribute must be an int");
                    break;
                }

                Invisibility = Convert.ToSByte(Math.Clamp(MathF.Floor(invisNum.Value), 0f, 100f));
                break;
            case "src":
                _compiler.UnimplementedWarning(statementSet.Location, "set src is not implemented");
                break;
        }
    }

    public void EmitUsageWarnings(Location fromLoc) {
        if (fromLoc.InDMStandard) // Don't emit these warnings for code inside DMStandard
            return;

        if (UnsupportedReason is not null) {
            _compiler.UnsupportedWarning(fromLoc, $"{_dmObject.Path.ToString()}.{Name}() is unsupported: {UnsupportedReason}");
        } else if ((Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) {
            _compiler.UnimplementedWarning(fromLoc, $"{_dmObject.Path.ToString()}.{Name}() is not implemented");
        }
    }

    public bool TryGetParameterByName(string name, [NotNullWhen(true)] out LocalVariable? param) {
        return _parameters.TryGetValue(name, out param);
    }

    public bool TryGetParameterAtIndex(int index, [NotNullWhen(true)] out LocalVariable? param) {
        if (_astDefinition == null || index >= _astDefinition.Parameters.Length) {
            param = null;
            return false;
        }

        var name = _astDefinition.Parameters[index].Name;
        return _parameters.TryGetValue(name, out param);
    }

    public string MakePlaceholderLabel() => $"PLACEHOLDER_{_pendingLabelReferences.Count}_LABEL";

    public CodeLabel? TryAddCodeLabel(string name) {
        if (_scopes.Peek().LocalCodeLabels.ContainsKey(name)) {
            _compiler.Emit(WarningCode.DuplicateVariable, Location, $"A label with the name \"{name}\" already exists");
            return null;
        }

        CodeLabel label = new CodeLabel(name, Position);
        _scopes.Peek().LocalCodeLabels.Add(name, label);
        return label;
    }

    public bool TryAddLocalVariable(string name, DreamPath? type, DMComplexValueType valType) {
        if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
            return false;

        int localVarId = AllocLocalVariable(name);
        return _scopes.Peek().LocalVariables.TryAdd(name, new LocalVariable(name, localVarId, false, type, valType));
    }

    public bool TryAddLocalConstVariable(string name, DreamPath? type, Constant value) {
        if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
            return false;

        int localVarId = AllocLocalVariable(name);
        return _scopes.Peek().LocalVariables.TryAdd(name, new LocalConstVariable(name, localVarId, type, value));
    }

    public LocalVariable? GetLocalVariable(string name) {
        if (_parameters.TryGetValue(name, out var parameter)) {
            return parameter;
        }

        DMProcScope? scope = _scopes.Peek();
        while (scope != null) {
            if (scope.LocalVariables.TryGetValue(name, out var localVariable))
                return localVariable;

            scope = scope.ParentScope;
        }

        return null;
    }

    public DMReference GetLocalVariableReference(string name) {
        LocalVariable? local = GetLocalVariable(name);

        return local.IsParameter ? DMReference.CreateArgument(local.Id) : DMReference.CreateLocal(local.Id);
    }

    public void Error() {
        WriteOpcode(DreamProcOpcode.Error);
    }

    public void DebugSource(Location location) {
        var sourceInfo = new SourceInfoJson {
            Line = location.Line ?? -1
        };

        _writerLocation = location;

        var sourceFile = location.SourceFile.Replace('\\', '/');

        // Only write the source file if it has changed
        if (_lastSourceFile != sourceFile) {
            sourceInfo.File = _compiler.DMObjectTree.AddString(sourceFile);
        } else if (_sourceInfo.Count > 0 && sourceInfo.Line == _sourceInfo[^1].Line) {
            // Don't need to write this source info if it's the same source & line as the last
            return;
        }

        _lastSourceFile = sourceFile;
        _sourceInfo.Add(sourceInfo);
    }

    public void PushReferenceValue(DMReference reference) {
        WriteOpcode(DreamProcOpcode.PushReferenceValue);
        WriteReference(reference);
    }

    public void CreateListEnumerator() {
        WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        WriteEnumeratorId(_enumeratorIdCounter++);
    }

    public void CreateFilteredListEnumerator(int filterTypeId, DreamPath filterType) {
        WriteOpcode(DreamProcOpcode.CreateFilteredListEnumerator);
        WriteEnumeratorId(_enumeratorIdCounter++);
        WriteFilterId(filterTypeId, filterType);
    }

    public void CreateTypeEnumerator() {
        WriteOpcode(DreamProcOpcode.CreateTypeEnumerator);
        WriteEnumeratorId(_enumeratorIdCounter++);
    }

    public void CreateRangeEnumerator() {
        WriteOpcode(DreamProcOpcode.CreateRangeEnumerator);
        WriteEnumeratorId(_enumeratorIdCounter++);
    }

    public void Enumerate(DMReference reference) {
        if (_loopStack?.TryPeek(out var peek) ?? false) {
            WriteOpcode(DreamProcOpcode.Enumerate);
            WriteEnumeratorId(_enumeratorIdCounter - 1);
            WriteReference(reference);
            WriteLabel($"{peek}_end");
        } else {
            _compiler.ForcedError(Location, "Cannot peek empty loop stack");
        }
    }

    public void EnumerateAssoc(DMReference assocRef, DMReference listRef, DMReference outputRef) {
        if (_loopStack?.TryPeek(out var peek) ?? false) {
            WriteOpcode(DreamProcOpcode.EnumerateAssoc);
            WriteEnumeratorId(_enumeratorIdCounter - 1);
            WriteReference(assocRef);
            WriteReference(listRef);
            WriteReference(outputRef);
            WriteLabel($"{peek}_end");
        } else {
            _compiler.ForcedError(Location, "Cannot peek empty loop stack");
        }
    }

    public void EnumerateNoAssign() {
        if (_loopStack?.TryPeek(out var peek) ?? false) {
            WriteOpcode(DreamProcOpcode.EnumerateNoAssign);
            WriteEnumeratorId(_enumeratorIdCounter - 1);
            WriteLabel($"{peek}_end");
        } else {
            _compiler.ForcedError(Location, "Cannot peek empty loop stack");
        }
    }

    public void DestroyEnumerator() {
        WriteOpcode(DreamProcOpcode.DestroyEnumerator);
        WriteEnumeratorId(--_enumeratorIdCounter);
    }

    public void CreateList(int size) {
        ResizeStack(-(size - 1)); //Shrinks by the size of the list, grows by 1
        WriteOpcode(DreamProcOpcode.CreateList);
        WriteListSize(size);
    }

    public void CreateMultidimensionalList(int dimensionCount) {
        ResizeStack(-(dimensionCount - 1)); // Pops the amount of dimensions, then pushes the list
        WriteOpcode(DreamProcOpcode.CreateMultidimensionalList);
        WriteListSize(dimensionCount);
    }

    public void CreateAssociativeList(int size) {
        ResizeStack(-(size * 2 - 1)); //Shrinks by twice the size of the list, grows by 1
        WriteOpcode(DreamProcOpcode.CreateAssociativeList);
        WriteListSize(size);
    }

    public void CreateStrictAssociativeList(int size) {
        ResizeStack(-(size * 2 - 1)); //Shrinks by twice the size of the alist, grows by 1
        WriteOpcode(DreamProcOpcode.CreateStrictAssociativeList);
        WriteListSize(size);
    }

    public string NewLabelName() {
        return "label" + _labelIdCounter++;
    }

    public void LoopStart(string loopLabel) {
        _loopStack ??= new Stack<string>(3); // Start, continue, end
        _loopStack.Push(loopLabel);

        AddLabel(loopLabel + "_start");
        StartScope();
    }

    public void MarkLoopContinue(string loopLabel) {
        AddLabel($"{loopLabel}_continue");
    }

    public void BackgroundSleep() {
        // TODO This seems like a bad way to handle background, doesn't it?

        if ((Attributes & ProcAttributes.Background) == ProcAttributes.Background) {
            if (!_compiler.DMObjectTree.TryGetGlobalProc("sleep", out var sleepProc)) {
                _compiler.Emit(WarningCode.ItemDoesntExist, Location, "Cannot do a background sleep without a sleep proc");
                return;
            }

            PushFloat(-1); // argument given to sleep()
            Call(DMReference.CreateGlobalProc(sleepProc.Id), DMCallArgumentsType.FromStack, 1);
            Pop(); // Pop the result of the sleep call
        }
    }

    public void LoopJumpToStart(string loopLabel) {
        BackgroundSleep();
        Jump($"{loopLabel}_start");
    }

    public void LoopEnd() {
        if (_loopStack?.TryPop(out var pop) ?? false) {
            AddLabel(pop + "_end");
        } else {
            _compiler.ForcedError(Location, "Cannot pop empty loop stack");
        }

        EndScope();
    }

    public void SwitchCase(string caseLabel) {
        WriteOpcode(DreamProcOpcode.SwitchCase);
        WriteLabel(caseLabel);
    }

    public void SwitchCaseRange(string caseLabel) {
        WriteOpcode(DreamProcOpcode.SwitchCaseRange);
        WriteLabel(caseLabel);
    }

    public void Browse() {
        WriteOpcode(DreamProcOpcode.Browse);
    }

    public void BrowseResource() {
        WriteOpcode(DreamProcOpcode.BrowseResource);
    }

    public void OutputControl() {
        WriteOpcode(DreamProcOpcode.OutputControl);
    }

    public void Link() {
        WriteOpcode(DreamProcOpcode.Link);
    }

    public void Ftp() {
        WriteOpcode(DreamProcOpcode.Ftp);
    }

    public void OutputReference(DMReference leftRef) {
        WriteOpcode(DreamProcOpcode.OutputReference);
        WriteReference(leftRef);
    }

    public void Output() {
        WriteOpcode(DreamProcOpcode.Output);
    }

    public void Input(DMReference leftRef, DMReference rightRef) {
        WriteOpcode(DreamProcOpcode.Input);
        WriteReference(leftRef);
        WriteReference(rightRef);
    }

    public void Spawn(string jumpTo) {
        WriteOpcode(DreamProcOpcode.Spawn);
        WriteLabel(jumpTo);
    }

    public void Break(DMASTIdentifier? label = null) {
        if (label is not null) {
            var codeLabel = (GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ?? label.Identifier + "_codelabel");
            if (!LabelExists(codeLabel)) {
                _compiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
            }

            Jump(codeLabel + "_end");
        } else if (_loopStack?.TryPeek(out var peek) ?? false) {
            Jump(peek + "_end");
        } else {
            _compiler.ForcedError(Location, "Cannot peek empty loop stack");
        }
    }

    public void BreakIfFalse() {
        if (_loopStack?.TryPeek(out var peek) ?? false) {
            JumpIfFalse($"{peek}_end");
        } else {
            _compiler.ForcedError(Location, "Cannot peek empty loop stack");
        }
    }

    public void Continue(DMASTIdentifier? label = null) {
        // TODO: Clean up this godawful label handling
        if (label is not null) {
            // Also, labelled loops always need the label declared first, so stick it like this way
            var codeLabel = (
                GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
                label.Identifier + "_codelabel"
            );
            if (!LabelExists(codeLabel)) {
                _compiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
            }

            var labelList = GetLabels().Keys.ToList();
            var continueLabel = string.Empty;
            for (var i = labelList.IndexOf(codeLabel) + 1; i < labelList.Count; i++) {
                if (labelList[i].EndsWith("_start")) {
                    continueLabel = labelList[i].Replace("_start", "_continue");
                    break;
                }
            }

            BackgroundSleep();
            Jump(continueLabel);
        } else {
            BackgroundSleep();

            if (_loopStack?.TryPeek(out var peek) ?? false) {
                Jump(peek + "_continue");
            } else {
                _compiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }
    }

    public void Goto(DMASTIdentifier label) {
        var placeholder = MakePlaceholderLabel();
        _pendingLabelReferences.Push(new CodeLabelReference(
            label.Identifier,
            placeholder,
            label.Location,
            _scopes.Peek()
        ));
        Jump(placeholder);
    }

    public void Pop() {
        WriteOpcode(DreamProcOpcode.Pop);
    }

    public void BooleanOr(string endLabel) {
        WriteOpcode(DreamProcOpcode.BooleanOr);
        WriteLabel(endLabel);
    }

    public void BooleanAnd(string endLabel) {
        WriteOpcode(DreamProcOpcode.BooleanAnd);
        WriteLabel(endLabel);
    }

    public void StartScope() {
        _scopes.Push(new DMProcScope(_scopes.Peek()));
    }

    public void EndScope() {
        DMProcScope destroyedScope = _scopes.Pop();
        DeallocLocalVariables(destroyedScope.LocalVariables.Count);
    }

    public void Jump(string label) {
        WriteOpcode(DreamProcOpcode.Jump);
        WriteLabel(label);
    }

    public void JumpIfFalse(string label) {
        WriteOpcode(DreamProcOpcode.JumpIfFalse);
        WriteLabel(label);
    }

    public void JumpIfNull(string label) {
        // Conditionally pops one value
        WriteOpcode(DreamProcOpcode.JumpIfNull);
        WriteLabel(label);
    }

    public void JumpIfNullNoPop(string label) {
        WriteOpcode(DreamProcOpcode.JumpIfNullNoPop);
        WriteLabel(label);
    }

    public void JumpIfTrueReference(DMReference reference, string label) {
        WriteOpcode(DreamProcOpcode.JumpIfTrueReference);
        WriteReference(reference, affectStack: false);
        WriteLabel(label);
    }

    public void JumpIfFalseReference(DMReference reference, string label) {
        WriteOpcode(DreamProcOpcode.JumpIfFalseReference);
        WriteReference(reference, affectStack: false);
        WriteLabel(label);
    }

    public void DereferenceField(string field) {
        WriteOpcode(DreamProcOpcode.DereferenceField);
        WriteString(field);
    }

    public void DereferenceIndex() {
        WriteOpcode(DreamProcOpcode.DereferenceIndex);
    }

    public void DereferenceCall(string field, DMCallArgumentsType argumentsType, int argumentStackSize) {
        ResizeStack(-argumentStackSize); // Pops proc owner and arguments, pushes result
        WriteOpcode(DreamProcOpcode.DereferenceCall);
        WriteString(field);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void Call(DMReference reference, DMCallArgumentsType argumentsType, int argumentStackSize) {
        ResizeStack(-(argumentStackSize - 1)); // Pops all arguments, pushes return value
        WriteOpcode(DreamProcOpcode.Call);
        WriteReference(reference);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void CallStatement(DMCallArgumentsType argumentsType, int argumentStackSize) {
        //Shrinks the stack by argumentStackSize. Could also shrink it by argumentStackSize+1, but assume not.
        ResizeStack(-argumentStackSize);
        WriteOpcode(DreamProcOpcode.CallStatement);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void Prompt(DMValueType types) {
        WriteOpcode(DreamProcOpcode.Prompt);
        WriteType(types);
    }

    public void Initial() {
        WriteOpcode(DreamProcOpcode.Initial);
    }

    public void Return() {
        WriteOpcode(DreamProcOpcode.Return);
    }

    public void Throw() {
        WriteOpcode(DreamProcOpcode.Throw);
    }

    public void Assign(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Assign);
        WriteReference(reference);
    }

    public void AssignInto(DMReference reference) {
        WriteOpcode(DreamProcOpcode.AssignInto);
        WriteReference(reference);
    }

    public void CreateObject(DMCallArgumentsType argumentsType, int argumentStackSize) {
        ResizeStack(-argumentStackSize); // Pops type and arguments, pushes new object
        WriteOpcode(DreamProcOpcode.CreateObject);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void DeleteObject() {
        WriteOpcode(DreamProcOpcode.DeleteObject);
    }

    public void Not() {
        WriteOpcode(DreamProcOpcode.BooleanNot);
    }

    public void Negate() {
        WriteOpcode(DreamProcOpcode.Negate);
    }

    public void Add() {
        WriteOpcode(DreamProcOpcode.Add);
    }

    public void Subtract() {
        WriteOpcode(DreamProcOpcode.Subtract);
    }

    public void Multiply() {
        WriteOpcode(DreamProcOpcode.Multiply);
    }

    public void MultiplyReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.MultiplyReference);
        WriteReference(reference);
    }

    public void Divide() {
        WriteOpcode(DreamProcOpcode.Divide);
    }

    public void DivideReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.DivideReference);
        WriteReference(reference);
    }

    public void Modulus() {
        WriteOpcode(DreamProcOpcode.Modulus);
    }

    public void ModulusModulus() {
        WriteOpcode(DreamProcOpcode.ModulusModulus);
    }

    public void ModulusReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.ModulusReference);
        WriteReference(reference);
    }

    public void ModulusModulusReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.ModulusModulusReference);
        WriteReference(reference);
    }

    public void Power() {
        WriteOpcode(DreamProcOpcode.Power);
    }

    public void Append(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Append);
        WriteReference(reference);
    }

    public void Increment(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Increment);
        WriteReference(reference);
    }

    public void Decrement(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Decrement);
        WriteReference(reference);
    }

    public void Remove(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Remove);
        WriteReference(reference);
    }

    public void Combine(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Combine);
        WriteReference(reference);
    }

    public void Mask(DMReference reference) {
        WriteOpcode(DreamProcOpcode.Mask);
        WriteReference(reference);
    }

    public void BitShiftLeft() {
        WriteOpcode(DreamProcOpcode.BitShiftLeft);
    }

    public void BitShiftLeftReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.BitShiftLeftReference);
        WriteReference(reference);
    }

    public void BitShiftRight() {
        WriteOpcode(DreamProcOpcode.BitShiftRight);
    }

    public void BitShiftRightReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.BitShiftRightReference);
        WriteReference(reference);
    }

    public void BinaryNot() {
        WriteOpcode(DreamProcOpcode.BitNot);
    }

    public void BinaryAnd() {
        WriteOpcode(DreamProcOpcode.BitAnd);
    }

    public void BinaryXor() {
        WriteOpcode(DreamProcOpcode.BitXor);
    }

    public void BinaryXorReference(DMReference reference) {
        WriteOpcode(DreamProcOpcode.BitXorReference);
        WriteReference(reference);
    }

    public void BinaryOr() {
        WriteOpcode(DreamProcOpcode.BitOr);
    }

    public void Equal() {
        WriteOpcode(DreamProcOpcode.CompareEquals);
    }

    public void NotEqual() {
        WriteOpcode(DreamProcOpcode.CompareNotEquals);
    }

    public void Equivalent() {
        WriteOpcode(DreamProcOpcode.CompareEquivalent);
    }

    public void NotEquivalent() {
        WriteOpcode(DreamProcOpcode.CompareNotEquivalent);
    }

    public void GreaterThan() {
        WriteOpcode(DreamProcOpcode.CompareGreaterThan);
    }

    public void GreaterThanOrEqual() {
        WriteOpcode(DreamProcOpcode.CompareGreaterThanOrEqual);
    }

    public void LessThan() {
        WriteOpcode(DreamProcOpcode.CompareLessThan);
    }

    public void LessThanOrEqual() {
        WriteOpcode(DreamProcOpcode.CompareLessThanOrEqual);
    }

    public void Sin() {
        WriteOpcode(DreamProcOpcode.Sin);
    }

    public void Cos() {
        WriteOpcode(DreamProcOpcode.Cos);
    }

    public void Tan() {
        WriteOpcode(DreamProcOpcode.Tan);
    }

    public void ArcSin() {
        WriteOpcode(DreamProcOpcode.ArcSin);
    }

    public void ArcCos() {
        WriteOpcode(DreamProcOpcode.ArcCos);
    }

    public void ArcTan() {
        WriteOpcode(DreamProcOpcode.ArcTan);
    }

    public void ArcTan2() {
        WriteOpcode(DreamProcOpcode.ArcTan2);
    }

    public void Sqrt() {
        WriteOpcode(DreamProcOpcode.Sqrt);
    }

    public void Log() {
        WriteOpcode(DreamProcOpcode.Log);
    }

    public void LogE() {
        WriteOpcode(DreamProcOpcode.LogE);
    }

    public void Abs() {
        WriteOpcode(DreamProcOpcode.Abs);
    }

    public void PushFloat(float value) {
        WriteOpcode(DreamProcOpcode.PushFloat);
        WriteFloat(value);
    }

    public void PushString(string value) {
        WriteOpcode(DreamProcOpcode.PushString);
        WriteString(value);
    }

    public void PushResource(string value) {
        WriteOpcode(DreamProcOpcode.PushResource);
        WriteResource(value);
    }

    public void PushType(int typeId) {
        WriteOpcode(DreamProcOpcode.PushType);
        WriteTypeId(typeId);
    }

    public void PushProc(int procId) {
        WriteOpcode(DreamProcOpcode.PushProc);
        WriteProcId(procId);
    }

    public void PushNull() {
        WriteOpcode(DreamProcOpcode.PushNull);
    }

    public void PushGlobalVars() {
        WriteOpcode(DreamProcOpcode.PushGlobalVars);
    }

    public void FormatString(string value) {
        int formatCount = 0;
        for (int i = 0; i < value.Length; i++) {
            if (StringFormatEncoder.Decode(value[i], out var formatType))
            {
                if(StringFormatEncoder.IsInterpolation(formatType.Value))
                    formatCount++;
            }
        }

        ResizeStack(-(formatCount - 1)); //Shrinks by the amount of formats in the string, grows 1
        WriteOpcode(DreamProcOpcode.FormatString);
        WriteString(value);
        WriteFormatCount(formatCount);
    }

    public void IsInList() {
        WriteOpcode(DreamProcOpcode.IsInList);
    }

    public void IsInRange() {
        WriteOpcode(DreamProcOpcode.IsInRange);
    }

    public void IsSaved() {
        WriteOpcode(DreamProcOpcode.IsSaved);
    }

    public void AsType() {
        WriteOpcode(DreamProcOpcode.AsType);
    }

    public void IsType() {
        WriteOpcode(DreamProcOpcode.IsType);
    }

    public void IsNull() {
        WriteOpcode(DreamProcOpcode.IsNull);
    }

    public void Length() {
        WriteOpcode(DreamProcOpcode.Length);
    }

    public void GetStep() {
        WriteOpcode(DreamProcOpcode.GetStep);
    }

    public void GetDir() {
        WriteOpcode(DreamProcOpcode.GetDir);
    }

    public void LocateCoordinates() {
        WriteOpcode(DreamProcOpcode.LocateCoord);
    }

    public void Gradient(DMCallArgumentsType argumentsType, int argumentStackSize) {
        ResizeStack(-(argumentStackSize - 1)); // Pops arguments, pushes gradient result
        WriteOpcode(DreamProcOpcode.Gradient);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void Rgb(DMCallArgumentsType argumentsType, int argumentStackSize) {
        ResizeStack(-(argumentStackSize - 1)); // Pops arguments, pushes rgb result
        WriteOpcode(DreamProcOpcode.Rgb);
        WriteArgumentType(argumentsType);
        WriteStackDelta(argumentStackSize);
    }

    public void PickWeighted(int count) {
        ResizeStack(-(count - 1));
        WriteOpcode(DreamProcOpcode.PickWeighted);
        WritePickCount(count);
    }

    public void PickUnweighted(int count) {
        ResizeStack(-(count - 1));
        WriteOpcode(DreamProcOpcode.PickUnweighted);
        WritePickCount(count);
    }

    public void Prob() {
        //Pops 1, pushes 1
        WriteOpcode(DreamProcOpcode.Prob);
    }

    public void MassConcatenation(int count) {
        ResizeStack(-(count - 1));
        WriteOpcode(DreamProcOpcode.MassConcatenation);
        WriteConcatCount(count);
    }

    public void Locate() {
        WriteOpcode(DreamProcOpcode.Locate);
    }

    public void StartTry(string label, DMReference reference) {
        WriteOpcode(DreamProcOpcode.Try);
        WriteLabel(label);
        WriteReference(reference);
    }

    public void StartTryNoValue(string label) {
        WriteOpcode(DreamProcOpcode.TryNoValue);
        WriteLabel(label);
    }

    public void EndTry() {
        WriteOpcode(DreamProcOpcode.EndTry);
    }

    // Annotated bytecode wrapper procedures
    private void WriteOpcode(DreamProcOpcode opcode) {
        AnnotatedBytecode.WriteOpcode(opcode, _writerLocation);
    }

    private void WriteReference(DMReference reference, bool affectStack = true) {
        AnnotatedBytecode.WriteReference(reference, _writerLocation, affectStack);
    }

    private void WriteArgumentType(DMCallArgumentsType argumentsType) {
        AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
    }

    private void WriteLabel(string label) {
        AnnotatedBytecode.WriteLabel(label, _writerLocation);
    }

    private void WriteString(string value) {
        AnnotatedBytecode.WriteString(value, _writerLocation);
    }

    private void WriteResource(string value) {
        AnnotatedBytecode.WriteResource(value, _writerLocation);
    }

    private void WriteTypeId(int typeId) {
        AnnotatedBytecode.WriteTypeId(typeId, _writerLocation);
    }

    private void WriteProcId(int procId) {
        AnnotatedBytecode.WriteProcId(procId, _writerLocation);
    }

    private void WriteEnumeratorId(int enumeratorId) {
        AnnotatedBytecode.WriteEnumeratorId(enumeratorId, _writerLocation);
    }

    private void WriteFloat(float value) {
        AnnotatedBytecode.WriteFloat(value, _writerLocation);
    }

    private void WriteListSize(int size) {
        AnnotatedBytecode.WriteListSize(size, _writerLocation);
    }

    private void WriteFormatCount(int count) {
        AnnotatedBytecode.WriteFormatCount(count, _writerLocation);
    }

    private void WritePickCount(int count) {
        AnnotatedBytecode.WritePickCount(count, _writerLocation);
    }

    private void WriteConcatCount(int count) {
        AnnotatedBytecode.WriteConcatCount(count, _writerLocation);
    }

    private void WriteFilterId(int filterId, DreamPath filter) {
        AnnotatedBytecode.WriteFilterId(filterId, filter, _writerLocation);
    }

    private void WriteStackDelta(int delta) {
        AnnotatedBytecode.WriteStackDelta(delta, _writerLocation);
    }

    private void WriteLocalVariable(string name) {
        AnnotatedBytecode.WriteLocalVariable(name, _writerLocation);
    }

    private void WriteLocalVariableDealloc(int amount) {
        AnnotatedBytecode.WriteLocalVariableDealloc(amount, _writerLocation);
    }

    private void ResizeStack(int delta) {
        AnnotatedBytecode.ResizeStack(delta);
    }

    private CodeLabel? GetCodeLabel(string identifier, DMProcScope scope) {
        return AnnotatedBytecode.GetCodeLabel(identifier, scope);
    }

    private void WriteType(DMValueType type) {
        AnnotatedBytecode.WriteType(type, _writerLocation);
    }

    public void AddLabel(string name) {
        AnnotatedBytecode.AddLabel(name);
    }

    private bool LabelExists(string name) {
        return AnnotatedBytecode.LabelExists(name);
    }

    private Dictionary<string, long> GetLabels() {
        return AnnotatedBytecode.GetLabels();
    }

    public void ResolveLabels() {
        AnnotatedBytecode.ResolveCodeLabelReferences(_pendingLabelReferences);
    }
}
