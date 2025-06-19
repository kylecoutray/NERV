using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockPauseController : MonoBehaviour
{
    [Header("UI References")]
    public Canvas PauseCanvas;      // assign your Canvas
    public TMP_Text BlockLabelText;   // assign the “Block X of Y” text
    public Button ContinueButton;   // assign your “Continue” button

    bool _continuePressed;

    void Awake()
    {
        // hide at runtime
        if (PauseCanvas != null)
            PauseCanvas.gameObject.SetActive(false);

        // hook up the button
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(() => _continuePressed = true);
    }

    /// <summary>
    /// Show a block‐pause screen reading "Block X of Y" and wait for the ContinueButton.
    /// </summary>
    public IEnumerator ShowPause(int blockIndex, int totalBlocks)
    {
        // reset flag
        _continuePressed = false;

        //End of game conditions:
        if (blockIndex > totalBlocks)
        {
            BlockLabelText.text = "GAME COMPLETE";
            PauseCanvas.gameObject.SetActive(true);
            while (!_continuePressed)
                yield return null;

            Debug.Log("END OF GAME");
            //End Play Mode (FUTURE: GO TO Menu Scene)
            #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // safety checks
        if (PauseCanvas == null || BlockLabelText == null || ContinueButton == null)
            yield break;


        // update & show block status
        BlockLabelText.text = $"Block {blockIndex} of {totalBlocks}";
        PauseCanvas.gameObject.SetActive(true);

        // wait for the button click
        while (!_continuePressed)
            yield return null;

        // hide and continue
        PauseCanvas.gameObject.SetActive(false);
    }
}
