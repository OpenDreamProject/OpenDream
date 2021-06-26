#version 330

in vec2 vertexPosition;
in vec2 textureCoord;

uniform vec2 viewportSize;
uniform vec2 translation;
uniform vec2 pixelOffset;
uniform mat3 transform;
uniform int iconSize;
uniform int repeatX, repeatY;

out vec2 fragmentTextureCoord;

void main() {
    vec2 position = (vec3(vertexPosition, 1.0) * transform).xy;

    position *= vec2(repeatX, repeatY);
    fragmentTextureCoord = abs(position / iconSize * 2) * textureCoord;
    position += vec2(iconSize * (repeatX - 1) / 2, iconSize * (repeatY - 1) / 2);

    position += translation;
    position += pixelOffset;

    gl_Position = vec4(position / viewportSize * 2, 1.0, 1.0);
}