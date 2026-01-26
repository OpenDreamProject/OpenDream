using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane(IRenderTexture mainRenderTarget) : IDisposable {
    public IRenderTexture RenderTarget => _temporaryRenderTarget ?? mainRenderTarget;
    public RendererMetaData? Master;
    public bool Enabled = true;

    public readonly List<RendererMetaData> Sprites = new();

    private IRenderTexture? _temporaryRenderTarget;

    public void Clear() {
        Master = null;
        Sprites.Clear();
        _temporaryRenderTarget = null;
    }

    public void Dispose() {
        mainRenderTarget.Dispose();
        Clear();
    }

    /// <summary>
    /// Sets this plane's main render target<br/>
    /// Persists through calls to <see cref="Clear()"/>
    /// </summary>
    public void SetMainRenderTarget(IRenderTexture renderTarget) {
        mainRenderTarget.Dispose();
        mainRenderTarget = renderTarget;
    }

    /// <summary>
    /// Sets this plane's render target until the next <see cref="Clear()"/>
    /// </summary>
    public void SetTemporaryRenderTarget(IRenderTexture renderTarget) {
        DebugTools.Assert(_temporaryRenderTarget == null, "Temporary render target has already been set");
        _temporaryRenderTarget = renderTarget;
    }

    /// <summary>
    /// Clears this plane's render target, then draws all the plane's icons onto it
    /// </summary>
    public void Draw(DreamViewOverlay overlay, DrawingHandleWorld handle, Box2 worldAABB) {
        // Draw all icons
        handle.RenderInRenderTarget(mainRenderTarget, () => {
            if (!Enabled) // Inside the RenderInRenderTarget() to ensure it gets cleared
                return;

            foreach (var sprite in Sprites) {
                var positionOffset = -worldAABB.BottomLeft;

                if (sprite.HasRenderSource && overlay.RenderSourceLookup.TryGetValue(sprite.RenderSource!, out var renderSourceTexture)) {
                    sprite.TextureOverride = renderSourceTexture.Texture;
                }

                overlay.DrawIcon(handle, mainRenderTarget.Size, sprite, positionOffset);
            }
        }, new Color());

        if (_temporaryRenderTarget != null) {
            // Draw again, but with the color applied
            handle.RenderInRenderTarget(_temporaryRenderTarget, () => {
                if (Master == null || !Enabled)
                    return;

                Master.TextureOverride = mainRenderTarget.Texture;
                var texture = Master.GetTexture(overlay, handle);
                Master.TextureOverride = null;
                if (texture == null)
                    return;

                handle.UseShader(overlay.GetBlendAndColorShader(Master, useOverlayMode: true));
                handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(_temporaryRenderTarget.Size, Master.MainIcon?.TextureRenderOffset ?? Vector2.Zero));
                handle.DrawTextureRect(texture, new Box2(Vector2.Zero, texture.Size), Master.ColorToApply);
            }, new Color());
        }
    }

    /// <summary>
    /// Draws this plane's mouse map onto the current render target
    /// </summary>
    public void DrawMouseMap(DrawingHandleWorld handle, DreamViewOverlay overlay, Vector2i renderTargetSize, Box2 worldAABB) {
        if (Master?.MouseOpacity == MouseOpacity.Transparent || !Enabled)
            return;

        handle.UseShader(overlay.BlockColorInstance);
        foreach (var sprite in Sprites) {
            if (sprite.MouseOpacity == MouseOpacity.Transparent || sprite.ShouldPassMouse)
                continue;

            var texture = sprite.MainIcon?.LastRenderedTexture;
            if (texture == null)
                continue;

            // For mouse_opacity = 2, discard transparency but keep the size.
            var textureSize = texture.Size;
            if (sprite.MouseOpacity == MouseOpacity.Opaque) {
                texture = Texture.White;
            }

            var pos = (sprite.Position - worldAABB.BottomLeft) * overlay.IconSize;
            if (sprite.MainIcon != null)
                pos += sprite.MainIcon.TextureRenderOffset;

            int hash = sprite.GetHashCode();
            var colorR = (byte)(hash & 0xFF);
            var colorG = (byte)((hash >> 8) & 0xFF);
            var colorB = (byte)((hash >> 16) & 0xFF);
            Color targetColor = new Color(colorR, colorG, colorB); //TODO - this could result in mis-clicks due to hash-collision since we ditch a whole byte.
            overlay.MouseMapLookup[targetColor] = sprite;
            handle.SetTransform(DreamViewOverlay.CalculateDrawingMatrix(sprite.TransformToApply, pos, textureSize, renderTargetSize));
            handle.DrawTextureRect(texture, new Box2(Vector2.Zero, textureSize), targetColor);
        }
    }
}
