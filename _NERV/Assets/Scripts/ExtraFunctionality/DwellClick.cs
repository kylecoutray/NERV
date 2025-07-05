using UnityEngine;

/// <summary>
/// Attach this component to your TrialManager GameObject to enable "dwell to click" on stimuli.
/// No extra setup required—uses Camera.main and detects any collider with a StimulusID.
/// </summary>
public class DwellClick : MonoBehaviour
{
    [Tooltip("Seconds the gaze must hover over a stimulus to register a click.")]
    public float dwellTime = 0.5f;

    /// <summary>
    /// True only on the frame a dwell-induced click occurs.
    /// Combine with Input.GetMouseButtonDown(0) in your TrialManager.
    /// </summary>
    public static bool ClickDownThisFrame { get; private set; }

    private Camera _camera;
    private GameObject _lastHit;
    private float _hoverTimer;

    void Awake()
    {
        // Use the main camera by default
        _camera = Camera.main;
        if (_camera == null)
            Debug.LogWarning("DwellClick: No Camera.main found; raycasts will fail.");
    }

    void Update()
    {
        // Reset the click flag at the start of each frame
        ClickDownThisFrame = false;

        // Raycast from current cursor/gaze (mouse) position
        Vector3 screenPos = Input.mousePosition;
        Ray ray = _camera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Only consider objects with StimulusID
            var stim = hit.collider.GetComponentInParent<StimulusID>();
            if (stim != null)
            {
                GameObject go = hit.collider.gameObject;

                if (go == _lastHit)
                {
                    // Accumulate dwell time
                    _hoverTimer += Time.deltaTime;
                    if (_hoverTimer >= dwellTime)
                    {
                        // Register a click this frame
                        ClickDownThisFrame = true;
                        _hoverTimer = 0f;
                    }
                }
                else
                {
                    // New stimulus: start timing
                    _lastHit = go;
                    _hoverTimer = 0f;
                }

                return; // skip reset
            }
        }

        // Nothing or non-stimulus under gaze: reset state
        _lastHit = null;
        _hoverTimer = 0f;
    }
}
