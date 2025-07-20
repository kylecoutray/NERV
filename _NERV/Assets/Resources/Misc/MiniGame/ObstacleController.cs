using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ObstacleController : MonoBehaviour
{
    [Header("Movement & Hitbox")]
    private float speed;
    private RectTransform rt;
    private RectTransform hitboxRt;
    private RectTransform playerRt;
    private bool scored;

    [Header("Fade Settings")]
    [Tooltip("Fade-out starts this many px before ground’s left edge")]
    public float fadeOutOffset = 30f;
    [Tooltip("Seconds to fade in at spawn and fade out at exit")]
    public float fadeDuration = 0.5f;

    private Image img;
    private bool isFadingOut;
    private float fadeTriggerX;

    /// <summary>
    /// Must be called immediately after Instantiate(spikePrefab).
    /// </summary>
    public void Init(float scrollSpeed, RectTransform groundPanel)
    {
        // 1) Cache movement & visuals
        speed = scrollSpeed;
        rt    = GetComponent<RectTransform>();
        img   = GetComponent<Image>();
        scored = false;

        // 2) Optional custom hitbox child
        var hb = transform.Find("Hitbox");
        if (hb != null) hitboxRt = hb.GetComponent<RectTransform>();

        // 3) Start invisible → fade in
        if (img != null)
        {
            var c = img.color;
            c.a = 0f;
            img.color = c;
            StartCoroutine(FadeIn());
        }

        // 4) Cache player reference
        var p = GameObject.Find("Player");
        if (p != null) playerRt = p.GetComponent<RectTransform>();

        // 5) Compute fadeTriggerX in canvas local coords
        Vector3[] corners = new Vector3[4];
        groundPanel.GetWorldCorners(corners);
        Vector3 worldLeftCenter = (corners[0] + corners[1]) * 0.5f;

        var canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, worldLeftCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPt, null, out Vector2 localLeft
        );

        float halfW = rt.rect.width * 0.5f;
        fadeTriggerX = localLeft.x - halfW + fadeOutOffset;
    }

    void Update()
    {
        if (rt == null || playerRt == null) return;

        // 1) Slide left
        rt.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        // 2) Score when hitbox passes the player’s left edge
        if (!scored)
        {
            var boxRT = hitboxRt != null ? hitboxRt : rt;
            Bounds spikeBounds  = GetWorldBounds(boxRT);
            Bounds playerBounds = GetWorldBounds(playerRt);

            if (spikeBounds.max.x < playerBounds.min.x)
            {
                scored = true;
                MiniGameManager.I.AddPoint();
            }
        }

        // 3) Trigger fade‑out when we cross fadeTriggerX
        if (!isFadingOut && rt.anchoredPosition.x < fadeTriggerX)
        {
            isFadingOut = true;
            StartCoroutine(FadeOutAndDestroy());
        }

        // 4) Collision check
        var hitRT = hitboxRt != null ? hitboxRt : rt;
        if (GetWorldBounds(hitRT).Intersects(GetWorldBounds(playerRt)))
            MiniGameManager.I.GameOver();
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(t / fadeDuration);
            yield return null;
        }
        SetAlpha(1f);
    }

    IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - (t / fadeDuration));
            yield return null;
        }
        Destroy(gameObject);
    }

    void SetAlpha(float a)
    {
        if (img != null)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }
    }

    /// <summary>
    /// Freeze on death.
    /// </summary>
    public void StopMovement() => speed = 0f;

    private Bounds GetWorldBounds(RectTransform rtf)
    {
        Vector3[] c = new Vector3[4];
        rtf.GetWorldCorners(c);
        Vector3 center = (c[0] + c[2]) * 0.5f;
        Vector3 size   = c[2] - c[0];
        return new Bounds(center, size);
    }
}
