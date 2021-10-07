using JetBrains.Annotations;
using Robust.Client.CEF;

namespace OpenDreamClient.Interface
{
    // Used for headless unit testing
    [UsedImplicitly]
    public class DummyCefManager : ICefManager
    {
        public void Initialize()
        {

        }

        public void CheckInitialized()
        {

        }

        public void Update()
        {

        }

        public void Shutdown()
        {

        }

        public IBrowserWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            return null;
        }
    }
}
