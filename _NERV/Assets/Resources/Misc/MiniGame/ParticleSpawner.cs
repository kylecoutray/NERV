using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class ParticleSpawner : MonoBehaviour
{
    [Header("Particle Settings")]
    public RectTransform particlePrefab;    // Tiny UI‐Image prefab
    public RectTransform playerRt;          // Drag your Player RectTransform here
    [Tooltip("Max simultaneous particles")]
    public int poolSize = 15;
    [Tooltip("Seconds between spawns (lower = more)")]
    public float minSpawnInterval = 0.02f;
    public float maxSpawnInterval = 0.1f;
    [Tooltip("How far in front of the player's left edge to start")]
    public float spawnXOffset = 5f;
    public float scrollSpeed = 200f;        // Match your spikes
    public float fadeDuration = 0.5f;       // Fade‐in/out timing

    [Header("Size Range (px)")]
    public float minWidth  = 3f;
    public float maxWidth  = 12f;
    public float minHeight = 3f;
    public float maxHeight = 8f;

    private RectTransform canvasRect;
    private List<RectTransform> pool;
    private int poolIndex = 0;
    private Vector3[] pCorners = new Vector3[4];
    private Coroutine spawnRoutine;

    void Awake()
    {
        // Cache Canvas
        var rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("[ParticleSpawner] Must be under a Canvas!");
            return;
        }
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        if (playerRt == null)
            Debug.LogError("[ParticleSpawner] playerRt not assigned!");

        // Pre‑allocate pool
        pool = new List<RectTransform>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var inst = Instantiate(particlePrefab, canvasRect, false);
            inst.gameObject.SetActive(false);
            var img = inst.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            pool.Add(inst);
        }
    }

    void OnEnable()
    {
        if (canvasRect != null && playerRt != null && particlePrefab != null)
        {
            if (spawnRoutine == null)
                spawnRoutine = StartCoroutine(SpawnRoutine());
        }
    }

    void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    /// <summary>
    /// Disables all pooled particles and resets the pool index.
    /// </summary>
    public void ResetPool()
    {
        if (pool == null) return;
        poolIndex = 0;
        foreach (var inst in pool)
            if (inst != null)
                inst.gameObject.SetActive(false);
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            SpawnParticle();
        }
    }

    void SpawnParticle()
    {
        if (pool == null || pool.Count == 0) return;

        // Get next particle from pool
        var inst = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Count;

        // Randomize size & pivot
        float w = Random.Range(minWidth, maxWidth);
        float h = Random.Range(minHeight, maxHeight);
        inst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        inst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
        inst.pivot = new Vector2(0.5f, 0f);

        // Compute spawn X at player's left edge + offset
        playerRt.GetWorldCorners(pCorners);
        Vector3 worldLeftCenter = (pCorners[0] + pCorners[1]) * 0.5f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, worldLeftCenter),
            null, out Vector2 leftLocal
        );
        float spawnX = leftLocal.x + spawnXOffset;

        // Compute random Y between player's bottom & top
        Vector3 worldBot = (pCorners[0] + pCorners[3]) * 0.5f;
        Vector3 worldTop = (pCorners[1] + pCorners[2]) * 0.5f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, worldBot),
            null, out Vector2 botLocal
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, worldTop),
            null, out Vector2 topLocal
        );
        float spawnY = Random.Range(botLocal.y, topLocal.y);

        // Activate & position
        inst.anchoredPosition = new Vector2(spawnX, spawnY);
        inst.localRotation    = Quaternion.identity;
        inst.gameObject.SetActive(true);

        // Initialize movement & fade
        var pc = inst.GetComponent<ParticleController>();
        if (pc == null) pc = inst.gameObject.AddComponent<ParticleController>();
        pc.Setup(scrollSpeed, fadeDuration);
    }
}
