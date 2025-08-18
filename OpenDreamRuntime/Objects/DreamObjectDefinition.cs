using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;

namespace OpenDreamRuntime.Objects;

public sealed class DreamObjectDefinition {
    // IoC dependencies & entity systems for DreamObjects to use
    // TODO: Wow, remove this
    public readonly DreamManager DreamManager;
    public readonly DreamObjectTree ObjectTree;
    public readonly AtomManager AtomManager;
    public readonly IDreamMapManager DreamMapManager;
    public readonly IMapManager MapManager;
    public readonly DreamResourceManager DreamResourceManager;
    public readonly WalkManager WalkManager;
    public readonly IEntityManager EntityManager;
    public readonly IPlayerManager PlayerManager;
    public readonly ISerializationManager SerializationManager;
    public readonly ServerAppearanceSystem? AppearanceSystem;
    public readonly TransformSystem? TransformSystem;
    public readonly PvsOverrideSystem? PvsOverrideSystem;
    public readonly MetaDataSystem? MetaDataSystem;
    public readonly ServerVerbSystem? VerbSystem;

    public readonly TreeEntry TreeEntry;
    public string Type => TreeEntry.Path;
    public DreamObjectDefinition? Parent => TreeEntry.ParentEntry?.ObjectDefinition;
    public int? InitializationProc;
    public bool NoConstructors {
        get {
            if (_noConstructors is not { } res)
                _noConstructors = CheckNoConstructors();

            return _noConstructors.Value;
        }
    }

    private bool? _noConstructors = null;
    public readonly Dictionary<string, int> Procs = new();
    public readonly Dictionary<string, int> OverridingProcs = new();
    public Dictionary<string, int>? Verbs;

    // Maps variables from their name to their initial value.
    public readonly Dictionary<string, DreamValue> Variables = new();
    // Maps /static variables from name to their index in the global variable table.
    public readonly Dictionary<string, int> GlobalVariables = new();
    // Contains hashes of variables that are tagged /const.
    public HashSet<string>? ConstVariables = null;
    // Contains hashes of variables that are tagged /tmp.
    public HashSet<string>? TmpVariables = null;

    public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
        DreamManager = copyFrom.DreamManager;
        ObjectTree = copyFrom.ObjectTree;
        AtomManager = copyFrom.AtomManager;
        DreamMapManager = copyFrom.DreamMapManager;
        MapManager = copyFrom.MapManager;
        DreamResourceManager = copyFrom.DreamResourceManager;
        WalkManager = copyFrom.WalkManager;
        EntityManager = copyFrom.EntityManager;
        PlayerManager = copyFrom.PlayerManager;
        SerializationManager = copyFrom.SerializationManager;
        AppearanceSystem = copyFrom.AppearanceSystem;
        TransformSystem = copyFrom.TransformSystem;
        PvsOverrideSystem = copyFrom.PvsOverrideSystem;
        MetaDataSystem = copyFrom.MetaDataSystem;
        VerbSystem = copyFrom.VerbSystem;

        TreeEntry = copyFrom.TreeEntry;
        InitializationProc = copyFrom.InitializationProc;

