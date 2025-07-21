using UnityEngine;
using System;
using System.IO;
using System.Linq;
using static NativeDAQmx;

public class CalibrationTester : MonoBehaviour
{
    [Header("Gaze Cursor")]
    public RectTransform testCursor;

    [Header("Device Settings")]
    public string deviceName = "Dev1"; // or "SimDev1"

    [Header("Simulation")]
    public bool simulateDAQ = true;
    public bool simulateByCursor = false; 
    public float simMinVoltage = -5f;
    public float simMaxVoltage = 5f;

    // Affine mapping coefficients (filled in Start)
    private float aX, bX, aY, bY;

    // Single DAQ task handle for ai0 & ai1
    private IntPtr task = IntPtr.Zero;

    void Start()
    {
        // 1) Load latest map
        string folder = Path.Combine(Application.dataPath, "Resources", "Calibrations");
        if (!Directory.Exists(folder))
        {
            Debug.LogError($"Calibration folder not found at {folder}");
            enabled = false;
            return;
        }

        var files = Directory.GetFiles(folder, "*_map.json");
        if (files.Length == 0)
        {
            Debug.LogError("No calibration maps found");
            enabled = false;
            return;
        }

        string latest = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        Map map = JsonUtility.FromJson<Map>(File.ReadAllText(latest));

        // pixels per degree average
        float pd = (map.pixdeg[0] + map.pixdeg[1]) * 0.5f;

        // create screen = a*volt + b
        aX = map.Xscale * pd;
        bX = Screen.width  * 0.5f - map.Xscalecenter * map.Xscale * pd;
        aY = map.Yscale * pd;
        bY = Screen.height * 0.5f - map.Yscalecenter * map.Yscale * pd;

        Debug.Log($"Loaded '{Path.GetFileName(latest)}' → aX={aX:F3}, bX={bX:F1}, aY={aY:F3}, bY={bY:F1}");

        // 2) DAQ setup if not simulating
        if (!simulateDAQ)
        {
            int err = DAQmxCreateTask("", out task);
            CheckError(err, "CreateTask");

            string chans = $"{deviceName}/ai0:1";
            err = DAQmxCreateAIVoltageChan(
                task, chans, "",
                DAQmx_Val_RSE,
                simMinVoltage, simMaxVoltage,
                DAQmx_Val_Volts,
                null);
            CheckError(err, "CreateAIVoltageChan");

            err = DAQmxStartTask(task);
            CheckError(err, "StartTask");
        }
        else
        {
            Debug.Log("[CalibrationTester] simulateDAQ ON: skipping NI-DAQmx setup");
        }
    }

    void Update()
    {
        Vector2 volts;

        if (simulateDAQ)
        {
            if (simulateByCursor)
            {
                // invert the affine: volt = (pixel - b)/a
                Vector2 mp = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                volts.x = (mp.x - bX) / aX;
                volts.y = (mp.y - bY) / aY;
            }
            else
            {
                // fixed mid-voltage
                float mid = (simMinVoltage + simMaxVoltage) * 0.5f;
                volts = new Vector2(mid, mid);
            }
        }
        else
        {
            // real DAQ read
            double[] data = new double[2];
            int sampsRead;
            int err = DAQmxReadAnalogF64(
                task,
                1,
                1.0,
                DAQmx_Val_GroupByChannel,
                data,
                (uint)data.Length,
                out sampsRead,
                IntPtr.Zero);
            CheckError(err, "ReadAI0&1");
            volts = new Vector2((float)data[0], (float)data[1]);
        }

        // apply affine mapping: screenPx = a*volt + b
        float px = aX * volts.x + bX;
        float py = aY * volts.y + bY;

        // set cursor (anchoredPosition origin at canvas center)
        testCursor.anchoredPosition = new Vector2(
            px - Screen.width  * 0.5f,
            py - Screen.height * 0.5f
        );
    }

    void OnDestroy()
    {
        if (task != IntPtr.Zero)
            DAQmxClearTask(task);
    }

    void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"NI-DAQmx Error [{ctx}]: {code}");
    }
}
