using Godot;
using ImGuiNET;

namespace Procedural_Content.Scenes;

public partial class TerrainGenerator : Node3D
{
    [Export]
    public int MapSize = 1024;
    
    [Export]
    public float NoiseScale = 50.0f;
    
    [Export]
    public int Octaves = 4;
    
    [Export]
    public float Persistence = 0.5f;
    
    [Export]
    public float Lacunarity = 2.0f;
    
    [Export]
    public uint Seed = 42;
    
    [Export]
    public Vector2 Offset = Vector2.Zero;
    
    [Export]
    private MeshInstance3D _mesh;
    
    [Export]
    public NodePath HeightMapGeneratorPath { get; set; }
    
    [Export]
    public int TerrainSize { get; set; } = 20; // World units (meters)
    
    [Export]
    public float HeightScale { get; set; } = 5.0f; // Maximum height in world units
    
    [Export]
    public int Resolution { get; set; } = 256; // Number of vertices in each direction
    
    private TextureGeneration _heightMapGenerator;
    
    public override void _Ready()
    {
        GD.Randomize();
        _heightMapGenerator = GetNode<TextureGeneration>(HeightMapGeneratorPath);
    }
    
    public void GenerateTerrain(GenerationParameters parameters)
    {
        // Generate the height map
        ImageTexture heightMap = _heightMapGenerator.GenerateHeightMap(parameters);
        
        // Create the plane mesh
        PlaneMesh planeMesh = new PlaneMesh();
        planeMesh.Size = new Vector2(TerrainSize, TerrainSize);
        planeMesh.SubdivideWidth = Resolution - 1;
        planeMesh.SubdivideDepth = Resolution - 1;
        
        // Apply the mesh
        _mesh.Mesh = planeMesh;
        
        // Create a material for the terrain
        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoTexture = heightMap; // You might want a different color texture
        material.HeightmapTexture = heightMap;
        material.HeightmapScale = HeightScale;
        
        _mesh.MaterialOverride = material;
        
        // For more realistic deformation, manually modify the mesh's vertices
        DeformMeshWithHeightMap(planeMesh, heightMap);
    }
    
    private void DeformMeshWithHeightMap(PlaneMesh planeMesh, ImageTexture heightMap)
    {
        // Create a surface tool to manipulate the mesh
        SurfaceTool surfaceTool = new SurfaceTool();
        surfaceTool.CreateFrom(planeMesh, 0);
        
        // Get the mesh data
        ArrayMesh arrayMesh = surfaceTool.Commit();
        
        // You'll need to access the vertex data
        // This requires working with mesh arrays
        if (arrayMesh.GetSurfaceCount() > 0)
        {
            Godot.Collections.Array meshData = arrayMesh.SurfaceGetArrays(0);
            Vector3[] vertices = (Vector3[])meshData[0];
            
            // Get the height map data
            Image heightImage = heightMap.GetImage();
            
            // Deform each vertex based on the height map
            for (int i = 0; i < vertices.Length; i++)
            {
                // Map the vertex position to UV coordinates (0 to 1)
                Vector2 uv = new Vector2(
                    (vertices[i].X + TerrainSize / 2.0f) / TerrainSize,
                    (vertices[i].Z + TerrainSize / 2.0f) / TerrainSize
                );
                
                // Map UV to pixel coordinates
                int x = Mathf.Clamp((int)(uv.X * heightImage.GetWidth()), 0, heightImage.GetWidth() - 1);
                int y = Mathf.Clamp((int)(uv.Y * heightImage.GetHeight()), 0, heightImage.GetHeight() - 1);
                
                // Sample the height from the texture
                Color heightColor = heightImage.GetPixel(x, y);
                float height = heightColor.R; // Using red channel (grayscale)
                
                // Apply the height to the vertex
                vertices[i].Y = height * HeightScale;
            }
            
            // Update the mesh with new vertices
            meshData[0] = vertices;
            
            // Create a new mesh with the updated data
            ArrayMesh newMesh = new ArrayMesh();
            newMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshData);
            
            // Apply the new mesh
            _mesh.Mesh = newMesh;
            
            // Recalculate normals for proper lighting
            surfaceTool.Clear();
            surfaceTool.CreateFrom(newMesh, 0);
            surfaceTool.GenerateNormals();
            _mesh.Mesh = surfaceTool.Commit();
        }
    }
    
    public override void _Process(double delta)
    {
        ImGui.Begin("Procedural Terrain Generator");
        ImGui.SliderInt("Octaves", ref Octaves, 1, 10);
        ImGui.SliderFloat("Persistence", ref Persistence, 0.0f, 1.0f);
        ImGui.SliderFloat("Lacunarity", ref Lacunarity, 0.0f, 5.0f);
        ImGui.SliderFloat("Scale", ref NoiseScale, 0.0f, 500f);
        ImGui.SliderInt("Map Size", ref MapSize, 128, 8192);
        ImGui.InputFloat("Offset X : ", ref Offset.X);
        ImGui.InputFloat("Offset Y : ", ref Offset.Y);
        if (ImGui.Button("New Seed"))
        {
            Seed = GD.Randi();
            GenerationParameters generationParameters;
            generationParameters.Seed = (int)Seed;
            generationParameters.Octaves = Octaves;
            generationParameters.Persistence = Persistence;
            generationParameters.Lacunarity = Lacunarity;
            generationParameters.Offset = Offset;
            generationParameters.Scale = NoiseScale;
            generationParameters.MapSize = MapSize;
            GenerateTerrain(generationParameters);
        }

        if (ImGui.Button("Generate"))
        {
            GenerationParameters generationParameters;
            generationParameters.Seed = (int)Seed;
            generationParameters.Octaves = Octaves;
            generationParameters.Persistence = Persistence;
            generationParameters.Lacunarity = Lacunarity;
            generationParameters.Offset = Offset;
            generationParameters.Scale = NoiseScale;
            generationParameters.MapSize = MapSize;
            GenerateTerrain(generationParameters);
        }
        ImGui.End();
    }
}