#version 330

in vec2 fragmentTextureCoord;

uniform sampler2D textureSampler;

out vec4 fragColour;

void main() {
  fragColour = texture2D(textureSampler, fragmentTextureCoord);
}