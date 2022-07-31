using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class B_Desert : Biome
{

	[Tooltip("The max deep and height of the snow dunes, low values")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int maxHeightDifference = Constants.MAX_HEIGHT / 5;

	[Header("Texture generation")]
	[Tooltip("Number vertex (y), where the sand end and the rock start")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int sandDeep = Constants.MAX_HEIGHT / 5;
	public override byte[] GenerateChunkData(int2 vecPos, float[] biomeMerge)
	{
		var chunkData = new byte[Constants.CHUNK_BYTES];
		float[] noise = NoiseManager.GenerateNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
		for (int n = 0; n < Constants.CHUNK_VERTEX_AREA; n++)
		{
			// Get surface height of the x,z position 
			float height = NoiseManager.Instance.worldConfig.surfaceLevel + Mathf.Lerp(0,//Biome merge height
				(terrainHeightCurve.Evaluate(noise[n]) * 2 - 1) * maxHeightDifference,//Desired biome height
				biomeMerge[n]);//Merge value,0 = full merge, 1 = no merge

			int heightY = Mathf.CeilToInt(height);//Vertex Y where surface start
			int lastVertexWeigh = (int)((255 - isoLevel) * (height % 1) + isoLevel);//Weigh of the last vertex

			for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
			{
				int index = ByteIndex(n,y);
				if (y < heightY - sandDeep)
				{
					chunkData[index] = 255;
					chunkData[index + 1] = 4;//Rock
				}
				else if (y > heightY)
				{
					chunkData[index] = 0;
					chunkData[index + 1] = Constants.NUMBER_MATERIALS;
				}
				else
				{
					if (y == heightY)
						chunkData[index] = (byte)lastVertexWeigh;
					else chunkData[index] = 255;
					chunkData[index + 1] = 6;//sand
				}
			}
		}
		return chunkData;
	}
}
