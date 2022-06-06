using System.Threading.Tasks;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects {
    public sealed class DreamObjectDefinition
    {
        [Dependency] private readonly IDreamManager _dreamMan = default!;
        public DreamPath Type;
        public IDreamMetaObject MetaObject = null;
        public int? InitializationProc;
        public readonly Dictionary<string, int> Procs = new();
        public readonly Dictionary<string, int> OverridingProcs = new();
        public readonly Dictionary<string, DreamValue> Variables = new();
        public readonly Dictionary<string, int> GlobalVariables = new();

        private DreamObjectDefinition _parentObjectDefinition = null;

        public DreamObjectDefinition(DreamPath type)
        {
            IoCManager.InjectDependencies(this);
            Type = type;
        }

        public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
            IoCManager.InjectDependencies(this);
            Type = copyFrom.Type;
            MetaObject = copyFrom.MetaObject;
            InitializationProc = copyFrom.InitializationProc;
            _parentObjectDefinition = copyFrom._parentObjectDefinition;

            Variables = new Dictionary<string, DreamValue>(copyFrom.Variables);
            GlobalVariables = new Dictionary<string, int>(copyFrom.GlobalVariables);
            Procs = new Dictionary<string, int>(copyFrom.Procs);
            OverridingProcs = new Dictionary<string, int>(copyFrom.OverridingProcs);
        }

        public DreamObjectDefinition(DreamPath type, DreamObjectDefinition parentObjectDefinition) {
            IoCManager.InjectDependencies(this);
            Type = type;
            InitializationProc = parentObjectDefinition.InitializationProc;
            _parentObjectDefinition = parentObjectDefinition;

            Variables = new Dictionary<string, DreamValue>(parentObjectDefinition.Variables);
            GlobalVariables = new Dictionary<string, int>(parentObjectDefinition.GlobalVariables);
        }

        public void SetVariableDefinition(string variableName, DreamValue value) {
            Variables[variableName] = value;
        }

        public void SetProcDefinition(string procName, int procId) {
            if (HasProc(procName))
            {
                var proc = _dreamMan.ObjectTree.Procs[procId];
                proc.SuperProc = GetProc(procName);
                OverridingProcs[procName] = procId;
            } else {
                Procs[procName] = procId;
            }
        }

        public void SetNativeProc(NativeProc.HandlerFn func)
        {
            var proc = _dreamMan.ObjectTree.CreateNativeProc(func, out var procId);
            SetProcDefinition(proc.Name, procId);
        }

        public void SetNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var proc = _dreamMan.ObjectTree.CreateAsyncNativeProc(func, out var procId);
            SetProcDefinition(proc.Name, procId);
        }

        public DreamProc GetProc(string procName) {
            if (TryGetProc(procName, out DreamProc proc)) {
                return proc;
            } else {
                throw new Exception("Object type '" + Type + "' does not have a proc named '" + procName + "'");
            }
        }

        public bool TryGetProc(string procName, out DreamProc proc) {
            if (OverridingProcs.TryGetValue(procName, out var procId))
            {
                proc = _dreamMan.ObjectTree.Procs[procId];
                return true;
            } else if (Procs.TryGetValue(procName, out procId)) {
                proc = _dreamMan.ObjectTree.Procs[procId];
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.TryGetProc(procName, out proc);
            } else
            {
                proc = null;
                return false;
            }
        }

        public bool HasProc(string procName) {
            if (Procs.ContainsKey(procName)) {
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.HasProc(procName);
            } else {
                return false;
            }
        }

        public bool HasVariable(string variableName) {
            return Variables.ContainsKey(variableName);
        }

        public bool TryGetVariable(string varName, out DreamValue value) {
            if (Variables.TryGetValue(varName, out value)) {
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.TryGetVariable(varName, out value);
            } else {
                return false;
            }
        }

        public bool IsSubtypeOf(DreamPath path) {
            if (Type.IsDescendantOf(path)) return true;
            else if (_parentObjectDefinition != null) return _parentObjectDefinition.IsSubtypeOf(path);
            else return false;
        }
    }
}
