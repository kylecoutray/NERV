// Assets/Scripts/TVEffectController.cs

using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Camera))]
public class TVEffectController : MonoBehaviour
{
    [Header("Shader & Material")]
    public Shader tvShader;
    private Material mat;

    [Header("Scanline Settings")]
    [Range(10,256)] public int scanlineCount      = 120;
    [Range(0,0.1f)] public float scanlineIntensity = 0.03f;
    public Color scanlineColor                   = Color.red;
    [Range(-2f,2f)] public float scanlineScrollSpeed = 0.5f;

    [Header("Flicker Settings")]
    [Range(0.5f,5f)] public float flickerSpeed   = 2f;
    [Range(0f,0.1f)] public float flickerAmount  = 0.02f;

    [Header("Glitch Settings")]
    [Range(0f,1f)]   public float glitchChance   = 0.02f;
    [Range(0f,0.2f)] public float glitchStrength = 0.05f;

    void Start()
    {
        if (tvShader == null)
            tvShader = Shader.Find("Custom/SuperhotTVEffect");
        mat = new Material(tvShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // scanlines
        mat.SetFloat("_ScanlineCount", scanlineCount);
        mat.SetFloat("_ScanlineIntensity", scanlineIntensity);
        mat.SetColor("_ScanlineColor", scanlineColor);
        mat.SetFloat("_ScanlineScrollSpeed", scanlineScrollSpeed);

        // flicker
        mat.SetFloat("_FlickerSpeed", flickerSpeed);
        mat.SetFloat("_FlickerAmount", flickerAmount);

        // glitch
        mat.SetFloat("_GlitchChance", glitchChance);
        mat.SetFloat("_GlitchStrength", glitchStrength);

        Graphics.Blit(src, dest, mat);
    }
}
