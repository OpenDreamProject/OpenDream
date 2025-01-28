using System.Linq;
using OpenDreamClient.Interface.Controls;
using OpenDreamShared.Dream;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.DebugWindows;

/// <summary>
/// A debug window that displays all existing verbs, and all executable verbs
/// </summary>
internal sealed class VerbsWindow : OSWindow {
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public VerbsWindow() {
        IoCManager.InjectDependencies(this);

        Title = "Verbs";
        SizeToContent = WindowSizeToContent.WidthAndHeight;

        var verbSystem = _entitySystemManager.GetEntitySystem<ClientVerbSystem>();
        var tabContainer = new TabContainer {
            Children = { CreateAllVerbsTab(verbSystem), CreateExecutableVerbsTab(verbSystem) }
        };

        AddChild(tabContainer);
    }

    private ScrollContainer CreateAllVerbsTab(ClientVerbSystem verbSystem) {
        var grid = new GridContainer {
            Columns = 3
        };

        foreach (var verbInfo in verbSystem.GetAllVerbs().Order(VerbsWindowNameComparer.Instance)) {
            grid.AddChild(new Label {
                Text = verbInfo.GetCommandName(),
                Margin = new(3)
            });

            grid.AddChild(new Label {
                Text = verbInfo.Name,
                Margin = new(3)
            });

            grid.AddChild(new Label {
                Text = verbInfo.Category,
                Margin = new(3)
            });
        }

        var scroll = new ScrollContainer {
            Children = { grid },
            HScrollEnabled = false,
            MinSize = new Vector2(520, 180)
        };

        TabContainer.SetTabTitle(scroll, "All Verbs");
        return scroll;
    }

    private ScrollContainer CreateExecutableVerbsTab(ClientVerbSystem verbSystem) {
        var grid = new GridContainer {
            Columns = 4
        };

        foreach (var (_, src, verbInfo) in verbSystem.GetExecutableVerbs(true).Order(VerbNameComparer.CultureInstance)) {
            grid.AddChild(new Label {
                Text = verbInfo.GetCommandName(),
                Margin = new(3)
            });

            grid.AddChild(new Label {
                Text = verbInfo.Name,
                Margin = new(3)
            });

            grid.AddChild(new Label {
                Text = verbInfo.Category,
                Margin = new(3)
            });

            grid.AddChild(new Label {
                Text = src.Type.ToString(),
                Margin = new(3)
            });
        }

        var scroll = new ScrollContainer {
            Children = { grid },
            HScrollEnabled = false,
            MinSize = new Vector2(520, 180)
        };

        TabContainer.SetTabTitle(scroll, "Executable Verbs");
        return scroll;
    }
}

public sealed class ShowVerbsCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "showverbs";
    public string Description => "Display the list of existing verbs and list of executable verbs";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        new VerbsWindow().Show();
    }
}

// Verbs are displayed alphabetically
internal sealed class VerbsWindowNameComparer : IComparer<VerbSystem.VerbInfo> {
    public static VerbsWindowNameComparer Instance = new();

    public int Compare(VerbSystem.VerbInfo a, VerbSystem.VerbInfo b) =>
        string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
}
