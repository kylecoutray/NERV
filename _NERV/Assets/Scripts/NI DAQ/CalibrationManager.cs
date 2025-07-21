using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;
using static NativeDAQmx;
using static SessionManager;

public class CalibrationManager : MonoBehaviour
{
    [Header("Device Settings")]
    public string deviceName = "Dev1";             // NI device name (e.g., "Dev1" or "SimDev1")

    [Header("UI Calibration Points")]  
    public List<RectTransform> calibrationPoints;

    [Header("Simulation")]
    public bool simulateDAQ = true;
    public bool simulateByCursor = false; // Use cursor position for simulated voltages
    public float simMinVoltage = -5f;
    public float simMaxVoltage = 5f;

    [Header("Sampling Parameters")]
    public int samplesPerPoint = 10;
    public float sampleInterval = 0.05f;
    public float maxStdDev = 0.1f;
    public int maxRetries = 3;

    [Header("Fixation Detection")]
    public float holdDuration = 1f;               // seconds of stable gaze required
    public float holdSampleInterval = 0.05f;      // seconds between fixation checks
    public float holdStdDev = 0.05f;              // max voltage stddev for fixation
    public float maxWaitForFixation = 5f;         // max time to wait for fixation

    [Header("Pixel-per-Degree (for JSON)")]
    [Tooltip("Enter your scene's pixel-per-degree scale here")]
    public float pixPerDegX = 1f;
    public float pixPerDegY = 1f;

    private List<Vector2> eyeVoltages = new List<Vector2>();
    private IntPtr task = IntPtr.Zero;

    void Start()
    {
        // Clear any leftover DAQ task
        if (task != IntPtr.Zero)
        {
            DAQmxStopTask(task);
            DAQmxClearTask(task);
            task = IntPtr.Zero;
            Debug.Log("[!] Cleared previous DAQ task.");
        }

        if (calibrationPoints == null || calibrationPoints.Count == 0)
        {
            Debug.LogError("[!] No calibration points assigned!");
            return;
        }

        if (!simulateDAQ)
            SetupTasks();
        else
            Debug.Log("[!] Simulation mode ON: no hardware tasks started.");

        StartCoroutine(RunCalibration());
    }

    public const int DAQmx_Val_GroupByChannel = 0;

    private Vector2 ReadVoltages()
    {
        if (simulateDAQ)
        {
            if (simulateByCursor)
            {
                Vector3 mp = Input.mousePosition;
                float vx = Mathf.Lerp(simMinVoltage, simMaxVoltage, mp.x / Screen.width);
                float vy = Mathf.Lerp(simMinVoltage, simMaxVoltage, mp.y / Screen.height);
                return new Vector2(vx, vy);
            }

            float mid = (simMinVoltage + simMaxVoltage) * 0.5f;
            return new Vector2(mid, mid);
        }

        double[] data = new double[2];
        int sampsRead;
        int err = DAQmxReadAnalogF64(
            task,
            1,
            10.0,
            DAQmx_Val_GroupByChannel,
            data,
            (uint)data.Length,
            out sampsRead,
            IntPtr.Zero);
        CheckError(err, "Read AI0&AI1");
        return new Vector2((float)data[0], (float)data[1]);
    }

    void SetupTasks()
    {
        Debug.Log($"[!] Initializing DAQ task on device {deviceName}");
        int err = DAQmxCreateTask(string.Empty, out task);
        CheckError(err, "CreateTask");

        string chanList = $"{deviceName}/ai0,{deviceName}/ai1";
        err = DAQmxCreateAIVoltageChan(
            task,
            chanList,
            string.Empty,
            DAQmx_Val_RSE,
            -5.0,
            5.0,
            DAQmx_Val_Volts,
            null);
        CheckError(err, "CreateAIVoltageChan");

        err = DAQmxStartTask(task);
        CheckError(err, "StartTask");
    }

