using OpenDreamClient.Rendering;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.DebugWindows;

/// <summary>
/// A debug window that lists all the client's screen objects, and gives the option to view their properties
/// </summary>
[GenerateTypedNameReferences]
internal sealed partial class ScreenObjectsDebugWindow : OSWindow {
    private readonly ClientScreenOverlaySystem _screenOverlaySystem;
    private readonly IEntityManager _entityManager;

    public ScreenObjectsDebugWindow() {
        RobustXamlLoader.Load(this);
        Title = "Screen Objects";

        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        _screenOverlaySystem = entitySystemManager.GetEntitySystem<ClientScreenOverlaySystem>();
        _entityManager = IoCManager.Resolve<IEntityManager>();

        RefreshButton.OnPressed += OnRefreshPressed;
        Update();
    }

    private void Update() {
        ScreenObjectsList.RemoveAllChildren();

        foreach (var screenObjectEntity in _screenOverlaySystem.ScreenObjects) {
            if (!_entityManager.TryGetComponent(screenObjectEntity, out DMISpriteComponent? screenObject))
                continue;

            AddScreenObject(screenObject);
        }
    }

    private void AddScreenObject(DMISpriteComponent screenObject) {
        var container = new BoxContainer {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new(4f, 0f)
        };

        container.AddChild(new TextureRect {
            Texture = screenObject.Icon.LastRenderedTexture,
            Stretch = TextureRect.StretchMode.KeepAspect,
            SetSize = new(128, 128)
        });

        container.AddChild(new Label {
            Text = screenObject.Icon.Appearance?.Name ?? "<No appearance>",
            HorizontalExpand = true
        });

        container.AddChild(new Label {
            Text = screenObject.ScreenLocation?.ToString() ?? "<No screen_loc>",
            HorizontalExpand = true
        });

        var viewButton = new Button {
            Text = "View",
            TextAlign = Label.AlignMode.Center,
            MinWidth = 92,
            MaxHeight = 38
        };

        viewButton.OnPressed += _ => {
            new IconDebugWindow(screenObject.Icon).Show();
        };

        container.AddChild(viewButton);
        ScreenObjectsList.AddChild(container);
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs e) {
        Update();
    }
}

public sealed class ShowScreenObjectsCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "showscreenobjects";
    public string Description => "Display a list of screen objects and give the option to view their properties";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        new ScreenObjectsDebugWindow {
            Owner = IoCManager.Resolve<IClyde>().MainWindow
        }.Show();
    }
}
