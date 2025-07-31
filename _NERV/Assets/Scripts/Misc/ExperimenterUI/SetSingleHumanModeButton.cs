// Assets/Scripts/Misc/ExperimenterUI/SetSingleHumanModeButton.cs
using UnityEngine;

public class SetSingleHumanModeButton : MonoBehaviour
{
    /// <summary>
    /// Attach this to your temporary button's OnClick event.
    /// </summary>
    public void OnClick_SetSingleHuman()
    {
        if (DisplayManager.I == null)
        {
            Debug.LogError("DisplayManager.I is null! Make sure a DisplayManager exists in the scene.");
            return;
        }

        // Switch mode and persist it
        DisplayManager.I.ApplyDisplayMode(DisplayManager.Mode.SingleHuman);
        DisplayManager.I.SaveConfig();

        Debug.Log("DisplayManager mode reset to SingleHuman");
    }
}
