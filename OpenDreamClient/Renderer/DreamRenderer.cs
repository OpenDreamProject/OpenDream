﻿using OpenDreamClient.Dream;
using SharpGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;

namespace OpenDreamClient.Renderer {
    class DreamRenderer {
        public SharpGLControl OpenGLViewControl;

        public int CameraX {
            get => Program.OpenDream.Eye?.X ?? 0;
        }

        public int CameraY {
            get => Program.OpenDream.Eye?.Y ?? 0;
        }
        
        public int CameraZ {
            get => Program.OpenDream.Eye?.Z ?? 0;
        }

        private OpenGL _gl;
        private OpenGLShader _shader;
        private uint _iconVerticesBuffer;
        private uint _iconTextureCoordBuffer;

        private static Dictionary<(string, Rectangle), DreamTexture> _textureCache = new();

        public DreamRenderer() {
            OpenGLViewControl = new SharpGLControl(480, 480);
            OpenGLViewControl.HorizontalAlignment = HorizontalAlignment.Center;
            OpenGLViewControl.VerticalAlignment = VerticalAlignment.Center;
            OpenGLViewControl.Render += RenderFrame;
            OpenGLViewControl.OpenGLContextCreated += InitOpenGL;
        }

        public void StopRendering() {
            OpenGLViewControl.Render -= RenderFrame;
        }

        public void SetViewportSize(int width, int height) {
            OpenGLViewControl.Width = width;
            OpenGLViewControl.Height = height;
            _gl.Viewport(0, 0, width, height);
            _shader.SetViewportSize((float)width, (float)height);
        }

        public Rectangle GetIconRect(ATOM atom, bool useScreenLocation) {
            System.Drawing.Point position;
            if (useScreenLocation) {
                position = atom.ScreenLocation.GetScreenCoordinates(32);
            }  else {
                int tileX = atom.X - CameraX + 7;
                int tileY = atom.Y - CameraY + 7;

                position = new System.Drawing.Point(tileX * 32 + atom.Icon.Appearance.PixelX, tileY * 32 + atom.Icon.Appearance.PixelY);
            }

            return new Rectangle(position, new System.Drawing.Size(32, 32));
        }

        public bool IsAtomVisible(ATOM atom, bool useScreenLocation) {
            Rectangle iconRect = GetIconRect(atom, useScreenLocation);

            if (atom.Icon.Appearance.Invisibility > 0) return false; //0 is the default invisibility a mob can see

            return (iconRect.X >= -iconRect.Width && iconRect.X <= OpenGLViewControl.Width &&
                    iconRect.Y >= -iconRect.Height && iconRect.Y <= OpenGLViewControl.Height);
        }

        private void InitOpenGL(OpenGL gl) {
            _gl = gl;
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

            SetViewportSize(480, 480);
        }

        private DreamTexture GetDreamTexture(DreamIcon icon) {
            DreamTexture texture = null;
            
            if (icon != null && icon.IsValidIcon()) {
                Rectangle textureRect = icon.GetTextureRect();

                if (!_textureCache.TryGetValue((icon.DMI.ResourcePath, textureRect), out texture)) {
                    texture = new DreamTexture(_gl, icon.DMI, textureRect);

                    _textureCache.Add((icon.DMI.ResourcePath, textureRect), texture);
                }
            }

            return texture;
        }

        private void RenderFrame(OpenGL gl) {
            _gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            if (Program.OpenDream.Map != null) {
                List<ATOM> turfs = Program.OpenDream.Map.GetTurfs(CameraX - 8, CameraY - 8, CameraZ, 16, 16);
                List<ATOM> mapAtoms = new();

                foreach (ATOM turf in turfs) {
                    if (IsAtomVisible(turf, false)) {
                        mapAtoms.Add(turf);
                        mapAtoms.AddRange(turf.Contents);
                    }
                }

                DrawAtoms(mapAtoms, false);
            }

            DrawAtoms(Program.OpenDream.ScreenObjects, true);
        }

        private void DrawAtoms(List<ATOM> atoms, bool useScreenLocation) {
            //Sort by layer
            atoms.Sort(
                new Comparison<ATOM>((ATOM first, ATOM second) => {
                    int layerSort = DreamIcon.LayerSort(first.Icon, second.Icon);

                    if (layerSort == 0) return (int)first.ID - (int)second.ID; //Sort by ID instead
                    else return layerSort;
                })
            );

            foreach (ATOM atom in atoms) {
                if (useScreenLocation) {
                    System.Drawing.Point screenCoordinates = atom.ScreenLocation.GetScreenCoordinates(32);

                    _shader.SetTranslation((float)screenCoordinates.X - (32 * 7), (float)screenCoordinates.Y - (32 * 7));
                } else {
                    _shader.SetTranslation((atom.X - CameraX) * 32.0f, (atom.Y - CameraY) * 32.0f);
                }

                if (IsAtomVisible(atom, useScreenLocation)) DrawDreamIcon(atom.Icon, !useScreenLocation);
            }
        }

        private void DrawDreamIcon(DreamIcon icon, bool usePixelOffsets, int pixelX = 0, int pixelY = 0, float[] transform = null) {
            DreamTexture texture = GetDreamTexture(icon);

            transform ??= icon.Appearance.Transform;
            if (usePixelOffsets) {
                pixelX += icon.Appearance.PixelX;
                pixelY += icon.Appearance.PixelY;
            }

            foreach (DreamIcon underlay in icon.Underlays) {
                DrawDreamIcon(underlay, usePixelOffsets, pixelX, pixelY, transform);
            }

            if (texture != null) {
                _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconVerticesBuffer);
                _gl.VertexAttribPointer(_shader.VertexLocation, 2, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                _shader.SetTransform(transform);
                _shader.SetPixelOffset(pixelX, pixelY);
                _gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _iconTextureCoordBuffer);
                _gl.VertexAttribPointer(_shader.TextureCoordLocation, 2, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                _gl.ActiveTexture(OpenGL.GL_TEXTURE0);
                _gl.BindTexture(OpenGL.GL_TEXTURE_2D, texture.TextureID);
                _gl.Uniform1(_shader.TextureSamplerUniform, 0);
                _shader.SetColor(icon.Appearance.Color);
                _gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 6);
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                DrawDreamIcon(overlay, usePixelOffsets, pixelX, pixelY, transform);
            }
        }
    }
}
