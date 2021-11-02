using JetBrains.Annotations;
using Robust.Client.WebView;

namespace OpenDreamClient.Interface
{
    // Used for headless unit testing
    [UsedImplicitly]
    public class DummyCefManager : IWebViewManager
    {
        public IWebViewWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            return null;
        }
    }
}
