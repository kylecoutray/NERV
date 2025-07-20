using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using TMPro;
using System.Linq;
using System.Reflection;


public class DisplayManager : MonoBehaviour
{
  public enum Mode
  {
    SingleHuman = 0,
    DualHuman = 1,
    DualMonkey = 2
  }

  [Serializable]
  private class ModeWrapper { public Mode mode; }

  public static DisplayManager I { get; private set; }
  public static event Action<Mode> OnModeChanged;



  public Mode CurrentMode { get; private set; }

  [Header("Overlays (Display 1)")]
  public GameObject humanOverlayPrefab;
  public GameObject monkeyOverlayPrefab;

  [Header("Experimenter UI Prefab (Display 2)")]
  public GameObject experimenterPrefab;

  private GameObject _humanOverlay;
  private GameObject _monkeyOverlay;
  private GameObject _experimenterUI;

  [Header("Pause UI")]
  [Tooltip("Drag your PauseCanvas here—even if it starts disabled")]
  [SerializeField] private Canvas pauseCanvas;
  private GameObject _pauseButton;

  private string ConfigPath => Path.Combine(Application.persistentDataPath, "displayConfig.json");

  void Awake()
  {
    // singleton
    if (I != null) { Destroy(gameObject); return; }
    I = this;

    // grab the PauseButton once by name
    _pauseButton = GameObject.Find("PauseButton");

    // re-apply whenever any scene loads
    SceneManager.sceneLoaded += OnSceneLoaded;
    LoadConfig();
    ApplyDisplayMode(CurrentMode);
  }

  void OnDestroy()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
  {
    ActivateDisplays();
    SetupDisplays();
    OnModeChanged?.Invoke(CurrentMode);
  }

  public void ApplyDisplayMode(Mode mode)
  {
    CurrentMode = mode;
    ActivateDisplays();
    SetupDisplays();
    OnModeChanged?.Invoke(mode);
    bool on = (mode != Mode.DualMonkey);
    SetTrialManagerScoreUI(on);
    SetTrialManagerFeedbackUI(on);

      if (_pauseButton != null) //hide pause button in DualMonkey mode
        _pauseButton.SetActive(mode != Mode.DualMonkey);
  }

  void ActivateDisplays()
  {
    // Always enable display 1
    if (Display.displays.Length > 0)
      Display.displays[0].Activate();

    // Enable display 2 only in dual modes
    if ((CurrentMode == Mode.DualHuman || CurrentMode == Mode.DualMonkey)
        && Display.displays.Length > 1)
    {
      Display.displays[1].Activate();
    }
  }

  void SetupDisplays()
  {
    // clear old overlays
    if (_humanOverlay != null) _humanOverlay.SetActive(false);
    if (_monkeyOverlay != null) _monkeyOverlay.SetActive(false);


    var sceneName = SceneManager.GetActiveScene().name;

    // 1) Human-waiting overlay on Display 1, **only** in TaskSelector
    if (CurrentMode == Mode.DualHuman && humanOverlayPrefab != null && sceneName == "TaskSelector")
    {
      if (_humanOverlay == null)
        _humanOverlay = Instantiate(humanOverlayPrefab);

      _humanOverlay.SetActive(true);
      var c = _humanOverlay.GetComponent<Canvas>();
      if (c != null) c.targetDisplay = 0;
    }

    // 2) Monkey overlay: ON in TaskSelector OR paused; OFF otherwise
    if (CurrentMode == Mode.DualMonkey
        && monkeyOverlayPrefab != null
        && (sceneName == "TaskSelector" || IsPaused()))
    {
      if (_monkeyOverlay == null)
        _monkeyOverlay = Instantiate(monkeyOverlayPrefab);

      _monkeyOverlay.SetActive(true);
      var mc = _monkeyOverlay.GetComponent<Canvas>();
      if (mc != null) mc.targetDisplay = 0;
    }

    // 3) Experimenter UI on Display 2 in non-TaskSelector scenes
    if ((CurrentMode == Mode.DualHuman || CurrentMode == Mode.DualMonkey)
        && sceneName != "TaskSelector"
        && experimenterPrefab != null)
    {
      // only create or find once
      if (_experimenterUI == null)
      {
        // 3a) try to find a scene-placed instance first (by name or tag)
        var existing = GameObject.Find("ExperimenterOverlay") ?? GameObject.FindWithTag("ExperimenterUI");
        if (existing != null)
        {
          _experimenterUI = existing;
        }
        else
        {
          // 3b) otherwise instantiate the prefab
          _experimenterUI = Instantiate(experimenterPrefab);
        }

        // now assign it to display 2
        var canvases = _experimenterUI.GetComponentsInChildren<Canvas>(true);
        if (canvases.Length == 0)
          Debug.LogError("DisplayManager: experimenterPrefab has no Canvas!");
        else
          foreach (var c in canvases)
            c.targetDisplay = 1;
      }
    }
  }
  // detects the PauseCanvas by name; tweak to use a tag or inspector reference

