using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TaskSelectorUI : MonoBehaviour
{
    [Header("Task Menu")]
    public TaskMenuController menu;


    [Header("Session Setup")]
    public TMP_InputField SessionNameInput;
    public Button StartSessionButton;
    public GameObject SessionPanel;    //

    [Header("Task Grid")]
    public GameObject TasksPanel;      // should point at your GridPanel root

    public RectTransform ToggleContainer;     // assign the panel under SessionPanel
    public GameObject TogglePrefab;       // assign TaskSelectorToggle.prefab
    List<string> _selectedAcronyms = new List<string>();

    [Header("Audio")]
    public Button soundToggleButton;    // drag your Button here
    public Sprite soundOnSprite;        // drag the “speaker” icon here
    public Sprite soundOffSprite;       // drag the “muted” icon here

    bool _controlsVisible = false;
    bool _soundOn = true;
    Image _soundBtnImage;

    [Header("Settings Panel")]
    public SettingsUIController SettingsUI;

    [Header("Controls-Visibility Toggle")]
    public Button ToggleControlsButton;     // new eye icon
    public Sprite ControlsOpenSprite;       // show-icon
    public Sprite ControlsClosedSprite;     // hide-icon
    public Button AdvancedSettingsButton; // button to open advanced settings
    public Button ExitButton; // self explanatory

    void Start()
    {
        // sanity‐checks
        if (SessionNameInput == null) Debug.LogError("SessionNameInput is null!");
        if (StartSessionButton == null) Debug.LogError("StartSessionButton is null!");
        if (SessionPanel == null) Debug.LogError("SessionPanel is null!");
        if (TasksPanel == null) Debug.LogError("TasksPanel is null!");

        // 0a) If we've already initialized a session (SessionName non-empty), skip session setup and go straight to task grid
        if (SessionManager.Instance.SessionStarted)
        {
            // hide session panel, show tasks
            SessionPanel.SetActive(false);
            TasksPanel.SetActive(true);

            // restore exactly what they chose
            _selectedAcronyms = new List<string>(SessionManager.Instance.SelectedTasks);
            menu.PopulateTasks(_selectedAcronyms);
            ControlsUISetup();
            return;
        }
        ControlsUISetup();

        // 0) Build your toggles
        foreach (var def in Resources.LoadAll<ExperimentDefinition>("ExperimentDefinitions"))
        {
            var go = Instantiate(TogglePrefab, ToggleContainer);
            var tt = go.GetComponent<TaskToggle>();
            tt.Init(def.Acronym);
            _selectedAcronyms.Add(def.Acronym);

            // when user toggles, update our list
            tt.OnValueChanged += (acr, isOn) =>
            {
                if (isOn && !_selectedAcronyms.Contains(acr))
                    _selectedAcronyms.Add(acr);
                else if (!isOn)
                    _selectedAcronyms.Remove(acr);
            };
        }

        // Cool Greeting!  :)
        Debug.Log(
            "<i><color=white><b>N</b>euro-<b>E</b>xperimental <b>R</b>untime for <b>V</b>anderbilt</color>\n"
        + "<b><color=#00FFF7>Questions?</color></b> <color=#AAAAAA>→</color> <b><color=#FFFF00>kyle@coutray.com</color></b></i>"
        );


        Debug.Log(
            "<b><size=14><color=white><<: ¤=[</color> <color=#FF2A50><b>WELCOME TO NERV</b></color> <color=white>]=¤ :>></color></size></b> "
        + "<i><color=white>• A Unity-based framework for neuroscience experiment execution</color></i>\n"
        + " "
        );



        // hide grid & leave session UI up front
        TasksPanel.SetActive(false);


        StartSessionButton.onClick.RemoveAllListeners();
        StartSessionButton.onClick.AddListener(OnStartSessionClicked);
    }

    void Update()
    {
        // if user presses the '`' key...
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            // start testing‐pulse coroutine. testing the communication.
            StartCoroutine(SendTestingPulses());
        }
    }

    public void OnStartSessionClicked()
    {

        SessionManager.Instance.Initialize(SessionNameInput.text);

        // start logging
        SessionLogManager.Instance.InitializeSerialPort();

        // lock down two COM controls
        SettingsUI.portInput.interactable = false;
        SettingsUI.testModeToggle.interactable = false;

        // Copy the toggles they just chose into the SessionManager
        SessionManager.Instance.SelectedTasks.Clear();
        SessionManager.Instance.SelectedTasks.AddRange(_selectedAcronyms);

        // disable the session UI
        SessionNameInput.interactable = false;
        StartSessionButton.interactable = false;
        SessionPanel.SetActive(false);

        // now populate only the selected scenes
        var menu = FindObjectOfType<TaskMenuController>();
        menu.PopulateTasks(_selectedAcronyms);

        // enable the grid
        TasksPanel.SetActive(true);

        // send testing TTL pulses if not test mode, debug only if so.
        StartCoroutine(SendTestingPulses());

    }
    public void ControlsUISetup()
    {
        if (SessionManager.Instance.SessionStarted)
        {
            // lock down two COM controls
            SettingsUI.portInput.interactable = false;
            SettingsUI.testModeToggle.interactable = false;
        }

        _soundOn = true;
        AudioListener.volume = 1f;
        soundToggleButton.gameObject.SetActive(false);
        SettingsUI.settingsButton.gameObject.SetActive(false);
        ExitButton.gameObject.SetActive(false);
        SettingsUI.advancedSettingsContainer.gameObject.SetActive(false);

        // Audio toggle setup
        if (soundToggleButton == null)
            Debug.LogError("SoundToggleButton is not assigned!");
        else
        {
            _soundBtnImage = soundToggleButton.GetComponent<Image>();
            if (_soundBtnImage == null)
                Debug.LogError("SoundToggleButton needs an Image component!");
            else
                _soundBtnImage.sprite = soundOnSprite;

            soundToggleButton.onClick.AddListener(OnSoundToggleClicked);
        }

        if (ExitButton != null)
            ExitButton.onClick.AddListener(OnExitButtonClicked);

        // hook up the new toggle
        if (ToggleControlsButton != null)
            ToggleControlsButton.onClick.AddListener(OnToggleControls);

        // toggle hover code:
        // add hover effect on controls‐visibility button
        var trigger = ToggleControlsButton.gameObject.AddComponent<EventTrigger>();
        // on pointer enter, show the open sprite
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) =>
        {
            ToggleControlsButton.image.sprite = ControlsOpenSprite;
        });
        trigger.triggers.Add(entryEnter);
        // on pointer exit, restore based on current visibility state
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) =>
        {
            ToggleControlsButton.image.sprite = _controlsVisible ? ControlsOpenSprite : ControlsClosedSprite;
        });
        trigger.triggers.Add(entryExit);

    }
    public void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        // Stop play-mode in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the standalone build
        Application.Quit();
