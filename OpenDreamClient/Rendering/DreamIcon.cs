using System;
using System.Collections.Generic;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamClient.Rendering {
    class DreamIcon {
        public delegate void DMIChangedEventHandler(DMIResource oldDMI, DMIResource newDMI);

        public List<DreamIcon> Overlays { get; } = new();
        public List<DreamIcon> Underlays { get; } = new();
        public event DMIChangedEventHandler DMIChanged;

        public DMIResource DMI {
            get => _dmi;
            private set {
                DMIChanged?.Invoke(_dmi, value);
                _dmi = value;
            }
        }
        private DMIResource _dmi;

        public int AnimationFrame {
            get {
                UpdateAnimation();
                return _animationFrame;
            }
        }

        public IconAppearance Appearance {
            get => _appearance;
            private set {
                _appearance = value;
                UpdateIcon();
            }
        }
        private IconAppearance _appearance;

        public AtlasTexture CurrentFrame {
            get => DMI?.GetState(Appearance.IconState)?.GetFrames(Appearance.Direction)[AnimationFrame];
        }

        private int _animationFrame;
        private DateTime _animationFrameTime = DateTime.Now;

        public DreamIcon() { }

        public DreamIcon(uint appearanceId, AtomDirection? parentDir = null) {
            SetAppearance(appearanceId, parentDir);
        }

        public void SetAppearance(uint? appearanceId, AtomDirection? parentDir = null) {
            if (appearanceId == null) {
                Appearance = null;
                return;
            }

            ClientAppearanceSystem appearanceSystem = EntitySystem.Get<ClientAppearanceSystem>();

            appearanceSystem.LoadAppearance(appearanceId.Value, appearance => {
                if (appearance.Direction == AtomDirection.None && parentDir != null) {
                    appearance = new IconAppearance(appearance) {
                        Direction = parentDir.Value
                    };
                }

                Appearance = appearance;
            });
        }

        public void UpdateAnimation() {
            DMIParser.ParsedDMIState dmiState = DMI.Description.GetState(Appearance.IconState);
            DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(Appearance.Direction);

            if (_animationFrame == frames.Length - 1 && !dmiState.Loop) return;

            double elapsedTime = DateTime.Now.Subtract(_animationFrameTime).TotalMilliseconds;
            while (elapsedTime >= frames[_animationFrame].Delay) {
                elapsedTime -= frames[_animationFrame].Delay;
                _animationFrameTime = _animationFrameTime.AddMilliseconds(frames[_animationFrame].Delay);
                _animationFrame++;

                if (_animationFrame >= frames.Length) _animationFrame -= frames.Length;
            }
        }

        private static int LayerSort(DreamIcon first, DreamIcon second) {
            float diff = first.Appearance.Layer - second.Appearance.Layer;

            if (diff < 0) return -1;
            else if (diff > 0) return 1;
            return 0;
        }

        private void UpdateIcon() {
            if (Appearance?.Icon == null) {
                DMI = null;
                return;
            }

            IoCManager.Resolve<IDreamResourceManager>().LoadResourceAsync<DMIResource>(Appearance.Icon, dmi => {
                if (dmi.ResourcePath != Appearance.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationFrame = 0;
                _animationFrameTime = DateTime.Now;
            });

            Overlays.Clear();
            foreach (uint overlayId in Appearance.Overlays) {
                Overlays.Add(new DreamIcon(overlayId, Appearance.Direction));
            }

            Underlays.Clear();
            foreach (uint underlayId in Appearance.Underlays) {
                Underlays.Add(new DreamIcon(underlayId, Appearance.Direction));
            }

            Overlays.Sort(new Comparison<DreamIcon>(LayerSort));
            Underlays.Sort(new Comparison<DreamIcon>(LayerSort));
        }
    }
}
