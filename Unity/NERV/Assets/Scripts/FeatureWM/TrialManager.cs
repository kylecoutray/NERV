using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public enum TrialState
{
    Idle,
    SampleOn,
    PostSampleDelay,
    DistractorsOn,
    PreTargetDelay,
    SearchPhase,
    Feedback
}

public class TrialManager : MonoBehaviour
{
    [Header("References")]
    public StimulusSpawner Spawner;
    public Camera PlayerCamera;   // assign your main camera
    public TMP_Text FeedbackText;
    public TMP_Text ScoreText;


    [Header("Timing & Scoring")]
    public float IdleDuration = 2f; // Wait before each sample
    public float MaxResponseTime = 10f;
    public float FeedbackDuration = 1f;
    public int PointsPerCorrect = 2;
    public int PointsPerWrong = -1;


    private List<TrialConfig> _trials;
    private int _currentIndex;
    private int _score = 0;

    //Audio beeps
    private AudioSource _audioSrc;
    private AudioClip _correctBeep;
    private AudioClip _errorBeep;
    private AudioClip _coinBarFullBeep;

    [Header("Coin Feedback")]
    public bool UseCoinFeedback = true;
    public GameObject CoinUI;
    public int CoinsPerCorrect = 2;

    [Header("UI Toggles")]
    public bool ShowScoreUI = true;
    public bool ShowFeedbackUI = true;


    void Start()
    {
        Debug.Log($"ConfigManager.Instance = {ConfigManager.Instance}");
        Debug.Log($"Spawner = {Spawner}");
        // grab all trials for this task
        _trials = ConfigManager.Instance.Trials;
        _currentIndex = 0;
        UpdateScoreUI();

        _audioSrc = GetComponent<AudioSource>();
        _correctBeep = Resources.Load<AudioClip>("AudioClips/correctBeep");
        _errorBeep = Resources.Load<AudioClip>("AudioClips/errorBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/coinBarFullBeep");


        if (!_correctBeep) Debug.LogError("Failed to load correctBeep");
        if (!_errorBeep) Debug.LogError("Failed to load errorBeep");
        if (!_coinBarFullBeep) Debug.LogError("Failed to load coinBarFullBeep");


        if (CoinUI != null)
            CoinUI.SetActive(UseCoinFeedback);

        if (FeedbackText != null)
            FeedbackText.gameObject.SetActive(ShowFeedbackUI);

        if (ScoreText != null)
            ScoreText.gameObject.SetActive(ShowScoreUI);

        CoinController.Instance.OnCoinBarFilled += () =>
        {
            if (_coinBarFullBeep != null)
                _audioSrc.PlayOneShot(_coinBarFullBeep);
        };



        SerialTTLManager.Instance.LogEvent("StartEndBlock");
        StartCoroutine(RunTrials());
    }



