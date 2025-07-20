using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpHeight = 150f;
    public float jumpDuration = 0.4f;

    [Header("References")]
    [Tooltip("Drag your Player UI Image's RectTransform here")]
    public RectTransform playerRect;

    private RectTransform rt;
    private bool isJumping;
    private Vector2 startPosition;
    private float groundY;
    private Quaternion startRotation;

    void Awake()
    {
        // Ensure we have a RectTransform to work with
        rt = playerRect != null
             ? playerRect
             : GetComponent<RectTransform>();

        if (rt == null)
            Debug.LogError("[PlayerController] No RectTransform found! Assign playerRect in inspector.");

        // Cache starting values
        startPosition = rt != null ? rt.anchoredPosition : Vector2.zero;
        startRotation = rt != null ? rt.localRotation : Quaternion.identity;
        groundY = startPosition.y;
    }

    public void ResetPlayer()
    {
        // Stop any mid‑jump coroutine
        StopAllCoroutines();
        isJumping = false;

        if (rt != null)
        {
            rt.anchoredPosition = startPosition;
            rt.localRotation = startRotation;
        }
    }

    void Update()
    {
        if (!isJumping && Input.GetKey(KeyCode.Space))
            StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float y = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            rt.anchoredPosition = new Vector2(startPosition.x, groundY + y);
            rt.localRotation = Quaternion.Euler(0, 0, -360f * t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap back
        rt.anchoredPosition = startPosition;
        rt.localRotation = startRotation;
        isJumping = false;
    }
    
    public void TriggerJump()
    {
        if (!isJumping)  // avoid double jumps
            StartCoroutine(JumpRoutine());
    }

}
