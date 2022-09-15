using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNative {
        public static void SetupNativeProcs(DreamObjectTree objectTree) {
            DreamProcNativeRoot.DreamManager = IoCManager.Resolve<IDreamManager>();

            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_abs);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_alert);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_animate);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arccos);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arcsin);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_arctan);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ascii2text);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckey);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckeyEx);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_clamp);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cmptext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_copytext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cos);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_CRASH);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy_rsc);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fdel);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fexists);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file2text);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_filter);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtext_char);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtextEx);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtextEx_char);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttext_char);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttextEx);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttextEx_char);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flick);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flist);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_hascall);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_decode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_encode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_icon_states);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_image);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isarea);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isfile);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isicon);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_islist);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isloc);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismob);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismovable);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnull);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnum);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ispath);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_istext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isturf);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_decode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_encode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_length);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_length_char);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_list2params);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_log);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_lowertext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_max);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_md5);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_min);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_nonspantext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_num2text);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oview);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oviewers);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_params2list);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand_seed);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ref);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_regex);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetextEx);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rgb);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rgb2num);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_roll);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_round);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sha1);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_shutdown);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sin);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sleep);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttextEx);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sound);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_splittext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sqrt);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_stat);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_statpanel);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_tan);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2ascii);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2file);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2num);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2path);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_time2text);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_typesof);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_uppertext);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_decode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_encode);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_view);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_viewers);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk_to);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winexists);
            objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winset);

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
