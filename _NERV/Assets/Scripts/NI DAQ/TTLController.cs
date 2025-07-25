// TTLController.cs
using System;
using UnityEngine;
using static NativeDAQmx;

public class TTLController : MonoBehaviour
{
    [Header("NI-DAQ Settings")]
    public string deviceName = "Dev1";   // e.g. "Dev1"
    
    private IntPtr doTask = IntPtr.Zero;

    void Start()
    {
        SetupDOTask();
    }

    // This will be invoked by your TrialManager's BroadcastMessage:
    void OnLogEvent(string stateName)
    {
        if (stateName == "StartEndBlock")
        {
            // Session on: port0/line1 high
            WriteLines(new bool[]{ false, true, false, false, false, false, false, false });
        }
        else if (stateName == "TrialOn")
        {
            // Trial on: port0/line1 + port0/line6 high
            WriteLines(new bool[]{ false, true, false, false, false, false, true, false });
        }
    }

    private void SetupDOTask()
    {
        int err = DAQmxCreateTask("", out doTask);
        CheckError(err, "CreateTask");
        // Lines: port0/line0–6 and port1/line0 => 8 bits total
        string lines = $"{deviceName}/port0/line0:6,{deviceName}/port1/line0";
        err = DAQmxCreateDOChan(doTask, lines, "", DAQmx_Val_ChanPerLine);
        CheckError(err, "CreateDOChan");
        err = DAQmxStartTask(doTask);
        CheckError(err, "StartTask");
    }

    private void WriteLines(bool[] data)
    {
        if (doTask == IntPtr.Zero) return;
        int written;
        int err = DAQmxWriteDigitalLines(
            doTask,
            1,                 // one sample
            true,              // autoStart
            1.0,               // timeout (s)
            DAQmx_Val_GroupByChannel,
            data,
            out written,
            IntPtr.Zero);
        CheckError(err, "WriteDigitalLines");
    }

    void OnDestroy()
    {
        if (doTask != IntPtr.Zero)
        {
            DAQmxStopTask(doTask);
            DAQmxClearTask(doTask);
            doTask = IntPtr.Zero;
        }
    }

    private void CheckError(int code, string ctx)
    {
        if (code != 0)
            Debug.LogError($"NI-DAQmx Error [{ctx}]: {code}");
    }
}
