using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNative {
    public static void SetupNativeProcs(DreamObjectTree objectTree) {
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_alert);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_animate);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ascii2text);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_block);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ceil);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckey);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ckeyEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_clamp);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cmptext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_cmptextEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_copytext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_copytext_char);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_CRASH);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fcopy_rsc);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fdel);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fexists);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_file2text);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_filter);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findtextEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_findlasttextEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flick);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_flist);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_floor);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_fract);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ftime);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_generator);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_get_step_to);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_get_steps_to);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_hascall);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_hearers);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_decode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_html_encode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_icon_states);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_image);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isarea);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isfile);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isicon);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isinf);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_islist);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isloc);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismob);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isobj);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ismovable);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnan);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnull);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isnum);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ispath);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_istext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_isturf);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_decode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_json_encode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_length_char);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_list2params);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_lowertext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_matrix);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_max);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_md5);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_min);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_nonspantext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_num2text);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ohearers);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_orange);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oview);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_oviewers);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_params2list);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rand_seed);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_range);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_ref);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_regex);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_replacetextEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_rgb2num);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_roll);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_round);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sha1);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_shutdown);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sleep);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sorttextEx);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_sound);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_spantext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_spantext_char);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_splicetext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_splicetext_char);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_splittext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_stat);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_statpanel);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2ascii);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2ascii_char);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2file);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2num);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_text2path);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_time2text);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_trimtext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_trunc);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_turn);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_typesof);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_uppertext);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_decode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_url_encode);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_view);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_viewers);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk_rand);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_walk_towards);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winclone);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winexists);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winget);
        objectTree.SetGlobalNativeProc(DreamProcNativeRoot.NativeProc_winset);

        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Add);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Copy);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Cut);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Find);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Insert);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Join);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Remove);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_RemoveAll);
        objectTree.SetNativeProc(objectTree.List, DreamProcNativeList.NativeProc_Swap);

        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Add);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Invert);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Multiply);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Scale);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Subtract);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Translate);
        objectTree.SetNativeProc(objectTree.Matrix, DreamProcNativeMatrix.NativeProc_Turn);

        objectTree.SetNativeProc(objectTree.Regex, DreamProcNativeRegex.NativeProc_Find);
        objectTree.SetNativeProc(objectTree.Regex, DreamProcNativeRegex.NativeProc_Replace);

        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Width);
        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Height);
        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Insert);
        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Blend);
        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Scale);
        objectTree.SetNativeProc(objectTree.Icon, DreamProcNativeIcon.NativeProc_Turn);

        objectTree.SetNativeProc(objectTree.Savefile, DreamProcNativeSavefile.NativeProc_ExportText);
        objectTree.SetNativeProc(objectTree.Savefile, DreamProcNativeSavefile.NativeProc_Flush);

        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_Export);
        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_GetConfig);
        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_Profile);
        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_SetConfig);
        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_ODHotReloadInterface);
        objectTree.SetNativeProc(objectTree.World, DreamProcNativeWorld.NativeProc_ODHotReloadResource);

        objectTree.SetNativeProc(objectTree.Database, DreamProcNativeDatabase.NativeProc_Close);
        objectTree.SetNativeProc(objectTree.Database, DreamProcNativeDatabase.NativeProc_Error);
        objectTree.SetNativeProc(objectTree.Database, DreamProcNativeDatabase.NativeProc_ErrorMsg);
        objectTree.SetNativeProc(objectTree.Database, DreamProcNativeDatabase.NativeProc_Open);

        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Add);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Clear);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Close);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Columns);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Error);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_ErrorMsg);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_Execute);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_GetColumn);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_GetRowData);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_NextRow);
        objectTree.SetNativeProc(objectTree.DatabaseQuery, DreamProcNativeDatabaseQuery.NativeProc_RowsAffected);

        SetOverridableNativeProc(objectTree, objectTree.World, DreamProcNativeWorld.NativeProc_Error);
        SetOverridableNativeProc(objectTree, objectTree.World, DreamProcNativeWorld.NativeProc_Reboot);
    }

    /// <summary>
    /// Sets a native proc that can be overriden by DM code
    /// </summary>
    private static void SetOverridableNativeProc(DreamObjectTree objectTree, TreeEntry type, NativeProc.HandlerFn func) {
        var nativeProc = objectTree.CreateNativeProc(type, func);

        var proc = objectTree.World.ObjectDefinition.GetProc(nativeProc.Name);
        if (proc.SuperProc == null) { // This proc was never overriden so just replace it
            type.ObjectDefinition.SetProcDefinition(proc.Name, nativeProc.Id, replace: true);
            return;
        }

        // Find the first override of the proc, we're replacing that one's super
        while (proc.SuperProc?.SuperProc != null)
            proc = proc.SuperProc;

        proc.SuperProc = nativeProc;
    }
}
