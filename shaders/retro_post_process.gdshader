shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform float pixel_size : hint_range(1.0, 16.0) = 4.0;
uniform float color_levels : hint_range(2.0, 32.0) = 8.0;
uniform float scanline_strength : hint_range(0.0, 1.0) = 0.12;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.08;
uniform float brightness : hint_range(0.5, 2.0) = 1.25;
uniform float shadow_lift : hint_range(0.0, 0.5) = 0.08;
uniform float exposure : hint_range(0.25, 3.0) = 1.15;
uniform float contrast : hint_range(0.5, 2.0) = 1.08;
uniform float saturation : hint_range(0.0, 2.0) = 1.12;
uniform vec3 color_tint = vec3(1.04, 1.0, 0.92);

void fragment() {
    vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE;

    vec2 pixel_uv = floor(SCREEN_UV * screen_size / pixel_size) * pixel_size / screen_size;
    vec3 color = texture(screen_texture, pixel_uv).rgb;

    // Lift the scene before posterization so dark colors do not collapse to black.
    color = color * brightness + shadow_lift * (1.0 - color);

    // Color grade the lighting before posterization.
    color *= exposure;
    color = (color - 0.5) * contrast + 0.5;
    float luma = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(vec3(luma), color, saturation);
    color *= color_tint;

    // Posterize colors.
    color = round(color * (color_levels - 1.0)) / (color_levels - 1.0);

    // Subtle scanlines.
    float scanline = 0.5 + 0.5 * sin(SCREEN_UV.y * screen_size.y * 3.14159);
    color *= 1.0 - scanline_strength * (1.0 - scanline);

    // Vignette.
    float dist = distance(SCREEN_UV, vec2(0.5));
    color *= 1.0 - smoothstep(0.35, 0.8, dist) * vignette_strength;

    COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
}
