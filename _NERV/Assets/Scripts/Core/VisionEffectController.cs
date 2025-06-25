using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class VisionEffectController : MonoBehaviour
{
    public Material terminalMat;
    public Material glitchMat;
    public Material liquidScannerMat;

    private enum VisionMode { Off, Terminal, Glitch, LiquidScanner }
    private VisionMode currentMode = VisionMode.Off;

    Vector3 originalCamPos;

    void Start()
    {
        originalCamPos = transform.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) currentMode = VisionMode.Terminal;
        if (Input.GetKeyDown(KeyCode.G)) currentMode = VisionMode.Glitch;
        if (Input.GetKeyDown(KeyCode.L)) currentMode = VisionMode.LiquidScanner;

        if (Input.GetKeyDown(KeyCode.N)) currentMode = VisionMode.Off;

        // Disable component if we're not in the TaskSelector scene
        if (SceneManager.GetActiveScene().name != "TaskSelector")
        {
            currentMode = VisionMode.Off;
            return;
        }

        if (currentMode == VisionMode.Glitch)
        {
            //  Add screen shake when glitch is active
            float intensity = Random.Range(0.01f, 0.04f);
            transform.localPosition = originalCamPos + Random.insideUnitSphere * intensity;
        }
        else
        {
            // Reset camera position
            transform.localPosition = originalCamPos;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        switch (currentMode)
        {
            case VisionMode.Terminal:
                if (terminalMat != null) Graphics.Blit(src, dest, terminalMat);
                else Graphics.Blit(src, dest);
                break;
            case VisionMode.Glitch:
                if (glitchMat != null) Graphics.Blit(src, dest, glitchMat);
                else Graphics.Blit(src, dest);
                break;
            case VisionMode.LiquidScanner:
                if (liquidScannerMat != null) Graphics.Blit(src, dest, liquidScannerMat);
                else Graphics.Blit(src, dest);
                break;
            default:
                Graphics.Blit(src, dest);
                break;
        }
    }
}
