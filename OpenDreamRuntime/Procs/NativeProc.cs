using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public sealed class NativeProc : DreamProc {
        public delegate DreamValue HandlerFn(State state);

        public static (string, Dictionary<string, DreamValue>, List<String>) GetNativeInfo(Delegate func) {
            List<Attribute> attributes = new(func.GetInvocationList()[0].Method.GetCustomAttributes());
            DreamProcAttribute procAttribute = (DreamProcAttribute)attributes.Find(attribute => attribute is DreamProcAttribute);
            if (procAttribute == null) throw new ArgumentException();

            Dictionary<string, DreamValue> defaultArgumentValues = null;
            var argumentNames = new List<string>();
            List<Attribute> parameterAttributes = attributes.FindAll(attribute => attribute is DreamProcParameterAttribute);
            foreach (Attribute attribute in parameterAttributes) {
                DreamProcParameterAttribute parameterAttribute = (DreamProcParameterAttribute)attribute;

                argumentNames.Add(parameterAttribute.Name);
                if (parameterAttribute.DefaultValue != default) {
                    defaultArgumentValues ??= new Dictionary<string, DreamValue>(1);
                    DreamValue defaultValue = parameterAttribute.DefaultValue switch {
                        // These are the only types you should be able to set in an attribute
                        int intValue => new(intValue),
                        float floatValue => new(floatValue),
                        string stringValue => new(stringValue),
                        _ => throw new Exception($"Invalid default value {parameterAttribute.DefaultValue}")
                    };

                    defaultArgumentValues.Add(parameterAttribute.Name, defaultValue);
                }
            }

            return (procAttribute.Name, defaultArgumentValues, argumentNames);
        }

        public sealed class State : ProcState {
            public static readonly Stack<State> Pool = new();

            public DreamObject? Src; // TODO: Maybe make this a generic so Src doesn't have to be casted
            public DreamObject? Usr;

            public DreamProcArguments Arguments => new(_arguments.AsSpan(0, _argumentCount));
            private readonly DreamValue[] _arguments = new DreamValue[128];
            private int _argumentCount;

            private NativeProc _proc = default!;
            public override NativeProc Proc => _proc;

            public IDreamManager DreamManager => _proc._dreamManager;
            public IAtomManager AtomManager => _proc._atomManager;
            public IDreamMapManager MapManager => _proc._mapManager;
            public DreamResourceManager ResourceManager => _proc._resourceManager;
            public IDreamObjectTree ObjectTree => _proc._objectTree;

            public void Initialize(NativeProc proc, DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
                base.Initialize(thread, true);

                _proc = proc;
                Src = src;
                Usr = usr;
                arguments.Values.CopyTo(_arguments);
                _argumentCount = arguments.Count;
            }

            public override ProcStatus Resume() {
                Result = _proc._handler.Invoke(this);

                return ProcStatus.Returned;
            }

            public override void AppendStackFrame(StringBuilder builder) {
                builder.Append($"{_proc.Name}");
            }

            public override void Dispose() {
                base.Dispose();

                _argumentCount = 0;
                Src = null!;
                Usr = null!;
                _proc = null!;

                Pool.Push(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DreamValue GetArgument(int argumentPosition, string argumentName) {
                if (argumentPosition < _argumentCount && _arguments[argumentPosition] != DreamValue.Null)
                    return _arguments[argumentPosition];

                return _proc._defaultArgumentValues?.TryGetValue(argumentName, out var argValue) == true ? argValue : DreamValue.Null;
            }
        }

        private readonly IDreamManager _dreamManager;
        private readonly IAtomManager _atomManager;
        private readonly IDreamMapManager _mapManager;
        private readonly DreamResourceManager _resourceManager;
        private readonly IDreamObjectTree _objectTree;

        private readonly Dictionary<string, DreamValue>? _defaultArgumentValues;
        private readonly HandlerFn _handler;

        public NativeProc(int id, DreamPath owningType, string name, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler, IDreamManager dreamManager, IAtomManager atomManager, IDreamMapManager mapManager, DreamResourceManager resourceManager, IDreamObjectTree objectTree)
            : base(id, owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null) {
            _defaultArgumentValues = defaultArgumentValues;
            _handler = handler;

            _dreamManager = dreamManager;
            _atomManager = atomManager;
            _mapManager = mapManager;
            _resourceManager = resourceManager;
            _objectTree = objectTree;
        }

        public override State CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
            if (!State.Pool.TryPop(out var state)) {
                state = new State();
            }

            state.Initialize(this, thread, src, usr, arguments);
            return state;
        }
    }
}
