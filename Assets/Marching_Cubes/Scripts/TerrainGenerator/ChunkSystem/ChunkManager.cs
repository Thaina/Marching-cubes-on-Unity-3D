using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

public class ChunkManager : Singleton<ChunkManager>
{
    [Tooltip("Material used by all the terrain.")]
    public Material terrainMaterial;
    [Range(3, Constants.REGION_SIZE/2)][Tooltip("Chunks load and visible for the player,radius distance.")]
    public int chunkViewDistance = 10;
    [Range(0.1f, 0.6f)][Tooltip("Distance extra for destroy inactive chunks, this chunks consume ram, but load faster.")]
    public float chunkMantainDistance = 0.3f;
    [Tooltip("Use the camera position to calculate the player position. True-> use Camera.main tag / False-> use Player tag")]
    public bool useCameraPosition = true;
    [Tooltip("F4 to active. Show the current chunk and the data of the voxel you are looking. Important: You need activate gizmos in Game tab!!")]
    public bool debugMode = false;

    private Dictionary<int2, Chunk> chunkDict = new Dictionary<int2, Chunk>();
    private Dictionary<int2, Region> regionDict = new Dictionary<int2, Region>();
    private Queue<int2> chunkLoadList = new Queue<int2>();

    private NoiseManager noiseManager;
    private Transform player;
    private float2 lastPlayerPos;
    private int lastChunkViewDistance;
    private float hideDistance;
    private float removeDistance;
    
    const float loadRegionDistance = Constants.CHUNK_SIDE * Constants.REGION_SIZE * Constants.VOXEL_SIDE * 0.9f;
    float2 RegionFromPosition(float3 pos) => math.floor(pos.xz / loadRegionDistance);

    //Search the player start position and the start the chunk load (Called from NoiseManager start)
    public void Initialize()
    {
        noiseManager = NoiseManager.Instance;
        player = useCameraPosition || !(GameObject.FindGameObjectWithTag("Player") is var gobj && gobj != null) ? Camera.main.transform : gobj.transform; // Search gameobject with tag Player

        var xz = RegionFromPosition(player.position);
        lastPlayerPos = xz * loadRegionDistance + loadRegionDistance / 2;
        initRegion((int2)xz);
    }

    /// <summary>
    /// Load surrounding regions of the player when first load
    /// </summary>
    void initRegion(int2 init)
    {
        for (int x = init.x-1; x < init.x+2; x++)
        {
            for (int z = init.y-1; z < init.y + 2; z++)
            {
                var key = new int2(x,z);
                regionDict.Add(key,new Region(key));
            }
        }
    }

    /// <summary>
    /// Load new regions and unload the older.
    /// </summary>
    void LoadRegion(int2 init)
    {
        var newRegionDict = new Dictionary<int2, Region>();

        for (int x = init.x-1; x < init.x+2; x++)
        {
            for (int z = init.y-1; z < init.y + 2; z++)
            {
                var key = new int2(x,z);
                newRegionDict.Add(key,regionDict.Remove(key,out var region) ? region : new Region(key));
            }
        }

        //save old regions
        foreach(var region in regionDict.Values)
            region.SaveRegionData();

        //Assign new region area
        regionDict = newRegionDict;
    }

    //Called each frame
    void Update()
    {
        if(lastChunkViewDistance != chunkViewDistance)
            CalculateDistances();
        HiddeRemoveChunk();
        CheckNewChunks();
        LoadChunkFromList();
        CheckRegion();
        if(Input.GetKeyDown(KeyCode.F4))
        {
            debugMode = !debugMode;
        }
        //Debug.Log("Regions: " + regionDict.Count + "   / Chunks: " + chunkDict.Count);
    }

    /// <summary>
    /// Check the distance to the player for inactive or remove the chunk.  
    /// </summary>
    void HiddeRemoveChunk()
    {
        var removeList = new List<int2>(); ;
        foreach(var chunk in chunkDict)
        {
            float distance = math.length(new float3(player.position - chunk.Value.transform.position).xz);
            if(distance > removeDistance)
            {
                chunk.Value.saveChunkInRegion();//Save chunk only in case that get some modifications
                Destroy(chunk.Value.gameObject);
                removeList.Add(chunk.Key);
            }
            else if(distance > hideDistance && chunk.Value.gameObject.activeSelf)
            {
                chunk.Value.gameObject.SetActive(false);
            }
        }

        //remove chunks
        if(removeList.Count != 0)
        {
            foreach(var key in removeList)
            {
                //Debug.Log("chunk deleted: " + key);
                chunkDict.Remove(key);
            }
        }
    }

