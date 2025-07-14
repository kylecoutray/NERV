using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LoggingContainer : MonoBehaviour
{
    public static LoggingContainer Instance { get; private set; }

    public SessionManager      SessionManager;
    public SessionLogManager   SessionLogManager;
    void Awake()
    {
        // 1) Singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 2) Detach from its parent (Dependencies) so only this root is carried over
        transform.SetParent(null, worldPositionStays: true);

        // 3) Now persist just this LoggingContainer across scenes
        DontDestroyOnLoad(gameObject);
    }
}