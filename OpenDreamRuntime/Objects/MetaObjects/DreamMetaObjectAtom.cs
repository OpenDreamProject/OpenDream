using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using System;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IAtomManager _atomManager = IoCManager.Resolve<IAtomManager>();
        private AppearanceSystem _appearanceSystem = EntitySystem.Get<AppearanceSystem>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            _atomManager.CreateAtomEntity(dreamObject);
            _dreamManager.WorldContentsList.AddValue(new DreamValue(dreamObject));

            DreamValue locArgument = creationArguments.GetArgument(0, "loc");
            if (locArgument.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out _)) {
                dreamObject.SetVariable("loc", locArgument); //loc is set before /New() is ever called
            } else if (creationArguments.ArgumentCount == 0) {
                creationArguments.OrderedArguments.Add(DreamValue.Null); //First argument is loc, which is null
            }

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
                UpdateAppearance(dreamObject, appearance => {
                    if (variableValue.TryGetValueAsDreamResource(out DreamResource resource)) {
                        appearance.Icon = new ResourcePath(resource.ResourcePath);
                    } else {
                        appearance.Icon = null;
                    }
                });
            } else if (variableName == "icon_state") {
                UpdateAppearance(dreamObject, appearance => {
                    variableValue.TryGetValueAsString(out appearance.IconState);
                });
            } else if (variableName == "pixel_x") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.PixelOffset.X = variableValue.GetValueAsInteger();
                });
            } else if (variableName == "pixel_y") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.PixelOffset.Y = variableValue.GetValueAsInteger();
                });
            } else if (variableName == "layer") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.Layer = variableValue.GetValueAsFloat();
                });
            } else if (variableName == "invisibility") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.Invisibility = variableValue.GetValueAsInteger();
                });
            } else if (variableName == "mouse_opacity") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.MouseOpacity = (MouseOpacity)variableValue.GetValueAsInteger();
                });
            } else if (variableName == "color") {
                UpdateAppearance(dreamObject, appearance => {
                    variableValue.TryGetValueAsString(out string color);
                    color ??= "white";
                    appearance.SetColor(color);
                });
            } else if (variableName == "dir") {
                UpdateAppearance(dreamObject, appearance => {
                    appearance.Direction = (AtomDirection)variableValue.GetValueAsInteger();
                });
            } else if (variableName == "transform") {
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
                case "contents":
                    DreamList contents = DreamList.Create();
                    IEntity entity = _atomManager.GetAtomEntity(dreamObject);

                    foreach (ITransformComponent child in entity.Transform.Children) {
                        DreamObject childAtom = _atomManager.GetAtomFromEntity(child.Owner);

                        contents.AddValue(new DreamValue(childAtom));
                    }

                    return new(contents);
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

        private void UpdateAppearance(DreamObject atom, Action<IconAppearance> update) {
            DMISpriteComponent sprite = _atomManager.GetAtomEntity(atom).GetComponent<DMISpriteComponent>();
            IconAppearance appearance = new IconAppearance(sprite.Appearance);

            update(appearance);
            _appearanceSystem.AddAppearance(appearance);
            sprite.Appearance = appearance;
        }

        /*private ServerIconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
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