    static int2 ActualChunk(float3 pos,float2 shift) => new int2(math.ceil((pos.xz + shift - Constants.CHUNK_SIDE / 2) / Constants.CHUNK_SIDE));

    /// <summary>
    /// Load in chunkLoadList or active Gameobject chunks at the chunkViewDistance radius of the player
    /// </summary>
    void CheckNewChunks()
    {
        var actualChunk = ActualChunk(player.position,0);
        //Debug.Log("Actual chunk: " + actualChunk);
        for(int x = actualChunk.x - chunkViewDistance; x < actualChunk.x + chunkViewDistance; x++)
        {
            for (int z = actualChunk.y - chunkViewDistance; z < actualChunk.y + chunkViewDistance; z++)
            {
                if(math.distancesq(actualChunk,new int2(x,z)) > chunkViewDistance * chunkViewDistance)
                {
                    continue;
                }

                var key = new int2(x, z);
                if(chunkDict.TryGetValue(key,out var chunk))
                {
                    if(!chunk.gameObject.activeSelf)
                        chunk.gameObject.SetActive(true);
                }
                else if(!chunkLoadList.Contains(key))
                {
                    chunkLoadList.Enqueue(key);
                }
            }
        }
    }

    /// <summary>
    /// Load one chunk per frame from the chunkLoadList
    /// </summary>
    void LoadChunkFromList()
    {
        if(!chunkLoadList.TryDequeue(out var key))
            return;

        var regionPos = new int2(math.floor((float2)key / Constants.REGION_SIZE));
        if(!regionDict.TryGetValue(regionPos,out var region))//In case that the chunk isn't in the loaded regions we remove it, tp or too fast movement.
            return;

        var chunkObj = new GameObject("Chunk_" + key.x + "|" + key.y, typeof(MeshFilter), typeof(MeshRenderer));
        chunkObj.transform.parent = transform;
        chunkObj.transform.position = new float3() { xz = (float2)key * Constants.CHUNK_SIDE };
        //Debug.Log("Try load: "+x+"|"+z +" in "+regionPos);

        var keyInsideChunk = key - regionPos * Constants.REGION_SIZE;
        //We get X and Y in the world position, we need calculate the x and y in the region.
        int chunkIndexInRegion = region.GetChunkIndex(keyInsideChunk);
        bool isFromRegion = chunkIndexInRegion != 0;
        var chunkData = isFromRegion ? region.GetChunkData(chunkIndexInRegion) : noiseManager.GenerateChunkData(key);//Generate chunk with the noise generator
        chunkDict.Add(key, chunkObj.AddComponent<Chunk>().ChunkInit(chunkData, keyInsideChunk, region, !isFromRegion && Constants.SAVE_GENERATED_CHUNKS));
    }

    /// <summary>
    /// Check chunk manager need load a new regions area
    /// </summary>
    void CheckRegion()
    {
        var pos = (float3)player.position;
        if(math.any(math.abs(lastPlayerPos - pos.xz) > loadRegionDistance))
        {
            var actual = RegionFromPosition(pos);
            lastPlayerPos = actual * loadRegionDistance + loadRegionDistance / 2;
            LoadRegion((int2)actual);
        }
    }


   
    /// <summary>
    /// Calculate the distances of hide, remove and load chunks.
    /// </summary>
    void CalculateDistances()
    {
        lastChunkViewDistance = chunkViewDistance;
        hideDistance = Constants.CHUNK_SIDE * chunkViewDistance;
        removeDistance = hideDistance + hideDistance * chunkMantainDistance;
    }

