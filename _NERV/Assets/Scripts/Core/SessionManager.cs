using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public string SessionName   { get; private set; }
    public string SessionTs     { get; private set; }
    public string SessionFolder { get; private set; }

    //track this so we can skip the session panel when returning
    public bool SessionStarted { get; private set; } = false;
    /// <summary>
    /// The list of task acronyms the user selected at session start.
    /// Persisted across scene changes so toggles stay remembered.
    /// </summary>
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

        // auto‐init default if none yet (so you can hit Play on any scene), but don’t mark as "started"
        if (string.IsNullOrEmpty(SessionName))
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

        // MASTER_LOGS root just above Assets folder
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string masterRoot  = Path.Combine(projectRoot, "MASTER_LOGS");
        Directory.CreateDirectory(masterRoot);

        // e.g. MASTER_LOGS/Session_20250623_153012
        SessionFolder = Path.Combine(masterRoot, $"{SessionName}_{SessionTs}");
        Directory.CreateDirectory(SessionFolder);

    }
}
