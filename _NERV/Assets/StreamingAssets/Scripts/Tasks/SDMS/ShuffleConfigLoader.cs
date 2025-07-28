using System;
using System.Collections.Generic;
using UnityEngine;

public class ShuffleConfigLoader : MonoBehaviour
{
    [Tooltip("Path under Resources (no .csv)")]
    public string resourcePath = "Configs/SDMS/Shuffle_Boolean";
    
    // 0/1 flags, one per trial row
    public List<int> ShuffleFlags { get; private set; }

    void Awake()
    {
        // Load the CSV
        TextAsset txt = Resources.Load<TextAsset>(resourcePath);
        if (txt == null)
        {
            Debug.LogError($"[ShuffleConfigLoader] Couldn't find Resources/{resourcePath}.csv");
            return;
        }

        // Split lines and parse
        var lines = txt.text
                      .Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
        ShuffleFlags = new List<int>(lines.Length - 1);
        for (int i = 1; i < lines.Length; i++)
        {
            if (int.TryParse(lines[i].Trim(), out int flag))
                ShuffleFlags.Add(flag);
            else
                Debug.LogWarning($"[ShuffleConfigLoader] Bad flag at line {i+1}: '{lines[i]}'");
        }

        Debug.Log($"[ShuffleConfigLoader] Loaded {ShuffleFlags.Count} shuffle flags");
    }
}
