using Content.Server.DM;
using OpenDreamShared.Dream;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.Dream.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IAtomManager _atomManager = IoCManager.Resolve<IAtomManager>();

        private const int BASE_ICON_LAYER = 0;
        private static readonly Dictionary<String, Color> _colors = new() {
            { "black", new Color(0, 0, 0) },
            { "silver", new Color(192, 192, 192) },
            { "gray", new Color(128, 128, 128) },
            { "grey", new Color(128, 128, 128) },
            { "white", new Color(255, 255, 255) },
            { "maroon", new Color(128, 0, 0) },
            { "red", new Color(255, 0, 0) },
            { "purple", new Color(128, 0, 128) },
            { "fuchsia", new Color(255, 0, 255) },
            { "magenta", new Color(255, 0, 255) },
            { "green", new Color(0, 192, 0) },
            { "lime", new Color(0, 255, 0) },
            { "olive", new Color(128, 128, 0) },
            { "gold", new Color(128, 128, 0) },
            { "yellow", new Color(255, 255, 0) },
            { "navy", new Color(0, 0, 128) },
            { "blue", new Color(0, 0, 255) },
            { "teal", new Color(0, 128, 128) },
            { "aqua", new Color(0, 255, 255) },
            { "cyan", new Color(0, 255, 255) }
        };

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            _atomManager.CreateAtomEntity(dreamObject);
            _dreamManager.WorldContentsList.AddValue(new DreamValue(dreamObject));

            DreamValue locArgument = creationArguments.GetArgument(0, "loc");
            if (locArgument.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out _)) {
                dreamObject.SetVariable("loc", locArgument); //loc is set before /New() is ever called
            } else if (creationArguments.ArgumentCount == 0) {
                creationArguments.OrderedArguments.Add(DreamValue.Null); //First argument is loc, which is null
            }

            //ServerIconAppearance atomAppearance = BuildAtomAppearance(dreamObject);
            //Runtime.StateManager.AddAtomCreation(dreamObject, atomAppearance);
            //UpdateAppearance(Runtime, dreamObject, atomAppearance);

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            _atomManager.DeleteAtomEntity(dreamObject);
            _dreamManager.WorldContentsList.RemoveValue(new DreamValue(dreamObject));

            //Runtime.OverlaysListToAtom.Remove(dreamObject.GetVariable("overlays").GetValueAsDreamList());
            //Runtime.UnderlaysListToAtom.Remove(dreamObject.GetVariable("underlays").GetValueAsDreamList());

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "icon") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();

                sprite.LayerSetTexture(BASE_ICON_LAYER, variableValue.GetValueAsString());
            } else if (variableName == "icon_state") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();

                sprite.LayerSetState(BASE_ICON_LAYER, variableValue.GetValueAsString());
            } else if (variableName == "pixel_x") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();

                sprite.Offset = new Vector2(variableValue.GetValueAsInteger(), sprite.Offset.Y);
            } else if (variableName == "pixel_y") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();

                sprite.Offset = new Vector2(sprite.Offset.X, variableValue.GetValueAsInteger());
            } else if (variableName == "layer") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();
                float layer = variableValue.GetValueAsFloat();

                //TODO: Better layer conversion
                sprite.DrawDepth = (int)(layer * 1000);
            } else if (variableName == "invisibility") {
                //TODO
            } else if (variableName == "mouse_opacity") {
                //TODO
            } else if (variableName == "color") {
                SpriteComponent sprite = _atomManager.GetAtomEntity(dreamObject).GetComponent<SpriteComponent>();
                Color color = Color.White;

                if (variableValue.TryGetValueAsString(out string colorText)) {
                    if (!_colors.TryGetValue(colorText, out color)) {
                        color = Color.TryFromHex(colorText) ?? Color.White;
                    }
                }

                sprite.LayerSetColor(BASE_ICON_LAYER, color);
            } else if (variableName == "dir") {
                AtomDirection dir = (AtomDirection)variableValue.GetValueAsInteger();

                //TODO
            } else if (variableName == "transform") {
                DreamObject matrix = variableValue.GetValueAsDreamObjectOfType(DreamPath.Matrix);

                //TODO
            } else if (variableName == "overlays") {
                //TODO

                //if (oldVariableValue != DreamValue.Null && oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                //    oldList.Cut();
                //    oldList.ValueAssigned -= OverlayValueAssigned;
                //    oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                //    Runtime.OverlaysListToAtom.Remove(oldList);
                //}

                //DreamList overlayList;
                //if (!variableValue.TryGetValueAsDreamList(out overlayList)) {
                //    overlayList = DreamList.Create(Runtime);
                //}

                //overlayList.ValueAssigned += OverlayValueAssigned;
                //overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                //Runtime.OverlaysListToAtom[overlayList] = dreamObject;
            } else if (variableName == "underlays") {
                //TODO

                //if (oldVariableValue != DreamValue.Null && oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                //    oldList.Cut();
                //    oldList.ValueAssigned -= UnderlayValueAssigned;
                //    oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                //    Runtime.UnderlaysListToAtom.Remove(oldList);
                //}

                //DreamList underlayList;
                //if (!variableValue.TryGetValueAsDreamList(out underlayList)) {
                //    underlayList = DreamList.Create(Runtime);
                //}

                //underlayList.ValueAssigned += UnderlayValueAssigned;
                //underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                //Runtime.UnderlaysListToAtom[underlayList] = dreamObject;
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            switch (variableName) {
                case "x":
                    return new(_atomManager.GetAtomEntity(dreamObject).Transform.WorldPosition.X);
                case "y":
                    return new(_atomManager.GetAtomEntity(dreamObject).Transform.WorldPosition.Y);
                case "z":
                    return new((int)_atomManager.GetAtomEntity(dreamObject).Transform.MapID);
                case "transform":
                    // Clone the matrix
                    //DreamObject matrix = Runtime.ObjectTree.CreateObject(DreamPath.Matrix);
                    //matrix.InitSpawn(new DreamProcArguments(new() { variableValue }));

                    //return new DreamValue(matrix);

                    //TODO
                    return DreamValue.Null;
                default:
                    return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        /*protected static void UpdateAppearance(DreamRuntime runtime, DreamObject atom, ServerIconAppearance newAppearance) {
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

            if (atom.GetVariable("mouse_opacity").TryGetValueAsInteger(out int mouseOpacity)) {
                appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
            }

            if (atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX)) {
                appearance.PixelX = pixelX;
            }

            if (atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY)) {
                appearance.PixelY = pixelY;
            }

            appearance.Layer = atom.GetVariable("layer").GetValueAsFloat();

            return appearance;
        }

        private ServerIconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            ServerIconAppearance appearance = new ServerIconAppearance(Runtime);

            if (value.TryGetValueAsString(out string valueString)) {
                appearance.Icon = GetAppearance(Runtime, atom).Icon;
                appearance.IconState = valueString;
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.TryGetValueAsDreamResource(out DreamResource iconResource)) {
                    appearance.Icon = iconResource.ResourcePath;
                } else if (icon.TryGetValueAsString(out string iconString)) {
                    appearance.Icon = iconString;
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
                appearance.Layer = mutableAppearance.GetVariable("layer").GetValueAsFloat();
                appearance.PixelX = mutableAppearance.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelY = mutableAppearance.GetVariable("pixel_y").GetValueAsInteger();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Image, out DreamObject image)) {
                DreamValue icon = image.GetVariable("icon");
                DreamValue iconState = image.GetVariable("icon_state");

                if (icon.TryGetValueAsDreamResource(out DreamResource iconResource)) {
                    appearance.Icon = iconResource.ResourcePath;
                } else {
                    appearance.Icon = icon.GetValueAsString();
                }

                if (iconState.TryGetValueAsString(out string iconStateString)) appearance.IconState = iconStateString;
                appearance.SetColor(image.GetVariable("color").GetValueAsString());
                appearance.Direction = (AtomDirection)image.GetVariable("dir").GetValueAsInteger();
                appearance.Layer = image.GetVariable("layer").GetValueAsFloat();
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
        }*/
    }
}
