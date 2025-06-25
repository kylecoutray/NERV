using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cycles the hue of an Image through the full rainbow.
/// Attach to a GameObject that has an Image component.
/// </summary>
[RequireComponent(typeof(Image))]
public class RainbowHueCycler : MonoBehaviour
{
    [Tooltip("Cycles per second (1 = one full rainbow every second).")]
    public float cyclesPerSecond = 0.2f;

    [Tooltip("Leave null to auto-use the Image on this GameObject.")]
    public Image targetImage;

    private float hue;   // Current hue value (0-1 range)

    void Awake()
    {
        // Fallback to the Image on this GameObject if none is assigned.
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Start from the Image’s original hue (keeps saturation/value intact).
        Color.RGBToHSV(targetImage.color, out hue, out _, out _);
    }

    void Update()
    {
        // Advance hue and wrap around.
        hue += cyclesPerSecond * Time.deltaTime;
        if (hue > 1f) hue -= 1f;

        // Full saturation/value for vivid colors—tweak if needed.
        targetImage.color = Color.HSVToRGB(hue, 1f, 1f);
    }
}
