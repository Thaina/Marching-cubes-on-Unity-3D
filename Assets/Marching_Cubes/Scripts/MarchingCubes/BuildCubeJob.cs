using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]//For test without burst, just remove this flag.
public struct FilterChunkJob : IJobParallelForFilter
{
    [ReadOnly]public int isoLevel;
    [ReadOnly]public NativeArray<byte> chunkData;
	bool IJobParallelForFilter.Execute(int index)
	{
        var pos = Constants.PositionXZY(index,Chunk.BoxSize);
        int cubeindex = 0;
        for(int i = 0; i < 8; i++)
        {
            var point = pos + Constants.CubePointOffSet(i);
            int bindex = Biome.ByteIndex(point);

            float w = chunkData[bindex];
            if(w < isoLevel)
                cubeindex |= (1 << i);
        }

        return cubeindex != 0 && cubeindex != 255;
	}
}

[BurstCompile]//For test without burst, just remove this flag.
public struct BuildCubeJob : IJobParallelFor,INativeDisposable
{
    [ReadOnly]public int isoLevel;
    [ReadOnly]public NativeArray<byte> chunkData;
    [WriteOnly]public NativeArray<CubeData> results;

    public struct CubeData
    {
        public int material;
        public int cubeindex;
        public CubePoint<float4> cube;
    }

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
    public void Execute(int index)
    {
        var pos = Constants.PositionXZY(index,Chunk.BoxSize);
        int mat = Constants.NUMBER_MATERIALS;
        int cubeindex = 0;
        CubePoint<float4> cube = default;
        for(int i = 0; i < 8; i++)
        {
            var point = pos + Constants.CubePointOffSet(i);
            int bindex = Biome.ByteIndex(point);

            float w = chunkData[bindex];
            if(w < isoLevel)
                cubeindex |= (1 << i);
            else mat = math.min(mat,chunkData[bindex + 1]);

            cube[i] = new float4((point - (float3)Chunk.BoxSize / 2) * Constants.VOXEL_SIDE,w);
        }

        results[index] = new CubeData() {
            cubeindex = cubeindex,
            material = mat,
            cube = cube,
        };
    }
}
