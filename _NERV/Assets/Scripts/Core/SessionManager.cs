using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public string SessionName { get; private set; }
    public string SessionTs { get; private set; }
    public string SessionFolder { get; private set; }

    //track this so we can skip the session panel when returning
    public bool SessionStarted { get; private set; } = false;
    /// <summary>
    /// The list of task acronyms the user selected at session start.
    /// Persisted across scene changes so toggles stay remembered.
    /// </summary>
    public int frameRate;
    public List<string> SelectedTasks { get; private set; } = new List<string>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0;           // disable V-Sync
        Application.targetFrameRate = Screen.currentResolution.refreshRate;        // or your display’s Hz

        frameRate = Application.targetFrameRate;

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