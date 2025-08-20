using System.Globalization;
using OpenDreamClient.Audio;
using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using OpenDreamShared;
using OpenDreamShared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.WebView;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace OpenDreamClient;

public sealed class EntryPoint : GameClient {
    [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;
    [Dependency] private readonly IDreamResourceManager _dreamResource = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly ParticlesManager _particleManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private const string IEUserAgent =
        "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.2; WOW64; Trident/7.0; .NET4.0C; .NET4.0E; .NET CLR 2.0.50727; .NET CLR 3.0.30729; .NET CLR 3.5.30729)";

    public override void PreInit() {
        var config = IoCManager.Resolve<IConfigurationManager>();

        // We share settings with other RT games, such as SS14.
        // SS14 supports fullscreen, but it breaks us horribly. This disables fullscreen if it's already set.
        config.SetCVar(CVars.DisplayWindowMode, 0);

        config.SetCVar(CVars.RenderTileEdges, false);

        if (config.GetCVar(OpenDreamCVars.SpoofIEUserAgent)) {
            config.OverrideDefault(WCVars.WebUserAgentOverride, IEUserAgent);
        }
    }

    public override void Init() {
        IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
        componentFactory.DoAutoRegistrations();

        ClientContentIoC.Register();

        // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
        foreach (var callback in TestingCallbacks) {
            var cast = (ClientModuleTestingCallbacks)callback;
            cast.ClientBeforeIoC?.Invoke();
        }

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);

        _configurationManager.OverrideDefault(CVars.NetPredict, false);
        _configurationManager.OverrideDefault(CVars.ResAutoScaleEnabled, false); // Fixes weird scaling when sizing windows too small

        IoCManager.Resolve<DreamUserInterfaceStateManager>().Initialize();

        componentFactory.GenerateNetIds();

        _dreamResource.Initialize();

        // Load localization. Needed for some engine texts, such as the ones in Robust ViewVariables.
        IoCManager.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));

        IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
        _particleManager.Initialize(); //TODO remove when particles RT PR is merged
    }

    public override void PostInit() {
        _lightManager.Enabled = false;

        // In PostInit() since the engine stylesheet gets set in Init()
        var uimanager = IoCManager.Resolve<IUserInterfaceManager>();
        uimanager.Stylesheet = DreamStylesheet.Make();
        uimanager.MainViewport.Visible = false;

        _dreamInterface.Initialize();
        IoCManager.Resolve<IDreamSoundEngine>().Initialize();

        _netManager.RegisterNetMessage<MsgAllAppearances>(RxAllAppearances);

        if (_configurationManager.GetCVar(CVars.DisplayCompat))
            _dreamInterface.OpenAlert(
                "Compatibility Mode Warning",
                "You are using compatibility mode. Clicking in-game objects is not supported in this mode.",
                "Ok", null, null, null);
    }

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs) {
        switch (level) {
            case ModUpdateLevel.FramePostEngine:
                _dreamInterface.FrameUpdate(frameEventArgs);
                _particleManager.FrameUpdate(frameEventArgs); //TODO remove when particles RT PR is merged
                break;
            case ModUpdateLevel.PostEngine:
                break;
        }
    }

    private void RxAllAppearances(MsgAllAppearances message) {
        if (!_entitySystemManager.TryGetEntitySystem<ClientAppearanceSystem>(out var clientAppearanceSystem)) {
            Logger.GetSawmill("opendream").Error("Received MsgAllAppearances before initializing entity systems");
            return;
        }

        clientAppearanceSystem.SetAllAppearances(message.AllAppearances);
    }
}
