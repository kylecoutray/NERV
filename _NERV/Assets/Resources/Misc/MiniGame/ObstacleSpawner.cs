using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public RectTransform spikePrefab;    // Your spike prefab (with Image component)
    public RectTransform groundPanel;    // Drag your Ground Panel here
    public float minSpawnInterval = 1f;  // Min seconds between spikes
    public float maxSpawnInterval = 4f;  // Max seconds between spikes
    public float scrollSpeed = 200f;     // How fast spikes move left
    public float spawnYOffset = 0f;      // Fine‑tune vertical alignment

    private RectTransform canvasRect;
    private Vector2 localRight;
    private Vector2 localTop;
    private Coroutine spawnRoutine;

    void Awake()
    {
        // Cache root Canvas
        var root = GetComponentInParent<Canvas>();
        if (root == null) { Debug.LogError("[Spawner] Must be under a Canvas!"); return; }
        canvasRect = root.GetComponent<RectTransform>();

        // Compute ground’s right‑center in canvas space
        Vector3[] corners = new Vector3[4];
        groundPanel.GetWorldCorners(corners);
        Vector3 worldRightCenter = (corners[2] + corners[3]) * 0.5f;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, worldRightCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPt, null, out localRight);

        // Compute ground’s top‑center in canvas space
        Vector3 worldTopCenter = (corners[1] + corners[2]) * 0.5f;
        screenPt = RectTransformUtility.WorldToScreenPoint(null, worldTopCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPt, null, out localTop);
    }

    void OnEnable()
    {
        if (spikePrefab == null)   Debug.LogError("[Spawner] spikePrefab not assigned!");
        if (groundPanel == null)   Debug.LogError("[Spawner] groundPanel not assigned!");
        if (canvasRect == null)    Debug.LogError("[Spawner] Canvas RectTransform missing!");

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            // 1) Instantiate under the Canvas (worldPositionStays=false)
            RectTransform inst = Instantiate(spikePrefab, canvasRect, false);

            // 2) Trim transparent padding by matching RectTransform size to sprite’s visible rect
            var img = inst.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                float w = img.sprite.rect.width;
                float h = img.sprite.rect.height;
                inst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                inst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }

            // 3) Pivot at bottom‑center so anchoredPosition.y is the sprite’s base
            inst.pivot = new Vector2(0.5f, 0f);

            // 4) Compute spawn X and Y
            float halfW = inst.rect.width * 0.5f;
            float spawnX = localRight.x + halfW;
            float spawnY = localTop.y + spawnYOffset;

            inst.anchoredPosition = new Vector2(spawnX, spawnY);

            // 5) Attach controller
            inst.gameObject
                .AddComponent<ObstacleController>()
                .Init(scrollSpeed, groundPanel);
        }
    }
    void OnDisable()
    {
        // stop spawning when deactivated
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null) spawnRoutine = StartCoroutine(SpawnRoutine());
    }
}