    IEnumerator RunCalibration()
    {
        for (int i = 0; i < calibrationPoints.Count; i++)
        {
            var pt = calibrationPoints[i];
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetries && !success)
            {
                pt.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.2f);

                // Fixation detection...
                int bufSize = Mathf.CeilToInt(holdDuration / holdSampleInterval);
                var vxBuf = new Queue<float>(bufSize);
                var vyBuf = new Queue<float>(bufSize);
                float holdTimer = 0f, elapsed = 0f;

                while (holdTimer < holdDuration && elapsed < maxWaitForFixation)
                {
                    Vector2 v = ReadVoltages();
                    vxBuf.Enqueue(v.x); vyBuf.Enqueue(v.y);
                    if (vxBuf.Count > bufSize) { vxBuf.Dequeue(); vyBuf.Dequeue(); }

                    float stdVx = ComputeStd(vxBuf), stdVy = ComputeStd(vyBuf);
                    if (stdVx <= holdStdDev && stdVy <= holdStdDev)
                        holdTimer += holdSampleInterval;
                    else
                        holdTimer = 0f;

                    elapsed += holdSampleInterval;
                    yield return new WaitForSeconds(holdSampleInterval);
                }

                if (holdTimer < holdDuration)
                {
                    attempt++;
                    Debug.LogWarning($"[!] Dot {i}: fixation not stable after {elapsed:F1}s, retrying ({attempt}/{maxRetries})");
                    pt.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }
                Debug.Log($"[!] Dot {i}: fixation confirmed after {elapsed:F1}s");

                // Voltage sampling
                var vxSamples = new List<float>();
                var vySamples = new List<float>();
                for (int s = 0; s < samplesPerPoint; s++)
                {
                    Vector2 v = ReadVoltages();
                    vxSamples.Add(v.x); vySamples.Add(v.y);
                    yield return new WaitForSeconds(sampleInterval);
                }

                float meanVx = vxSamples.Average();
                float meanVy = vySamples.Average();
                float stdSVx = ComputeStd(vxSamples);
                float stdSVy = ComputeStd(vySamples);
                Debug.Log($"[!] Point {i}, attempt {attempt+1}: mean=({meanVx:F3},{meanVy:F3}), std=({stdSVx:F3},{stdSVy:F3})");

                if (stdSVx <= maxStdDev && stdSVy <= maxStdDev)
                {
                    eyeVoltages.Add(new Vector2(meanVx, meanVy));
                    success = true;
                }
                else
                {
                    attempt++;
                    Debug.LogWarning($"[!] Dot {i}: high variance, retrying ({attempt}/{maxRetries})");
                }
            }

            pt.gameObject.SetActive(false);
            if (!success)
            {
                Debug.LogError($"[!] Calibration failed at point {i}.");
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (!simulateDAQ)
            CleanupTasks();

        SaveMapping();
        Debug.Log("[!] Calibration completed successfully.");
        SceneManager.LoadScene("TestCalibration");
    }

    private float ComputeStd(IEnumerable<float> data)
    {
        float mean = data.Average();
        return Mathf.Sqrt(data.Sum(v => (v - mean) * (v - mean)) / data.Count());
    }

    void CleanupTasks()
    {
        if (task != IntPtr.Zero)
        {
            DAQmxClearTask(task);
            task = IntPtr.Zero;
        }
    }

    void SaveMapping()
    {
        // Gather screen X/Y and voltage X/Y
        var pixX = calibrationPoints.Select(r => r.anchoredPosition.x).ToList();
        var pixY = calibrationPoints.Select(r => r.anchoredPosition.y).ToList();
        var voltX = eyeVoltages.Select(v => v.x).ToList();
        var voltY = eyeVoltages.Select(v => v.y).ToList();

        // Means
        float meanPixX = pixX.Average();
        float meanPixY = pixY.Average();
        float meanVoltX = voltX.Average();
        float meanVoltY = voltY.Average();

        // Compute slopes via least‐squares
        float covX = 0f, varVX = 0f, covY = 0f, varVY = 0f;
        for (int i = 0; i < eyeVoltages.Count; i++)
        {
            covX += (voltX[i] - meanVoltX) * (pixX[i] - meanPixX);
            varVX += (voltX[i] - meanVoltX) * (voltX[i] - meanVoltX);
            covY += (voltY[i] - meanVoltY) * (pixY[i] - meanPixY);
            varVY += (voltY[i] - meanVoltY) * (voltY[i] - meanVoltY);
        }
        float slopeX = covX / varVX;
        float slopeY = covY / varVY;

        // Build our Map JSON
        var map = new Map()
        {
            Xscale = slopeX,
            Xscalecenter = meanVoltX,
            Yscale = slopeY,
            Yscalecenter = meanVoltY,
            pixdeg = new float[] { pixPerDegX, pixPerDegY }
        };

        // Write out to Resources/Calibrations
        string folder = Path.Combine(Application.dataPath, "Resources", "Calibrations");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string session = SessionManager.Instance.SessionName;
        string fileName = $"{timestamp}_{session}_map.json";
        string path = Path.Combine(folder, fileName);

        File.WriteAllText(path, JsonUtility.ToJson(map, true));
        Debug.Log($"[!] Calibration saved → {path}");
    }

    void OnDisable()
    {
        if (task != IntPtr.Zero)
        {
            DAQmxStopTask(task);
            DAQmxClearTask(task);
            task = IntPtr.Zero;
            Debug.Log("[!] DAQ task stopped on disable.");
        }
    }

    void Update()
    {
        // Live debug of raw voltages
        Vector2 v = ReadVoltages();
        Debug.Log($"Raw voltages: X={v.x:F3}, Y={v.y:F3}");
    }

    void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"[!] NI-DAQmx Error [{ctx}]: {code}");
    }
}
