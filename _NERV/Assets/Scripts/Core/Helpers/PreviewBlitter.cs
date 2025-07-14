using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PreviewBlitter : MonoBehaviour
{
    [Tooltip("RenderTexture that will receive this camera’s final image")]
    public RenderTexture destRT;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (destRT != null)
            Graphics.Blit(src, destRT);
        Graphics.Blit(src, dest);
    }
}
