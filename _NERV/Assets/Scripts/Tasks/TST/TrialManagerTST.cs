using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

/// <summary>
/// The structure of this script is as follows:
/// 1) Core Trial Loop: The main experimental logic is run here.
/// 2) Dependencies and Variables: All dependencies and variables are defined.
/// 3) Initialization: Runs first, setting up dependencies, audio, and UI.
/// 4) Helper Functions: Customizable utilities for the task.
///
/// Start() runs first -> Calls WarmUpAndThenRun() -> Runs RunTrials() coroutine.
/// RunTrials() contains the main trial logic. Here is where you can edit the trial flow
/// and set custom behaviors for each trial state.
///
/// This structure allows for quick customization of the trial flow,
/// while keeping the core logic intact.
///
/// If you have any questions, or need assistance please reach out via
/// GitHub or email kyle@coutray.com
/// </summary>
public class TrialManagerTST : MonoBehaviour
{
    // Dictionary for TTL event codes
    private Dictionary<string,int> TTLEventCodes = new Dictionary<string,int>
    {
        { "StartEndBlock", 8 }
    };

    // ==========================================================
    //  🧠 CORE TRIAL LOOP: Called by WarmUpAndThenRun() in Start()
    //  This is the main experimental logic developers should edit.
    // ==========================================================
    IEnumerator RunTrials()
    {
        // Check current trial block count (for pause handling)
        int lastBlock = _trials[0].BlockCount;

        //Begin trials paused
        if (PauseController != null && _trials?.Count > 0)
        {
            yield return StartCoroutine(
                _trials[0].BlockCount == 0
                    ? PauseController.ShowPause("PRACTICE") // Displays practice if we set up practice sessions. (By making TrialID PRACTICE)
                    : PauseController.ShowPause(lastBlock, _totalBlocks)
                );
        };
        // Log the start of our testing block
        LogEvent("StartEndBlock");

        // Begin the main loop for running trials
        while (_currentIndex < _trials.Count)
        {

            OnTrialStart?.Invoke(); // Notify any listeners that a trial is starting
            /// === [TRIAL VAR. HANDLING] ===
            // Start global trial timer
            float t0 = Time.realtimeSinceStartup;
            _trialStartTime = Time.time;
            _trialStartFrame = Time.frameCount;
            _trialsCompleted++;

            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];
            int[] cueIdxs = new int[0]; // Used for "correct" logic
            /// ==========[END SETUP]==========

            /// === [BEGIN USER-DEFINED STATES LOGIC] ===
            // — SAMPLEON —
            LogEventNextFrame("SampleOn");
            var idxs1 = trial.GetStimIndices("SampleOn");
            cueIdxs = idxs1; // Store for correct logic
            var locs1 = trial.GetStimLocations("SampleOn");
            if (idxs1.Length > 0 && locs1.Length > 0)
            {
                var goList1 = Spawner.SpawnStimuli(idxs1, locs1);
                spawnedItems.AddRange(goList1);
                lastIdxs = idxs1;
            }

            yield return StartCoroutine(WaitInterruptable(SampleOnDuration));

            // — DISTRACTORON —
            LogEventNextFrame("DistractorOn");
            var idxs2 = trial.GetStimIndices("DistractorOn");
            cueIdxs = idxs2; // Store for correct logic
            var locs2 = trial.GetStimLocations("DistractorOn");
            if (idxs2.Length > 0 && locs2.Length > 0)
            {
                var goList2 = Spawner.SpawnStimuli(idxs2, locs2);
                spawnedItems.AddRange(goList2);
                lastIdxs = idxs2;
            }

            yield return StartCoroutine(WaitInterruptable(DistractorOnDuration));

            // — CHOICE —
            LogEvent("Choice");
            bool answered   = false;
            int  pickedIdx  = -1;
            float reactionT = 0f;
            yield return StartCoroutine(WaitForChoice((i, rt) =>
            {
                answered  = true;
                pickedIdx = i;
                reactionT = rt;
            }));

            // Strip out destroyed references
            spawnedItems.RemoveAll(go => go == null);
            GameObject targetGO = spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx);

            yield return null;

            // — FEEDBACK —
            LogEvent("Feedback");

            // — Feedback and Beep —
            bool correct = answered && cueIdxs.Contains(pickedIdx);
            //Flash Feedback
            if (targetGO != null)
                StartCoroutine(FlashFeedback(targetGO, correct));

