using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TrialStateDEMO
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

public class TrialManagerDEMO : MonoBehaviour
{
    [Header("References")]
    public Camera PlayerCamera;
    public StimulusSpawner Spawner;
    public TMP_Text FeedbackText;
    public TMP_Text ScoreText;
    public GameObject CoinUI;

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
        if (Spawner == null) Spawner = FindObjectOfType<StimulusSpawner>();
        _trials       = GenericConfigManager.Instance.Trials;
        _currentIndex = 0;
        UpdateScoreUI();
        _audioSrc     = GetComponent<AudioSource>();
        _correctBeep  = Resources.Load<AudioClip>("AudioClips/correctBeep");
        _errorBeep    = Resources.Load<AudioClip>("AudioClips/errorBeep");
        _coinBarFullBeep = Resources.Load<AudioClip>("AudioClips/coinBarFullBeep");
        if (CoinUI     != null) CoinUI.SetActive(UseCoinFeedback);
        if (FeedbackText!= null) FeedbackText.gameObject.SetActive(ShowFeedbackUI);
        if (ScoreText   != null) ScoreText.gameObject.SetActive(ShowScoreUI);
        CoinController.Instance.OnCoinBarFilled += () => _audioSrc.PlayOneShot(_coinBarFullBeep);
        LogTTL("StartEndBlock");
        StartCoroutine(RunTrials());
    }

    IEnumerator RunTrials()
    {
        while (_currentIndex < _trials.Count)
        {
            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];

            // — IDLE —
            LogTTL("Idle");
            yield return new WaitForSeconds(IdleDuration);
            // — SAMPLEON —
            LogTTL("SampleOn");
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
            LogTTL("SampleOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return new WaitForSeconds(SampleOffDuration);
            // — POSTSAMPLEDELAY —
            LogTTL("PostSampleDelay");
            yield return new WaitForSeconds(PostSampleDelayDuration);
            // — TARGETON —
            LogTTL("TargetOn");
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
            LogTTL("Choice");
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
            LogTTL("Feedback");
            //   the blocks below are because of the IsFeedback checkmark
            // — feedback and beep —
            bool correct = answered && (pickedIdx == (lastIdxs.Length > 0 ? lastIdxs[0] : -1));
            if (correct)
            {
                LogTTL("SelectingTarget");
                _score += PointsPerCorrect;
                if (!CoinController.Instance.CoinBarWasJustFilled)
                    _audioSrc.PlayOneShot(_correctBeep);
                LogTTL("AudioPlaying");
                LogTTL("Success");
                FeedbackText.text = $"+{PointsPerCorrect}";
            }
            else
            {
                _score += PointsPerWrong;
                UpdateScoreUI();
                _audioSrc.PlayOneShot(_errorBeep);
                LogTTL("AudioPlaying");
                if (answered) { LogTTL("Choice"); LogTTL("Fail"); }
                else          { LogTTL("Timeout"); LogTTL("Fail"); }
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";
            }
            Vector2 clickScreenPos = Input.mousePosition;
            if (pickedIdx >= 0 && UseCoinFeedback)
            {
                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickScreenPos);
                else         CoinController.Instance.RemoveCoins(1);
            }
            UpdateScoreUI();
            if (ShowFeedbackUI) FeedbackText.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSeconds(FeedbackDuration);

            yield return null;
            // — RESET —
            LogTTL("Reset");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            if (ShowFeedbackUI) FeedbackText.CrossFadeAlpha(0f, 0.3f, false);
            _currentIndex++;
        }
        // end of all trials
        _currentIndex--;
        LogTTL("StartEndBlock");
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

    private void LogTTL(string label)
    {
        LogManager.Instance.LogEvent(label, _trials[_currentIndex].TrialID);
        if (TTLEventCodes.TryGetValue(label, out int code))
            SerialTTLManager.Instance.LogEvent(label, code);
        else
            SerialTTLManager.Instance.LogEvent(label);
    }
}
