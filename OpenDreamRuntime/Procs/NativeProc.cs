using System.Reflection;
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
                    if (defaultArgumentValues == null) defaultArgumentValues = new Dictionary<string, DreamValue>();

                    defaultArgumentValues.Add(parameterAttribute.Name, new DreamValue(parameterAttribute.DefaultValue));
                }
            }

            return (procAttribute.Name, defaultArgumentValues, argumentNames);
        }

        public sealed class State : ProcState {
            public static readonly Stack<State> Pool = new();

            public DreamObject? Src;
            public DreamObject? Usr;
            public DreamProcArguments Arguments;

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
                Arguments = arguments;
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

                Src = null!;
                Usr = null!;
                Arguments = default;
                _proc = null!;

                Pool.Push(this);
            }
        }

        private readonly IDreamManager _dreamManager;
        private readonly IAtomManager _atomManager;
        private readonly IDreamMapManager _mapManager;
        private readonly DreamResourceManager _resourceManager;
        private readonly IDreamObjectTree _objectTree;

        private readonly Dictionary<string, DreamValue> _defaultArgumentValues;
        private readonly HandlerFn _handler;

        public NativeProc(DreamPath owningType, string name, List<String> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler, IDreamManager dreamManager, IAtomManager atomManager, IDreamMapManager mapManager, DreamResourceManager resourceManager, IDreamObjectTree objectTree)
            : base(owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null) {
            _defaultArgumentValues = defaultArgumentValues;
            _handler = handler;

            _dreamManager = dreamManager;
            _atomManager = atomManager;
            _mapManager = mapManager;
            _resourceManager = resourceManager;
            _objectTree = objectTree;
        }

        public override State CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
            if (_defaultArgumentValues != null) {
                var newNamedArguments = arguments.NamedArguments;
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        newNamedArguments ??= new();
                        newNamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
                arguments = new DreamProcArguments(arguments.OrderedArguments, newNamedArguments);
            }

            if (!State.Pool.TryPop(out var state)) {
                state = new State();
            }

            state.Initialize(this, thread, src, usr, arguments);
            return state;
        }
    }
}
