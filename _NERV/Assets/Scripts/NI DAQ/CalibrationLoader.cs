using UnityEngine;
using System;
using System.IO;

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
    [Header("Calibration JSON (optional)")]
    [Tooltip("Drag <savename>_map.json here if you want an inspector fallback")]
    public TextAsset mapJson;

    // the deserialized map
    private Map map;

    // expose it so UI can read/write values before saving
    public Map CurrentMap => map;

    // Singleton for easy access
    public static CalibrationLoader Instance { get; private set; }

    void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 1) try user-chosen file from SessionManager
        string customPath = SessionManager.Instance?.CalibrationPath;
        if (!string.IsNullOrEmpty(customPath))
        {
            if (LoadFromPath(customPath))
            {
                Debug.Log($"[CalibrationLoader] Custom Map: Xscale={map.Xscale:F3}, pixdeg=[{map.pixdeg[0]:F1},{map.pixdeg[1]:F1}]");
                return;  // success!
            }
            Debug.LogWarning($"[CalibrationLoader] Failed loading custom file '{customPath}', falling back…");
        }

        // 2) fallback to inspector asset
        if (mapJson != null)
        {
            map = JsonUtility.FromJson<Map>(mapJson.text);
            Debug.Log($"[CalibrationLoader] Loaded inspector asset map: Xscale={map.Xscale:F3}, pixdeg=[{map.pixdeg[0]:F1},{map.pixdeg[1]:F1}]");
            return;
        }

        // 3) fallback to default Resources file
        TextAsset defaultAsset = Resources.Load<TextAsset>("Calibrations/Example_Calibration");
        if (defaultAsset != null)
        {
            map = JsonUtility.FromJson<Map>(defaultAsset.text);
            Debug.Log($"[CalibrationLoader] Loaded default Example_Calibration.json from Resources");
            Debug.Log($"[CalibrationLoader] Default Map: Xscale={map.Xscale:F3}, pixdeg=[{map.pixdeg[0]:F1},{map.pixdeg[1]:F1}]");
            return;
        }

        // 4) nothing found → disable
        Debug.LogWarning("[CalibrationLoader] No calibration found (custom, inspector asset, or default) → disabling loader.");
        enabled = false;
    }

    /// <summary>
    /// Loads JSON from an arbitrary file path.
    /// Returns true on success (and sets 'map'), false on failure.
    /// </summary>
    public bool LoadFromPath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[CalibrationLoader] File not found: {filePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            map = JsonUtility.FromJson<Map>(json);
            Debug.Log($"[CalibrationLoader] Loaded custom map from: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CalibrationLoader] Error parsing JSON at {filePath}: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Convert raw voltages → screen‐pixel position.
    /// Callers should guard against Instance==null or enabled==false.
    /// </summary>
    public Vector2 VoltToPixel(Vector2 volts)
    {
        float degX = (volts.x - map.Xscalecenter) * map.Xscale;
        float degY = (volts.y - map.Yscalecenter) * map.Yscale;
        float pd   = (map.pixdeg[0] + map.pixdeg[1]) * 0.5f;

        float px = degX * pd + Screen.width  * 0.5f;
        float py = degY * pd + Screen.height * 0.5f;

        return new Vector2(px, py);
    }
}
