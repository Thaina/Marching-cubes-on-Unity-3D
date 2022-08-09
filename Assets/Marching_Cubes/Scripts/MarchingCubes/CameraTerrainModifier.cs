using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class CameraTerrainModifier : MonoBehaviour
{
    public Text textSize;
    public Text textMaterial;
    [Tooltip("Range where the player can interact with the terrain")]
    public float rangeHit = 100;
    [Tooltip("Force of modifications applied to the terrain")]
    public float modiferStrengh = 10;
    [Tooltip("Size of the brush, number of vertex modified")]
    public int sizeHit = 6;
    [Tooltip("Color of the new voxels generated")][Range(0, Constants.NUMBER_MATERIALS-1)]
    public int buildingMaterial = 0;

    private RaycastHit hit;
    private ChunkManager chunkManager;

    void Awake()
    {
        chunkManager = ChunkManager.Instance;
        UpdateUI();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    int lockingState;
    [DllImport("__Internal")]
    private static extern bool PointerLocked();
#endif

    public int Mode { get; set; }
    void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if(PointerLocked())
            lockingState = lockingState > 1 ? 0 : 1;
        else lockingState = lockingState < 2 ? 2 : 3;

        Cursor.visible = lockingState > 1;
        if(lockingState == 2)
#else
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        if(Input.GetKey(KeyCode.Escape))
#endif
        {
            Cursor.lockState = CursorLockMode.None;
        }

        bool locked = Cursor.lockState == CursorLockMode.Locked;
        if(!locked && Input.GetMouseButtonDown(0))
        {
            if(EventSystem.current is var eventSystem && eventSystem != null)
            {
                var cache = new List<RaycastResult>();
                eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = Input.mousePosition },cache);
                if(cache.Count < 1)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            else Cursor.lockState = CursorLockMode.Locked;
		}

        if(!Cursor.visible)
        {
            int modification = 0;
            if(Input.GetMouseButton(0))
                modification += 1;
            if(Input.GetMouseButton(1))
                modification -= 1;

            if(modification != 0 && Physics.Raycast(transform.position, transform.forward, out hit, rangeHit))
            {
                if(Mode == 0)
                    chunkManager.ModifyChunkData(hit.point, sizeHit,modification * modiferStrengh, buildingMaterial);
                else chunkManager.AddSnow(hit.point, Constants.MAX_HEIGHT - 16, sizeHit * (modification > 0 ? 3 : 5),modification * modiferStrengh);
            }

            //Inputs
            var scroll = new int2(Input.mouseScrollDelta);
            if(scroll.y != 0)
            {
                if(Input.GetKey(KeyCode.LeftShift))
                    buildingMaterial += scroll.y;
                else sizeHit += scroll.y;

                UpdateUI();
            }
        }
    }
    
    public void UpdateUI()
    {
        sizeHit = Mathf.Clamp(sizeHit,1,10);
        buildingMaterial = Mathf.Clamp(buildingMaterial,0,Constants.NUMBER_MATERIALS - 1);

        textSize.text = "(Mouse wheel) Brush size: " + sizeHit;
        textMaterial.text = "(Shift Mouse wheel) material: " + buildingMaterial;
    }
}
