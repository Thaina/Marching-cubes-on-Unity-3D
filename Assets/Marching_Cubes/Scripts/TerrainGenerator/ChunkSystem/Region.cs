using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using System.Linq;

public class Region
{
    private readonly string worldpath;
    /*REGIONS DATA = lookTable (REGION_LOOKTABLE_POS_BYTE * number of chunks in a region) + chunks data (2 byte per vertex in each chunk)
    lookTable: Indicate the start position of the chunk data in the region Data byte list +REGION_LOOKTABLE_POS_BYTE. Because the 0 it's reserved for indicate empty chunk.
    chunks data: Contains the data of all chunks saved in the region*/
    private BiomeChunkData[][] regionData;
    private int2 region;
    private bool modified = false;

    public static int OctreeIndex(int3 point)
    {
        int index = 0;
		int p = 0;
        while(math.any(point > 0))
        {
            point = point >> 1;
            var anded = point & 1;
            var shift = (p * 3) + new int3(0,2,1);
            index |= (anded.x << shift.x) | (anded.y << shift.y) | (anded.z << shift.z);
            p++;
        }

        return index;
    }

    public static void OctreePoint(int i,out int3 p)
    {
        p = 0;
		int n2 = 1;
		while(i > 0)
		{
            p += n2 * (new int3(i >> 0,i >> 2,i >> 1) & 1);
			i >>= 3;
			n2 *= 2;
		}
    }

    /// <summary>
    /// Load the data of a region from a file.
    /// </summary>
    public Region(int2 location)
    {
        worldpath = WorldManager.GetSelectedWorldName();
        region = location;

        try
        {
            var data = WorldManager.LoadFile(DirectionChunkFile());
            Debug.LogWarning("TODO: Use saved data");
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }

        regionData = new BiomeChunkData[Constants.REGION_CHUNKS][];//Look table initialized, all 0
    }
    
    /// <summary>
    /// Return the byte[] from a chunk in the region.
    /// </summary>
    public BiomeChunkData[] GetChunkData(int index)
    {
        return regionData[index];
    }

    public int GetStartPos(int2 key) => key.x + (key.y * Constants.REGION_SIZE);

    /// <summary>
    /// Get the index from the lookTable, the first section of the regionData list<byte>.
    /// </summary>
    public int GetChunkIndex(int2 key) => GetStartPos(key);

    /// <summary>
    /// save a chunk byte[] data in the regionData list<byte> of the Chunk class.
    /// </summary>
    public void saveChunkData(BiomeChunkData[] chunkData, int2 key)
    {
        int i = GetChunkIndex(key);
        regionData[i] = chunkData;
        modified = true;
    }

    /// <summary>
    /// Save the region data in a file.
    /// </summary>
    public void SaveRegionData()
    {
        if(!modified)
            return;

        modified = false;
        var data = regionData.SelectMany((datas) => datas).SelectMany((data) => new[] { data.Value,data.Material }).ToArray();
        var encoded = System.Convert.ToBase64String(Constants.REGION_SAVE_COMPRESSED ? CompressHelper.Compress(data) : data);
        WorldManager.SaveFile(DirectionChunkFile(),encoded);
    }


    //Help function, get chunk file direction.
    private string DirectionChunkFile()
    {
        return System.IO.Path.Combine(worldpath,region.x + "." + region.y + ".reg");
    }
}
