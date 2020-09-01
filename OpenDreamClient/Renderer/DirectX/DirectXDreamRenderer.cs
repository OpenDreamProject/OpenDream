using Microsoft.Wpf.Interop.DirectX;
using OpenDreamClient.Interface;
using System;
using System.Windows.Media;
using D3D = SharpDX.Direct3D;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System.Windows.Interop;
using System.Windows;
using OpenDreamClient.Dream;
using System.Collections.Concurrent;
using System.Drawing;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;

namespace OpenDreamClient.Renderer.DirectX {
    class DirectXDreamRenderer : IDreamRenderer {
        private D3D11.Device _device;
        private GameWindow _gameWindow;
        private D3D11Image _interopImage;
        private DXGI.SwapChain _swapChain;
        private D3D11.RenderTargetView _renderTargetView;
        private D3D11.Buffer _iconVertexBuffer;
        private D3D11.Buffer _constantBuffer = null;
        private ConcurrentDictionary<(string, Rectangle), DirectXTexture> _textureCache = new ConcurrentDictionary<(string, Rectangle), DirectXTexture>();
        private IntPtr _resourcePointer;
        private ATOM _eye = null;

        public DirectXDreamRenderer(GameWindow gameWindow) {
            _gameWindow = gameWindow;
            DXGI.SwapChainDescription swapChainDescription = new DXGI.SwapChainDescription() {
                OutputHandle = new WindowInteropHelper(_gameWindow).Handle,
                BufferCount = 1,
                Flags = DXGI.SwapChainFlags.AllowModeSwitch,
                IsWindowed = true,
                ModeDescription = new DXGI.ModeDescription(0, 0, new DXGI.Rational(60, 1), DXGI.Format.B8G8R8A8_UNorm),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                SwapEffect = DXGI.SwapEffect.Discard,
                Usage = DXGI.Usage.RenderTargetOutput | DXGI.Usage.Shared
            };

            D3D11.Device.CreateWithSwapChain(D3D.DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport, swapChainDescription, out _device, out _swapChain);

            DirectXShader mainShader = new DirectXShader(_device, "Renderer/DirectX/VertexShader.hlsl", "Renderer/DirectX/PixelShader.hlsl");
            SetShader(mainShader);

            SharpDX.Vector2[] iconVertices = {
                new SharpDX.Vector2(-16.0f, -16.0f),
                new SharpDX.Vector2(0.0f, 1.0f),
                new SharpDX.Vector2(-16.0f, 16.0f),
                new SharpDX.Vector2(0.0f, 0.0f),
                new SharpDX.Vector2(16.0f, 16.0f),
                new SharpDX.Vector2(1.0f, 0.0f),
                new SharpDX.Vector2(16.0f, 16.0f),
                new SharpDX.Vector2(1.0f, 0.0f),
                new SharpDX.Vector2(16.0f, -16.0f),
                new SharpDX.Vector2(1.0f, 1.0f),
                new SharpDX.Vector2(-16.0f, -16.0f),
                new SharpDX.Vector2(0.0f, 1.0f)
            };
            _iconVertexBuffer = D3D11.Buffer.Create(_device, D3D11.BindFlags.VertexBuffer, iconVertices);

            D3D11.SamplerStateDescription samplerDescription = new D3D11.SamplerStateDescription() {
                Filter = D3D11.Filter.MinMagMipLinear,
                MaximumAnisotropy = 0,
                AddressU = D3D11.TextureAddressMode.Border,
                AddressV = D3D11.TextureAddressMode.Border,
                AddressW = D3D11.TextureAddressMode.Border,
                MipLodBias = 0.0f,
                MinimumLod = 0.0f,
                MaximumLod = 0.0f,
                ComparisonFunction = D3D11.Comparison.Never,
                BorderColor = new RawColor4(1.0f, 1.0f, 0.0f, 0.0f)
            };
            _device.ImmediateContext.PixelShader.SetSampler(0, new D3D11.SamplerState(_device, samplerDescription));

            D3D11.BlendStateDescription blendStateDescription = new D3D11.BlendStateDescription();
            blendStateDescription.RenderTarget[0] = new D3D11.RenderTargetBlendDescription() {
                IsBlendEnabled = true,
                SourceBlend = D3D11.BlendOption.SourceAlpha,
                DestinationBlend = D3D11.BlendOption.InverseSourceAlpha,
                BlendOperation = D3D11.BlendOperation.Add,
                SourceAlphaBlend = D3D11.BlendOption.One,
                DestinationAlphaBlend = D3D11.BlendOption.Zero,
                AlphaBlendOperation = D3D11.BlendOperation.Add,
                RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All
            };
            _device.ImmediateContext.OutputMerger.SetBlendState(new D3D11.BlendState(_device, blendStateDescription));

            _interopImage = new D3D11Image();
            _interopImage.WindowOwner = (new WindowInteropHelper(Program.OpenDream.GameWindow)).Handle;
            _interopImage.OnRender = this.DoRender;
            CreateConstantBuffer();
        }

