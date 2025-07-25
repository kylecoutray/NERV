using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class EdgeOutlineEffect : MonoBehaviour
{
    public Material outlineMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (outlineMaterial != null)
            Graphics.Blit(src, dest, outlineMaterial);
        else
            Graphics.Blit(src, dest);
    }
}
