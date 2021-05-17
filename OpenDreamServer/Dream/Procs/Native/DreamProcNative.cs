﻿using System;
using System.Collections.Generic;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs.Native {
    class SleepProc : Proc
    {
        public static SleepProc Instance = new();

        class State : ProcState
        {
            // TODO: Using DateTime here is probably terrible.
            public DateTime WakeTime { get; }
            private bool _beganSleep;

            public override Proc Proc => SleepProc.Instance;

            public State(ExecutionContext context, DateTime wakeTime)
                : base(context)
            {
                WakeTime = wakeTime;
            }

            public override ProcStatus Resume()
            {
                if (!_beganSleep) {
                    _beganSleep = true;
                    // TODO: Move context to sleeper list
                    return ProcStatus.Deferred;
                }

                return ProcStatus.Returned;
            }
        }

        private SleepProc()
            // TODO: pass argument list?
            : base("sleep", null, null, null)
        {}

        public override ProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            float delay = arguments.GetArgument(0, "Delay").GetValueAsNumber();
            int delayMilliseconds = (int)(delay * 100);

            var wakeTime = DateTime.Now;
            wakeTime.AddMilliseconds(delayMilliseconds);

            return new State(context, wakeTime);
        }
    }

    static class DreamProcNative {
        public static void SetupNativeProcs() {
            DreamObjectDefinition root = Program.DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Root);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_abs);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_animate);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_arccos);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_arcsin);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_arctan);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_ascii2text);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_ckey);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_clamp);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_cmptext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_copytext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_cos);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_CRASH);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_fcopy);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_fcopy_rsc);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_fdel);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_fexists);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_file);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_file2text);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_findtext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_findtextEx);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_findlasttext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_flick);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_flist);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_html_decode);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_html_encode);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_image);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isarea);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isfile);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_islist);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isloc);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_ismob);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isnull);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isnum);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_ispath);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_istext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_isturf);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_json_decode);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_json_encode);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_length);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_list2params);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_log);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_lowertext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_max);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_md5);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_min);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_num2text);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_oview);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_oviewers);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_params2list);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_pick);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_prob);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_rand);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_replacetext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_replacetextEx);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_rgb);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_roll);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_round);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sin);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sleep);
            root.SetProcDefinition("sleep", SleepProc.Instance);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sorttext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sorttextEx);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sound);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_splittext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_sqrt);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_stat);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_statpanel);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_tan);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_text);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_text2ascii);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_text2file);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_text2num);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_text2path);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_time2text);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_typesof);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_uppertext);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_url_encode);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_view);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_viewers);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_walk);
            root.SetTrivialNativeProc(DreamProcNativeRoot.NativeProc_walk_to);

            DreamObjectDefinition list = Program.DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.List);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Add);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Copy);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Cut);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Find);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Insert);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Remove);
            list.SetTrivialNativeProc(DreamProcNativeList.NativeProc_Swap);

            DreamObjectDefinition regex = Program.DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Regex);
            regex.SetTrivialNativeProc(DreamProcNativeRegex.NativeProc_Find);
            regex.SetTrivialNativeProc(DreamProcNativeRegex.NativeProc_Replace);
        }
    }
}
