using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

public class CustomDebugLogUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Text       logText;             // your single Text UI element
    public Scrollbar  verticalScrollbar;   // assign your top-to-bottom Scrollbar
    public Scrollbar  horizontalScrollbar; // assign your left-to-right Scrollbar
    public RectTransform viewport;         // RectTransform of DebugLogPanelUI (the mask)

    float _originalX;

    [Header("Settings")]
    public int visibleLines = 20;          // how many lines to show at once

    struct Entry { public string colored; }
    List<Entry> entries = new List<Entry>();

    int   currentIndex = 0;   // which entry is at the top of the window
    bool  autoScroll   = true;
    bool  pointerOver  = false;

    const float VERTICAL_THRESHOLD = 0.05f;  // how close to the top counts as “still watching”

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
        verticalScrollbar.onValueChanged.AddListener(OnVerticalScroll);
        horizontalScrollbar.onValueChanged.AddListener(OnHorizontalScroll);
        _originalX = logText.rectTransform.anchoredPosition.x;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        verticalScrollbar.onValueChanged.RemoveListener(OnVerticalScroll);
        horizontalScrollbar.onValueChanged.RemoveListener(OnHorizontalScroll);
    }

    void HandleLog(string msg, string stack, LogType type)
    {
        // 1) Color‐wrap only errors/warnings
        string line;
        if (type == LogType.Error)
            line = $"<color=red>{msg}</color>";
        else if (type == LogType.Warning)
            line = $"<color=yellow>{msg}</color>";
        else
            line = msg;  // leave any nested tags intact

        // 2) Insert at the front
        entries.Insert(0, new Entry{ colored = line });

        // 3) Cap the buffer
        if (entries.Count > 1000)
            entries.RemoveAt(entries.Count - 1);

        // 4) If we’re still at the “newest” position, keep it there
        if (autoScroll)
            currentIndex = 0;

        // 5) Refresh both sliders & the text window
        RefreshVertical();
        RefreshText();
    }

    void OnVerticalScroll(float value)
    {
        int maxIdx = Mathf.Max(0, entries.Count - visibleLines);
        currentIndex = Mathf.RoundToInt(value * maxIdx);
        autoScroll = (currentIndex == 0);
        RefreshText();
    }

    void OnHorizontalScroll(float value)
    {
        // 1) How wide the text actually is
        float textW = logText.preferredWidth;
        float viewW = viewport.rect.width;

        // 2) Resize the text container
        var rt = logText.rectTransform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textW);

        // 3) How far we can scroll
        float maxOffset = Mathf.Max(0f, textW - viewW);

        // 4) Compute scroll offset
        float offset = Mathf.Clamp01(value) * maxOffset;

        // 5) **Preserve** that original inset by subtracting from it
        Vector2 pos = rt.anchoredPosition;
        pos.x = _originalX - offset;     
        rt.anchoredPosition = pos;
    }


    // mouse-wheel support when pointer is over
    public void OnScroll(PointerEventData e)
    {
        if (!pointerOver || entries.Count <= visibleLines) return;
        int delta = e.scrollDelta.y > 0 ? 1 : -1;
        int maxIdx = Mathf.Max(0, entries.Count - visibleLines);
        currentIndex = Mathf.Clamp(currentIndex + delta, 0, maxIdx);
        autoScroll = (currentIndex == 0);
        RefreshVertical();
        RefreshText();
    }

    public void OnPointerEnter(PointerEventData e) => pointerOver = true;
    public void OnPointerExit(PointerEventData e)  => pointerOver = false;

    void RefreshVertical()
    {
        int maxIdx = Mathf.Max(0, entries.Count - visibleLines);
        verticalScrollbar.numberOfSteps = maxIdx + 1;
        verticalScrollbar.value = (maxIdx > 0) ? (float)currentIndex / maxIdx : 0f;
    }

    void RefreshText()
    {
        var sb = new StringBuilder();
        int end = Mathf.Min(currentIndex + visibleLines, entries.Count);
        for (int i = currentIndex; i < end; i++)
            sb.AppendLine(entries[i].colored);
        logText.text = sb.ToString();
    }
}
