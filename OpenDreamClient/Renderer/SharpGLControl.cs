using SharpGL;
using SharpGL.RenderContextProviders;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenDreamClient.Renderer {
    //SharpGL.WPF contains an OpenGLControl that does this for us
    //However, it calls GC.Collect() at the end of every frame
    //This is a HUGE hit to performance, so we use our own modified version
    class SharpGLControl : UserControl {
        public delegate void OpenGLContextCreatedDelegate(OpenGL gl);
        public delegate void RenderDelegate(OpenGL gl);

        public event OpenGLContextCreatedDelegate OpenGLContextCreated;
        public event RenderDelegate Render;

        public OpenGL GL = new OpenGL();

        private DispatcherTimer _renderTimer = new DispatcherTimer(DispatcherPriority.Render);
        private int _width, _height;
        private Image _image = new Image();

        public SharpGLControl(int width, int height) {
            _renderTimer.Interval = TimeSpan.FromMilliseconds(1000 / 60); //60 FPS
            _renderTimer.Tick += _renderTimer_Tick;
            _width = width;
            _height = height;

            Content = _image;
            RenderSize = new Size(_width, _height);

            Loaded += SharpGLControl_Loaded;
            Unloaded += SharpGLControl_Unloaded;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            GL.SetDimensions(_width, _height);
            GL.Create(SharpGL.Version.OpenGLVersion.OpenGL2_1, RenderContextType.FBO, _width, _height, 32, null);
            GL.Viewport(0, 0, _width, _height);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.DepthFunc(OpenGL.GL_LEQUAL);

            if (OpenGLContextCreated != null) OpenGLContextCreated.Invoke(GL);
        }

        private static BitmapSource HBitmapToBitmapSource(IntPtr hBitmap) {
            BitmapSource bitSrc = null;

            try {
                if (hBitmap != IntPtr.Zero) {
                    bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            } catch (Win32Exception) {
                bitSrc = null;
            } finally {
                Win32.DeleteObject(hBitmap);
                //GC.Collect(); TODO: Find an alternatice to calling GC.Collect()
            }

            return bitSrc;
        }

        private void UpdateImage() {
            FBORenderContextProvider provider = GL.RenderContextProvider as FBORenderContextProvider;
            IntPtr hBitmap = provider.InternalDIBSection.HBitmap;

            if (hBitmap != IntPtr.Zero) {
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = HBitmapToBitmapSource(hBitmap);
                formattedBitmap.DestinationFormat = PixelFormats.Rgb24;
                formattedBitmap.EndInit();

                _image.Source = formattedBitmap;
            }
        }

        private void _renderTimer_Tick(object sender, EventArgs e) {
            GL.MakeCurrent();

            if (Render != null) Render.Invoke(GL);

            GL.Blit(IntPtr.Zero);
            UpdateImage();
        }

        private void SharpGLControl_Loaded(object sender, RoutedEventArgs e) {
            _renderTimer.Start();
        }

        private void SharpGLControl_Unloaded(object sender, RoutedEventArgs e) {
            _renderTimer.Stop();
        }
    }
}
