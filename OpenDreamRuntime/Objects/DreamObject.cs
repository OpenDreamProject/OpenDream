using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream.Procs;
using System.Globalization;
using System.Linq;

namespace OpenDreamRuntime.Objects {
    [Virtual]
    public class DreamObject {
        public DreamObjectDefinition ObjectDefinition { get; private set; }
        public bool Deleted { get; private set; } = false;

        private Dictionary<string, DreamValue>? _variables;

        public DreamObject(DreamObjectDefinition objectDefinition) {
            ObjectDefinition = objectDefinition;
        }

        public void InitSpawn(DreamProcArguments creationArguments) {
            var thread = new DreamThread("new " + ObjectDefinition.Type);
            var procState = InitProc(thread, null, creationArguments);
            thread.PushProcState(procState);
            thread.Resume();
        }

        public ProcState InitProc(DreamThread thread, DreamObject? usr, DreamProcArguments arguments) {
            if (Deleted) {
                throw new Exception("Cannot init proc on a deleted object");
            }

            if (!InitDreamObjectState.Pool.TryPop(out var state)) {
                state = new InitDreamObjectState(ObjectDefinition.DreamManager, ObjectDefinition.ObjectTree);
            }

            state.Initialize(thread, this, usr, arguments);
            return state;
        }

        public void Delete(IDreamManager manager) {
            if (Deleted) return;
            ObjectDefinition?.MetaObject?.OnObjectDeleted(this);
            Deleted = true;
            //we release all relevant information, making this a very tiny object
            _variables = null;
            ObjectDefinition = null!;

            manager.ReferenceIDs.Remove(this);
        }

        public void SetObjectDefinition(DreamObjectDefinition objectDefinition) {
            ObjectDefinition = objectDefinition;
            _variables?.Clear();
        }

        public bool IsSubtypeOf(IDreamObjectTree.TreeEntry ancestor) {
            return ObjectDefinition.IsSubtypeOf(ancestor);
        }

        public bool HasVariable(string name) {
            if(Deleted){
                return false;
            }
            return ObjectDefinition.HasVariable(name);
        }

        public DreamValue GetVariable(string name) {
            if(Deleted){
                throw new NullReferenceException("Cannot read " + name + " on a deleted object");
            }
            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new KeyNotFoundException("Variable " + name + " doesn't exist");
            }
        }

        public IEnumerable<KeyValuePair<string, DreamValue>> GetAllVariables() {
            return (_variables ?? Enumerable.Empty<KeyValuePair<string, DreamValue>>())
                .Concat(ObjectDefinition.Variables ?? Enumerable.Empty<KeyValuePair<string, DreamValue>>())
                .DistinctBy(kvp => kvp.Key);
        }

        public IEnumerable<string> GetVariableNames() {
            if (Deleted) {
                throw new Exception("Cannot get variable names of a deleted object");
            }
            return ObjectDefinition.Variables.Keys;
        }

        public bool TryGetVariable(string name, out DreamValue variableValue) {
            if(Deleted){
                throw new Exception("Cannot try to get variable on a deleted object");
            }

            if ((_variables?.TryGetValue(name, out variableValue) is true) || ObjectDefinition.Variables.TryGetValue(name, out variableValue)) {
                if (ObjectDefinition.MetaObject != null) variableValue = ObjectDefinition.MetaObject.OnVariableGet(this, name, variableValue);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles setting a variable, and special behavior by calling OnVariableSet()
        /// </summary>
        public void SetVariable(string name, DreamValue value) {
            if(Deleted){
                throw new Exception("Cannot set variable on a deleted object!");
            }
            var oldValue = SetVariableValue(name, value);
            if (ObjectDefinition.MetaObject != null) ObjectDefinition.MetaObject.OnVariableSet(this, name, value, oldValue);
        }

        /// <summary>
        /// Directly sets a variable's value, bypassing any special behavior
        /// </summary>
        /// <returns>The OLD variable value</returns>
        public DreamValue SetVariableValue(string name, DreamValue value) {
            if(Deleted){
                throw new Exception("Cannot set variable on a deleted object");
            }
            if (_variables?.TryGetValue(name, out DreamValue oldValue) is not true)
                oldValue = ObjectDefinition.Variables[name];
            _variables ??= new();
            _variables[name] = value;
            return oldValue;
        }

        public DreamProc GetProc(string procName) {
            if(Deleted){
                throw new Exception("Cannot get proc on a deleted object");
            }
            return ObjectDefinition.GetProc(procName);
        }

        public bool TryGetProc(string procName, out DreamProc proc) {
            if(Deleted){
                throw new Exception("Cannot try to get proc on a deleted object");
            }
            return ObjectDefinition.TryGetProc(procName, out proc);
        }

        public DreamValue SpawnProc(string procName, DreamProcArguments arguments, DreamObject? usr = null) {
            if(Deleted){
                throw new Exception("Cannot spawn proc on a deleted object");
            }
            var proc = GetProc(procName);
            return DreamThread.Run(proc, this, usr, arguments);
        }

        public DreamValue SpawnProc(string procName, DreamObject? usr = null) {
            return SpawnProc(procName, new DreamProcArguments(null), usr);
        }

        /// <returns>true if \proper noun formatting should be used, false if \improper</returns>
        public static bool StringIsProper(string str) // This could probably be placed elsewhere. Not sure where tho
        {
            if (str.Length == 0)
                return true;
            if(StringFormatEncoder.Decode(str[0], out var propermaybe))
            {
                switch (propermaybe)
                {
                    case StringFormatEncoder.FormatSuffix.Proper:
                        return true;
                    case StringFormatEncoder.FormatSuffix.Improper:
                        return false;
                    default:
                        break;
                }
            }
            if (char.IsWhiteSpace(str[0])) // NOTE: This might result in slightly different behaviour (since C# may be more unicode-friendly about what "whitespace" means)
                return true;
            return char.IsUpper(str[0]);
        }

        public static bool StringStartsWithVowel(string str)
        {
            if (str.Length == 0)
                return false;
            char start = char.ToLower(str[0], CultureInfo.InvariantCulture);
            switch (start)
            {
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
            if (!TryGetVariable("name", out DreamValue nameVar) || !nameVar.TryGetValueAsString(out string? name))
                return ObjectDefinition?.Type.ToString() ?? String.Empty;
            bool isProper = StringIsProper(name);
            name = StringFormatEncoder.RemoveFormatting(name); // TODO: Care about other formatting macros for obj names beyond \proper & \improper
            if(!isProper)
            {
                return name;
            }
            switch(suffix)
            {
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
        public string GetNameUnformatted()
        {
            if (!TryGetVariable("name", out DreamValue nameVar) || !nameVar.TryGetValueAsString(out string? name))
                return ObjectDefinition?.Type.ToString() ?? String.Empty;
            return StringFormatEncoder.RemoveFormatting(name);
        }

        public override string ToString() {
            if (Deleted) {
                return "<deleted>";
            }

            string name = GetNameUnformatted();
            if (!string.IsNullOrEmpty(name)) {
                return $"{ObjectDefinition.Type}{{name=\"{name}\"}}";
            }
            return ObjectDefinition.Type.ToString();
        }
    }
}
