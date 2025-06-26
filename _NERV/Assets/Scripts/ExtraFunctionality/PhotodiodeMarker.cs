using UnityEngine;
using System.Collections.Generic;

public class PhotodiodeMarker : MonoBehaviour
{
    [Tooltip("List of state names that trigger a flash")]
    public List<string> statesToFlash = new List<string>();

    [Tooltip("If empty, will auto-load from Resources/Prefabs/WhiteCube")]
    private GameObject markerPrefab;

    [Tooltip("Duration of the flash in seconds")]
    public float flashDuration = 0.05f;

    private GameObject _instance;
    private float _disableTime;

    void Awake()
    {
        // auto-load fallback
        if (markerPrefab == null)
            markerPrefab = Resources.Load<GameObject>("Prefabs/WhiteCube");

        if (markerPrefab != null)
        {
            _instance = Instantiate(markerPrefab, transform);
            _instance.SetActive(false);
        }
        else
        {
            Debug.LogError("[PhotodiodeMarker] Failed to load WhiteCube prefab from Resources/Prefabs/WhiteCube");
        }
    }

    void OnLogEvent(string stateName)
    {
        if (_instance != null && statesToFlash.Contains(stateName))
        {
            _instance.SetActive(true);
            _disableTime = Time.unscaledTime + flashDuration;
        }
    }

    void LateUpdate()
    {
        if (_instance != null && _instance.activeSelf && Time.unscaledTime >= _disableTime)
            _instance.SetActive(false);
    }
}
