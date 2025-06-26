using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Parses two CSVs:
///  - TrialDefFile: rows of TrialID,BlockCount,<StateName>StimIndices,<StateName>StimLocations,<StateName>Duration,...
///  - StimIndexFile (asset.text): rows of Index,PrefabName
/// Exposes both in-memory for TrialManager<TTK> and StimulusSpawner.
/// </summary>
public class GenericConfigManager : MonoBehaviour
{
    public static GenericConfigManager Instance { get; private set; }

    [Header("Auto-load CSVs from Resources")]
    [Tooltip("Leave blank to use current scene name")]
    public string Acronym;

    [Header("Stimuli Settings")]
    [Tooltip("Folder under Resources/ containing stimulus prefabs.")]
    public string StimuliFolderName = "Stimuli";


    [HideInInspector]
    public List<TrialData> Trials = new List<TrialData>();

    [HideInInspector]
    public Dictionary<int,string> StimIndexToFile = new Dictionary<int,string>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Find acronym from TrialManager{acr} 
        if (string.IsNullOrEmpty(Acronym))
        {
            var trialManagerGO = GameObject.FindObjectsOfType<MonoBehaviour>()
                .FirstOrDefault(m => m.GetType().Name.StartsWith("TrialManager"));

            if (trialManagerGO != null)
            {
                string typeName = trialManagerGO.GetType().Name;  // e.g., "TrialManagerWMR"
                if (typeName.Length > "TrialManager".Length)
                {
                    Acronym = typeName.Substring("TrialManager".Length);
                }
            }

            if (string.IsNullOrEmpty(Acronym))
                Debug.LogWarning("[GenericConfigManager] Acronym not detected from TrialManager name.");
        }

        // 2) Load the two CSVs from Resources/Configs/{Acronym}/
        var defText  = Resources.Load<TextAsset>($"Configs/{Acronym}/{Acronym}_Trial_Def");
        var stimText = Resources.Load<TextAsset>($"Configs/{Acronym}/{Acronym}_Stim_Index");
        if (defText == null || stimText == null)
        {
            Debug.LogError($"[GenericCFG] Missing CSVs for “{Acronym}” in Resources/Configs/{Acronym}");
            return;
        }

