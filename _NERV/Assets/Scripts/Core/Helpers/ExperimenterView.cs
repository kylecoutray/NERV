using UnityEngine;
using UnityEngine.UI;

public class ExperimenterView : MonoBehaviour
{
    [Header("Preview Settings")]
    [Tooltip("Low-res RenderTexture for the mini-preview (e.g. 640×360)")]
    public RenderTexture lowResPreviewRT;

    [Tooltip("RawImage on your ExperimenterCanvas that shows the preview")]
    public RawImage previewImage;

    void Start()
    {
        if (lowResPreviewRT == null || previewImage == null)
        {
            Debug.LogError("ExperimenterView: assign both lowResPreviewRT and previewImage!");
            enabled = false;
            return;
        }

        // 1) Hook up the UI
        previewImage.texture = lowResPreviewRT;

        // 2) Find the one and only camera rendering to Display 1
        Camera sourceCam = null;
        foreach (var cam in Camera.allCameras)
        {
            if (cam.targetDisplay == 0)
            {
                sourceCam = cam;
                break;
            }
        }
        if (sourceCam == null)
        {
            Debug.LogError("ExperimenterView: no Camera with targetDisplay==0 found!");
            return;
        }

        // 3) Add (or reuse) the blitter on that camera
        var bl = sourceCam.GetComponent<PreviewBlitter>();
        if (bl == null) bl = sourceCam.gameObject.AddComponent<PreviewBlitter>();
        bl.destRT = lowResPreviewRT;
    }
}
