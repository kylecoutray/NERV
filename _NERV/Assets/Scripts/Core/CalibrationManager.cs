using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

public class CalibrationManager : MonoBehaviour
{
    [Header("Device Settings")]
    public string deviceName = "Dev1";             // NI device name (e.g., "Dev1" or "SimDev1")

    [Header("UI Calibration Points")]  
    public List<RectTransform> calibrationPoints;

    [Header("Simulation")]
    public bool simulateDAQ = true;
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

    private List<Vector2> eyeVoltages = new List<Vector2>();
    private IntPtr task = IntPtr.Zero;

    private float minScreenX, maxScreenX, minScreenY, maxScreenY;

    void Start()
    {
        // If Unity crashed or you stopped Play Mode early, task may still be open
        if (task != IntPtr.Zero)
        {
            NativeDAQmx.DAQmxStopTask(task);
            NativeDAQmx.DAQmxClearTask(task);
            task = IntPtr.Zero;
            Debug.Log("Cleared previous DAQ task.");
        }


        if (calibrationPoints == null || calibrationPoints.Count == 0)
        {
            Debug.LogError("No calibration points assigned!");
            return;
        }
        // Compute extents for simulation mapping
        minScreenX = calibrationPoints.Min(r => r.anchoredPosition.x);
        maxScreenX = calibrationPoints.Max(r => r.anchoredPosition.x);
        minScreenY = calibrationPoints.Min(r => r.anchoredPosition.y);
        maxScreenY = calibrationPoints.Max(r => r.anchoredPosition.y);

        if (!simulateDAQ)
            SetupTasks();
        else
            Debug.Log("Simulation mode ON: no hardware tasks started.");

        StartCoroutine(RunCalibration());
    }

    // P/Invoke for multi-channel read
    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxReadAnalogF64(
        IntPtr taskHandle,
        int numSampsPerChan,
        double timeout,
        int fillMode,
        [Out] double[] readArray,
        uint arraySizeInSamps,
        out int sampsPerChanRead,
        IntPtr reserved);

    public const int DAQmx_Val_GroupByChannel = 0;

    private Vector2 ReadVoltages()
    {
        if (simulateDAQ)
        {
            float mid = (simMinVoltage + simMaxVoltage) * 0.5f;
            return new Vector2(mid, mid);
        }
        double[] data = new double[2];
        int sampsRead;
        // Read one sample per channel
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
        Debug.Log($"Initializing DAQ task on device {deviceName}");

        // 1) Create a single task
        int err = NativeDAQmx.DAQmxCreateTask(string.Empty, out task);
        CheckError(err, "CreateTask");

        // 2) Add both AI channels in one call with a comma list
        //    e.g. "SimDev1/ai0,SimDev1/ai1" or "Dev1/ai0,Dev1/ai1"
        string chanList = $"{deviceName}/ai0,{deviceName}/ai1";
        err = NativeDAQmx.DAQmxCreateAIVoltageChan(
            task,
            chanList,
            string.Empty,
            NativeDAQmx.DAQmx_Val_RSE,
            -5.0,
            5.0,
            NativeDAQmx.DAQmx_Val_Volts,
            null);
        CheckError(err, "CreateAIVoltageChan");

        // 3) Start the task once
        err = NativeDAQmx.DAQmxStartTask(task);
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

                // Fixation detection
                int bufSize = Mathf.CeilToInt(holdDuration / holdSampleInterval);
                Queue<float> vxBuf = new Queue<float>(bufSize);
                Queue<float> vyBuf = new Queue<float>(bufSize);
                float holdTimer = 0f;
                float elapsed = 0f;
                while (holdTimer < holdDuration && elapsed < maxWaitForFixation)
                {
                    Vector2 v = ReadVoltages();
                    vxBuf.Enqueue(v.x); vyBuf.Enqueue(v.y);
                    if (vxBuf.Count > bufSize) { vxBuf.Dequeue(); vyBuf.Dequeue(); }

                    float stdVx = ComputeStd(vxBuf);
                    float stdVy = ComputeStd(vyBuf);
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
                    Debug.LogWarning($"Dot {i}: fixation not stable after {elapsed:F1}s, retrying ({attempt}/{maxRetries})");
                    pt.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }
                Debug.Log($"Dot {i}: fixation confirmed after {elapsed:F1}s");

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
                Debug.Log($"Point {i}, attempt {attempt+1}: mean=({meanVx:F3},{meanVy:F3}), std=({stdSVx:F3},{stdSVy:F3})");

                if (stdSVx <= maxStdDev && stdSVy <= maxStdDev)
                {
                    eyeVoltages.Add(new Vector2(meanVx, meanVy));
                    success = true;
                }
                else
                {
                    attempt++;
                    Debug.LogWarning($"Dot {i}: high variance, retrying ({attempt}/{maxRetries})");
                }
            }

            pt.gameObject.SetActive(false);
            if (!success)
            {
                Debug.LogError($"Calibration failed at point {i}.");
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (!simulateDAQ)
            CleanupTasks();
        SaveMapping();
    }

    private float ComputeStd(IEnumerable<float> data)
    {
        float mean = data.Average();
        return Mathf.Sqrt(data.Sum(v => (v - mean)*(v - mean)) / data.Count());
    }

    void CleanupTasks()
    {
        if (task != IntPtr.Zero)
        {
            NativeDAQmx.DAQmxClearTask(task);
            task = IntPtr.Zero;
        }
    }

    void SaveMapping()
    {
        var map = new CalibrationMap()
        {
            screenPoints = calibrationPoints.ConvertAll(r => r.anchoredPosition),
            voltagePoints = eyeVoltages
        };
        string path = Path.Combine(Application.persistentDataPath, "calib.json");
        File.WriteAllText(path, JsonUtility.ToJson(map, true));
        Debug.Log($"Calibration saved → {path}");
    }

    void OnDisable()
    {
        if (task != IntPtr.Zero)
        {
            NativeDAQmx.DAQmxStopTask(task);
            NativeDAQmx.DAQmxClearTask(task);
            task = IntPtr.Zero;
            Debug.Log("DAQ task stopped on disable.");
        }
    }

    void Update()
    {
        Vector2 v = ReadVoltages();
        Debug.Log($"Raw voltages: X={v.x:F3}, Y={v.y:F3}");

    }

    void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"NI-DAQmx Error [{ctx}]: {code}");
    }

    [Serializable]
    class CalibrationMap { public List<Vector2> screenPoints; public List<Vector2> voltagePoints; }
}

public static class NativeDAQmx
{
    // Terminal configuration and units
    public const int DAQmx_Val_RSE               = 10083; // Referenced Single-Ended
    public const int DAQmx_Val_Volts             = 10348;
    public const int DAQmx_Val_GroupByChannel    = 0;

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxCreateTask(
        string taskName,
        out IntPtr taskHandle);

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxCreateAIVoltageChan(
        IntPtr taskHandle,
        string physicalChannel,
        string nameToAssignToChannel,
        int terminalConfig,
        double minVal,
        double maxVal,
        int units,
        string customScaleName);

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxStartTask(
        IntPtr taskHandle);

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxStopTask(
        IntPtr taskHandle);

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxClearTask(
        IntPtr taskHandle);

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxReadAnalogF64(
        IntPtr taskHandle,
        int numSampsPerChan,
        double timeout,
        int fillMode,
        [Out] double[] readArray,
        uint arraySizeInSamps,
        out int sampsPerChanRead,
        IntPtr reserved);
}
