using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]//For test without burst, just remove this flag.
public struct BuildChunkJob<C> : IJobFor,INativeDisposable where C : struct,IChunkData
{
    [ReadOnly]public NativeArray<C> chunkData;
    [WriteOnly]public NativeList<float3> vertex;
    [WriteOnly]public NativeList<float2> uv;

    [ReadOnly] public int isoLevel;
    [ReadOnly] public bool interpolate;

	public JobHandle Dispose(JobHandle inputDeps)
	{
		return chunkData.Dispose(uv.Dispose(vertex.Dispose(inputDeps)));
	}

	public void Dispose()
	{
        vertex.Dispose();
        uv.Dispose();
        chunkData.Dispose();
	}

    /// <summary>
    /// Called when run the job.
    /// </summary>
    public void Execute(int index)
    {
        var pos = Constants.PositionXZY(index,Chunk.BoxSize);

        var cube = new NativeArray<float4>(8,Allocator.Temp);
        int mat = Constants.NUMBER_MATERIALS;
        int cubeindex = 0;
        for(int i = 0; i < 8; i++)
        {
            var point = pos + Constants.CubePointOffSet(i);
            int bindex = Biome.ByteIndex(point);

            float w = chunkData[bindex].Value;
            if(w >= isoLevel)
                mat = math.min(mat,chunkData[bindex].Material);
            else cubeindex |= (1 << i);

            cube[i] = new float4((point - (float3)Chunk.BoxSize / 2) * Constants.VOXEL_SIDE,w);
        }

        CalculateVertex(cube, mat, cubeindex);
    }

    /// <summary>
    ///  Calculate the vertices of the voxels, get the vertices of the triangulation table and his position in the world. Also check materials of that vertex (UV position).
    /// </summary>
    public void CalculateVertex(NativeArray<float4> cube, int colorVert,int cubeindex)
    {
        var indexTopRight = new int3x2(new int3(0,1,4),new int3(1,3,4));
        //Values above isoLevel are inside the figure, value of 0 means that the cube is entirely inside of the figure.
        for (int i = cubeindex * 16; MarchCubeJob.jobTriTable[i] != -1; i++)
        {
            var (v1,v2) = MarchCubeJob.cornerIndexFromEdge[MarchCubeJob.jobTriTable[i]];

            float weight = 0.5f;//Unused variable, must be used for interpolation terrain
            vertex.Add(interpolate ? interporlateVertex(cube[v1], cube[v2], out weight) : (0.5f * (cube[v1].xyz + cube[v2].xyz)));

            const float uvOffset = 0.01f; //Small offset for avoid pick pixels of other textures
            const float outerOffset = Constants.MATERIAL_SIZE - uvOffset;
            //NEED REWORKING FOR CORRECT WORKING, now have problems with the directions of the uv
            var cuv = Constants.MATERIAL_SIZE * (float2)Constants.ModDiv(colorVert,Constants.MATERIAL_FOR_ROW);
            var isTopRight = i % 6 == indexTopRight;
            cuv += math.select(outerOffset,uvOffset,new bool2(math.any(isTopRight.c0),math.any(isTopRight.c1)));
            uv.Add(new float2(cuv.x,1 - cuv.y));
        }
    }

    #region helpMethods
    /// <summary>
    /// Calculate a point between two vertex using the weight of each vertex , used in interpolation voxel building.
    /// </summary>
    public float3 interporlateVertex(in float4 p1,in float4 p2, out float interpolation)
    {
        interpolation = (isoLevel - p1.w) / (p2.w - p1.w);
        return math.lerp(p1.xyz, p2.xyz, interpolation);
    }

	#endregion
}
