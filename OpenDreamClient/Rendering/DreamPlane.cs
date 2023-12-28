using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane {
    public IRenderTexture RenderTarget => _temporaryRenderTarget ?? _mainRenderTarget;
    public RendererMetaData? Master;

    public readonly List<Action<Vector2i>> IconDrawActions = new();
    public readonly List<Action<Vector2i>> MouseMapDrawActions = new();

    private IRenderTexture _mainRenderTarget;
    private IRenderTexture? _temporaryRenderTarget;

    public DreamPlane(IRenderTexture renderTarget) {
        _mainRenderTarget = renderTarget;
    }

    public void Clear() {
        Master = null;
        IconDrawActions.Clear();
        MouseMapDrawActions.Clear();
        _temporaryRenderTarget = null;
    }

    /// <summary>
    /// Sets this plane's main render target<br/>
    /// Persists through calls to <see cref="Clear()"/>
    /// </summary>
    public void SetMainRenderTarget(IRenderTexture renderTarget) {
        _mainRenderTarget.Dispose();
        _mainRenderTarget = renderTarget;
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
    public void Draw(DreamViewOverlay overlay, DrawingHandleWorld handle) {
        // Draw all icons
        handle.RenderInRenderTarget(_mainRenderTarget, () => {
            foreach (Action<Vector2i> iconAction in IconDrawActions)
                iconAction(_mainRenderTarget.Size);
        }, new Color());

        if (_temporaryRenderTarget != null) {
            // Draw again, but with the color applied
            handle.RenderInRenderTarget(_temporaryRenderTarget, () => {
                handle.UseShader(overlay.GetBlendAndColorShader(Master, useOverlayMode: true));
                handle.SetTransform(overlay.CreateRenderTargetFlipMatrix(_temporaryRenderTarget.Size, Vector2.Zero));
                handle.DrawTextureRect(_mainRenderTarget.Texture, new Box2(Vector2.Zero, _mainRenderTarget.Size));
                handle.SetTransform(Matrix3.Identity);
                handle.UseShader(null);
            }, new Color());
        }
    }

    /// <summary>
    /// Draws this plane's mouse map onto the current render target
    /// </summary>
    public void DrawMouseMap(Vector2i renderTargetSize) {
        foreach (Action<Vector2i> mouseMapAction in MouseMapDrawActions)
            mouseMapAction(renderTargetSize);
    }
}
