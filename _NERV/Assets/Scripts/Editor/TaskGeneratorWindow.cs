using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using System;
using UnityEditor.Compilation;

public class TaskGeneratorWindow : EditorWindow
{
    // Remember last generation for manual attach of TrialManager{ACR}:
    private string _lastScriptPath;
    private string _lastAcronym;
    private GameObject _lastDepsGO;

    ExperimentDefinition experimentDef;
    GameObject dependenciesPrefab;
    private int _expDefPickerControlID;


    [MenuItem("Tools/Task Generator")]
    public static void ShowWindow() => GetWindow<TaskGeneratorWindow>("Task Generator");

    void OnEnable()
    {
        // only auto-assign if nothing has been set yet
        if (dependenciesPrefab == null)
        {
            // Load all GameObjects under Resources/Prefabs
            var all = Resources.LoadAll<GameObject>("Prefabs");
            // Filter to only those that have a DependenciesContainer
            var deps = all.Where(go => go.GetComponent<DependenciesContainer>() != null).ToArray();

            if (deps.Length == 1)
            {
                dependenciesPrefab = deps[0];
                Repaint(); // ensure the UI updates immediately
                Debug.Log($"[TaskGenerator] Auto‐assigned Dependencies prefab: {deps[0].name}");
            }
            else if (deps.Length > 1)
            {
                Debug.LogWarning($"[TaskGenerator] Found {deps.Length} Dependencies prefabs; please select one manually.");
            }
            else
            {
                Debug.LogWarning("[TaskGenerator] No Dependencies prefab found in Resources/Prefabs.");
            }
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Generate TrialManager from ExperimentDefinition", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ================================
        // Experiment Definition + Find…
        // ================================
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Experiment Definition");
        experimentDef = (ExperimentDefinition)EditorGUILayout.ObjectField(
            experimentDef,
            typeof(ExperimentDefinition),
            false,
            GUILayout.ExpandWidth(true)
        );
        if (GUILayout.Button("Find", GUILayout.Width(40)))
        {
            // grab a unique ID for this picker
            _expDefPickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            // open the picker tied to it
            EditorGUIUtility.ShowObjectPicker<ExperimentDefinition>(
                null, false, "t:ExperimentDefinition", _expDefPickerControlID
            );
        }

        // live‐assign as soon as you move the selection in the picker
        if (Event.current.commandName == "ObjectSelectorUpdated"
            && EditorGUIUtility.GetObjectPickerControlID() == _expDefPickerControlID)
        {
            experimentDef = EditorGUIUtility.GetObjectPickerObject() as ExperimentDefinition;
            Repaint();  // redraw the UI so the ObjectField shows your choice
        }

        EditorGUILayout.EndHorizontal();



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

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(_lastScriptPath))
        {
            if (GUILayout.Button("Attach TrialManager Now"))
            {
                AttachTrialManager();
            }
        }

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

        // 1.5) Set skybox
        // Load skybox material from Resources
        var skyboxMat = Resources.Load<Material>("Skybox/blackStone");
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
            Debug.Log("[TaskGenerator] Skybox set from Resources/Skybox/blackStone");
        }
        else
        {
            Debug.LogWarning("[TaskGenerator] Skybox material not found in Resources.");
        }


        // 2) Instantiate Dependencies
        GameObject depsGO = (GameObject)PrefabUtility.InstantiatePrefab(dependenciesPrefab);

        var genericCfg = depsGO.GetComponent<GenericConfigManager>();
        if (genericCfg != null)
            genericCfg.Acronym = acr;

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
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System;");
        sb.AppendLine();

