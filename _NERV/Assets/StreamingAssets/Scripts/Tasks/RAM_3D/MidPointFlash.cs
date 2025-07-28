using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class MidPointFlash : MonoBehaviour
{
    public Image borderPanel;            // your full-screen UI panel
    public float flashDuration = 0.5f;
    
    // These colors match MNM
    public Color32 matchColor    = new Color32(0x00, 0xBF, 0xFF, 0xAF);
    public Color32 nonMatchColor = new Color32(0xFF, 0x6F, 0x00, 0xAF);


    [HideInInspector] public int contextType;  // set by the runner
    [HideInInspector] public bool FlashDone;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        FlashDone = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("MainCamera")) return;
        StartCoroutine(DoFlash());
        // disable so we only trigger once per trial
        GetComponent<Collider>().enabled = false;
    }

    private IEnumerator DoFlash()
    {
        // pick color
        var c = (contextType == 0) ? matchColor : nonMatchColor;
        borderPanel.color = c;
        borderPanel.canvasRenderer.SetAlpha(1f);

        yield return new WaitForSeconds(flashDuration);

        // fade out
        borderPanel.CrossFadeAlpha(0f, 0.3f, false);

        FlashDone = true;
    }
}
