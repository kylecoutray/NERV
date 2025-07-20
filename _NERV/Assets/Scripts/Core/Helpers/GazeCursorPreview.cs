using UnityEngine;
using UnityEngine.UI;

public class GazeCursorPreview : MonoBehaviour
{
    [Header("Drag your preview RawImage parent here (must have a RectTransform)")]
    public RectTransform previewParent;

    [Tooltip("If your preview canvas is Screen-Space Camera, assign that Camera; otherwise leave null.")]
    public Camera previewUICamera;

    private GazeCursorController gazeController;
    private RectTransform gazeCursor;
    private RectTransform previewCursor;
    private Vector2 previewSize;
    private float scaleX, scaleY;
    public float previewScale = 2.5f; // Default scale factor for the preview cursor

    void Awake()
    {
        // 1) Is gaze enabled?
        gazeController = FindObjectOfType<GazeCursorController>();
        if (gazeController == null)
        {
            Debug.Log("GazeCursorPreview: No GazeCursorController found; preview stays off.");
            return;
        }

        // 2) Locate the live GazeCursor under Dependencies
        var deps = GameObject.Find("Dependencies");
        if (deps == null) { Debug.LogError("No 'Dependencies' root found."); return; }
        var go = FindDeepChild(deps, "GazeCursor");
        if (go == null) { Debug.LogError("'GazeCursor' not found under Dependencies."); return; }
        gazeCursor = go.GetComponent<RectTransform>();

        // 3) Measure preview dims and compute scale factors
        previewSize = previewParent.rect.size;              // e.g. (320,180)
        scaleX = previewSize.x / Screen.width;              // e.g. 320/1920
        scaleY = previewSize.y / Screen.height;             // e.g. 180/1080

        // 4) Clone under preview parent
        previewCursor = Instantiate(gazeCursor, previewParent, false);
        previewCursor.name = "PreviewGazeCursor";

        // 5) Set the specified scale
        previewCursor.localScale = new Vector3(previewScale, previewScale, 1f);

        // 6) Resize the clone so it’s visible at preview scale
        previewCursor.sizeDelta = new Vector2(
            gazeCursor.sizeDelta.x * scaleX,
            gazeCursor.sizeDelta.y * scaleY
        );

        // 7) Make it pure white
        var img = previewCursor.GetComponent<Image>();
        if (img != null)
            img.color = Color.white;

        Debug.Log("GazeCursorPreview: Preview cursor instantiated with white color and 2.5x scale.");
    }

    void Update()
    {
        if (previewCursor == null) return;

        // Show/hide based on controller
        previewCursor.gameObject.SetActive(gazeController != null);

        // Map full-screen pos → normalized [0..1]
        Vector2 screenPos = gazeCursor.position;
        var norm = new Vector2(screenPos.x / Screen.width,
                               screenPos.y / Screen.height);

        // Convert to preview local point:
        Vector2 pivotOffset = previewSize * previewParent.pivot;
        Vector2 localPos = new Vector2(
            norm.x * previewSize.x - pivotOffset.x,
            norm.y * previewSize.y - pivotOffset.y
        );

        previewCursor.anchoredPosition = localPos;
    }

    // Recursive search helper
    private static GameObject FindDeepChild(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name) return child.gameObject;
            var res = FindDeepChild(child.gameObject, name);
            if (res != null) return res;
        }
        return null;
    }
}
