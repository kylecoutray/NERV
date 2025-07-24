using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using TMPro;

public class TrialProgressDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI progressText;

    private SessionManager sm;
    public float sessionStartRealtime;

    // Set by SessionLogManager.RegisterTrialManager(...)
    private dynamic tm;
    private string _currentTaskAcronym;

    // For unsubscribing trial start event
    private Delegate _onTrialStartDelegate;

    void Awake()
    {
        StartCoroutine(InitializeAfterEverything());
    }

    void OnDestroy()
    {
        Unsubscribe();
        CancelInvoke(nameof(UpdateText));
    }

    void OnDisable()
    {
        Unsubscribe();
        CancelInvoke(nameof(UpdateText));
    }

    /// <summary>
    /// Called by SessionLogManager in OnSceneLoaded:
    ///     ui.RegisterTrialManager(tm, acr);
    /// </summary>
    public void RegisterTrialManager(object manager, string acronym)
    {
        tm = manager;
        _currentTaskAcronym = acronym;

        // subscribe to OnTrialStart
        var ev = tm.GetType()
                .GetEvent("OnTrialStart", BindingFlags.Public | BindingFlags.Instance);
        if (ev != null)
        {
            _onTrialStartDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType, this, nameof(UpdateText), false);
            ev.AddEventHandler(tm, _onTrialStartDelegate);
        }

        // wait one frame so TrialManager.Start() has populated everything:
    }


    private void Unsubscribe()
    {
        if (tm == null || _onTrialStartDelegate == null) return;
        EventInfo ev = ((object)tm).GetType().GetEvent("OnTrialStart");
        ev?.RemoveEventHandler(tm, _onTrialStartDelegate);
        _onTrialStartDelegate = null;
    }

    private void UpdateText()
    {
        if (tm == null || sm == null) return;

        // Safely get summary
        SessionLogManager.TaskSummary summary;
        try
        {
            summary = tm.GetTaskSummary();
        }
        catch (Exception)
        {
            summary = new SessionLogManager.TaskSummary
            {
                TrialsTotal = 0,
                Accuracy = 0f,
                MeanRT_ms = 0f
            };
        }

        // Pull data
        string sessionName = sm.SessionName;
        string currentTrial = string.Empty;
        try
        {
            IList list = tm._trials as IList;
            int idx = tm._currentIndex;
            if (list != null && idx >= 0 && idx < list.Count)
            {
                dynamic trialObj = list[idx];
                currentTrial = trialObj.TrialID;
            }
        }
        catch { }

        // Total number of trials in the session (not just completed)
        int totalTrials = tm._trials.Count;
        int currentBlock = tm.thisBlock;
        int totalBlocks = tm._totalBlocks;
        float pctCorrect = summary.Accuracy * 100f;
        float avgRTms = summary.MeanRT_ms;

        // Elapsed time
        float e = Time.realtimeSinceStartup - sessionStartRealtime;
        int h = Mathf.FloorToInt(e / 3600f);
        int m = Mathf.FloorToInt((e % 3600f) / 60f);
        int s = Mathf.FloorToInt(e % 60f);
        string elapsed = $"{h:D2}:{m:D2}:{s:D2}";

        // Update UI
        progressText.text =
            $"<b>{sessionName}</b>\n\n" +
            $"<b>Current Trial:</b> {currentTrial}\n" +
            $"<b>Total Trials:</b>  {totalTrials}\n" +
            $"<b>Current Block:</b> {currentBlock}\n" +
            $"<b>Total Blocks:</b>  {totalBlocks}\n" +
            $"<b>% Correct:</b>     {pctCorrect:F1}%\n" +
            $"<b>Avg RT:</b>        {avgRTms:F0} ms\n" +
            $"<b>Elapsed Time:</b>  {elapsed}";
    }

    public string GetElapsedTimeStamp()
    {
        float elapsed = Time.realtimeSinceStartup - sessionStartRealtime;
        int mm = Mathf.FloorToInt(elapsed / 60f);
        int ss = Mathf.FloorToInt(elapsed % 60f);
        return $"[{mm:D2}:{ss:D2}]";
    }
    
    private IEnumerator InitializeAfterEverything()
    {
        // wait one frame (so TrialManager.Start has already run)
        yield return new WaitForEndOfFrame();
    

        // now it’s safe to grab it
        sm = FindObjectOfType<SessionManager>();
        if (sm == null)
        {
            Debug.LogError("TrialProgressDisplay: No SessionManager after scene load!");
            yield break;
        }
        // do one initial draw
        UpdateText();
        // then start your repeating updates
        InvokeRepeating(nameof(UpdateText), 1f, 1f);
    }

}
