using Robust.Client.Graphics;

namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane {
    public IRenderTexture RenderTarget;
    public RendererMetaData? Master;

    public readonly List<Action> IconDrawActions = new();
    public readonly List<Action> MouseMapDrawActions = new();

    public DreamPlane(IRenderTexture renderTarget) {
        RenderTarget = renderTarget;
    }

    public void Clear() {
        Master = null;
        IconDrawActions.Clear();
        MouseMapDrawActions.Clear();
    }

    /// <summary>
    /// Clears this plane's render target, then draws all the plane's icons onto it
    /// </summary>
    public void Draw(DrawingHandleWorld handle) {
        // Draw all icons
        handle.RenderInRenderTarget(RenderTarget, () => {
            foreach (Action iconAction in IconDrawActions)
                iconAction();
        }, new Color());
    }

    /// <summary>
    /// Draws this plane's mouse map onto the current render target
    /// </summary>
    public void DrawMouseMap() {
        foreach (Action mouseMapAction in MouseMapDrawActions)
            mouseMapAction();
    }
}
