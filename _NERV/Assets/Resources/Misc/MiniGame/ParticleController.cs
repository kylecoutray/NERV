// ParticleController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ParticleController : MonoBehaviour
{
    private float speed;
    private float fadeDuration;
    private RectTransform rt;
    private Image img;

    public void Setup(float scrollSpeed, float fadeDur)
    {
        speed        = scrollSpeed;
        fadeDuration = fadeDur;
        rt           = GetComponent<RectTransform>();
        img          = GetComponent<Image>();
        StartCoroutine(FadeSequence());
    }

    void Update()
    {
        if (rt != null)
            rt.anchoredPosition += Vector2.left * speed * Time.deltaTime;
    }

    IEnumerator FadeSequence()
    {
        // fade in
        float t = 0f;
        while (t < (fadeDuration * 0.25f))
        {
            t += Time.deltaTime;
            SetAlpha(t / (fadeDuration * 0.25f));
            yield return null;
        }
        SetAlpha(1f);

        // fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - (t / fadeDuration));
            yield return null;
        }

        // disable and return to pool
        gameObject.SetActive(false);
    }
    public void StopMovement()
    {
        speed = 0f;
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
}
