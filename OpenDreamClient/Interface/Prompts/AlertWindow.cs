using System;
using OpenDreamShared.Dream.Procs;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace OpenDreamClient.Interface.Prompts
{
    sealed class AlertWindow : PromptWindow
    {
        public AlertWindow(int promptId, String title, String message, String button1, String button2, String button3) :
            base(promptId, title, message)
        {
            CreateButton(button1, true);
            if (!String.IsNullOrEmpty(button2)) CreateButton(button2, false);
            if (!String.IsNullOrEmpty(button3)) CreateButton(button3, false);
        }

        protected override void ButtonClicked(string button)
        {
            FinishPrompt(DMValueType.Text, button);

            base.ButtonClicked(button);
        }
    }

    [UsedImplicitly]
    public sealed class AlertCommand : IConsoleCommand
    {
        public string Command => "alert";
        public string Description { get; }
        public string Help { get; }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = (DreamInterfaceManager)IoCManager.Resolve<IDreamInterfaceManager>();
            mgr.OpenAlert(0, "A", "B", "C");
        }
    }
}
