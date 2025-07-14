using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ContinueBroadcaster : MonoBehaviour
{
    [Header("Preview UI References")]
    [Tooltip("Assign the Canvas you want to show/hide")]
    public Canvas OtherPauseCanvas;
    [Tooltip("Assign the BlockLabelText TMP_Text in the preview canvas")]
    public TMP_Text OtherBlockLabelText;
    [Tooltip("Assign the Continue‐style button on this prefab")]
    public Button OtherContinueButton;

    private BlockPauseController _pauseCtrl;
    private TMP_Text             _sourceLabel;

    void Awake()
    {
        // find the real pause controller (even if its canvas is inactive)
        _pauseCtrl = FindPauseController();
        if (_pauseCtrl == null)
            Debug.LogError("ContinueBroadcaster: no BlockPauseController found in scene!");
        else
            _sourceLabel = _pauseCtrl.BlockLabelText;
    }

    void Start()
    {
        if (_pauseCtrl == null) return;

        // 1) Mirror initial canvas visibility
        if (OtherPauseCanvas != null)
            OtherPauseCanvas.gameObject.SetActive(_pauseCtrl.PauseCanvas.gameObject.activeSelf);
        else
            Debug.LogError("ContinueBroadcaster: OtherPauseCanvas not assigned!");

        // 2) Mirror initial label text
        if (OtherBlockLabelText != null)
            OtherBlockLabelText.text = _sourceLabel.text;
        else
            Debug.LogError("ContinueBroadcaster: OtherBlockLabelText not assigned!");

        // 3) Wire & mirror continue button
        if (OtherContinueButton != null)
        {
            OtherContinueButton.onClick.AddListener(_pauseCtrl.PressContinue);
            OtherContinueButton.gameObject.SetActive(_pauseCtrl.PauseCanvas.gameObject.activeSelf);
        }
        else
            Debug.LogError("ContinueBroadcaster: OtherContinueButton not assigned!");
    }

    void Update()
    {
        if (_pauseCtrl == null) return;

        bool isPaused = _pauseCtrl.PauseCanvas.gameObject.activeSelf;

        // mirror canvas visibility
        if (OtherPauseCanvas != null
            && OtherPauseCanvas.gameObject.activeSelf != isPaused)
        {
            OtherPauseCanvas.gameObject.SetActive(isPaused);
        }

        // mirror continue-button visibility
        if (OtherContinueButton != null
            && OtherContinueButton.gameObject.activeSelf != isPaused)
        {
            OtherContinueButton.gameObject.SetActive(isPaused);
        }

        // mirror label text
        if (OtherBlockLabelText != null
            && OtherBlockLabelText.text != _sourceLabel.text)
        {
            OtherBlockLabelText.text = _sourceLabel.text;
        }
    }

    /// <summary>
    /// Searches all root GameObjects (including inactive) for a BlockPauseController.
    /// </summary>
    private BlockPauseController FindPauseController()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var ctrl = root.GetComponentInChildren<BlockPauseController>(true);
            if (ctrl != null)
                return ctrl;
        }
        return null;
    }
}
