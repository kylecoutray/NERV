using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Outline))]
public class HomeButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Name of the scene to load when Home is clicked")]
    [SerializeField] private string sceneToLoad = "TaskSelector";

    private Outline outline;

    void Awake()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;  // start hidden
    }

    // Show the outline when the cursor enters
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
    }

    // Hide the outline when the cursor leaves
    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
    }

    // Load scene on click
    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
