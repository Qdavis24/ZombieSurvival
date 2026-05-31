shader_type spatial;

render_mode diffuse_lambert, specular_disabled;

// Shared globally across the whole game.
global uniform float global_brightness;

// Unique per zombie skin.
uniform sampler2D albedo_tex : source_color, filter_nearest;
uniform float color_levels : hint_range(2.0, 64.0, 1.0) = 24.0;
uniform float color_brightness : hint_range(0.0, 2.0, 0.01) = 0.82;
uniform float saturation : hint_range(0.0, 2.0, 0.01) = 0.68;
uniform float grime_strength : hint_range(0.0, 1.0, 0.01) = 0.35;
uniform float grime_scale : hint_range(1.0, 64.0, 1.0) = 18.0;
uniform vec3 grime_color : source_color = vec3(0.12, 0.09, 0.05);
uniform bool virtual_texture_resolution_enabled = true;
uniform vec2 virtual_texture_size = vec2(256.0, 256.0);
uniform bool vertex_wobble_enabled = true;
uniform vec2 vertex_snap_resolution = vec2(320.0, 240.0);
uniform float alpha_scissor : hint_range(0.0, 1.0) = 0.5;

float random(vec2 position) {
    return fract(sin(dot(position, vec2(12.9898, 78.233))) * 43758.5453);
}

float value_noise(vec2 position) {
    vec2 cell = floor(position);
    vec2 blend = smoothstep(vec2(0.0), vec2(1.0), fract(position));

    float bottom = mix(random(cell), random(cell + vec2(1.0, 0.0)), blend.x);
    float top = mix(random(cell + vec2(0.0, 1.0)), random(cell + vec2(1.0, 1.0)), blend.x);
    return mix(bottom, top, blend.y);
}

void vertex() {
    if (vertex_wobble_enabled) {
        vec4 clip_position = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
        vec2 half_resolution = max(vertex_snap_resolution * 0.5, vec2(1.0));
        vec2 snapped_ndc = round((clip_position.xy / clip_position.w) * half_resolution) / half_resolution;
        clip_position.xy = snapped_ndc * clip_position.w;
        POSITION = clip_position;
    }
}

void fragment() {
    vec2 sampled_uv = UV;
    if (virtual_texture_resolution_enabled) {
        sampled_uv = (floor(UV * virtual_texture_size) + 0.5) / virtual_texture_size;
    }

    vec4 tex = texture(albedo_tex, sampled_uv);
    vec3 color = tex.rgb * global_brightness * color_brightness;
    float luma = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(vec3(luma), color, saturation);
    float grime = value_noise(UV * grime_scale);
    grime = smoothstep(0.38, 0.78, grime) * grime_strength;
    color = mix(color, grime_color, grime);

    color = round(color * (color_levels - 1.0))
        / (color_levels - 1.0);

    ALBEDO = color;
    ALPHA = tex.a;
    ALPHA_SCISSOR_THRESHOLD = alpha_scissor;
    ROUGHNESS = 1.0;
    SPECULAR = 0.0;
}
