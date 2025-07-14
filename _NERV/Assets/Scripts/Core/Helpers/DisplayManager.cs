using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using TMPro;

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


  private string ConfigPath => Path.Combine(Application.persistentDataPath, "displayConfig.json");

  void Awake()
  {
    // singleton
    if (I != null) { Destroy(gameObject); return; }
    I = this;
    DontDestroyOnLoad(gameObject);

    // re-apply whenever any scene loads
    SceneManager.sceneLoaded += OnSceneLoaded;

    LoadConfig();
    ApplyDisplayMode(CurrentMode);
  }

  void OnDestroy()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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
    if (_humanOverlay != null) { Destroy(_humanOverlay); _humanOverlay = null; }
    if (_monkeyOverlay != null) { Destroy(_monkeyOverlay); _monkeyOverlay = null; }

    var sceneName = SceneManager.GetActiveScene().name;

    // 1) Human-waiting overlay on Display 1, **only** in TaskSelector
    if (CurrentMode == Mode.DualHuman && humanOverlayPrefab != null && sceneName == "TaskSelector")
    {
      _humanOverlay = Instantiate(humanOverlayPrefab);
      var c = _humanOverlay.GetComponent<Canvas>();
      if (c != null) c.targetDisplay = 0;
    }

    // 2) Monkey overlay: ON in TaskSelector OR paused; OFF otherwise
    if (CurrentMode == Mode.DualMonkey
        && monkeyOverlayPrefab != null
        && (sceneName == "TaskSelector" || IsPaused()))
    {
      _monkeyOverlay = Instantiate(monkeyOverlayPrefab);
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
          var existing = GameObject.Find("ExperimenterCanvas") ?? GameObject.FindWithTag("ExperimenterUI");
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
    var pauseGO = GameObject.Find("PauseCanvas");
    return pauseGO != null && pauseGO.activeInHierarchy;
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
}
