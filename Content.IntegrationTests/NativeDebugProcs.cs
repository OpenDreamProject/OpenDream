using System;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using Robust.Shared.GameObjects;
#nullable enable
internal sealed class NativeDebugProcs {
    private static ServerVerbSystem VerbSystem = null!;

    internal static void SetupDependencies(IEntitySystemManager systemManager) {
        VerbSystem = systemManager.GetEntitySystem<ServerVerbSystem>();
    }

    internal static void SetupDebugNatives(DreamObjectTree objectTree) {
        TreeEntry debugType = objectTree.GetTreeEntry("/debug_hooks");
        DreamProcNative.BindNativeProc(objectTree, debugType, NativeProc_CanExecuteVerb);
        DreamProcNative.BindNativeProc(objectTree, debugType, NativeProc_GetExecutableVerbs);
    }

    [DreamProc("GetExecutableVerbs")]
    [DreamProcParameter("client", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("target", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("possible_verbs", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    private static DreamValue NativeProc_GetExecutableVerbs(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObjectClient? client = (DreamObjectClient?)bundle.GetArgument(0, "client").MustGetValueAsDreamObject();
        if(client is null) {
            throw new ArgumentException("client can't be null");
        }

        DreamObject? target = bundle.GetArgument(1, "target").MustGetValueAsDreamObject();
        if(target is null) {
            throw new ArgumentException("target can't be null");
        }

        DreamConnection connection = client.Connection;

        DreamList? possibleVerbs = bundle.GetArgument(2, "possible_verbs").MustGetValueAsDreamList();
        DreamList resultingVerbs = bundle.ObjectTree.CreateList();
        foreach(DreamValue key in possibleVerbs.Enumerate()) {
           DreamProc verb = key.MustGetValueAsProc();
           if(VerbSystem.CanExecute(connection, target, verb)) {
                resultingVerbs.Add(new(verb));
            }
        }

        return new(resultingVerbs);
    }

    [DreamProc("CanExecuteVerb")]
    [DreamProcParameter("client", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("target", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("verb", Type = DreamValue.DreamValueTypeFlag.DreamProc)]
    private static DreamValue NativeProc_CanExecuteVerb(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObjectClient? client = (DreamObjectClient?)bundle.GetArgument(0, "client").MustGetValueAsDreamObject();
        if(client is null) {
            throw new ArgumentException("client can't be null");
        }

        DreamObject? target = bundle.GetArgument(1, "target").MustGetValueAsDreamObject();
        if(target is null) {
            throw new ArgumentException("target can't be null");
        }

        DreamConnection connection = client.Connection;
        DreamProc proc = bundle.GetArgument(2, "verb").MustGetValueAsProc();

        return VerbSystem.CanExecute(connection, target, proc) ? DreamValue.True : DreamValue.False;
    }
}
