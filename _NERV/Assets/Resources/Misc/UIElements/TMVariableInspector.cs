using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Reflection;

/// <summary>
/// Dynamically lists and edits public fields of any TrialManager{ACR} in the scene,
/// grouping by [Header] and allowing optional filtering by header name.
/// Attach this to a ScrollView Content parent (with VerticalLayoutGroup).
/// Prefabs in the scene act as templates—these will be hidden at runtime.
/// </summary>
public class TMVariableInspector : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent transform under which rows will be instantiated.")]
    [SerializeField] private Transform contentParent;

    [Tooltip("Template GameObject for section headers (child 'Label').")]
    [SerializeField] private GameObject headerRowPrefab;

    [Tooltip("Template GameObject for int/float/string rows (child 'Label' & 'InputField').")]
    [SerializeField] private GameObject textFieldRowPrefab;

    [Tooltip("Template GameObject for bool rows (child 'Label' & 'Toggle').")]
    [SerializeField] private GameObject boolFieldRowPrefab;

    [Header("Filter Settings")]
    [Tooltip("If non‑empty, only show these header sections.")]

    [Header("Bool Toggle Sprites")]
    [SerializeField] private Sprite boolOnSprite;
    [SerializeField] private Sprite boolOffSprite;

    [SerializeField] private string[] includedHeaders;

    private object tmInstance;

    void Start()
    {
        // Hide in‑scene templates so they don't appear directly
        if (headerRowPrefab    != null) headerRowPrefab.SetActive(false);
        if (textFieldRowPrefab != null) textFieldRowPrefab.SetActive(false);
        if (boolFieldRowPrefab != null) boolFieldRowPrefab.SetActive(false);

        // 1) Locate the TrialManager{ACR} instance
        var go = GameObject.FindGameObjectWithTag("TrialManager");
        if (go != null)
            tmInstance = go.GetComponents<MonoBehaviour>()
                           .FirstOrDefault(mb => mb.GetType().Name.StartsWith("TrialManager"));
        if (tmInstance == null)
            tmInstance = FindObjectsOfType<MonoBehaviour>()
                         .FirstOrDefault(mb => mb.GetType().Name.StartsWith("TrialManager"));
        if (tmInstance == null)
        {
            Debug.LogError("TMVariableInspector: No TrialManager found.");
            return;
        }

        bool filterActive = includedHeaders != null && includedHeaders.Length > 0;
        bool sectionAllowed = !filterActive;
        string currentSection = null;

        // 2) Get all public fields in declared order
        var fields = tmInstance.GetType()
                       .GetFields(BindingFlags.Instance | BindingFlags.Public)
                       .OrderBy(f => f.MetadataToken);

        foreach (var f in fields)
        {
            // Check for a [Header] attribute on this field
            var headerAttr = f.GetCustomAttribute<HeaderAttribute>();
            if (headerAttr != null)
            {
                // Start a new section
                currentSection = headerAttr.header;
                sectionAllowed = !filterActive || includedHeaders.Contains(currentSection);
                if (sectionAllowed)
                {
                    var hdrRow = Instantiate(headerRowPrefab, contentParent);
                    hdrRow.SetActive(true);
                    var hdrLabel = hdrRow.GetComponentInChildren<TextMeshProUGUI>();
                    if (hdrLabel != null)
                        hdrLabel.text = currentSection;
                }
                else
                {
                    // Section is filtered out; skip its header and fields
                    continue;
                }
            }
                        // Skip fields in disallowed sections
            if (!sectionAllowed)
                continue;
            if (!sectionAllowed)
                continue;

            // Instantiate the appropriate row for this field
            var fieldType = f.FieldType;
            var value     = f.GetValue(tmInstance);

            // — Bool fields —
            if (fieldType == typeof(bool))
            {
                var row = Instantiate(boolFieldRowPrefab, contentParent);
                row.SetActive(true);

                // find label & toggle
                var label  = row.transform.Find("Label").GetComponent<TextMeshProUGUI>();
                var toggle = row.transform.Find("Toggle").GetComponent<Toggle>();

                label.text = f.Name;
                bool initial = (bool)value;
                toggle.isOn = initial;

                // grab the Toggle's graphic Image (must exist on your prefab)
                var img = toggle.targetGraphic as Image;
                if (img == null)
                    img = toggle.GetComponent<Image>();

                // set initial sprite
                if (img != null)
                    img.sprite = initial ? boolOnSprite : boolOffSprite;

                // when the user clicks, update both the field and the sprite
                toggle.onValueChanged.AddListener(b =>
                {
                    f.SetValue(tmInstance, b);
                    if (img != null)
                        img.sprite = b ? boolOnSprite : boolOffSprite;

                    // toggle coin UI immediately if this field controls it
                    if (f.Name == "UseCoinFeedback")
                    {
                        var coinController = CoinController.Instance;
                        if (coinController != null)
                            coinController.UseCoinFeedback = b;
                    }

                    if (f.Name == "ShowScoreUI")
                    {
                        DisplayManager.I.SetTrialManagerScoreUI(b);
                    }

                    if (f.Name == "ShowFeedbackUI")
                    {
                        DisplayManager.I.SetTrialManagerFeedbackUI(b);
                    }
                });
            }

            else if (fieldType == typeof(int)
                  || fieldType == typeof(float)
                  || fieldType == typeof(string))
            {
                var row   = Instantiate(textFieldRowPrefab, contentParent);
                row.SetActive(true);
                var label = row.transform.Find("Label").GetComponent<TextMeshProUGUI>();
                var input = row.transform.Find("InputField").GetComponent<TMP_InputField>();
                label.text = f.Name;
                input.text = value?.ToString() ?? string.Empty;
                input.onEndEdit.AddListener(s =>
                {
                    try
                    {
                        object parsed;
                        if (fieldType == typeof(int)) parsed = int.Parse(s);
                        else if (fieldType == typeof(float)) parsed = float.Parse(s);
                        else parsed = s;
                        f.SetValue(tmInstance, parsed);
                    }
                    catch
                    {
                        Debug.LogWarning($"Invalid value '{s}' for field '{f.Name}'");
                    }
                });
            }
            // skip other types
        }
    }
}
