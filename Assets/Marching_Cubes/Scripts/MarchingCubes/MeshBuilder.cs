
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

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

        try
        {
            var jobHandle = buildChunkJob.Schedule(Constants.CHUNK_VOXEL_SIZE,default);
            jobHandle.Complete();

            //Get all the data from the jobs and use to generate a Mesh
            var meshGenerated = new Mesh();

            meshGenerated.SetVertices(buildChunkJob.vertex.AsArray(),0,buildChunkJob.vertex.Length,MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            meshGenerated.SetUVs(0,buildChunkJob.uv.AsArray(),0,buildChunkJob.uv.Length,MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            meshGenerated.SetIndices(Enumerable.Range(0,buildChunkJob.vertex.Length).ToArray(),MeshTopology.Triangles,0,false);

            meshGenerated.RecalculateBounds();
            meshGenerated.RecalculateNormals();
            meshGenerated.RecalculateTangents();

            return meshGenerated;
        }
        finally
        {
            //Dispose (Clear the jobs NativeLists)
            buildChunkJob.Dispose();
        }
    }
}
