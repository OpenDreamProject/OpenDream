using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

public sealed class RenderTargetPool(IClyde clyde) {
    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargets = new();
    private readonly Stack<IRenderTexture> _renderTargetsToReturn = new();

    public IRenderTexture Rent(Vector2i size) {
        IRenderTexture result;

        if (!_renderTargets.TryGetValue(size, out var listResult)) {
            result = clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
        } else {
            result = listResult.Count > 0
                ? listResult.Pop()
                : clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
        }

        return result;
    }

    public void ReturnAtEndOfFrame(IRenderTexture rental) {
        _renderTargetsToReturn.Push(rental);
    }

    public void Return(IRenderTexture rental) {
        if (!_renderTargets.TryGetValue(rental.Size, out var storeList)) {
            storeList = new List<IRenderTexture>(4);
            _renderTargets.Add(rental.Size, storeList);
        }

        storeList.Add(rental);
    }

    [Access(typeof(DreamViewOverlay))]
    public void HandleEndOfFrame() {
        //some render targets need to be kept until the end of the render cycle, so return them here.
        while (_renderTargetsToReturn.TryPop(out var toReturn))
            Return(toReturn);
    }
}
