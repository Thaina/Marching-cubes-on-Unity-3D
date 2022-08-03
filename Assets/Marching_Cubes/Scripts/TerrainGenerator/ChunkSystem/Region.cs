using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

public class Region
{
    private readonly string worldpath;
    /*REGIONS DATA = lookTable (REGION_LOOKTABLE_POS_BYTE * number of chunks in a region) + chunks data (2 byte per vertex in each chunk)
    lookTable: Indicate the start position of the chunk data in the region Data byte list +REGION_LOOKTABLE_POS_BYTE. Because the 0 it's reserved for indicate empty chunk.
    chunks data: Contains the data of all chunks saved in the region*/
    private List<byte> regionData;
    private int2 region;
    private bool modified = false;

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
            if (!string.IsNullOrEmpty(data))
            {
                var bytes = System.Convert.FromBase64String(data);
                regionData = new List<byte>(Constants.REGION_SAVE_COMPRESSED ? CompressHelper.Decompress(bytes) : bytes);
                return;
            }
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }

        regionData = new List<byte>(new byte[Constants.REGION_LOOKTABLE_BYTES]);//Look table initialized, all 0
    }
    
    /// <summary>
    /// Return the byte[] from a chunk in the region.
    /// </summary>
    public byte[] GetChunkData(int index)
    {
        int startPos = Constants.REGION_LOOKTABLE_BYTES + (index - 1) * Constants.CHUNK_BYTES; // index-1 because the lookTable start at 1. LookTable position 10 = chunk data position 9.
        var chunk = new byte[Constants.CHUNK_BYTES];

        for (int i = startPos, j = 0; i < (startPos + Constants.CHUNK_BYTES); i ++,j++)
        {
            chunk[j] = regionData[i];
        }

        return chunk;
    }

    /// <summary>
    /// Get the index from the lookTable, the first section of the regionData list<byte>.
    /// </summary>
    public int GetChunkIndex(int2 key)
    {

        int startPos = (key.x + key.y * Constants.REGION_SIZE) * Constants.REGION_LOOKTABLE_POS_BYTE + Constants.REGION_LOOKTABLE_POS_BYTE;
        int index = 0;

        for (int i = 0; i< Constants.REGION_LOOKTABLE_POS_BYTE; i++)
        {
   
            index |= regionData[startPos+ Constants.REGION_LOOKTABLE_POS_BYTE-i-1] << 8*i;
        }

        return index;


    }

    /// <summary>
    /// save a chunk byte[] data in the regionData list<byte> of the Chunk class.
    /// </summary>
    public void saveChunkData(byte[] chunkData, int2 key)
    {
        int chunksDataStartPos = GetChunkIndex(key); 
        if(chunksDataStartPos == 0)//Chunk no saved, assign a new position in the chunks data for the lookTable.
        {
            int lookTablePos = (key.x + key.y * Constants.REGION_SIZE) * Constants.REGION_LOOKTABLE_POS_BYTE + Constants.REGION_LOOKTABLE_POS_BYTE ;
            int increasePos = Constants.REGION_LOOKTABLE_POS_BYTE - 1;
            for (int i = Constants.REGION_LOOKTABLE_POS_BYTE-1; i >= 0; i--)
            {
                //First done the increase because the 0 is reserved for empty, the lookTable start at 1.
                if (i == increasePos)
                {
                    if (regionData[i] == 255)//Reach 255, need return to 0 and change the previous byte.
                    {
                        regionData[i] = 0;
                        increasePos--;
                    }
                    else
                        regionData[i]++;
                }
                //Save the position of chunk in the chunks data inside the lookTable
                regionData[lookTablePos + i] = regionData[i];
            }
            //Write chunks bytes in the regionData list<byte>
            regionData.AddRange(chunkData);
        }
        else
        {
            int startPos = (chunksDataStartPos - 1) * Constants.CHUNK_BYTES + Constants.REGION_LOOKTABLE_BYTES;
            // Write chunks bytes in the regionData list<byte>
            for(int i = 0; i < Constants.CHUNK_BYTES; i++)
            {
                regionData[startPos + i] = chunkData[i];
            }
        }
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
        var data = regionData.ToArray();
        var encoded = System.Convert.ToBase64String(Constants.REGION_SAVE_COMPRESSED ? CompressHelper.Compress(data) : data);
        WorldManager.SaveFile(DirectionChunkFile(),encoded);
    }


    //Help function, get chunk file direction.
    private string DirectionChunkFile()
    {
        return System.IO.Path.Combine(worldpath,region.x + "."+ region.y + ".reg");
    }


}