    /// <summary>
    /// Modify voxels in a specific point of a chunk.
    /// </summary>
    public void ModifyChunkData(float3 modificationPoint, float range, float modification, int mat = -1)
    {
        modificationPoint /= Constants.VOXEL_SIDE;

        //Chunk voxel position (based on the chunk system)
        var vertexOrigin = (int3)modificationPoint;

        //intRange (convert vec3 real world range to the voxel size range)
        int intRange = (int)(range * Constants.VOXEL_SIDE / 2);//range /2 because the for is from -intRange to +intRange

        for (int y = -intRange; y <= intRange; y++)
        {
            for (int z = -intRange; z <= intRange; z++)
            {
                for (int x = -intRange; x <= intRange; x++)
                {
                    //Edit vertex of the chunk
                    var vertexPoint = vertexOrigin + new float3(x,y,z);
                    //Avoid edit the first and last height vertex of the chunk, for avoid non-faces in that heights
                    if(math.abs(vertexPoint.y) >= Constants.MAX_HEIGHT / 2)
                        continue;

                    float distance = math.distance(vertexPoint, modificationPoint);
                    if(distance > range)//Not in range of modification, we check other vertexs
                    {
                        //Debug.Log("no Rango: "+ distance + " > " + range+ " |  "+ vertexPoint +" / " + modificationPoint);
                        continue;
                    }

                    //Chunk of the vertexPoint
                    var hitChunk = ActualChunk(vertexPoint,1);
                    //Position of the vertexPoint in the chunk (x,y,z)
                    var vertexChunk = (int3)(vertexPoint - new float3() { xz = hitChunk * Constants.CHUNK_SIZE } + Chunk.VertexSize / 2);

                    int chunkModification = (int)(modification * (1 - distance / range));

                    if(chunkDict.TryGetValue(hitChunk,out var chunk00))
                    {
                        chunk00.modifyTerrain(vertexChunk, chunkModification, mat);
                    }

                    var isZero = vertexChunk.xz == 0;

                    //Functions for change last vertex of chunk (vertex that touch others chunk)
                    if(math.all(isZero) && chunkDict.TryGetValue(hitChunk - new int2(1,1),out var chunk11))//Interact with chunk(-1,-1)
                    {
                        chunk11.modifyTerrain(new int3(vertexChunk) { xz = Constants.CHUNK_SIZE }, chunkModification, mat);
                    }

                    if(isZero.x && chunkDict.TryGetValue(hitChunk - new int2(1,0),out var chunk10))//Interact with vertex of chunk(-1,0)
                    {
                        chunk10.modifyTerrain(new int3(vertexChunk) { x = Constants.CHUNK_SIZE }, chunkModification, mat);
                    }
                    
                    if(isZero.y && chunkDict.TryGetValue(hitChunk - new int2(0,1),out var chunk01))//Interact with vertex of chunk(0,-1)
                    {
                        chunk01.modifyTerrain(new int3(vertexChunk) { z = Constants.CHUNK_SIZE }, chunkModification, mat);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the material(byte) from a specific point in the world
    /// </summary>
    public byte GetMaterialFromPoint(float3 point)
    {
        point /= Constants.VOXEL_SIDE;

        var vertexOrigin = (float3)math.int3(point);

        //Loop next vertex for get a other material different to air
        for (int i = -1; i < 6; i++)
        {
            var nextVertexPoint = vertexOrigin + new int3 {
                x = i == 1 ? 1 : (i == 2 ? -1 : 0),
                y = i == 5 ? 1 : (i == 0 ? -1 : 0),
                z = i == 3 ? 1 : (i == 4 ? -1 : 0),
            };

            //Chunk of the vertexPoint
            var hitChunk = ActualChunk(nextVertexPoint,1);
            //Position of the vertexPoint in the chunk (x,y,z)
            var vertexChunk = new int3(nextVertexPoint - new float3() { xz = hitChunk * Constants.CHUNK_SIZE } + Chunk.VertexSize / 2);

            if(chunkDict[hitChunk].GetMaterial(vertexChunk) is var tmp && tmp != Constants.NUMBER_MATERIALS)//not air material, we return it
            {
                return tmp;
            }
        }

        return Constants.NUMBER_MATERIALS;//only air material in that point.
    }

    /// <summary>
    /// Save all chunk and regions data when user close the game.
    /// </summary>
    void OnApplicationQuit() => SaveRegions();

    public void SaveRegions()
    {
        try
        {
            //save chunks
            foreach(var chunk in chunkDict.Values)
                chunk.saveChunkInRegion();

            //save regions
            foreach(var region in regionDict.Values)
                region.SaveRegionData();
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    #region DebugMode
    //The code of the region is used for the Debug system, allow you to check your current chunk and see data from the voxels.

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(debugMode && Application.isPlaying)
        {
            //Show chunk
            var actualChunk = ActualChunk(player.position,0);
            var chunkCenter = new float3() { xz = (float2)actualChunk * Constants.CHUNK_SIDE };
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(chunkCenter,(float3)Chunk.BoxSize * Constants.VOXEL_SIDE);

            //Show voxel
            if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hitInfo, 100.0f))
            {
                var voxelRealPosition = math.floor(hitInfo.point / Constants.VOXEL_SIDE) * Constants.VOXEL_SIDE + Constants.VOXEL_SIDE / 2;

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(voxelRealPosition, Vector3.one * Constants.VOXEL_SIDE);
            }
        }
    }

#endif
#endregion
}



