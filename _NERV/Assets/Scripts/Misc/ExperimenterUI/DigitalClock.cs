using UnityEngine;
using TMPro;
using System;

public class DigitalClock : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;

    void Awake()
    {
        if (clockText == null)
            Debug.LogError("DigitalClock: assign a TextMeshProUGUI in the Inspector!");
    }

    void OnEnable()
    {
        // update immediately, then once per second
        UpdateClock();
        InvokeRepeating(nameof(UpdateClock), 1f, 1f);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(UpdateClock));
    }

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        string hh = now.ToString("HH");
        string mm = now.ToString("mm");
        string ss = now.ToString("ss");
        clockText.text = $"{hh}:<color=#CF574A>{mm}</color>:{ss}";
    }
}
