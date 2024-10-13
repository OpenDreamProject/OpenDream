using System.Reflection;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using DMCompiler.DM;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using System.Threading;

namespace OpenDreamRuntime.Procs;

public sealed unsafe class NativeProc : DreamProc {
    public delegate DreamValue HandlerFn(Bundle bundle, DreamObject? src, DreamObject? usr);

    public static (string, Dictionary<string, DreamValue>?, List<string>) GetNativeInfo(Delegate func) {
        List<Attribute> attributes = new(func.GetInvocationList()[0].Method.GetCustomAttributes());
        DreamProcAttribute? procAttribute = (DreamProcAttribute?)attributes.Find(attribute => attribute is DreamProcAttribute);
        if (procAttribute == null) throw new ArgumentException();

        Dictionary<string, DreamValue>? defaultArgumentValues = null;
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

    private readonly DreamManager _dreamManager;
    private readonly AtomManager _atomManager;
    private readonly IDreamMapManager _mapManager;
    private readonly DreamResourceManager _resourceManager;
    private readonly WalkManager _walkManager;
    private readonly DreamObjectTree _objectTree;

    public readonly ref struct Bundle {
        public readonly NativeProc Proc;

        // NOTE: Deliberately not using DreamProcArguments here, tis slow.
        public readonly ReadOnlySpan<DreamValue> Arguments;

        public DreamManager DreamManager => Proc._dreamManager;
        public AtomManager AtomManager => Proc._atomManager;
        public IDreamMapManager MapManager => Proc._mapManager;
        public DreamResourceManager ResourceManager => Proc._resourceManager;
        public WalkManager WalkManager => Proc._walkManager;
        public DreamObjectTree ObjectTree => Proc._objectTree;
        private readonly DreamThread _thread;
        public DreamValue? LastAnimatedObject {
            get => _thread.LastAnimatedObject;
            set => _thread.LastAnimatedObject = value;
        }

        public Bundle(NativeProc proc, DreamThread thread, DreamProcArguments arguments) {
            Proc = proc;
            Arguments = arguments.Values;
            _thread = thread;
        }

        [Pure]
        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (Arguments.Length > argumentPosition && !Arguments[argumentPosition].IsNull) {
                return Arguments[argumentPosition];
            }

            return GetArgumentFallback(argumentName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DreamValue GetArgumentFallback(string argumentName) {
            return Proc._defaultArgumentValues?.TryGetValue(argumentName, out var argValue) == true ? argValue : DreamValue.Null;
        }
    }

    private readonly Dictionary<string, DreamValue>? _defaultArgumentValues;
    private readonly delegate*<Bundle, DreamObject?, DreamObject?, DreamValue> _handler;

    public NativeProc(int id, TreeEntry owningType, string name, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler, DreamManager dreamManager, AtomManager atomManager, IDreamMapManager mapManager, DreamResourceManager resourceManager, WalkManager walkManager, DreamObjectTree objectTree)
        : base(id, owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null, 0) {
        _defaultArgumentValues = defaultArgumentValues;
        _handler = (delegate*<Bundle, DreamObject?, DreamObject?, DreamValue>)handler.Method.MethodHandle.GetFunctionPointer();

        _dreamManager = dreamManager;
        _atomManager = atomManager;
        _mapManager = mapManager;
        _resourceManager = resourceManager;
        _walkManager = walkManager;
        _objectTree = objectTree;
    }

    public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr,
        DreamProcArguments arguments) {
        throw new InvalidOperationException("Synchronous native procs cannot create a state. Use Call() instead.");
    }

    public DreamValue Call(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
        var bundle = new Bundle(this, thread, arguments);

        // TODO: Include this call in the thread's stack in error traces
        return _handler(bundle, src, usr);
    }
}