#endif

    }
    public void OnTaskIconClicked(string sceneName)
    {
        if (string.IsNullOrEmpty(SessionManager.Instance.SessionName))
        {
            Debug.LogError("[TSUI] Session not started yet!");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    private void OnSoundToggleClicked()
    {
        // flip state
        _soundOn = !_soundOn;

        // set master volume: 1 = on, 0 = off
        AudioListener.volume = _soundOn ? 1f : 0f;

        // swap the button’s icon
        if (_soundBtnImage != null)
            _soundBtnImage.sprite = _soundOn
                ? soundOnSprite
                : soundOffSprite;
    }


    public void OnToggleControls()
    {
        // flip the flag
        _controlsVisible = !_controlsVisible;

        // change the toggle’s icon
        var img = ToggleControlsButton.GetComponent<Image>();
        if (img != null)
            img.sprite = _controlsVisible
                            ? ControlsOpenSprite
                            : ControlsClosedSprite;

        // ALWAYS hide the settings‐panel itself when you collapse
        SettingsUI.settingsPanel.SetActive(false);
        SettingsUI._isOpen = false;

        SettingsUI.advancedSettingsContainer.SetActive(false);
        SettingsUI.


        // show/hide the 3 buttons
        ExitButton.gameObject.SetActive(_controlsVisible);
        soundToggleButton.gameObject.SetActive(_controlsVisible);
        SettingsUI.settingsButton.gameObject.SetActive(_controlsVisible);
        SettingsUI._buttonImage.sprite = !_controlsVisible ? SettingsUI.closeIcon : SettingsUI.openIcon;

    }

    private IEnumerator SendTestingPulses()
    {
        if (!SessionLogManager.Instance.testMode)
        {
            // When actually sending pulses (TEST MODE OFF)
            Debug.Log("<color=yellow><b> TTL Test Mode OFF:</b> Sending 3 test pulses to verify serial-port communication. Check your hardware now.</color>");

            // send three 0xFF pulses, half a second apart
            for (int i = 0; i < 3; i++)
            {
                SessionLogManager.Instance.SendRawByte(0xFF);
                yield return new WaitForSeconds(0.5f);
            }
        }

        else
            Debug.Log("<color=yellow><b> TTL Test Mode ON:</b> Skipping real pulses—simulation only.</color>");
            // When simulating only (TEST MODE ON)
    }
}