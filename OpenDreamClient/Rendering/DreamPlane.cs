namespace OpenDreamClient.Rendering;

internal sealed class DreamPlane {
    public RendererMetaData? Master;

    public readonly List<Action> IconDrawActions = new();
    public readonly List<Action> MouseMapDrawActions = new();

    public void Clear() {
        Master = null;
        IconDrawActions.Clear();
        MouseMapDrawActions.Clear();
    }
}
