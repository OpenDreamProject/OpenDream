using OpenDreamClient.Input;
using OpenDreamClient.Interface.Prompts;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.DebugWindows;

/// <summary>
/// A debug window that displays all the current macro sets and allows you to execute them
/// </summary>
public sealed class MacrosWindow : OSWindow {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private readonly DreamCommandSystem _commandSystem;

    public MacrosWindow() {
        IoCManager.InjectDependencies(this);
        _commandSystem = _entitySystemManager.GetEntitySystem<DreamCommandSystem>();

        Title = "Macros";
        SizeToContent = WindowSizeToContent.WidthAndHeight;

        var tabs = new TabContainer();

        foreach (var macroSet in _interfaceManager.MacroSets.Values) {
            var isCurrent = (macroSet == _interfaceManager.DefaultWindow?.Macro);
            var tabName = macroSet.Id;
            if (isCurrent)
                tabName += " (Current)";

            var macroTable = CreateMacroTable(macroSet);
            TabContainer.SetTabTitle(macroTable, tabName);
            tabs.AddChild(macroTable);

            if (isCurrent)
                tabs.CurrentTab = tabs.ChildCount - 1;
        }

        AddChild(tabs);
    }

    private GridContainer CreateMacroTable(InterfaceMacroSet macroSet) {
        var macroTable = new GridContainer {
            Columns = 3,
            Margin = new(5)
        };

        foreach (var macro in macroSet.Macros.Values) {
            var idText = macro.Id;
            if (macro.ElementDescriptor.Name != idText)
                idText += $" ({macro.ElementDescriptor.Name})";

            var idLabel = new Label { Text = idText };
            var commandLabel = new Label { Text = macro.Command };
            var executeButton = new Button { Text = "Execute" };

            executeButton.OnPressed += _ => {
                if (macro.Command.Contains("[[*]]")) {
                    var prompt = new TextPrompt("Key", "What key?", string.Empty, true, (_, value) => {
                        if (value == null)
                            return; // Cancelled

                        _commandSystem.RunCommand(macro.Command.Replace("[[*]]", (string)value));
                    });

                    prompt.Owner = ClydeWindow;
                    prompt.Show();
                } else {
                    _commandSystem.RunCommand(macro.Command);
                }
            };

            macroTable.AddChild(idLabel);
            macroTable.AddChild(commandLabel);
            macroTable.AddChild(executeButton);
        }

        return macroTable;
    }
}

public sealed class ShowMacrosCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "showmacros";
    public string Description => "Display the current macro sets";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        new MacrosWindow().Show();
    }
}
