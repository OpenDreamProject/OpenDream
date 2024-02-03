using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamShared.Dream;
using Robust.Shared.Timing;

namespace OpenDreamClient.Interface;

/// <summary>
/// Used in unit testing to run a headless client.
/// </summary>
public sealed class DummyDreamInterfaceManager : IDreamInterfaceManager {
    public (string, string, string)[] AvailableVerbs => Array.Empty<(string, string, string)>();
    public Dictionary<string, ControlWindow> Windows { get; } = new();
    public Dictionary<string, InterfaceMenu> Menus { get; } = new();
    public Dictionary<string, InterfaceMacroSet> MacroSets { get; } = new();
    public ControlWindow? DefaultWindow => null;
    public ControlOutput? DefaultOutput => null;
    public ControlInfo? DefaultInfo => null;
    public ControlMap? DefaultMap => null;
    public InterfaceDescriptor InterfaceDescriptor { get; }
    public ViewRange View => new(5);

    public void Initialize() {

    }

    public void FrameUpdate(FrameEventArgs frameEventArgs) {

    }

    public InterfaceElement? FindElementWithId(string id) {
        return null;
    }

    public void SaveScreenshot(bool openDialog) {

    }

    public void LoadInterfaceFromSource(string source) {

    }

    public void WinSet(string? controlId, string winsetParams) {

    }

    public void Prompt(DreamValueType types, string title, string message, string defaultValue, Action<DreamValueType, object?>? onClose) {

    }

    public void RunCommand(string fullCommand) {

    }

    public void StartRepeatingCommand(string command) {

    }

    public void StopRepeatingCommand(string command) {

    }
}
