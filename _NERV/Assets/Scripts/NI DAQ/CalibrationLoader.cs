using UnityEngine;

[System.Serializable]
public class Map
{
    public float Xscale;
    public float Xscalecenter;
    public float Yscale;
    public float Yscalecenter;
    public float[] pixdeg;   // [pixPerDegX, pixPerDegY]
}

public class CalibrationLoader : MonoBehaviour
{
    [Header("Calibration JSON")]
    public TextAsset mapJson;        // Drag <savename>_map.json here

    Map map;

    // Singleton for easy access
    public static CalibrationLoader Instance { get; private set; }

    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mapJson == null)
        {
            Debug.LogError("[CalibrationLoader] No mapJson assigned!");
            return;
        }

        // Parse the JSON into our Map object
        map = JsonUtility.FromJson<Map>(mapJson.text);
        Debug.Log($"[CalibrationLoader] Loaded map: Xscale={map.Xscale:F3}, pixdeg=[{map.pixdeg[0]:F1},{map.pixdeg[1]:F1}]");
    }

    /// <summary>
    /// Convert raw voltages (voltX, voltY) → screen‐pixel position.
    /// </summary>
    public Vector2 VoltToPixel(Vector2 volts)
    {
        // 1) volts → degrees
        float degX = (volts.x - map.Xscalecenter) * map.Xscale;
        float degY = (volts.y - map.Yscalecenter) * map.Yscale;

        // 2) degrees → pixels (average pix/deg to smooth X/Y mismatch)
        float pd = (map.pixdeg[0] + map.pixdeg[1]) * 0.5f;
        float px = degX * pd + Screen.width  * 0.5f;
        float py = degY * pd + Screen.height * 0.5f;

        return new Vector2(px, py);
    }
}
