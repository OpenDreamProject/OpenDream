using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        public static Dictionary<DreamObject, UInt16> AtomIDs = new();
        public static Dictionary<UInt16, DreamObject> AtomIDToAtom = new();
        public static ConcurrentDictionary<DreamObject, int> AtomToAppearanceID = new();

        private static UInt16 _atomIDCounter = 0;
        private static Dictionary<DreamList, DreamObject> _overlaysListToAtom = new();
        private static Dictionary<DreamObject, Dictionary<DreamValue, (UInt16, ServerIconAppearance)>> _atomOverlays = new();
        private static object _atomListsLock = new object();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            lock (_atomListsLock) {
                UInt16 atomID = _atomIDCounter++;

                AtomIDs.Add(dreamObject, atomID);
                AtomIDToAtom.Add(atomID, dreamObject);
            }

            Program.DreamStateManager.AddAtomCreation(dreamObject);

            ATOMBase atomBase = ATOMBase.AtomBases[Program.AtomBaseIDs[dreamObject.ObjectDefinition]];
            ServerIconAppearance atomAppearance = ServerIconAppearance.GetAppearance(atomBase.IconAppearanceID);
            UpdateAppearance(dreamObject, atomAppearance);

            DreamObject locArgument = FindLocArgument(creationArguments);
            if (locArgument != null) {
                dreamObject.SetVariable("loc", new DreamValue(locArgument)); //loc is set before /New() is ever called
            } else if (creationArguments.ArgumentCount == 0) {
                creationArguments.OrderedArguments.Add(new DreamValue(locArgument)); //First argument is loc, which is null
            }

            DreamObject worldContents = Program.WorldInstance.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);
            worldContents.CallProc("Add", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            if (Program.WorldInstance.GetVariable("contents").TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject worldContents)) {
                worldContents.CallProc("Remove", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));
            }

            Program.DreamStateManager.AddAtomDeletion(dreamObject);

            lock (_atomListsLock) {
                AtomIDToAtom.Remove(AtomIDs[dreamObject]);
                AtomIDs.Remove(dreamObject);
                AtomToAppearanceID.Remove(dreamObject, out _);
                _overlaysListToAtom.Remove(DreamMetaObjectList.DreamLists[dreamObject.GetVariable("overlays").GetValueAsDreamObjectOfType(DreamPath.List)]);
                _atomOverlays.Remove(dreamObject);
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "icon") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.Icon = variableValue.GetValueAsDreamResource().ResourcePath;
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "icon_state") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.IconState = variableValue.GetValueAsString();
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "pixel_x") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.PixelX = variableValue.GetValueAsInteger();
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "pixel_y") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.PixelY = variableValue.GetValueAsInteger();
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "color") {
                string color;
                if (variableValue.Type == DreamValue.DreamValueType.String) {
                    color = variableValue.GetValueAsString();
                } else {
                    color = "white";
                }

                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.SetColor(color);
            } else if (variableName == "dir") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.Direction = (AtomDirection)variableValue.GetValueAsInteger();
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "overlays") {
                if (oldVariableValue.Value != null && oldVariableValue.TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject oldListObject)) {
                    DreamList oldList = DreamMetaObjectList.DreamLists[oldListObject];

                    oldList.Cut();
                    oldList.ValueAssigned -= OverlayValueAssigned;
                    oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                    _overlaysListToAtom.Remove(oldList);
                }

                DreamObject overlayListObject;
                if (!variableValue.TryGetValueAsDreamObjectOfType(DreamPath.List, out overlayListObject)) {
                    overlayListObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
                }

                DreamList overlayList = DreamMetaObjectList.DreamLists[overlayListObject];
                overlayList.ValueAssigned += OverlayValueAssigned;
                overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                _overlaysListToAtom[overlayList] = dreamObject;
            }
        }

        protected static DreamObject FindLocArgument(DreamProcArguments arguments) {
            if (arguments.ArgumentCount >= 1) {
                DreamValue loc = arguments.GetArgument(0, "loc");

                if (loc.Value != null && loc.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject locValue)) {
                    return locValue;
                }
            }

            return null;
        }

        protected static ServerIconAppearance GetAppearance(DreamObject atom) {
            return ServerIconAppearance.GetAppearance(AtomToAppearanceID[atom]);
        }

        protected static void UpdateAppearance(DreamObject atom, ServerIconAppearance newAppearance) {
            int appearanceID = newAppearance.GetID();

            if (!AtomToAppearanceID.ContainsKey(atom) || AtomToAppearanceID[atom] != appearanceID) {
                AtomToAppearanceID[atom] = appearanceID;

                Program.DreamStateManager.AddAtomIconAppearanceDelta(atom, newAppearance);
            }
        }

        private Dictionary<DreamValue, (UInt16, ServerIconAppearance)> GetAtomOverlays(DreamObject atom) {
            Dictionary<DreamValue, (UInt16, ServerIconAppearance)> overlays;
            if (!_atomOverlays.TryGetValue(atom, out overlays)) {
                overlays = new Dictionary<DreamValue, (UInt16, ServerIconAppearance)>();
                _atomOverlays[atom] = overlays;
            }

            return overlays;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue overlayKey, DreamValue overlayValue) {
            if (overlayValue.Value == null) return;

            DreamObject atom = _overlaysListToAtom[overlayList];
            Dictionary<DreamValue, (UInt16, ServerIconAppearance)> overlays = GetAtomOverlays(atom);
            (UInt16, ServerIconAppearance) overlay = overlays.GetValueOrDefault(overlayValue);

            if (overlay.Item1 == default) overlay.Item1 = (UInt16)overlays.Count;
            if (overlay.Item2 == null) overlay.Item2 = new ServerIconAppearance();
            if (overlayValue.IsType(DreamValue.DreamValueType.String)) {
                DreamValue icon = atom.GetVariable("icon");
                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    overlay.Item2.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else {
                    overlay.Item2.Icon = icon.GetValueAsString();
                }

                overlay.Item2.IconState = overlayValue.GetValueAsString();
            } else if (overlayValue.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    overlay.Item2.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else {
                    overlay.Item2.Icon = icon.GetValueAsString();
                }

                overlay.Item2.IconState = mutableAppearance.GetVariable("icon_state").GetValueAsString();
                overlay.Item2.SetColor(mutableAppearance.GetVariable("color").GetValueAsString());
                overlay.Item2.Layer = (float)mutableAppearance.GetVariable("layer").GetValueAsNumber();
                overlay.Item2.PixelX = mutableAppearance.GetVariable("pixel_x").GetValueAsInteger();
                overlay.Item2.PixelY = mutableAppearance.GetVariable("pixel_y").GetValueAsInteger();
            } else if (overlayValue.TryGetValueAsDreamObjectOfType(DreamPath.Image, out DreamObject image)) {
                DreamValue icon = image.GetVariable("icon");
                DreamValue iconState = image.GetVariable("icon_state");

                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    overlay.Item2.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else {
                    overlay.Item2.Icon = icon.GetValueAsString();
                }

                if (iconState.IsType(DreamValue.DreamValueType.String)) overlay.Item2.IconState = iconState.GetValueAsString();
                overlay.Item2.SetColor(image.GetVariable("color").GetValueAsString());
                overlay.Item2.Direction = (AtomDirection)image.GetVariable("dir").GetValueAsInteger();
                overlay.Item2.Layer = (float)image.GetVariable("layer").GetValueAsNumber();
                overlay.Item2.PixelX = image.GetVariable("pixel_x").GetValueAsInteger();
                overlay.Item2.PixelY = image.GetVariable("pixel_y").GetValueAsInteger();
            } else {
                return;
            }

            overlays[overlayValue] = overlay;
            Program.DreamStateManager.AddAtomOverlay(atom, overlay.Item1, overlay.Item2);
        }
        
        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue overlayKey, DreamValue overlayValue) {
            if (overlayValue.Value == null) return;

            DreamObject atom = _overlaysListToAtom[overlayList];
            Dictionary<DreamValue, (UInt16, ServerIconAppearance)> overlays = GetAtomOverlays(atom);

            if (overlays.ContainsKey(overlayValue)) {
                (UInt16, ServerIconAppearance) overlay = overlays[overlayValue];

                Program.DreamStateManager.RemoveAtomOverlay(atom, overlay.Item1);
            }
        }
    }
}
