using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using D3D11 = SharpDX.Direct3D11;

namespace OpenDreamClient.Renderer.DirectX {
    class DirectXTexture : IDreamTexture {
        public D3D11.Texture2D Texture2D;
        public D3D11.ShaderResourceView ShaderResource;

        public DirectXTexture(D3D11.Device _device, Bitmap bitmap, int x, int y, int width, int height) {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(x, y, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            D3D11.Texture2DDescription textureDescription = new D3D11.Texture2DDescription() {
                Width = bitmapData.Width,
                Height = bitmapData.Height,
                ArraySize = 1,
                BindFlags = D3D11.BindFlags.ShaderResource,
                Usage = D3D11.ResourceUsage.Immutable,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = D3D11.ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            };

            Texture2D = new D3D11.Texture2D(_device, textureDescription, new SharpDX.DataRectangle(bitmapData.Scan0, bitmapData.Stride));
            ShaderResource = new D3D11.ShaderResourceView(_device, Texture2D);
            bitmap.UnlockBits(bitmapData);
        }

        ~DirectXTexture() {
            Texture2D.Dispose();
            ShaderResource.Dispose();
        }
    }
}
