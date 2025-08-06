using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class TrialDefinitionGeneratorWindow : EditorWindow
{
    // Global random for locations to ensure unique sequences
    private bool headersOnly = false; // toggle for header-only that can be used to generate just the header row
    // this makes it easier to manually add custom state spawn logic
    private static readonly System.Random rngGlobal = new System.Random();
    private ExperimentDefinition experimentDef;
    private TextAsset stimIndexCSV;
    private string trialIDPrefix = "WMT_";
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
        EditorGUIUtility.labelWidth = 200; 
        GUILayout.Label("Generate Trial Definition CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Space(4);
        headersOnly = EditorGUILayout.ToggleLeft(
            new GUIContent(" Generate Headers Only",
                        "Skip all inputs and math; just output the CSV header row."),
            headersOnly);
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

        if (!headersOnly)
        {
            // Compute and display trial counts based on stimulus count and sample settings
            if (experimentDef != null && stimIndexCSV != null)
            {
                var lines = stimIndexCSV.text
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                int stimCount = Math.Max(0, lines.Length - 2);

                // Find sample state config
                var sampleState = experimentDef.States
                    .FirstOrDefault(s => stateConfigs.ContainsKey(s.Name) && stateConfigs[s.Name].isSample);

                if (sampleState != null)
                {
                    var cfgsample = stateConfigs[sampleState.Name];
                    int S = stimCount;
                    int M = Mathf.Max(1, cfgsample.sampleRepetitions);
                    int N = Mathf.Max(1, cfgsample.expectedStimCount);

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
                            "(StimCount * sample Repetitions) must be divisible by # sample Stimuli per Trial for balanced design.",
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
                        cfg.isSample = st.IsSample;
                        if (cfg.isSample)
                        {
                            cfg.sampleRepetitions = Mathf.Max(1,
                                EditorGUILayout.IntField(
                                    new GUIContent("Sample Repetitions per Stim",
                                                "Repeat each stimulus this many times per block."),
                                    cfg.sampleRepetitions));
                            cfg.expectedStimCount = Mathf.Clamp(
                                EditorGUILayout.IntField(
                                    new GUIContent("# Sample Stimuli per Trial",
                                                "Pick this many distinct stimuli each sample trial."),
                                    cfg.expectedStimCount),
                                1, int.MaxValue);
                        }

                        cfg.percentage = EditorGUILayout.IntSlider("% Occurrence", cfg.percentage, 0, 100);
                        EditorGUILayout.Space();
                        cfg.expectedStimCount = Mathf.Max(1,
                            EditorGUILayout.IntField(
                                new GUIContent("# Stimuli", "Number of stimuli per trial."),
                                cfg.expectedStimCount));
                        

                        cfg.randomStim = EditorGUILayout.Toggle(
                            new GUIContent("Random Order", "Shuffle selected stimuli each trial."),
                            cfg.randomStim);

                        // always show custom‑indices field
                        cfg.customIndices = CSVUtil.DrawIntListField(
                            "Custom Stim Indices (optional)",
                            cfg.customIndices);

                        // helper text
                        EditorGUILayout.HelpBox(
                            "If you enter any indices above, that exact list will be used each trial.",
                            MessageType.Info);

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
        if (headersOnly)
        {
            // rename locals to avoid conflicts
            var headerStimStates = experimentDef?.States
                                    .Where(s => s.IsStimulus)
                                    .ToList()
                                ?? new List<StateDefinition>();

            var headerColumns = new List<string> { "TrialID", "BlockCount" };
            foreach (var st in headerStimStates)
            {
                headerColumns.Add(st.Name + "StimIndices");
                headerColumns.Add(st.Name + "StimLocations");
            }

            // write just the header
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
            string headerPath = Path.Combine(outputFolder, $"{acr}_Trial_Def.csv");
            File.WriteAllLines(headerPath, new[] { string.Join(",", headerColumns) });
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Success",
                "Header CSV created at:\n" + headerPath,
                "OK"
            );
            return;
        }

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
        string sampleName = stimStates
            .FirstOrDefault(s => stateConfigs[s.Name].isSample)
            ?.Name;
        var cfgsample = (sampleName != null) ? stateConfigs[sampleName] : null;
        int S = stimMap.Count;
        int M = (cfgsample != null) ? cfgsample.sampleRepetitions : 1;
        int N = (cfgsample != null) ? cfgsample.expectedStimCount : 1;
        if (N < 1 || ((S * M) % N) != 0)
        {
            EditorUtility.DisplayDialog("Error",
                "Unbalanced design: (StimCount * Repetitions) must " +
                "be divisible by # sample Stimuli per Trial.",
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
            allLocs[st.Name] = BuildStimLocationsSequence(cfg, totalTrials, allIndices[st.Name]);
        }

        // Sync last event with sample
        if (!string.IsNullOrEmpty(sampleName))
        {
            string lastName = stimStates.Last().Name;
            for (int t = 0; t < totalTrials; t++)
            {
                var sampleList = allIndices[sampleName][t];
                var list    = allIndices[lastName][t];
                foreach (int sampleIdx in sampleList)
                {
                    if (list.Contains(sampleIdx)) list.Remove(sampleIdx);
                    list.Insert(0, sampleIdx);
                }
                var cfg = stateConfigs[lastName];
                int targetCount = cfg.randomStim
                    ? cfg.expectedStimCount
                    : cfg.customIndices.Count;
                if (cfg.isSample) targetCount = cfg.sampleRepetitions;
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
                // get the config for this state
                var cfg = stateConfigs[st.Name];

                // 1) StimIndices (unchanged)
                var idxs = allIndices[st.Name][t];
                row.Add("\"[" + string.Join(",", idxs) + "]\"");

                // 2) StimLocations (conditional formatting)
                var locs = allLocs[st.Name][t];
                var parts = locs.Select(v =>
                    cfg.randomLocations
                        // random → round to whole ints
                        ? $"[{Mathf.RoundToInt(v.x)},{Mathf.RoundToInt(v.y)},{Mathf.RoundToInt(v.z)}]"
                        // custom → preserve decimals (3‐place precision)
                        : $"[{v.x:F3},{v.y:F3},{v.z:F3}]"
                );
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
            // 1) StimIndices
            var idxs = allIndices[st.Name][trialIndex];
            rowList.Add("\"[" + string.Join(",", idxs) + "]\"");

            // 2) StimLocations — fetch the config to know whether to round
            var locs = allLocs[st.Name][trialIndex];
            var cfg  = stateConfigs[st.Name];                   // <-- HERE
            var parts = locs.Select(v =>
                cfg.randomLocations
                    ? $"[{Mathf.RoundToInt(v.x)},{Mathf.RoundToInt(v.y)},{Mathf.RoundToInt(v.z)}]"
                    : $"[{v.x:F3},{v.y:F3},{v.z:F3}]"
            );
            rowList.Add("\"[" + string.Join(",", parts) + "]\"");
        }
        return string.Join(",", rowList);
    }

    private List<List<int>> BuildStimIndicesSequence(
        Dictionary<string,int> stimMap,
        StateConfig cfg,
        int total)
    {
        // 1) Declare seq up front so it's always in scope
        var seq = new List<List<int>>(Enumerable
            .Range(0, total)
            .Select(_ => new List<int>())
            .ToList());

        // 2) sample logic: N samples per trial (or custom override)
        if (cfg.isSample)
        {
            // 1) Custom‑indices override
            if (cfg.customIndices != null && cfg.customIndices.Count > 0)
            {
                for (int t = 0; t < total; t++)
                    seq[t] = new List<int>(cfg.customIndices);
                return seq;
            }

            // 2) Build one‑block flat list of each stim × repetitions
            var flat = new List<int>();
            foreach (var idx in stimMap.Values)
                for (int r = 0; r < cfg.sampleRepetitions; r++)
                    flat.Add(idx);

            // 3) Shuffle it
            flat = flat.OrderBy(_ => Guid.NewGuid()).ToList();

            int N = cfg.expectedStimCount;
            // 4) Slice into trials, fixing any in‑trial duplicates
            for (int t = 0; t < total; t++)
            {
                // get the next N entries
                var slice = flat
                    .Skip(t * N)
                    .Take(N)
                    .ToList();

                // remove duplicates
                var unique = slice.Distinct().ToList();

                // if we lost some to duplication, fill from the remaining pool
                if (unique.Count < N)
                {
                    int need = N - unique.Count;
                    var pool = stimMap.Values.Except(unique).ToList();
                    unique.AddRange(
                        pool
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(need)
                    );
                }

                seq[t] = unique;
            }

            return seq;
        }

        // 3) Non‐sample logic (your original code)
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
            {
                seq[i] = new List<int>();
                continue;
            }

            // 1) Custom override
            if (cfg.customIndices != null && cfg.customIndices.Count > 0)
            {
                seq[i] = new List<int>(cfg.customIndices);
            }
            else
            {
                // 2) Otherwise pick N either shuffled or sequential
                var pool = stimMap.Values.Except(cfg.excludeIndices);
                if (cfg.randomStim)
                {
                    seq[i] = pool
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(cfg.expectedStimCount)
                        .ToList();
                }
                else
                {
                    seq[i] = pool
                        .Take(cfg.expectedStimCount)
                        .ToList();
                }
            }
        }

        return seq;
    }


    private List<List<Vector3>> BuildStimLocationsSequence(
        StateConfig cfg,
        int total,
        List<List<int>> indexSeq)
    {
        var seq = new List<List<Vector3>>(new List<Vector3>[total]);

        for (int i = 0; i < total; i++)
        {
            // If there are no indices this trial → no locations
            if (indexSeq[i] == null || indexSeq[i].Count == 0)
            {
                seq[i] = new List<Vector3>();
                continue;
            }

            // Number of locations needed matches number of indices
            int count = indexSeq[i].Count;

            if (cfg.randomLocations)
            {
                // --- random within bounds ---
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
                // --- custom locations override ---
                // If customLocations length matches count, use them directly;
                // otherwise you might choose to trim or repeat as needed.
                seq[i] = new List<Vector3>(cfg.customLocations.Take(count));
            }
        }

        return seq;
    }


    [Serializable]
    private class StateConfig
    {
        public bool foldout = true;
        public bool isSample = false;
        public int sampleRepetitions = 1;
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
