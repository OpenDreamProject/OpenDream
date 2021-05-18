using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamServer.Dream.Objects;

namespace OpenDreamServer.Dream.Procs.Native {
    static class SleepQueue {
        struct Item {
            public DateTime _wakeTime;
            public ExecutionContext _context;
        }

        private static List<Item> _sleepers = new();

        public static void Push(DateTime wakeTime, ExecutionContext context) {
            _sleepers.Add(new Item {
                _wakeTime = wakeTime,
                _context = context,
            });
        }

        public static void Process() {
            var now = DateTime.Now;

            // This all obviously sucks but it's the simplest way to handle mutations to _sleepers that can happen during a context's execution
            
            var toProcess = _sleepers;
            _sleepers = new();

            foreach (var item in toProcess) {
                if (item._wakeTime <= now) {
                    item._context.Resume();
                } else {
                    _sleepers.Add(item);
                }
            }
        }
    }

    class SleepProc : DreamProc
    {
        public static SleepProc Instance = new();

        class State : ProcState
        {
            // TODO: Using DateTime here is probably terrible.
            // It should instead figure out the number of ticks to sleep for using tick_lag
            // int ticksToSleep = (int)Math.Ceiling(delayMilliseconds / (Program.WorldInstance.GetVariable("tick_lag").GetValueAsNumber() * 100));
            public DateTime WakeTime { get; }
            private bool _beganSleep;

            public State(ExecutionContext context, DateTime wakeTime)
                : base(context)
            {
                WakeTime = wakeTime;
            }

            public override DreamProc Proc => SleepProc.Instance;

            protected override ProcStatus InternalResume()
            {
                if (!_beganSleep) {
                    _beganSleep = true;
                    SleepQueue.Push(WakeTime, Context);
                    return ProcStatus.Deferred;
                }

                return ProcStatus.Returned;
            }

            public override void AppendStackFrame(StringBuilder builder)
            {
                builder.Append("sleep(...)");
            }
        }

        private SleepProc()
            : base("sleep", null, null, null)
        {}

        public override ProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            float delay = arguments.GetArgument(0, "Delay").GetValueAsNumber();
            int delayMilliseconds = (int)(delay * 100);

            return new State(context, DateTime.Now.AddMilliseconds(delayMilliseconds));
        }
    }
}