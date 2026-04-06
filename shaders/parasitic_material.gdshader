shader_type spatial;

uniform float speed = 5.0;
uniform float strength = 0.02;
uniform vec4 albedo_color : source_color = vec4(0.6, 0.8, 0.2, 1.0);

void vertex() {
    VERTEX.z += sin(TIME * speed + VERTEX.y * 20.0) * strength;
	VERTEX.x += sin(TIME * speed + VERTEX.y * 20.0) * strength;
   
}

void fragment() {
	ALBEDO = albedo_color.rgb;
	ROUGHNESS = .3;
}

