using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Outline))]
public class PauseTriggerButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Tooltip("What label to show on the pause screen")]
    public string pauseLabel = "EXP. PAUSE";

    private Outline outline;
    private BlockPauseController _pauseCtrl;

    void Awake()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;

        // find your BlockPauseController
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            _pauseCtrl = root.GetComponentInChildren<BlockPauseController>(true);
            if (_pauseCtrl != null) break;
        }
        if (_pauseCtrl == null)
            Debug.LogError("PauseTriggerButton: No BlockPauseController found!");
    }

    // New: these will now get called
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_pauseCtrl != null)
            StartCoroutine(TriggerPause());
    }

    private IEnumerator TriggerPause()
    {
        yield return StartCoroutine(_pauseCtrl.ShowPause(pauseLabel));
    }
}
