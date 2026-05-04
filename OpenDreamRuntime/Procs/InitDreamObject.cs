using System.Text;
using JetBrains.Annotations;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs;

internal sealed class InitDreamObjectState(DreamManager dreamManager, DreamObjectTree objectTree) : ProcState {
    public static readonly Stack<InitDreamObjectState> Pool = new();

    private enum Stage {
        // Need to call the object's (init) proc
        Init,

        // Need to call IDreamMetaObject.OnObjectCreated & New
        OnObjectCreated,

        // Time to return
        Return,
    }

    public void Initialize(DreamThread thread, DreamObject dreamObject, DreamObject? usr, [HandlesResourceDisposal] DreamProcArguments arguments) {
        base.Initialize(thread, true);

        _dreamObject = dreamObject;
        _dreamObject.IncRef();
        _usr = usr;
        _usr?.IncRef();
        arguments.Values.CopyTo(_arguments);
        _argumentCount = arguments.Count;
        _stage = Stage.Init;
    }

    private DreamObject _dreamObject;
    private DreamObject? _usr;
    private readonly DreamValue[] _arguments = new DreamValue[256];
    private int _argumentCount;
    private Stage _stage = Stage.Init;

    public override DreamProc? Proc => null;

#if TOOLS
        public override (string SourceFile, int Line) TracyLocationId => ($"new {_dreamObject.ObjectDefinition.Type}",0);
#endif

    public override void AppendStackFrame(StringBuilder builder) {
        builder.AppendLine($"new {_dreamObject.ObjectDefinition.Type}");
    }

    public override ReadOnlySpan<DreamValue> GetArguments() {
        return _arguments.AsSpan(0, ArgumentCount);
    }

    public override void SetArgument(int id, DreamValue value) {
        if (id < 0 || id >= ArgumentCount)
            throw new IndexOutOfRangeException($"Given argument id ({id}) was out of range");

        value.IncRef();
        _arguments[id].DecRef();
        _arguments[id] = value;
    }

    public override void Dispose() {
        base.Dispose();

        for (int i = 0; i < _argumentCount; i++) {
            _arguments[i].Dispose();
        }

        Array.Clear(_arguments, 0, _argumentCount);
        _dreamObject.DecRef();
        _dreamObject = null!;
        _usr?.DecRef();
        _usr = null;
        _argumentCount = 0;

        Pool.Push(this);
    }

    public override ProcStatus Resume() {
        var src = _dreamObject;

        switch_start:
        switch (_stage) {
            case Stage.Init: {
                _stage = Stage.OnObjectCreated;

                if (src.ObjectDefinition.InitializationProc == null || objectTree.Procs[src.ObjectDefinition.InitializationProc.Value] is DMProc { IsNullProc: true }) {
                    goto switch_start;
                }

                var proc = objectTree.Procs[src.ObjectDefinition.InitializationProc.Value];
                var initProcState = proc.CreateState(Thread, src, _usr, new());
                Thread.PushProcState(initProcState);
                return ProcStatus.Called;
            }

            case Stage.OnObjectCreated: {
                _stage = Stage.Return;

                using var initArgs = new DreamProcArguments(_arguments.AsSpan(0, _argumentCount));
                _dreamObject.Initialize(initArgs);

                if (!dreamManager.Initialized) {
                    // Suppress all New() calls during /world/<init>() and map loading.
                    goto switch_start;
                }

                if (src.ShouldCallNew) {
                    var newProc = src.GetProc("New");
                    if (newProc is DMProc { IsNullProc: true })
                        goto switch_start;

                    var args = _arguments.AsSpan(0, _argumentCount);
                    var newProcState = newProc.CreateState(Thread, src, _usr, new DreamProcArguments(args));
                    Thread.PushProcState(newProcState);
                    return ProcStatus.Called;
                }

                goto switch_start;
            }

            case Stage.Return:
                Result = new DreamValue(_dreamObject);
                Result.IncRef();
                return ProcStatus.Returned;
        }

        throw new InvalidOperationException();
    }
}
