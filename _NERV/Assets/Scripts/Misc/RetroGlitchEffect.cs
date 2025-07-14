using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RetroGlitchEffect : MonoBehaviour
{
    /* ────────── Core Glitch Parameters ────────── */
    [Header("Glitch Settings")]
    public Shader glitchShader;

    [Range(0f, 1f)] public float baseIntensity  = 0.3f;
    [Range(0f, 1f)] public float spikeIntensity = 0.5f;
    [Range(0f, 1f)] public float scanlineIntensity = 0.5f;
    [Range(0f, 0.1f)] public float baseRgbSplit  = 0.02f;
    [Range(0f, 0.1f)] public float spikeRgbSplit = 0.05f;
    [Range(0f, 10f)] public float speed = 1f;
    public Texture2D noiseTex;

    /* ────────── Spike Timing ────────── */
    [Header("Intensity Spike Timing (sec)")]
    public Vector2 intensityIntervalRange = new Vector2(5f, 15f);
    public Vector2 intensityDurationRange = new Vector2(0.20f, 0.35f);

    [Header("RGB Split Spike Timing (sec)")]
    public Vector2 rgbIntervalRange = new Vector2(5f, 15f);
    public Vector2 rgbDurationRange = new Vector2(0.20f, 0.35f);

    /* ────────── Internals ────────── */
    private Material mat;
    private bool isIntensitySpiking, isRgbSpiking;
    private float nextIntensitySpikeTime, nextRgbSpikeTime;
    private float currentIntensity, currentRgbSplit;

    void Awake()
    {
        // always subscribe to sceneLoaded so we reset on every return to TaskSelector
        SceneManager.sceneLoaded += OnSceneLoaded;
        // also run once in case we're already in TaskSelector
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // only initialize (or re-initialize) when entering TaskSelector
        if (scene.name != "TaskSelector")
            return;

        if (glitchShader == null)
            throw new System.Exception("RetroGlitchEffect: No glitchShader assigned!");
        if (mat == null)
            mat = new Material(glitchShader);

        // reset spike timers
        isIntensitySpiking = false;
        isRgbSpiking = false;
        ScheduleNextIntensitySpike();
        ScheduleNextRgbSpike();
    }

    void Update()
    {
        // only run spikes logic in TaskSelector
        if (SceneManager.GetActiveScene().name != "TaskSelector")
            return;

        if (!isIntensitySpiking && Time.time >= nextIntensitySpikeTime)
            StartCoroutine(IntensitySpike());
        if (!isRgbSpiking && Time.time >= nextRgbSpikeTime)
            StartCoroutine(RgbSpike());

        currentIntensity = isIntensitySpiking ? spikeIntensity : baseIntensity;
        currentRgbSplit  = isRgbSpiking     ? spikeRgbSplit : baseRgbSplit;
    }

    IEnumerator IntensitySpike()
    {
        isIntensitySpiking = true;
        yield return new WaitForSeconds(
            Random.Range(intensityDurationRange.x, intensityDurationRange.y)
        );
        isIntensitySpiking = false;
        ScheduleNextIntensitySpike();
    }

    IEnumerator RgbSpike()
    {
        isRgbSpiking = true;
        yield return new WaitForSeconds(
            Random.Range(rgbDurationRange.x, rgbDurationRange.y)
        );
        isRgbSpiking = false;
        ScheduleNextRgbSpike();
    }

    void ScheduleNextIntensitySpike()
    {
        nextIntensitySpikeTime = Time.time
            + Random.Range(intensityIntervalRange.x, intensityIntervalRange.y);
    }

    void ScheduleNextRgbSpike()
    {
        nextRgbSpikeTime = Time.time
            + Random.Range(rgbIntervalRange.x, rgbIntervalRange.y);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // pass through on any non-TaskSelector scene or before material is ready
        if (SceneManager.GetActiveScene().name != "TaskSelector" || mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // apply glitch shader
        mat.SetFloat("_GlitchIntensity",   currentIntensity);
        mat.SetFloat("_ScanlineIntensity", scanlineIntensity);
        mat.SetFloat("_RGBSplitAmount",    currentRgbSplit);
        mat.SetFloat("_Speed",             speed);
        mat.SetTexture("_NoiseTex",        noiseTex);

        Graphics.Blit(src, dest, mat);
    }
}