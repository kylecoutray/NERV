using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class TrialDefinitionGeneratorWindow : EditorWindow
{
    // Global random for locations to ensure unique sequences
    private static readonly System.Random rngGlobal = new System.Random();
    private ExperimentDefinition experimentDef;
    private TextAsset stimIndexCSV;
    private string trialIDPrefix = "WMTEST";
    private int practiceTrials = 0; // number of practice trials to prepend
    private int blocks = 1;
    private string outputFolder = "Assets/Resources/Configs";
    private string acr = "ACR";

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
        acr = EditorGUILayout.TextField("Acronym (Like XYZ):", acr);
        stimIndexCSV = (TextAsset)EditorGUILayout.ObjectField(
            "StimIndex CSV", stimIndexCSV, typeof(TextAsset), false);
        if (EditorGUI.EndChangeCheck())
            stateConfigs.Clear();

        trialIDPrefix = EditorGUILayout.TextField("TrialID Prefix", trialIDPrefix);
        blocks = Mathf.Max(1, EditorGUILayout.IntField("Blocks", blocks));
        practiceTrials = Mathf.Max(0, EditorGUILayout.IntField("Practice Trials", practiceTrials));
        outputFolder = $"Assets/Resources/Configs/{acr}";
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        EditorGUILayout.Space();

        // Compute and display trial counts based on stimulus count and cue settings
        if (experimentDef != null && stimIndexCSV != null)
        {
            var lines = stimIndexCSV.text
                .Split(new[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
            int stimCount = Math.Max(0, lines.Length - 2);

            // Find cue state config
            var cueState = experimentDef.States
                .FirstOrDefault(s => stateConfigs.ContainsKey(s.Name) && stateConfigs[s.Name].isCue);

            if (cueState != null)
            {
                var cfgCue = stateConfigs[cueState.Name];
                int S = stimCount;
                int M = Mathf.Max(1, cfgCue.cueRepetitions);
                int N = Mathf.Max(1, cfgCue.expectedStimCount);

                if ((S * M) % N == 0)
                {
                    int trialsPerBlock = (S * M) / N;
                    int totalTrials = trialsPerBlock * blocks;

                    EditorGUILayout.LabelField("Stimuli Count", S.ToString());
                    EditorGUILayout.LabelField("Trials / Block", trialsPerBlock.ToString());
                    EditorGUILayout.LabelField("Computed Total Trials", totalTrials.ToString());
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "(StimCount * Cue Repetitions) must be divisible by # Cue Stimuli per Trial for balanced design.",
                        MessageType.Error);
                }
            }
        }

        // State configuration UI
        if (experimentDef != null)
        {
            var stimStates = experimentDef.States
                .Where(s => s.IsStimulus)
                .ToList();

            scrollPos = EditorGUILayout.BeginScrollView(
                scrollPos,
                GUILayout.Height(400),
                GUILayout.ExpandWidth(true));

            foreach (var st in stimStates)
            {
                if (!stateConfigs.ContainsKey(st.Name))
                    stateConfigs[st.Name] = new StateConfig();

                var cfg = stateConfigs[st.Name];
                EditorGUILayout.BeginVertical("box");
                cfg.foldout = EditorGUILayout.Foldout(cfg.foldout, st.Name);
                if (cfg.foldout)
                {
                    cfg.isCue = EditorGUILayout.Toggle("Is Cue (memory) state", cfg.isCue);
                    if (cfg.isCue)
                    {
                        cfg.cueRepetitions = Mathf.Max(1,
                            EditorGUILayout.IntField(
                                new GUIContent("Cue Repetitions per Stimulus",
                                              "Repeat each stimulus this many times per block."),
                                cfg.cueRepetitions));
                        cfg.expectedStimCount = Mathf.Clamp(
                            EditorGUILayout.IntField(
                                new GUIContent("# Cue Stimuli per Trial",
                                              "Pick this many distinct stimuli each cue trial."),
                                cfg.expectedStimCount),
                            1, int.MaxValue);
                    }

                    cfg.percentage = EditorGUILayout.IntSlider("% Occurrence", cfg.percentage, 0, 100);
                    EditorGUILayout.Space();

                    cfg.randomStim = EditorGUILayout.Toggle("Random Stimuli", cfg.randomStim);
                    if (cfg.randomStim)
                    {
                        cfg.expectedStimCount = Mathf.Max(1,
                            EditorGUILayout.IntField(
                                new GUIContent("# Stimuli", "Number of random picks per trial."),
                                cfg.expectedStimCount));
                    }
                    else
                    {
                        cfg.customIndices = CSVUtil.DrawIntListField(
                            "Custom Stim Indices (comma-separated)", cfg.customIndices);
                    }

                    EditorGUILayout.Space();

                    cfg.randomLocations = EditorGUILayout.Toggle("Random Locations", cfg.randomLocations);
                    if (cfg.randomLocations)
                    {
                        cfg.locationMin = EditorGUILayout.Vector2Field("Min (X,Y)", cfg.locationMin);
                        cfg.locationMax = EditorGUILayout.Vector2Field("Max (X,Y)", cfg.locationMax);
                    }
                    else
                    {
                        cfg.customLocations = CSVUtil.DrawVector3ListField(
                            "Custom Locs (x,y,z; separated by ;)", cfg.customLocations);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        // Generate button
        if (GUILayout.Button("Generate CSV"))
        {
            if (experimentDef == null || stimIndexCSV == null)
            {
                EditorUtility.DisplayDialog("Error",
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
        var lines = stimIndexCSV.text
            .Split(new[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
        var stimMap = new Dictionary<string,int>();
        for (int i = 2; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length >= 2 && int.TryParse(cols[0], out int idx))
                stimMap[cols[1].Trim()] = idx;
        }

        // Identify stimulus states
        var stimStates = experimentDef.States
            .Where(s => s.IsStimulus).ToList();

        // Compute trial counts
        string cueName = stimStates
            .FirstOrDefault(s => stateConfigs[s.Name].isCue)
            ?.Name;
        var cfgCue = (cueName != null) ? stateConfigs[cueName] : null;
        int S = stimMap.Count;
        int M = (cfgCue != null) ? cfgCue.cueRepetitions : 1;
        int N = (cfgCue != null) ? cfgCue.expectedStimCount : 1;
        if (N < 1 || ((S * M) % N) != 0)
        {
            EditorUtility.DisplayDialog("Error",
                "Unbalanced design: (StimCount * Repetitions) must " +
                "be divisible by # Cue Stimuli per Trial.",
                "OK");
            return;
        }
        int trialsPerBlock = (S * M) / N;
        int totalTrials     = trialsPerBlock * blocks;

        // Build CSV header
        var headerCols = new List<string> { "TrialID", "BlockCount" };
        foreach (var st in stimStates)
        {
            headerCols.Add(st.Name + "StimIndices");
            headerCols.Add(st.Name + "StimLocations");
        }
        var outLines = new List<string> { string.Join(",", headerCols) };

        // Precompute sequences
        var allIndices = new Dictionary<string, List<List<int>>>();
        var allLocs    = new Dictionary<string, List<List<Vector3>>>();
        foreach (var st in stimStates)
        {
            var cfg = stateConfigs[st.Name];
            allIndices[st.Name] = BuildStimIndicesSequence(stimMap, cfg, totalTrials);
            allLocs[st.Name]    = BuildStimLocationsSequence(cfg, totalTrials);
        }

        // Sync last event with cue
        if (!string.IsNullOrEmpty(cueName))
        {
            string lastName = stimStates.Last().Name;
            for (int t = 0; t < totalTrials; t++)
            {
                var cueList = allIndices[cueName][t];
                var list    = allIndices[lastName][t];
                foreach (int cueIdx in cueList)
                {
                    if (list.Contains(cueIdx)) list.Remove(cueIdx);
                    list.Insert(0, cueIdx);
                }
                var cfg = stateConfigs[lastName];
                int targetCount = cfg.randomStim
                    ? cfg.expectedStimCount
                    : cfg.customIndices.Count;
                if (cfg.isCue) targetCount = cfg.cueRepetitions;
                while (list.Count > targetCount)
                    list.RemoveAt(list.Count - 1);
            }
        }

        // Generate CSV rows
        for (int t = 0; t < totalTrials; t++)
        {
            var row = new List<string> { trialIDPrefix + (t + 1) };
            int blockNum = Mathf.FloorToInt((float)t / trialsPerBlock) + 1;
            row.Add(blockNum.ToString());

            foreach (var st in stimStates)
            {
                var idxs = allIndices[st.Name][t];
                row.Add("\"[" + string.Join(",", idxs) + "]\"");
                var locs = allLocs[st.Name][t];
                var parts = locs.Select(v =>
                    $"[{Mathf.RoundToInt(v.x)},{Mathf.RoundToInt(v.y)},{Mathf.RoundToInt(v.z)}]");
                row.Add("\"[" + string.Join(",", parts) + "]\"");
            }
            outLines.Add(string.Join(",", row));
        }

        // *** PRACTICE TRIALS LOGIC START ***
        if (practiceTrials > 0)
        {
            // sample unique trial indices without replacement
            var rng = new System.Random();
            var practiceIndices = Enumerable.Range(0, totalTrials)
                                     .OrderBy(_ => rng.Next())
                                     .Take(practiceTrials)
                                     .ToList();
            // prepend in sampled order
            for (int i = 0; i < practiceIndices.Count; i++)
            {
                int t = practiceIndices[i];
                // build a practice row with block 0
                string practiceRow = BuildRow(true, i + 1, t, stimStates, trialsPerBlock, totalTrials, allIndices, allLocs);
                outLines.Insert(1 + i, practiceRow);
            }
        }
        // *** PRACTICE TRIALS LOGIC END ***
        // Insert your custom logging logic here before writing the final CSV

        // Write CSV
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);
        string path = Path.Combine(outputFolder, $"{acr}_Trial_Def.csv");
        File.WriteAllLines(path, outLines);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "CSV created at " + path, "OK");
    }

        private string BuildRow(bool practiceID, int practiceIndex, int trialIndex, List<StateDefinition> stimStates, int trialsPerBlock, int totalTrials, Dictionary<string, List<List<int>>> allIndices, Dictionary<string, List<List<Vector3>>> allLocs)
    {
        var rowList = new List<string>();
        if (practiceID)
        {
            rowList.Add(trialIDPrefix + "P" + practiceIndex);
            rowList.Add("0");
        }
        else
        {
            rowList.Add(trialIDPrefix + (trialIndex + 1));
            int blockNum = Mathf.FloorToInt((float)trialIndex / trialsPerBlock) + 1;
            rowList.Add(blockNum.ToString());
        }
        foreach (var st in stimStates)
        {
            var idxs = allIndices[st.Name][trialIndex];
            rowList.Add("\"[" + string.Join(",", idxs) + "]\"");
            var locs = allLocs[st.Name][trialIndex];
            var parts = locs.Select(v =>
                $"[{Mathf.RoundToInt(v.x)},{Mathf.RoundToInt(v.y)},{Mathf.RoundToInt(v.z)}]");
            rowList.Add("\"[" + string.Join(",", parts) + "]\"");
        }
        return string.Join(",", rowList);
    }

    private List<List<int>> BuildStimIndicesSequence(
        Dictionary<string,int> stimMap,
        StateConfig cfg,
        int total)
    {
        var seq = new List<List<int>>(new List<int>[total]);
        if (cfg.isCue)
        {
            var pool = stimMap.Values.ToList();
            for (int i = 0; i < total; i++)
            {
                var chosen = cfg.randomStim
                    ? pool.OrderBy(_ => Guid.NewGuid())
                          .Take(cfg.expectedStimCount)
                          .ToList()
                    : new List<int>(cfg.customIndices);
                // repeat each cue M times
                var repSeq = new List<int>();
                for (int r = 0; r < cfg.cueRepetitions; r++)
                    repSeq.AddRange(chosen);
                seq[i] = cfg.randomStim
                    ? repSeq.OrderBy(_ => Guid.NewGuid()).ToList()
                    : repSeq;
            }
            return seq;
        }

        bool[] occurs = Enumerable.Repeat(true, total).ToArray();
        if (cfg.percentage < 100)
        {
            int countOccur = Mathf.RoundToInt(total * cfg.percentage / 100f);
            occurs = Enumerable
                .Repeat(true, countOccur)
                .Concat(Enumerable.Repeat(false, total - countOccur))
                .OrderBy(x => Guid.NewGuid())
                .ToArray();
        }

        for (int i = 0; i < total; i++)
        {
            if (!occurs[i])
                seq[i] = new List<int>();
            else if (cfg.randomStim)
                seq[i] = stimMap.Values
                    .Except(cfg.excludeIndices)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(cfg.expectedStimCount)
                    .ToList();
            else
                seq[i] = new List<int>(cfg.customIndices);
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
            int countOccur = Mathf.RoundToInt(total * cfg.percentage / 100f);
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

            int count = cfg.isCue ? cfg.expectedStimCount * cfg.cueRepetitions : cfg.expectedStimCount;
            if (cfg.randomLocations)
            {
                // compute bounds
                int xMin = Mathf.CeilToInt(cfg.locationMin.x);
                int xMax = Mathf.FloorToInt(cfg.locationMax.x);
                int yMin = Mathf.CeilToInt(cfg.locationMin.y);
                int yMax = Mathf.FloorToInt(cfg.locationMax.y);

                var locs = new List<Vector3>(count);
                var used = new HashSet<(int, int)>();
                for (int j = 0; j < count; j++)
                {
                    int xi, yi, attempts = 0;
                    do
                    {
                        xi = rngGlobal.Next(xMin, xMax + 1);
                        yi = rngGlobal.Next(yMin, yMax + 1);
                        attempts++;
                    } while (!used.Add((xi, yi)) && attempts < 1000);
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
        public bool foldout = true;
        public bool isCue = false;
        public int cueRepetitions = 1;
        public int percentage = 100;
        public bool randomStim = true;
        public int expectedStimCount = 1;
        public List<int> customIndices = new List<int>();
        public List<int> excludeIndices = new List<int>();
        public bool randomLocations = true;
        public Vector2 locationMin = new Vector2(-4, -2);
        public Vector2 locationMax = new Vector2(4, 2);
        public List<Vector3> customLocations = new List<Vector3>();
    }

    private static class CSVUtil
    {
        public static List<int> DrawIntListField(string label, List<int> list)
        {
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
            float prevWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;
            string s = string.Join(";", list.Select(v => $"{v.x},{v.y},{v.z}"));
            s = EditorGUILayout.TextField(label, s);
            EditorGUIUtility.labelWidth = prevWidth;

            var outList = new List<Vector3>();
            foreach (var part in s.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var c = part.Split(',');
                if (c.Length >= 3 &&
                    float.TryParse(c[0], out var x) &&
                    float.TryParse(c[1], out var y) &&
                    float.TryParse(c[2], out var z))
                {
                    outList.Add(new Vector3(x, y, z));
                }
            }
            return outList;
        }
    }
}
