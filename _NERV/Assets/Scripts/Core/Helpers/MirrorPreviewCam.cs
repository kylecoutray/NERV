using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MirrorPreviewCam : MonoBehaviour
{
    [Header("Preview Camera")]
    [Tooltip("If left empty, will look for a GameObject named 'PreviewCam'.")]
    public Transform previewTransform;
    public string previewName = "PreviewCam";

    [Header("Sync Options")]
    [Tooltip("Copy transform every frame if true; otherwise only once on Start.")]
    public bool copyEveryFrame = true;

    [Tooltip("How many times per second to update when copying each frame.")]
    [Range(1, 60)]
    public float updateRate = 20f;

    // Optional: maintain initial offset
    public bool keepOffset = false;
    private Vector3 posOffset;
    private Quaternion rotOffset;

    // Internal timer
    private float nextUpdateTime = 0f;
    private float updateInterval => 1f / updateRate;

    void Awake()
    {
        if (previewTransform == null)
        {
            var go = GameObject.Find(previewName);
            if (go != null) previewTransform = go.transform;
            else Debug.LogWarning($"MirrorPreviewCam: Couldn’t find '{previewName}'.");
        }

        if (previewTransform != null && keepOffset)
        {
            posOffset = previewTransform.position - transform.position;
            rotOffset = Quaternion.Inverse(transform.rotation) * previewTransform.rotation;
        }
    }

    void Start()
    {
        if (previewTransform != null)
            CopyToPreview();
        nextUpdateTime = Time.time + updateInterval;
    }

    void LateUpdate()
    {
        if (!copyEveryFrame || previewTransform == null) return;

        if (Time.time >= nextUpdateTime)
        {
            CopyToPreview();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void CopyToPreview()
    {
        if (keepOffset)
        {
            previewTransform.SetPositionAndRotation(
                transform.position + posOffset,
                transform.rotation * rotOffset
            );
        }
        else
        {
            previewTransform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }
}
