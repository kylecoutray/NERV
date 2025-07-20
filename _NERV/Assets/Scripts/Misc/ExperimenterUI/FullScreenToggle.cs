using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class FullScreenToggle : MonoBehaviour
{
    [Header("Icon (Optional)")]
    [Tooltip("The Image component showing your button icon.")]
    [SerializeField] private Image iconImage;
    [Tooltip("Sprite when the full‑screen panel is hidden.")]
    [SerializeField] private Sprite defaultIcon;
    [Tooltip("Sprite when the full‑screen panel is shown.")]
    [SerializeField] private Sprite toggledIcon;

    [Header("Full‑Screen Panel")]
    [Tooltip("Drag the full‑screen GameObject or prefab here.")]
    [SerializeField] private GameObject targetPanel;

    [Header("TV Effect Controller (Optional)")]
    [Tooltip("Drag the TVEffectController here, or leave blank to auto‑find.")]
    [SerializeField] private TVEffectController tvEffectController;

    private Button _button;
    private bool   _isShown;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        // initialize panel hidden
        _isShown = false;
        if (targetPanel != null)
            targetPanel.SetActive(false);

        // set initial icon
        if (iconImage != null && defaultIcon != null)
            iconImage.sprite = defaultIcon;

        // auto‑find the effect controller if not dragged in
        if (tvEffectController == null)
        {
            // look on the parent of this Button’s parent
            var parent = transform.parent;
            if (parent != null && parent.parent != null)
                tvEffectController = parent.parent.GetComponentInChildren<TVEffectController>();
            if (tvEffectController == null)
                Debug.LogWarning("FullScreenToggle: TVEffectController not assigned or found as sibling.");
        }
    }

    void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        // flip visibility state
        _isShown = !_isShown;

        // toggle panel
        if (targetPanel != null)
            targetPanel.SetActive(_isShown);

        // swap icon
        if (iconImage != null)
            iconImage.sprite = _isShown ? toggledIcon : defaultIcon;

        // toggle TV effect: disable when panel shown, enable when hidden
        if (tvEffectController != null)
            tvEffectController.enabled = !_isShown;
    }
}
