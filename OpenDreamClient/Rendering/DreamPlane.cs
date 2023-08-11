using Robust.Client.Graphics;

namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane {
    public IRenderTexture RenderTarget => _temporaryRenderTarget ?? _mainRenderTarget;
    public RendererMetaData? Master;

    public readonly List<Action> IconDrawActions = new();
    public readonly List<Action> MouseMapDrawActions = new();

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
        _temporaryRenderTarget?.Dispose();
        _temporaryRenderTarget = renderTarget;
    }

    /// <summary>
    /// Clears this plane's render target, then draws all the plane's icons onto it
    /// </summary>
    public void Draw(DrawingHandleWorld handle) {
        // Draw all icons
        handle.RenderInRenderTarget(_mainRenderTarget, () => {
            foreach (Action iconAction in IconDrawActions)
                iconAction();
        }, new Color());

        if (_temporaryRenderTarget != null) {
            // Copy it over to the secondary render target if we have one
            // We don't just render to it in the first place because this will flip it into the correct orientation
            handle.RenderInRenderTarget(_temporaryRenderTarget, () => {
                handle.DrawTextureRect(_mainRenderTarget.Texture, new(Vector2.Zero, _mainRenderTarget.Size));
            }, new Color());
        }
    }

    /// <summary>
    /// Draws this plane's mouse map onto the current render target
    /// </summary>
    public void DrawMouseMap() {
        foreach (Action mouseMapAction in MouseMapDrawActions)
            mouseMapAction();
    }
}
