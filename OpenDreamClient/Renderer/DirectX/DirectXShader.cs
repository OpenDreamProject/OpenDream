using System;
using System.Collections.Generic;
using System.Text;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using D3DCompiler = SharpDX.D3DCompiler;

namespace OpenDreamClient.Renderer.DirectX {
    class DirectXShader {
        public D3D11.VertexShader VertexShader;
        public D3D11.PixelShader PixelShader;
        public D3DCompiler.ShaderSignature InputSignature;
        public D3D11.InputElement[] InputElements = new D3D11.InputElement[] {
            new D3D11.InputElement("POSITION", 0, DXGI.Format.R32G32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
            new D3D11.InputElement("TEXCOORD", 0, DXGI.Format.R32G32_Float, D3D11.InputElement.AppendAligned, 0, D3D11.InputClassification.PerVertexData, 0)
        };
        
        public DirectXShader(D3D11.Device _device, string vertexShaderFilepath, string pixelShaderFilepath) {
            D3DCompiler.CompilationResult vsCompilationResult = D3DCompiler.ShaderBytecode.CompileFromFile(vertexShaderFilepath, "VertexMain", "vs_4_0");
            D3DCompiler.CompilationResult psCompilationResult = D3DCompiler.ShaderBytecode.CompileFromFile(pixelShaderFilepath, "PixelMain", "ps_4_0");

            if (vsCompilationResult.ResultCode.Failure) {
                throw new Exception("Failed to compile vertex shader '" + vertexShaderFilepath + "': " + vsCompilationResult.Message);
            } else if (psCompilationResult.ResultCode.Failure) {
                throw new Exception("Failed to compile pixel shader '" + pixelShaderFilepath + "': " + psCompilationResult.Message);
            }

            VertexShader = new D3D11.VertexShader(_device, vsCompilationResult);
            PixelShader = new D3D11.PixelShader(_device, psCompilationResult);
            InputSignature = D3DCompiler.ShaderSignature.GetInputSignature(vsCompilationResult);
        }

        ~DirectXShader() {
            VertexShader.Dispose();
            PixelShader.Dispose();
        }
    }
}
