using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to your TrialManager GameObject to enable "dwell to click" on stimuli.
/// Use mouse or a GazeCursor RectTransform for gaze-based simulation.
/// </summary>
public class DwellClick : MonoBehaviour
{
    [Header("Dwell Settings")]
    [Tooltip("Seconds the cursor must hover over a stimulus to register a click.")]
    public float dwellTime = 0.5f;

    [Tooltip("If true, uses mouse position; otherwise uses the gaze cursor position.")]
    public bool simulateWithMouse = true;

    [Tooltip("Show or hide the gaze cursor graphic in the scene.")]
    public bool showGazeCursor = true;

    [Tooltip("Assign the GazeCursor RectTransform instance for gaze-based input.")]
    public RectTransform gazeCursor;

    /// <summary>
    /// True on the frame a dwell click occurs. Combine with GetMouseButtonDown in TrialManager.
    /// </summary>
    public static bool ClickDownThisFrame { get; private set; }

    private Camera _camera;
    private StimulusID _lastStim;
    private float _hoverTimer;

    void Awake()
    {
        _camera = Camera.main;
        if (_camera == null)
            Debug.LogWarning("DwellClick: No Camera.main found; raycasts will fail.");

        // Auto-find or instantiate GazeCursor
        if (gazeCursor == null)
        {
            var existingGO = GameObject.Find("GazeCursor");
            if (existingGO != null)
            {
                gazeCursor = existingGO.GetComponent<RectTransform>();
            }
            else
            {
                // Load the GazeCursor prefab (as GameObject) from Resources
                var prefabGO = Resources.Load<GameObject>("Prefabs/GazeCursor");
                if (prefabGO != null)
                {
                    // Get its RectTransform
                    var prefab = prefabGO.GetComponent<RectTransform>();
                    if (prefab != null)
                    {
                        var canvas = FindObjectOfType<Canvas>();
                        if (canvas != null)
                        {
                            // Instantiate under Canvas so UI elements render correctly
                            RectTransform inst = Instantiate(prefab, canvas.transform, false);
                            inst.name = "GazeCursor";
                            // reset UI transform so it's visible at center
                            inst.anchorMin = new Vector2(0.5f, 0.5f);
                            inst.anchorMax = new Vector2(0.5f, 0.5f);
                            inst.anchoredPosition = Vector2.zero;
                            inst.localScale = Vector3.one;
                            // bring to front
                            inst.SetAsLastSibling();
                            gazeCursor = inst;
                        }
                        else
                        {
                            var inst = Instantiate(prefab);
                            inst.name = "GazeCursor";
                            gazeCursor = inst;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("DwellClick: Could not find or load GazeCursor prefab at Resources/Prefabs/GazeCursor");
                    }
                }
            }
        }
    }
    void Update()
    {
        // Toggle cursor visibility
        if (gazeCursor != null)
            gazeCursor.gameObject.SetActive(showGazeCursor);

        // Reset click flag at start of frame
        ClickDownThisFrame = false;

        // Determine screen position:
        // - If simulateWithMouse is true, use the current mouse pointer position.
        // - Otherwise use the GazeCursor RectTransform (which follows your eye-tracker or custom coordinates).
        // To hook in a real eye-tracker, replace gazeCursor.position with your tracker’s output:
        // Example with Tobii SDK:
        // var gazePoint = Tobii.Gaming.TobiiAPI.GetGazePoint();
        // Vector2 gazePos = new Vector2(gazePoint.Screen.x, gazePoint.Screen.y);
        // screenPos = gazePos;

        // For EyeLink or other APIs, similarly assign screenPos = new Vector2(YourEyeTracker.X, YourEyeTracker.Y);
        // Vector2 gazePos = new Vector2(YourTracker.GetX(), YourTracker.GetY());(YourTracker.GetX(), YourTracker.GetY());
        //    screenPos = gazePos;
        Vector2 screenPos = simulateWithMouse || gazeCursor == null
            ? (Vector2)Input.mousePosition
            : (Vector2)gazeCursor.position; // Change this line to YOUR Eye Tracker output position.

        // Update visualizer position
        if (gazeCursor != null)
        {
            // In Screen Space - Overlay canvas, RectTransform.position = screenPos
            gazeCursor.position = screenPos;
        }
        // Raycast into scene
        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var stim = hit.collider.GetComponentInParent<StimulusID>();
            if (stim != null)
            {
                // Accumulate dwell on same stimulus
                if (stim == _lastStim)
                {
                    _hoverTimer += Time.deltaTime;
                    if (_hoverTimer >= dwellTime)
                    {
                        ClickDownThisFrame = true;
                        _hoverTimer = 0f;
                    }
                }
                else
                {
                    // New target: reset timer
                    _lastStim = stim;
                    _hoverTimer = 0f;
                }
                return;
            }
        }

        // No stimulus under gaze: reset
        _lastStim = null;
        _hoverTimer = 0f;
    }
}