        Variables = new Dictionary<string, DreamValue>(copyFrom.Variables);
        GlobalVariables = new Dictionary<string, int>(copyFrom.GlobalVariables);
        ConstVariables = copyFrom.ConstVariables is not null ? new HashSet<string>(copyFrom.ConstVariables) : null;
        TmpVariables = copyFrom.TmpVariables is not null ? new HashSet<string>(copyFrom.TmpVariables) : null;
        Procs = new Dictionary<string, int>(copyFrom.Procs);
        OverridingProcs = new Dictionary<string, int>(copyFrom.OverridingProcs);
        if (copyFrom.Verbs != null)
            Verbs = new Dictionary<string, int>(copyFrom.Verbs);
    }

    public DreamObjectDefinition(DreamManager dreamManager, DreamObjectTree objectTree, AtomManager atomManager, IDreamMapManager dreamMapManager, IMapManager mapManager, DreamResourceManager dreamResourceManager, WalkManager walkManager, IEntityManager entityManager, IPlayerManager playerManager, ISerializationManager serializationManager, ServerAppearanceSystem? appearanceSystem, TransformSystem? transformSystem, PvsOverrideSystem? pvsOverrideSystem, MetaDataSystem? metaDataSystem, ServerVerbSystem? verbSystem, TreeEntry? treeEntry) {
        DreamManager = dreamManager;
        ObjectTree = objectTree;
        AtomManager = atomManager;
        DreamMapManager = dreamMapManager;
        MapManager = mapManager;
        DreamResourceManager = dreamResourceManager;
        WalkManager = walkManager;
        EntityManager = entityManager;
        PlayerManager = playerManager;
        SerializationManager = serializationManager;
        AppearanceSystem = appearanceSystem;
        TransformSystem = transformSystem;
        PvsOverrideSystem = pvsOverrideSystem;
        MetaDataSystem = metaDataSystem;
        VerbSystem = verbSystem;

        TreeEntry = treeEntry;

        if (Parent != null) {
            InitializationProc = Parent.InitializationProc;
            Variables = new Dictionary<string, DreamValue>(Parent.Variables);
            if (Parent.Verbs != null)
                Verbs = new Dictionary<string, int>(Parent.Verbs);
            if (Parent != ObjectTree.Root.ObjectDefinition) // Don't include root-level globals
                GlobalVariables = new Dictionary<string, int>(Parent.GlobalVariables);
            if (Parent.ConstVariables != null)
                ConstVariables = new HashSet<string>(Parent.ConstVariables);
            if (Parent.TmpVariables != null)
                TmpVariables = new HashSet<string>(Parent.TmpVariables);
        }
    }

    private bool CheckNoConstructors() {
        var noInit = InitializationProc is null ||
                     ObjectTree.Procs[InitializationProc.Value] is DMProc {IsNullProc: true};
        var noNew = !TryGetProc("New", out var proc) || proc is DMProc {IsNullProc: true};
        if (noInit && noNew) {
            return true;
        }

        return false;
    }

    public void SetVariableDefinition(string variableName, DreamValue value) {
        Variables[variableName] = value;
    }

    public void SetProcDefinition(string procName, int procId, bool replace = false) {
        if (HasProc(procName) && !replace) {
            var proc = ObjectTree.Procs[procId];
            proc.SuperProc = GetProc(procName);
            OverridingProcs[procName] = procId;
        } else {
            Procs[procName] = procId;
        }
    }

    public DreamProc GetProc(string procName) {
        if (TryGetProc(procName, out DreamProc? proc)) {
            return proc;
        } else {
            throw new Exception("Object type '" + Type + "' does not have a proc named '" + procName + "'");
        }
    }

    public bool TryGetProc(string procName, [NotNullWhen(true)] out DreamProc? proc) {
        if (OverridingProcs.TryGetValue(procName, out var procId)) {
            proc = ObjectTree.Procs[procId];
            return true;
        } else if (Procs.TryGetValue(procName, out procId)) {
            proc = ObjectTree.Procs[procId];
            return true;
        } else if (Parent != null) {
            return Parent.TryGetProc(procName, out proc);
        } else {
            proc = null;
            return false;
        }
    }

    public bool HasProc(string procName) {
        if (Procs.ContainsKey(procName)) {
            return true;
        } else if (Parent != null) {
            return Parent.HasProc(procName);
        } else {
            return false;
        }
    }

    public bool HasVariable(string variableName) {
        return Variables.ContainsKey(variableName);
    }

    public bool TryGetVariable(string varName, out DreamValue value) {
        if (Variables.TryGetValue(varName, out value)) {
            return true;
        } else if (Parent != null) {
            return Parent.TryGetVariable(varName, out value);
        } else {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSubtypeOf(TreeEntry ancestor) {
        return TreeEntry.IsSubtypeOf(ancestor);
    }
}
