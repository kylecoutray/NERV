using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CalibrationUIController : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_InputField pathInput;
    public Button browseButton;
    public Button saveAllButton;

    void Awake()
    {
        browseButton.onClick.AddListener(OnBrowse);
        saveAllButton.onClick.AddListener(OnSaveAll);

        // preload last‐used path
        string prev = SessionManager.Instance.CalibrationPath;
        if (!string.IsNullOrEmpty(prev))
            pathInput.text = prev;
    }

    void OnBrowse()
    {
        #if UNITY_EDITOR
            // Editor: native file dialog
            string picked = EditorUtility.OpenFilePanel(
                "Select Calibration JSON",
                Application.dataPath,
                "json"
            );
            if (!string.IsNullOrEmpty(picked))
                pathInput.text = picked;
        #else
            // Runtime: use SimpleFileBrowser
            StartCoroutine(ShowLoadDialogCoroutine());
        #endif
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        // 1) filter to JSON
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
        FileBrowser.SetDefaultFilter(".json");

        // 2) find which display this UI is on
        var parentCanvas = browseButton.GetComponentInParent<Canvas>();
        int displayIndex = (parentCanvas != null)
            ? parentCanvas.targetDisplay
            : 0;  // fallback to primary

        // 3) start the browser and concurrently schedule our display hack
        var waitDialog = FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            /* initialPath */ null,
            "Select Calibration JSON",
            "Load"
        );
        StartCoroutine(_AssignBrowserToDisplay(displayIndex));

        // 4) wait for user
        yield return waitDialog;

        // 5) grab result
        if (FileBrowser.Success && FileBrowser.Result.Length > 0)
            pathInput.text = FileBrowser.Result[0];
    }

    IEnumerator _AssignBrowserToDisplay(int disp)
    {
        // wait a couple frames so SimpleFileBrowser can spawn its Canvas
        yield return null;
        yield return null;

        // find the FileBrowserCanvas and reassign
        foreach (var canv in FindObjectsOfType<Canvas>())
        {
            if (canv.gameObject.name.Contains("FileBrowser"))
            {
                canv.targetDisplay = disp;
                Debug.Log($"[CalibrationUI] Moved FileBrowser popup to display #{disp}");
                break;
            }
        }
    }


    void OnSaveAll()
    {
        var path = pathInput.text.Trim();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[CalibrationUI] No calibration path set!");
            return;
        }

        // persist for next scenes
        SessionManager.Instance.CalibrationPath = path;
        Debug.Log($"[CalibrationUI] Calibration path saved: {path}");

        // if a loader exists here, apply it immediately
        if (CalibrationLoader.Instance != null && CalibrationLoader.Instance.enabled)
        {
            bool ok = CalibrationLoader.Instance.LoadFromPath(path);
            if (!ok)
                Debug.LogError($"[CalibrationUI] Failed to load calibration from {path}");
        }
    }
}
