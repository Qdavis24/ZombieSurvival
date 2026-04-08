shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always;

uniform sampler2D noise_texture_1 : source_color;
uniform sampler2D noise_texture_2 : source_color;

uniform float noise_blend : hint_range(0.0, 1.0) = 0.5;
uniform float noise_speed_1 : hint_range(0.0, 2.0) = 0.4;
uniform float noise_speed_2 : hint_range(0.0, 2.0) = 0.25;

uniform vec2 noise_dir_1 = vec2(1.0, 1.0);
uniform vec2 noise_dir_2 = vec2(1.0, -1.0);

uniform float noise_scale_1 = 0.1;
uniform float noise_scale_2 = 0.1;

uniform vec3 base_color : source_color = vec3(0, 0, 0);
uniform vec3 smoke_color : source_color = vec3(0.01, 0.01, 0.01);

// NEW controls
uniform float noise_power : hint_range(0.5, 5.0) = 2.5;
uniform float noise_darkness : hint_range(0.0, 1.0) = 0.3;

uniform float alpha_strength : hint_range(0.0, 1.0) = 0.99;

void fragment()
{
    vec2 uv1 = UV * noise_scale_1 + TIME * noise_dir_1 * noise_speed_1 * 0.1;
    vec2 uv2 = UV * noise_scale_2 + TIME * noise_dir_2 * noise_speed_2 * 0.1;

    float n1 = texture(noise_texture_1, uv1).r;
    float n2 = texture(noise_texture_2, uv2).r;

    float n = mix(n1, n2, noise_blend);

    n = pow(n, noise_power);   // crush whites
    n *= noise_darkness;       // overall darkening

    ALBEDO = mix(base_color, smoke_color, n);
	ALPHA = max(0.97, n * alpha_strength);
}