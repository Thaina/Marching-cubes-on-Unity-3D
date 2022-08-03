using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using Path = System.IO.Path;
using System.Runtime.InteropServices;
#else
using System.IO;
#endif

public class WorldManager : Singleton<WorldManager>
{
    [SerializeField]private string world = "default"; //World selected by the manager
    public const string WORLDS_DIRECTORY = "/worlds"; //Directory worlds (save folder,that contains the worlds folders)

    private static string WorldsDirectoryPath => Path.Combine(Application.persistentDataPath,WORLDS_DIRECTORY);
    private static string WorldPath(string world) => Path.Combine(Application.persistentDataPath,WORLDS_DIRECTORY,world);
    
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
#else
            if (!Directory.Exists(WorldsDirectoryPath))//in case worlds directory not created,create the "worlds" directory 
                Directory.CreateDirectory(WorldsDirectoryPath);

            if (!Directory.Exists(WorldPath(world)))//in case world not created,create the world (generate folder)
                Directory.CreateDirectory(WorldPath(world));
#endif
        }
    }

    /// <summary>
    /// Create and select a new world (save/load folder),a worldConfig can be passed as second optional parameter for being used by the Noisemanager (or empty for default one).
    /// </summary>
    public static bool CreateWorld(string worldName,NoiseManager.WorldConfig newWorldConfig = null)
    {
        var wPath = WorldPath(worldName);
#if UNITY_WEBGL && !UNITY_EDITOR
        if(LocalStorageKeys.Any((path) => path.StartsWith(wPath)))
#else
        if (!Directory.Exists(wPath))
            Directory.CreateDirectory(wPath);
        else
#endif
        {
            Debug.LogError("folder already exists");
            return false;
        }

        Instance.world = worldName;
        if(newWorldConfig == null)//Use the WorldConfig passed as parameter
            newWorldConfig = new NoiseManager.WorldConfig() { worldSeed = Random.Range(int.MinValue,int.MaxValue) };

        SaveFile(Path.Combine(worldName,"worldConfig.json"),JsonUtility.ToJson(newWorldConfig));
        return true;
    }

    public static string LoadFile(string key)
    {
        return LocalStorageLoad(WorldPath(key));
    }

    public static void SaveFile(string key,string value)
    {
        LocalStorageSave(WorldPath(key),value);
    }

    /// <summary>
    /// Delete a world (save/load folder) and remove all related files.
    /// </summary>
    public static bool DeleteWorld(string worldName)
    {
        return LocalStorageDeleteFolder(WorldPath(worldName));
    }

    /// <summary>
    /// Return the name of the selected world.
    /// </summary>
    public static string GetSelectedWorldName()
    {
        return Instance.world;
    }

    /// <summary>
    /// Return WorldConfig of the selected world.
    /// </summary>
    public static NoiseManager.WorldConfig GetSelectedWorldConfig()
    {
        string path = Path.Combine(Instance.world,"worldConfig.json");
        if (LoadFile(path) is string json && !string.IsNullOrEmpty(json))
            return JsonUtility.FromJson<NoiseManager.WorldConfig>(json);

        Debug.LogError("No worldConfig.json exist,generating a new one,using the default parameters.");
        var newWorldConfig = new NoiseManager.WorldConfig();
        newWorldConfig.worldSeed = Random.Range(int.MinValue,int.MaxValue);
        string worldConfig = JsonUtility.ToJson(newWorldConfig);
        SaveFile(path,worldConfig);
        return newWorldConfig;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    public static IEnumerable<string> LocalStorageKeys
    {
        get
        {
            int i = 0;
            while(i < LocalStorageLength())
            {
                yield return LocalStorageKey(i);
                i++;
            }
        }
    }

    public static IEnumerable<string> LocalStorageKeysReverse
    {
        get
        {
            int i = LocalStorageLength();
            while(i > 0)
            {
                i--;
                yield return LocalStorageKey(i);
            }
        }
    }

    [DllImport("__Internal")]
    private static extern void LocalStorageSave(string key,string value);

    [DllImport("__Internal")]
    private static extern string LocalStorageLoad(string key);

    [DllImport("__Internal")]
    private static extern void LocalStorageDelete(string key);

    [DllImport("__Internal")]
    private static extern void LocalStorageClear();

    [DllImport("__Internal")]
    private static extern int LocalStorageLength();

    [DllImport("__Internal")]
    private static extern string LocalStorageKey(int index);
#else
    private static void LocalStorageSave(string key,string value)
    {
        File.WriteAllText(key,value);
    }

    private static string LocalStorageLoad(string key)
    {
        return File.Exists(key) ? File.ReadAllText(key) : null;
    }

    private static void LocalStorageDelete(string key)
    {
        if(File.Exists(key))
            File.Delete(key);
    }

    private static void LocalStorageClear()
    {
        Directory.Delete(WorldsDirectoryPath,true);
    }
#endif

    private static bool LocalStorageDeleteFolder(string folder)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        bool any = false;
        foreach(var path in LocalStorageKeysReverse)
        {
            if(path.StartsWith(folder))
            {
                LocalStorageDelete(path);
                any = true;
            }
        }

        return any;
#else
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder,true);
            return true;
        }
        else
        {
            Debug.LogError("folder not exists");
            return false;
        }
#endif
    }
}
