using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using Robust.Shared.Timing;

namespace OpenDreamClient.Interface {
    /// <summary>
    /// Used in unit testing to run a headless client.
    /// </summary>
    public sealed class DummyDreamInterfaceManager : IDreamInterfaceManager {
        public (string, string, string)[] AvailableVerbs { get; }
        public Dictionary<string, ControlWindow> Windows { get; }
        public Dictionary<string, InterfaceMenu> Menus { get; }
        public InterfaceDescriptor InterfaceDescriptor { get; }

        public void Initialize()
        {

        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {

        }

        public InterfaceElement FindElementWithName(string name)
        {
            return null;
        }

        public void SaveScreenshot(bool openDialog)
        {

        }

        public void LoadInterfaceFromSource(string source)
        {

        }

        public void WinSet(string controlId, string winsetParams) {

        }
    }
}
