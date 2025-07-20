using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class TiledScrollBackground : MonoBehaviour
{
    [Header("Tiling Settings")]
    public float tilesX = 10f;
    public bool maintainTileAspect = true;
    public float tilesY = 10f;

    [Header("Scroll Settings")]
    [Tooltip("Speed of the scroll along the diagonal in UV units per second.")]
    public float scrollSpeed = 0.02f;

    [Tooltip("Angle (degrees) to skew the tiling grid (tiles stay upright).")]
    public float tileGridAngle = 20f;

    private RawImage _rawImage;
    private RectTransform _rt;
    private Rect _uv;

    // Precomputed rotation for UV offset
    private Vector2 _uvDirection;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rt       = _rawImage.rectTransform;

        // Compute how many tiles vertically to keep square
        float h = maintainTileAspect
            ? tilesX * (_rt.rect.height / _rt.rect.width)
            : tilesY;

        _uv = new Rect(0, 0, tilesX, h);
        _rawImage.uvRect = _uv;

        // Precompute a direction vector for scrolling based on the angle
        float rad = tileGridAngle * Mathf.Deg2Rad;
        _uvDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    void Update()
    {
        // Move the UV offset along the rotated grid
        _uv.x += _uvDirection.x * scrollSpeed * Time.deltaTime;
        _uv.y += _uvDirection.y * scrollSpeed * Time.deltaTime;

        _rawImage.uvRect = _uv;
    }
}