        // --- summary comments ---
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// The structure of this script is as follows:");
        sb.AppendLine("/// 1) Core Trial Loop: The main experimental logic is run here.");
        sb.AppendLine("/// 2) Dependencies and Variables: All dependencies and variables are defined.");
        sb.AppendLine("/// 3) Initialization: Runs first, setting up dependencies, audio, and UI.");
        sb.AppendLine("/// 4) Helper Functions: Customizable utilities for the task.");
        sb.AppendLine("///");
        sb.AppendLine("/// Start() runs first -> Calls WarmUpAndThenRun() -> Runs RunTrials() coroutine.");
        sb.AppendLine("/// RunTrials() contains the main trial logic. Here is where you can edit the trial flow");
        sb.AppendLine("/// and set custom behaviors for each trial state.");
        sb.AppendLine("///");
        sb.AppendLine("/// This structure allows for quick customization of the trial flow,");
        sb.AppendLine("/// while keeping the core logic intact.");
        sb.AppendLine("///");
        sb.AppendLine("/// If you have any questions, or need assistance please reach out via");
        sb.AppendLine("/// GitHub or email kyle@coutray.com");
        sb.AppendLine("/// </summary>");

        // --- class header ---
        sb.AppendLine($"public class TrialManager{acr} : MonoBehaviour");
        sb.AppendLine("{");

        // --- TTL dictionary (dynamic) ---
        sb.AppendLine("    // Dictionary for TTL event codes");
        sb.AppendLine("    private Dictionary<string,int> TTLEventCodes = new Dictionary<string,int>");
        sb.AppendLine("    {");
        foreach (var st in experimentDef.States.Where(s => s.IsTTL))
            sb.AppendLine($"        {{ \"{st.Name}\", {st.TTLCode} }},");
        sb.AppendLine("        { \"StartEndBlock\", 8 }");
        sb.AppendLine("    };");
        sb.AppendLine();

        // --- Core Trial Loop Header Comment ---
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine("    //  🧠 CORE TRIAL LOOP: Called by WarmUpAndThenRun() in Start()");
        sb.AppendLine("    //  This is the main experimental logic developers should edit.");
        sb.AppendLine("    // ==========================================================");

        // --- RunTrials() ---
        sb.AppendLine("    IEnumerator RunTrials()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check current trial block count (for pause handling)");
        sb.AppendLine("        int lastBlock = _trials[0].BlockCount;");
        sb.AppendLine();
        sb.AppendLine("        //Begin trials paused");
        sb.AppendLine("        if (PauseController != null && _trials?.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            yield return StartCoroutine(");
        sb.AppendLine("                _trials[0].BlockCount == 0");
        sb.AppendLine("                    ? PauseController.ShowPause(\"PRACTICE\") // Displays practice if we set up practice sessions. (By making TrialID PRACTICE)");
        sb.AppendLine("                    : PauseController.ShowPause(lastBlock, _totalBlocks)");
        sb.AppendLine("                );");
        sb.AppendLine("        };");
        sb.AppendLine("        // Log the start of our testing block");
        sb.AppendLine("        LogEvent(\"StartEndBlock\");");
        sb.AppendLine();
        sb.AppendLine("        // Begin the main loop for running trials");
        sb.AppendLine("        while (_currentIndex < _trials.Count)");
        sb.AppendLine("        {");
        sb.AppendLine("            /// === [TRIAL VAR. HANDLING] ===");
        sb.AppendLine("            // Start global trial timer");
        sb.AppendLine("            float t0 = Time.realtimeSinceStartup;");
        sb.AppendLine("            _trialStartTime = Time.time;");
        sb.AppendLine("            _trialStartFrame = Time.frameCount;");
        sb.AppendLine("            _trialsCompleted++;");
        sb.AppendLine();
        sb.AppendLine("            var trial = _trials[_currentIndex];");
        sb.AppendLine("            var spawnedItems = new List<GameObject>();");
        sb.AppendLine("            int[] lastIdxs = new int[0];");
        sb.AppendLine("            int[] cueIdxs = new int[0]; // Used for \"correct\" logic");
        sb.AppendLine("            /// ==========[END SETUP]==========");
        sb.AppendLine();
        sb.AppendLine("            /// === [BEGIN USER-DEFINED STATES LOGIC] ===");

