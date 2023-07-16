using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs;

public unsafe sealed class FastNativeProc : DreamProc
{
    public delegate DreamValue HandlerFn(FastNativeProcBundle bundle, DreamObject? src, DreamObject? usr);

    private readonly DreamManager _dreamManager;
    private readonly AtomManager _atomManager;
    private readonly IDreamMapManager _mapManager;
    private readonly DreamResourceManager _resourceManager;
    private readonly DreamObjectTree _objectTree;

    public ref struct FastNativeProcBundle {
        public FastNativeProc Proc;

        // NOTE: Deliberately not using DreamProcArguments here, tis slow.
        public readonly ReadOnlySpan<DreamValue> Arguments;

        public DreamManager DreamManager => Proc._dreamManager;
        public AtomManager AtomManager => Proc._atomManager;
        public IDreamMapManager MapManager => Proc._mapManager;
        public DreamResourceManager ResourceManager => Proc._resourceManager;
        public DreamObjectTree ObjectTree => Proc._objectTree;

        public FastNativeProcBundle(FastNativeProc proc, DreamProcArguments arguments) {
            Proc = proc;
            Arguments = arguments.Values;
        }

        [Pure]
        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (Arguments.Length > argumentPosition) {
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
    private readonly delegate*<FastNativeProcBundle, DreamObject?, DreamObject?, DreamValue> _handler;

    public unsafe FastNativeProc(int id, DreamPath owningType, string name, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler, DreamManager dreamManager, AtomManager atomManager, IDreamMapManager mapManager, DreamResourceManager resourceManager, DreamObjectTree objectTree)
        : base(id, owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null) {
        _defaultArgumentValues = defaultArgumentValues;
        _handler = (delegate*<FastNativeProcBundle, DreamObject?, DreamObject?, DreamValue>)handler.Method.MethodHandle.GetFunctionPointer();

        _dreamManager = dreamManager;
        _atomManager = atomManager;
        _mapManager = mapManager;
        _resourceManager = resourceManager;
        _objectTree = objectTree;
    }

    public int CallCount = 0;

    public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr,
        DreamProcArguments arguments)
    {
        throw new NotImplementedException(); // By design.
    }

    public DreamValue FastCall(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
        var bundle = new FastNativeProcBundle(this, arguments);
        CallCount++;
        return _handler(bundle, src, usr);
    }
}
