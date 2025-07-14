using UnityEngine;
using UnityEngine.UI;

public class AdvancedSettingsPanel : MonoBehaviour
{
    [Header("Display 1 Buttons")]
    public Button humanButton;
    public Button monkeyButton;

    [Header("Display 2 Buttons")]
    public Button expButton;
    public Button noneButton;

    [Header("Monitor Icons (inside frames)")]
    public Image monitor1Content;
    public Image monitor2Content;
    public Sprite humanSprite;
    public Sprite monkeySprite;
    public Sprite expSprite;
    public Sprite noneSprite;

    [Header("Save")]
    public Button saveButton;  // optional

    bool _firstIsHuman  = true;
    bool _secondEnabled = false;

    void Awake()
    {
        humanButton.onClick  .AddListener(() => SelectFirst(true));
        monkeyButton.onClick .AddListener(() => SelectFirst(false));
        expButton.onClick    .AddListener(() => SelectSecond(true));
        noneButton.onClick   .AddListener(() => SelectSecond(false));

        if (saveButton != null)
            saveButton.onClick.AddListener(() => DisplayManager.I.SaveConfig());

        // initialize from saved mode
        var m = DisplayManager.I.CurrentMode;
        _firstIsHuman  = (m == DisplayManager.Mode.SingleHuman || m == DisplayManager.Mode.DualHuman);
        _secondEnabled = (m == DisplayManager.Mode.DualHuman || m == DisplayManager.Mode.DualMonkey);

        UpdateUIAndApply();
    }

    void SelectFirst(bool isHuman)
    {
        Debug.Log($"[AdvancedSettings] SelectFirst({isHuman})");
        _firstIsHuman = isHuman;
        if (!isHuman) _secondEnabled = true;  // monkey always forces dual
        UpdateUIAndApply();
    }

    void SelectSecond(bool enabled)
    {
        Debug.Log($"[AdvancedSettings] SelectSecond({enabled})");
        if (!_firstIsHuman) return;  // can’t toggle second if monkey on
        _secondEnabled = enabled;
        UpdateUIAndApply();
    }

    void UpdateUIAndApply()
    {
        // 1) swap the **inner** icons
        monitor1Content.sprite = _firstIsHuman ? humanSprite : monkeySprite;
        monitor2Content.sprite = _secondEnabled ? expSprite      : noneSprite;

        humanButton.interactable  = !_firstIsHuman;
        monkeyButton.interactable =  _firstIsHuman;
        expButton.interactable    =  _firstIsHuman && !_secondEnabled;
        noneButton.interactable   =  _firstIsHuman &&  _secondEnabled;


        // 3) apply immediately
        var mode = DisplayManager.Mode.SingleHuman;
        if (_secondEnabled)
            mode = _firstIsHuman
                   ? DisplayManager.Mode.DualHuman
                   : DisplayManager.Mode.DualMonkey;

        DisplayManager.I.ApplyDisplayMode(mode);
    }
}
