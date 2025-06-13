using UnityEngine;
using System.Collections;

public class CircleHalo : MonoBehaviour
{
    [Header("Halo Prefabs (assign or auto-load)")]
    public GameObject CorrectHaloPrefab;    // yellow ring
    public GameObject IncorrectHaloPrefab;  // white ring

    [Header("Sizing & Position")]
    public float BaseScale     = 0.3f;
    public float OffsetDistance= -1f;

    [Header("Light Settings (optional)")]
    public float HaloIntensity = 1f;
    public float HaloRange     = 1.5f;

    void Awake()
    {
        // auto-load if you haven’t assigned in Inspector
        if (CorrectHaloPrefab == null)
            CorrectHaloPrefab = Resources.Load<GameObject>("TaskObjects/HaloRingCorrect");
        if (IncorrectHaloPrefab == null)
            IncorrectHaloPrefab = Resources.Load<GameObject>("TaskObjects/HaloRingIncorrect");
    }

    /// <summary>
    /// Spawns the correct or incorrect halo prefab behind this object.
    /// </summary>
    public IEnumerator CreateOneShot(bool correct, float duration)
    {
        var cam = Camera.main ?? Camera.current;
        if (cam == null) yield break;

        // spawn behind object, along camera→object vector
        Vector3 dir      = (transform.position - cam.transform.position).normalized;
        Vector3 spawnPos = transform.position - dir * OffsetDistance;

        var prefab = correct ? CorrectHaloPrefab : IncorrectHaloPrefab;
        var halo   = Instantiate(prefab, spawnPos, Quaternion.identity);

        // scale & light
        halo.transform.localScale = Vector3.one * BaseScale;
        var light = halo.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.intensity = HaloIntensity;
            light.range     = HaloRange;
        }

        yield return new WaitForSeconds(duration);
        Destroy(halo);
    }
}