        ~DirectXDreamRenderer() {
            _iconVertexBuffer.Dispose();
            _constantBuffer.Dispose();
            _swapChain.Dispose();
            _device.Dispose();
            _interopImage.Dispose();
        }

        public ImageSource GetImageSource() {
            return _interopImage;
        }

        public void UpdateViewportSize(double width, double height) {
            double dpiScale = 1.0;

            HwndTarget hwndTarget = PresentationSource.FromVisual(_gameWindow).CompositionTarget as HwndTarget;
            if (hwndTarget != null) {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            int surfWidth = (int)(width < 0 ? 0 : Math.Ceiling(width * dpiScale));
            int surfHeight = (int)(height < 0 ? 0 : Math.Ceiling(height * dpiScale));

            _interopImage.SetPixelSize(surfWidth, surfHeight);
            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, (float)_interopImage.Width, (float)_interopImage.Height, 0.0f, 1.0f);
            CreateConstantBuffer();
        }

        public void RenderFrame() {
            _interopImage.RequestRender();
        }

        public IDreamTexture CreateTexture(ResourceDMI dmi, Rectangle rect) {
            if (_textureCache.ContainsKey((dmi.ResourcePath, rect))) {
                return _textureCache[(dmi.ResourcePath, rect)];
            } else {
                DirectXTexture texture = new DirectXTexture(_device, dmi.ImageBitmap, rect.X, rect.Y, rect.Width, rect.Height);

                _textureCache[(dmi.ResourcePath, rect)] = texture;
                return texture;
            }
        }

        public void SetEye(ATOM eye) {
            _eye = eye;
        }

        public System.Drawing.Point GetCameraPosition() {
            return new System.Drawing.Point((_eye != null) ? _eye.X : 0, (_eye != null) ? _eye.Y : 0);
        }

        public void SetShader(DirectXShader shader) {
            _device.ImmediateContext.VertexShader.Set(shader.VertexShader);
            _device.ImmediateContext.PixelShader.Set(shader.PixelShader);
            _device.ImmediateContext.InputAssembler.PrimitiveTopology = D3D.PrimitiveTopology.TriangleList;
            _device.ImmediateContext.InputAssembler.InputLayout = new D3D11.InputLayout(_device, shader.InputSignature, shader.InputElements);
        }

        private void CreateConstantBuffer() {
            SharpDX.Vector2[] constantBufferData = {
                new SharpDX.Vector2((float)_interopImage.Width, (float)_interopImage.Height)
            };

            if (_constantBuffer != null) _constantBuffer.Dispose();
            _constantBuffer = D3D11.Buffer.Create(_device, D3D11.BindFlags.VertexBuffer, constantBufferData);
            _device.ImmediateContext.VertexShader.SetConstantBuffer(0, _constantBuffer);
        }