        int stimCount = 1;
        // Per‐state
        foreach (var st in experimentDef.States)
        {

            sb.AppendLine($"            // — {st.Name.ToUpper()} —");

            if (!st.IsStimulus)
                sb.AppendLine($"            LogEvent(\"{st.Name}\");");
            else
                sb.AppendLine($"            LogEventNextFrame(\"{st.Name}\");");

            // Stimulus
            if (st.IsStimulus)
            {
                sb.AppendLine($"            var idxs{stimCount} = trial.GetStimIndices(\"{st.Name}\");");
                sb.AppendLine($"            cueIdxs = idxs{stimCount}; // Store for correct logic");
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
                sb.AppendLine("            // Strip out destroyed references");
                sb.AppendLine("            spawnedItems.RemoveAll(go => go == null);");
                sb.AppendLine("            GameObject targetGO = spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx);");
                sb.AppendLine();
            }

            // Feedback
            if (st.IsFeedback)
            {
                sb.AppendLine();
                sb.AppendLine("            // — Feedback and Beep —");
                sb.AppendLine($"            bool correct = answered && cueIdxs.Contains(pickedIdx);");
                sb.AppendLine("            //Flash Feedback");
                sb.AppendLine("            if (targetGO != null)");
                sb.AppendLine("                StartCoroutine(FlashFeedback(targetGO, correct));");
                sb.AppendLine();
                sb.AppendLine("            if (correct)");
                sb.AppendLine("            {");
                sb.AppendLine("                LogEvent(\"TargetSelected\");");
                sb.AppendLine("                _score += PointsPerCorrect;");
                sb.AppendLine("                if (!CoinController.Instance.CoinBarWasJustFilled)");
                sb.AppendLine("                    _audioSrc.PlayOneShot(_correctBeep);");
                sb.AppendLine("                LogEvent(\"AudioPlaying: _correctBeep\");");
                sb.AppendLine("                LogEvent(\"Success\");");
                sb.AppendLine("                FeedbackText.text = $\"+{PointsPerCorrect}\";");
                sb.AppendLine("            }");
                sb.AppendLine("            else");
                sb.AppendLine("            {");
                sb.AppendLine("                _score += PointsPerWrong;");
                sb.AppendLine("                UpdateScoreUI();");
                sb.AppendLine("                _audioSrc.PlayOneShot(_errorBeep);");
                sb.AppendLine("                LogEvent(\"AudioPlaying: _errorBeep\");");
                sb.AppendLine("                if (answered) { LogEvent(\"TargetSelected\"); LogEvent(\"Fail\"); }");
                sb.AppendLine("                else          { LogEvent(\"Timeout\"); LogEvent(\"Fail\"); }");
                sb.AppendLine("                FeedbackText.text = answered ? \"Wrong!\" : \"Too Slow!\";");
                sb.AppendLine("            }");
                sb.AppendLine("            Vector2 clickScreenPos = Input.mousePosition;");
                sb.AppendLine("            if (pickedIdx >= 0 && UseCoinFeedback)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);");
                sb.AppendLine("                else         CoinController.Instance.RemoveCoins(1);");
                sb.AppendLine("            }");
                sb.AppendLine("            else if(UseCoinFeedback)");
                sb.AppendLine("                CoinController.Instance.RemoveCoins(1);");
                sb.AppendLine();
                sb.AppendLine("            UpdateScoreUI();");
                sb.AppendLine("            if (ShowFeedbackUI) FeedbackText.canvasRenderer.SetAlpha(1f);");
                sb.AppendLine("            yield return StartCoroutine(WaitInterruptable(FeedbackDuration));");
                sb.AppendLine();

            }

            // Delay
            if (st.IsDelay)
            {
                sb.AppendLine($"            yield return StartCoroutine(WaitInterruptable({st.Name}Duration));");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("            yield return null;");
                sb.AppendLine();
            }
        }

