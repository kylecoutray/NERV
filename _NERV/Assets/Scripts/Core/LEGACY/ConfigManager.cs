using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;


public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    public TextAsset StimIndexFile;      // assign in Inspector
    public TextAsset TrialDefFile;       // assign in Inspector

    public Dictionary<int, string> StimIndexToFile = 
        new Dictionary<int, string>();
    public List<TrialConfig> Trials = new List<TrialConfig>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            LoadStimIndex();
            LoadTrialDefs();
        }
        else
        {
            Destroy(gameObject);
        }
    }




    void LoadStimIndex()
    {
        var lines = StimIndexFile.text
                    .Split(new[] { "\r\n", "\n" }, 
                            StringSplitOptions.RemoveEmptyEntries);

        // skip header row at index 0
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            // split on commas (your FileName has no embedded commas)
            var cols = line.Split(',');

            if (cols.Length < 2)
            {
                Debug.LogWarning($"LoadStimIndex: malformed line {i}: \"{line}\"");
                continue;
            }

            // clean up whitespace or quotes
            string idxStr  = cols[0].Trim().Trim('\"');
            string fname   = cols[1].Trim().Trim('\"');

            if (int.TryParse(idxStr, out int idx))
            {
                StimIndexToFile[idx] = fname;
            }
            else
            {
                Debug.LogWarning($"LoadStimIndex: invalid index on line {i}: \"{idxStr}\"");
            }
        }

        Debug.Log($"Loaded {StimIndexToFile.Count} stimulus mappings");
    }

        /// Splits a CSV line into fields, keeping quoted commas intact.
    private string[] SplitCsvLine(string line)
    {
        var fields   = new List<string>();
        var current  = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }
    void LoadTrialDefs()
    {
        var lines = TrialDefFile.text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // skip header
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);

            if (cols.Length < 12)
            {
                Debug.LogWarning($"[Config] Line {i} has {cols.Length} fields, expected 12. Skipping.");
                continue;
            }

            // trim whitespace and outer quotes
            for (int j = 0; j < cols.Length; j++)
                cols[j] = cols[j].Trim().Trim('\"');

            var t = new TrialConfig
            {
                TrialID                          = cols[0],
                BlockCount                       = int.Parse(cols[1]),
                DisplaySampleDuration            = float.Parse(cols[2]),
                PostSampleDelayDuration          = float.Parse(cols[3]),
                DisplayPostSampleDistractorsDuration = float.Parse(cols[4]),
                PreTargetDelayDuration           = float.Parse(cols[5]),
                SampleStimLocation               = ParseVector3(cols[6]),
                SearchStimIndices                = ParseIntArray(cols[7]),
                SearchStimLocations              = ParseVector3Array(cols[8]),
                SearchStimTokenReward            = ParseIntArray(cols[9]),
                PostSampleDistractorStimIndices  = ParseIntArray(cols[10]),
                PostSampleDistractorStimLocations= ParseVector3Array(cols[11])
            };

            Trials.Add(t);
        }

        Debug.Log($"Loaded {Trials.Count} trial definitions");
    }


    Vector3 ParseVector3(string s)
    {
        // expects something like "[x, y, z]"
        s = s.Trim();
        if (s.StartsWith("[") && s.EndsWith("]"))
            s = s.Substring(1, s.Length - 2);
        var parts = s.Split(',');
        return new Vector3(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2])
        );
    }

    int[] ParseIntArray(string s)
    {
        s = s.Trim().TrimStart('[').TrimEnd(']');
        if (string.IsNullOrEmpty(s)) return new int[0];
        var parts = s.Split(',');
        var arr = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            arr[i] = int.Parse(parts[i]);
        return arr;
    }

    Vector3[] ParseVector3Array(string s)
    {
        // expects something like "[[x1,y1,z1],[x2,y2,z2],...]"
        s = s.Trim();
        if (s.StartsWith("[") && s.EndsWith("]"))
            s = s.Substring(1, s.Length - 2);

        if (string.IsNullOrEmpty(s))
            return new Vector3[0];

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

        var vecs = new Vector3[elems.Count];
        for (int i = 0; i < elems.Count; i++)
            vecs[i] = ParseVector3(elems[i]);
        return vecs;
    }
}
