using UnityEngine;

[DefaultExecutionOrder(-200)]  // Make sure our Start() runs before GenericConfigManager.Start()
public class RuleModeManager : MonoBehaviour
{
    [Header("Choose 1-rule or 2-rule mode")]
    [Tooltip("If true, uses the 2-rule CSV; otherwise uses the 1-rule CSV.")]
    public bool useTwoRules = true;

    [Header("Drag your generated CSVs here")]
    [Tooltip("The 1-rule trial definition CSV, imported as a TextAsset")]
    public TextAsset oneRuleTrials;

    [Tooltip("The 2-rule trial definition CSV, imported as a TextAsset")]
    public TextAsset twoRuleTrials;

    [Tooltip("The stimulus-index CSV (common to both modes), as a TextAsset")]
    public TextAsset stimIndexCsv;

    // Remove Awake() entirely—no file-copying on disk anymore

    void Start()
    {
        // 1) Pick the correct trial-def asset
        TextAsset trialDef = useTwoRules ? twoRuleTrials : oneRuleTrials;
        if (trialDef == null)
        {
            Debug.LogError($"[RuleModeManager] Missing {(useTwoRules ? "2-rule" : "1-rule")} TextAsset! Please drag it in.");
            return;
        }

        // 2) Ensure your stim-index CSV is assigned
        if (stimIndexCsv == null)
        {
            Debug.LogError("[RuleModeManager] Missing StimIndex CSV TextAsset! Please drag it in.");
            return;
        }

        // 3) Grab your config manager singleton
        var cfg = GenericConfigManager.Instance;
        if (cfg == null)
        {
            Debug.LogError("[RuleModeManager] GenericConfigManager.Instance is null. Make sure it exists in the scene.");
            return;
        }

        // 4) Apply the overrides so GenericConfigManager.Start() will pick them up
        cfg.trialDefOverride = trialDef;
        cfg.stimIndexOverride = stimIndexCsv;

        Debug.Log($"[RuleModeManager] Applied {(useTwoRules ? "2-rule" : "1-rule")} overrides to GenericConfigManager.");
    }
}
