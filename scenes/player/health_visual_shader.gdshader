shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float hit_flash : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec2 uv = UV * (1.0 - UV.xy);
    float vignette = pow(uv.x * uv.y * 15.0, mix(1.0, 0.1, intensity));
    float edge = 1.0 - vignette;
    COLOR = vec4(1.0, 0.0, 0.0, edge * intensity * 0.8 + hit_flash * 0.25);
}