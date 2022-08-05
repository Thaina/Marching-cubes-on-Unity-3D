
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
	public byte isoLevel = 128;
	[Tooltip("Allow to get a middle point between the voxel vertices in function of the weight of the vertices")]
	public bool interpolate = false;

	/// <summary>
	/// Method that calculate cubes, vertex and mesh in that order of a chunk.
	/// </summary>
	/// <param name="b"> data of the chunk</param>
	public Mesh BuildChunk(byte[] b)
	{
		JobHandle jobHandle;
		var chunkData = new NativeArray<byte>(b, Allocator.TempJob);

		var indices = new NativeList<int>(Allocator.TempJob);
		var filterChunkJob = new FilterChunkJob() { isoLevel = this.isoLevel,chunkData = chunkData };
		jobHandle = filterChunkJob.ScheduleAppend(indices,Constants.CHUNK_VOXEL_SIZE,64);
		jobHandle.Complete();

		var buildChunkJob = new BuildCubeJob() {
			isoLevel = this.isoLevel,
			chunkData = chunkData,
			chunkIndexes = indices,
			results = new NativeArray<BuildCubeJob.CubeData>(indices.Length, Allocator.TempJob)
		};

		try
		{
			jobHandle = buildChunkJob.Schedule(indices.Length,default);
			jobHandle.Complete();

			indices.Clear();

			var filterCubeJob = new FilterCubeJob() { cubeDatas = buildChunkJob.results, };
			jobHandle = filterCubeJob.ScheduleAppend(indices,buildChunkJob.results.Length * 16,64);
			jobHandle.Complete();

			var marchCubeJob = new MarchCubeJob() {
				isoLevel = this.isoLevel,
				interpolate = this.interpolate,
				cubeDatas = buildChunkJob.results,
				indices = indices,
				vertex = new NativeArray<float3>(indices.Length,Allocator.TempJob),
				uv = new NativeArray<float2>(indices.Length,Allocator.TempJob),
			};

			try
			{
				jobHandle = marchCubeJob.Schedule(indices.Length,default);
				jobHandle.Complete();

				//Get all the data from the jobs and use to generate a Mesh
				var meshGenerated = new Mesh();

				meshGenerated.SetVertices(marchCubeJob.vertex,0,marchCubeJob.vertex.Length,MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
				meshGenerated.SetUVs(0,marchCubeJob.uv,0,marchCubeJob.uv.Length,MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
				meshGenerated.SetIndices(Enumerable.Range(0,marchCubeJob.vertex.Length).ToArray(),MeshTopology.Triangles,0,false);

				meshGenerated.RecalculateBounds();
				meshGenerated.RecalculateNormals();
				meshGenerated.RecalculateTangents();

				return meshGenerated;
			}
			finally
			{
				//Dispose (Clear the jobs NativeLists)
				marchCubeJob.Dispose();
			}
		}
		finally
		{
			indices.Dispose();

			//Dispose (Clear the jobs NativeLists)
			buildChunkJob.Dispose();
		}
	}

	public Mesh BuildChunkOld(byte[] b)
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
