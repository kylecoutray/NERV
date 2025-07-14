// GazeCursorController.cs
using UnityEngine;
using System;
using static NativeDAQmx;

// You would have to edit this to work with your specific 
// eye tracking DAQ device if you want to move around the
// GazeCursorController too -- Otherwise if you just want
// eye tracking, edit the if statement in line 123 of 
// DwellClick.cs shown below:

//Vector2 screenPos = simulateWithMouse || gazeCursor == null
//            ? (Vector2)Input.mousePosition
//            : (Vector2)gazeCursor.position; 

// This script above ^^^



public class GazeCursorController : MonoBehaviour
{
    [Header("Cursor & Canvas")]
    public RectTransform gazeCursor;      // will auto-load if null
    public Canvas targetCanvas;     // will try FindObjectOfType if null

    [Header("DAQ Settings")]
    public string deviceName = "Dev1";    // your NI device
    public bool simulateDAQ = false;    // for testing without hardware

    // Internal DAQ task handle
    private IntPtr aiTask = IntPtr.Zero;

    void Awake()
    {
        // Auto-instantiate GazeCursor prefab if none assigned
        if (gazeCursor == null)
        {
            var prefabGO = Resources.Load<GameObject>("Prefabs/GazeCursor");
            if (prefabGO != null)
            {
                var prefabRT = prefabGO.GetComponent<RectTransform>();
                if (prefabRT != null)
                {
                    if (targetCanvas == null)
                        targetCanvas = FindGazeCanvas();

                    if (targetCanvas != null)
                    {
                        var inst = Instantiate(prefabRT, targetCanvas.transform, false);
                        inst.name = "GazeCursor";
                        inst.anchorMin = new Vector2(0.5f, 0.5f);
                        inst.anchorMax = new Vector2(0.5f, 0.5f);
                        inst.anchoredPosition = Vector2.zero;
                        inst.localScale = Vector3.one;
                        inst.SetAsLastSibling();
                        gazeCursor = inst;
                    }
                    else
                    {
                        var inst = Instantiate(prefabRT);
                        inst.name = "GazeCursor";
                        gazeCursor = inst;
                    }
                }
                else
                {
                    Debug.LogError("Prefabs/GazeCursor does not have a RectTransform.");
                }
            }
            else
            {
                Debug.LogError("Could not load 'Prefabs/GazeCursor' from Resources.");
            }
        }

        // Find Canvas if not set
        if (targetCanvas == null)
        {
            targetCanvas = FindGazeCanvas();
            if (targetCanvas == null)
                Debug.LogError("No Canvas found in scene for GazeCursorController.");
        }
    }

    void Start()
    {
        // Setup AI task if not simulating
        if (!simulateDAQ)
        {
            int err = DAQmxCreateTask("", out aiTask);
            CheckError(err, "CreateTask");
            string chans = $"{deviceName}/ai0,{deviceName}/ai1";
            err = DAQmxCreateAIVoltageChan(
                aiTask, chans, "",
                DAQmx_Val_RSE, -5.0, 5.0,
                DAQmx_Val_Volts, null);
            CheckError(err, "CreateAIVoltageChan");
            err = DAQmxStartTask(aiTask);
            CheckError(err, "StartTask");
        }
    }

    void Update()
    {
        // 1) Read raw voltages
        Vector2 volts;
        if (simulateDAQ)
        {
            float x = UnityEngine.Random.Range(-5f, 5f);
            float y = UnityEngine.Random.Range(-5f, 5f);
            volts = new Vector2(x, y);
        }
        else
        {
            volts = ReadVoltages();
        }

        // 2) Map volts -> pixels
        Vector2 pix = CalibrationLoader.Instance.VoltToPixel(volts);

        // 3) Convert pixel coords (bottom-left origin) -> anchoredPosition
        float canvasW = targetCanvas.pixelRect.width;
        float canvasH = targetCanvas.pixelRect.height;
        Vector2 anchored = new Vector2(
            pix.x - (canvasW * 0.5f),
            pix.y - (canvasH * 0.5f)
        );

        // 4) Move your cursor
        if (gazeCursor != null)
            gazeCursor.anchoredPosition = anchored;
    }

    private Vector2 ReadVoltages()
    {
        double[] data = new double[2];
        int samps;
        int err = DAQmxReadAnalogF64(
            aiTask, 1, 1.0, DAQmx_Val_GroupByChannel,
            data, (uint)data.Length, out samps, IntPtr.Zero);
        CheckError(err, "ReadAnalog");
        return new Vector2((float)data[0], (float)data[1]);
    }

    void OnDestroy()
    {
        if (aiTask != IntPtr.Zero)
        {
            DAQmxStopTask(aiTask);
            DAQmxClearTask(aiTask);
        }
    }

    void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"NI-DAQ Error [{ctx}]: {code}");
    }
    private Canvas FindGazeCanvas()
    {
        var go = GameObject.Find("GazeCanvas");
        if (go != null)
        {
            Debug.Log($"Found GazeCanvas: {go.name}");
            return go.GetComponent<Canvas>();
        }
        Debug.Log("GazeCanvas not found by name, searching for any Canvas in scene.");
        return FindObjectOfType<Canvas>();
    }

}