        // 3) Parse them
        LoadStimIndex(stimText);
        LoadTrialDefs(defText);

    }

    void LoadStimIndex(TextAsset asset)
    {
        var lines = asset.text.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        // check if first line is just a folder (no commas)
        int mapStart = 1;
        if (lines.Length > 0 && !lines[0].Contains(","))
        {
            StimuliFolderName = lines[0].Trim();
            mapStart = 2; // skip folder line + header row
        }

        for (int i = mapStart; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0].Trim(), out var idx))
                {
                    StimIndexToFile[idx] = parts[1].Trim().Trim('"');
                }
            }
        Debug.Log($"[GenericCFG] Loaded {StimIndexToFile.Count} stimulus mappings");
    }

    void LoadTrialDefs(TextAsset asset)
    {
        var lines   = asset.text.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );
        if (lines.Length < 2)
        {
            Debug.LogError("GenericConfigManager: not enough rows in TrialDefFile");
            return;
        }

        var headers = SplitCsvLine(lines[0]);
        var colMap  = new Dictionary<string,int>();
        for (int i = 0; i < headers.Length; i++)
            colMap[headers[i].Trim()] = i;

        if (!colMap.ContainsKey("TrialID") || !colMap.ContainsKey("BlockCount"))
        {
            Debug.LogError("GenericConfigManager: CSV missing TrialID or BlockCount");
            return;
        }

        for (int r = 1; r < lines.Length; r++)
        {
            var cols = SplitCsvLine(lines[r]);
            var t    = new TrialData
            {
                TrialID    = cols[colMap["TrialID"]].Trim(),
                BlockCount = int.Parse(cols[colMap["BlockCount"]].Trim())
            };

            // dynamically read any <StateName>StimIndices, StimLocations, Duration
            foreach (var kv in colMap)
            {
                string hdr  = kv.Key;
                int    c    = kv.Value;
                string cell = cols[c].Trim();

                try
                {
                    if (hdr.EndsWith("StimIndices", StringComparison.Ordinal))
                    {
                        string state = hdr.Substring(0, hdr.Length - "StimIndices".Length);
                        t.StimIndices[state] = ParseIntArray(cell);
                    }
                    else if (hdr.EndsWith("StimLocations", StringComparison.Ordinal))
                    {
                        string state = hdr.Substring(0, hdr.Length - "StimLocations".Length);
                        t.StimLocations[state] = ParseVector3Array(cell);
                    }
                    else if (hdr.EndsWith("Duration", StringComparison.Ordinal))
                    {
                        string state = hdr.Substring(0, hdr.Length - "Duration".Length);
                        t.Durations[state] = float.Parse(cell);
                    }
                }
                catch (FormatException ex)
                {
                    Debug.LogError($"[GenericCFG] CSV parse error at row {r + 1}, column '{hdr}', value='{cell}': {ex.Message}");
                    throw;
                }
            }

            Trials.Add(t);
        }

        Debug.Log($"[GenericCFG] Loaded {Trials.Count} trials");
    }

    //--- CSV helpers ---
    private string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var cur    = new StringBuilder();
        bool inQ   = false;
        foreach (char ch in line)
        {
            if (ch == '"') inQ = !inQ;
            else if (ch == ',' && !inQ)
            {
                fields.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(ch);
        }
        fields.Add(cur.ToString());
        return fields.ToArray();
    }

    private int[] ParseIntArray(string s)
    {
        s = s.Trim().TrimStart('[').TrimEnd(']');
        if (string.IsNullOrEmpty(s)) return Array.Empty<int>();
        var parts = s.Split(',');
        var arr   = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            arr[i] = int.Parse(parts[i].Trim());
        return arr;
    }

    private Vector3[] ParseVector3Array(string s)
    {
        s = s.Trim();
        // Empty or Blank
        if (string.IsNullOrEmpty(s) || s == "[]")
            return Array.Empty<Vector3>();

        // Single vector "[x,y,z]"?
        if (s.StartsWith("[") && s.EndsWith("]") && !s.Contains("],[") && !s.Contains("[["))
        {
            // remove outer [ ]
            string inner = s.Substring(1, s.Length - 2);
            var p = inner.Split(',');
            return new[] {
                new Vector3(
                    float.Parse(p[0].Trim()),
                    float.Parse(p[1].Trim()),
                    float.Parse(p[2].Trim())
                )
            };
        }

        // Single vector "[[x,y,z]]"?
        if (s.StartsWith("[[") && s.EndsWith("]]") && !s.Contains("],["))
        {
            // remove outer [[ ]]
            string inner = s.Substring(2, s.Length - 4);
            var p = inner.Split(',');
            return new[] {
                new Vector3(
                    float.Parse(p[0].Trim()),
                    float.Parse(p[1].Trim()),
                    float.Parse(p[2].Trim())
                )
            };
        }

        // Multi-vector "[[x,y,z],[x2,y2,z2],...]"?
        if (s.StartsWith("[") && s.EndsWith("]"))
            s = s.Substring(1, s.Length - 2);

        if (string.IsNullOrEmpty(s))
            return Array.Empty<Vector3>();

        var elems = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '[')
            {
                if (depth++ == 0) start = i;
            }
            else if (s[i] == ']')
            {
                if (--depth == 0)
                    elems.Add(s.Substring(start, i - start + 1));
            }
        }

        var outArr = new Vector3[elems.Count];
        for (int i = 0; i < elems.Count; i++)
        {
            var sub = elems[i].Trim('[', ']');
            var p   = sub.Split(',');
            outArr[i] = new Vector3(
                float.Parse(p[0].Trim()),
                float.Parse(p[1].Trim()),
                float.Parse(p[2].Trim())
            );
        }
        return outArr;
    }

}
