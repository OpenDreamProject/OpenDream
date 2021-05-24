using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream.Procs;
using OpenDreamRuntime.Procs.Native;

namespace OpenDreamRuntime.Procs {
    public class NativeProc : DreamProc {
        public delegate DreamValue HandlerFn(DreamObject src, DreamObject usr, DreamProcArguments arguments);

        public class State : ProcState
        {
            public DreamObject Src;
            public DreamObject Usr;
            public DreamProcArguments Arguments;
            
            private NativeProc _proc;
            public override DreamProc Proc => _proc;

            public State(NativeProc proc, DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments)
                : base(thread)
            {
                _proc = proc;
                Src = src;
                Usr = usr;
                Arguments = arguments;
            }

            protected override ProcStatus InternalResume()
            {
                DreamProcNativeRoot.RuntimeStack.Push(Thread.Runtime);

                try {
                    Result = _proc.Handler.Invoke(Src, Usr, Arguments);
                } catch (Exception) {
                    throw;
                } finally {
                    DreamProcNativeRoot.RuntimeStack.Pop();
                }

                return ProcStatus.Returned;
            }

            public override void AppendStackFrame(StringBuilder builder)
            {
                if (_proc == null) {
                    builder.Append("<anonymous proc>");
                    return;
                }

                builder.Append($"{_proc.Name}(...)");
            }
        }

        private Dictionary<string, DreamValue> _defaultArgumentValues;
        public HandlerFn Handler { get; }

        public NativeProc(string name, DreamRuntime runtime, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler)
            : base(name, runtime, superProc, true, argumentNames, argumentTypes)
        {
            _defaultArgumentValues = defaultArgumentValues;
            Handler = handler;
        }

        public override State CreateState(DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            if (_defaultArgumentValues != null) {
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
            }

            return new State(this, thread, src, usr, arguments);
        }

        public static ProcState CreateAnonymousState(DreamThread thread, HandlerFn handler) {
            return new State(null, thread, null, null, new DreamProcArguments(null));
        }
    }
}
