using UnityEngine;
using UnityEngine.UI;            // For UI.Image & UI.Button
using TMPro;                     // If using TextMeshPro

public class InstructionToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject instructionPanel;      // Panel parent (inactive by default)
    public Image   instructionImage;        // UI Image inside that panel
    public TextMeshProUGUI buttonText;      // Or use UnityEngine.UI.Text

    [Header("Scene-Specific Sprite")]
    public Sprite instructionSprite;        // Assign per scene in inspector

    private bool isOpen = false;

    // Hook this method to your Button's OnClick()
    public void ToggleInstructions()
    {
        isOpen = !isOpen;
        instructionPanel.SetActive(isOpen);

        if (isOpen)
        {
            instructionImage.sprite = instructionSprite;
            buttonText.text         = "CLOSE";
        }
        else
        {
            buttonText.text         = "HOW TO PLAY";
        }
    }
}
