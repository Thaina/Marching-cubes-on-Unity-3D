using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class B_Plains : Biome
{
	[Tooltip("The max deep and height of the plains, low values")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int maxHeightDifference = Constants.MAX_HEIGHT/5;

	public override BiomeChunkData[] GenerateChunkData(int2 vecPos, float[] biomeMerge)
	{
		var chunkData = new BiomeChunkData[Constants.CHUNK_TOTAL_VERTEX];
		var noise = NoiseManager.GenerateNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
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
				if (y < heightY)
				{
					chunkData[index].Value = 255;
					if (y < heightY - 5)
						chunkData[index].Material = 4;//Rock
					else chunkData[index].Material = 1;//dirt
				}
				else if (y == heightY)
				{
					chunkData[index].Value = (byte)lastVertexWeigh;
					chunkData[index].Material = 0;//grass
				}
				else
				{
					chunkData[index].Value = 0;
					chunkData[index].Material = Constants.NUMBER_MATERIALS;
				}
			}
		}
		return chunkData;
	}
}
