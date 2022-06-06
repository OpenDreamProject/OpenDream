using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
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
                case "name": {
                    variableValue.TryGetValueAsString(out string name);
                    EntityUid entity = _atomManager.GetAtomEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out MetaDataComponent metaData))
                        break;

                    metaData.EntityName = name;
                    break;
                }
                case "desc": {
                    variableValue.TryGetValueAsString(out string desc);
                    EntityUid entity = _atomManager.GetAtomEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out MetaDataComponent metaData))
                        break;

                    metaData.EntityDescription = desc;
                    break;
                }
                case "icon":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        if (variableValue.TryGetValueAsDreamResource(out DreamResource resource)) {
                            appearance.Icon = resource.ResourcePath;
                        } else if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out DreamObject iconObject)) {
                            DreamMetaObjectIcon.DreamIconObject icon = DreamMetaObjectIcon.ObjectToDreamIcon[iconObject];

                            appearance.Icon = icon.Icon;
                            if (icon.State != null) appearance.IconState = icon.State;
                            //TODO: If a dir is set, the icon will stay that direction. Likely will be a part of "icon generation" when that's implemented.
                            if (icon.Direction != null) appearance.Direction = icon.Direction.Value;
                        } else {
                            appearance.Icon = null;
                        }
                    });
                    break;
                case "icon_state":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsString(out appearance.IconState);
                    });
                    break;
                case "pixel_x":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsInteger(out appearance.PixelOffset.X);
                    });
                    break;
                case "pixel_y":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                    });
                    break;
                case "layer":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsFloat(out appearance.Layer);
                    });
                    break;
                case "invisibility":
                    variableValue.TryGetValueAsInteger(out int vis);
                    vis = Math.Clamp(vis, -127, 127); // DM ref says [0, 101]. BYOND compiler says [-127, 127]
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        appearance.Invisibility = vis;
                    });
                    dreamObject.SetVariableValue("invisibility", new DreamValue(vis));
                    break;
                case "mouse_opacity":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        //TODO figure out the weird inconsistencies with this being internally clamped
                        variableValue.TryGetValueAsInteger(out var opacity);
                        appearance.MouseOpacity = (MouseOpacity)opacity;
                    });
                    break;
                case "color":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        variableValue.TryGetValueAsString(out string color);
                        color ??= "white";
                        appearance.SetColor(color);
                    });
                    break;
                case "dir":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        //TODO figure out the weird inconsistencies with this being internally clamped
                        if (!variableValue.TryGetValueAsInteger(out var dir))
                        {
                            dir = 2; // SOUTH
                        }
                        appearance.Direction = (AtomDirection)dir;
                    });
                    break;
                case "transform":
                {
                    DreamObject matrix = variableValue.GetValueAsDreamObjectOfType(DreamPath.Matrix);

                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        appearance.Transform = DreamMetaObjectMatrix.MatrixToFloatArray(matrix);
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

                appearance.IconState = mutableAppearance.GetVariable("icon_state").TryGetValueAsString(out var iconState) ? iconState : null;
                appearance.Layer = mutableAppearance.GetVariable("layer").GetValueAsFloat();
                appearance.PixelOffset.X = mutableAppearance.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelOffset.Y = mutableAppearance.GetVariable("pixel_y").GetValueAsInteger();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Image, out DreamObject image)) {
                DreamValue icon = image.GetVariable("icon");
                DreamValue iconState = image.GetVariable("icon_state");

                if (icon.TryGetValueAsDreamResource(out DreamResource iconResource)) {
                    appearance.Icon = iconResource.ResourcePath;
                } else {
                    appearance.Icon = icon.TryGetValueAsString(out var iconString) ? iconString : null;
                }

                if (iconState.TryGetValueAsString(out string iconStateString)) appearance.IconState = iconStateString;
                var color = image.GetVariable("color").TryGetValueAsString(out var colorString)
                    ? colorString
                    : "#FFFFFF"; // Defaults to white
                appearance.SetColor(color);
                appearance.Direction = (AtomDirection)image.GetVariable("dir").GetValueAsInteger();
                appearance.Layer = image.GetVariable("layer").GetValueAsFloat();
                appearance.PixelOffset.X = image.GetVariable("pixel_x").GetValueAsInteger();
                appearance.PixelOffset.Y = image.GetVariable("pixel_y").GetValueAsInteger();
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject overlayAtom))
            {
                appearance = _atomManager.CreateAppearanceFromAtom(overlayAtom);
            } else if (value.TryGetValueAsPath(out DreamPath path))
            {
                var def = _dreamManager.ObjectTree.GetObjectDefinition(path);
                appearance = _atomManager.CreateAppearanceFromDefinition(def);
            }
            else {
                throw new Exception($"Invalid overlay {value}");
            }

            return appearance;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];

            _atomManager.UpdateAppearance(atom, appearance => {
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

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Overlays.Remove(overlayAppearanceId.Value);
            });
        }

        private void UnderlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[overlayList];

            _atomManager.UpdateAppearance(atom, appearance => {
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

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Underlays.Remove(underlayAppearanceId.Value);
            });
        }
    }
}
