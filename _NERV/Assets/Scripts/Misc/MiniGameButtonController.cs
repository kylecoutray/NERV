using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MiniGameButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Mini Game References")]
    public GameObject miniGameRoot;           // Root object for the mini-game (inactive by default)
    public PlayerController playerController; // Drag your PlayerController here directly
    public MiniGameManager gameManager;      // Reference to the MiniGameManager

    [Header("UI References")]
    public TextMeshProUGUI buttonText;        // TMP text for this button
    public string defaultText = "Pass the time here.";
    public string activeText = "JUMP!";
    public GameObject exitButtonPrefab;       // Prefab for the small "X" exit button

    [Header("Jump Settings")]
    public float holdJumpInterval = 0.1f;     // Time between jump triggers while held

    private bool isActive = false;
    private bool isHolding = false;
    private float jumpTimer = 0f;
    private GameObject exitButtonInstance;

    void Awake()
    {
        if (miniGameRoot != null)
            miniGameRoot.SetActive(false);

        if (buttonText != null)
            buttonText.text = defaultText;

        if (SceneManager.GetActiveScene().name != "TaskSelector")
            gameObject.SetActive(false);
    }

    void Update()
    {
        if (isActive && isHolding && playerController != null)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0f)
            {
                playerController.TriggerJump();
                jumpTimer = holdJumpInterval;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive)
        {
            ActivateMiniGame();  // Start the game AND spawn X button
        }

        // Now treat as holding for jump if the game is active
        if (isActive)
        {
            isHolding = true;
            jumpTimer = 0f;  // Trigger an immediate jump
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }

    private void ActivateMiniGame()
    {
        isActive = true;

        if (miniGameRoot != null)
            miniGameRoot.SetActive(true);

        if (buttonText != null)
            buttonText.text = activeText;

        // Ensure the X button spawns exactly once
        if (exitButtonPrefab != null && exitButtonInstance == null)
        {
            exitButtonInstance = Instantiate(exitButtonPrefab, transform.parent);
            var exitController = exitButtonInstance.AddComponent<MiniGameExitButton>();
            exitController.Init(this);
        }
    }

    public void DeactivateMiniGame()
    {
        gameManager.ResetGame();
        isActive = false;
        isHolding = false;

        if (miniGameRoot != null)
            miniGameRoot.SetActive(false);

        if (buttonText != null)
            buttonText.text = defaultText;

        if (exitButtonInstance != null)
        {
            Destroy(exitButtonInstance);
            exitButtonInstance = null;
        }
    }
}

public class MiniGameExitButton : MonoBehaviour, IPointerClickHandler
{
    private MiniGameButtonController mainButton;

    public void Init(MiniGameButtonController button)
    {
        mainButton = button;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mainButton != null)
            mainButton.DeactivateMiniGame();
    }
}
