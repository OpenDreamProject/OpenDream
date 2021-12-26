﻿using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IAtomManager _atomManager = IoCManager.Resolve<IAtomManager>();
        private IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
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

            _atomManager.OverlaysListToAtom.Remove(dreamObject.GetVariable("overlays").GetValueAsDreamList());
            _atomManager.UnderlaysListToAtom.Remove(dreamObject.GetVariable("underlays").GetValueAsDreamList());

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue)
        {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            switch (variableName)
            {
                case "icon":
                    UpdateAppearance(dreamObject, appearance => {
                        if (variableValue.TryGetValueAsDreamResource(out DreamResource resource)) {
                            appearance.Icon = resource.ResourcePath;
                        } else {
                            appearance.Icon = null;
                        }
                    });
                    break;
                case "icon_state":
                    UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsString(out appearance.IconState);
                    });
                    break;
                case "pixel_x":
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.PixelOffset.X = variableValue.GetValueAsInteger();
                    });
                    break;
                case "pixel_y":
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.PixelOffset.Y = variableValue.GetValueAsInteger();
                    });
                    break;
                case "layer":
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.Layer = variableValue.GetValueAsFloat();
                    });
                    break;
                case "invisibility":
                    variableValue.TryGetValueAsInteger(out int vis);
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.Invisibility = vis;
                    });
                    dreamObject.SetVariableValue("invisibility", new DreamValue(vis));
                    break;
                case "mouse_opacity":
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.MouseOpacity = (MouseOpacity)variableValue.GetValueAsInteger();
                    });
                    break;
                case "color":
                    UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsString(out string color);
                        color ??= "white";
                        appearance.SetColor(color);
                    });
                    break;
                case "dir":
                    UpdateAppearance(dreamObject, appearance => {
                        appearance.Direction = (AtomDirection)variableValue.GetValueAsInteger();
                    });
                    break;
                case "transform":
                {
                    DreamObject matrix = variableValue.GetValueAsDreamObjectOfType(DreamPath.Matrix);

                    UpdateAppearance(dreamObject, appearance => {
                        appearance.Transform[0] = matrix.GetVariable("a").GetValueAsFloat();
                        appearance.Transform[1] = matrix.GetVariable("d").GetValueAsFloat();
                        appearance.Transform[2] = matrix.GetVariable("b").GetValueAsFloat();
                        appearance.Transform[3] = matrix.GetVariable("e").GetValueAsFloat();
                        appearance.Transform[4] = matrix.GetVariable("c").GetValueAsFloat();
                        appearance.Transform[5] = matrix.GetVariable("f").GetValueAsFloat();
                    });
                    break;
                }
                case "overlays":
                {
                    if (oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= OverlayValueAssigned;
                        oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                        _atomManager.OverlaysListToAtom.Remove(oldList);
                    }

                    DreamList overlayList;
                    if (!variableValue.TryGetValueAsDreamList(out overlayList)) {
                        overlayList = DreamList.Create();
                    }

                    overlayList.ValueAssigned += OverlayValueAssigned;
                    overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                    _atomManager.OverlaysListToAtom[overlayList] = dreamObject;
                    dreamObject.SetVariableValue(variableName, new DreamValue(overlayList));
                    break;
                }
                case "underlays":
                {
                    if (oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= UnderlayValueAssigned;
                        oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                        _atomManager.UnderlaysListToAtom.Remove(oldList);
                    }

                    DreamList underlayList;
                    if (!variableValue.TryGetValueAsDreamList(out underlayList)) {
                        underlayList = DreamList.Create();
                    }

                    underlayList.ValueAssigned += UnderlayValueAssigned;
                    underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                    _atomManager.UnderlaysListToAtom[underlayList] = dreamObject;
                    dreamObject.SetVariableValue(variableName, new DreamValue(underlayList));
                    break;
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            switch (variableName) {
                case "x":
                    return new(_entityManager.GetComponentOrNull<TransformComponent>(_atomManager.GetAtomEntity(dreamObject))?.WorldPosition.X ?? 0);
                case "y":
                    return new(_entityManager.GetComponentOrNull<TransformComponent>(_atomManager.GetAtomEntity(dreamObject))?.WorldPosition.Y ?? 0);
                case "z":
                    return new(((int?)_entityManager.GetComponentOrNull<TransformComponent>(_atomManager.GetAtomEntity(dreamObject))?.MapID) ?? 0);
                case "contents":
                    DreamList contents = DreamList.Create();
                    EntityUid entity = _atomManager.GetAtomEntity(dreamObject);

                    if (_entityManager.TryGetComponent<TransformComponent>(entity, out var transform)) {
                        foreach (TransformComponent child in transform.Children) {
                            DreamObject childAtom = _atomManager.GetAtomFromEntity(child.Owner);

                            contents.AddValue(new DreamValue(childAtom));
                        }
                    }

                    return new(contents);
                case "transform":
                    // Clone the matrix
                    DreamObject matrix = _dreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                    matrix.InitSpawn(new DreamProcArguments(new() { variableValue }));

                    return new DreamValue(matrix);
                default:
                    return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        private void UpdateAppearance(DreamObject atom, Action<IconAppearance> update) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(_atomManager.GetAtomEntity(atom), out var sprite))
                return;
            IconAppearance appearance = new IconAppearance(sprite.Appearance);

            update(appearance);
            sprite.Appearance = appearance;
        }

        private IconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            IconAppearance appearance = new IconAppearance();

            if (value.TryGetValueAsString(out string valueString)) {
                appearance.Icon = _atomManager.GetAppearance(atom)?.Icon;
                appearance.IconState = valueString;
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.TryGetValueAsDreamResource(out DreamResource iconResource)) {
                    appearance.Icon = iconResource.ResourcePath;
                } else if (icon.TryGetValueAsString(out string iconString)) {
                    appearance.Icon = iconString;
                } else if (icon == DreamValue.Null) {
                    appearance.Icon = _atomManager.GetAppearance(atom)?.Icon;
                }

                DreamValue colorValue = mutableAppearance.GetVariable("color");
                if (colorValue.TryGetValueAsString(out string color)) {
                    appearance.SetColor(color);
                } else {
                    appearance.SetColor("white");
                }

                appearance.IconState = mutableAppearance.GetVariable("icon_state").GetValueAsString();
                appearance.Layer = mutableAppearance.GetVariable("layer").GetValueAsFloat();
                appearance.PixelOffset.X = mutableAppearance.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelOffset.Y = mutableAppearance.GetVariable("pixel_y").GetValueAsInteger();
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
                appearance.PixelOffset.X = image.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelOffset.Y = image.GetVariable("pixel_y").GetValueAsInteger();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject overlayAtom)) {
                appearance = _atomManager.CreateAppearanceFromAtom(overlayAtom);
            } else {
                throw new Exception($"Invalid overlay {value}");
            }

            return appearance;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];

            UpdateAppearance(atom, appearance => {
                IconAppearance overlay = CreateOverlayAppearance(atom, value);
                uint id = EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(overlay);

                appearance.Overlays.Add(id);
            });
        }

        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];
            IconAppearance overlayAppearance = CreateOverlayAppearance(atom, value);
            uint? overlayAppearanceId = EntitySystem.Get<ServerAppearanceSystem>().GetAppearanceId(overlayAppearance);
            if (overlayAppearanceId == null) return;

            UpdateAppearance(atom, appearance => {
                appearance.Overlays.Remove(overlayAppearanceId.Value);
            });
        }

        private void UnderlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[overlayList];

            UpdateAppearance(atom, appearance => {
                IconAppearance underlay = CreateOverlayAppearance(atom, value);
                uint id = EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(underlay);

                appearance.Underlays.Add(id);
            });
        }

        private void UnderlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[overlayList];
            IconAppearance underlayAppearance = CreateOverlayAppearance(atom, value);
            uint? underlayAppearanceId = EntitySystem.Get<ServerAppearanceSystem>().GetAppearanceId(underlayAppearance);
            if (underlayAppearanceId == null) return;

            UpdateAppearance(atom, appearance => {
                appearance.Underlays.Remove(underlayAppearanceId.Value);
            });
        }
    }
}
