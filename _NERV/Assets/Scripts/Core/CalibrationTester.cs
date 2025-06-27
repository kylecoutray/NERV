using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class CalibrationTester : MonoBehaviour
{
    [Header("Gaze Cursor")]
    public RectTransform testCursor;

    [Header("Device Settings")]
    public string deviceName = "Dev1"; // or "SimDev1" for NI-MAX

    // Affine mapping coefficients (filled in Start)
    private float aX, bX, aY, bY;

    // Single DAQ task handle for ai0 & ai1
    private IntPtr task = IntPtr.Zero;

    // DAQmx group-by-channel constant
    private const int DAQmx_Val_GroupByChannel = 0;

    void Start()
    {
        // --- 1) Load calibration map ---
        string path = Path.Combine(Application.persistentDataPath, "calib.json");
        if (!File.Exists(path))
        {
            Debug.LogError($"Calibration file not found at {path}");
            enabled = false;
            return;
        }
        var map = JsonUtility.FromJson<CalibrationMap>(File.ReadAllText(path));

        // --- 2) Compute affine fit: screen = a*volt + b ---
        int n = map.voltagePoints.Length;
        double sumVx=0, sumPx=0, sumVy=0, sumPy=0;
        for(int i=0;i<n;i++){
            sumVx += map.voltagePoints[i].x;
            sumPx += map.screenPoints[i].x;
            sumVy += map.voltagePoints[i].y;
            sumPy += map.screenPoints[i].y;
        }
        double mVx = sumVx/n, mPx = sumPx/n, mVy = sumVy/n, mPy = sumPy/n;
        double numX=0, denX=0, numY=0, denY=0;
        for(int i=0;i<n;i++){
            double dvx = map.voltagePoints[i].x - mVx;
            double dpx = map.screenPoints[i].x - mPx;
            numX += dvx*dpx; denX += dvx*dvx;
            double dvy = map.voltagePoints[i].y - mVy;
            double dpy = map.screenPoints[i].y - mPy;
            numY += dvy*dpy; denY += dvy*dvy;
        }
        aX = (float)(numX/denX);
        bX = (float)(mPx - aX*mVx);
        aY = (float)(numY/denY);
        bY = (float)(mPy - aY*mVy);
        Debug.Log($"Affine mapping: X = {aX:F3}*volt + {bX:F1}, Y = {aY:F3}*volt + {bY:F1}");

        // --- 3) Setup one DAQ task for both channels ---
        int err = NativeDAQmx.DAQmxCreateTask("", out task);
        CheckError(err, "CreateTask");
        // note: "ai0:1" means ai0 and ai1
        string chans = $"{deviceName}/ai0:1";
        err = NativeDAQmx.DAQmxCreateAIVoltageChan(
            task, chans, "",
            NativeDAQmx.DAQmx_Val_RSE,
            -5.0, 5.0,
            NativeDAQmx.DAQmx_Val_Volts,
            null);
        CheckError(err, "CreateAIVoltageChan");
        err = NativeDAQmx.DAQmxStartTask(task);
        CheckError(err, "StartTask");
    }

    void Update()
    {
        if (task == IntPtr.Zero) return;

        // --- 4) Read both channels at once ---
        double[] data = new double[2];
        int sampsRead;
        int err = NativeDAQmx.DAQmxReadAnalogF64(
            task,
            1,                    // one sample per channel
            1.0,                  // timeout
            DAQmx_Val_GroupByChannel,
            data,
            (uint)data.Length,
            out sampsRead,
            IntPtr.Zero);
        CheckError(err, "ReadAI0&1");

        float vx = (float)data[0];
        float vy = (float)data[1];

        // --- 5) Map voltages → screen and move cursor ---
        float px = aX * vx + bX;
        float py = aY * vy + bY;
        testCursor.anchoredPosition = new Vector2(px, py);
    }

    void OnDestroy()
    {
        // --- 6) Clean up ---
        if (task != IntPtr.Zero)
            NativeDAQmx.DAQmxClearTask(task);
    }

    void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"NI-DAQmx Error [{ctx}]: {code}");
    }

    [Serializable]
    class CalibrationMap
    {
        public Vector2[] screenPoints;
        public Vector2[] voltagePoints;
    }
}

