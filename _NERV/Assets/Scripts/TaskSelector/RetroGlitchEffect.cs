using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RetroGlitchEffect : MonoBehaviour
{
    [Header("Glitch Settings")]
    public Shader glitchShader;
    [Range(0f, 1f)] public float glitchIntensity = 0.3f;
    [Range(0f, 1f)] public float scanlineIntensity = 0.5f;
    [Range(0f, 0.1f)] public float rgbSplit = 0.02f;
    [Range(0f, 10f)] public float speed = 1f;
    public Texture2D noiseTex;

    private Material mat;

    void Awake()
    {
        // Disable component if we're not in the TaskSelector scene
        if (SceneManager.GetActiveScene().name != "TaskSelector")
        {
            enabled = false;
            return;
        }

        // Initialize the glitch material
        if (glitchShader != null)
            mat = new Material(glitchShader);
        else
            Debug.LogError("RetroGlitchEffect: No glitchShader assigned!");
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // If disabled or material missing, just blit normally
        if (!enabled || mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // Update shader parameters
        mat.SetFloat("_GlitchIntensity", glitchIntensity);
        mat.SetFloat("_ScanlineIntensity", scanlineIntensity);
        mat.SetFloat("_RGBSplitAmount", rgbSplit);
        mat.SetFloat("_Speed", speed);
        mat.SetTexture("_NoiseTex", noiseTex);

        // Apply post-processing
        Graphics.Blit(src, dest, mat);
    }
}
