using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
                GameLog.LogWarning(typeof(T).ToString() + " instance is NULL");
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            Init();
        }
        else if (_instance != this)
        {
            GameLog.LogWarning($"Duplicate {typeof(T)} detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public virtual void Init()
    {
        // Optional - override in derived classes
    }
}
