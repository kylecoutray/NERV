using UnityEngine;
using TMPro;

public class CanvasMirror : MonoBehaviour
{
    private BlockPauseController _pauseCtrl;
    private TMP_Text             _previewLabel;
    private GameObject           _previewContinueBtn;
    private bool                 _lastPaused;

    void Awake()
    {
        // 1) Find the real pause controller (even if its canvas is inactive)
        _pauseCtrl = FindObjectOfType<BlockPauseController>(true);
        if (_pauseCtrl == null)
            Debug.LogError("CanvasMirror: BlockPauseController not found!");

        // 2) Cache your own UI children
        var labelTr = transform.Find("BlockLabelText");
        if (labelTr != null)
            _previewLabel = labelTr.GetComponent<TMP_Text>();

        var buttonTr = transform.Find("ContinueButton");
        if (buttonTr != null)
            _previewContinueBtn = buttonTr.gameObject;

        // 3) Hide initially; we’ll set real state in Start()
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Sync initial paused state
        bool paused = _pauseCtrl != null && _pauseCtrl.PauseCanvas.gameObject.activeSelf;
        _lastPaused = !paused;
        UpdatePauseMirror(paused);
    }

    void Update()
    {
        if (_pauseCtrl == null) return;

        // Check for pause-state changes
        bool paused = _pauseCtrl.PauseCanvas.gameObject.activeSelf;
        if (paused != _lastPaused)
            UpdatePauseMirror(paused);
    }

    private void UpdatePauseMirror(bool paused)
    {
        // 1) Show/hide the preview pause canvas
        gameObject.SetActive(paused);
        _lastPaused = paused;

        if (!paused) 
            return; // nothing more when un-paused

        // 2) Mirror the block label
        if (_previewLabel != null)
            _previewLabel.text = _pauseCtrl.BlockLabelText.text;

        // 3) Mirror the continue-button
        if (_previewContinueBtn != null)
            _previewContinueBtn.SetActive(
                _pauseCtrl.ContinueButton.gameObject.activeSelf
            );
    }
}
