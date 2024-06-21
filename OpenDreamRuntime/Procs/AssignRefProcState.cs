using System.Text;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs;
internal sealed class AssignRefProcState : ProcState {
    public static readonly Stack<AssignRefProcState> Pool = new();
    private enum Stage {
        // Call the associated proc
        Call,

        // Handle the assignment
        AssignRef,
    }

    private DreamObject _dreamObject;
    private DreamObject? _usr;
    private readonly DreamValue[] _arguments = new DreamValue[256];
    private int _argumentCount;
    private Stage _stage = Stage.Call;
    public override DreamProc? Proc => null;
    private DreamProc _overloadProc;
    private DMProcState _parentState;
    private DreamReference _assignRef;

    public AssignRefProcState() {
    }

    public void Initialize(DMProcState state, DreamProc overloadProc, DreamObject dreamObject, DreamObject? usr, DreamProcArguments arguments, DreamReference assignRef) {
        base.Initialize(state.Thread, true);
        _parentState = state;
        _overloadProc = overloadProc;
        _dreamObject = dreamObject;
        _usr = usr;
        arguments.Values.CopyTo(_arguments);
        _argumentCount = arguments.Count;
        _stage = Stage.Call;
        _assignRef = assignRef;
    }


    public override void AppendStackFrame(StringBuilder builder) {
        builder.AppendLine($"Operator overload: {_overloadProc}");
    }

    public override void Dispose() {
        base.Dispose();
        _overloadProc = null!;
        _dreamObject = null!;
        _usr = null;
        _argumentCount = 0;
        _parentState = null!;

        Pool.Push(this);
    }

    public override ProcStatus Resume() {
        var src = _dreamObject;

        switch (_stage) {
            case Stage.Call: {
                _stage = Stage.AssignRef;
                ProcState overloadProcState = _overloadProc.CreateState(Thread, src, _usr, new DreamProcArguments(_arguments.AsSpan(0, _argumentCount)));
                Thread.PushProcState(overloadProcState);
                return ProcStatus.Called;
            }
            case Stage.AssignRef: {
                //the assignment is handled in ReturnedInto()
                return ProcStatus.Returned;
            }
        }

        throw new InvalidOperationException();
    }

    public override void ReturnedInto(DreamValue value) {
        _parentState.AssignReference(_assignRef, value);
        _parentState.Push(value);
    }
}

