using System;
using System.Collections.Generic;
using OpenDreamClient.Dream;
using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace OpenDreamClient.Renderer
{
    public class DreamOverlay : Overlay
    {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly OpenDream _openDream = default!;

        [ViewVariables]
        public (int X, int Y, int Z) Camera;
        private readonly int _viewDistance = 7;

        [ViewVariables]
        private static Dictionary<(string, UIBox2), Texture> _textureCache = new();

        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        public DreamOverlay()
        {
            IoCManager.InjectDependencies(this);
        }

        private Texture GetDreamTexture(DreamIcon icon) {
            Texture texture = null;

            if (icon != null && icon.IsValidIcon()) {
                var textureRect = icon.GetTextureRect();

                if (!_textureCache.TryGetValue((icon.DMI.ResourcePath, textureRect), out texture)) {

                    texture = new AtlasTexture(icon.DMI.Texture, textureRect);

                    _textureCache.Add((icon.DMI.ResourcePath, textureRect), texture);
                }
            }

            return texture;
        }

        public UIBox2 GetIconRect(ATOM atom, bool useScreenLocation) {
            System.Drawing.Point position;
            System.Drawing.Size size = new System.Drawing.Size(32, 32);

            if (useScreenLocation) {
                position = atom.ScreenLocation.GetScreenCoordinates(32);
                size.Width *= atom.ScreenLocation.RepeatX;
                size.Height *= atom.ScreenLocation.RepeatY;
            }  else {
                int tileX = atom.X - Camera.X + 7;
                int tileY = atom.Y - Camera.Y + 7;

                position = new System.Drawing.Point(tileX * 32 + atom.Icon.Appearance.PixelX, tileY * 32 + atom.Icon.Appearance.PixelY);
            }

            return UIBox2.FromDimensions(new Vector2(position.X, position.Y), new Vector2(position.X, position.Y));
        }

        public bool IsAtomVisible(ATOM atom, bool useScreenLocation) {
            UIBox2 iconRect = GetIconRect(atom, useScreenLocation);

            if (atom.Icon.Appearance.Invisibility > 0) return false; //0 is the default invisibility a mob can see


            // TODO ROBUST: Fix this
            return true;
            //return (iconRect.X >= -iconRect.Width && iconRect.X <= OpenGLViewControl.Width &&
            //        iconRect.Y >= -iconRect.Height && iconRect.Y <= OpenGLViewControl.Height);
        }

        private void UpdateCameraPosition() {
            ATOM Eye = _openDream.Eye;

            if (Eye != null) {
                Camera = (Eye.X, Eye.Y, Eye.Z);

                if (_openDream.Perspective.HasFlag(ClientPerspective.Edge)) {
                    Map map = _openDream.Map;

                    Camera.X = Math.Min(Math.Max(Camera.X, _viewDistance), map.Width - _viewDistance);
                    Camera.Y = Math.Min(Math.Max(Camera.Y, _viewDistance), map.Height - _viewDistance);
                }
            } else {
                Camera = (1, 1, 1);
            }
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            UpdateCameraPosition();

            if (_openDream.Map == null)
                return;

            switch (args.Space)
            {
                case OverlaySpace.WorldSpace:
                {
                    List<ATOM> turfs = _openDream.Map.GetTurfs(Camera.X - _viewDistance - 1, Camera.Y - _viewDistance - 1, Camera.Z, 16, 16);
                    List<ATOM> mapAtoms = new();

                    foreach (ATOM turf in turfs) {
                        if (IsAtomVisible(turf, false)) {
                            mapAtoms.Add(turf);
                            mapAtoms.AddRange(turf.Contents);
                        }
                    }

                    DrawAtoms(mapAtoms, args.WorldHandle);
                    break;
                }
                case OverlaySpace.ScreenSpace:
                    DrawAtoms(_openDream.ScreenObjects, args.ScreenHandle);
                    break;
            }
        }

        private void DrawAtoms(List<ATOM> atoms, DrawingHandleScreen handle)
        {
            SortAtoms(atoms);

            foreach (var atom in atoms)
            {
                var screenCoordinates = atom.ScreenLocation.GetScreenCoordinates(32);

                handle.SetTransform(new Vector2(screenCoordinates.X, screenCoordinates.Y), Angle.Zero);

                if (IsAtomVisible(atom, true))
                    DrawDreamIcon(atom.Icon, false, handle);

                handle.SetTransform(Matrix3.Identity);
            }
        }

        private void DrawAtoms(List<ATOM> atoms, DrawingHandleWorld handle)
        {
            SortAtoms(atoms);

            foreach (var atom in atoms)
            {
                handle.SetTransform(new Vector2((atom.X - Camera.X), (atom.Y - Camera.Y)), Angle.Zero);

                if (IsAtomVisible(atom, false))
                    DrawDreamIcon(atom.Icon, true, handle);
            }
        }

        private void SortAtoms(List<ATOM> atoms)
        {
            //Sort by layer
            atoms.Sort(
                new Comparison<ATOM>((ATOM first, ATOM second) => {
                    int layerSort = DreamIcon.LayerSort(first.Icon, second.Icon);

                    if (layerSort == 0)
                        return (int)first.ID - (int)second.ID; //Sort by ID instead
                    return layerSort;
                })
            );
        }

        private void DrawDreamIcon(DreamIcon icon, bool usePixelOffsets, DrawingHandleBase handle, int pixelX = 0, int pixelY = 0, float[] transform = null) {
            Texture texture = GetDreamTexture(icon);

            transform ??= icon.Appearance.Transform;
            if (usePixelOffsets) {
                pixelX += icon.Appearance.PixelX;
                pixelY += icon.Appearance.PixelY;
            }

            foreach (DreamIcon underlay in icon.Underlays) {
                DrawDreamIcon(underlay, usePixelOffsets, handle, pixelX, pixelY, transform);
            }

            if (texture != null) {
                if(handle is DrawingHandleScreen screen)
                    screen.DrawTexture(texture, new Vector2(pixelX, pixelY));
                else if(handle is DrawingHandleWorld world)
                    world.DrawTexture(texture, new Vector2(pixelX / 32f, pixelY / 32f));
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                DrawDreamIcon(overlay, usePixelOffsets, handle, pixelX, pixelY, transform);
            }
        }
    }
}
