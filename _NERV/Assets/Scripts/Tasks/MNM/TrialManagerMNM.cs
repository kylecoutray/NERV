using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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

public class TrialManagerMNM : MonoBehaviour
{
    // Dictionary for TTL event codes
    private Dictionary<string, int> TTLEventCodes = new Dictionary<string, int>
    {
        { "Fixation", 1 },
        { "CueOn", 2 },
        { "SampleOn", 3 },
        { "Choice", 4 },
        { "Feedback", 5 },
        { "StartEndBlock", 8}
    };

    // ==========================================================
    //  🧠 CORE TRIAL LOOP: Called by WarmUpAndThenRun() in Start()
    //  This is the main experimental logic collaborators should edit.
    // ==========================================================
    IEnumerator RunTrials()
    {
        // Check current trial block count (for pause handling)
        int lastBlock = _trials[0].BlockCount;

        //Start with paused first scene    
        if (PauseController != null && _trials?.Count > 0)
        {
            yield return StartCoroutine(
                _trials[0].TrialID == "PRACTICE"
                    ? PauseController.ShowPause("PRACTICE") // displays practice if we set up practice sessions.
                    : PauseController.ShowPause(lastBlock, _totalBlocks)
            );
        }

        // Log the start of our testing block
        LogEvent("StartEndBlock");

        while (_currentIndex < _trials.Count)
        {
            // start global trial timer
            float t0 = Time.realtimeSinceStartup;
            _trialStartTime = Time.time;
            _trialStartFrame = Time.frameCount;
            _trialsCompleted++;

            // Begin the main loop for running trials
            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];

            // — IDLE —
            LogEvent("Idle");
            yield return StartCoroutine(WaitInterruptable(IdleDuration));


            // — FIXATION —
            LogEvent("Fixation");
            yield return StartCoroutine(WaitInterruptable(FixationDuration));


            // — CUEON —
            LogEventNextFrame("CueOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs1 = trial.GetStimIndices("CueOn");
            var locs1 = trial.GetStimLocations("CueOn");
            if (idxs1.Length > 0 && locs1.Length > 0)
            {
                var goList1 = Spawner.SpawnStimuli(idxs1, locs1);
                spawnedItems.AddRange(goList1);
                lastIdxs = idxs1;
            }

            yield return StartCoroutine(WaitInterruptable(CueOnDuration));
            // — CUEOFF —
            LogEvent("CueOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            // — DELAY1 —
            LogEvent("Delay1");
            yield return StartCoroutine(WaitInterruptable(Delay1Duration));
            // — SAMPLEON —
            LogEventNextFrame("SampleOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs2 = trial.GetStimIndices("SampleOn");
            var locs2 = trial.GetStimLocations("SampleOn");
            if (idxs2.Length > 0 && locs2.Length > 0)
            {
                var goList2 = Spawner.SpawnStimuli(idxs2, locs2);
                spawnedItems.AddRange(goList2);
                lastIdxs = idxs2;
            }

            yield return StartCoroutine(WaitInterruptable(SampleOnDuration));
            // — SAMPLEOFF —
            LogEvent("SampleOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            // — DELAY2 —
            LogEvent("Delay2");
            yield return StartCoroutine(WaitInterruptable(Delay2Duration));


            // — CHOICE —
            ////////////////////////
            /// Custom CHOICE AND FEEDBACK Logic for this Game. 
            // — CHOICE OPTIONS —  
            LogEventNextFrame("Choice");

            // 1) Determine correct answer  
            //    true ⇒ match is correct, false ⇒ non-match
            int cueIdx = trial.GetStimIndices("CueOn")[0];
            int sampleIdx = trial.GetStimIndices("SampleOn")[0];
            bool isMatch = (cueIdx == sampleIdx);
            int correctIdx = isMatch ? 0 : 1;  // we’ll assign 0→match, 1→non-match

            // 2) Spawn two buttons  
            var choiceItems = new List<GameObject>();

            // match (index 0)  
            var goMatch = Instantiate(
                MatchPrefab,
                new Vector3(0, ChoiceY, 0),
                Quaternion.identity,
                transform
            );
            var sidMatch = goMatch.GetComponent<StimulusID>();
            sidMatch.Index = 0;
            choiceItems.Add(goMatch);

            // non-match (index 1)  
            var goNon = Instantiate(
                NonMatchPrefab,
                new Vector3(0, -ChoiceY, 0),
                Quaternion.identity,
                transform
            );
            var sidNon = goNon.GetComponent<StimulusID>();
            sidNon.Index = 1;
            choiceItems.Add(goNon);

            // 3) Wait for click or timeout  
            bool answered = false;
            int pickedIdx = -1;
            float reactionT = 0f;

            yield return StartCoroutine(WaitForChoice((i, rt) =>
            {
                answered = true;
                pickedIdx = i;
                reactionT = rt;
            }));

            // 4) Identify the clicked GameObject  
            GameObject chosenGO = null;
            if (pickedIdx == 0) chosenGO = goMatch;
            else if (pickedIdx == 1) chosenGO = goNon;

            // 5) Flash it (green if correct, red if wrong)  
            bool correct = answered && (pickedIdx == correctIdx);
            if (chosenGO != null)
                StartCoroutine(FlashFeedback(chosenGO, correct));


            // 7) Reward / penalty logic  
            if (correct)
            {
                _score += PointsPerCorrect;
                LogEvent("Success");
                _audioSrc.PlayOneShot(_correctBeep);
                LogEvent("AudioPlaying");  // if you want TTL on the beep
                FeedbackText.text = $"+{PointsPerCorrect}";
            }
            else
            {
                _score += PointsPerWrong;
                LogEvent(answered ? "Fail" : "Timeout");
                _audioSrc.PlayOneShot(_errorBeep);
                LogEvent("AudioPlaying");  // if you want TTL on the beep
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";
            }

            UpdateScoreUI();

            // 8) Coin feedback (optional)  
            if (answered && UseCoinFeedback)
            {
                var clickPos = Input.mousePosition;
                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickPos);
                else CoinController.Instance.RemoveCoins(1);
            }

            // 9) wait out the feedback UI
            yield return StartCoroutine(WaitInterruptable(FeedbackDuration));

            if (ShowFeedbackUI)       // fade‐out
                StartCoroutine(ShowFeedback());

            LogEvent("Reset");
            // 10) Clean up choice items  
            foreach (var go in choiceItems)
                Destroy(go);

            // end global trial timer
            float t1 = Time.realtimeSinceStartup;

            //Block Handling
            int thisBlock = trial.BlockCount;
            int nextBlock = (_currentIndex + 1 < _trials.Count) ? _trials[_currentIndex + 1].BlockCount : -1;

            // SUMMARY Handling
            float rtMs = reactionT * 1000f;
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
            // next trial incrementations
            lastBlock = thisBlock; //keep true last block
            _currentIndex++; // advance to the next trial
        }


        // end of all trials
        _currentIndex--;
        LogEvent("StartEndBlock");
        LogEvent("AllTrialsComplete");
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(-1, _totalBlocks));// the -1 is to send it to end game state
    }

    // ==========================================================
    // Here is where all variables are defined, and dependencies are wired.
    // ===========================================================

    #region Dependencies and Variable Declarations

    [Header("Dependencies (auto-wired)")]
    public DependenciesContainer Deps;
    private Camera PlayerCamera;
    private StimulusSpawner Spawner;
    private TMPro.TMP_Text FeedbackText;
    private TMPro.TMP_Text ScoreText;
    private GameObject CoinUI;
    private BlockPauseController PauseController;

    [Header("Block Pause")]
    public bool PauseBetweenBlocks = true;
    private int _totalBlocks;
    [Header("Timing & Scoring")]
    public float MaxChoiceResponseTime = 10f;
    public float FeedbackDuration = 1f;
    public int PointsPerCorrect = 2;
    public int PointsPerWrong = -1;

    public float IdleDuration = 2f;
    public float FixationDuration = 1f;
    public float CueOnDuration = 0.5f;
    public float Delay1Duration = 0.25f;
    public float SampleOnDuration = 0.5f;
    public float Delay2Duration = 0.25f;

    private List<TrialData> _trials;
    private int _currentIndex;
    private int _score = 0;

    private AudioSource _audioSrc;
    private AudioClip _correctBeep, _errorBeep, _coinBarFullBeep;

    [Header("Coin Feedback")]
    public bool UseCoinFeedback = true;
    public int CoinsPerCorrect = 2;

    [Header("UI Toggles")]
    public bool ShowScoreUI = true;
    public bool ShowFeedbackUI = true;

    // Pause Handling
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


    [Header("Match/Non-Match Options")]
    public GameObject MatchPrefab;      // drag in your “match.png” prefab
    public GameObject NonMatchPrefab;   // drag in your “nonmatch.png” prefab
    public float ChoiceY = 1.5f;   // vertical offset for match (non-match is -ChoiceY)

    #endregion

    // ==========================================================
    //  INITIALIZATION: Runs FIRST, but moved under RunTrials() for simplicity.
    //  This is where you set up your dependencies, audio, and UI.
    // ==========================================================

    #region Task Initialization: Start() and Update()
    void Start() // Setting everything up for our main RunTrials() loop.
    {
        //Force the GenericConfigManager into existence
        if (GenericConfigManager.Instance == null)
        {
            new GameObject("GenericConfigManager")
                .AddComponent<GenericConfigManager>();
        }

        // Auto-grab everything from the one DependenciesContainer in the scene
        if (Deps == null)
            Deps = FindObjectOfType<DependenciesContainer>();

        // now assign local refs
        PlayerCamera = Deps.MainCamera;
        Spawner = Deps.Spawner;
        FeedbackText = Deps.FeedbackText;
        ScoreText = Deps.ScoreText;
        CoinUI = Deps.CoinUI;
        PauseController = Deps.PauseController;


        _trials = GenericConfigManager.Instance.Trials;
        _currentIndex = 0;
        _taskAcronym = GetType().Name.Replace("TrialManager", "");

        // hand yourself off to SessionLogManager
        SessionLogManager.Instance.RegisterTrialManager(this, _taskAcronym);

        // replace hard-coded TotalBlocks inspector value
        _totalBlocks = (_trials.Count > 0) ? _trials[_trials.Count - 1].BlockCount : 1;

        // UI Loads
        UpdateScoreUI();
        _audioSrc = GetComponent<AudioSource>();
        _correctBeep = Resources.Load<AudioClip>("AudioClips/positiveBeep");
        _errorBeep = Resources.Load<AudioClip>("AudioClips/negativeBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/completeBar");

        // CoinController setup
        if (CoinUI != null) CoinUI.SetActive(UseCoinFeedback);
        if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        if (ScoreText != null) ScoreText.gameObject.SetActive(ShowScoreUI);
        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);
        StartCoroutine(WarmUpAndThenRun());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowScoreUI = !ShowScoreUI;
            ShowFeedbackUI = !ShowFeedbackUI;
            if (ScoreText != null) ScoreText.gameObject.SetActive(ShowScoreUI);
            if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        }
        if (Input.GetKeyDown(KeyCode.P) && _inPause == false)
            _pauseRequested = true;
    }

    #endregion
    // ========= Helper Functions (customizable utilities) =========
    // These functions are separated out for clarity and included for Task customizability.
    #region Helper Functions

    private void LogEvent(string label)
    {
        BroadcastMessage("OnLogEvent", label, SendMessageOptions.DontRequireReceiver);

        if (_currentIndex >= _trials.Count) //this is for post RunTrials Log Calls. 
        {
            // the -1 is to ensure it has the correct header
            // we increment to break out of our old loops, but still need this to be labeled correctly
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
                LogEvent("Clicked");
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

    //////////////
    /// Custom FlashFeedback to account for match/nonmatch sprite colors
    /// ////////////    
    private IEnumerator FlashFeedback(GameObject go, bool correct)
    {
        // choose flash color
        Color flashColor = correct ? Color.green : Color.red;

        // find an unlit color shader
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogError("Unlit/Color shader not found!");
            yield break;
        }

        // create a one‐off material
        Material flashMat = new Material(shader) { color = flashColor };

        // grab all renderers and stash their original materials
        var rends = go.GetComponentsInChildren<Renderer>();
        var origMats = new Material[rends.Length];
        for (int i = 0; i < rends.Length; i++)
            origMats[i] = rends[i].material;

        const int flashes = 1;
        const float interval = 0.3f;

        for (int f = 0; f < flashes; f++)
        {
            // swap in flash material
            foreach (var r in rends)
                r.material = flashMat;
            yield return StartCoroutine(WaitInterruptable(interval));

            // restore originals
            for (int i = 0; i < rends.Length; i++)
                rends[i].material = origMats[i];
            yield return StartCoroutine(WaitInterruptable(interval));
        }

        // cleanup
        UnityEngine.Object.Destroy(flashMat);
    }

    // added this to allow pause scene functionality
    private IEnumerator WaitInterruptable(float duration)
    {
        float t0 = Time.time;
        while (Time.time - t0 < duration)
        {
            // if user hit P, immediately pause
            if (_pauseRequested && PauseController != null)
            {
                _inPause = true;
                _pauseRequested = false;
                yield return StartCoroutine(PauseController.ShowPause("PAUSED"));

            }
            _inPause = false;
            yield return null;  // next frame
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
        yield return new WaitForSeconds(0.1f); // optional: give GPU/Unity a moment to breathe
        yield return StartCoroutine(RunTrials());
    }

    #endregion
}

public enum TrialStateMNM
{
    Idle,
    Fixation,
    CueOn,
    CueOff,
    Delay1,
    SampleOn,
    SampleOff,
    Delay2,
    Choice,
    Feedback,
}