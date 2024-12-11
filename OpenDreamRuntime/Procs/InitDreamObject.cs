using System.Text;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs {
    internal sealed class InitDreamObjectState : ProcState {
        public static readonly Stack<InitDreamObjectState> Pool = new();

        private readonly DreamManager _dreamMan;
        private readonly DreamObjectTree _objectTree;

        private enum Stage {
        // Need to call the object's (init) proc
            Init,

            // Need to call IDreamMetaObject.OnObjectCreated & New
            OnObjectCreated,

            // Time to return
            Return,
        }

        public InitDreamObjectState(DreamManager dreamManager, DreamObjectTree objectTree) {
            _dreamMan = dreamManager;
            _objectTree = objectTree;
        }

        public void Initialize(DreamThread thread, DreamObject dreamObject, DreamObject? usr, DreamProcArguments arguments) {
            base.Initialize(thread, true);

            _dreamObject = dreamObject;
            _usr = usr;
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

        public override void Dispose() {
            base.Dispose();

            _dreamObject = null!;
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

                    if (src.ObjectDefinition.InitializationProc == null || _objectTree.Procs[src.ObjectDefinition.InitializationProc.Value] is DMProc { IsNullProc: true }) {
                        goto switch_start;
                    }

                    var proc = _objectTree.Procs[src.ObjectDefinition.InitializationProc.Value];
                    var initProcState = proc.CreateState(Thread, src, _usr, new());
                    Thread.PushProcState(initProcState);
                    return ProcStatus.Called;
                }

                case Stage.OnObjectCreated: {
                    _stage = Stage.Return;

                    _dreamObject.Initialize(new DreamProcArguments(_arguments.AsSpan(0, _argumentCount)));

                    if (!_dreamMan.Initialized) {
                        // Suppress all New() calls during /world/<init>() and map loading.
                        goto switch_start;
                    }

                    if (src.ShouldCallNew) {
                        var newProc = src.GetProc("New");
                        if (newProc is DMProc { IsNullProc: true})
                            goto switch_start;

                        var newProcState = newProc.CreateState(Thread, src, _usr, new DreamProcArguments(_arguments.AsSpan(0, _argumentCount)));
                        Thread.PushProcState(newProcState);
                        return ProcStatus.Called;
                    }

                    goto switch_start;
                }

                case Stage.Return:
                    Result = new DreamValue(_dreamObject);
                    return ProcStatus.Returned;
            }

            throw new InvalidOperationException();
        }
    }
}
