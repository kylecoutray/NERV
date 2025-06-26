using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public enum TrialStateP2T
{
    Idle,
    SampleOn,
    SampleOff,
    PostSampleDelay,
    TargetOn,
    Choice,
    Feedback,
    Reset,
}

public class TrialManagerP2T : MonoBehaviour
{
    [Header("Dependencies (auto-wired)")]
    public DependenciesContainer Deps;
    private Camera   PlayerCamera;
    private StimulusSpawner  Spawner;
    private TMPro.TMP_Text   FeedbackText;
    private TMPro.TMP_Text   ScoreText;
    private GameObject   CoinUI;
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
    public float SampleOnDuration = 1f;
    public float SampleOffDuration = 1f;
    public float PostSampleDelayDuration = 1f;

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

    private Dictionary<string,int> TTLEventCodes = new Dictionary<string,int>
    {
        { "SampleOn", 1 },
        { "TargetOn", 4 },
        { "Choice", 2 },
        { "Reset", 3 },
    };

    void Start()
    {
            // Force the GenericConfigManager into existence
        if (GenericConfigManager.Instance == null)
        {
            new GameObject("GenericConfigManager")
                .AddComponent<GenericConfigManager>();
        }
        Debug.Log("Running start TrialManagerP2t");
         // Auto-grab everything from the one DependenciesContainer in the scene
        if (Deps == null)
            Deps = FindObjectOfType<DependenciesContainer>();

        // now assign local refs
        PlayerCamera    = Deps.MainCamera;
        Spawner         = Deps.Spawner;
        FeedbackText    = Deps.FeedbackText;
        ScoreText       = Deps.ScoreText;
        CoinUI          = Deps.CoinUI;
        PauseController = Deps.PauseController;


        _trials       = GenericConfigManager.Instance.Trials;
        _currentIndex = 0;

        // replace hard-coded TotalBlocks inspector value
        _totalBlocks = (_trials.Count > 0) ? _trials[_trials.Count - 1].BlockCount : 1;


        UpdateScoreUI();
        _audioSrc     = GetComponent<AudioSource>();
        _correctBeep  = Resources.Load<AudioClip>("AudioClips/positiveBeep");
        _errorBeep    = Resources.Load<AudioClip>("AudioClips/negativeBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/completeBar");

        if (CoinUI     != null) CoinUI.SetActive(UseCoinFeedback);
        if (FeedbackText!= null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        if (ScoreText   != null) ScoreText.gameObject.SetActive(ShowScoreUI);
        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);
        StartCoroutine(RunTrials());
    }

    IEnumerator RunTrials()
    {
        
        int lastBlock = _trials[0].BlockCount;

        //Start with paused first scene
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(lastBlock, _totalBlocks));

        LogEvent("StartEndBlock");

        while (_currentIndex < _trials.Count)
        {
            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];

            // — IDLE —
            LogEvent("Idle");
            yield return new WaitForSeconds(IdleDuration);
            // — SAMPLEON —
            LogEvent("SampleOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs1 = trial.GetStimIndices("SampleOn");
            var locs1 = trial.GetStimLocations("SampleOn");
            if (idxs1.Length > 0 && locs1.Length > 0)
            {
                var goList1 = Spawner.SpawnStimuli(idxs1, locs1);
                spawnedItems.AddRange(goList1);
                lastIdxs = idxs1;
            }

            yield return new WaitForSeconds(SampleOnDuration);
            // — SAMPLEOFF —
            LogEvent("SampleOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return new WaitForSeconds(SampleOffDuration);
            // — POSTSAMPLEDELAY —
            LogEvent("PostSampleDelay");
            yield return new WaitForSeconds(PostSampleDelayDuration);
            // — TARGETON —
            LogEvent("TargetOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs2 = trial.GetStimIndices("TargetOn");
            var locs2 = trial.GetStimLocations("TargetOn");
            if (idxs2.Length > 0 && locs2.Length > 0)
            {
                var goList2 = Spawner.SpawnStimuli(idxs2, locs2);
                spawnedItems.AddRange(goList2);
                lastIdxs = idxs2;
            }

            yield return null;
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

            // strip out destroyed references
            spawnedItems.RemoveAll(go => go == null);
            GameObject targetGO = spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx);

            yield return null;
            // — FEEDBACK —
            LogEvent("Feedback");
            //   the blocks below are because of the IsFeedback checkmark
            // — feedback and beep —
            bool correct = answered && (pickedIdx == (lastIdxs.Length > 0 ? lastIdxs[0] : -1));
            //flash feedback
            if (targetGO != null)
                StartCoroutine(FlashFeedback(targetGO, correct));

            if (correct)
            {
                LogEvent("SelectingTarget");
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
            yield return new WaitForSeconds(FeedbackDuration);

            yield return null;
            // — RESET —
            LogEvent("Reset");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);


            //Block Handling
            int thisBlock = trial.BlockCount;
            int nextBlock = (_currentIndex + 1 < _trials.Count) ? _trials[_currentIndex+1].BlockCount : -1;
            

            //Only run when enabled
            if (PauseBetweenBlocks && nextBlock != thisBlock && nextBlock != -1)
            {
                LogEvent("StartEndBlock");

                if (PauseController != null)
                    yield return StartCoroutine(PauseController.ShowPause(nextBlock, _totalBlocks));
                _currentIndex ++;
                LogEvent("StartEndBlock");
                _currentIndex --;
            }
            // next trial
            lastBlock = thisBlock; // keep true last block
            _currentIndex++; // advance to next trial
        }

        _currentIndex--; // make sure log has current header
        // end of all trials
        LogEvent("StartEndBlock");
        LogEvent("AllTrialsComplete");
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(-1, _totalBlocks));// the +1 is to send it to end game state
    }

    IEnumerator ShowFeedback()
    {
        FeedbackText.canvasRenderer.SetAlpha(1f);
        yield return new WaitForSeconds(FeedbackDuration);
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
            if (Input.GetMouseButtonDown(0))
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
            ShowScoreUI    = !ShowScoreUI;
            ShowFeedbackUI = !ShowFeedbackUI;
            if (ScoreText    != null) ScoreText.gameObject.SetActive(ShowScoreUI);
            if (FeedbackText != null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        }
    }

    void OnDestroy()
    {
        if (CoinController.Instance != null)
            CoinController.Instance.OnCoinBarFilled -= () => _audioSrc.PlayOneShot(_coinBarFullBeep);
    }

        
    /// <summary>
    /// Always write the event into ALL_LOGS, and if it has a TTL code, also fire TTL_LOGS.
    /// </summary>
    private void LogEvent(string label)
    {

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
            yield return new WaitForSeconds(interval);

            // revert to original
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = originals[i];
            yield return new WaitForSeconds(interval);
        }
    }
}
