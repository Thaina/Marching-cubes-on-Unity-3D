﻿using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class B_Mountains : Biome
{
	[Tooltip("The highest point of the surface")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int maxSurfaceheight = Constants.MAX_HEIGHT - 1;

	[Header("Texture generation")]
	[Tooltip("Increase the effect of the hightMatMult")][Range(1, 20f)]
	public float heightMatOffset = 10;
	[Tooltip("Multiplier of the slope in dependence of the height")]
	public AnimationCurve hightMatMult;
	[Tooltip("Height where the grass change to snow")][Range(0, Constants.MAX_HEIGHT)]
	public int snowHeight = 35;
	[Tooltip("Slope vale where terrain start to be rock")][Range(0, 1f)]
	public float rockLevel = 0.6f;
	[Tooltip("Slope vale where terrain start to be dirt")][Range(0, 1f)]
	public float dirtLevel = 0.25f;


	public override BiomeChunkData[] GenerateChunkData(int2 vecPos, float[] biomeMerge)
	{
		int surfaceStart = NoiseManager.Instance.worldConfig.surfaceLevel;//Avoid too high value that generate bad mesh
		var chunkData = new BiomeChunkData[Constants.CHUNK_TOTAL_VERTEX];
		var noise = NoiseManager.GenerateExtendedNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
		for (int a = 0; a < Constants.CHUNK_VERTEX_AREA; a++)//start a 1 because the noise start at -1 of the chunk vertex
		{
			var p = Constants.ModDiv(a,Constants.CHUNK_VERTEX_SIZE);
			// Get surface height of the x,z position 1276120704
			float height = surfaceStart + Mathf.Lerp(0,//Biome merge height
				terrainHeightCurve.Evaluate(noise[Noise.Index(p)]) * (maxSurfaceheight - surfaceStart),//Desired biome height
				biomeMerge[a]);//Merge value,0 = full merge, 1 = no merge

			//557164096
			int heightY = Mathf.CeilToInt(height);//Vertex Y where surface start
			int lastVertexWeigh = (int)((255 - isoLevel) * (height % 1) + isoLevel);//Weigh of the last vertex
			float slope = CalculateSlope(p, noise);

			for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
			{
				int index = ByteIndex(a,y);//apply x-1 and z-1 for get the correct index
				if (y < heightY)
				{
					chunkData[index].Value = 255;
					if (y < heightY - 5 || slope > rockLevel)
						chunkData[index].Material = 4;//Rock
					else if (slope < dirtLevel && y > snowHeight)//Avoid dirt in snow areas
						chunkData[index].Material = 3;
					else
						chunkData[index].Material = 1;//dirt
				}
				else if (y == heightY)
				{
					chunkData[index].Value = (byte)lastVertexWeigh;//
					if (slope > rockLevel)
						chunkData[index].Material = 4;//Mountain Rock
					else if (slope > dirtLevel)
						chunkData[index].Material = 1;//dirt
					else if (y > snowHeight)
						chunkData[index].Material = 3;//snow
					else chunkData[index].Material = 0;//grass
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

	/// <summary>
	/// Function that calculate the slope of the terrain
	/// </summary>
	private float CalculateSlope(int2 p, float[] noise)
	{
		float minValue = 1000;
		for (int x = -1; x <= 1; x++)
		{
			for (int z = -1; z <= 1; z++)
			{
				float value = terrainHeightCurve.Evaluate(noise[Noise.Index(p + new int2(x,z))]);
				if (value < minValue)
					minValue = value;
			}
		}
		float pointValue = terrainHeightCurve.Evaluate(noise[Noise.Index(p)]);
		return (1 - (minValue / pointValue)) * (hightMatMult.Evaluate(pointValue) * heightMatOffset);
	}
}
