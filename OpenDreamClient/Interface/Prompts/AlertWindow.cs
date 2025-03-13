using JetBrains.Annotations;
using OpenDreamShared.Common.DM;
using OpenDreamShared.Dream;
using Robust.Shared.Console;

namespace OpenDreamClient.Interface.Prompts;

internal sealed class AlertWindow : PromptWindow {
    public AlertWindow(string title, string message, string button1, string? button2, string? button3, Action<DMValueType, object?>? onClose) :
        base(title, message, onClose) {
        CreateButton(button1, true);
        if (!string.IsNullOrEmpty(button2)) CreateButton(button2, false);
        if (!string.IsNullOrEmpty(button3)) CreateButton(button3, false);
    }

    protected override void ButtonClicked(string button) {
        FinishPrompt(DMValueType.Text, button);

        base.ButtonClicked(button);
    }
}

[UsedImplicitly]
public sealed class AlertCommand : IConsoleCommand {
    public string Command => "alert";
    public string Description => "Opens a test alert";
    public string Help => "alert";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        var mgr = (DreamInterfaceManager)IoCManager.Resolve<IDreamInterfaceManager>();
        mgr.OpenAlert("A", "B", "C", null, null, null);
    }
}
