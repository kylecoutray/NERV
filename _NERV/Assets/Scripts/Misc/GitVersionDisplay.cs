using UnityEngine;
using TMPro;

public class GitVersionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI versionText;

    void Start()
    {
        if (versionText == null)
            versionText = GetComponent<TextMeshProUGUI>();

        // Load the git_commit.txt from Resources
        TextAsset commitFile = Resources.Load<TextAsset>("git_commit");
        string commit = commitFile != null ? commitFile.text.Trim() : "unknown";

        // Format and display
        versionText.text = $"{commit}";
    }
}
