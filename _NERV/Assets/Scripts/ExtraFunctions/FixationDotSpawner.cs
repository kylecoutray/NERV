using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FixationDotSpawner : MonoBehaviour
{
    [Tooltip("State names that trigger the fixation dot")]
    public List<string> statesToSpawn = new List<string>();

    [Tooltip("Fixation dot prefab; if left blank, auto-loads Resources/Prefabs/FixationDot")]
    public GameObject fixationDotPrefab;

    [Tooltip("World-unit diameter of the dot")]
    public float dotSize = 0.2f;

    [Tooltip("How long the dot stays visible (seconds)")]
    public float fixationDuration = 1.5f;

    private const string DefaultPath = "Prefabs/FixationDot";

    void Awake()
    {
        if (fixationDotPrefab == null)
        {
            fixationDotPrefab = Resources.Load<GameObject>(DefaultPath);
            if (fixationDotPrefab == null)
                Debug.LogError($"[FixationDotSpawner] Couldn't load FixationDot at Resources/{DefaultPath}");
        }
    }

    void OnLogEvent(string stateName)
    {
        if (fixationDotPrefab != null && statesToSpawn.Contains(stateName))
            StartCoroutine(SpawnAndDestroyDot());
    }

    private IEnumerator SpawnAndDestroyDot()
    {
        var dot = Instantiate(fixationDotPrefab, Vector3.zero, Quaternion.identity);
        dot.transform.localScale = Vector3.one * dotSize;
        yield return new WaitForSeconds(fixationDuration);
        Destroy(dot);
    }
}
