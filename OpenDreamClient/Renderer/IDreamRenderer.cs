using OpenDreamClient.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Drawing;
using OpenDreamClient.Resources.ResourceTypes;

namespace OpenDreamClient.Renderer {
    interface IDreamRenderer {
        public ImageSource GetImageSource();
        public void UpdateViewportSize(double width, double height);
        public void RenderFrame();
        public IDreamTexture CreateTexture(ResourceDMI dmi, Rectangle rect);
        public void SetEye(ATOM eye);
        public Point GetCameraPosition();
    }
}
