﻿using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]//For test without burst, just remove this flag.
public struct FilterChunkJob<C> : IJobFilter where C : struct,IChunkData
{
	[ReadOnly]public byte isoLevel;
	[ReadOnly]public NativeArray<C> chunkData;
	bool IJobFilter.Execute(int index)
	{
		var pos = Constants.PositionXZY(index,Chunk.BoxSize);
		int cubeindex = 0;
		for(int i = 0; i < 8; i++)
		{
			var point = pos + Constants.CubePointOffSet(i);
			int bindex = Biome.ByteIndex(point);

			byte w = chunkData[bindex].Value;
			if(w < isoLevel)
				cubeindex |= (1 << i);
		}

		return cubeindex != 0 && cubeindex != 255;
	}
}

public struct BuildCubeData
{
	public int material;
	public int cubeindex;
	public CubePoint<float4> cube;
}

[BurstCompile]//For test without burst, just remove this flag.
public struct BuildCubeJob<C> : IJobParallelFor,INativeDisposable where C : struct,IChunkData
{
	[ReadOnly]public byte isoLevel;
	[ReadOnly]public NativeArray<int> chunkIndexes;
	[ReadOnly]public NativeArray<C> chunkData;
	[WriteOnly]public NativeArray<BuildCubeData> results;

	public JobHandle Dispose(JobHandle inputDeps)
	{
		return chunkData.Dispose(results.Dispose(inputDeps));
	}

	public void Dispose()
	{
		results.Dispose();
		chunkData.Dispose();
	}

	/// <summary>
	/// Called when run the job.
	/// </summary>
	public void Execute(int n)
	{
		int index = chunkIndexes[n];

		var pos = Constants.PositionXZY(index,Chunk.BoxSize);
		int mat = Constants.NUMBER_MATERIALS;
		int cubeindex = 0;
		CubePoint<float4> cube = default;
		for(int i = 0; i < 8; i++)
		{
			var point = pos + Constants.CubePointOffSet(i);
			int bindex = Biome.ByteIndex(point);

			byte w = chunkData[bindex].Value;
			if(w < isoLevel)
				cubeindex |= (1 << i);
			else mat = math.min(mat,chunkData[bindex].Material);

			cube[i] = new float4((point - (float3)Chunk.BoxSize / 2) * Constants.VOXEL_SIDE,w);
		}

		results[n] = new BuildCubeData() {
			cubeindex = cubeindex,
			material = mat,
			cube = cube,
		};
	}
}
