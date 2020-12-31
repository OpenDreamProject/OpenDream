#version 330

in vec2 vertexPosition;
in vec2 textureCoord;

uniform vec2 viewportSize;
uniform vec2 translation;
uniform vec2 pixelOffset;

out vec2 fragmentTextureCoord;

void main() {
    vec2 position = vertexPosition + translation + pixelOffset;

    gl_Position = vec4(position / viewportSize * 2, 1.0, 1.0);
    fragmentTextureCoord = textureCoord;
}