            if (correct)
            {
                LogEvent("TargetSelected");
                _score += PointsPerCorrect;
                if (!CoinController.Instance.CoinBarWasJustFilled)
                    _audioSrc.PlayOneShot(_correctBeep);
                LogEvent("AudioPlaying: _correctBeep");
                LogEvent("Success");
                FeedbackText.text = $"+{PointsPerCorrect}";
            }
            else
            {
                _score += PointsPerWrong;
                UpdateScoreUI();
                _audioSrc.PlayOneShot(_errorBeep);
                LogEvent("AudioPlaying: _errorBeep");
                if (answered) { LogEvent("TargetSelected"); LogEvent("Fail"); }
                else          { LogEvent("Timeout"); LogEvent("Fail"); }
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";
            }
            Vector2 clickScreenPos = Input.mousePosition;
            if (pickedIdx >= 0 && UseCoinFeedback)
            {
                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);
                else         CoinController.Instance.RemoveCoins(1);
            }
            else if(UseCoinFeedback)
                CoinController.Instance.RemoveCoins(1);

            UpdateScoreUI();
            if (ShowFeedbackUI) FeedbackText.canvasRenderer.SetAlpha(1f);
            yield return StartCoroutine(WaitInterruptable(FeedbackDuration));

            yield return null;

            // End global trial timer
            float t1 = Time.realtimeSinceStartup;

            // Normal Increment / Trial Handling events
            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);


            //Block Handling
            thisBlock = trial.BlockCount;
            int nextBlock = (_currentIndex + 1 < _trials.Count) ? _trials[_currentIndex+1].BlockCount : -1;


            // SUMMARY Handling
            float rtMs = reactionT * 1000f;  // reactionT is seconds → ms
            float duration = t1 - t0;
            _trialResults.Add(new TrialResult
            {
                isCorrect = correct,
                ReactionTimeMs = rtMs,
            });

            //Only run when enabled
            if (PauseBetweenBlocks && nextBlock != thisBlock && nextBlock != -1)
            {
                LogEvent("StartEndBlock");

                if (PauseController != null)
                    yield return StartCoroutine(PauseController.ShowPause(nextBlock, _totalBlocks));
                _currentIndex++; // Make sure we have the right header
                LogEvent("StartEndBlock");
                _currentIndex--; // Set the index back since we increment outside this loop
            }
            // Next trial incrementations
            lastBlock = thisBlock; // Keep true last block
            _currentIndex++; // Advance to the next trial
        }


        // End of all trials
        _currentIndex--;
        LogEvent("StartEndBlock");
        LogEvent("AllTrialsComplete");
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(-1, _totalBlocks));// The -1 is to send us to the end game state
    }


    // ==========================================================
    // Here is where all variables are defined, and dependencies are wired.
    // ==========================================================

    #region Dependencies and Variables
    [Header("Dependencies (auto-wired)")]
    public DependenciesContainer Deps;
    private Camera   PlayerCamera;
    private StimulusSpawner  Spawner;
    private TMPro.TMP_Text   FeedbackText;
    private TMPro.TMP_Text   ScoreText;
    private GameObject   CoinUI;
    private BlockPauseController PauseController;

    [Header("UI Toggles")]
    public bool ShowScoreUI = true;
    public bool ShowFeedbackUI = true;

    [Header("Coin Feedback")]
    public bool UseCoinFeedback = true;
    public int CoinsPerCorrect = 2;

    [Header("Block Pause")]
    public bool PauseBetweenBlocks = true;

    [Header("Timing & Scoring")]
    public float MaxChoiceResponseTime = 10f;
    public float FeedbackDuration = 1f;
    public int PointsPerCorrect = 2;
    public int PointsPerWrong = -1;

    public float SampleOnDuration = 1f;
    public float DistractorOnDuration = 0f;

    [Header("Helper Variables")]
    public List<TrialData> _trials;
    public int _currentIndex;
    private int _score = 0;
    public int _totalBlocks;
    public int thisBlock;

    private AudioSource _audioSrc;
    private AudioClip _correctBeep, _errorBeep, _coinBarFullBeep;

    //Pause Handling
    private bool _pauseRequested = false;
    private bool _inPause = false;

    // Trial Summary Variables
    private struct TrialResult
    {
        public bool isCorrect;
        public float ReactionTimeMs;
        public int DroppedFrames;
    }
    private List<TrialResult> _trialResults = new List<TrialResult>();
    private float _trialStartTime;
    private int _trialStartFrame;

    private string _taskAcronym;
    private int _trialsCompleted = 0;
    public event Action OnTrialStart;
    #endregion

    // ==========================================================
    //  INITIALIZATION: Runs FIRST, but moved under RunTrials() for simplicity.
    //  This is where you set up your dependencies, audio, and UI.
    // ==========================================================

    #region Task Initialization: Start() and Update()

    void Start()
    {
        //Force the GenericConfigManager into existence
        if (GenericConfigManager.Instance == null)
        {
            new GameObject("GenericConfigManager")
                .AddComponent<GenericConfigManager>();
        }

       Debug.Log("Running TrialManagerTST Start()");
        // Auto-grab everything from the one DependenciesContainer in the scene
        if (Deps == null)
            Deps = FindObjectOfType<DependenciesContainer>();

        // Now assign local refs
        PlayerCamera    = Deps.MainCamera;
        Spawner         = Deps.Spawner;
        FeedbackText    = Deps.FeedbackText;
        ScoreText       = Deps.ScoreText;
        CoinUI          = Deps.CoinUI;
        PauseController = Deps.PauseController;


        _trials       = GenericConfigManager.Instance.Trials;
        _currentIndex = 0;
        _taskAcronym = GetType().Name.Replace("TrialManager", "");

        // Hand yourself off to SessionLogManager
        SessionLogManager.Instance.RegisterTrialManager(this, _taskAcronym);

        // Replace hard-coded TotalBlocks inspector value
        _totalBlocks = (_trials.Count > 0) ? _trials[_trials.Count - 1].BlockCount : 1;

        // UI Loads
        UpdateScoreUI();
        _audioSrc     = GetComponent<AudioSource>();
        _correctBeep  = Resources.Load<AudioClip>("AudioClips/positiveBeep");
        _errorBeep    = Resources.Load<AudioClip>("AudioClips/negativeBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/completeBar");

        // CoinController setup
        if (CoinUI     != null) CoinUI.SetActive(UseCoinFeedback);
        if (FeedbackText!= null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        if (ScoreText   != null) ScoreText.gameObject.SetActive(ShowScoreUI);
        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);
        StartCoroutine(WarmUpAndThenRun()); // Preloads all stimuli first, then starts the RunTrials() loop
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            if (Input.GetKeyDown(KeyCode.S)) // Toggle Score UI
            {
                ShowScoreUI    = !ShowScoreUI;
                ShowFeedbackUI = !ShowFeedbackUI;
                if (ScoreText    != null) ScoreText.gameObject.SetActive(ShowScoreUI);
                if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
            }

            if (Input.GetKeyDown(KeyCode.P) && _inPause == false) // Pause the scene
            _pauseRequested = true;
        }
    }
    #endregion

    // ========= Helper Functions (customizable utilities) =========
    // These functions are separated out for clarity and included for Task customizability.
    // ==========================================================
    #region Helper Functions

    IEnumerator ShowFeedback()
    {
        FeedbackText.canvasRenderer.SetAlpha(1f);
        yield return StartCoroutine(WaitInterruptable(FeedbackDuration));
        if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);
    }

    void UpdateScoreUI()
    {
        if (ShowScoreUI && ScoreText != null) ScoreText.text = $"Score: {_score}";
        else if (ScoreText != null) ScoreText.text = "";
    }

    private IEnumerator WaitForChoice(System.Action<int, float> callback)
    {
        float startTime = Time.time;
        while (Time.time - startTime < MaxChoiceResponseTime)
        {
            if (_pauseRequested)
            {
                _inPause = true;
                float timePassed = Time.time - startTime;
                _pauseRequested = false;
                yield return StartCoroutine(PauseController.ShowPause("PAUSED"));
                startTime = Time.time - timePassed;
            }
            _inPause = false;

            if (Input.GetMouseButtonDown(0) || DwellClick.ClickDownThisFrame)
            {
                var ray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    var stimID = hit.collider.GetComponent<StimulusID>();
                    if (stimID != null)
                    {
                        float rt = Time.time - startTime;
                        callback(stimID.Index, rt);
                        yield break;
                    }
                }
            }
            yield return null;
        }
        callback(-1, MaxChoiceResponseTime);
    }


    private void LogEvent(string label)
    {
        // This is for ExtraFunctionality scripts
        BroadcastMessage("OnLogEvent", label, SendMessageOptions.DontRequireReceiver);

        if (_currentIndex >= _trials.Count) // This is for post RunTrials Log Calls. 
        {
            // The -1 is to ensure it has the correct header
            // We increment to break out of our old loops, but still need this to be labeled correctly
            _currentIndex--;
        }

        string trialID = _trials[_currentIndex].TrialID;

        // 1) Always log to ALL_LOGS
        SessionLogManager.Instance.LogAll(trialID, label, "");

        // 2) If it has a TTL code, log to TTL_LOGS
        if (TTLEventCodes.TryGetValue(label, out int code))
            SessionLogManager.Instance.LogTTL(trialID, label, code);

    }

    private void LogEventNextFrame(string label)
    {
        StartCoroutine(LogEventNextFrameCoroutine(label));
    }

    private IEnumerator LogEventNextFrameCoroutine(string label)
    {
        // This is for ExtraFunctionality scripts
        BroadcastMessage("OnLogEvent", label, SendMessageOptions.DontRequireReceiver);

        yield return null; // Wait a frame to accurately log stimuli events
        if (_currentIndex >= _trials.Count) // This is for post RunTrials Log Calls.
        {
            // The -1 is to ensure it has the correct header
            // We increment to break out of our old loops, but still need this to be labeled correctly
            _currentIndex--;
        }

        string trialID = _trials[_currentIndex].TrialID;

        // 1) Always log to ALL_LOGS
        SessionLogManager.Instance.LogAll(trialID, label, "");

        // 2) If it has a TTL code, log to TTL_LOGS
        if (TTLEventCodes.TryGetValue(label, out int code))
            SessionLogManager.Instance.LogTTL(trialID, label, code);
    }


    private IEnumerator FlashFeedback(GameObject go, bool correct)
    {
        // Grab all mesh renderers under the object
        var renderers = go.GetComponentsInChildren<Renderer>();
        // Cache their original colors
        var originals = renderers.Select(r => r.material.color).ToArray();
        Color flashCol = correct ? Color.green : Color.red;

        const int   flashes = 1;
        const float interval = 0.3f;  

        for (int f = 0; f < flashes; f++)
        {
            // Set to flash color
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = flashCol;
            yield return StartCoroutine(WaitInterruptable(interval));

            // Revert to original
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = originals[i];
            yield return StartCoroutine(WaitInterruptable(interval));
        }
    }
    // Added this to allow pause scene functionality
    private IEnumerator WaitInterruptable(float duration)
    {
        yield return null; // Wait a frame to ensure we are not in the middle of a coroutine
        float t0 = Time.time;
        while (Time.time - t0 < duration)
        {
            // If user hit P, immediately pause
            if (_pauseRequested && PauseController != null)
            {
                _inPause = true;
                _pauseRequested = false;
                yield return StartCoroutine(PauseController.ShowPause("PAUSED"));
            }
            _inPause = false;
            yield return null;  // Next frame
        }
    }

    /// <summary>
    /// Called reflectively by SessionLogManager when leaving this scene.
    /// </summary>
    public SessionLogManager.TaskSummary GetTaskSummary()
    {
        int total = _trialResults.Count;
        int corrects = _trialResults.Count(r => r.isCorrect);
        float meanRt = _trialResults.Average(r => r.ReactionTimeMs);

        return new SessionLogManager.TaskSummary
        {
            TrialsTotal = total,
            Accuracy = (float)corrects / total,
            MeanRT_ms = meanRt
        };
    }
    /// <summary>
    /// Called by SessionLogManager to pull every trial’s metrics.
    /// </summary>
    public List<SessionLogManager.TrialDetail> GetTaskDetails()
    {
        return _trialResults
            .Select((r, i) => new SessionLogManager.TrialDetail
            {
                TrialIndex = i + 1,
                Correct = r.isCorrect,
                ReactionTimeMs = r.ReactionTimeMs,
            })
            .ToList();
    }
    IEnumerator WarmUp()
    {
        var prefabDict = GenericConfigManager.Instance.StimIndexToFile;
        var usedIndices = prefabDict.Keys.ToList();  // All stimulus indices used in this session
        var locs = Enumerable.Range(0, usedIndices.Count)
            .Select(i => new Vector3(i * 1000f, 1000f, 0)) // Place far offscreen
            .ToArray();
        // Spawn all stimuli
        var goList = Spawner.SpawnStimuli(usedIndices.ToArray(), locs);
        Debug.Log($"[WarmUp] Spawned {goList.Count} stimuli for warmup.");
        // Wait for Unity to register them
        yield return new WaitForEndOfFrame();
        yield return null;
        yield return new WaitForEndOfFrame();

        // Trigger photodiode flash to warm up UI and rendering
        BroadcastMessage("OnLogEvent", "WarmupFlash", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.05f);  // Enough to get one frame out
        Spawner.ClearAll();
        yield return null;
    }
    private IEnumerator WarmUpAndThenRun()
    {
        yield return StartCoroutine(WarmUp());
        yield return new WaitForSeconds(0.1f); // give GPU/Unity a moment to breathe
        yield return StartCoroutine(RunTrials());
    }
        #endregion
}
public enum TrialStateTST
{
    SampleOn,
    DistractorOn,
    Choice,
    Feedback,
}

