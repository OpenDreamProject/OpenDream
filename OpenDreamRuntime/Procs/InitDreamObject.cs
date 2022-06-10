using System.Text;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs {
    sealed class InitDreamObjectState : ProcState
    {
        [Dependency] private readonly IDreamManager _dreamMan = default!;
        enum Stage {
            // Need to call the object's (init) proc
            Init,

            // Need to call IDreamMetaObject.OnObjectCreated & New
            OnObjectCreated,

            // Time to return
            Return,
        }

        public InitDreamObjectState(DreamThread thread, DreamObject dreamObject, DreamObject usr, DreamProcArguments arguments)
            : base(thread)
        {
            IoCManager.InjectDependencies(this);
            _dreamObject = dreamObject;
            _usr = usr;
            _arguments = arguments;
        }

        private DreamObject _dreamObject;
        private DreamObject _usr;
        private DreamProcArguments _arguments;
        private Stage _stage = Stage.Init;

        public override DreamProc Proc => null;

        public override void AppendStackFrame(StringBuilder builder)
        {
            builder.AppendLine($"<InitDreamObject {_dreamObject}>");
        }

        protected override ProcStatus InternalResume()
        {
            var src = _dreamObject;

            switch_start:
            switch (_stage) {
                case Stage.Init: {
                    _stage = Stage.OnObjectCreated;

                    if (src.ObjectDefinition.InitializationProc == null) {
                        goto switch_start;
                    }

                    var proc = _dreamMan.ObjectTree.Procs[src.ObjectDefinition.InitializationProc.Value];
                    var initProcState = proc.CreateState(Thread, src, _usr, new(null));
                    Thread.PushProcState(initProcState);
                    return ProcStatus.Called;
                }

                case Stage.OnObjectCreated: {
                    _stage = Stage.Return;

                    if (src.ObjectDefinition.MetaObject == null) {
                        goto switch_start;
                    }

                    _dreamObject.ObjectDefinition.MetaObject.OnObjectCreated(_dreamObject, _arguments);

                    if (src.ObjectDefinition.MetaObject.ShouldCallNew) {
                        var newProc = src.GetProc("New");
                        var newProcState = newProc.CreateState(Thread, src, _usr, _arguments);
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
