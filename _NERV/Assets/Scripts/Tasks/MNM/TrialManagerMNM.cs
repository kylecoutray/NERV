using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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

public class TrialManagerMNM : MonoBehaviour
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

    [Header("Match/Non-Match Options")]
    public GameObject MatchPrefab;      // drag in your “match.png” prefab
    public GameObject NonMatchPrefab;   // drag in your “nonmatch.png” prefab
    public float      ChoiceY = 1.5f;   // vertical offset for match (non-match is -ChoiceY)


    [Header("Fixation Dot")]
    public GameObject FixationDotPrefab;    // drag your Prefab here
    public float      FixationDotSize = 0.2f; // world‐units diameter
    public bool UseFixationDot = false;


    private Dictionary<string,int> TTLEventCodes = new Dictionary<string,int>
    {
        { "Fixation", 1 },
        { "CueOn", 2 },
        { "SampleOn", 3 },
        { "Choice", 4 },
        { "Feedback", 5 },
    };

    void Start()
    {
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
        LogTTL("StartEndBlock");
        int lastBlock = _trials[0].BlockCount;

        //Start with paused first scene
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(lastBlock, _totalBlocks));

        while (_currentIndex < _trials.Count)
        {
            var trial = _trials[_currentIndex];
            var spawnedItems = new List<GameObject>();
            int[] lastIdxs = new int[0];

            // — IDLE —
            LogTTL("Idle");
            yield return new WaitForSeconds(IdleDuration);


            // — FIXATION —
            LogTTL("Fixation");

            /////////////////////////////////
            // custom code for this MNM game:
            // 1) spawn & size
            if (UseFixationDot == true) 
            {
                GameObject fixDot = null;
                if (FixationDotPrefab != null)
                {
                    fixDot = Instantiate(
                        FixationDotPrefab,
                        Vector3.zero,                    // world‐center
                        Quaternion.identity,
                        this.transform                   // parent under your manager for cleanliness
                    );
                    fixDot.transform.localScale = Vector3.one * FixationDotSize;
                }

                // 2) wait out
                yield return new WaitForSeconds(FixationDuration);

                // 3) cleanup
                if (fixDot != null)
                    Destroy(fixDot);
            }


            // — CUEON —
            LogTTL("CueOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs1 = trial.GetStimIndices("CueOn");
            var locs1 = trial.GetStimLocations("CueOn");
            if (idxs1.Length > 0 && locs1.Length > 0)
            {
                var goList1 = Spawner.SpawnStimuli(idxs1, locs1);
                spawnedItems.AddRange(goList1);
                lastIdxs = idxs1;
            }

            yield return new WaitForSeconds(CueOnDuration);
            // — CUEOFF —
            LogTTL("CueOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            // — DELAY1 —
            LogTTL("Delay1");
            yield return new WaitForSeconds(Delay1Duration);
            // — SAMPLEON —
            LogTTL("SampleOn");
            //   the next 7 lines are because of the IsStimulus checkmark.
            var idxs2 = trial.GetStimIndices("SampleOn");
            var locs2 = trial.GetStimLocations("SampleOn");
            if (idxs2.Length > 0 && locs2.Length > 0)
            {
                var goList2 = Spawner.SpawnStimuli(idxs2, locs2);
                spawnedItems.AddRange(goList2);
                lastIdxs = idxs2;
            }

            yield return new WaitForSeconds(SampleOnDuration);
            // — SAMPLEOFF —
            LogTTL("SampleOff");
            //   the stuff below is because of the IsClearAll checkmark
            Spawner.ClearAll();

            yield return null;
            // — DELAY2 —
            LogTTL("Delay2");
            yield return new WaitForSeconds(Delay2Duration);


            // — CHOICE —
            ////////////////////////
            /// Custom CHOICE AND FEEDBACK Logic for this Game. 
            // — CHOICE OPTIONS —  
            LogTTL("Choice");

            // 1) Determine correct answer  
            //    true ⇒ match is correct, false ⇒ non-match
            int cueIdx    = trial.GetStimIndices("CueOn")[0];
            int sampleIdx = trial.GetStimIndices("SampleOn")[0];
            bool isMatch  = (cueIdx == sampleIdx);
            int correctIdx = isMatch ? 0 : 1;  // we’ll assign 0→match, 1→non-match

            // 2) Spawn two buttons  
            var choiceItems = new List<GameObject>();

            // match (index 0)  
            var goMatch = Instantiate(
                MatchPrefab,
                new Vector3(0,  ChoiceY, 0),
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
            bool answered  = false;
            int  pickedIdx = -1;
            float rt       = 0f;

            yield return StartCoroutine(WaitForChoice((i, t) =>
            {
                answered  = true;
                pickedIdx = i;
                rt        = t;
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
                LogTTL("Success");
                _audioSrc.PlayOneShot(_correctBeep);
                LogTTL("AudioPlaying");  // if you want TTL on the beep
                FeedbackText.text = $"+{PointsPerCorrect}";
            }
            else
            {
                _score += PointsPerWrong;
                LogTTL(answered ? "Fail" : "Timeout");
                _audioSrc.PlayOneShot(_errorBeep);
                LogTTL("AudioPlaying");  // if you want TTL on the beep
                FeedbackText.text = answered ? "Wrong!" : "Too Slow!";
            }

            UpdateScoreUI();

            // 8) Coin feedback (optional)  
            if (answered && UseCoinFeedback)
            {
                var clickPos = Input.mousePosition;
                if (correct) CoinController.Instance.AddCoinsAtScreen(CoinsPerCorrect, clickPos);
                else         CoinController.Instance.RemoveCoins(1);
            }

            // 9) wait out the feedback UI
            yield return new WaitForSeconds(FeedbackDuration);
    
            if (ShowFeedbackUI)       // fade‐out
                StartCoroutine(ShowFeedback());

            LogTTL("Reset");
            // 10) Clean up choice items  
            foreach (var go in choiceItems)
                Destroy(go);

            //Block Handling
            trial = _trials[_currentIndex];
            int thisBlock = trial.BlockCount + 1;
            _currentIndex++;

            //Only run when enabled
            if (PauseBetweenBlocks && thisBlock != lastBlock && thisBlock <= _totalBlocks)
            {
                _currentIndex--; //This is so the log file has the correct header
                LogTTL("StartEndBlock");
                _currentIndex++; //Put the value back for the rest of the trials

                if (PauseController != null)
                    yield return StartCoroutine(PauseController.ShowPause(thisBlock, _totalBlocks));
                lastBlock = thisBlock;
                LogTTL("StartEndBlock");
            }
        }

        // end of all trials
        LogTTL("StartEndBlock");
        LogTTL("AllTrialsComplete");
        if (PauseController != null)
            yield return StartCoroutine(PauseController.ShowPause(lastBlock+1, _totalBlocks));// the +1 is to send it to end game state
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
                LogTTL("Clicked");
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
            if (_currentIndex >= _trials.Count) //this is for post RunTrials Log Calls. 
            {
                // the -1 is to ensure it has the correct header
                // we increment to break out of our old loops, but still need this to be labeled correctly
                _currentIndex--;
            }

            LogManager.Instance.LogEvent(label, _trials[_currentIndex].TrialID);
            if (TTLEventCodes.TryGetValue(label, out int code))
                SerialTTLManager.Instance.LogEvent(label, code);

            //If you want SerialTTLManager to also log all events, uncomment this below.
            //else
                //SerialTTLManager.Instance.LogEvent(label);
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

        const int   flashes  = 1;
        const float interval = 0.3f;

        for (int f = 0; f < flashes; f++)
        {
            // swap in flash material
            foreach (var r in rends)
                r.material = flashMat;
            yield return new WaitForSeconds(interval);

            // restore originals
            for (int i = 0; i < rends.Length; i++)
                rends[i].material = origMats[i];
            yield return new WaitForSeconds(interval);
        }

        // cleanup
        UnityEngine.Object.Destroy(flashMat);
    }

}
