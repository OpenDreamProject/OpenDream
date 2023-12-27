using System.Globalization;
using OpenDreamClient.Audio;
using OpenDreamClient.Interface;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using OpenDreamClient.States.MainMenu;
using OpenDreamShared;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.WebView;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace OpenDreamClient;

public sealed class EntryPoint : GameClient {
    [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;
    [Dependency] private readonly IDreamResourceManager _dreamResource = default!;
    [Dependency] private readonly IDreamSoundEngine _soundEngine = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ILocalizationManager _contentLoc = default!;

    private const string UserAgent =
        "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.2; WOW64; Trident/7.0; .NET4.0C; .NET4.0E; .NET CLR 2.0.50727; .NET CLR 3.0.30729; .NET CLR 3.5.30729)";

    public override void PreInit() {
        var config = IoCManager.Resolve<IConfigurationManager>();
        if (config.GetCVar(OpenDreamCVars.SpoofIEUserAgent)) {
            config.OverrideDefault(WCVars.WebUserAgentOverride, UserAgent);
        }

        IoCManager.Resolve<IEntitySystemManager>().SystemLoaded += OnEntitySystemLoaded;
    }

    public override void Init() {
        ClientContentIoC.Register();

        // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
        foreach (var callback in TestingCallbacks) {
            var cast = (ClientModuleTestingCallbacks) callback;
            cast.ClientBeforeIoC?.Invoke();
        }

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);

        _componentFactory.DoAutoRegistrations();

        _componentFactory.GenerateNetIds();
        // Load localization. Needed for some engine texts, such as the ones in Robust ViewVariables.
        _contentLoc.LoadCulture(new CultureInfo("en-US"));
        _dreamResource.Initialize();

        _configManager.OverrideDefault(CVars.NetPredict, false);
    }

    public override void PostInit() {
        base.PostInit();
        _lightManager.Enabled = false;

        // In PostInit() since the engine stylesheet gets set in Init()
        _userInterfaceManager.Stylesheet = DreamStylesheet.Make();
        IoCManager.Resolve<DreamUserInterfaceStateManager>().Initialize(); // later hooked for stylesheet
        _stateManager.RequestStateChange<MainMenuState>(); // reset state

        _dreamInterface.Initialize();
        IoCManager.Resolve<IDreamSoundEngine>().Initialize();
    }

    protected override void Dispose(bool disposing) {
        _dreamResource.Shutdown();
    }

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs) {
        switch (level) {
            case ModUpdateLevel.FramePostEngine:
                _dreamInterface.FrameUpdate(frameEventArgs);
                break;
            case ModUpdateLevel.PostEngine:
                _soundEngine.StopFinishedChannels();
                break;
        }
    }

    // As of RobustToolbox v0.90.0.0 there's a TileEdgeOverlay that breaks our rendering
    // because we don't have an ITileDefinition for each tile.
    // This removes that overlay immediately after MapSystem adds it.
    // TODO: Fix this engine-side
    private void OnEntitySystemLoaded(object? sender, SystemChangedArgs e) {
        if (e.System is not MapSystem)
            return;

        _overlayManager.RemoveOverlay<TileEdgeOverlay>();
    }
}
