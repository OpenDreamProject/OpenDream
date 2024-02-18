using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane(IRenderTexture mainRenderTarget) : IDisposable {
    public IRenderTexture RenderTarget => _temporaryRenderTarget ?? mainRenderTarget;
    public RendererMetaData? Master;

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
            foreach (var sprite in Sprites) {
                if (sprite.HasRenderSource && overlay.RenderSourceLookup.TryGetValue(sprite.RenderSource!, out var renderSourceTexture)) {
                    sprite.TextureOverride = renderSourceTexture.Texture;
                    overlay.DrawIcon(handle, mainRenderTarget.Size, sprite, (-worldAABB.BottomLeft)-(worldAABB.Size/2)+new Vector2(0.5f,0.5f));
                } else {
                    overlay.DrawIcon(handle, mainRenderTarget.Size, sprite, -worldAABB.BottomLeft);
                }
            }
        }, new Color());

        if (_temporaryRenderTarget != null) {
            // Draw again, but with the color applied
            handle.RenderInRenderTarget(_temporaryRenderTarget, () => {
                handle.UseShader(overlay.GetBlendAndColorShader(Master, useOverlayMode: true));
                handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(_temporaryRenderTarget.Size, Vector2.Zero));
                handle.DrawTextureRect(mainRenderTarget.Texture, new Box2(Vector2.Zero, mainRenderTarget.Size));
            }, new Color());
        }
    }

    /// <summary>
    /// Draws this plane's mouse map onto the current render target
    /// </summary>
    public void DrawMouseMap(DrawingHandleWorld handle, DreamViewOverlay overlay, Vector2i renderTargetSize, Box2 worldAABB) {
        if (Master?.MouseOpacity == MouseOpacity.Transparent)
            return;
        handle.UseShader(overlay.BlockColorInstance);
        foreach (var sprite in Sprites) {
            if (sprite.MouseOpacity == MouseOpacity.Transparent || sprite.ShouldPassMouse)
                continue;

            var texture = sprite.Texture;
            if (texture == null)
                continue;

            var pos = (sprite.Position - worldAABB.BottomLeft) * EyeManager.PixelsPerMeter;
            if (sprite.TextureOverride != null)
                pos -= sprite.TextureOverride.Size / 2 - new Vector2(EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter) / 2;

            int hash = sprite.GetHashCode();
            var colorR = (byte)(hash & 0xFF);
            var colorG = (byte)((hash >> 8) & 0xFF);
            var colorB = (byte)((hash >> 16) & 0xFF);
            Color targetColor = new Color(colorR, colorG, colorB); //TODO - this could result in mis-clicks due to hash-collision since we ditch a whole byte.
            overlay.MouseMapLookup[targetColor] = sprite;

            handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(renderTargetSize, pos));
            handle.DrawTextureRect(texture, new Box2(Vector2.Zero, texture.Size), targetColor);
        }
    }
}
