using System.Threading.Tasks;
using Godot;

namespace Procedural_Content.Scenes;

public ref struct GenerationParameters
{
    public int Octaves;
    public float Lacunarity;
    public float Persistence;
    public float Scale;
    public int Seed;
    public int MapSize;
    public Vector2 Offset;
}

public partial class TextureGeneration : Node3D
{
    [Export]
    public MeshInstance3D Mesh { get; set; }
    
    private FastNoiseLite _noise;
    private ImageTexture _heightMapTexture;
    
    public virtual ImageTexture GenerateHeightMap(GenerationParameters parameters)
    {
        // Initialize noise
        _noise.Seed = parameters.Seed;
        _noise.FractalOctaves = parameters.Octaves;
        _noise.FractalLacunarity = parameters.Lacunarity;
        _noise.FractalGain = parameters.Persistence;
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        
        // Create image for the height map
        Image heightImage = Image.CreateEmpty(parameters.MapSize, parameters.MapSize, false, Image.Format.Rgb8);
        
        // Fill the height map with noise values
        for (int y = 0; y < parameters.MapSize; y++)
        {
            for (int x = 0; x < parameters.MapSize; x++)
            {
                float sampleX = (float)x / parameters.Scale + parameters.Offset.X;
                float sampleY = (float)y / parameters.Scale + parameters.Offset.Y;
                
                float noiseValue = GenerateNoiseValue(sampleX, sampleY);
                
                // Convert noise value (-1 to 1) to color (0 to 1)
                float normalizedValue = (noiseValue + 1) * 0.5f;
                
                // Set the pixel color (grayscale, where the value represents height)
                Color heightColor = new Color(normalizedValue, normalizedValue, normalizedValue);
                heightImage.SetPixel(x, y, heightColor);
            }
        }
        
        // Create ImageTexture from the image
        _heightMapTexture = ImageTexture.CreateFromImage(heightImage);
        
        return _heightMapTexture;
    }
    
    private float GenerateNoiseValue(float x, float y)
    {
        // For more complex terrain, you can combine multiple noise functions
        // or implement additional algorithms like domain warping here
        return _noise.GetNoise2D(x, y);
    }
    
    public virtual ImageTexture GetHeightMapTexture()
    {
        return _heightMapTexture;
    }
}