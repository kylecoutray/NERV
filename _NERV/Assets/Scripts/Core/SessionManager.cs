using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public string SessionName { get; private set; }
    public string SessionTs   { get; private set; }
    public string SessionFolder { get; private set; }

    //track this so we can skip the session panel when returning
    public bool SessionStarted { get; private set; } = false;

    /// <summary>
    /// The list of task acronyms the user selected at session start.
    /// Persisted across scene changes so toggles stay remembered.
    /// </summary>
    private int frameRate;
    public List<string> SelectedTasks { get; private set; } = new List<string>();

    // Calibration integration
    /// <summary>
    /// Path to the user-chosen calibration JSON.
    /// Set by CalibrationUIController; consumed by CalibrationLoader.
    /// </summary>
    [HideInInspector]
    public string CalibrationPath { get; set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        frameRate = Screen.currentResolution.refreshRate;    // get the current screen refresh rate
        QualitySettings.vSyncCount = 1;                     // enable V-Sync for accurate stimulus timing
        Application.targetFrameRate = frameRate;            // set target FPS to screen refresh rate

        // Listen for scene loads to dispatch calibration
        SceneManager.sceneLoaded += OnSceneLoaded;

        // auto-init default if none yet (so you can hit Play on any scene), but don’t mark as "started"
        if (string.IsNullOrEmpty(SessionName)
            && SceneManager.GetActiveScene().name != "TaskSelector")
        {
            Initialize("Session");
            // reset started flag so TaskSelectorUI will show panel first
            SessionStarted = false;
        }
    }

    /// <summary>
    /// Whenever a new scene loads, if the user has set a CalibrationPath
    /// and there's a CalibrationLoader in the scene, re-load it.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(CalibrationPath)
            && CalibrationLoader.Instance != null
            && CalibrationLoader.Instance.enabled)
        {
            CalibrationLoader.Instance.LoadFromPath(CalibrationPath);
        }
    }

    /// <summary>
    /// Call once at TaskSelector start with your desired name.
    /// </summary>
    public void Initialize(string sessionName)
    {
        SessionStarted = true;
        SessionName = string.IsNullOrWhiteSpace(sessionName)
            ? "Session"
            : sessionName.Trim();
        SessionTs = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // figure out where "build root" is
        string root;
    #if UNITY_EDITOR
        // Editor: project folder (one up from Assets/)
        root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    #elif UNITY_STANDALONE_OSX
        // macOS: Application.dataPath = .../MyGame.app/Contents
        // so go up two to the folder containing MyGame.app
        root = new DirectoryInfo(Application.dataPath)
                    .Parent   // .../MyGame.app
                    .Parent   // folder containing MyGame.app
                    .FullName;
    #else
        // Windows & Linux: Application.dataPath = .../MyGame_Data
        // so go up one to the folder containing MyGame.exe
        root = Directory.GetParent(Application.dataPath).FullName;
    #endif

        // now create MASTER_LOGS there
        string masterRoot = Path.Combine(root, "MASTER_LOGS");
        Directory.CreateDirectory(masterRoot);

        // then per-session folder
        SessionFolder = Path.Combine(masterRoot, $"{SessionName}_{SessionTs}");
        Directory.CreateDirectory(SessionFolder);
    }
}
