using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public enum TrialStateDMS
{
    TrialOn,
    SampleOn,
    SampleOff,
    DistractorOn,
    DistractorOff,
    TargetOn,
    Choice,
    Feedback,
    Reset,
}

public class TrialManagerDMS : MonoBehaviour
{
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

    public float TrialOnDuration = 2f;
    public float SampleOnDuration = 0.5f;
    public float SampleOffDuration = 0.5f;
    public float DistractorOnDuration = 0.5f;
    public float DistractorOffDuration = 2f;

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

    private Dictionary<string, int> TTLEventCodes = new Dictionary<string, int>
    {
        { "TrialOn", 1 },
        { "SampleOn", 2 },
        { "SampleOff", 3 },
        { "DistractorOn", 4 },
        { "DistractorOff", 5 },
        { "TargetOn", 6 },
        { "Choice", 7 },
        { "StartEndBlock", 8 }
    };

    void Start()
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

        // replace hard-coded TotalBlocks inspector value
        _totalBlocks = (_trials.Count > 0) ? _trials[_trials.Count - 1].BlockCount : 1;


        UpdateScoreUI();
        _audioSrc = GetComponent<AudioSource>();
        _correctBeep = Resources.Load<AudioClip>("AudioClips/positiveBeep");
        _errorBeep = Resources.Load<AudioClip>("AudioClips/negativeBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/completeBar");

        if (CoinUI != null) CoinUI.SetActive(UseCoinFeedback);
        if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        if (ScoreText != null) ScoreText.gameObject.SetActive(ShowScoreUI);
        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);


        StartCoroutine(RunTrials());
    }

    IEnumerator RunTrials()
    {
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



        LogEvent("StartEndBlock");

        while (_currentIndex < _trials.Count)
        {
            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];
            int[] cueIdxs = new int[0]; // Used for "correct" logic

            // — TRIALON —
            LogEvent("TrialOn");
            yield return StartCoroutine(WaitInterruptable(TrialOnDuration));
            // — SAMPLEON —
            LogEvent("SampleOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
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
            // — SAMPLEOFF —
            LogEvent("SampleOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return StartCoroutine(WaitInterruptable(SampleOffDuration));
            // — DISTRACTORON —
            LogEvent("DistractorOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs2 = trial.GetStimIndices("DistractorOn");
            var locs2 = trial.GetStimLocations("DistractorOn");
            if (idxs2.Length > 0 && locs2.Length > 0)
            {
                var goList2 = Spawner.SpawnStimuli(idxs2, locs2);
                spawnedItems.AddRange(goList2);
                lastIdxs = idxs2;
            }

            yield return StartCoroutine(WaitInterruptable(DistractorOnDuration));
            // — DISTRACTOROFF —
            LogEvent("DistractorOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return StartCoroutine(WaitInterruptable(DistractorOffDuration));
            // — TARGETON —
            LogEvent("TargetOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs3 = trial.GetStimIndices("TargetOn");
            var locs3 = trial.GetStimLocations("TargetOn");
            if (idxs3.Length > 0 && locs3.Length > 0)
            {
                var goList3 = Spawner.SpawnStimuli(idxs3, locs3);
                spawnedItems.AddRange(goList3);
                lastIdxs = idxs3;
            }

            yield return null;
            // — CHOICE —
            LogEvent("Choice");
            bool answered = false;
            int pickedIdx = -1;
            float reactionT = 0f;
            yield return StartCoroutine(WaitForChoice((i, rt) =>
            {
                answered = true;
                pickedIdx = i;
                reactionT = rt;
            }));

            // strip out destroyed references
            spawnedItems.RemoveAll(go => go == null);
            GameObject targetGO = spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx);

            yield return null;
            // — FEEDBACK —
            LogEvent("Feedback");

            //   the blocks below are because of the IsFeedback checkmark
            // — feedback and beep —
            bool correct = answered && cueIdxs.Contains(pickedIdx);
            //flash feedback
            if (targetGO != null)
                StartCoroutine(FlashFeedback(targetGO, correct));

            if (correct)
            {
                LogEvent("TargetSelected");
                _score += PointsPerCorrect;
                if (!CoinController.Instance.CoinBarWasJustFilled)
                    _audioSrc.PlayOneShot(_correctBeep);
                LogEvent("AudioPlaying");
                LogEvent("Success");
                FeedbackText.text = $"+{PointsPerCorrect}";
            }
            else
            {
                _score += PointsPerWrong;
                UpdateScoreUI();
                _audioSrc.PlayOneShot(_errorBeep);
                LogEvent("AudioPlaying");
                if (answered) { LogEvent("TargetSelected"); LogEvent("Fail"); }
                else { LogEvent("Timeout"); LogEvent("Fail"); }
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";
            }
            Vector2 clickScreenPos = Input.mousePosition;
            if (pickedIdx >= 0 && UseCoinFeedback)
            {
                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);
                else CoinController.Instance.RemoveCoins(1);
            }
            else if (UseCoinFeedback)
                CoinController.Instance.RemoveCoins(1);

            UpdateScoreUI();
            if (ShowFeedbackUI) FeedbackText.canvasRenderer.SetAlpha(1f);
            yield return StartCoroutine(WaitInterruptable(FeedbackDuration));

            yield return null;
            // — RESET —
            LogEvent("Reset");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);


            //Block Handling
            int thisBlock = trial.BlockCount;
            int nextBlock = (_currentIndex + 1 < _trials.Count) ? _trials[_currentIndex + 1].BlockCount : -1;


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

    void OnDestroy()
    {
        if (CoinController.Instance != null)
            CoinController.Instance.OnCoinBarFilled -= () => _audioSrc.PlayOneShot(_coinBarFullBeep);
    }
    private void LogEvent(string label)
    {
        // This is for ExtraFunctionality scripts
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

    private IEnumerator FlashFeedback(GameObject go, bool correct)
    {
        // grab all mesh renderers under the object
        var renderers = go.GetComponentsInChildren<Renderer>();
        // cache their original colors
        var originals = renderers.Select(r => r.material.color).ToArray();
        Color flashCol = correct ? Color.green : Color.red;

        const int flashes = 1;
        const float interval = 0.3f;  // quick

        for (int f = 0; f < flashes; f++)
        {
            // set to flash color
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = flashCol;
            yield return StartCoroutine(WaitInterruptable(interval));

            // revert to original
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = originals[i];
            yield return StartCoroutine(WaitInterruptable(interval));
        }
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
}


