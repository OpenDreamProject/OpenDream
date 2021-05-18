using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs {
    class NativeProc : DreamProc {
        public delegate DreamValue NativeProcHandler(DreamObject src, DreamObject usr, DreamProcArguments arguments);

        private Dictionary<string, DreamValue> _defaultArgumentValues;
        public NativeProcHandler Handler { get; }

        public NativeProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, Dictionary<string, DreamValue> defaultArgumentValues, NativeProcHandler handler)
            : base(name, superProc, argumentNames, argumentTypes)
        {
            _defaultArgumentValues = defaultArgumentValues;
            Handler = handler;
        }

        public override NativeProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            if (_defaultArgumentValues != null) {
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
            }

            return new NativeProcState(this, context, src, usr, arguments);
        }
    }

    class NativeProcState : ProcState
    {
        public DreamObject Src;
        public DreamObject Usr;
        public DreamProcArguments Arguments;
        
        private NativeProc _proc;
        public override DreamProc Proc => _proc;

        public NativeProcState(NativeProc proc, ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
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
            builder.Append($"{Proc.Name}(...)");
        }
    }
}