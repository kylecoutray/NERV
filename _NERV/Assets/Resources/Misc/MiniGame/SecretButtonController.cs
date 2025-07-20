using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SecretButtonController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Drag your mini‑game root GameObject here (set inactive by default)")]
    public GameObject miniGameRoot;
    [Tooltip("Drag your MiniGameManager here")]
    public MiniGameManager gameManager;
    [Tooltip("Degrees to rotate on hover")]
    public float hoverAngle = 45f;

    private RectTransform rt;
    private Image img;
    private Quaternion originalRot;
    private Color originalColor;
    private bool isActive;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        originalRot   = rt.localRotation;
        originalColor = img != null ? img.color : Color.white;
        if (miniGameRoot != null)
            miniGameRoot.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        rt.localRotation = originalRot * Quaternion.Euler(0, 0, hoverAngle);
    }

    public void OnPointerExit(PointerEventData e)
    {
        rt.localRotation = originalRot;
    }

    public void OnPointerClick(PointerEventData e)
    {
        // 1) Always clear/reset the game state
        if (gameManager != null)
            gameManager.ResetGame();

        // 2) Toggle visibility
        isActive = !isActive;
        if (miniGameRoot != null)
            miniGameRoot.SetActive(isActive);

        // 3) Update button color
        if (img != null)
            img.color = isActive ? Color.red : originalColor;
    }
}
