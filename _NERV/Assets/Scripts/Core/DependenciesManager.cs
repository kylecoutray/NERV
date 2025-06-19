using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DependenciesManager : MonoBehaviour
{
    private static DependenciesManager instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);  // This is a duplicate; remove it
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
