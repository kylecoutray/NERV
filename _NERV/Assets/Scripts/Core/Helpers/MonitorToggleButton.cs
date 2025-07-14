using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonitorToggleButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Monitor Sprites & Image")]
    public Image  monitorImage;
    public Sprite spriteOff;
    public Sprite spriteOn;

    [Header("Optional Click Region")]
    public RectTransform clickableRegion;

    [Header("Sibling RawImage (auto-found)")]
    public RawImage siblingRawImage;

    bool _isOn;

    void Awake()
    {
        // 1) Ensure monitorImage reference
        if (monitorImage == null)
            monitorImage = GetComponent<Image>();

        // 2) Auto-find the RawImage sibling if none assigned
        if (siblingRawImage == null && transform.parent != null)
        {
            foreach (Transform sib in transform.parent)
            {
                if (sib == transform) continue;
                var raw = sib.GetComponent<RawImage>();
                if (raw != null)
                {
                    siblingRawImage = raw;
                    break;
                }
            }
        }

        // 3) Force initial state ON
        _isOn = true;
        monitorImage.sprite = spriteOn;
        if (siblingRawImage != null)
            siblingRawImage.enabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 4) Only toggle if within the clickable region (if set)
        if (clickableRegion != null &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                clickableRegion,
                eventData.position,
                eventData.pressEventCamera))
        {
            return;
        }

        // 5) Flip state, swap sprite
        _isOn = !_isOn;
        monitorImage.sprite = _isOn ? spriteOn : spriteOff;

        // 6) Mirror RawImage to the same ON/OFF state
        if (siblingRawImage != null)
            siblingRawImage.enabled = _isOn;
    }
}
