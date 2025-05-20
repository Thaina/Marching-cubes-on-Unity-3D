using Unity.Mathematics;

public static class Constants
{
    #region configurable variables
    public const int CHUNK_SIZE = 16; //Number voxel per side
    public const int MAX_HEIGHT = 80; //Number of voxel of height in a chunk, pair number recommended
    public const float VOXEL_SIDE = 1f; //Size of a side of a voxel

    public const int REGION_SIZE = 32; //Number chunk per side. If change REGION_SIZE maybe you need change REGION_LOOKTABLE_POS_BYTE.
    public const int REGION_LOOKTABLE_POS_BYTE = 2; //Number of byte needed for represent (REGION_SIZE * REGION_SIZE) +1. Example: (32 x 32) +1= 1025 = 2 bytes needed.  MAX = 4.

    public const int NUMBER_MATERIALS = 9; //Total number of different materials, max = 256 = 1 byte
    public const int MATERIAL_FOR_ROW = 3; //Number of materials in a row of the texture

    public const bool SAVE_GENERATED_CHUNKS = false; //False, no save unmodified chunks, use seed to generate again the next time (-File size -save time +Generation cost). True, generated chunk are saved in the memory (+File size +save time -Generation cost )
    public const bool REGION_SAVE_COMPRESSED = true; //Compress the .reg files. -File size +CPU cost of save a file. RECOMMENDED: TRUE
    public const bool AUTO_CLEAR_WHEN_NOISE_CHANGE = true; //If World Manager not exists in the scene and the current noise change, we clear the old world data.

    #endregion

    # region auto-configurable variables
    public const int REGION_CHUNKS = REGION_SIZE * REGION_SIZE;
    public const int REGION_LOOKTABLE_BYTES = REGION_LOOKTABLE_POS_BYTE * (REGION_CHUNKS + 1);//REGION_LOOKTABLE_POS_BYTE offset because first position indicate the last writes position in the chunkTable

    public const float CHUNK_SIDE = CHUNK_SIZE * VOXEL_SIDE;
    public const int CHUNK_VOXEL_AREA = CHUNK_SIZE * CHUNK_SIZE;
    public const int CHUNK_VOXEL_SIZE = CHUNK_VOXEL_AREA * MAX_HEIGHT;
    public const int CHUNK_VERTEX_SIZE = CHUNK_SIZE + 1;
    public const int CHUNK_VERTEX_HEIGHT = MAX_HEIGHT + 1;
    public const int CHUNK_VERTEX_AREA = CHUNK_VERTEX_SIZE * CHUNK_VERTEX_SIZE;
    public const int CHUNK_TOTAL_VERTEX = CHUNK_VERTEX_AREA * CHUNK_VERTEX_HEIGHT;

    public const float MATERIAL_SIZE = (float)MATERIAL_FOR_ROW / (float)NUMBER_MATERIALS;
    public const float MATERIAL_OFFSET = MATERIAL_SIZE / 2f;

    #endregion

    public static int3 CubePointOffSet(int i) => new int3(((i + 1) / 2) % 2,i / 4,1 - ((i / 2) % 2));

	public static int2 ModDiv(int n,int size) => new int2(n % size,n / size);
	public static int3 PositionXZY(int index,int3 size)
    {
        var p = ModDiv(index,size.x);
        return new int3(p.x,p.y / size.z,p.y % size.z);
    }
}

public struct CubePoint<T>
{
    public T p001;
    public T p101;
    public T p100;
    public T p000;
    public T p011;
    public T p111;
    public T p110;
    public T p010;

    public static ref T Indexing(ref CubePoint<T> cp,int i)
    {
        switch(i)
        {
            case 0: return ref cp.p001;
            case 1: return ref cp.p101;
            case 2: return ref cp.p100;
            case 3: return ref cp.p000;
            case 4: return ref cp.p011;
            case 5: return ref cp.p111;
            case 6: return ref cp.p110;
            case 7: return ref cp.p010;
            default: throw new System.IndexOutOfRangeException("i = " + i);
        }
    }

    public T this[int i]
    {
        get => Indexing(ref this,i);
        set => Indexing(ref this,i) = value;
    }
}
