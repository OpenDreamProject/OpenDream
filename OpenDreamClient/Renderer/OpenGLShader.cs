using SharpGL;
using System;
using System.IO;
using System.Text;

namespace OpenDreamClient.Renderer {
    class OpenGLShader {
        public uint VertexShader, FragmentShader;
        public uint ShaderProgram;
        public uint VertexLocation, TextureCoordLocation;
        public int ViewportSizeUniform, TranslationUniform, TransformUniform,
                    PixelOffsetUniform, TextureSamplerUniform, ColorUniform,
                    IconSizeUniform, RepeatXUniform, RepeatYUniform;

        private OpenGL _gl;

        public OpenGLShader(OpenGL gl, string vertexShaderPath, string fragmentShaderPath) {
            _gl = gl;

            VertexShader = CreateShader(OpenGL.GL_VERTEX_SHADER, vertexShaderPath);
            FragmentShader = CreateShader(OpenGL.GL_FRAGMENT_SHADER, fragmentShaderPath);

            ShaderProgram = _gl.CreateProgram();
            _gl.AttachShader(ShaderProgram, VertexShader);
            _gl.AttachShader(ShaderProgram, FragmentShader);
            _gl.LinkProgram(ShaderProgram);

            VertexLocation = (uint)_gl.GetAttribLocation(ShaderProgram, "vertexPosition");
            TextureCoordLocation = (uint)_gl.GetAttribLocation(ShaderProgram, "textureCoord");
            ViewportSizeUniform = _gl.GetUniformLocation(ShaderProgram, "viewportSize");
            TranslationUniform = _gl.GetUniformLocation(ShaderProgram, "translation");
            TransformUniform = _gl.GetUniformLocation(ShaderProgram, "transform");
            PixelOffsetUniform = _gl.GetUniformLocation(ShaderProgram, "pixelOffset");
            TextureSamplerUniform = _gl.GetUniformLocation(ShaderProgram, "textureSampler");
            ColorUniform = _gl.GetUniformLocation(ShaderProgram, "color");
            IconSizeUniform = _gl.GetUniformLocation(ShaderProgram, "iconSize");
            RepeatXUniform = _gl.GetUniformLocation(ShaderProgram, "repeatX");
            RepeatYUniform = _gl.GetUniformLocation(ShaderProgram, "repeatY");
        }

        public void SetViewportSize(float width, float height) {
            _gl.Uniform2(ViewportSizeUniform, (float)width, (float)height);
        }

        public void SetColor(UInt32 color) {
            float r = (float)((color & 0xFF000000) >> 24) / 255;
            float g = (float)((color & 0xFF0000) >> 16) / 255;
            float b = (float)((color & 0xFF00) >> 8) / 255;
            float a = (float)(color & 0xFF) / 255;

            _gl.Uniform4(ColorUniform, r, g, b, a);
        }

        public void SetTranslation(float x, float y) {
            _gl.Uniform2(TranslationUniform, x, y);
        }

        public void SetTransform(float[] transform) {
            _gl.UniformMatrix3(TransformUniform, 1, true, new float[] {
                transform[0], transform[1], 0,
                transform[2], transform[3], 0,
                transform[4], transform[5], 1
            });
        }

        public void SetPixelOffset(int x, int y) {
            _gl.Uniform2(PixelOffsetUniform, (float)x, (float)y);
        }

        private uint CreateShader(uint shaderType, string shaderFilePath) {
            uint shader = _gl.CreateShader(shaderType);

            _gl.ShaderSource(shader, File.ReadAllText(shaderFilePath));
            _gl.CompileShader(shader);

            int[] shaderParameters = new int[] { 0 };
            _gl.GetShader(shader, OpenGL.GL_COMPILE_STATUS, shaderParameters);
            if (shaderParameters[0] != OpenGL.GL_TRUE) {
                _gl.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, shaderParameters);
                StringBuilder shaderError = new StringBuilder(shaderParameters[0]);

                _gl.GetShaderInfoLog(shader, shaderError.Capacity, IntPtr.Zero, shaderError);
                throw new Exception(shaderError.ToString());
            }

            return shader;
        }
    }
}
