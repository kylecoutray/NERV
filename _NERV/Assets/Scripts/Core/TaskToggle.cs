using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class TaskToggle : MonoBehaviour
{
    [Header("Toggle Colors")]
    public Color onColor  = Color.green;
    public Color offColor = Color.red;
    public string Acronym { get; private set; }
    public bool   IsOn    => _toggle != null && _toggle.isOn;

    // Fires whenever the user toggles this on/off
    public event Action<string,bool> OnValueChanged;

    Toggle   _toggle;
    TMP_Text _label;
    Image    _background;

    void Awake()
    {
        // cache references
        _toggle     = GetComponent<Toggle>();
        _label      = GetComponentInChildren<TMP_Text>();
        // Try to get the Image on the Toggle’s targetGraphic
        _background = _toggle.targetGraphic as Image ?? GetComponent<Image>();

        // initialize background color based on starting toggle state
        if (_background != null)
            _background.color = _toggle.isOn ? onColor : offColor;
        else
            Debug.LogWarning("[TaskToggle] No background Image found!");

        // subscribe to UI toggle changes
        _toggle.onValueChanged.AddListener(isOn =>
        {
            if (_background != null)
                _background.color = isOn ? onColor : offColor;

            OnValueChanged?.Invoke(Acronym, isOn);
        });
    }

    /// <summary>
    /// Call this once to initialize the toggle’s label and default state.
    /// </summary>
    public void Init(string acronym)
    {
        Acronym = acronym;
        if (_label != null)
            _label.text = acronym;
        else
            Debug.LogWarning("[TaskToggle] No TMP_Text child for label!");

        // ensure toggle is checked on start
        _toggle.isOn = true;
        if (_background != null)
            _background.color = onColor;
    }
}