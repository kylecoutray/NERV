using System;
using System.Collections.Generic;
using UnityEngine;

public class ShuffleConfigLoader : MonoBehaviour
{
    [Tooltip("Path under Resources (no .csv)")]
    public string resourcePath = "Configs/SDMS/SDMS_Trial_Def";

    // 0/1 flags, one per trial row
    public List<int> ShuffleFlags { get; private set; }

    void Awake()
    {
        // Load the CSV text
        TextAsset txt = Resources.Load<TextAsset>(resourcePath);
        if (txt == null)
        {
            Debug.LogError($"[ShuffleConfigLoader] Couldn't find Resources/{resourcePath}.csv");
            return;
        }

        // Split into lines, removing empties
        var lines = txt.text
                      .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            Debug.LogError("[ShuffleConfigLoader] CSV must have a header and at least one data row.");
            return;
        }

        // Parse header to find the ShuffleBooleans column
        var headers = lines[0].Split(',');
        int colIndex = Array.IndexOf(headers, "ShuffleBooleans");
        if (colIndex == -1)
        {
            Debug.LogError("[ShuffleConfigLoader] Header does not contain a 'ShuffleBooleans' column.");
            return;
        }

        // Prepare the list for flags
        ShuffleFlags = new List<int>(lines.Length - 1);

        // Process each data line
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length <= colIndex)
            {
                Debug.LogWarning($"[ShuffleConfigLoader] Line {i+1} is missing column {colIndex}: '{lines[i]}'");
                continue;
            }

            var raw = fields[colIndex].Trim();
            if (int.TryParse(raw, out int flag))
            {
                ShuffleFlags.Add(flag);
            }
            else
            {
                Debug.LogWarning($"[ShuffleConfigLoader] Bad flag at line {i+1}, column 'ShuffleBooleans': '{raw}'");
            }
        }

        Debug.Log($"[ShuffleConfigLoader] Loaded {ShuffleFlags.Count} shuffle flags from column 'ShuffleBooleans'");
    }
}
