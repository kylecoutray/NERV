using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InputField    inputField;
    [SerializeField] private Button        sendButton;
    [SerializeField] private ScrollRect    commentsScrollRect;
    [SerializeField] private RectTransform commentsContent;
    [SerializeField] private GameObject    commentBoxPrefab;

    private TrialProgressDisplay progressDisplay;
    private bool hasTyped;
    private int typingStartFrame;

    void Awake()
    {
        progressDisplay = FindObjectOfType<TrialProgressDisplay>();
        if (progressDisplay == null)
            Debug.LogError("CommentManager: No TrialProgressDisplay found.");

        hasTyped = false;

        inputField.onValueChanged.AddListener(_ =>
        {
            if (!hasTyped) hasTyped = true;
            typingStartFrame = Time.frameCount;
        });

        inputField.lineType = InputField.LineType.SingleLine;
        inputField.onEndEdit.AddListener(text =>
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                PostComment();
                inputField.ActivateInputField();
            }
        });

        sendButton.onClick.AddListener(PostComment);
    }

    void OnDestroy()
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();
        sendButton.onClick.RemoveAllListeners();
    }

    private void PostComment()
    {
        string txt = inputField.text.Trim();
        if (string.IsNullOrEmpty(txt)) return;

        string ts = progressDisplay.GetElapsedTimeStamp();

        // Instantiate at top
        var box = Instantiate(commentBoxPrefab, commentsContent);
        box.transform.SetAsFirstSibling();
        box.SetActive(true);

        // Set the text
        var commentText = box.transform.Find("CommentText")
                             .GetComponent<TextMeshProUGUI>();
        if (commentText == null)
        {
            Debug.LogError("CommentManager: 'CommentText' child not found!");
            return;
        }
        commentText.text = $"<b>{ts}</b> {txt}";
        commentText.ForceMeshUpdate();

        // Calculate needed height
        int   lines       = commentText.textInfo.lineCount;
        float fontSize    = commentText.fontSize;
        float lineSpacing = commentText.lineSpacing;
        float paddingV    = 16f;

        float textH  = lines * fontSize + (lines - 1) * lineSpacing;
        float finalH = textH + paddingV;

        // Resize the root RectTransform so layout group positions correctly
        var rootRT = box.GetComponent<RectTransform>();
        rootRT.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            finalH
        );

        // Resize only the RedBG child to fill the root
        var redBG = box.transform.Find("RedBG")?
                          .GetComponent<RectTransform>();
        if (redBG != null)
        {
            redBG.anchorMin = new Vector2(0, 1);
            redBG.anchorMax = new Vector2(1, 1);
            redBG.pivot     = new Vector2(0.5f, 1);
            redBG.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                finalH
            );
        }
        else
        {
            Debug.LogWarning("CommentManager: 'RedBG' not found.");
        }

        // Rebuild the parent layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(commentsContent);

        // Scroll to top
        if (commentsScrollRect != null)
            commentsScrollRect.verticalNormalizedPosition = 1f;

        // Log to ALL_LOGS.txt
        SessionLogManager.Instance.LogExperimenterComment($"Frame: {typingStartFrame} | Elapsed Time: {ts} | Comment: {txt}");

        // Clear and reset
        inputField.text = "";
        hasTyped = false;
    }
}
