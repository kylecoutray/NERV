using UnityEngine;
using UnityEngine.UI;            // For UI.Image & UI.Button
using TMPro;                     // If using TextMeshPro

public class InstructionToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject instructionPanel;      // Panel parent (inactive by default)
    public Image   instructionImage;         // UI Image inside that panel
    public TextMeshProUGUI buttonText;       // Or use UnityEngine.UI.Text

    [Header("Scene-Specific Sprite")]
    public Sprite instructionSprite;         // Assign per scene in inspector

    private Sprite fallbackSprite;
    private bool isOpen = false;

    void Awake()
    {
        // Load the fallback from Resources/Misc/Instructions/TEMPLATE
        fallbackSprite = Resources.Load<Sprite>("Misc/Instructions/TEMPLATE");
        if (fallbackSprite == null)
        {
            Debug.LogWarning("Fallback TEMPLATE sprite not found at Resources/Misc/Instructions/TEMPLATE");
        }
    }

    // Hook this method to your Button's OnClick()
    public void ToggleInstructions()
    {
        isOpen = !isOpen;
        instructionPanel.SetActive(isOpen);

        if (isOpen)
        {
            // Use scene sprite if set; otherwise fallback
            instructionImage.sprite = instructionSprite != null 
                                      ? instructionSprite 
                                      : fallbackSprite;

            buttonText.text = "CLOSE";
        }
        else
        {
            buttonText.text = "HOW TO PLAY";
        }
    }
}
