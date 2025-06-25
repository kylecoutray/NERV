using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

public class DependenciesManager : MonoBehaviour
{
    // 1) Singleton accessor
    public static DependenciesManager Instance { get; private set; }

    // 2) Drag your DependenciesContainer (child) here in the Inspector
    [Header("Drag your Dependencies Container here")]
    public DependenciesContainer DepsContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
       
    }

    /// <summary>
    /// Grab any dependency by type. E.g. Get<SessionManager>(), Get<LogManager>()…
    /// </summary>
public T Get<T>() where T : class
{
    // first try your main container
    var mainField = typeof(DependenciesContainer)
                   .GetFields(BindingFlags.Instance | BindingFlags.Public)
                   .FirstOrDefault(fi => typeof(T).IsAssignableFrom(fi.FieldType));
    if (mainField != null)
        return mainField.GetValue(DepsContainer) as T;

    // then try your logging sub‐container
    if (DepsContainer.LoggingContainer != null)
    {
        var logField = typeof(LoggingContainer)
                     .GetFields(BindingFlags.Instance | BindingFlags.Public)
                     .FirstOrDefault(fi => typeof(T).IsAssignableFrom(fi.FieldType));
        if (logField != null)
            return logField.GetValue(DepsContainer.LoggingContainer) as T;
    }

    return null;
}
}



