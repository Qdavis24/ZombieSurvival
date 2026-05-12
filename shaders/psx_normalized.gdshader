shader_type spatial;

render_mode diffuse_lambert, specular_disabled;

// Shared globally across the whole game.
global uniform float global_color_levels;
global uniform float global_brightness;

// Unique per material/asset.
uniform sampler2D albedo_tex : source_color, filter_nearest;
uniform float alpha_scissor : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    vec4 tex = texture(albedo_tex, UV);

    vec3 color = tex.rgb * global_brightness;

    color = round(color * (global_color_levels - 1.0))
        / (global_color_levels - 1.0);

    ALBEDO = color;

    ALPHA = tex.a;
    ALPHA_SCISSOR_THRESHOLD = alpha_scissor;

    ROUGHNESS = 1.0;
    SPECULAR = 0.0;
}