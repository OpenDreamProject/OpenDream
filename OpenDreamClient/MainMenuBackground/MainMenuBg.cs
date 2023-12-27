using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace OpenDreamClient.MainMenuBackground;

public class MainMenuBg : Control {
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public MainMenuBg() {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(DrawingHandleScreen handle) {
        // TODO prototype of byond games bg on here instead of a const
        var tex = _resourceCache.GetResource<TextureResource>("/OpenDream/layer1.png").Texture;
        var texSize = new Vector2(tex.Size.X * (int) Size.X, tex.Size.Y * (int) Size.X) * 3 / tex.Size.Length;

        var ourSize = PixelSize;
        var currentTime = (float) _timing.RealTime.TotalSeconds;
        var offset =  new Vector2(-MathF.Cos(currentTime * 0.4f), MathF.Sin(currentTime * 0.4f)) * (ourSize * 0.5f);

        var origin = ((ourSize - texSize) / 2) + offset;

        // blur it "slightly"
        handle.UseShader(_prototypeManager.Index<ShaderPrototype>("blur").Instance());
        handle.DrawTextureRect(tex, UIBox2.FromDimensions(origin, texSize));
    }
}
