shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform float pixel_size : hint_range(1.0, 16.0) = 4.0;
uniform float color_levels : hint_range(2.0, 32.0) = 8.0;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.08;
uniform float brightness : hint_range(0.5, 2.0) = 1.25;
uniform float shadow_lift : hint_range(0.0, 0.5) = 0.08;
uniform float exposure : hint_range(0.25, 3.0) = 1.15;
uniform float contrast : hint_range(0.5, 2.0) = 1.08;
uniform float saturation : hint_range(0.0, 2.0) = 1.12;
uniform vec3 color_tint = vec3(1.04, 1.0, 0.92);
uniform float dither_strength : hint_range(0.0, 1.0) = 0.25;
uniform float grain_strength : hint_range(0.0, 1.0) = 0.06;
uniform float chromatic_aberration : hint_range(0.0, 4.0) = 0.75;
uniform float curvature : hint_range(0.0, 0.1) = 0.01;
uniform float infection_intensity : hint_range(0.0, 2.0) = 0.0;
uniform float infection_pulse_strength : hint_range(0.0, 1.0) = 0.35;
uniform float infection_vein_strength : hint_range(0.0, 1.0) = 0.45;
uniform float infection_rot_strength : hint_range(0.0, 1.0) = 0.35;
uniform float infection_edge_darkness : hint_range(0.0, 1.0) = 0.45;
uniform vec3 infection_tint = vec3(0.68, 0.92, 0.35);

float bayer4(vec2 p) {
    vec2 ip = mod(floor(p), 4.0);

    if (ip.y < 0.5) {
        if (ip.x < 0.5) { return 0.0 / 16.0; }
        if (ip.x < 1.5) { return 8.0 / 16.0; }
        if (ip.x < 2.5) { return 2.0 / 16.0; }
        return 10.0 / 16.0;
    }

    if (ip.y < 1.5) {
        if (ip.x < 0.5) { return 12.0 / 16.0; }
        if (ip.x < 1.5) { return 4.0 / 16.0; }
        if (ip.x < 2.5) { return 14.0 / 16.0; }
        return 6.0 / 16.0;
    }

    if (ip.y < 2.5) {
        if (ip.x < 0.5) { return 3.0 / 16.0; }
        if (ip.x < 1.5) { return 11.0 / 16.0; }
        if (ip.x < 2.5) { return 1.0 / 16.0; }
        return 9.0 / 16.0;
    }

    if (ip.x < 0.5) { return 15.0 / 16.0; }
    if (ip.x < 1.5) { return 7.0 / 16.0; }
    if (ip.x < 2.5) { return 13.0 / 16.0; }
    return 5.0 / 16.0;
}

