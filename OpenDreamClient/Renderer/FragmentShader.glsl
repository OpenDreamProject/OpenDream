#version 330

in vec2 fragmentTextureCoord;

uniform sampler2D textureSampler;
uniform vec4 color;

out vec4 fragColor;

void main() {
  fragColor = color * texture2D(textureSampler, fragmentTextureCoord);
}