using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace Procedural_Content.Scenes;

public partial class ComputeTextureGenerator : TextureGeneration
{
    private RDShaderFile _computeShaderFile;
    private RenderingDevice _rd;
    private Rid _shader;
    private Rid _pipeline;
    private Rid _uniformSet;
    private Rid _heightMapImageTexture;
    private ImageTexture _resultTexture;
    
    private bool _isGenerating = false;
    private long _computeListId;
    public override void _Ready()
    {
        // Get the rendering device
        _rd = RenderingServer.GetRenderingDevice();
        
        // Load the compute shader
        _computeShaderFile = GD.Load<RDShaderFile>("res://Shaders/heightmap_compute.glsl");
        
        // Initialize the compute shader pipeline
        InitializeComputePipeline();
    }
    
    private void InitializeComputePipeline()
    {
        if (_rd == null || _computeShaderFile == null)
            return;
        
        // Create shader from shader file
        RDShaderSpirV shaderSpirV = _computeShaderFile.GetSpirV();
        _shader = _rd.ShaderCreateFromSpirV(shaderSpirV);
        
        // Create compute pipeline
        _pipeline = _rd.ComputePipelineCreate(_shader);
    }
    
    public override ImageTexture GenerateHeightMap(GenerationParameters parameters)
    {
        if (_rd == null || _isGenerating)
            return null;
            
        _isGenerating = true;
        
        // Create an image to hold our height map data
        Image heightImage = Image.CreateEmpty(parameters.MapSize, parameters.MapSize, false, Image.Format.Rf);
        
        // Create a texture format
        RDTextureFormat textureFormat = new RDTextureFormat();
        textureFormat.Format = RenderingDevice.DataFormat.R32Sfloat;
        textureFormat.Width = (uint)parameters.MapSize;
        textureFormat.Height = (uint)parameters.MapSize;
        textureFormat.UsageBits = RenderingDevice.TextureUsageBits.StorageBit | 
                              RenderingDevice.TextureUsageBits.CanUpdateBit | 
                              RenderingDevice.TextureUsageBits.CanCopyFromBit;
        
        // Create image texture in the rendering device
        Rid heightMapImageTexture = _rd.TextureCreate(textureFormat, new RDTextureView());
        
        // The uniform buffer needs to be properly aligned for std140 layout
        // We need to ensure proper alignment and padding of our data
        byte[] uniformData = new byte[32]; // 8 float values (4 bytes each) with padding
        
        // Fill the uniform buffer with our parameters
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Scale), 0, uniformData, 0, 4);  // float noise_scale
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Octaves), 0, uniformData, 4, 4);     // int octaves
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Persistence), 0, uniformData, 8, 4);  // float persistence
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Lacunarity), 0, uniformData, 12, 4);  // float lacunarity
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Seed), 0, uniformData, 16, 4);        // int seed
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Offset.X), 0, uniformData, 20, 4);    // vec2 offset.x
        Buffer.BlockCopy(BitConverter.GetBytes(parameters.Offset.Y), 0, uniformData, 24, 4);    // vec2 offset.y
        // Last 4 bytes are padding to ensure alignment
        
        // Create a uniform buffer (not storage buffer)
        Rid uniformBuffer = _rd.UniformBufferCreate((uint)uniformData.Length);
        _rd.BufferUpdate(uniformBuffer, 0, (uint)uniformData.Length, uniformData);
        
        // Create a uniform to hold our buffer - using UniformType.UniformBuffer
        RDUniform paramsUniform = new RDUniform();
        paramsUniform.UniformType = RenderingDevice.UniformType.UniformBuffer;
        paramsUniform.Binding = 0;
        paramsUniform.AddId(uniformBuffer);
        
        // Create a uniform for the height map image
        RDUniform heightMapUniform = new RDUniform();
        heightMapUniform.UniformType = RenderingDevice.UniformType.Image;
        heightMapUniform.Binding = 1;
        heightMapUniform.AddId(heightMapImageTexture);
        
        // Create a uniform set with our bindings
        Rid uniformSet = _rd.UniformSetCreate([paramsUniform, heightMapUniform], _shader, 0);
        
        // Dispatch the compute shader
        _computeListId = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(_computeListId, _pipeline);
        _rd.ComputeListBindUniformSet(_computeListId, uniformSet, 0);
        
        // Calculate dispatch size (ceil(size / local_size))
        int xGroups = (parameters.MapSize + 7) / 8;
        int yGroups = (parameters.MapSize + 7) / 8;
        _rd.ComputeListDispatch(_computeListId, (uint)xGroups, (uint)yGroups, 1);
        _rd.ComputeListEnd();
        
        // Read back the texture data
        byte[] data = _rd.TextureGetData(heightMapImageTexture, 0);
        
        // Convert the data to an image
        Image resultImage = Image.CreateFromData(parameters.MapSize, parameters.MapSize, false, Image.Format.Rf, data);
        
        // Create ImageTexture from the image
        _resultTexture = ImageTexture.CreateFromImage(resultImage);
        
        // Clean up resources
        _rd.FreeRid(heightMapImageTexture);
        _rd.FreeRid(uniformBuffer);
        
        _isGenerating = false;
        
        return _resultTexture;
    }
    
    public override ImageTexture GetHeightMapTexture()
    {
        return _resultTexture;
    }
    
    public override void _ExitTree()
    {
        // Clean up resources
        if (_rd != null)
        {
            if (_shader.Id != 0) _rd.FreeRid(_shader);
            if (_pipeline.Id != 0) _rd.FreeRid(_pipeline);
        }
    }
}