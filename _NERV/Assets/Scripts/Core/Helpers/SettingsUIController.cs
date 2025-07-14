using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI References")]
    public Button     settingsButton;    // your gear icon Button
    public Sprite     openIcon;          // e.g. “gear-outline”
    public Sprite     closeIcon;         // e.g. “gear-filled”
    public GameObject settingsPanel;     // the panel root
    public Button    advancedSettingsButton; // button to open advanced settings
    public GameObject advancedSettingsContainer; // container for advanced settings panel
    public Button     ExitButton;        // optional exit button to close the app

    [Header("Panel Fields")]
    public TMP_InputField portInput;     // drag your COM-port input here
    public Toggle          testModeToggle;  // drag your test-mode toggle here

    public bool _isOpen = false;
    public Image _buttonImage;

    void Start()
    {
        // sanity-check
        if (settingsButton == null || settingsPanel == null
          || portInput == null || testModeToggle == null || advancedSettingsButton == null)
        {
            Debug.LogError("[SettingsUI] assign all references!");
            enabled = false;
            return;
        }

        // cache
        _buttonImage = settingsButton.GetComponent<Image>();
        if (_buttonImage == null)
            Debug.LogError("[SettingsUI] settingsButton needs an Image!");

        // start closed
        settingsPanel.SetActive(false);
        _buttonImage.sprite = openIcon;

        // hook up open/close logic
        settingsButton.onClick.AddListener(ToggleSettingsPanel);

        advancedSettingsButton.onClick.AddListener(() =>
        {
            advancedSettingsContainer.SetActive(!advancedSettingsContainer.activeSelf);
        });

        // initialize fields from SessionLogManager
        portInput.text = SessionLogManager.Instance.portName;
        testModeToggle.isOn = SessionLogManager.Instance.testMode;

        // wire up live updates
        portInput.onEndEdit.AddListener(OnPortChanged);
        testModeToggle.onValueChanged.AddListener(OnTestModeChanged);
    }

    void ToggleSettingsPanel()
    {
        _isOpen = !_isOpen;
        settingsPanel.SetActive(_isOpen);
        advancedSettingsContainer.SetActive(false); // close advanced settings if open
        _buttonImage.sprite = _isOpen ? closeIcon : openIcon;
    }

    void OnPortChanged(string newPort)
    {
        SessionLogManager.Instance.portName = newPort;
    }

    void OnTestModeChanged(bool isOn)
    {
        SessionLogManager.Instance.testMode = isOn;

    }
}