    IEnumerator RunTrials()
    {
        while (_currentIndex < _trials.Count)
        {
            var trial = _trials[_currentIndex];

            // — TRIAL ON —
            LogManager.Instance.LogEvent("TrialOn", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("TrialOn");

            // — IDLE —
            LogManager.Instance.LogEvent("Idle", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("Idle");
            yield return new WaitForSeconds(IdleDuration);

            // — SAMPLE ON —
            LogManager.Instance.LogEvent("SampleOn", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("SampleOn");
            var sampleGO = Spawner.SpawnStimuli(
                new int[] { trial.SearchStimIndices[0] },
                new Vector3[] { trial.SampleStimLocation }
            )[0];
            yield return new WaitForSeconds(trial.DisplaySampleDuration);

            Spawner.ClearAll();
            // mark sample off
            LogManager.Instance.LogEvent("SampleOff", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("SampleOff");

            // — POST-SAMPLE DELAY —
            LogManager.Instance.LogEvent("PostSampleDelay", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("PostSampleDelay");
            yield return new WaitForSeconds(trial.PostSampleDelayDuration);

            // — DISTRACTORS —
            if (trial.PostSampleDistractorStimIndices.Length > 0)
            {
                LogManager.Instance.LogEvent("DistractorOn", trial.TrialID);
                SerialTTLManager.Instance.LogEvent("DistractorOn");
                Spawner.SpawnStimuli(
                    trial.PostSampleDistractorStimIndices,
                    trial.PostSampleDistractorStimLocations
                );
                yield return new WaitForSeconds(trial.DisplayPostSampleDistractorsDuration);

                Spawner.ClearAll();
                LogManager.Instance.LogEvent("DistractorOff", trial.TrialID);
                SerialTTLManager.Instance.LogEvent("DistractorOff");
            }

            // — PRE-TARGET DELAY —
            LogManager.Instance.LogEvent("PreTargetDelay", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("PreTargetDelay");
            yield return new WaitForSeconds(trial.PreTargetDelayDuration);

            // — SEARCH PHASE (TargetOn) —
            LogManager.Instance.LogEvent("TargetOn", trial.TrialID);
            SerialTTLManager.Instance.LogEvent("TargetOn");
            List<GameObject> spawnedItems = Spawner.SpawnStimuli(
                trial.SearchStimIndices,
                trial.SearchStimLocations
            );

            // — WAIT FOR CHOICE OR TIMEOUT —
            bool answered = false;
            int pickedIdx = -1;
            float reactionT = 0f;

            yield return StartCoroutine(WaitForChoice((idx, rt) =>
            {
                answered = true;
                pickedIdx = idx;
                reactionT = rt;
            }));

            // decide which GameObject was clicked
            GameObject targetGO = (pickedIdx >= 0)
                ? spawnedItems.Find(go => go.GetComponent<StimulusID>().Index == pickedIdx)
                : null;


            // — FEEDBACK & BEEP & HALO —  
            bool correct = answered && (pickedIdx == trial.SearchStimIndices[0]);

            if (correct)
            {
                SerialTTLManager.Instance.LogEvent("Choice");
                LogManager.Instance.LogEvent("Choice", trial.TrialID);
                _score += PointsPerCorrect;

                // only play correct beep if not full -- else play coinBarFull beep
                if (!CoinController.Instance.CoinBarWasJustFilled)
                    _audioSrc.PlayOneShot(_correctBeep);

                SerialTTLManager.Instance.LogEvent("AudioPlaying");


                // log success
                SerialTTLManager.Instance.LogEvent("Success");
                FeedbackText.text = $"+{PointsPerCorrect}";


            }
            else
            {
                // deduct penalty for wrong answers
                _score += PointsPerWrong;
                UpdateScoreUI();

                _audioSrc.PlayOneShot(_errorBeep);
                SerialTTLManager.Instance.LogEvent("AudioPlaying");
                if (answered)
                {
                    SerialTTLManager.Instance.LogEvent("Choice");
                    LogManager.Instance.LogEvent("Choice", trial.TrialID);
                    SerialTTLManager.Instance.LogEvent("Fail");
                }
                else
                {
                    LogManager.Instance.LogEvent("Timeout", trial.TrialID);
                    SerialTTLManager.Instance.LogEvent("Timeout");
                    SerialTTLManager.Instance.LogEvent("Fail");
                }
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";


            }

            Vector2 clickScreenPos = Input.mousePosition;

            // only spawn coins if a stimulus was clicked
            if (pickedIdx >= 0 && UseCoinFeedback)
            {
                if (correct)
                {
                    // existing correct‐coin code…
                    CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);
                }
                else
                {
                    // **new: remove one coin from the bar**
                    CoinController.Instance.RemoveCoins(1);
                }
            }



            // **FLASH THE HALO** using your helper
            if (targetGO != null)
                StartCoroutine(
                    targetGO.GetComponent<CircleHalo>()
                            .CreateOneShot(correct, FeedbackDuration)
                );

            UpdateScoreUI();

            if (ShowFeedbackUI)
                FeedbackText.canvasRenderer.SetAlpha(1f);

            yield return new WaitForSeconds(FeedbackDuration);


            // — CLEANUP FOR NEXT TRIAL —
            foreach (var go in spawnedItems)
                Destroy(go);
            Spawner.ClearAll();

            if (ShowFeedbackUI)
                FeedbackText.CrossFadeAlpha(0f, 0.3f, false);

            _currentIndex++;
        }

        // — BLOCK END —
        LogManager.Instance.LogEvent("StartEndBlock", "AllTrialsDone");
        SerialTTLManager.Instance.LogEvent("StartEndBlock");
    }





    IEnumerator ShowFeedback()
    {
        FeedbackText.canvasRenderer.SetAlpha(1f);
        yield return new WaitForSeconds(FeedbackDuration);

        if (ShowFeedbackUI)
            FeedbackText.CrossFadeAlpha(0f, 0.3f, false);

    }

    void UpdateScoreUI()
    {
        if (ShowScoreUI && ScoreText != null)
            ScoreText.text = $"Score: {_score}";
        else if (ScoreText != null)
            ScoreText.text = "";
    }


    private IEnumerator WaitForChoice(System.Action<int, float> callback)
    {
        float startTime = Time.time;
        while (Time.time - startTime < MaxResponseTime)
        {
            if (Input.GetMouseButtonDown(0))
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
        // timeout
        callback(-1, MaxResponseTime);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowScoreUI    = !ShowScoreUI;
            ShowFeedbackUI = !ShowFeedbackUI;

            if (ScoreText != null)
                ScoreText.gameObject.SetActive(ShowScoreUI);

            if (FeedbackText != null)
                FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        }
    }


}
