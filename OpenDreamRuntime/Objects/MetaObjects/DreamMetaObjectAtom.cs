using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        public DreamMetaObjectAtom(DreamRuntime runtime)
            : base(runtime)

        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            UInt32 atomID = Runtime.AtomIDCounter++;
            Runtime.AtomIDs.Add(dreamObject, atomID);
            Runtime.AtomIDToAtom.Add(atomID, dreamObject);

            ServerIconAppearance atomAppearance = BuildAtomAppearance(dreamObject);
            Runtime.StateManager.AddAtomCreation(dreamObject, atomAppearance);
            UpdateAppearance(Runtime, dreamObject, atomAppearance);

            DreamValue locArgument = creationArguments.GetArgument(0, "loc");
            if (locArgument.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out _)) {
                dreamObject.SetVariable("loc", locArgument); //loc is set before /New() is ever called
            } else if (creationArguments.ArgumentCount == 0) {
                creationArguments.OrderedArguments.Add(DreamValue.Null); //First argument is loc, which is null
            }

            Runtime.WorldContentsList.AddValue(new DreamValue(dreamObject));
            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            Runtime.WorldContentsList.RemoveValue(new DreamValue(dreamObject));
            Runtime.StateManager.AddAtomDeletion(dreamObject);

            Runtime.AtomIDToAtom.Remove(Runtime.AtomIDs[dreamObject]);
            Runtime.AtomIDs.Remove(dreamObject);
            Runtime.AtomToAppearance.Remove(dreamObject, out _);
            Runtime.OverlaysListToAtom.Remove(dreamObject.GetVariable("overlays").GetValueAsDreamList());
            Runtime.UnderlaysListToAtom.Remove(dreamObject.GetVariable("underlays").GetValueAsDreamList());

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "icon") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.Icon = variableValue.GetValueAsDreamResource().ResourcePath;
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "icon_state") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                if (variableValue.Value != null) newAppearance.IconState = variableValue.GetValueAsString();
                else newAppearance.IconState = "";

                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "pixel_x") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.PixelX = (int)variableValue.GetValueAsNumber();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "pixel_y") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.PixelY = (int)variableValue.GetValueAsNumber();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "layer") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.Layer = variableValue.GetValueAsNumber();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "invisibility") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.Invisibility = variableValue.GetValueAsInteger();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "color") {
                string color;
                if (!variableValue.TryGetValueAsString(out color)) {
                    color = "white";
                }

                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.SetColor(color);
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "dir") {
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.Direction = (AtomDirection)variableValue.GetValueAsInteger();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "transform") {
                DreamObject matrix = variableValue.GetValueAsDreamObjectOfType(DreamPath.Matrix);
                ServerIconAppearance newAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, dreamObject));

                newAppearance.Transform[0] = matrix.GetVariable("a").GetValueAsNumber();
                newAppearance.Transform[1] = matrix.GetVariable("d").GetValueAsNumber();
                newAppearance.Transform[2] = matrix.GetVariable("b").GetValueAsNumber();
                newAppearance.Transform[3] = matrix.GetVariable("e").GetValueAsNumber();
                newAppearance.Transform[4] = matrix.GetVariable("c").GetValueAsNumber();
                newAppearance.Transform[5] = matrix.GetVariable("f").GetValueAsNumber();
                UpdateAppearance(Runtime, dreamObject, newAppearance);
            } else if (variableName == "overlays") {
                if (oldVariableValue != DreamValue.Null && oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                    oldList.Cut();
                    oldList.ValueAssigned -= OverlayValueAssigned;
                    oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                    Runtime.OverlaysListToAtom.Remove(oldList);
                }

                DreamList overlayList;
                if (!variableValue.TryGetValueAsDreamList(out overlayList)) {
                    overlayList = new DreamList(Runtime);
                }

                overlayList.ValueAssigned += OverlayValueAssigned;
                overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                Runtime.OverlaysListToAtom[overlayList] = dreamObject;
            } else if (variableName == "underlays") {
                if (oldVariableValue != DreamValue.Null && oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                    oldList.Cut();
                    oldList.ValueAssigned -= UnderlayValueAssigned;
                    oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                    Runtime.UnderlaysListToAtom.Remove(oldList);
                }

                DreamList underlayList;
                if (!variableValue.TryGetValueAsDreamList(out underlayList)) {
                    underlayList = new DreamList(Runtime);
                }

                underlayList.ValueAssigned += UnderlayValueAssigned;
                underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                Runtime.UnderlaysListToAtom[underlayList] = dreamObject;
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "transform") {
                DreamObject matrix = Runtime.ObjectTree.CreateObject(DreamPath.Matrix, new DreamProcArguments(new() { variableValue })); //Clone the matrix

                return new DreamValue(matrix);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        protected static ServerIconAppearance GetAppearance(DreamRuntime runtime, DreamObject atom) {
            return runtime.AtomToAppearance[atom];
        }

        protected static void UpdateAppearance(DreamRuntime runtime, DreamObject atom, ServerIconAppearance newAppearance) {
            if (!runtime.AtomToAppearance.TryGetValue(atom, out ServerIconAppearance oldAppearance) || oldAppearance.GetID() != newAppearance.GetID()) {
                runtime.AtomToAppearance[atom] = newAppearance;

                atom.Runtime.StateManager.AddAtomIconAppearanceDelta(atom, newAppearance);
            }
        }

        private ServerIconAppearance BuildAtomAppearance(DreamObject atom) {
            ServerIconAppearance appearance = new ServerIconAppearance(Runtime);

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
            ServerIconAppearance appearance = new ServerIconAppearance(Runtime);

            if (value.IsType(DreamValue.DreamValueType.String)) {
                appearance.Icon = GetAppearance(Runtime, atom).Icon;
                appearance.IconState = value.GetValueAsString();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.IsType(DreamValue.DreamValueType.DreamResource)) {
                    appearance.Icon = icon.GetValueAsDreamResource().ResourcePath;
                } else if (icon.IsType(DreamValue.DreamValueType.String)) {
                    appearance.Icon = icon.GetValueAsString();
                } else if (icon == DreamValue.Null) {
                    appearance.Icon = GetAppearance(Runtime, atom).Icon;
                }

                DreamValue colorValue = mutableAppearance.GetVariable("color");
                if (colorValue.TryGetValueAsString(out string color)) {
                    appearance.SetColor(color);
                } else {
                    appearance.SetColor("white");
                }

                appearance.IconState = mutableAppearance.GetVariable("icon_state").GetValueAsString();
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
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject overlayAtom)) {
                appearance = BuildAtomAppearance(overlayAtom);
            } else {
                throw new Exception("Invalid overlay (" + value + ")");
            }

            return appearance;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = Runtime.OverlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, atom));
            ServerIconAppearance overlayAppearance = CreateOverlayAppearance(atom, value);

            atomAppearance.Overlays.Add(overlayAppearance.GetID());
            UpdateAppearance(Runtime, atom, atomAppearance);
        }
        
        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = Runtime.OverlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = GetAppearance(Runtime, atom);
            ServerIconAppearance overlayAppearance = CreateOverlayAppearance(atom, value);
            int overlayAppearanceId = overlayAppearance.GetID();

            if (atomAppearance.Overlays.Contains(overlayAppearanceId)) {
                atomAppearance = new ServerIconAppearance(Runtime, atomAppearance);
                atomAppearance.Overlays.Remove(overlayAppearance.GetID());
                UpdateAppearance(Runtime, atom, atomAppearance);
            }
        }
        
        private void UnderlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = Runtime.UnderlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = new ServerIconAppearance(Runtime, GetAppearance(Runtime, atom));
            ServerIconAppearance underlayAppearance = CreateOverlayAppearance(atom, value);

            atomAppearance.Underlays.Add(underlayAppearance.GetID());
            UpdateAppearance(Runtime, atom, atomAppearance);
        }
        
        private void UnderlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = Runtime.UnderlaysListToAtom[overlayList];
            ServerIconAppearance atomAppearance = GetAppearance(Runtime, atom);
            ServerIconAppearance underlayAppearance = CreateOverlayAppearance(atom, value);
            int underlayAppearanceId = underlayAppearance.GetID();

            if (atomAppearance.Underlays.Contains(underlayAppearanceId)) {
                atomAppearance = new ServerIconAppearance(Runtime, atomAppearance);
                atomAppearance.Underlays.Remove(underlayAppearance.GetID());
                UpdateAppearance(Runtime, atom, atomAppearance);
            }
        }
    }
}
