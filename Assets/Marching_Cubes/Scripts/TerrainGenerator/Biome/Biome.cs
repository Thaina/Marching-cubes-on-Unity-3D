using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct BiomeChunkData : IChunkData
{
	public byte Value { get; set; }
	public byte Material { get; set; }

	public void Deconstruct(out byte value, out byte material)
	{
		value = Value;
		material = Material;
	}
}

abstract public class Biome : MonoBehaviour
{
	[Header("Noise / terrain generation")]
	[Tooltip("Animation curve for attenuate the height in some ranges")]
	public AnimationCurve terrainHeightCurve = AnimationCurve.Linear(0,0,1,1);
	[Tooltip("Scale of the noise map")][Range(0.001f, 100f)]
	public float scale = 50f;
	[Tooltip("Number of deferents relief apply to the terrain surface")][Range(1, 5)]
	public int octaves = 4;
	[Tooltip("Amplitude decrease of reliefs")][Range(0.001f, 1f)]
	public float persistance = 0.5f;
	[Tooltip("Frequency increase of reliefs")][Range(1, 20)]
	public float lacunarity = 2f;

	protected int isoLevel;

	public static int ByteIndex(int n,int y) => n + y * Constants.CHUNK_VERTEX_AREA;
	public static int ByteIndex(int3 p) => ByteIndex(p.x + p.z * Constants.CHUNK_VERTEX_SIZE,p.y);

	public virtual void Start()
	{
		isoLevel = MeshBuilder.Instance.isoLevel;
	}

	/// <summary>
	/// Generate the chunk data
	/// </summary>
	public abstract BiomeChunkData[] GenerateChunkData(int2 vecPos, float[] biomeMerge);
}
