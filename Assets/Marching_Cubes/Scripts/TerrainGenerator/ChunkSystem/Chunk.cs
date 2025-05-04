using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public interface IChunkData
{
    byte Value { get; }
    byte Material { get; }
}

public class Chunk : MonoBehaviour
{
    [Tooltip("Active gizmos that represent the area of the chunk")]
    public bool debug = false;
    private BiomeChunkData[] data;
    private int2 pos;
    private Region fatherRegion;
    private bool modified = false;
    private bool changesUnsaved;

    /// <summary>
    /// Create a Chunk using a byte[] that contain all the data of the chunk.
    /// </summary>
    /// <param name="b"> data of the chunk</param>
    public Chunk ChunkInit(BiomeChunkData[] b, int2 p, Region region, bool save)
    {
        data = b;
        pos = p;
        fatherRegion = region;
        changesUnsaved = save;

        var myMesh = MeshBuilder.Instance.BuildChunk(b);
        GetComponent<MeshFilter>().mesh = myMesh;

        //Assign random color, new material each chunk.
        //mat mymaterial = new mat(Shader.Find("Custom/Geometry/FlatShading"));//Custom/DoubleFaceShader  |   Specular
        //mymat.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        GetComponent<MeshRenderer>().material = ChunkManager.Instance.terrainMaterial;
        gameObject.AddComponent<MeshCollider>();

        return this;
    }

    public void Update()
    {
        if(modified)
        {
            modified = false;
            changesUnsaved = true;

            var myMesh = MeshBuilder.Instance.BuildChunk(data);
            GetComponent<MeshFilter>().mesh = myMesh;
            GetComponent<MeshCollider>().sharedMesh = myMesh;

        }
    }

    /// <summary>
    /// Call depending of the type of modification to removeTerrain or addTerrain
    /// </summary>
    /// <param name="vertexPoint"></param>
    /// <param name="modification"></param>
    /// <param name="mat"></param>
    public void modifyTerrain(int3 vertexPoint, int modification, int mat = -1)
    {
        int byteIndex = Biome.ByteIndex(vertexPoint);

        var (value,material) = data[byteIndex];
        byte newValue = (byte)math.clamp(value + modification, 0, 255);

        if (value != newValue)
        {
            data[byteIndex].Value = newValue;
            modified = true;
        }
        
        if (material != mat && mat >= 0 && mat <= 255) // addTerrain
        {
            data[byteIndex].Material = (byte)mat;
            modified = true;
        }
        
        //Don't direct change because some vertex are modifier in the same editions, wait to next frame
    }

    /// <summary>
    /// Get the material(byte) from a specific point in the chunk
    /// </summary>
    public BiomeChunkData GetData(int3 vertexPoint)
    {
        return data[Biome.ByteIndex(vertexPoint)];
    }

    /// <summary>
    /// Get the material(byte) from a specific point in the chunk
    /// </summary>
    public byte GetMaterial(int3 vertexPoint)
    {
        return GetData(vertexPoint).Material;
    }

    public static float3 VertexSize => new float3(Constants.CHUNK_VERTEX_SIZE,Constants.CHUNK_VERTEX_HEIGHT,Constants.CHUNK_VERTEX_SIZE);

    /// <summary>
    /// Save the chunk data in the region if the chunk get some changes.
    /// </summary>
    public void saveChunkInRegion()
    {
        if(changesUnsaved)
            fatherRegion.saveChunkData(data,pos);
    }

#if UNITY_EDITOR
    //Used for visual debug
    void OnDrawGizmos()
    {
        if (debug)
        {
            //Gizmos.color = new Color(1f,0.28f,0f);
            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((transform.position.x + transform.position.z) % 100) / 100);

            Gizmos.DrawWireCube(transform.position,(float3)BoxSize * Constants.VOXEL_SIDE);
        }
    }
#endif

    public static int3 BoxSize => new int3(Constants.CHUNK_SIZE, Constants.MAX_HEIGHT, Constants.CHUNK_SIZE);
    public static int Index(int3 p) => p.x + p.z * Constants.CHUNK_SIZE + p.y * Constants.CHUNK_VOXEL_AREA;
}


