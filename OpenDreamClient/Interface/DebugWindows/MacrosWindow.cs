using OpenDreamClient.Interface.Prompts;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.DebugWindows;

/// <summary>
/// A debug window that displays all the current macro sets and allows you to execute them
/// </summary>
internal sealed class MacrosWindow : OSWindow {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

    public MacrosWindow() {
        IoCManager.InjectDependencies(this);

        Title = "Macros";
        SizeToContent = WindowSizeToContent.WidthAndHeight;

        var tabs = new TabContainer();

        foreach (var macroSet in _interfaceManager.MacroSets.Values) {
            var isCurrent = (macroSet == _interfaceManager.DefaultWindow?.Macro);
            var tabName = macroSet.Id;
            if (isCurrent)
                tabName.Value += " (Current)";

            var macroTable = CreateMacroTable(macroSet);
            TabContainer.SetTabTitle(macroTable, tabName.AsRaw());
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
            if (macro.ElementDescriptor.Id.Value != idText.Value)
                idText.Value += $" ({macro.ElementDescriptor.Id.AsRaw()})";

            var idLabel = new Label { Text = idText.AsRaw() };
            var commandLabel = new Label { Text = macro.Command };
            var executeButton = new Button { Text = "Execute" };

            executeButton.OnPressed += _ => {
                if (macro.Command.Contains("[[*]]")) {
                    var prompt = new TextPrompt("Key", "What key?", string.Empty, true, (_, value) => {
                        if (value == null)
                            return; // Cancelled

                        _interfaceManager.RunCommand(macro.Command.Replace("[[*]]", (string)value));
                    });

                    prompt.Owner = ClydeWindow;
                    prompt.Show();
                } else {
                    _interfaceManager.RunCommand(macro.Command);
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
