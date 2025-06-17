using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class TrialDefinitionGeneratorWindow : EditorWindow
{
    private ExperimentDefinition experimentDef;
    private TextAsset stimIndexCSV;
    private string trialIDPrefix = "WMTEST";
    private int totalTrials = 24;
    private int blocks = 1;
    private string outputFolder = "Assets/Configs";

    private Dictionary<string, StateConfig> stateConfigs = new Dictionary<string, StateConfig>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Trial Definition Generator")]
    public static void ShowWindow()
    {
        GetWindow<TrialDefinitionGeneratorWindow>("Trial Definition Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Trial Definition CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        experimentDef = (ExperimentDefinition)EditorGUILayout.ObjectField(
            "Experiment Definition", experimentDef, typeof(ExperimentDefinition), false);

        stimIndexCSV = (TextAsset)EditorGUILayout.ObjectField(
            "StimIndex CSV", stimIndexCSV, typeof(TextAsset), false);

        if (EditorGUI.EndChangeCheck())
        {
            stateConfigs.Clear();
        }

        trialIDPrefix = EditorGUILayout.TextField("TrialID Prefix", trialIDPrefix);
        totalTrials = Mathf.Max(1, EditorGUILayout.IntField("Total Trials", totalTrials));
        blocks = Mathf.Max(1, EditorGUILayout.IntField("Blocks", blocks));
        EditorGUILayout.HelpBox(
            "Total Trials must equal number of stimuli * Cue Repetitions",
            MessageType.Info);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        EditorGUILayout.Space();

        if (experimentDef != null)
        {
            List<StateDefinition> stimStates =
                experimentDef.States.Where(s => s.IsStimulus).ToList();

            scrollPos = EditorGUILayout.BeginScrollView(
                scrollPos,
                GUILayout.Height(400),
                GUILayout.ExpandWidth(true));

            foreach (StateDefinition st in stimStates)
            {
                if (!stateConfigs.ContainsKey(st.Name))
                {
                    stateConfigs[st.Name] = new StateConfig();
                }

                StateConfig cfg = stateConfigs[st.Name];
                EditorGUILayout.BeginVertical("box");

                cfg.foldout = EditorGUILayout.Foldout(
                    cfg.foldout,
                    st.Name,
                    true);

                if (cfg.foldout)
                {
                    // Cue settings
                    cfg.isCue = EditorGUILayout.Toggle(
                        "Is Cue (memory) state",
                        cfg.isCue);

                    if (cfg.isCue)
                    {
                        cfg.cueRepetitions = EditorGUILayout.IntField(
                            new GUIContent(
                                "Cue Repetitions per Stimulus",
                                "Repeat each stimulus this many times (random order)."),
                            cfg.cueRepetitions);
                    }

                    // Occurrence slider
                    cfg.percentage = EditorGUILayout.IntSlider(
                        "% Occurrence",
                        cfg.percentage,
                        0,
                        100);

                    EditorGUILayout.Space();

                    // Stimuli selection
                    cfg.randomStim = EditorGUILayout.Toggle(
                        "Random Stimuli",
                        cfg.randomStim);

                    if (cfg.randomStim)
                    {
                        cfg.expectedStimCount = EditorGUILayout.IntField(
                            new GUIContent(
                                "# Stimuli",
                                "Number of random picks per trial."),
                            cfg.expectedStimCount);
                    }
                    else
                    {
                        cfg.customIndices = CSVUtil.DrawIntListField(
                            "Custom Stim Indices (comma-separated)",
                            cfg.customIndices);
                    }

                    EditorGUILayout.Space();

                    // Locations selection
                    cfg.randomLocations = EditorGUILayout.Toggle(
                        "Random Locations",
                        cfg.randomLocations);

                    if (cfg.randomLocations)
                    {
                        cfg.locationMin = EditorGUILayout.Vector2Field(
                            new GUIContent(
                                "Min (X,Y)",
                                "Random location range: X and Y bounds."),
                            cfg.locationMin);

                        cfg.locationMax = EditorGUILayout.Vector2Field(
                            new GUIContent(
                                "Max (X,Y)",
                                "Random location range: X and Y bounds."),
                            cfg.locationMax);
                    }
                    else
                    {
                        cfg.customLocations =
                            CSVUtil.DrawVector3ListField(
                                "Custom Locs (x,y,z; separated by ;)",
                                cfg.customLocations);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        if (GUILayout.Button("Generate CSV"))
        {
            if (experimentDef == null || stimIndexCSV == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Assign both Experiment Definition and StimIndex CSV.",
                    "OK");

                return;
            }

            GenerateTrialDefinitionCSV();
        }
    }

    private void GenerateTrialDefinitionCSV()
    {
        // Parse StimIndex CSV
        string[] lines = stimIndexCSV.text
            .Split(new[] {'\n','\r'},
                   StringSplitOptions.RemoveEmptyEntries);

        var stimMap = new Dictionary<string,int>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length >= 2 &&
                int.TryParse(cols[0], out int idx))
            {
                stimMap[cols[1].Trim()] = idx;
            }
        }

        // Collect stimulus states
        List<StateDefinition> stimStates =
            experimentDef.States.Where(s => s.IsStimulus).ToList();

        // Validate cue settings
        foreach (StateDefinition st in stimStates)
        {
            StateConfig cfg = stateConfigs[st.Name];

            if (cfg.isCue)
            {
                if (cfg.cueRepetitions < 1)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Cue Repetitions must be >= 1.",
                        "OK");

                    return;
                }

                int expected = stimMap.Count * cfg.cueRepetitions;

                if (expected != totalTrials)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "TotalTrials must equal stimuli count * Repetitions.",
                        "OK");

                    return;
                }
            }
        }

        // Build CSV header
        var headerCols = new List<string> { "TrialID", "BlockCount" };

        foreach (StateDefinition st in stimStates)
        {
            headerCols.Add(st.Name + "StimIndices");
            headerCols.Add(st.Name + "StimLocations");
        }

        var outLines = new List<string> { string.Join(",", headerCols) };

        // Precompute sequences
        var allIndices = new Dictionary<string, List<List<int>>>();
        var allLocs    = new Dictionary<string, List<List<Vector3>>>();

        foreach (StateDefinition st in stimStates)
        {
            StateConfig cfg = stateConfigs[st.Name];
            allIndices[st.Name] =
                BuildStimIndicesSequence(stimMap, cfg, totalTrials);

            allLocs[st.Name] =
                BuildStimLocationsSequence(cfg, totalTrials);
        }

        // Sync last event with cue
        string cueName = stimStates
            .FirstOrDefault(s => stateConfigs[s.Name].isCue)
            ?.Name;

        if (!string.IsNullOrEmpty(cueName))
        {
            string lastName = stimStates.Last().Name;

            for (int t = 0; t < totalTrials; t++)
            {
                int cueIdx = allIndices[cueName][t].FirstOrDefault();
                var list = allIndices[lastName][t];

                if (list.Contains(cueIdx))
                {
                    list.Remove(cueIdx);
                }

                list.Insert(0, cueIdx);
            }
        }

        // Generate CSV rows
        for (int t = 0; t < totalTrials; t++)
        {
            var row = new List<string>
            {
                trialIDPrefix + (t + 1).ToString()
            };

            int blockNum = Mathf.FloorToInt(
                (float)t / ((float)totalTrials / blocks)) + 1;

            row.Add(blockNum.ToString());

            foreach (StateDefinition st in stimStates)
            {
                // StimIndices (quoted)
                List<int> idxs = allIndices[st.Name][t];
                string idxStr = "[" + string.Join(",", idxs) + "]";
                row.Add("\"" + idxStr + "\"");

                // StimLocations (quoted)
                List<Vector3> locs = allLocs[st.Name][t];
                IEnumerable<string> parts =
                    locs.Select(v => $"[{Mathf.RoundToInt(v.x)},{Mathf.RoundToInt(v.y)},{Mathf.RoundToInt(v.z)}]");
                string locStr = "[" + string.Join(",", parts) + "]";
                row.Add("\"" + locStr + "\"");
            }

            outLines.Add(string.Join(",", row));
        }

        // Write CSV file
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string path = Path.Combine(
            outputFolder,
            trialIDPrefix + "_Trial_Def.csv");

        File.WriteAllLines(path, outLines);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            "CSV created at " + path,
            "OK");
    }

    private List<List<int>> BuildStimIndicesSequence(
        Dictionary<string,int> stimMap,
        StateConfig cfg,
        int total)
    {
        var seq = new List<List<int>>(new List<int>[total]);

        if (cfg.isCue)
        {
            List<int> pool = stimMap.Values
                .SelectMany(i => Enumerable.Repeat(i, cfg.cueRepetitions))
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            for (int i = 0; i < total; i++)
            {
                seq[i] = new List<int> { pool[i] };
            }

            return seq;
        }

        bool[] occurs = Enumerable.Repeat(true, total).ToArray();

        if (cfg.percentage < 100)
        {
            int countOccur = Mathf.RoundToInt(
                total * cfg.percentage / 100f);

            occurs = Enumerable.Repeat(true, countOccur)
                      .Concat(Enumerable.Repeat(false, total - countOccur))
                      .OrderBy(x => Guid.NewGuid())
                      .ToArray();
        }

        for (int i = 0; i < total; i++)
        {
            if (!occurs[i])
            {
                seq[i] = new List<int>();
            }
            else if (cfg.randomStim)
            {
                seq[i] = stimMap.Values
                    .Except(cfg.excludeIndices)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(cfg.expectedStimCount)
                    .ToList();
            }
            else
            {
                seq[i] = new List<int>(cfg.customIndices);
            }
        }

        return seq;
    }

    private List<List<Vector3>> BuildStimLocationsSequence(
        StateConfig cfg,
        int total)
    {
        var seq = new List<List<Vector3>>(new List<Vector3>[total]);
        bool[] occurs = Enumerable.Repeat(true, total).ToArray();

        if (!cfg.isCue && cfg.percentage < 100)
        {
            int countOccur = Mathf.RoundToInt(
                total * cfg.percentage / 100f);

            occurs = Enumerable.Repeat(true, countOccur)
                      .Concat(Enumerable.Repeat(false, total - countOccur))
                      .OrderBy(x => Guid.NewGuid())
                      .ToArray();
        }

        for (int i = 0; i < total; i++)
        {
            if (!occurs[i])
            {
                seq[i] = new List<Vector3>();
                continue;
            }

            int count = cfg.isCue ? cfg.cueRepetitions : cfg.expectedStimCount;

            if (cfg.randomLocations)
            {
                var rng  = new System.Random(i);
                var used = new HashSet<(int, int)>();
                var locs = new List<Vector3>();

                int xMin = Mathf.CeilToInt(cfg.locationMin.x);
                int xMax = Mathf.FloorToInt(cfg.locationMax.x);
                int yMin = Mathf.CeilToInt(cfg.locationMin.y);
                int yMax = Mathf.FloorToInt(cfg.locationMax.y);

                for (int j = 0; j < count; j++)
                {
                    int xi, yi;
                    int attempts = 0;

                    do
                    {
                        xi = rng.Next(xMin, xMax + 1);
                        yi = rng.Next(yMin, yMax + 1);
                        attempts++;
                    }
                    while (!used.Add((xi, yi)) && attempts < 1000);

                    locs.Add(new Vector3(xi, yi, 0));
                }

                seq[i] = locs;
            }
            else
            {
                seq[i] = new List<Vector3>(cfg.customLocations);
            }
        }

        return seq;
    }

    [Serializable]
    private class StateConfig
    {
        public bool foldout           = true;
        public bool isCue             = false;
        public int  cueRepetitions    = 1;
        public int  percentage        = 100;
        public bool randomStim        = true;
        public int  expectedStimCount = 1;
        public List<int>    customIndices    = new List<int>();
        public List<int>    excludeIndices   = new List<int>();
        public bool randomLocations    = true;
        public Vector2 locationMin      = new Vector2(-4, -2);
        public Vector2 locationMax      = new Vector2(4,  2);
        public List<Vector3> customLocations = new List<Vector3>();
    }

        private static class CSVUtil
    {
        public static List<int> DrawIntListField(string label, List<int> list)
        {
            // Adjust label width so text field starts after full label
            float prevWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;

            string s = string.Join(",", list);
            s = EditorGUILayout.TextField(label, s);

            EditorGUIUtility.labelWidth = prevWidth;

            return s.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
                    .ToList();
        }

        public static List<Vector3> DrawVector3ListField(string label, List<Vector3> list)
        {
            // Adjust label width so text field starts after full label
            float prevWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;

            string s = string.Join(";", list.Select(v => $"{v.x},{v.y},{v.z}"));
            s = EditorGUILayout.TextField(label, s);

            EditorGUIUtility.labelWidth = prevWidth;

            var outList = new List<Vector3>();
            foreach (var part in s.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var c = part.Split(',');
                if (c.Length >= 3
                    && float.TryParse(c[0], out var x)
                    && float.TryParse(c[1], out var y)
                    && float.TryParse(c[2], out var z))
                {
                    outList.Add(new Vector3(x, y, z));
                }
            }
            return outList;
        }
    }
}
