using SharpGL;
using System;
using System.IO;
using System.Text;

namespace OpenDreamClient.Renderer {
    class OpenGLShader {
        public uint VertexShader, FragmentShader;
        public uint ShaderProgram;
        public uint VertexLocation, TextureCoordLocation;
        public int ViewportSizeUniform, TranslationUniform, TransformUniform, PixelOffsetUniform, TextureSamplerUniform, ColorUniform;

        public OpenGLShader(OpenGL gl, string vertexShaderPath, string fragmentShaderPath) {
            VertexShader = CreateShader(gl, OpenGL.GL_VERTEX_SHADER, vertexShaderPath);
            FragmentShader = CreateShader(gl, OpenGL.GL_FRAGMENT_SHADER, fragmentShaderPath);

            ShaderProgram = gl.CreateProgram();
            gl.AttachShader(ShaderProgram, VertexShader);
            gl.AttachShader(ShaderProgram, FragmentShader);
            gl.LinkProgram(ShaderProgram);

            VertexLocation = (uint)gl.GetAttribLocation(ShaderProgram, "vertexPosition");
            TextureCoordLocation = (uint)gl.GetAttribLocation(ShaderProgram, "textureCoord");
            ViewportSizeUniform = gl.GetUniformLocation(ShaderProgram, "viewportSize");
            TranslationUniform = gl.GetUniformLocation(ShaderProgram, "translation");
            TransformUniform = gl.GetUniformLocation(ShaderProgram, "transform");
            PixelOffsetUniform = gl.GetUniformLocation(ShaderProgram, "pixelOffset");
            TextureSamplerUniform = gl.GetUniformLocation(ShaderProgram, "textureSampler");
            ColorUniform = gl.GetUniformLocation(ShaderProgram, "color");
        }

        private uint CreateShader(OpenGL gl, uint shaderType, string shaderFilePath) {
            uint shader = gl.CreateShader(shaderType);

            gl.ShaderSource(shader, File.ReadAllText(shaderFilePath));
            gl.CompileShader(shader);

            int[] shaderParameters = new int[] { 0 };
            gl.GetShader(shader, OpenGL.GL_COMPILE_STATUS, shaderParameters);
            if (shaderParameters[0] != OpenGL.GL_TRUE) {
                gl.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, shaderParameters);
                StringBuilder shaderError = new StringBuilder(shaderParameters[0]);

                gl.GetShaderInfoLog(shader, shaderError.Capacity, IntPtr.Zero, shaderError);
                throw new Exception(shaderError.ToString());
            }

            return shader;
        }
    }
}
