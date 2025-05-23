﻿using UnityEngine;

/// <summary>
/// Singleton. See <a href="https://github.com/UnityCommunity/UnitySingleton">Unity Singleton</a>
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    #region Fields
    /// <summary>
    /// The instance.
    /// </summary>
    private static T instance;

    #endregion

    #region Properties
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (instance == null)
                {
                    var obj = new GameObject();
                    obj.name = typeof(T).Name;
                    instance = obj.AddComponent<T>();
                    //DontDestroyOnLoad(instance);
                }
            }
            return instance;
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Use this for initialization.
    /// </summary>
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            //DontDestroyOnLoad(gameObject);
        }
        else if(instance != this)
        {
            Destroy(gameObject);
        }
    }

    //Destroy singleton instance on destroy
    public void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Check if singleton is already created
    /// </summary>
    public static bool IsCreated()
    {
        return (instance != null);
    }

    #endregion
}