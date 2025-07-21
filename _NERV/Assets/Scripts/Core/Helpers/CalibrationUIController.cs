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
        // only show .json files
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
        FileBrowser.SetDefaultFilter(".json");

        // show load dialog: pick files only, single-select
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            /* initialPath */ null,
            "Select Calibration JSON",
            "Load"
        );

        if (FileBrowser.Success && FileBrowser.Result.Length > 0)
            pathInput.text = FileBrowser.Result[0];
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
