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

vec2 pixelate_uv(vec2 uv, vec2 screen_size) {
    return floor(uv * screen_size / pixel_size) * pixel_size / screen_size;
}

void fragment() {
    vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE;

    vec2 warped_uv = curve_uv(SCREEN_UV);
    if (warped_uv.x < 0.0 || warped_uv.x > 1.0 || warped_uv.y < 0.0 || warped_uv.y > 1.0) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
    } else {
        vec2 pixel_uv = pixelate_uv(warped_uv, screen_size);
        vec2 chroma_offset = vec2(chromatic_aberration, 0.0) * SCREEN_PIXEL_SIZE * pixel_size;

        float red = texture(screen_texture, pixelate_uv(pixel_uv + chroma_offset, screen_size)).r;
        float green = texture(screen_texture, pixel_uv).g;
        float blue = texture(screen_texture, pixelate_uv(pixel_uv - chroma_offset, screen_size)).b;
        vec3 color = vec3(red, green, blue);

        // Lift the scene before posterization so dark colors do not collapse to black.
        color = color * brightness + shadow_lift * (1.0 - color);

        // Color grade the lighting before posterization.
        color *= exposure;
        color = (color - 0.5) * contrast + 0.5;
        float luma = dot(color, vec3(0.299, 0.587, 0.114));
        color = mix(vec3(luma), color, saturation);
        color *= color_tint;

        // Ordered dithering breaks up posterized bands.
        float dither = bayer4(FRAGCOORD.xy) - 0.5;
        color += dither * dither_strength / color_levels;

        // Posterize colors.
        color = round(color * (color_levels - 1.0)) / (color_levels - 1.0);

        // Film grain, strongest in darker areas.
        float grain = random(FRAGCOORD.xy + vec2(TIME * 60.0, TIME * 17.0)) - 0.5;
        float darkness = 1.0 - clamp(dot(color, vec3(0.299, 0.587, 0.114)), 0.0, 1.0);
        color += grain * grain_strength * mix(0.35, 1.0, darkness);

        // Vignette.
        float dist = distance(warped_uv, vec2(0.5));
        color *= 1.0 - smoothstep(0.35, 0.8, dist) * vignette_strength;

        COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
    }
}