        private D3D11.Buffer CreatePositionBuffer(float x, float y) {
            SharpDX.Vector2[] positionBufferData = {
                new SharpDX.Vector2(x, y)
            };

            return D3D11.Buffer.Create(_device, D3D11.BindFlags.VertexBuffer, positionBufferData);
        }

        private void DoRender(IntPtr resourcePointer, bool a) {
            if (_resourcePointer != resourcePointer) {
                if (_resourcePointer != IntPtr.Zero) {
                    _renderTargetView.Dispose();
                    _renderTargetView = null;
                }

                _resourcePointer = resourcePointer;
            } else {
                D3D11.Texture2D texture = _renderTargetView.ResourceAs<D3D11.Texture2D>();

                if (texture.Description.Width != (int)_interopImage.Width || texture.Description.Height != (int)_interopImage.Height) {
                    _renderTargetView.Dispose();
                    _renderTargetView = null;
                }
            }

            if (_renderTargetView == null) {
                DXGI.Resource dxgiResource;
                using (SharpDX.ComObject r = new SharpDX.ComObject(resourcePointer)) {
                    dxgiResource = r.QueryInterface<DXGI.Resource>();
                }

                D3D11.Texture2D directx11Texture = _device.OpenSharedResource<D3D11.Texture2D>(dxgiResource.SharedHandle);
                _renderTargetView = new D3D11.RenderTargetView(_device, directx11Texture);
                _device.ImmediateContext.OutputMerger.SetRenderTargets(_renderTargetView);
            }

            _device.ImmediateContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color4(0.0f, 0.0f, 0.0f, 1.0f));
            if (Program.OpenDream.Map != null) {
                Map map = Program.OpenDream.Map;
                System.Drawing.Point cameraPosition = GetCameraPosition();

                for (int x = Math.Max(cameraPosition.X - 8, 0); x < Math.Min(cameraPosition.X + 8, map.Width); x++) {
                    for (int y = Math.Max(cameraPosition.Y - 8, 0); y < Math.Min(cameraPosition.Y + 8, map.Height); y++) {
                        D3D11.Buffer positionBuffer = CreatePositionBuffer((x - cameraPosition.X) * 32.0f, (y - cameraPosition.Y) * 32.0f);
                        ATOM atom = Program.OpenDream.Map.Turfs[x, y];


                        _device.ImmediateContext.VertexShader.SetConstantBuffer(1, positionBuffer);
                        DrawATOM(atom);
                        positionBuffer.Dispose();
                    }
                }
            }

            foreach (ATOM screenObject in Program.OpenDream.ScreenObjects) {
                D3D11.Buffer positionBuffer = CreatePositionBuffer(screenObject.ScreenLocation.X - 224.0f, screenObject.ScreenLocation.Y - 224.0f);

                _device.ImmediateContext.VertexShader.SetConstantBuffer(1, positionBuffer);
                DrawATOM(screenObject);
                positionBuffer.Dispose();
            }

            _device.ImmediateContext.Flush();
        }

        private void DrawATOM(ATOM atom) {
            DrawDreamIcon(atom.Icon);
            foreach (DreamIcon overlayIcon in atom.Icon.Overlays.Values) {
                DrawDreamIcon(overlayIcon);
            }

            if (atom.Type == ATOMType.Turf) {
                foreach (ATOM contentATOM in atom.Contents) {
                    DrawATOM(contentATOM);
                }
            }
        }

        private void DrawDreamIcon(DreamIcon icon) {
            if (icon != null && icon.DreamTexture != null) {
                _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(_iconVertexBuffer, SharpDX.Utilities.SizeOf<SharpDX.Vector2>() * 2, 0));
                _device.ImmediateContext.PixelShader.SetShaderResource(0, ((DirectXTexture)icon.DreamTexture).ShaderResource);
                _device.ImmediateContext.Draw(6, 0);
            }
        }
    }
}
