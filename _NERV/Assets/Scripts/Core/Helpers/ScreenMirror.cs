using UnityEngine;

public class ScreenMirror : MonoBehaviour
{
    [Tooltip("Camera that renders into your preview RenderTexture")]
    public Camera previewCam;
    [Tooltip("How many times per second to update the preview")]
    public int previewFPS = 5;

    float dt;
    float accumulator = 0f;

    void Awake()
    {
        dt = 1f / previewFPS;

        // Disable Unity's automatic render on this camera
        if (previewCam != null)
            previewCam.enabled = false;
    }

    void LateUpdate()
    {
        accumulator += Time.unscaledDeltaTime;
        if (accumulator >= dt)
        {
            previewCam.Render();     // now only called ~previewFPS times/sec
            accumulator -= dt;       // preserve any extra time beyond dt
        }
    }
}
