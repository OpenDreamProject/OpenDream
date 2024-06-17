using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Procs;
using System.Globalization;
using System.Runtime.CompilerServices;
using DMCompiler.Bytecode;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects {
    [Virtual]
    public class DreamObject {
        public DreamObjectDefinition ObjectDefinition;

        [Access(typeof(DreamObject))]
        public bool Deleted;

        public virtual bool ShouldCallNew => true;

        // Shortcuts to IoC dependencies & entity systems
        protected DreamManager DreamManager => ObjectDefinition.DreamManager;
        protected DreamObjectTree ObjectTree => ObjectDefinition.ObjectTree;
        protected AtomManager AtomManager => ObjectDefinition.AtomManager;
        protected IDreamMapManager DreamMapManager => ObjectDefinition.DreamMapManager;
        protected IMapManager MapManager => ObjectDefinition.MapManager;
        protected DreamResourceManager DreamResourceManager => ObjectDefinition.DreamResourceManager;
        protected WalkManager WalkManager => ObjectDefinition.WalkManager;
        protected IEntityManager EntityManager => ObjectDefinition.EntityManager;
        protected IPlayerManager PlayerManager => ObjectDefinition.PlayerManager;
        protected ISerializationManager SerializationManager => ObjectDefinition.SerializationManager;
        protected ServerAppearanceSystem? AppearanceSystem => ObjectDefinition.AppearanceSystem;
        protected TransformSystem? TransformSystem => ObjectDefinition.TransformSystem;
        protected PvsOverrideSystem? PvsOverrideSystem => ObjectDefinition.PvsOverrideSystem;
        protected MetaDataSystem? MetaDataSystem => ObjectDefinition.MetaDataSystem;
        protected ServerVerbSystem? VerbSystem => ObjectDefinition.VerbSystem;

        protected Dictionary<string, DreamValue>? Variables;
        //handle to the list of vars on this object so that it's only created once and refs to object.vars are consistent
        private DreamListVars? _varsList;

        private string? Tag {
            get => _tag;
            set {
                // Even if we're setting it to the same string we still need to remove it
                if (!string.IsNullOrEmpty(_tag)) {
                    var list = ObjectDefinition.DreamManager.Tags[_tag];

                    if (list.Count > 1) {
                        list.Remove(this);
                    } else {
                        ObjectDefinition.DreamManager.Tags.Remove(_tag);
                    }
                }

                _tag = value;

                // Now we add it (if it's a string)
                if (!string.IsNullOrEmpty(_tag)) {
                    if (ObjectDefinition.DreamManager.Tags.TryGetValue(_tag, out var list)) {
                        list.Add(this);
                    } else {
                        var newList = new List<DreamObject> {
                            this
                        };

                        ObjectDefinition.DreamManager.Tags.Add(_tag, newList);
                    }
                }
            }
        }
        private string? _tag;

        public DreamObject(DreamObjectDefinition objectDefinition) {
            ObjectDefinition = objectDefinition;

            // Atoms are in world.contents
            if (this is not DreamObjectAtom && IsSubtypeOf(ObjectTree.Datum)) {
                ObjectDefinition.DreamManager.Datums.Add(this);
            }
        }

        public virtual void Initialize(DreamProcArguments args) {
            // For subtypes to implement
        }

        protected virtual void HandleDeletion() {
            // Atoms are in world.contents
            if (this is not DreamObjectAtom && IsSubtypeOf(ObjectTree.Datum)) {
                DreamManager.Datums.Remove(this);
            }

            if (DreamManager.ReferenceIDs.Remove(this, out var refId))
                DreamManager.ReferenceIDsToDreamObject.Remove(refId);

            Tag = null;
            Deleted = true;
            //we release all relevant information, making this a very tiny object
            Variables = null;

            ObjectDefinition = null!;
        }

        public void Delete() {
            if (Deleted)
                return;

            if (TryGetProc("Del", out var delProc))
                DreamThread.Run(delProc, this, null);

            HandleDeletion();
        }

        public bool IsSubtypeOf(TreeEntry ancestor) {
            return ObjectDefinition.IsSubtypeOf(ancestor);
        }

        #region Variables
        public virtual DreamValue Initial(string name) {
            switch (name) {
                case "parent_type": {
                    return (ObjectDefinition.Parent == null || ObjectDefinition.Parent.TreeEntry == ObjectTree.Root) ? DreamValue.Null : new DreamValue(ObjectDefinition.Parent.TreeEntry);
                }
                case "type": {
                    return new DreamValue(ObjectDefinition.TreeEntry);
                }
            }
            return ObjectDefinition.Variables[name];
        }

        public virtual bool IsSaved(string name) {
            return ObjectDefinition.Variables.ContainsKey(name)
                && !ObjectDefinition.GlobalVariables.ContainsKey(name)
                && !(ObjectDefinition.ConstVariables is not null && ObjectDefinition.ConstVariables.Contains(name))
                && !(ObjectDefinition.TmpVariables is not null && ObjectDefinition.TmpVariables.Contains(name));
        }

        public bool HasVariable(string name) {
            DebugTools.Assert(!Deleted, "Cannot call HasVariable() on a deleted object");

            return ObjectDefinition.HasVariable(name);
        }

        public DreamValue GetVariable(string name) {
            DebugTools.Assert(!Deleted, "Cannot call GetVariable() on a deleted object");

            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new KeyNotFoundException($"Variable {name} doesn't exist");
            }
        }

        public IEnumerable<string> GetVariableNames() {
            DebugTools.Assert(!Deleted, "Cannot call GetVariableNames() on a deleted object");

            return ObjectDefinition.Variables.Keys;
        }

        protected virtual bool TryGetVar(string varName, out DreamValue value) {
            switch (varName) {
                case "type":
                    value = new(ObjectDefinition.TreeEntry);
                    return true;
                case "parent_type":
                    if (ObjectDefinition.Parent != null)
                        value = new(ObjectDefinition.Parent.TreeEntry);
                    else
                        value = DreamValue.Null;

                    return true;
                case "vars":
                    _varsList ??= new DreamListVars(ObjectTree.List.ObjectDefinition, this);
                    value = new(_varsList);
                    return true;
                case "tag":
                    value = (Tag != null) ? new(Tag) : DreamValue.Null;
                    return true;
                default:
                    return (Variables?.TryGetValue(varName, out value) is true) ||
                     (ObjectDefinition.Variables.TryGetValue(varName, out value) is true) ||
                        (ObjectDefinition.GlobalVariables.TryGetValue(varName, out var globalIndex) is true) && ObjectDefinition.DreamManager.Globals.TryGetValue(globalIndex, out value);
            }
        }

        protected virtual void SetVar(string varName, DreamValue value) {
            switch (varName) {
                case "type":
                case "parent_type":
                case "vars":
                    throw new Exception($"Cannot set var \"{varName}\"");
                case "tag":
                    value.TryGetValueAsString(out var newTag);

                    Tag = newTag;
                    break;
                default:
                    if (ObjectDefinition.ConstVariables is not null && ObjectDefinition.ConstVariables.Contains(varName))
                        throw new Exception($"Cannot set const var \"{varName}\" on {ObjectDefinition.Type}");
                    if (!ObjectDefinition.Variables.ContainsKey(varName))
                        throw new Exception($"Cannot set var \"{varName}\" on {ObjectDefinition.Type}");

                    SetVariableValue(varName, value);
                    break;
            }
        }

        public bool TryGetVariable(string name, out DreamValue variableValue) {
            DebugTools.Assert(!Deleted, "Cannot call TryGetVariable() on a deleted object");

            return TryGetVar(name, out variableValue);
        }

        /// <summary>
        /// Handles setting a variable, and special behavior by calling OnVariableSet()
        /// </summary>
        public void SetVariable(string name, DreamValue value) {
            DebugTools.Assert(!Deleted, "Cannot call SetVariable() on a deleted object");

            SetVar(name, value);
        }

        /// <summary>
        /// Directly sets a variable's value, bypassing any special behavior
        /// </summary>
        /// <returns>The OLD variable value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVariableValue(string name, DreamValue value) {
            DebugTools.Assert(!Deleted, "Cannot call SetVariableValue() on a deleted object");

            Variables ??= new(4);
            Variables[name] = value;
        }
        #endregion Variables

        #region Proc Helpers
        public DreamProc GetProc(string procName) {
            DebugTools.Assert(!Deleted, "Cannot call GetProc() on a deleted object");

            return ObjectDefinition.GetProc(procName);
        }

        public bool TryGetProc(string procName, [NotNullWhen(true)] out DreamProc? proc) {
            DebugTools.Assert(!Deleted, "Cannot call TryGetProc() on a deleted object");

            return ObjectDefinition.TryGetProc(procName, out proc);
        }

        public void InitSpawn(DreamProcArguments creationArguments) {
            if (ObjectDefinition.NoConstructors) {
                // Skip thread spinup.
                Initialize(creationArguments);
                return;
            }

            var thread = new DreamThread("new " + ObjectDefinition.Type);
            var procState = InitProc(thread, null, creationArguments);

            thread.PushProcState(procState);
            thread.Resume();
        }

        public ProcState InitProc(DreamThread thread, DreamObject? usr, DreamProcArguments arguments) {
            DebugTools.Assert(!Deleted, "Cannot call InitProc() on a deleted object");

            if (!InitDreamObjectState.Pool.TryPop(out var state)) {
                state = new InitDreamObjectState(ObjectDefinition.DreamManager, ObjectDefinition.ObjectTree);
            }

            state.Initialize(thread, this, usr, arguments);
            return state;
        }

        public DreamValue SpawnProc(string procName, DreamObject? usr = null, params DreamValue[] arguments) {
            DebugTools.Assert(!Deleted, "Cannot call SpawnProc() on a deleted object");

            var proc = GetProc(procName);
            return DreamThread.Run(proc, this, usr, arguments);
        }
        #endregion Proc Helpers

        #region Name Helpers
        // This could probably be placed elsewhere. Not sure where tho
        /// <returns>true if \proper noun formatting should be used, false if \improper</returns>
        public static bool StringIsProper(string str) {
            if (str.Length == 0)
                return true;
            if (StringFormatEncoder.Decode(str[0], out var properMaybe)) {
                switch (properMaybe) {
                    case StringFormatEncoder.FormatSuffix.Proper:
                        return true;
                    case StringFormatEncoder.FormatSuffix.Improper:
                        return false;
                }
            }

            if (char.IsWhiteSpace(
                    str[0])) // NOTE: This might result in slightly different behaviour (since C# may be more unicode-friendly about what "whitespace" means)
                return true;
            return char.IsUpper(str[0]);
        }

        public static bool StringStartsWithVowel(string str) {
            if (str.Length == 0)
                return false;
            char start = char.ToLower(str[0], CultureInfo.InvariantCulture);
            switch (start) {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the display name of this object, WITH ALL FORMATTING EVALUATED OR REMOVED!
        /// </summary>
        public string GetDisplayName(StringFormatEncoder.FormatSuffix? suffix = null) {
            // /client is a little special and will return its key var
            // TODO: Maybe this should be an override to GetDisplayName()?
            if (this is DreamObjectClient client)
                return client.Connection.Session!.Name;

            var name = GetRawName();
            bool isProper = StringIsProper(name);
            name = StringFormatEncoder.RemoveFormatting(name); // TODO: Care about other formatting macros for obj names beyond \proper & \improper
            if(!isProper) {
                return name;
            }

            switch(suffix) {
                case StringFormatEncoder.FormatSuffix.UpperDefiniteArticle:
                    return isProper ? name : $"The {name}";
                case StringFormatEncoder.FormatSuffix.LowerDefiniteArticle:
                    return isProper ? name : $"the {name}";
                default:
                    return name;
            }
        }

        /// <summary>
        /// Similar to <see cref="GetDisplayName"/> except it just returns the name as plaintext, with formatting removed. No article or anything.
        /// </summary>
        public string GetNameUnformatted() {
            return StringFormatEncoder.RemoveFormatting(GetRawName());
        }

        /// <summary>
        /// Returns the name of this object with no formatting evaluated
        /// </summary>
        /// <returns></returns>
        public string GetRawName() {
            if (!TryGetVariable("name", out DreamValue nameVar) || !nameVar.TryGetValueAsString(out string? name))
                return ObjectDefinition.Type.ToString();

            return name;
        }
        #endregion Name Helpers

        #region Operators
        // +
        public virtual ProcStatus OperatorAdd(DreamValue b, DMProcState state, out DreamValue? result) {
            if(TryGetProc("operator+", out var proc)) {
                var operatorProcState = proc.CreateState(state.Thread, this, state.Usr, new DreamProcArguments(b));
                state.Thread.PushProcState(operatorProcState);
                result = null;
                return ProcStatus.Called;
            }

            throw new InvalidOperationException($"Addition cannot be done between {this} and {b}");
        }

        // -
        public virtual ProcStatus OperatorSubtract(DreamValue b, DMProcState state, out DreamValue? result) {
            if(TryGetProc("operator-", out var proc)) {
                var operatorProcState = proc.CreateState(state.Thread, this, state.Usr, new DreamProcArguments(b));
                state.Thread.PushProcState(operatorProcState);
                result = null;
                return ProcStatus.Called;
            }

            throw new InvalidOperationException($"Subtraction cannot be done between {this} and {b}");
        }

        // *
        public virtual ProcStatus OperatorMultiply(DreamValue b, DMProcState state, out DreamValue? result) {
            if(TryGetProc("operator*", out var proc)) {
                var operatorProcState = proc.CreateState(state.Thread, this, state.Usr, new DreamProcArguments(b));
                state.Thread.PushProcState(operatorProcState);
                result = null;
                return ProcStatus.Called;
            }

            throw new InvalidOperationException($"Multiplication cannot be done between {this} and {b}");
        }

        public virtual ProcStatus OperatorDivide(DreamValue b, DMProcState state, out DreamValue? result) {
            if(TryGetProc("operator/", out var proc)) {
                var operatorProcState = proc.CreateState(state.Thread, this, state.Usr, new DreamProcArguments(b));
                state.Thread.PushProcState(operatorProcState);
                result = null;
                return ProcStatus.Called;
            }

            throw new InvalidOperationException($"Division cannot be done between {this} and {b}");
        }

        // |
        public virtual ProcStatus OperatorOr(DreamValue b, DMProcState state, out DreamValue? result) {
            if(TryGetProc("operator|", out var proc)) {
                var operatorProcState = proc.CreateState(state.Thread, this, state.Usr, new DreamProcArguments(b));
                state.Thread.PushProcState(operatorProcState);
                result = null;
                return ProcStatus.Called;
            }

            throw new InvalidOperationException($"Cannot or {this} and {b}");
        }

        // +=
        public virtual DreamValue OperatorAppend(DreamValue b) {
            throw new InvalidOperationException($"Cannot append {b} to {this}");
        }

        // -=
        public virtual DreamValue OperatorRemove(DreamValue b) {
            throw new InvalidOperationException($"Cannot remove {b} from {this}");
        }

        // |=
        public virtual DreamValue OperatorCombine(DreamValue b) {
            throw new InvalidOperationException($"Cannot combine {this} and {b}");
        }

        // &=
        public virtual DreamValue OperatorMask(DreamValue b) {
            throw new InvalidOperationException($"Cannot mask {this} and {b}");
        }

        // ~=
        public virtual DreamValue OperatorEquivalent(DreamValue b) {
            if (!b.TryGetValueAsDreamObject(out var bObject))
                return DreamValue.False;

            return Equals(bObject) ? DreamValue.True : DreamValue.False;
        }

        // << statement
        public virtual void OperatorOutput(DreamValue b) {
            throw new InvalidOperationException($"Cannot output {b} to {this}");
        }

        // []
        public virtual DreamValue OperatorIndex(DreamValue index) {
            throw new InvalidOperationException($"Cannot index {this} with {index}");
        }

        // []=
        public virtual void OperatorIndexAssign(DreamValue index, DreamValue value) {
            throw new InvalidOperationException($"Cannot assign {value} to index {index} of {this}");
        }
        #endregion Operators

        public override string ToString() {
            if (Deleted) {
                return "<deleted>";
            }

            string name = GetNameUnformatted();
            if (!string.IsNullOrEmpty(name)) {
                return $"{ObjectDefinition.Type}{{name=\"{name}\"}}";
            }

            return ObjectDefinition.Type;
        }
    }
}
