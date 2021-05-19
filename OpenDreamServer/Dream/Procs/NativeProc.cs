using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs {
    class NativeProc : DreamProc {
        public delegate DreamValue HandlerFn(DreamObject src, DreamObject usr, DreamProcArguments arguments);

        public class State : ProcState
        {
            public DreamObject Src;
            public DreamObject Usr;
            public DreamProcArguments Arguments;
            
            private NativeProc _proc;
            public override DreamProc Proc => _proc;

            public State(NativeProc proc, DreamThread context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
                : base(context)
            {
                _proc = proc;
                Src = src;
                Usr = usr;
                Arguments = arguments;
            }

            protected override ProcStatus InternalResume()
            {
                Result = _proc.Handler.Invoke(Src, Usr, Arguments);
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

        public NativeProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler)
            : base(name, superProc, argumentNames, argumentTypes)
        {
            _defaultArgumentValues = defaultArgumentValues;
            Handler = handler;
        }

        public override State CreateState(DreamThread context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            if (_defaultArgumentValues != null) {
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
            }

            return new State(this, context, src, usr, arguments);
        }

        public static ProcState CreateAnonymousState(DreamThread context, HandlerFn handler) {
            return new State(null, context, null, null, new DreamProcArguments(null));
        }
    }
}