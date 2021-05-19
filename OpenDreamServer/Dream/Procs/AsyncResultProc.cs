using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs {
    // TODO: Delete this in favor of AsyncNativeProc
    class AsyncResultProc : DreamProc {
        public static AsyncResultProc Instance = new();

        class State : ProcState
        {
            public State(DreamThread context, Action<DreamValue> handler, ProcState targetState)
                : base(context)
            {
                _handler = handler;
                _targetState = targetState;
            }

            private Action<DreamValue> _handler;
            private ProcState _targetState;            

            public override DreamProc Proc => AsyncResultProc.Instance;

            public override void AppendStackFrame(StringBuilder builder)
            {
                builder.AppendLine("<async wrapper>");
            }

            protected override ProcStatus InternalResume()
            {
                // If _handler is null it means our target proc has returned. We can return too.
                if (_handler == null) {
                    return ProcStatus.Returned;
                }

                // This is our first resume, call the target proc
                Context.PushProcState(_targetState);
                return ProcStatus.Called;
            }

            public override void ReturnedInto(DreamValue value) {
                _handler.Invoke(value);
                _handler = null;
            }
        }

        public AsyncResultProc()
            : base("<async wrapper>", null, null, null)
        {}

        public override ProcState CreateState(DreamThread context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            // This proc's state gets instantiated through a new overload. It shouldn't reach this path.
            throw new NotImplementedException();
        }

        public ProcState CreateState(DreamThread context, Action<DreamValue> handler, DreamProc targetProc, DreamObject src, DreamObject usr, DreamProcArguments arguments) {
            var targetState = targetProc.CreateState(context, src, usr, arguments);
            return new State(context, handler, targetState);
        }
    }
}