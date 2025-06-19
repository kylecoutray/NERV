using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TaskMenuController : MonoBehaviour
{
    [Header("References")]
    public RectTransform GridParent;    // assign GridPanel.transform
    public Button        ButtonPrefab;  // assign TaskButton prefab

    void Start()
    {
        var defs = Resources.LoadAll<ExperimentDefinition>("ExperimentDefinitions");
        Debug.Log($"Found {defs.Length} definitions.");
        foreach (var def in defs)
        {
            Debug.Log($"  Adding button for {def.name} (scene: {def.name})");
            var btn = Instantiate(ButtonPrefab, GridParent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            label.text = def.Acronym;

            string sceneToLoad = def.name;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"Loading scene: {sceneToLoad}");
                SceneManager.LoadScene(sceneToLoad);
            });
        }
    }

}
