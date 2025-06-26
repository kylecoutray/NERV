using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CursorWarpController : MonoBehaviour
{
    public Material cursorWarpMat;
    private bool effectEnabled = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            effectEnabled = !effectEnabled;

        if (!effectEnabled) return;

        // Update cursor UV
        Vector3 mouse = Input.mousePosition;
        Vector2 uv = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
        cursorWarpMat.SetVector("_Cursor", new Vector4(uv.x, uv.y, 0, 0));

        // On click, trigger pulse
        if (Input.GetMouseButtonDown(0))
        {
            cursorWarpMat.SetVector("_PulseCenter", new Vector4(uv.x, uv.y, 0, 0));
            cursorWarpMat.SetFloat("_PulseStartTime", Time.time);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (effectEnabled && cursorWarpMat != null)
            Graphics.Blit(src, dest, cursorWarpMat);
        else
            Graphics.Blit(src, dest);
    }
}