float random(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

vec2 curve_uv(vec2 uv) {
    vec2 centered = uv * 2.0 - 1.0;
    return uv + centered * dot(centered, centered) * curvature;
}

vec2 infection_uv(vec2 uv, vec2 screen_pixel_size) {
    vec2 centered = uv - vec2(0.5);
    float radius = length(centered);
    float collapse = max(infection_intensity - 1.0, 0.0);
    float fever = sin(TIME * 4.5 + radius * 22.0);
    vec2 twitch = vec2(
        sin(uv.y * 32.0 + TIME * 3.2 + collapse * sin(uv.x * 80.0)),
        cos(uv.x * 28.0 - TIME * 2.7 + collapse * cos(uv.y * 74.0))
    ) * screen_pixel_size * mix(3.0, 24.0, collapse) * infection_intensity;

    float angle = atan(centered.y, centered.x);
    angle += collapse * sin(radius * 30.0 - TIME * 5.0) * 0.8;
    vec2 collapsed = vec2(cos(angle), sin(angle)) * radius + vec2(0.5);

    vec2 warped = mix(uv, collapsed, collapse * 0.75);
    return warped + centered * fever * mix(0.015, 0.12, collapse) * infection_intensity + twitch;
}

vec2 pixelate_uv(vec2 uv, vec2 screen_size) {
    return floor(uv * screen_size / pixel_size) * pixel_size / screen_size;
}

void fragment() {
    vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE;

    vec2 warped_uv = curve_uv(infection_uv(SCREEN_UV, SCREEN_PIXEL_SIZE));
    if (warped_uv.x < 0.0 || warped_uv.x > 1.0 || warped_uv.y < 0.0 || warped_uv.y > 1.0) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
    } else {
        vec2 pixel_uv = pixelate_uv(warped_uv, screen_size);
        vec2 chroma_offset = vec2(chromatic_aberration, 0.0) * SCREEN_PIXEL_SIZE * pixel_size;

        float red = texture(screen_texture, pixelate_uv(pixel_uv + chroma_offset, screen_size)).r;
        float green = texture(screen_texture, pixel_uv).g;
        float blue = texture(screen_texture, pixelate_uv(pixel_uv - chroma_offset, screen_size)).b;
        vec3 color = vec3(red, green, blue);

        float collapse = max(infection_intensity - 1.0, 0.0);
        float infection_noise = random(floor(FRAGCOORD.xy / max(pixel_size, 1.0)) + floor(TIME * mix(10.0, 38.0, collapse)));
        vec2 center_uv = SCREEN_UV - vec2(0.5);
        float radius = length(center_uv);
        float angle = atan(center_uv.y, center_uv.x);
        float veins = sin(angle * 18.0 + radius * 48.0 - TIME * 2.5);
        veins += sin(angle * 9.0 - radius * 72.0 + TIME * 1.8);
        veins = smoothstep(1.1, 1.85, veins) * smoothstep(0.12, 0.95, radius);
        veins *= infection_vein_strength * infection_intensity;

        // Lift the scene before posterization so dark colors do not collapse to black.
        color = color * brightness + shadow_lift * (1.0 - color);

        // Color grade the lighting before posterization.
        color *= exposure;
        color = (color - 0.5) * contrast + 0.5;
        float luma = dot(color, vec3(0.299, 0.587, 0.114));
        color = mix(vec3(luma), color, saturation);
        color *= color_tint;

        // Zombie infection: sick tint, fever pulse, rot blotches, and creeping veins.
        float infection_pulse = 0.5 + 0.5 * sin(TIME * 3.4 + radius * 16.0);
        vec3 sick_color = vec3(luma) * infection_tint;
        color = mix(color, sick_color, infection_intensity * 0.55);
        color.g += infection_intensity * 0.12 * infection_pulse;
        color.r -= infection_intensity * 0.08;
        color.b -= infection_intensity * 0.10;
        color *= 1.0 - infection_rot_strength * infection_intensity * infection_noise * 0.22;
        color -= vec3(0.04, 0.12, 0.02) * veins;
        color *= 1.0 + infection_pulse_strength * infection_intensity * (infection_pulse - 0.5) * 0.35;
        color = mix(color, vec3(color.g, color.r * 0.45, color.b * 0.2), collapse * 0.75);
        color += (infection_noise - 0.5) * collapse * 0.55;
        color = mix(color, vec3(step(0.48 + infection_noise * 0.12, luma)), collapse * 0.28);

        // Ordered dithering breaks up posterized bands.
        float dither = bayer4(FRAGCOORD.xy) - 0.5;
        color += dither * dither_strength / color_levels;

        // Posterize colors.
        color = round(color * (color_levels - 1.0)) / (color_levels - 1.0);

        // Film grain, strongest in darker areas.
        float grain = random(FRAGCOORD.xy + vec2(TIME * 60.0, TIME * 17.0)) - 0.5;
        float darkness = 1.0 - clamp(dot(color, vec3(0.299, 0.587, 0.114)), 0.0, 1.0);
        color += grain * (grain_strength * mix(0.35, 1.0, darkness) + collapse * 0.35);

        // Vignette.
        float dist = distance(warped_uv, vec2(0.5));
        float base_vignette = smoothstep(0.35, 0.8, dist) * vignette_strength;
        float infection_vignette = smoothstep(0.22, 0.78, dist) * infection_edge_darkness * infection_intensity;
        infection_vignette += smoothstep(0.05, 0.62, dist) * collapse * 0.75;
        color *= 1.0 - clamp(base_vignette + infection_vignette, 0.0, 0.95);

        COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
    }
}
