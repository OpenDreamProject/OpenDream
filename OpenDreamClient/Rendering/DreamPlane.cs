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
}
