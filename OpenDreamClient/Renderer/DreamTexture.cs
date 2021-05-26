using OpenDreamClient.Resources.ResourceTypes;
using SharpGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenDreamClient.Renderer {
    class DreamTexture {
        public uint TextureID;

        private OpenGL _gl;

        public DreamTexture(OpenGL gl, ResourceDMI dmi, Rectangle textureRect) {
            _gl = gl;

            uint[] textureIDs = new uint[] { 0 };
            _gl.GenTextures(1, textureIDs);
            TextureID = textureIDs[0];

            BitmapData bitmapData = dmi.ImageBitmap.LockBits(textureRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            {
                int stride = bitmapData.Width * 4;
                byte[] bytes = new byte[stride * bitmapData.Height];

                for (int y = 0; y < bitmapData.Height; y++) {
                    Marshal.Copy(bitmapData.Scan0 + (bitmapData.Stride * y), bytes, stride * y, stride);
                }

                _gl.BindTexture(OpenGL.GL_TEXTURE_2D, TextureID);
                _gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, textureRect.Width, textureRect.Height, 0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_INT_8_8_8_8_REV, bytes);

                uint[] texParameters = new uint[] { OpenGL.GL_NEAREST };
                _gl.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, texParameters);
                _gl.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, texParameters);
            }
            dmi.ImageBitmap.UnlockBits(bitmapData);
        }

        ~DreamTexture() {
            _gl.DeleteTextures(1, new uint[] { TextureID });
        }
    }
}
