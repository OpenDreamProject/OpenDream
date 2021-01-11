using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
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
        private static object _atomListsLock = new object();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            UInt16 atomID = _atomIDCounter++;
            lock (_atomListsLock) {
                AtomIDs.Add(dreamObject, atomID);
                AtomIDToAtom.Add(atomID, dreamObject);
            }

            ServerIconAppearance atomAppearance = BuildAtomAppearance(dreamObject);
            Program.DreamStateManager.AddAtomCreation(dreamObject, atomAppearance);
            UpdateAppearance(dreamObject, atomAppearance);

            DreamObject locArgument = FindLocArgument(creationArguments);
            if (locArgument != null) {
                dreamObject.SetVariable("loc", new DreamValue(locArgument)); //loc is set before /New() is ever called
            } else if (creationArguments.ArgumentCount == 0) {
                creationArguments.OrderedArguments.Add(new DreamValue(locArgument)); //First argument is loc, which is null
            }

            DreamMetaObjectWorld.ContentsList.AddValue(new DreamValue(dreamObject));
            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            DreamMetaObjectWorld.ContentsList.RemoveValue(new DreamValue(dreamObject));
            Program.DreamStateManager.AddAtomDeletion(dreamObject);

            lock (_atomListsLock) {
                AtomIDToAtom.Remove(AtomIDs[dreamObject]);
                AtomIDs.Remove(dreamObject);
                AtomToAppearanceID.Remove(dreamObject, out _);
                _overlaysListToAtom.Remove(DreamMetaObjectList.DreamLists[dreamObject.GetVariable("overlays").GetValueAsDreamObjectOfType(DreamPath.List)]);
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
            } else if (variableName == "layer") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.Layer = variableValue.GetValueAsNumber();
                UpdateAppearance(dreamObject, newAppearance);
            } else if (variableName == "invisibility") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(GetAppearance(dreamObject));

                newAppearance.Invisibility = variableValue.GetValueAsInteger();
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

                if (loc.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject locValue)) {
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

        private ServerIconAppearance BuildAtomAppearance(DreamObject atom) {
            ServerIconAppearance appearance = new ServerIconAppearance();

            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource resource)) {
                appearance.Icon = resource.ResourcePath;
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string iconState)) {
                appearance.IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string color)) {
                appearance.SetColor(color);
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                appearance.Direction = (AtomDirection)dir;
            }
            
            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                appearance.Invisibility = invisibility;
            }
            
            if (atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX)) {
                appearance.PixelX = pixelX;
            }
            
            if (atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY)) {
                appearance.PixelY = pixelY;
            }

            appearance.Layer = atom.GetVariable("layer").GetValueAsNumber();

            return appearance;
        }

        private ServerIconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            ServerIconAppearance appearance = new ServerIconAppearance();

            if (value.IsType(DreamValue.DreamValueType.String)) {
                appearance.Icon = GetAppearance(atom).Icon;
                appearance.IconState = value.GetValueAsString();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    appearance.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else {
                    appearance.Icon = icon.GetValueAsString();
                }

                appearance.IconState = mutableAppearance.GetVariable("icon_state").GetValueAsString();
                appearance.SetColor(mutableAppearance.GetVariable("color").GetValueAsString());
                appearance.Layer = mutableAppearance.GetVariable("layer").GetValueAsNumber();
                appearance.PixelX = mutableAppearance.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelY = mutableAppearance.GetVariable("pixel_y").GetValueAsInteger();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Image, out DreamObject image)) {
                DreamValue icon = image.GetVariable("icon");
                DreamValue iconState = image.GetVariable("icon_state");

                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    appearance.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else {
                    appearance.Icon = icon.GetValueAsString();
                }

                if (iconState.IsType(DreamValue.DreamValueType.String)) appearance.IconState = iconState.GetValueAsString();
                appearance.SetColor(image.GetVariable("color").GetValueAsString());
                appearance.Direction = (AtomDirection)image.GetVariable("dir").GetValueAsInteger();
                appearance.Layer = image.GetVariable("layer").GetValueAsNumber();
                appearance.PixelX = image.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelY = image.GetVariable("pixel_y").GetValueAsInteger();
            } else {
                throw new Exception("Invalid overlay (" + value + ")");
            }

            return appearance;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue overlayKey, DreamValue overlayValue) {
            if (overlayValue.Value == null) return;

            DreamObject atom = _overlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = new ServerIconAppearance(GetAppearance(atom));
            ServerIconAppearance overlayAppearance = CreateOverlayAppearance(atom, overlayValue);

            atomAppearance.Overlays.Add(overlayAppearance.GetID());
            UpdateAppearance(atom, atomAppearance);
        }
        
        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue overlayKey, DreamValue overlayValue) {
            if (overlayValue.Value == null) return;

            DreamObject atom = _overlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = GetAppearance(atom);
            ServerIconAppearance overlayAppearance = CreateOverlayAppearance(atom, overlayValue);
            int overlayAppearanceId = overlayAppearance.GetID();

            if (atomAppearance.Overlays.Contains(overlayAppearanceId)) {
                atomAppearance = new ServerIconAppearance(atomAppearance);
                atomAppearance.Overlays.Remove(overlayAppearance.GetID());
                UpdateAppearance(atom, atomAppearance);
            }
        }
    }
}
