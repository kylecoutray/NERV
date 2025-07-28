using System;
using System.Collections;
using UnityEngine;

public class ShuffleManager : MonoBehaviour
{
    [Tooltip("Relative path under Resources. Defaults to 'Stimuli/Default'.")]
    public string resourcePath = "Stimuli/Default";

    [Tooltip("Empty GameObject to parent all shuffle instances.")]
    public Transform shuffleContainer;
    public float instanceScale = 1.0f;

    private GameObject[] _pool;

    void Awake()
    {
        // Load all stimulus prefabs at runtime
        GameObject[] stimuli = Resources.LoadAll<GameObject>(resourcePath);
        Debug.Log($"[ShuffleManager] Loaded {stimuli.Length} stimuli from Resources/{resourcePath}");
        if (stimuli == null || stimuli.Length == 0)
        {
            Debug.LogError($"[ShuffleManager] No stimuli found at Resources/{resourcePath}");
            return;
        }

        // Pre‑spawn & deactivate
        _pool = new GameObject[stimuli.Length];
        for (int i = 0; i < stimuli.Length; i++)
        {
            GameObject go = Instantiate(stimuli[i], shuffleContainer);
            go.transform.localScale = Vector3.one * instanceScale;
            go.SetActive(false);
            _pool[i] = go;
        }
    }

    /// <summary>
    /// Flickers random stimuli at the given local positions.
    /// </summary>
    public IEnumerator DoShuffle(
        int[] exclusion,       // sample indices to skip
        Vector3[] positions,   // three local positions
        float duration,        // total shuffle time
        float interval = 0.1f  // time between updates
    )
    {
        float endTime = Time.time + duration;
        var rnd = new System.Random();
        int N = _pool.Length;

        while (Time.time < endTime)
        {
            // deactivate all
            foreach (var go in _pool) go.SetActive(false);

            // pick and show 3 random stimuli
            for (int slot = 0; slot < positions.Length; slot++)
            {
                int idx;
                do
                {
                    idx = rnd.Next(0, N);
                }
                while (Array.IndexOf(exclusion, idx) >= 0);

                GameObject go = _pool[idx];
                go.transform.localPosition = positions[slot];
                go.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        // deactivate at end
        foreach (var go in _pool) go.SetActive(false);
    }
}