  public bool IsPaused()
  {
    // Find every Canvas in the scene, including inactive ones
    var allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
    foreach (var c in allCanvases)
    {
      if (c.gameObject.name == "PauseCanvas")
      {
        bool isActive = c.gameObject.activeInHierarchy;
        return isActive;
      }
    }

    Debug.LogError("DisplayManager.IsPaused: no Canvas named 'PauseCanvas' found!");
    return false;
  }


  void LoadConfig()
  {
    if (!File.Exists(ConfigPath))
    {
      CurrentMode = Mode.SingleHuman;
      return;
    }
    var json = File.ReadAllText(ConfigPath);
    CurrentMode = JsonUtility.FromJson<ModeWrapper>(json).mode;
  }

  public void SaveConfig()
  {
    var w = new ModeWrapper { mode = CurrentMode };
    File.WriteAllText(ConfigPath, JsonUtility.ToJson(w, true));
  }

  /// <summary>
  /// Public entry point to re-evaluate all overlays (human, monkey, experimenter).
  /// </summary>
  public void RefreshOverlays()
  {
    // just re-run your existing setup logic
    SetupDisplays();
  }

  /// <summary>
  /// Call this to immediately show / hide ONLY the Score UI.
  /// </summary>
  public void SetTrialManagerScoreUI(bool show)
  {
    var tm = FindObjectsOfType<MonoBehaviour>()
               .FirstOrDefault(mb => mb.GetType().Name.StartsWith("TrialManager"));
    if (tm == null) return;

    var t = tm.GetType();
    // 1) set the public bool field
    var fScore = t.GetField("ShowScoreUI", BindingFlags.Public|BindingFlags.Instance);
    if (fScore != null) fScore.SetValue(tm, show);

    // 2) also toggle the actual ScoreText component
    var scoreField = t.GetField("ScoreText", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    if (scoreField != null)
    {
      var txt = scoreField.GetValue(tm) as TMP_Text;
      if (txt != null)
        txt.gameObject.SetActive(show);
    }
  }

  /// <summary>
  /// Call this to immediately show / hide ONLY the Feedback UI.
  /// </summary>
  public void SetTrialManagerFeedbackUI(bool show)
  {
    var tm = FindObjectsOfType<MonoBehaviour>()
               .FirstOrDefault(mb => mb.GetType().Name.StartsWith("TrialManager"));
    if (tm == null) return;

    var t = tm.GetType();
    var fFeed = t.GetField("ShowFeedbackUI", BindingFlags.Public|BindingFlags.Instance);
    if (fFeed != null) fFeed.SetValue(tm, show);

    var feedField = t.GetField("FeedbackText", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    if (feedField != null)
    {
      var txt = feedField.GetValue(tm) as TMP_Text;
      if (txt != null)
        txt.gameObject.SetActive(show);
    }
  }

}
