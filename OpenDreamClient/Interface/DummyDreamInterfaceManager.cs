using OpenDreamShared.Interface;
using Robust.Shared.Timing;

namespace OpenDreamClient.Interface
{
    public class DummyDreamInterfaceManager : IDreamInterfaceManager
    {
        public string[] AvailableVerbs { get; }
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
    }
}
