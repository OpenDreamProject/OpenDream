using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Objects {
    [Virtual]
    public class DreamObject {
        public DreamObjectDefinition? ObjectDefinition { get; protected set; }
        public bool Deleted = false;

        private Dictionary<string, DreamValue> _variables = new();

        public DreamObject(DreamObjectDefinition? objectDefinition) {
            ObjectDefinition = objectDefinition;
        }

        public void InitSpawn(DreamProcArguments creationArguments) {
            var thread = new DreamThread();
            var procState = InitProc(thread, null, creationArguments);
            thread.PushProcState(procState);

            if (thread.Resume() == DreamValue.Null) {
                thread.HandleException(new InvalidOperationException("DreamObject.InitSpawn called a yielding proc!"));
            }
        }

        public ProcState InitProc(DreamThread thread, DreamObject usr, DreamProcArguments arguments) {
            if(Deleted){
                throw new Exception("Cannot init proc on a deleted object");
            }
            return new InitDreamObjectState(thread, this, usr, arguments);
        }

        public static DreamObject? GetFromReferenceID(IDreamManager manager, string refId) {
            foreach (KeyValuePair<DreamObject, string> referenceIdPair in manager.ReferenceIDs) {
                if (referenceIdPair.Value == refId) return referenceIdPair.Key;
            }

            return null;
        }

        public void Delete(IDreamManager manager) {
            if (Deleted) return;
            ObjectDefinition?.MetaObject?.OnObjectDeleted(this);
            Deleted = true;
            //we release all relevant information, making this a very tiny object
            _variables = null;
            ObjectDefinition = null;

            manager.ReferenceIDs.Remove(this);
        }

        public void SetObjectDefinition(DreamObjectDefinition objectDefinition) {
            ObjectDefinition = objectDefinition;
            _variables.Clear();
        }

        public bool IsSubtypeOf(DreamPath path) {
            return ObjectDefinition.IsSubtypeOf(path);
        }

        public bool HasVariable(string name) {
            if(Deleted){
                return false;
            }
            return ObjectDefinition.HasVariable(name);
        }

        public DreamValue GetVariable(string name) {
            if(Deleted){
                throw new Exception("Cannot read " + name + " on a deleted object");
            }
            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new Exception("Variable " + name + " doesn't exist");
            }
        }

        public List<DreamValue> GetVariableNames() {
            if(Deleted){
                throw new Exception("Cannot get variable names of a deleted object");
            }
            List<DreamValue> list = new(_variables.Count);
            // This is only ever called on a few specific types, none of them /list, so ObjectDefinition must be non-null.
            foreach (String key in ObjectDefinition!.Variables.Keys) { 
                list.Add(new(key));
            }
            return list;
        }

        public bool TryGetVariable(string name, out DreamValue variableValue) {
            if(Deleted){
                throw new Exception("Cannot try to get variable on a deleted object");
            }
            if (_variables.TryGetValue(name, out variableValue) || ObjectDefinition.Variables.TryGetValue(name, out variableValue)) {
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
            DreamValue oldValue = _variables.ContainsKey(name) ? _variables[name] : ObjectDefinition.Variables[name];
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

        /// <returns>true if /proper noun formatting should be used, false if \improper</returns>
        public static bool PropernessOfString(string str) // This could probably be placed elsewhere. Not sure where tho
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
            return char.IsUpper(str[0]);
        }

        public static bool StringStartsWithVowel(string str)
        {
            if (str.Length == 0)
                return false;
            char start = str.Substring(0, 1).ToLower()[0];
            switch(start)
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
            if (!TryGetVariable("name", out DreamValue nameVar) || !nameVar.TryGetValueAsString(out string name))
                return ObjectDefinition?.Type.ToString() ?? String.Empty;
            bool isProper = PropernessOfString(name);
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
            if (!TryGetVariable("name", out DreamValue nameVar) || !nameVar.TryGetValueAsString(out string name))
                return ObjectDefinition?.Type.ToString() ?? String.Empty;
            return StringFormatEncoder.RemoveFormatting(name);
        }

        public override string ToString() {
            if(Deleted) {
                return "DreamObject(DELETED)";
            }

            return "DreamObject(" + ObjectDefinition.Type + ")";
        }
    }
}
