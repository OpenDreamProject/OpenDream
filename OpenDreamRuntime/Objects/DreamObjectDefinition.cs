﻿using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;

namespace OpenDreamRuntime.Objects {
    public sealed class DreamObjectDefinition {
        // IoC dependencies & entity systems for DreamObjects to use
        public readonly IDreamManager DreamManager;
        public readonly IDreamObjectTree ObjectTree;
        public readonly IAtomManager AtomManager;
        public readonly IDreamMapManager DreamMapManager;
        public readonly IMapManager MapManager;
        public readonly DreamResourceManager DreamResourceManager;
        public readonly IEntityManager EntityManager;
        public readonly IPlayerManager PlayerManager;
        public readonly ISerializationManager SerializationManager;
        public readonly ServerAppearanceSystem? AppearanceSystem;
        public readonly TransformSystem? TransformSystem;

        public readonly IDreamObjectTree.TreeEntry TreeEntry;
        public DreamPath Type => TreeEntry.Path;
        public DreamObjectDefinition? Parent => TreeEntry.ParentEntry?.ObjectDefinition;
        public int? InitializationProc;
        public readonly Dictionary<string, int> Procs = new();
        public readonly Dictionary<string, int> OverridingProcs = new();
        public List<int>? Verbs;

        // Maps variables from their name to their initial value.
        public readonly Dictionary<string, DreamValue> Variables = new();
        // Maps /static variables from name to their index in the global variable table.
        public readonly Dictionary<string, int> GlobalVariables = new();

        public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
            DreamManager = copyFrom.DreamManager;
            ObjectTree = copyFrom.ObjectTree;
            AtomManager = copyFrom.AtomManager;
            DreamMapManager = copyFrom.DreamMapManager;
            MapManager = copyFrom.MapManager;
            DreamResourceManager = copyFrom.DreamResourceManager;
            EntityManager = copyFrom.EntityManager;
            PlayerManager = copyFrom.PlayerManager;
            SerializationManager = copyFrom.SerializationManager;
            AppearanceSystem = copyFrom.AppearanceSystem;
            TransformSystem = copyFrom.TransformSystem;

            TreeEntry = copyFrom.TreeEntry;
            InitializationProc = copyFrom.InitializationProc;

            Variables = new Dictionary<string, DreamValue>(copyFrom.Variables);
            GlobalVariables = new Dictionary<string, int>(copyFrom.GlobalVariables);
            Procs = new Dictionary<string, int>(copyFrom.Procs);
            OverridingProcs = new Dictionary<string, int>(copyFrom.OverridingProcs);
            if (copyFrom.Verbs != null)
                Verbs = new List<int>(copyFrom.Verbs);
        }

        public DreamObjectDefinition(IDreamManager dreamManager, IDreamObjectTree objectTree, IAtomManager atomManager, IDreamMapManager dreamMapManager, IMapManager mapManager, DreamResourceManager dreamResourceManager, IEntityManager entityManager, IPlayerManager playerManager, ISerializationManager serializationManager, ServerAppearanceSystem? appearanceSystem, TransformSystem? transformSystem, IDreamObjectTree.TreeEntry? treeEntry) {
            DreamManager = dreamManager;
            ObjectTree = objectTree;
            AtomManager = atomManager;
            DreamMapManager = dreamMapManager;
            MapManager = mapManager;
            DreamResourceManager = dreamResourceManager;
            EntityManager = entityManager;
            PlayerManager = playerManager;
            SerializationManager = serializationManager;
            AppearanceSystem = appearanceSystem;
            TransformSystem = transformSystem;

            TreeEntry = treeEntry;

            if (Parent != null) {
                InitializationProc = Parent.InitializationProc;
                Variables = new Dictionary<string, DreamValue>(Parent.Variables);
                if (Parent.Verbs != null)
                    Verbs = new List<int>(Parent.Verbs);
                if (Parent != ObjectTree.Root.ObjectDefinition) // Don't include root-level globals
                    GlobalVariables = new Dictionary<string, int>(Parent.GlobalVariables);
            }
        }

        public void SetVariableDefinition(string variableName, DreamValue value) {
            Variables[variableName] = value;
        }

        public void SetProcDefinition(string procName, int procId) {
            if (HasProc(procName)) {
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

        public bool IsSubtypeOf(IDreamObjectTree.TreeEntry ancestor) {
            // Unsigned underflow is desirable here
            return (TreeEntry.TreeIndex - ancestor.TreeIndex) <= ancestor.ChildCount;
        }
    }
}
