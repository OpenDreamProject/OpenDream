using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
    class DreamMetaObjectAtom : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;

        public DreamMetaObjectAtom() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            if (!dreamObject.IsSubtypeOf(DreamPath.Turf)) {
                // Turfs can be new()ed multiple times, so let DreamMapManager handle it.
                _dreamManager.WorldContentsList.AddValue(new DreamValue(dreamObject));
            }

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            _atomManager.DeleteMovableEntity(dreamObject);
            _dreamManager.WorldContentsList.RemoveValue(new DreamValue(dreamObject));

            _atomManager.OverlaysListToAtom.Remove(dreamObject.GetVariable("overlays").GetValueAsDreamList());
            _atomManager.UnderlaysListToAtom.Remove(dreamObject.GetVariable("underlays").GetValueAsDreamList());
            _atomManager.FiltersListToAtom.Remove(dreamObject.GetVariable("filters").GetValueAsDreamList());
            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue)
        {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName)
            {
                case "icon":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        if (value.TryGetValueAsDreamResource(out DreamResource resource)) {
                            appearance.Icon = resource.ResourcePath;
                        } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out DreamObject iconObject)) {
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
                        value.TryGetValueAsString(out appearance.IconState);
                    });
                    break;
                case "pixel_x":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        value.TryGetValueAsInteger(out appearance.PixelOffset.X);
                    });
                    break;
                case "pixel_y":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        value.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                    });
                    break;
                case "layer":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        value.TryGetValueAsFloat(out appearance.Layer);
                    });
                    break;
                case "invisibility":
                    value.TryGetValueAsInteger(out int vis);
                    vis = Math.Clamp(vis, -127, 127); // DM ref says [0, 101]. BYOND compiler says [-127, 127]
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        appearance.Invisibility = vis;
                    });
                    dreamObject.SetVariableValue("invisibility", new DreamValue(vis));
                    break;
                case "opacity":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        value.TryGetValueAsInteger(out var opacity);
                        appearance.Opacity = (opacity != 0);
                    });
                    break;
                case "mouse_opacity":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        //TODO figure out the weird inconsistencies with this being internally clamped
                        value.TryGetValueAsInteger(out var opacity);
                        appearance.MouseOpacity = (MouseOpacity)opacity;
                    });
                    break;
                case "color":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        value.TryGetValueAsString(out string color);
                        color ??= "white";
                        appearance.SetColor(color);
                    });
                    break;
                case "dir":
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        //TODO figure out the weird inconsistencies with this being internally clamped
                        if (!value.TryGetValueAsInteger(out var dir))
                        {
                            dir = 2; // SOUTH
                        }
                        appearance.Direction = (AtomDirection)dir;
                    });
                    break;
                case "transform":
                {
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        float[] matrixArray = value.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out var matrix)
                            ? DreamMetaObjectMatrix.MatrixToTransformFloatArray(matrix)
                            : DreamMetaObjectMatrix.IdentityMatrixArray;

                        appearance.Transform = matrixArray;
                    });
                    break;
                }
                case "overlays":
                {
                    if (oldValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= OverlayValueAssigned;
                        oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                        _atomManager.OverlaysListToAtom.Remove(oldList);
                    }

                    DreamList overlayList;
                    if (!value.TryGetValueAsDreamList(out overlayList)) {
                        overlayList = DreamList.Create();
                    }

                    overlayList.ValueAssigned += OverlayValueAssigned;
                    overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                    _atomManager.OverlaysListToAtom[overlayList] = dreamObject;
                    dreamObject.SetVariableValue(varName, new DreamValue(overlayList));
                    break;
                }
                case "underlays":
                {
                    if (oldValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= UnderlayValueAssigned;
                        oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                        _atomManager.UnderlaysListToAtom.Remove(oldList);
                    }

                    DreamList underlayList;
                    if (!value.TryGetValueAsDreamList(out underlayList)) {
                        underlayList = DreamList.Create();
                    }

                    underlayList.ValueAssigned += UnderlayValueAssigned;
                    underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                    _atomManager.UnderlaysListToAtom[underlayList] = dreamObject;
                    dreamObject.SetVariableValue(varName, new DreamValue(underlayList));
                    break;
                }
                case "filters":
                {
                    if(value == DreamValue.Null)
                    {
                        dreamObject.SetVariableValue(varName, DreamValue.Null);
                        _atomManager.UpdateAppearance(dreamObject, appearance => {
                            appearance.Filters.Clear();
                        });
                        break;
                    }
                    DreamList filterList;
                    if (!value.TryGetValueAsDreamList(out filterList)) {
                        filterList = DreamList.Create();
                        _atomManager.FiltersListToAtom[filterList] = dreamObject;
                        filterList.ValueAssigned += FiltersValueAssigned;
                        filterList.BeforeValueRemoved += FiltersValueAssigned;
                        filterList.AddValue(value);
                    }
                    else
                    {
                        _atomManager.FiltersListToAtom[filterList] = dreamObject;
                        filterList.ValueAssigned += FiltersValueAssigned;
                        filterList.BeforeValueRemoved += FiltersValueAssigned;
                        if(filterList.GetLength() > 0)
                            FiltersValueAssigned(filterList, value, value); //this is super hacky, but trigger the update behaviour here
                    }
                    dreamObject.SetVariableValue(varName, new DreamValue(filterList));
                    break;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "transform":
                    // Clone the matrix
                    DreamObject matrix = _dreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                    matrix.InitSpawn(new DreamProcArguments(new() { value }));

                    return new DreamValue(matrix);
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        private IconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            IconAppearance appearance = new IconAppearance();

            if (value.TryGetValueAsString(out string valueString)) {
                appearance.Icon = _atomManager.GetMovableAppearance(atom)?.Icon;
                appearance.IconState = valueString;
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.MutableAppearance, out DreamObject mutableAppearance)) {
                DreamValue icon = mutableAppearance.GetVariable("icon");
                if (icon.TryGetValueAsDreamResource(out DreamResource iconResource)) {
                    appearance.Icon = iconResource.ResourcePath;
                } else if (icon.TryGetValueAsString(out string iconString)) {
                    appearance.Icon = iconString;
                } else if (icon == DreamValue.Null) {
                    appearance.Icon = _atomManager.GetMovableAppearance(atom)?.Icon;
                }

                DreamValue colorValue = mutableAppearance.GetVariable("color");
                if (colorValue.TryGetValueAsString(out string color)) {
                    appearance.SetColor(color);
                } else {
                    appearance.SetColor("white");
                }

                appearance.IconState = mutableAppearance.GetVariable("icon_state").TryGetValueAsString(out var iconState) ? iconState : null;
                mutableAppearance.GetVariable("layer").TryGetValueAsFloat(out appearance.Layer);
                mutableAppearance.GetVariable("pixel_x").TryGetValueAsInteger(out appearance.PixelOffset.X);
                mutableAppearance.GetVariable("pixel_y").TryGetValueAsInteger(out appearance.PixelOffset.Y);
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
                image.GetVariable("layer").TryGetValueAsFloat(out appearance.Layer);
                image.GetVariable("pixel_x").TryGetValueAsInteger(out appearance.PixelOffset.X);
                image.GetVariable("pixel_y").TryGetValueAsInteger(out appearance.PixelOffset.Y);
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

        private void FiltersValueAssigned(DreamList filterList, DreamValue key, DreamValue value)
        {
            DreamObject dreamObject = _atomManager.FiltersListToAtom[filterList];
            _atomManager.UpdateAppearance(dreamObject, appearance => {
                    appearance.Filters.Clear();
            });
            foreach(DreamValue listValue in filterList.GetValues())
            {
                _atomManager.UpdateAppearance(dreamObject, appearance => {
                    DreamFilter newFilter = new DreamFilter();

                    DreamObject DMFilterObject;
                    if(!listValue.TryGetValueAsDreamObjectOfType(DreamPath.Filter, out DMFilterObject))
                        throw new Exception("Tried to add a non-filter object to a list of filters");
                    DreamMetaObjectFilter._FilterToDreamObject[DMFilterObject] = dreamObject;
                    DreamValue filterVarValue;

                    if(DMFilterObject.TryGetVariable("type", out filterVarValue))
                    {
                        DreamPath typedVal;
                        if(filterVarValue.TryGetValueAsPath(out typedVal))
                            newFilter.filter_type = typedVal.LastElement;
                    }
                    Dictionary<string, ValueTuple<Type, bool, Object>> variableDict = DreamFilter.filterParameters[DreamPath.Filter.AddToPath(newFilter.filter_type)];
                    foreach(string varName in variableDict.Keys)
                    {
                        ValueTuple<Type, bool, Object> varInfo = variableDict[varName];
                        if(varInfo.Item1 == typeof(float))
                        {
                            float varValue;
                            if(!DMFilterObject.TryGetVariable(varName, out filterVarValue) || !filterVarValue.TryGetValueAsFloat(out varValue))
                                if(varInfo.Item2)
                                    throw new Exception($"Variable {varName} is mandatory for filter type {newFilter.filter_type}");
                                else
                                    varValue = (float) varInfo.Item3;
                            newFilter.parameters[varName] = varValue;
                        }
                        if(varInfo.Item1 == typeof(string))
                        {
                            string varValue;
                            if(!DMFilterObject.TryGetVariable(varName, out filterVarValue) || !filterVarValue.TryGetValueAsString(out varValue))
                                if(varInfo.Item2)
                                    throw new Exception($"Variable {varName} is mandatory for filter type {newFilter.filter_type}");
                                else
                                    varValue = (string) varInfo.Item3;
                            newFilter.parameters[varName] = varValue;
                        }
                        if(varInfo.Item1 == typeof(Color))
                        {
                            string colorString;
                            Color varValue;
                            if(!DMFilterObject.TryGetVariable(varName, out filterVarValue) || !filterVarValue.TryGetValueAsString(out colorString))
                                if(varInfo.Item2)
                                    throw new Exception($"Variable {varName} is mandatory for filter type {newFilter.filter_type}");
                                else
                                    varValue = (Color) varInfo.Item3;
                            else if(!ColorHelpers.TryParseColor(colorString, out varValue))
                                throw new Exception($"Invalid color: {colorString}");
                            newFilter.parameters[varName] = varValue;
                        }
                    }
                    appearance.Filters.Add(newFilter);
                });
            }
        }

    }
}
