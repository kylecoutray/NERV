using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskMenuController : MonoBehaviour
{
    [Header("References")]
    public RectTransform GridParent;    // assign your GridPanel.transform here
    public Button        ButtonPrefab;  // assign your TaskButton prefab here

    private TaskSelectorUI _ui;
    private ExperimentDefinition[] _allDefs;

    void Awake()
    {
        // grab the UI manager so we can funnel through its checks
        _ui = FindObjectOfType<TaskSelectorUI>();
        if (_ui == null)
            Debug.LogError("[TaskMenuController] TaskSelectorUI not found in scene!");

        // load all defs once
        _allDefs = Resources.LoadAll<ExperimentDefinition>("ExperimentDefinitions");

        // uncheck this if you want to know how many Exp defs you have -- im omitting to have a cool "Welcome" Debug lol
        // Debug.Log($"[TaskMenuController] Loaded {_allDefs.Length} ExperimentDefinitions");
    }

    /// <summary>
    /// Call this after the toggles have been set.
    /// Only the acronyms in this list will be made into buttons.
    /// </summary>
    public void PopulateTasks(IEnumerable<string> acronyms)
    {
        // clear existing buttons (if any)
        foreach (Transform child in GridParent)
            Destroy(child.gameObject);

        var list = acronyms.ToList();
        // uncheck if you need this debug
        // Debug.Log($"[TaskMenuController] Populating {list.Count} task(s).");

        foreach (var acr in list)
        {
            // find the def whose Acronym matches
            var def = _allDefs.FirstOrDefault(d => d.Acronym == acr);
            if (def == null)
            {
                Debug.LogWarning($"[TaskMenuController] No ExperimentDefinition found for acronym '{acr}'");
                continue;
            }

            // instantiate a button
            var btn = Instantiate(ButtonPrefab, GridParent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            label.text = def.Acronym;

            string sceneToLoad = def.name; // full scene name
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[TaskMenuController] Loading scene: {sceneToLoad}");
                _ui.OnTaskIconClicked(sceneToLoad);
            });
        }

        // if you’re using a GridLayoutGroup and want to center / wrap after 18:
        var grid = GridParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            int total = list.Count;
            int cols  = Mathf.Min(18, total);
            grid.constraintCount = cols;
            grid.childAlignment = TextAnchor.MiddleCenter;
        }
    }
}