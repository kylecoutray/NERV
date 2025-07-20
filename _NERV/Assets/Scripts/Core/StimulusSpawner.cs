using System.Collections.Generic;
using UnityEngine;

public class StimulusSpawner : MonoBehaviour
{
    public float BaseStimulusScale = 1f;
    private Camera _mainCamera;
    private Dictionary<string, GameObject> _prefabDict;
    public Transform StimuliParent; // assign your StimuliRoot here
    [Header("Located in: Assets/Resources/{Stimuli Folder}")]
    public string StimuliFolder = "Stimuli";

    void Awake()
    {
        _mainCamera = Camera.main ?? Camera.current;

        // decide which Resources folder to load from
        string folder = StimuliFolder;
        if (GenericConfigManager.Instance != null 
            && !string.IsNullOrEmpty(GenericConfigManager.Instance.StimuliFolderName))
        {
            folder = GenericConfigManager.Instance.StimuliFolderName;
        }

        // load all prefabs from Resources/{folder}
        var prefabs = Resources.LoadAll<GameObject>(folder);
        _prefabDict = new Dictionary<string, GameObject>();
        foreach (var p in prefabs)
            _prefabDict[p.name] = p;

        // ensure we have a parent container for spawned stimuli
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

        Debug.Log($"[Spawner] Loaded {_prefabDict.Count} prefabs from Resources/{folder}");
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

            if (GenericConfigManager.Instance.StimIndexToFile.TryGetValue(indices[i], out var fileName)
                && _prefabDict.TryGetValue(fileName, out var prefab))
            {
                var go = Instantiate(prefab, locations[i], Quaternion.identity, StimuliParent);
                go.transform.localScale = Vector3.one * BaseStimulusScale;
                
                if (_mainCamera != null)
                {
                    // horizontal billboard (only yaw) so the sprite always faces camera but stays flat
                    Vector3 dir = _mainCamera.transform.position - go.transform.position;
                    dir.y = 0;                        // zero out vertical tilt
                    if (dir.sqrMagnitude > 0.001f)
                        go.transform.rotation = Quaternion.LookRotation(dir);

                }



                // Tag it with its index
                var id = go.AddComponent<StimulusID>();
                id.Index = stimIndex;

                // ← Automatically add a BoxCollider if none exists
                if (go.GetComponent<Collider>() == null)
                {
                    var box = go.AddComponent<BoxCollider>();
                    // Set your desired properties
                    box.center = new Vector3(0f, 0.15f, 0f);
                    box.size   = new Vector3(1.15f, 1.6f, 1f);
                    box.isTrigger = false; // explicitly match inspector setting
                }

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
