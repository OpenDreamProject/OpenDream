using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs;

public sealed class FastNativeProc : DreamProc
{
    internal delegate DreamValue HandlerFn(FastNativeProcBundle bundle, DreamObject? src, DreamObject? usr);

    private readonly DreamManager _dreamManager;
    private readonly AtomManager _atomManager;
    private readonly DreamMapManager _mapManager;
    private readonly DreamResourceManager _resourceManager;
    private readonly DreamObjectTree _objectTree;

    internal ref struct FastNativeProcBundle {
        public FastNativeProc Proc;

        // TODO: Evaluate if it's faster to just entirely inline this.
        public DreamProcArguments Arguments;

        public DreamManager DreamManager => Proc._dreamManager;
        public AtomManager AtomManager => Proc._atomManager;
        public DreamMapManager MapManager => Proc._mapManager;
        public DreamResourceManager ResourceManager => Proc._resourceManager;
        public DreamObjectTree ObjectTree => Proc._objectTree;

        public FastNativeProcBundle(FastNativeProc proc, DreamProcArguments arguments) {
            Proc = proc;
            Arguments = arguments;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            var r = Arguments.GetArgument(argumentPosition);
            if (argumentPosition < Arguments.Values.Length && !r.IsNull)
                return r;

            return Proc._defaultArgumentValues?.TryGetValue(argumentName, out var argValue) == true ? argValue : DreamValue.Null;
        }
    }

    private readonly Dictionary<string, DreamValue>? _defaultArgumentValues;
    private readonly HandlerFn _handler;

    internal FastNativeProc(int id, DreamPath owningType, string name, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler, IDreamManager dreamManager, IAtomManager atomManager, IDreamMapManager mapManager, DreamResourceManager resourceManager, IDreamObjectTree objectTree)
        : base(id, owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null) {
        _defaultArgumentValues = defaultArgumentValues;
        _handler = handler;

        // todo: remove the stupid OOP!! This shit shouldn't have to be internal all these interfaces are redundant as fuck
        _dreamManager = (DreamManager)dreamManager;
        _atomManager = (AtomManager)atomManager;
        _mapManager = (DreamMapManager)mapManager;
        _resourceManager = resourceManager;
        _objectTree = (DreamObjectTree)objectTree;
    }

    public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr,
        DreamProcArguments arguments)
    {
        throw new NotImplementedException(); // By design.
    }

    public DreamValue FastCall(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
        var bundle = new FastNativeProcBundle(this, arguments);

        return _handler(bundle, src, usr);
    }
}
