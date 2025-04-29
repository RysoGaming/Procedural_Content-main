#[compute]
#version 450

// Définir la taille du groupe de travail
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Paramètres de bruit envoyés depuis C#
layout(set = 0, binding = 0, std140) uniform NoiseParams {
    float noise_scale;
    int octaves;
    float persistence;
    float lacunarity;
    int seed;
    vec2 offset;
} noise_params;

// Image de sortie
layout(r32f, set = 0, binding = 1) uniform image2D height_map;

// Hash simple
vec2 hash(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

// Simplex noise (2D)
vec3 permute(vec3 x) { return mod(((x * 34.0) + 1.0) * x, 289.0); }

float snoise(vec2 v){
    const vec4 C = vec4(0.211324865405187, 0.366025403784439,
                        -0.577350269189626, 0.024390243902439);
    vec2 i = floor(v + dot(v, C.yy));
    vec2 x0 = v - i + dot(i, C.xx);

    vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;

    i = mod(i, 289.0);
    vec3 p = permute(permute(i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0));

    vec3 m = max(0.5 - vec3(dot(x0, x0), dot(x12.xy, x12.xy),
                            dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;

    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;

    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);

    vec3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;

    return 130.0 * dot(m, g);
}

// Bruit fractal (fBM)
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = 1.0;
    float normalization = 0.0;

    for (int i = 0; i < noise_params.octaves; i++) {
        value += amplitude * snoise(p * frequency);
        normalization += amplitude;
        amplitude *= noise_params.persistence;
        frequency *= noise_params.lacunarity;
    }

    value /= normalization;
    return (value + 1.0) * 0.5;
}

// Fonction principale
void main() {
    ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 image_size = imageSize(height_map);

    if (pixel_coords.x >= image_size.x || pixel_coords.y >= image_size.y)
        return;

    vec2 uv = vec2(pixel_coords) / vec2(image_size);
    vec2 sample_pos = (uv + noise_params.offset) * noise_params.noise_scale;

    sample_pos += vec2(float(noise_params.seed));

    float height = fbm(sample_pos);

    imageStore(height_map, pixel_coords, vec4(height, 0.0, 0.0, 1.0));
}
