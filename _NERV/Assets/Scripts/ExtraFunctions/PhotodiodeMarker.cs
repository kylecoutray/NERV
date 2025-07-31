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
        if (markerPrefab == null)
            markerPrefab = Resources.Load<GameObject>("Prefabs/WhiteCube");

        if (markerPrefab != null)
        {
            Camera cam = Camera.main;

            // Spawn at screen's upper-left (z is distance from camera)
            Vector3 screenPos = new Vector3(0, Screen.height, cam.nearClipPlane + 1f);
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

            _instance = Instantiate(markerPrefab);
            _instance.transform.position = worldPos;
            _instance.transform.localScale = new Vector3(1f, 1f, 0.01f); // adjust if needed
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