        // Normal increment if not Reset
        sb.AppendLine("            // End global trial timer");
        sb.AppendLine("            float t1 = Time.realtimeSinceStartup;");
        sb.AppendLine();
        sb.AppendLine("            // Normal Increment / Trial Handling events");
        sb.AppendLine("            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("            //Block Handling");
        sb.AppendLine("            int thisBlock = trial.BlockCount;");
        sb.AppendLine("            int nextBlock = (_currentIndex + 1 < _trials.Count) ? _trials[_currentIndex+1].BlockCount : -1;");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("            // SUMMARY Handling");
        sb.AppendLine("            float rtMs = reactionT * 1000f;  // reactionT is seconds → ms");
        sb.AppendLine("            float duration = t1 - t0;");
        sb.AppendLine("            _trialResults.Add(new TrialResult");
        sb.AppendLine("            {");
        sb.AppendLine("                isCorrect = correct,");
        sb.AppendLine("                ReactionTimeMs = rtMs,");
        sb.AppendLine("            });");
        sb.AppendLine();
        sb.AppendLine("            //Only run when enabled");
        sb.AppendLine("            if (PauseBetweenBlocks && nextBlock != thisBlock && nextBlock != -1)");
        sb.AppendLine("            {");
        sb.AppendLine("                LogEvent(\"StartEndBlock\");");
        sb.AppendLine();
        sb.AppendLine("                if (PauseController != null)");
        sb.AppendLine("                    yield return StartCoroutine(PauseController.ShowPause(nextBlock, _totalBlocks));");
        sb.AppendLine("                _currentIndex++; // Make sure we have the right header");
        sb.AppendLine("                LogEvent(\"StartEndBlock\");");
        sb.AppendLine("                _currentIndex--; // Set the index back since we increment outside this loop");
        sb.AppendLine("            }");
        sb.AppendLine("            // Next trial incrementations");
        sb.AppendLine("            lastBlock = thisBlock; // Keep true last block");
        sb.AppendLine("            _currentIndex++; // Advance to the next trial");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("        // End of all trials");
        sb.AppendLine("        _currentIndex--;");
        sb.AppendLine("        LogEvent(\"StartEndBlock\");");
        sb.AppendLine("        LogEvent(\"AllTrialsComplete\");");
        sb.AppendLine("        if (PauseController != null)");
        sb.AppendLine("            yield return StartCoroutine(PauseController.ShowPause(-1, _totalBlocks));// The -1 is to send us to the end game state");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- Dependencies and Variable Declarations ---
        sb.AppendLine();
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine("    // Here is where all variables are defined, and dependencies are wired.");
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine();
        sb.AppendLine("    #region Dependencies and Variables");
        // References
        sb.AppendLine("    [Header(\"Dependencies (auto-wired)\")]");
        sb.AppendLine("    public DependenciesContainer Deps;");

        sb.AppendLine("    private Camera   PlayerCamera;");
        sb.AppendLine("    private StimulusSpawner  Spawner;");
        sb.AppendLine("    private TMPro.TMP_Text   FeedbackText;");
        sb.AppendLine("    private TMPro.TMP_Text   ScoreText;");
        sb.AppendLine("    private GameObject   CoinUI;");
        sb.AppendLine("    private BlockPauseController PauseController;");
        sb.AppendLine();

        // Block Information
        sb.AppendLine("    [Header(\"Block Pause\")]");
        sb.AppendLine("    public bool PauseBetweenBlocks = true;");
        sb.AppendLine("    private int _totalBlocks;");
        sb.AppendLine();

        // Timing & Scoring
        sb.AppendLine("    [Header(\"Timing & Scoring\")]");
        sb.AppendLine("    public float MaxChoiceResponseTime = 10f;");
        sb.AppendLine("    public float FeedbackDuration = 1f;");
        sb.AppendLine("    public int PointsPerCorrect = 2;");
        sb.AppendLine("    public int PointsPerWrong = -1;");
        sb.AppendLine();

        //   Public floats for every IsDelay state
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

        // Pause Handling
        sb.AppendLine("    //Pause Handling");
        sb.AppendLine("    private bool _pauseRequested = false;");
        sb.AppendLine("    private bool _inPause = false;");
        sb.AppendLine();
        sb.AppendLine("    // Trial Summary Variables");
        sb.AppendLine("    private struct TrialResult");
        sb.AppendLine("    {");
        sb.AppendLine("        public bool isCorrect;");
        sb.AppendLine("        public float ReactionTimeMs;");
        sb.AppendLine("        public int DroppedFrames;");
        sb.AppendLine("    }");
        sb.AppendLine("    private List<TrialResult> _trialResults = new List<TrialResult>();");
        sb.AppendLine("    private float _trialStartTime;");
        sb.AppendLine("    private int _trialStartFrame;");
        sb.AppendLine();
        sb.AppendLine("    private string _taskAcronym;");
        sb.AppendLine("    private int _trialsCompleted = 0;");
        sb.AppendLine("    #endregion");
        sb.AppendLine();

        // --- Initialization ---
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine("    //  INITIALIZATION: Runs FIRST, but moved under RunTrials() for simplicity.");
        sb.AppendLine("    //  This is where you set up your dependencies, audio, and UI.");
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine();
        sb.AppendLine("    #region Task Initialization: Start() and Update()");
        sb.AppendLine();
        // --- Start() ---
        sb.AppendLine("    void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        //Force the GenericConfigManager into existence");
        sb.AppendLine("        if (GenericConfigManager.Instance == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            new GameObject(\"GenericConfigManager\")");
        sb.AppendLine("                .AddComponent<GenericConfigManager>();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"       Debug.Log(\"Running TrialManager{acr} Start()\");");
        sb.AppendLine("        // Auto-grab everything from the one DependenciesContainer in the scene");
        sb.AppendLine("        if (Deps == null)");
        sb.AppendLine("            Deps = FindObjectOfType<DependenciesContainer>();");
        sb.AppendLine();
        sb.AppendLine("        // Now assign local refs");
        sb.AppendLine("        PlayerCamera    = Deps.MainCamera;");
        sb.AppendLine("        Spawner         = Deps.Spawner;");
        sb.AppendLine("        FeedbackText    = Deps.FeedbackText;");
        sb.AppendLine("        ScoreText       = Deps.ScoreText;");
        sb.AppendLine("        CoinUI          = Deps.CoinUI;");
        sb.AppendLine("        PauseController = Deps.PauseController;");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("        _trials       = GenericConfigManager.Instance.Trials;");
        sb.AppendLine("        _currentIndex = 0;");
        sb.AppendLine("        _taskAcronym = GetType().Name.Replace(\"TrialManager\", \"\");");
        sb.AppendLine();
        sb.AppendLine("        // Hand yourself off to SessionLogManager");
        sb.AppendLine("        SessionLogManager.Instance.RegisterTrialManager(this, _taskAcronym);");
        sb.AppendLine();
        sb.AppendLine("        // Replace hard-coded TotalBlocks inspector value");
        sb.AppendLine("        _totalBlocks = (_trials.Count > 0) ? _trials[_trials.Count - 1].BlockCount : 1;");
        sb.AppendLine();
        sb.AppendLine("        // UI Loads");
        sb.AppendLine("        UpdateScoreUI();");
        sb.AppendLine("        _audioSrc     = GetComponent<AudioSource>();");
        sb.AppendLine("        _correctBeep  = Resources.Load<AudioClip>(\"AudioClips/positiveBeep\");");
        sb.AppendLine("        _errorBeep    = Resources.Load<AudioClip>(\"AudioClips/negativeBeep\");");
        sb.AppendLine("        _coinBarFullBeep = Resources.Load<AudioClip>(\"AudioClips/completeBar\");");
        sb.AppendLine();
        sb.AppendLine("        // CoinController setup");
        sb.AppendLine("        if (CoinUI     != null) CoinUI.SetActive(UseCoinFeedback);");
        sb.AppendLine("        if (FeedbackText!= null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);");
        sb.AppendLine("        if (ScoreText   != null) ScoreText.gameObject.SetActive(ShowScoreUI);");
        sb.AppendLine("        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);");
        sb.AppendLine("        StartCoroutine(WarmUpAndThenRun()); // Preloads all stimuli first, then starts the RunTrials() loop");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (Input.GetKeyDown(KeyCode.S)) // Toggle Score UI");
        sb.AppendLine("        {");
        sb.AppendLine("            ShowScoreUI    = !ShowScoreUI;");
        sb.AppendLine("            ShowFeedbackUI = !ShowFeedbackUI;");
        sb.AppendLine("            if (ScoreText    != null) ScoreText.gameObject.SetActive(ShowScoreUI);");
        sb.AppendLine("            if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (Input.GetKeyDown(KeyCode.P) && _inPause == false) // Pause the scene");
        sb.AppendLine("        _pauseRequested = true;");
        sb.AppendLine("    }");
        sb.AppendLine("    #endregion");
        sb.AppendLine();
        sb.AppendLine("    // ========= Helper Functions (customizable utilities) =========");
        sb.AppendLine("    // These functions are separated out for clarity and included for Task customizability.");
        sb.AppendLine("    // ==========================================================");
        sb.AppendLine("    #region Helper Functions");
        sb.AppendLine();
        // --- Helpers ---
        sb.AppendLine("    IEnumerator ShowFeedback()");
        sb.AppendLine("    {");
        sb.AppendLine("        FeedbackText.canvasRenderer.SetAlpha(1f);");
        sb.AppendLine("        yield return StartCoroutine(WaitInterruptable(FeedbackDuration));");
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
        sb.AppendLine("            if (_pauseRequested)");
        sb.AppendLine("            {");
        sb.AppendLine("                _inPause = true;");
        sb.AppendLine("                float timePassed = Time.time - startTime;");
        sb.AppendLine("                _pauseRequested = false;");
        sb.AppendLine("                yield return StartCoroutine(PauseController.ShowPause(\"PAUSED\"));");
        sb.AppendLine("                startTime = Time.time - timePassed;");
        sb.AppendLine("            }");
        sb.AppendLine("            _inPause = false;");
        sb.AppendLine();
        sb.AppendLine("            if (Input.GetMouseButtonDown(0) || DwellClick.ClickDownThisFrame)");
        sb.AppendLine("            {");
        sb.AppendLine("                LogEvent(\"Clicked\");");
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
        sb.AppendLine();
        sb.AppendLine("    private void LogEvent(string label)");
        sb.AppendLine("    {");
        sb.AppendLine("        // This is for ExtraFunctionality scripts");
        sb.AppendLine("        BroadcastMessage(\"OnLogEvent\", label, SendMessageOptions.DontRequireReceiver);");
        sb.AppendLine();
        sb.AppendLine("        if (_currentIndex >= _trials.Count) // This is for post RunTrials Log Calls. ");
        sb.AppendLine("        {");
        sb.AppendLine("            // The -1 is to ensure it has the correct header");
        sb.AppendLine("            // We increment to break out of our old loops, but still need this to be labeled correctly");
        sb.AppendLine("            _currentIndex--;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        string trialID = _trials[_currentIndex].TrialID;");
        sb.AppendLine();
        sb.AppendLine("        // 1) Always log to ALL_LOGS");
        sb.AppendLine("        SessionLogManager.Instance.LogAll(trialID, label, \"\");");
        sb.AppendLine();
        sb.AppendLine("        // 2) If it has a TTL code, log to TTL_LOGS");
        sb.AppendLine("        if (TTLEventCodes.TryGetValue(label, out int code))");
        sb.AppendLine("            SessionLogManager.Instance.LogTTL(trialID, label, code);");
        sb.AppendLine();
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void LogEventNextFrame(string label)");
        sb.AppendLine("    {");
        sb.AppendLine("        StartCoroutine(LogEventNextFrameCoroutine(label));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private IEnumerator LogEventNextFrameCoroutine(string label)");
        sb.AppendLine("    {");
        sb.AppendLine("        // This is for ExtraFunctionality scripts");
        sb.AppendLine("        BroadcastMessage(\"OnLogEvent\", label, SendMessageOptions.DontRequireReceiver);");
        sb.AppendLine();
        sb.AppendLine("        yield return null; // Wait a frame to accurately log stimuli events");
        sb.AppendLine("        if (_currentIndex >= _trials.Count) // This is for post RunTrials Log Calls.");
        sb.AppendLine("        {");
        sb.AppendLine("            // The -1 is to ensure it has the correct header");
        sb.AppendLine("            // We increment to break out of our old loops, but still need this to be labeled correctly");
        sb.AppendLine("            _currentIndex--;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        string trialID = _trials[_currentIndex].TrialID;");
        sb.AppendLine();
        sb.AppendLine("        // 1) Always log to ALL_LOGS");
        sb.AppendLine("        SessionLogManager.Instance.LogAll(trialID, label, \"\");");
        sb.AppendLine();
        sb.AppendLine("        // 2) If it has a TTL code, log to TTL_LOGS");
        sb.AppendLine("        if (TTLEventCodes.TryGetValue(label, out int code))");
        sb.AppendLine("            SessionLogManager.Instance.LogTTL(trialID, label, code);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("    private IEnumerator FlashFeedback(GameObject go, bool correct)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Grab all mesh renderers under the object");
        sb.AppendLine("        var renderers = go.GetComponentsInChildren<Renderer>();");
        sb.AppendLine("        // Cache their original colors");
        sb.AppendLine("        var originals = renderers.Select(r => r.material.color).ToArray();");
        sb.AppendLine("        Color flashCol = correct ? Color.green : Color.red;");
        sb.AppendLine();
        sb.AppendLine("        const int   flashes = 1;");
        sb.AppendLine("        const float interval = 0.3f;  ");
        sb.AppendLine();
        sb.AppendLine("        for (int f = 0; f < flashes; f++)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Set to flash color");
        sb.AppendLine("            for (int i = 0; i < renderers.Length; i++)");
        sb.AppendLine("                renderers[i].material.color = flashCol;");
        sb.AppendLine("            yield return StartCoroutine(WaitInterruptable(interval));");
        sb.AppendLine();
        sb.AppendLine("            // Revert to original");
        sb.AppendLine("            for (int i = 0; i < renderers.Length; i++)");
        sb.AppendLine("                renderers[i].material.color = originals[i];");
        sb.AppendLine("            yield return StartCoroutine(WaitInterruptable(interval));");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    // Added this to allow pause scene functionality");
        sb.AppendLine("    private IEnumerator WaitInterruptable(float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return null; // Wait a frame to ensure we are not in the middle of a coroutine");
        sb.AppendLine("        float t0 = Time.time;");
        sb.AppendLine("        while (Time.time - t0 < duration)");
        sb.AppendLine("        {");
        sb.AppendLine("            // If user hit P, immediately pause");
        sb.AppendLine("            if (_pauseRequested && PauseController != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                _inPause = true;");
        sb.AppendLine("                _pauseRequested = false;");
        sb.AppendLine("                yield return StartCoroutine(PauseController.ShowPause(\"PAUSED\"));");
        sb.AppendLine("            }");
        sb.AppendLine("            _inPause = false;");
        sb.AppendLine("            yield return null;  // Next frame");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Called reflectively by SessionLogManager when leaving this scene.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public SessionLogManager.TaskSummary GetTaskSummary()");
        sb.AppendLine("    {");
        sb.AppendLine("        int total = _trialResults.Count;");
        sb.AppendLine("        int corrects = _trialResults.Count(r => r.isCorrect);");
        sb.AppendLine("        float meanRt = _trialResults.Average(r => r.ReactionTimeMs);");
        sb.AppendLine();
        sb.AppendLine("        return new SessionLogManager.TaskSummary");
        sb.AppendLine("        {");
        sb.AppendLine("            TrialsTotal = total,");
        sb.AppendLine("            Accuracy = (float)corrects / total,");
        sb.AppendLine("            MeanRT_ms = meanRt");
        sb.AppendLine("        };");
        sb.AppendLine("    }");

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Called by SessionLogManager to pull every trial’s metrics.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public List<SessionLogManager.TrialDetail> GetTaskDetails()");
        sb.AppendLine("    {");
        sb.AppendLine("        return _trialResults");
        sb.AppendLine("            .Select((r, i) => new SessionLogManager.TrialDetail");
        sb.AppendLine("            {");
        sb.AppendLine("                TrialIndex = i + 1,");
        sb.AppendLine("                Correct = r.isCorrect,");
        sb.AppendLine("                ReactionTimeMs = r.ReactionTimeMs,");
        sb.AppendLine("            })");
        sb.AppendLine("            .ToList();");
        sb.AppendLine("    }");

        sb.AppendLine("    IEnumerator WarmUp()");
        sb.AppendLine("    {");
        sb.AppendLine("        var prefabDict = GenericConfigManager.Instance.StimIndexToFile;");
        sb.AppendLine("        var usedIndices = prefabDict.Keys.ToList();  // All stimulus indices used in this session");

        sb.AppendLine("        var locs = Enumerable.Range(0, usedIndices.Count)");
        sb.AppendLine("            .Select(i => new Vector3(i * 1000f, 1000f, 0)) // Place far offscreen");
        sb.AppendLine("            .ToArray();");

        sb.AppendLine("        // Spawn all stimuli");
        sb.AppendLine("        var goList = Spawner.SpawnStimuli(usedIndices.ToArray(), locs);");

        sb.AppendLine("        // Wait for Unity to register them");
        sb.AppendLine("        yield return new WaitForEndOfFrame();");
        sb.AppendLine("        yield return null;");
        sb.AppendLine("        yield return new WaitForEndOfFrame();");
        sb.AppendLine();
        sb.AppendLine("        // Trigger photodiode flash to warm up UI and rendering");
        sb.AppendLine("        BroadcastMessage(\"OnLogEvent\", \"WarmupFlash\", SendMessageOptions.DontRequireReceiver);");
        sb.AppendLine("        yield return new WaitForSeconds(0.05f);  // Enough to get one frame out");

        sb.AppendLine("        Spawner.ClearAll();");
        sb.AppendLine("        yield return null;");
        sb.AppendLine("    }");

        sb.AppendLine("    private IEnumerator WarmUpAndThenRun()");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return StartCoroutine(WarmUp());");
        sb.AppendLine("        yield return new WaitForSeconds(0.1f); // give GPU/Unity a moment to breathe");
        sb.AppendLine("        yield return StartCoroutine(RunTrials());");
        sb.AppendLine("    }");

        sb.AppendLine("        #endregion");
        sb.AppendLine("}"); // End class

        // --- enum ---
        sb.AppendLine($"public enum TrialState{acr}");
        sb.AppendLine("{");
        foreach (var st in experimentDef.States)
            sb.AppendLine($"    {st.Name},");
        sb.AppendLine("}");
        sb.AppendLine();

        // 5) Write out
        var path = folder + $"TrialManager{acr}.cs";
        // … after writing the file …
        File.WriteAllText(path, sb.ToString());

        // force Unity to notice it
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh();
        // … after AssetDatabase.Refresh() …
        _lastScriptPath = path;
        _lastAcronym = acr;
        _lastDepsGO = depsGO;


    }    // End of GenerateTrialManager()

    private void AttachTrialManager()
    {
        // 1) Load the compiled script
        var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(_lastScriptPath);
        var managerType = ms?.GetClass();
        if (managerType == null)
        {
            Debug.LogError($"[TG] Couldn’t find public class in {_lastScriptPath}");
            return;
        }

        // 2) Create the GameObject & add component
        var go = new GameObject($"TrialManager{_lastAcronym}");
        var comp = go.AddComponent(managerType);
        go.AddComponent<AudioSource>();

        // 3) Wire its Deps field
        var field = managerType.GetField("Deps");
        if (field != null && _lastDepsGO != null)
        {
            var dc = _lastDepsGO.GetComponent<DependenciesContainer>();
            if (dc != null) field.SetValue(comp, dc);
        }

        // 4) Mark & save the scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log($"[TG] Attached '{go.name}' with component {managerType.Name}");

        // Clear so button disappears
        _lastScriptPath = null;
        _lastAcronym = null;
        _lastDepsGO = null;
    }
    

}
