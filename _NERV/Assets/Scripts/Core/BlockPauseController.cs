using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockPauseController : MonoBehaviour
{
    [Header("UI References")]
    public Canvas PauseCanvas;      // assign your Canvas
    public TMP_Text BlockLabelText;   // assign the “Block X of Y” text
    public Button ContinueButton;   // assign your “Continue” button

    public Button EndSessionButton;    // the “house” button
    bool _continuePressed;

    [Header("Audio Toggle")]
    public Button SoundToggleButton;       // drag your SoundButton here
    public Sprite _soundOnSprite;
    public Sprite _soundOffSprite;
    private bool _soundOn = true;
    void Awake()
    {
        // hide at runtime
        if (PauseCanvas != null)
            PauseCanvas.gameObject.SetActive(false);

        // hook up the button
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(() => _continuePressed = true);

        // ensure the initial sprite matches the “on” state
        var img = SoundToggleButton.GetComponent<Image>();
        if (img != null)
            img.sprite = _soundOnSprite;

        // hook up the click so it toggles volume & swaps the sprite
        SoundToggleButton.onClick.AddListener(ToggleSound);

        // EndSession always jumps back to TaskSelector (restore timeScale first)
        EndSessionButton.onClick.AddListener(() =>
        {
            // ensure normal time when returning
            Time.timeScale = 1f;
            SceneManager.LoadScene("TaskSelector");
        });

        // Toggle Outline when button is highlighted
        var outline = EndSessionButton.GetComponent<Outline>();
        var colors = EndSessionButton.colors;
        colors.highlightedColor = new Color(colors.highlightedColor.r, colors.highlightedColor.g, colors.highlightedColor.b, 1f); // Ensure highlighted color triggers change
        EndSessionButton.colors = colors;

        EventTrigger trigger = EndSessionButton.gameObject.AddComponent<EventTrigger>();

        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) =>
        {
            if (outline != null) outline.enabled = true;
        });

        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((eventData) =>
        {
            if (outline != null) outline.enabled = false;
        });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    /// <summary>
    /// Show a block‐pause screen reading "Block X of Y" and wait for the ContinueButton.
    /// </summary>
    public IEnumerator ShowPause(int blockIndex, int totalBlocks)
    {

        // reset flag
        _continuePressed = false;

        // End of game conditions:
        if (blockIndex == -1)
        {

            BlockLabelText.text = "GAME COMPLETE";
            PauseCanvas.gameObject.SetActive(true);
            DisplayManager.I.RefreshOverlays();

            // hide the house button (no home icon at GAME COMPLETE)
            EndSessionButton.gameObject.SetActive(false);

            // Wait for the player to press continue
            while (!_continuePressed)
                yield return null;

            Debug.Log("END OF GAME");
            // hide and continue
            PauseCanvas.gameObject.SetActive(false);
            // Load TaskSelector scene
            SceneManager.LoadScene("TaskSelector");

            yield break;
        }

        // safety checks
        if (PauseCanvas == null || BlockLabelText == null || ContinueButton == null)
            yield break;


        // show both buttons
        ContinueButton.gameObject.SetActive(true);
        EndSessionButton.gameObject.SetActive(true);


        // update & show block status
        BlockLabelText.text = $"Block {blockIndex} of {totalBlocks}";
        PauseCanvas.gameObject.SetActive(true);
        DisplayManager.I.RefreshOverlays();

        // wait for the button click
        while (!_continuePressed)
            yield return null;


        // hide and continue
        PauseCanvas.gameObject.SetActive(false);
        DisplayManager.I.RefreshOverlays();
    }

    public IEnumerator ShowPause(string labelText)
    {

        float oldTS = Time.timeScale; // freeze unity time
        Time.timeScale = 0f;

        _continuePressed = false;

        if (PauseCanvas == null || BlockLabelText == null || ContinueButton == null)
            yield break;

        BlockLabelText.text = labelText;

        PauseCanvas.gameObject.SetActive(true);
        DisplayManager.I.RefreshOverlays();
        EndSessionButton.gameObject.SetActive(true);

        Debug.Log($"[BlockPauseController] Scene in a {labelText} State");

        while (!_continuePressed)
            yield return new WaitForSecondsRealtime(0.1f);


        PauseCanvas.gameObject.SetActive(false);
        DisplayManager.I.RefreshOverlays();

        Time.timeScale = oldTS; // restore unity time
    }

    private void ToggleSound()
    {
        // flip the flag
        _soundOn = !_soundOn;

        // set master volume (1 or 0)
        AudioListener.volume = _soundOn ? 1f : 0f;

        // swap the button image
        var img = SoundToggleButton.GetComponent<Image>();
        if (img != null)
            img.sprite = _soundOn ? _soundOnSprite : _soundOffSprite;
    }
    
    public void PressContinue()
    {
        _continuePressed = true;
            
    }
}
