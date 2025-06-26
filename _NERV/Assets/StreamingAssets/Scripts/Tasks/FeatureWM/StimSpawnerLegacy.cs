using System.Collections.Generic;
using UnityEngine;

public class StimSpawnerLegacy : MonoBehaviour
{
    public float BaseStimulusScale = 1f;
    private Camera _mainCamera;
    private Dictionary<string, GameObject> _prefabDict;
    public Transform StimuliParent; // assign your StimuliRoot here

    void Awake()
    {
        _mainCamera = Camera.main ?? Camera.current;
        // (same as before) load Resources/Stimuli into _prefabDict and set StimuliParent
        var prefabs = Resources.LoadAll<GameObject>("Stimuli");
        _prefabDict = new Dictionary<string, GameObject>();
        foreach (var p in prefabs)
            _prefabDict[p.name] = p;

        if (StimuliParent == null)
        {
            var t = transform.Find("StimuliRoot");
            if (t != null) StimuliParent = t;
            else
            {
                var go = new GameObject("StimuliRoot");
                go.transform.SetParent(transform);
                StimuliParent = go.transform;
            }
        }
        Debug.Log($"[Spawner] Loaded {_prefabDict.Count} prefabs from Resources/Stimuli");

    }

    public List<GameObject> SpawnStimuli(int[] indices, Vector3[] locations)
    {
        ClearAll();

        if (indices.Length != locations.Length)
            Debug.LogWarning($"SpawnStimuli: indices length ({indices.Length}) != locations length ({locations.Length})");

        var spawned = new List<GameObject>();
        int count = Mathf.Min(indices.Length, locations.Length);

        for (int i = 0; i < count; i++)
        {
            int stimIndex = indices[i];

            if (ConfigManager.Instance.StimIndexToFile.TryGetValue(stimIndex, out var fileName)
                && _prefabDict.TryGetValue(fileName, out var prefab))
            {
                var go = Instantiate(prefab, locations[i], Quaternion.identity, StimuliParent);
                go.transform.localScale = Vector3.one * BaseStimulusScale;

                // add this:
                if (_mainCamera != null)
                {
                    // make the front (+Z) of the object face the camera
                    go.transform.LookAt(_mainCamera.transform.position);
                }

                // attach a CircleHalo and assign the prefab
                var haloHelper = go.GetComponent<CircleHalo>() 
               ?? go.AddComponent<CircleHalo>();



                // Tag it with its index
                var id = go.AddComponent<StimulusID>();
                id.Index = stimIndex;

                // ← Automatically add a BoxCollider if none exists
                if (go.GetComponent<Collider>() == null)
                    go.AddComponent<BoxCollider>();

                spawned.Add(go);

            }
            else
            {
                Debug.LogWarning($"No prefab mapping for stimulus index {stimIndex}");
            }
        }

        return spawned;
    }

    public void ClearAll()
    {
        if (StimuliParent == null) return;
        for (int c = StimuliParent.childCount - 1; c >= 0; c--)
            Destroy(StimuliParent.GetChild(c).gameObject);
    }
}