shader_type canvas_item;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
	vec4 bg = texture(SCREEN_TEXTURE, SCREEN_UV);
	float col = (bg.x + bg.y + bg.z) / 3.0;
	COLOR = vec4(col,col,col,1.0);
}
