using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

public sealed class RenderTargetPool(IClyde clyde) {
    private static readonly TextureSampleParameters SrgbEncodedSampling = new() { Filter = true };

    private readonly Dictionary<(Vector2i Size, bool SrgbEncoded), List<IRenderTexture>> _renderTargets = new();
    private readonly HashSet<IRenderTexture> _srgbEncodedTargets = new();
    private readonly Stack<IRenderTexture> _renderTargetsToReturn = new();

    /// <param name="srgbEncoded">Rent a non-sRGB target with linear filtering instead of an sRGB one</param>
    public IRenderTexture Rent(Vector2i size, bool srgbEncoded = false) {
        if (_renderTargets.TryGetValue((size, srgbEncoded), out var list) && list.Count > 0)
            return list.Pop();

        if (!srgbEncoded)
            return clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));

        var target = clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8), SrgbEncodedSampling);
        _srgbEncodedTargets.Add(target);
        return target;
    }

    public void ReturnAtEndOfFrame(IRenderTexture rental) {
        _renderTargetsToReturn.Push(rental);
    }

    public void Return(IRenderTexture rental) {
        var key = (rental.Size, _srgbEncodedTargets.Contains(rental));
        if (!_renderTargets.TryGetValue(key, out var storeList)) {
            storeList = new List<IRenderTexture>(4);
            _renderTargets.Add(key, storeList);
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
