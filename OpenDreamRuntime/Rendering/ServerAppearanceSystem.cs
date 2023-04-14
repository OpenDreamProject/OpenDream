using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Rendering {
    sealed class ServerAppearanceSystem : SharedAppearanceSystem {
        private readonly Dictionary<IconAppearance, uint> _appearanceToId = new();
        private readonly Dictionary<uint, IconAppearance> _idToAppearance = new();
        private uint _appearanceIdCounter = 0;

        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly DreamResourceManager _resourceManager = default!;

        public override void Initialize() {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown() {
            _appearanceToId.Clear();
            _idToAppearance.Clear();
            _appearanceIdCounter = 0;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
            if (e.NewStatus == SessionStatus.InGame) {
                RaiseNetworkEvent(new AllAppearancesEvent(_idToAppearance), e.Session.ConnectedClient);
            }
        }

        public uint AddAppearance(IconAppearance appearance) {
            if (!_appearanceToId.TryGetValue(appearance, out uint appearanceId)) {
                appearanceId = _appearanceIdCounter++;
                _appearanceToId.Add(appearance, appearanceId);
                _idToAppearance.Add(appearanceId, appearance);
                RaiseNetworkEvent(new NewAppearanceEvent(appearanceId, appearance));
            }

            return appearanceId;
        }

        public uint? GetAppearanceId(IconAppearance appearance) {
            if (_appearanceToId.TryGetValue(appearance, out uint id)) return id;

            return null;
        }

        public IconAppearance MustGetAppearance(uint appearanceId) {
            return _idToAppearance[appearanceId];
        }

        public bool TryGetAppearance(uint appearanceId, [NotNullWhen(true)] out IconAppearance? appearance) {
            return _idToAppearance.TryGetValue(appearanceId, out appearance);
        }

        public IconAppearance CreateAppearanceFrom(DreamValue value) {
            if (value.TryGetValueAsAppearance(out var copyFromAppearance)) {
                return new(copyFromAppearance);
            }

            if (value.TryGetValueAsDreamObjectOfType(_objectTree.Image, out var copyFromImage)) {
                return new(DreamMetaObjectImage.ObjectToAppearance[copyFromImage]);
            }

            if (value.TryGetValueAsType(out var copyFromType)) {
                return _atomManager.CreateAppearanceFromDefinition(copyFromType.ObjectDefinition);
            }

            if (value.TryGetValueAsDreamObjectOfType(_objectTree.Atom, out var copyFromAtom)) {
                return _atomManager.CreateAppearanceFromAtom(copyFromAtom);
            }

            var appearance = new IconAppearance();
            if (_resourceManager.TryLoadIcon(value, out var iconResource)) {
                appearance.Icon = iconResource.Id;
            } else if (value != DreamValue.Null) {
                throw new Exception($"Cannot create an appearance from {value}");
            }

            return appearance;
        }

        public bool IsValidAppearanceVar(string name) {
            switch (name) {
                case "icon":
                case "icon_state":
                case "dir":
                case "pixel_x":
                case "pixel_y":
                case "color":
                case "layer":
                case "invisibility":
                case "opacity":
                case "mouse_opacity":
                case "plane":
                case "blend_mode":
                case "appearance_flags":
                case "alpha":
                case "render_source":
                case "render_target":
                    return true;

                // Get/SetAppearanceVar doesn't handle these
                case "overlays":
                case "underlays":
                case "filters":
                case "transform":
                default:
                    return false;
            }
        }

        public void SetAppearanceVar(IconAppearance appearance, string varName, DreamValue value) {
            switch (varName) {
                case "icon":
                    if (_resourceManager.TryLoadIcon(value, out var icon)) {
                        appearance.Icon = icon.Id;
                    } else {
                        appearance.Icon = null;
                    }

                    break;
                case "icon_state":
                    value.TryGetValueAsString(out appearance.IconState);
                    break;
                case "dir":
                    //TODO figure out the weird inconsistencies with this being internally clamped
                    value.TryGetValueAsInteger(out var dir);

                    appearance.Direction = (AtomDirection)dir;
                    break;
                case "pixel_x":
                    value.TryGetValueAsInteger(out appearance.PixelOffset.X);
                    break;
                case "pixel_y":
                    value.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                    break;
                case "color":
                    if(value.TryGetValueAsDreamList(out var list)) {
                        if(DreamProcNativeHelpers.TryParseColorMatrix(list, out var matrix)) {
                            appearance.SetColor(in matrix);
                            break;
                        }

                        throw new ArgumentException($"Cannot set appearance's color to {value}");
                    }

                    value.TryGetValueAsString(out var colorString);
                    colorString ??= "white";
                    appearance.SetColor(colorString);
                    break;
                case "layer":
                    value.TryGetValueAsFloat(out appearance.Layer);
                    break;
                case "invisibility":
                    value.TryGetValueAsInteger(out int vis);
                    vis = Math.Clamp(vis, -127, 127); // DM ref says [0, 101]. BYOND compiler says [-127, 127]
                    appearance.Invisibility = vis;
                    break;
                case "opacity":
                    value.TryGetValueAsInteger(out var opacity);
                    appearance.Opacity = (opacity != 0);
                    break;
                case "mouse_opacity":
                    //TODO figure out the weird inconsistencies with this being internally clamped
                    value.TryGetValueAsInteger(out var mouseOpacity);
                    appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
                    break;
                case "plane":
                    value.TryGetValueAsInteger(out appearance.Plane);
                    break;
                case "blend_mode":
                    value.TryGetValueAsFloat(out float blendMode);
                    appearance.BlendMode = Enum.IsDefined((BlendMode)blendMode) ? (BlendMode)blendMode : BlendMode.BLEND_DEFAULT;
                    break;
                case "appearance_flags":
                    value.TryGetValueAsInteger(out int flagsVar);
                    appearance.AppearanceFlags = (AppearanceFlags) flagsVar;
                    break;
                case "alpha":
                    value.TryGetValueAsFloat(out float floatAlpha);
                    appearance.Alpha = (byte) floatAlpha;
                    break;
                case "render_source":
                    value.TryGetValueAsString(out appearance.RenderSource);
                    break;
                case "render_target":
                    value.TryGetValueAsString(out appearance.RenderTarget);
                    break;
                // TODO: overlays, underlays, filters, transform
                //       Those are handled separately by whatever is calling SetAppearanceVar currently
                default:
                    throw new ArgumentException($"Invalid appearance var {varName}");
            }
        }

        public DreamValue GetAppearanceVar(IconAppearance appearance, string varName) {
            switch (varName) {
                case "icon":
                    if (appearance.Icon == null)
                        return DreamValue.Null;

                    var iconResource = _resourceManager.GetResource(appearance.Icon.Value);
                    return new(iconResource);
                case "icon_state":
                    if (appearance.IconState == null)
                        return DreamValue.Null;

                    return new(appearance.IconState);
                case "dir":
                    return new((int) appearance.Direction);
                case "pixel_x":
                    return new(appearance.PixelOffset.X);
                case "pixel_y":
                    return new(appearance.PixelOffset.Y);
                case "color":
                    if(!appearance.ColorMatrix.Equals(ColorMatrix.Identity)) {
                        var matrixList = _objectTree.CreateList(20);
                        foreach (float entry in appearance.ColorMatrix.GetValues())
                            matrixList.AddValue(new DreamValue(entry));
                        return new DreamValue(matrixList);
                    }

                    if (appearance.Color == Color.White) {
                        return DreamValue.Null;
                    }

                    return new DreamValue(appearance.Color.ToHexNoAlpha().ToLower()); // BYOND quirk, does not return the alpha channel for some reason.
                case "layer":
                    return new(appearance.Layer);
                case "invisibility":
                    return new(appearance.Invisibility);
                case "opacity":
                    return appearance.Opacity ? DreamValue.True : DreamValue.False;
                case "mouse_opacity":
                    return new((int)appearance.MouseOpacity);
                case "plane":
                    return new(appearance.Plane);
                case "blend_mode":
                    return new((int) appearance.BlendMode);
                case "appearance_flags":
                    return new((int) appearance.AppearanceFlags);
                case "alpha":
                    return new(appearance.Alpha);
                case "render_source":
                    return new(appearance.RenderSource);
                case "render_target":
                    return new(appearance.RenderTarget);
                // TODO: overlays, underlays, filters, transform
                //       Those are handled separately by whatever is calling GetAppearanceVar currently
                default:
                    throw new ArgumentException($"Invalid appearance var {varName}");
            }
        }

        public void Animate(EntityUid entity, IconAppearance targetAppearance, TimeSpan duration) {
            uint appearanceId = AddAppearance(targetAppearance);

            RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration));
        }
    }
}
