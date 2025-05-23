﻿using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class B_Ice : Biome
{
	[Tooltip("The max deep and height of the desert dunes, low values")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int maxHeightDifference = Constants.MAX_HEIGHT / 5;

	[Tooltip("Number vertex (y), where the snow end and the rock start")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int snowDeep = Constants.MAX_HEIGHT / 5;

	[Header("Ice columns configuration")]
	[Tooltip("Scale of the noise used for the ice columns appear")][Range(0, 100)]
	public int iceNoiseScale = 40;
	[Tooltip("Value in the ice noise map where the ice columns appear")][Range(0, 1)]
	public float iceApearValue = 0.8f;
	[Tooltip("Ice columns max height")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int iceMaxHeight = 5;
	[Tooltip("Amplitude decrease of reliefs")]
	[Range(0.001f, 1f)]
	public float IcePersistance = 0.5f;
	[Tooltip("Frequency increase of reliefs")]
	[Range(1, 20)]
	public float IceLacunarity = 2f;

	public override BiomeChunkData[] GenerateChunkData(int2 vecPos, float[] biomeMerge)
	{
		var chunkData = new BiomeChunkData[Constants.CHUNK_TOTAL_VERTEX];
		var noise = NoiseManager.GenerateNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
		var iceNoise = NoiseManager.GenerateNoiseMap(iceNoiseScale,2,IcePersistance,IceLacunarity, vecPos);
		for (int n = 0; n < Constants.CHUNK_VERTEX_AREA; n++)
		{
			// Get surface height of the x,z position 
			float height = NoiseManager.Instance.worldConfig.surfaceLevel + Mathf.Lerp(0,//Biome merge height
				(terrainHeightCurve.Evaluate(noise[n]) * 2 - 1) * maxHeightDifference,//Desired biome height
				biomeMerge[n]);//Merge value,0 = full merge, 1 = no merge

			int heightY = Mathf.CeilToInt(height);//Vertex Y where surface start
			int lastVertexWeigh = (int)((255 - isoLevel) * (height % 1) + isoLevel);//Weigh of the last vertex

			//Ice calculations
			int iceExtraHeigh = 0;
			if (iceNoise[n] > iceApearValue)
				iceExtraHeigh = Mathf.CeilToInt((1- iceNoise[n] ) / iceApearValue * iceMaxHeight);

			for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
			{
				int index = ByteIndex(n,y);
				if (y < heightY - snowDeep)
				{
					chunkData[index].Value = 255;
					chunkData[index].Material = 4;//Rock
				}
				else if (y > heightY + iceExtraHeigh)
				{
					chunkData[index].Value = 0;
					chunkData[index].Material = Constants.NUMBER_MATERIALS;
				}
				else
				{
					if(y < heightY + iceExtraHeigh)
						chunkData[index].Value = 255;
					else chunkData[index].Value = (byte)lastVertexWeigh;

					if (y <= heightY)
						chunkData[index].Material = 3;//snow
					else chunkData[index].Material = 5;//ice
				}
			}
		}
		return chunkData;
	}
}

