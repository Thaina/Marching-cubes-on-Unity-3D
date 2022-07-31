using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

public static class Noise 
{
	public const int CHUNK_SIZE = Constants.CHUNK_VERTEX_SIZE + 2;
	public const int CHUNK_AREA = CHUNK_SIZE * CHUNK_SIZE;

	public static int Index(int2 p) => Index(p.x,p.y);
	public static int Index(int x,int z) => (x + 1) + (z + 1) * CHUNK_SIZE;

    public static float [,] StandarNoise(int mapWidth, int mapHeight, float scale)
    {
		float[,] noiseMap = new float[mapWidth, mapHeight];

		if (scale <= 0)
		{
			scale = 0.0001f;
		}

		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				float sampleX = x / scale;
				float sampleY = y / scale;

				float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
				noiseMap[x, y] = perlinValue;
			}
		}
		return noiseMap;
	}


}
