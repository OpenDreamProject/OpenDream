using System.Linq;
using OpenDreamClient.Rendering;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.DebugWindows;

/// <summary>
/// A debug window that lists all the client's screen objects, and gives the option to view their properties
/// </summary>
[GenerateTypedNameReferences]
internal sealed partial class PlanesDebugWindow : OSWindow {
    private readonly DreamViewOverlay _overlay;

    public PlanesDebugWindow() {
        RobustXamlLoader.Load(this);
        Title = "Planes";

        _overlay = IoCManager.Resolve<IOverlayManager>().GetOverlay<DreamViewOverlay>();

        RefreshButton.OnPressed += OnRefreshPressed;
        Update();
    }

    private void Update() {
        PlanesList.RemoveAllChildren();

        foreach (var (planeId, plane) in _overlay.Planes.OrderBy(v => v.Key)) {
            AddPlane(planeId, plane);
        }
    }

    private void AddPlane(int planeId, DreamPlane plane) {
        var container = new BoxContainer {
            HorizontalExpand = true,
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new(4f, 0f)
        };

        container.AddChild(new TextureRect {
            Texture = plane.RenderTarget.Texture,
            Stretch = TextureRect.StretchMode.KeepAspect,
            SetSize = new(256, 256)
        });

        container.AddChild(new Label {
            Text = planeId.ToString(),
            Align = Label.AlignMode.Center,
            MinWidth = 30
        });

        if (plane.Master?.MainIcon?.Appearance != null) {
            var viewButton = new Button {
                Text = plane.Master.MainIcon.Appearance.Name,
                HorizontalAlignment = HAlignment.Center,
                MinWidth = 92,
                SetHeight = 38
            };

            viewButton.Label.Margin = new(4f, 0f);
            viewButton.OnPressed += _ => {
                if (plane.Master?.MainIcon == null) // We could theoretically lose the master by the time we're clicked
                    return;

                new IconDebugWindow(plane.Master.MainIcon).Show();
            };

            container.AddChild(new Control {
                HorizontalExpand = true,
                Children = { viewButton }
            });
        } else {
            container.AddChild(new Label {
                Text = "No plane master",
                Align = Label.AlignMode.Center,
                HorizontalExpand = true
            });
        }

        var disableButton = new Button {
            Text = plane.Enabled ? "Disable" : "Enable",
            TextAlign = Label.AlignMode.Center,
            HorizontalAlignment = HAlignment.Right,
            SetWidth = 92,
            SetHeight = 38
        };

        disableButton.OnPressed += _ => {
            plane.Enabled = !plane.Enabled;
            disableButton.Text = plane.Enabled ? "Disable" : "Enable";
        };

        container.AddChild(disableButton);
        PlanesList.AddChild(container);
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs e) {
        Update();
    }
}

public sealed class ShowPlanesCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "showplanes";
    public string Description => "Display a list of planes and give the option to view their masters or disable them";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        new PlanesDebugWindow {
            Owner = IoCManager.Resolve<IClyde>().MainWindow
        }.Show();
    }
}
