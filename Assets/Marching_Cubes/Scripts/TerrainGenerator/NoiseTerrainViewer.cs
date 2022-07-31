using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class NoiseTerrainViewer : MonoBehaviour
{

    [Tooltip("Number of chunks of the view area")]
    [Range(1, 20)]
    public int testSize = 1;
    [Tooltip("Offset from the chunk (0,0), move the whole map generation")]
    public int2 chunkOffset;
    private Dictionary<int2, Chunk> chunkDict = new Dictionary<int2, Chunk>();
    private NoiseManager noiseManager;
    private Region fakeRegion;//Used because chunks need a fahter region
 

    private void Start()
    {
        noiseManager = NoiseManager.Instance;
        fakeRegion = new Region(1000);
        GenerateTerrain();
    }

    /// <summary>
    /// Generate a terrain for preview the NoiseManager values.
    /// </summary>
    public void GenerateTerrain()
    {
        if(chunkDict.Count != 0)
        {
            foreach(Chunk chunk in chunkDict.Values)
            {
                Destroy(chunk.gameObject);
            }
            chunkDict.Clear();
        }
        int halfSize = Mathf.FloorToInt(testSize / 2);
        for(int z= -halfSize; z< halfSize+1; z++)
        {
            for (int x = -halfSize; x < halfSize+1; x++)
            {
                var key = new int2(x, z);
                var chunkObj = new GameObject("Chunk_" + key.x + "|" + key.y, typeof(MeshFilter), typeof(MeshRenderer));
                chunkObj.transform.parent = transform;
                chunkObj.transform.position = new float3() { xz = (float2)key * Constants.CHUNK_SIDE };

                var offsetKey = new int2(x + chunkOffset.x, z+ chunkOffset.y);
                chunkDict.Add(key, chunkObj.AddComponent<Chunk>().ChunkInit(noiseManager.GenerateChunkData(offsetKey), key, fakeRegion, false));
            }
        }
    }

}


