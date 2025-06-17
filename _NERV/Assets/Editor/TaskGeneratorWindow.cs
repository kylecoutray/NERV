using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using System;

public class TaskGeneratorWindow : EditorWindow
{
    ExperimentDefinition experimentDef;
    GameObject dependenciesPrefab;

    [MenuItem("Tools/Task Generator")]
    public static void ShowWindow() => GetWindow<TaskGeneratorWindow>("Task Generator");

    void OnGUI()
    {
        GUILayout.Label("Generate TrialManager from ExperimentDefinition", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        experimentDef = (ExperimentDefinition)EditorGUILayout.ObjectField(
            "Experiment Definition",
            experimentDef,
            typeof(ExperimentDefinition),
            false
        );

        dependenciesPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Dependencies Prefab",
            dependenciesPrefab,
            typeof(GameObject),
            false
        );

        EditorGUILayout.Space();
        if (experimentDef == null || dependenciesPrefab == null)
        {
            EditorGUILayout.HelpBox("Assign both an ExperimentDefinition asset and a Dependencies prefab.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Generate TrialManager"))
            GenerateTrialManager();
    }

    void GenerateTrialManager()
    {
        // 0) Validate
        string acr = experimentDef.Acronym.Trim();
        if (string.IsNullOrEmpty(acr))
        {
            Debug.LogError("ExperimentDefinition.Acronym is empty!");
            return;
        }

        // 1) New empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        scene.name = experimentDef.name;
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{experimentDef.name}.unity");

        // 2) Instantiate Dependencies
        PrefabUtility.InstantiatePrefab(dependenciesPrefab);

        // 3) Script folder
        string folder = $"Assets/Scripts/Tasks/{acr}/";
        Directory.CreateDirectory(folder);

        // 4) Build TrialManager<ACR>.cs
        var sb = new StringBuilder();

        // --- usings ---
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();

        // --- enum ---
        sb.AppendLine($"public enum TrialState{acr}");
        sb.AppendLine("{");
        foreach (var st in experimentDef.States)
            sb.AppendLine($"    {st.Name},");
        sb.AppendLine("}");
        sb.AppendLine();

        // --- class header ---
        sb.AppendLine($"public class TrialManager{acr} : MonoBehaviour");
        sb.AppendLine("{");

        // References
        sb.AppendLine("    [Header(\"References\")]");
        sb.AppendLine("    public Camera PlayerCamera;");
        sb.AppendLine("    public StimulusSpawner Spawner;");
        sb.AppendLine("    public TMP_Text FeedbackText;");
        sb.AppendLine("    public TMP_Text ScoreText;");
        sb.AppendLine("    public GameObject CoinUI;");
        sb.AppendLine();

        // Timing & Scoring
        sb.AppendLine("    [Header(\"Timing & Scoring\")]");
        sb.AppendLine("    public float MaxChoiceResponseTime = 10f;");
        sb.AppendLine("    public float FeedbackDuration = 1f;");
        sb.AppendLine("    public int PointsPerCorrect = 2;");
        sb.AppendLine("    public int PointsPerWrong = -1;");
        sb.AppendLine();

        //   floats for every IsDelay state
        foreach (var st in experimentDef.States.Where(s => s.IsDelay))
            sb.AppendLine($"    public float {st.Name}Duration = {st.PostStateDelay}f;");
        sb.AppendLine();

        // Private fields
        sb.AppendLine("    private List<TrialData> _trials;");
        sb.AppendLine("    private int _currentIndex;");
        sb.AppendLine("    private int _score = 0;");
        sb.AppendLine();

        // Audio
        sb.AppendLine("    private AudioSource _audioSrc;");
        sb.AppendLine("    private AudioClip _correctBeep, _errorBeep, _coinBarFullBeep;");
        sb.AppendLine();

        // Coin Feedback
        sb.AppendLine("    [Header(\"Coin Feedback\")]");
        sb.AppendLine("    public bool UseCoinFeedback = true;");
        sb.AppendLine("    public int CoinsPerCorrect = 2;");
        sb.AppendLine();

        // UI Toggles
        sb.AppendLine("    [Header(\"UI Toggles\")]");
        sb.AppendLine("    public bool ShowScoreUI = true;");
        sb.AppendLine("    public bool ShowFeedbackUI = true;");
        sb.AppendLine();

        // TTL dictionary (dynamic)
        sb.AppendLine("    private Dictionary<string,int> TTLEventCodes = new Dictionary<string,int>");
        sb.AppendLine("    {");
        foreach (var st in experimentDef.States.Where(s => s.IsTTL))
            sb.AppendLine($"        {{ \"{st.Name}\", {st.TTLCode} }},");
        sb.AppendLine("    };");
        sb.AppendLine();

        // --- Start() ---
        sb.AppendLine("    void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (Spawner == null) Spawner = FindObjectOfType<StimulusSpawner>();");
        sb.AppendLine("        _trials       = GenericConfigManager.Instance.Trials;");
        sb.AppendLine("        _currentIndex = 0;");
        sb.AppendLine("        UpdateScoreUI();");
        sb.AppendLine("        _audioSrc     = GetComponent<AudioSource>();");
        sb.AppendLine("        _correctBeep  = Resources.Load<AudioClip>(\"AudioClips/correctBeep\");");
        sb.AppendLine("        _errorBeep    = Resources.Load<AudioClip>(\"AudioClips/errorBeep\");");
        sb.AppendLine("        _coinBarFullBeep = Resources.Load<AudioClip>(\"AudioClips/coinBarFullBeep\");");
        sb.AppendLine("        if (CoinUI     != null) CoinUI.SetActive(UseCoinFeedback);");
        sb.AppendLine("        if (FeedbackText!= null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);");
        sb.AppendLine("        if (ScoreText   != null) ScoreText.gameObject.SetActive(ShowScoreUI);");
        sb.AppendLine("        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);");
        sb.AppendLine("        LogTTL(\"StartEndBlock\");");
        sb.AppendLine("        StartCoroutine(RunTrials());");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- RunTrials() ---
        sb.AppendLine("    IEnumerator RunTrials()");
        sb.AppendLine("    {");
        sb.AppendLine("        while (_currentIndex < _trials.Count)");
        sb.AppendLine("        {");
        sb.AppendLine("            var trial = _trials[_currentIndex];");
        sb.AppendLine("            var spawnedItems = new List<GameObject>();");
        sb.AppendLine("            int[] lastIdxs = new int[0];");
        sb.AppendLine();

        int stimCount = 1;
        // per‐state
        foreach (var st in experimentDef.States)
        {

            sb.AppendLine($"            // — {st.Name.ToUpper()} —");
            sb.AppendLine($"            LogTTL(\"{st.Name}\");");

            // Stimulus
            if (st.IsStimulus)
            {
                sb.AppendLine("            //   the next 7 lines are because of the IsStimulus checkmark.");
                sb.AppendLine($"            var idxs{stimCount} = trial.GetStimIndices(\"{st.Name}\");");
                sb.AppendLine($"            var locs{stimCount} = trial.GetStimLocations(\"{st.Name}\");");
                sb.AppendLine($"            if (idxs{stimCount}.Length > 0 && locs{stimCount}.Length > 0)");
                sb.AppendLine("            {");
                sb.AppendLine($"                var goList{stimCount} = Spawner.SpawnStimuli(idxs{stimCount}, locs{stimCount});");
                sb.AppendLine($"                spawnedItems.AddRange(goList{stimCount});");
                sb.AppendLine($"                lastIdxs = idxs{stimCount};");
                sb.AppendLine("            }");
                sb.AppendLine();
                stimCount++;
            }

            // ClearAll (for states like SampleOff or Reset)
            if (st.IsClearAll && !st.IsChoice && !st.IsFeedback)
            {
                sb.AppendLine("            //   the stuff below is because of the IsClearAll checkmark");
                sb.AppendLine("            Spawner.ClearAll();");
                sb.AppendLine();
            }

            // Choice
            if (st.IsChoice)
            {

                sb.AppendLine("            bool answered   = false;");
                sb.AppendLine("            int  pickedIdx  = -1;");
                sb.AppendLine("            float reactionT = 0f;");
                sb.AppendLine("            yield return StartCoroutine(WaitForChoice((i, rt) =>");
                sb.AppendLine("            {");
                sb.AppendLine("                answered  = true;");
                sb.AppendLine("                pickedIdx = i;");
                sb.AppendLine("                reactionT = rt;");
                sb.AppendLine("            }));");
                sb.AppendLine();
                sb.AppendLine("            // strip out destroyed references");
                sb.AppendLine("            spawnedItems.RemoveAll(go => go == null);");
                sb.AppendLine("            GameObject targetGO = spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx);");
                sb.AppendLine();
            }

            // Feedback
            if (st.IsFeedback)
            {
                sb.AppendLine("            //   the blocks below are because of the IsFeedback checkmark");
                sb.AppendLine("            // — feedback and beep —");
                sb.AppendLine($"            bool correct = answered && (pickedIdx == (lastIdxs.Length > 0 ? lastIdxs[0] : -1));");
                sb.AppendLine("            if (correct)");
                sb.AppendLine("            {");
                sb.AppendLine("                LogTTL(\"SelectingTarget\");");
                sb.AppendLine("                _score += PointsPerCorrect;");
                sb.AppendLine("                if (!CoinController.Instance.CoinBarWasJustFilled)");
                sb.AppendLine("                    _audioSrc.PlayOneShot(_correctBeep);");
                sb.AppendLine("                LogTTL(\"AudioPlaying\");");
                sb.AppendLine("                LogTTL(\"Success\");");
                sb.AppendLine("                FeedbackText.text = $\"+{PointsPerCorrect}\";");
                sb.AppendLine("            }");
                sb.AppendLine("            else");
                sb.AppendLine("            {");
                sb.AppendLine("                _score += PointsPerWrong;");
                sb.AppendLine("                UpdateScoreUI();");
                sb.AppendLine("                _audioSrc.PlayOneShot(_errorBeep);");
                sb.AppendLine("                LogTTL(\"AudioPlaying\");");
                sb.AppendLine("                if (answered) { LogTTL(\"Choice\"); LogTTL(\"Fail\"); }");
                sb.AppendLine("                else          { LogTTL(\"Timeout\"); LogTTL(\"Fail\"); }");
                sb.AppendLine("                FeedbackText.text = answered ? \"Wrong!\" : \"Too Slow!\";");
                sb.AppendLine("            }");
                sb.AppendLine("            Vector2 clickScreenPos = Input.mousePosition;");
                sb.AppendLine("            if (pickedIdx >= 0 && UseCoinFeedback)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);");
                sb.AppendLine("                else         CoinController.Instance.RemoveCoins(1);");
                sb.AppendLine("            }");
                sb.AppendLine("            UpdateScoreUI();");
                sb.AppendLine("            if (ShowFeedbackUI) FeedbackText.canvasRenderer.SetAlpha(1f);");
                sb.AppendLine("            yield return new WaitForSeconds(FeedbackDuration);");
                sb.AppendLine();

            }

            // Delay
            if (st.IsDelay)
            {
                sb.AppendLine($"            yield return new WaitForSeconds({st.Name}Duration);");
            }
            else
            {
                sb.AppendLine("            yield return null;");
            }
        }
        // Normal increment if not Reset
        sb.AppendLine("            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);");
        sb.AppendLine("            _currentIndex++;");
        sb.AppendLine("        }");
        sb.AppendLine("        // end of all trials");
        sb.AppendLine("        _currentIndex--;");
        sb.AppendLine("        LogTTL(\"StartEndBlock\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- Helpers ---
        sb.AppendLine("    IEnumerator ShowFeedback()");
        sb.AppendLine("    {");
        sb.AppendLine("        FeedbackText.canvasRenderer.SetAlpha(1f);");
        sb.AppendLine("        yield return new WaitForSeconds(FeedbackDuration);");
        sb.AppendLine("        if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void UpdateScoreUI()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ShowScoreUI && ScoreText != null) ScoreText.text = $\"Score: {_score}\";");
        sb.AppendLine("        else if (ScoreText != null) ScoreText.text = \"\";");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private IEnumerator WaitForChoice(System.Action<int, float> callback)");
        sb.AppendLine("    {");
        sb.AppendLine("        float startTime = Time.time;");
        sb.AppendLine("        while (Time.time - startTime < MaxChoiceResponseTime)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (Input.GetMouseButtonDown(0))");
        sb.AppendLine("            {");
        sb.AppendLine("                var ray = PlayerCamera.ScreenPointToRay(Input.mousePosition);");
        sb.AppendLine("                if (Physics.Raycast(ray, out var hit))");
        sb.AppendLine("                {");
        sb.AppendLine("                    var stimID = hit.collider.GetComponent<StimulusID>();");
        sb.AppendLine("                    if (stimID != null)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        float rt = Time.time - startTime;");
        sb.AppendLine("                        callback(stimID.Index, rt);");
        sb.AppendLine("                        yield break;");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        callback(-1, MaxChoiceResponseTime);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (Input.GetKeyDown(KeyCode.S))");
        sb.AppendLine("        {");
        sb.AppendLine("            ShowScoreUI    = !ShowScoreUI;");
        sb.AppendLine("            ShowFeedbackUI = !ShowFeedbackUI;");
        sb.AppendLine("            if (ScoreText    != null) ScoreText.gameObject.SetActive(ShowScoreUI);");
        sb.AppendLine("            if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void LogTTL(string label)");
        sb.AppendLine("    {");
        sb.AppendLine("        LogManager.Instance.LogEvent(label, _trials[_currentIndex].TrialID);");
        sb.AppendLine("        if (TTLEventCodes.TryGetValue(label, out int code))");
        sb.AppendLine("            SerialTTLManager.Instance.LogEvent(label, code);");
        sb.AppendLine("        else");
        sb.AppendLine("            SerialTTLManager.Instance.LogEvent(label);");
        sb.AppendLine("    }");
        sb.AppendLine("}"); // end class

        // 5) Write out
        var path = folder + $"TrialManager{acr}.cs";
        // … after writing the file …
        File.WriteAllText(path, sb.ToString());

        // force Unity to notice it
        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();
        // 6) Create GameObject
        var managerGO = new GameObject($"TrialManager{acr}");
    }
}
