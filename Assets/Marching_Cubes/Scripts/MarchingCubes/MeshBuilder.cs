
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class MeshBuilder : Singleton<MeshBuilder>
{
    [Tooltip("Value from which the vertices are inside the figure")][Range(0, 255)]
    public int isoLevel = 128;
    [Tooltip("Allow to get a middle point between the voxel vertices in function of the weight of the vertices")]
    public bool interpolate = false;


    /// <summary>
    /// Method that calculate cubes, vertex and mesh in that order of a chunk.
    /// </summary>
    /// <param name="b"> data of the chunk</param>
    public Mesh BuildChunk(byte[] b)
    {
        var buildChunkJob = new BuildChunkJob() {
            chunkData = new NativeArray<byte>(b, Allocator.TempJob),
            isoLevel = this.isoLevel,
            interpolate = this.interpolate,
            vertex = new NativeList<float3>(500, Allocator.TempJob),
            uv = new NativeList<float2>(100, Allocator.TempJob),
        };

        JobHandle jobHandle = buildChunkJob.Schedule();
        jobHandle.Complete();

        //Get all the data from the jobs and use to generate a Mesh
        var meshGenerated = new Mesh();
        var meshVert = new Vector3[buildChunkJob.vertex.Length];
        var meshTriangles = new int[buildChunkJob.vertex.Length];
        for (int i = 0; i < buildChunkJob.vertex.Length; i++)
        {
            meshVert[i] = buildChunkJob.vertex[i];
            meshTriangles[i] = i;
        }
        meshGenerated.vertices = meshVert;

        var meshUV = new Vector2[buildChunkJob.vertex.Length];

        for (int i = 0; i < buildChunkJob.vertex.Length; i++)
        {
            meshUV[i] = buildChunkJob.uv[i];
        }
        meshGenerated.uv = meshUV;
        meshGenerated.triangles = meshTriangles;
        meshGenerated.RecalculateNormals();
        meshGenerated.RecalculateTangents();

        //Dispose (Clear the jobs NativeLists)
        buildChunkJob.Dispose();

        return meshGenerated;
    }
}
