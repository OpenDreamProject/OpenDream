using OpenDreamClient.Dream;
using OpenDreamShared.Dream;
using SharpGL;
using SharpGL.WPF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;

namespace OpenDreamClient.Renderer {
    class DreamRenderer {
        public OpenGLControl OpenGLViewControl;

        private OpenGL _gl;
        private OpenGLShader _shader;
        private uint _iconVerticesBuffer;
        private uint _iconTextureCoordBuffer;

        private static Dictionary<(string, Rectangle), DreamTexture> _textureCache = new Dictionary<(string, Rectangle), DreamTexture>();

        public DreamRenderer() {
            OpenGLViewControl = new OpenGLControl();
            OpenGLViewControl.RenderContextType = RenderContextType.FBO;
            OpenGLViewControl.HorizontalAlignment = HorizontalAlignment.Center;
            OpenGLViewControl.VerticalAlignment = VerticalAlignment.Center;
            OpenGLViewControl.OpenGLDraw += RenderFrame;
            OpenGLViewControl.OpenGLInitialized += InitOpenGL;
        }

        ~DreamRenderer() {
            
        }

        public void SetViewportSize(int width, int height) {
            OpenGLViewControl.Width = width;
            OpenGLViewControl.Height = height;
            OpenGLViewControl.OpenGL.Viewport(0, 0, width, height);
            _gl.Uniform2(_shader.ViewportSizeUniform, (float)width, (float)height);
        }

        public (int, int) GetCameraPosition() {
            ATOM eye = Program.OpenDream.Eye;

            if (eye != null) {
                return (eye.X, eye.Y);
            } else {
                return (0, 0);
            }
        }

        private void InitOpenGL(object sender, OpenGLRoutedEventArgs args) {
            _gl = args.OpenGL;
            _shader = new OpenGLShader(_gl, "Renderer/VertexShader.glsl", "Renderer/FragmentShader.glsl");

            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
            _gl.Enable(OpenGL.GL_BLEND);
            _gl.Enable(OpenGL.GL_DEPTH_TEST);
            _gl.UseProgram(_shader.ShaderProgram);

            uint[] buffers = new uint[] { 0, 0 };
            _gl.GenBuffers(2, buffers);
            _iconVerticesBuffer = buffers[0];
            _iconTextureCoordBuffer = buffers[1];

            _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconVerticesBuffer);
            _gl.BufferData(OpenGL.GL_ARRAY_BUFFER, new float[] {
                -16.0f, -16.0f,
                16.0f, 16.0f,
                16.0f, -16.0f,

                -16.0f, -16.0f,
                -16.0f, 16.0f,
                16.0f, 16.0f
            }, OpenGL.GL_STATIC_DRAW);
            _gl.EnableVertexAttribArray(_shader.VertexLocation);

            _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconTextureCoordBuffer);
            _gl.BufferData(OpenGL.GL_ARRAY_BUFFER, new float[] {
                0.0f, 1.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,

                0.0f, 1.0f,
                0.0f, 0.0f,
                1.0f, 0.0f
            }, OpenGL.GL_STATIC_DRAW);
            _gl.EnableVertexAttribArray(_shader.TextureCoordLocation);

            _gl.Uniform1(_shader.LayerUniform, 1.0f); //TODO: Use atom icons' layers
        }

        private DreamTexture GetDreamTexture(DreamIcon icon) {
            DreamTexture texture = null;
            
            if (icon != null && icon.DMI != null && icon.DMI.Description.States.ContainsKey(icon.VisualProperties.IconState)) {
                Rectangle textureRect = icon.DMI.GetTextureRect(icon.VisualProperties.IconState, icon.VisualProperties.Direction, icon.GetCurrentAnimationFrame());

                if (!_textureCache.TryGetValue((icon.DMI.ResourcePath, textureRect), out texture)) {
                    texture = new DreamTexture(_gl, icon.DMI, textureRect);

                    _textureCache.Add((icon.DMI.ResourcePath, textureRect), texture);
                }
            }

            return texture;
        }

        private void RenderFrame(object sender, OpenGLRoutedEventArgs args) {
            _gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            
            if (Program.OpenDream.Map != null) {
                Map map = Program.OpenDream.Map;
                (int, int) cameraPosition = GetCameraPosition();
                
                for (int x = Math.Max(cameraPosition.Item1 - 8, 0); x < Math.Min(cameraPosition.Item1 + 8, map.Width); x++) {
                    for (int y = Math.Max(cameraPosition.Item2 - 8, 0); y < Math.Min(cameraPosition.Item2 + 8, map.Height); y++) {
                        ATOM atom = Program.OpenDream.Map.Turfs[x, y];

                        _gl.Uniform2(_shader.TranslationUniform, (-cameraPosition.Item1 + x) * 32.0f, (-cameraPosition.Item2 + y) * 32.0f);
                        DrawATOM(atom);
                    }
                }
            }
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
            DreamTexture texture = GetDreamTexture(icon);

            if (texture != null) {
                _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconVerticesBuffer);
                _gl.VertexAttribPointer(_shader.VertexLocation, 2, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconTextureCoordBuffer);
                _gl.VertexAttribPointer(_shader.TextureCoordLocation, 2, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                _gl.ActiveTexture(OpenGL.GL_TEXTURE0);
                _gl.BindTexture(OpenGL.GL_TEXTURE_2D, texture.TextureID);
                _gl.Uniform1(_shader.TextureSamplerUniform, 0);
                _gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 6);
            }
        }
    }
}
