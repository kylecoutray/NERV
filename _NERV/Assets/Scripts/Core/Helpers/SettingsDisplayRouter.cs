using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class SettingsDisplayRouter : MonoBehaviour
{
    [Tooltip("Drag your TaskSelector Canvas here (or leave blank to auto-find by tag)")]
    public Canvas uiCanvas;

    [Tooltip("Drag your TaskSelector Camera here (or leave blank to auto-find by tag)")]
    public Camera settingsCamera;

    private Camera _cam;

    void Awake()
    {
        // Cache camera reference if assigned in Inspector, else try grabbing from this GameObject.
        _cam = settingsCamera != null 
             ? settingsCamera 
             : GetComponent<Camera>();

        if (_cam == null)
            Debug.LogWarning("SettingsDisplayRouter: no Camera found on this GameObject or via Inspector.");

        if (uiCanvas == null)
            Debug.LogWarning("SettingsDisplayRouter: uiCanvas not assigned in Inspector.");
    }

    void OnEnable()
    {
        // Hook scene‐load and mode‐change
        SceneManager.sceneLoaded       += OnSceneLoaded;
        DisplayManager.OnModeChanged  += RouteDisplay;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded       -= OnSceneLoaded;
        DisplayManager.OnModeChanged  -= RouteDisplay;
    }

    void Start()
    {
        // Only route once everything exists
        if (DisplayManager.I == null) return;
        if (_cam == null || uiCanvas == null) return;
        RouteDisplay(DisplayManager.I.CurrentMode);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        // Whenever we load a new scene, re-find the Camera & Canvas if needed
        if (scene.name != "TaskSelector") return;

        if (settingsCamera == null)
        {
            var camGO = GameObject.FindWithTag("SettingsCam");
            settingsCamera = camGO?.GetComponent<Camera>();
            _cam = settingsCamera ?? Camera.main;
        }
        if (uiCanvas == null)
        {
            var cvGO = GameObject.FindWithTag("SettingsCanvas");
            uiCanvas = cvGO?.GetComponent<Canvas>();
        }

        // And then route
        if (_cam != null && uiCanvas != null)
            RouteDisplay(DisplayManager.I.CurrentMode);
    }

    private void RouteDisplay(DisplayManager.Mode mode)
    {
        // Only in TaskSelector scene do we reroute
        if (SceneManager.GetActiveScene().name != "TaskSelector") return;

        bool dual = mode == DisplayManager.Mode.DualHuman || mode == DisplayManager.Mode.DualMonkey;
        int target = dual ? 1 : 0;

        _cam.targetDisplay       = target;
        uiCanvas.targetDisplay   = target;
        Debug.Log($"[SettingsRouter] TaskSelector → routing to Display {target+1}");
    }
}
