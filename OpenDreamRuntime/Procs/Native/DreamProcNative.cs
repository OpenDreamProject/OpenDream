using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNative {
        public static void SetupNativeProcs(DreamObjectTree objectTree) {
            IDreamManager dreamManager = IoCManager.Resolve<IDreamManager>();

            DreamProcNativeRoot.DreamManager = dreamManager;

            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_abs);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_alert);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_animate);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arccos);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arcsin);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arctan);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ascii2text);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckey);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckeyEx);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_clamp);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cmptext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_copytext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cos);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_CRASH);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy_rsc);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fdel);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fexists);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file2text);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtextEx);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttextEx);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flick);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flist);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_hascall);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_decode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_encode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_icon_states);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_image);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isarea);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isfile);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isicon);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_islist);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isloc);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismob);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismovable);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnull);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnum);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ispath);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_istext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isturf);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_decode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_encode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_length);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_length_char);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_list2params);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_log);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_lowertext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_max);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_md5);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_min);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_nonspantext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_num2text);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oview);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oviewers);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_params2list);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_prob);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand_seed);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ref);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_regex);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetextEx);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rgb);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rgb2num);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_roll);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_round);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_shutdown);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sin);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sleep);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttextEx);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sound);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_splittext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sqrt);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_stat);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_statpanel);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_tan);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2ascii);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2file);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2num);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2path);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_time2text);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_typesof);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_uppertext);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_decode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_encode);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_view);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_viewers);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk_to);
            dreamManager.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winset);

            DreamObjectDefinition list = objectTree.GetObjectDefinition(DreamPath.List);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Add);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Copy);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Cut);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Find);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Insert);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Remove);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Swap);

            DreamObjectDefinition regex = objectTree.GetObjectDefinition(DreamPath.Regex);
            regex.SetNativeProc(DreamProcNativeRegex.NativeProc_Find);
            regex.SetNativeProc(DreamProcNativeRegex.NativeProc_Replace);

            DreamObjectDefinition icon = objectTree.GetObjectDefinition(DreamPath.Icon);
            icon.SetNativeProc(DreamProcNativeIcon.NativeProc_Width);
            icon.SetNativeProc(DreamProcNativeIcon.NativeProc_Height);

            //DreamObjectDefinition savefile = objectTree.GetObjectDefinitionFromPath(DreamPath.Savefile);
            //savefile.SetNativeProc(DreamProcNativeSavefile.NativeProc_Flush);

            DreamObjectDefinition world = objectTree.GetObjectDefinition(DreamPath.World);
            world.SetNativeProc(DreamProcNativeWorld.NativeProc_Export);
        }
    }
}
