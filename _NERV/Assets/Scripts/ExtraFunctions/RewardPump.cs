using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RewardPump : MonoBehaviour
{
    [Header("DAQ Settings")]
    public string pumpChannel = "Dev1/port0/line0";  // match your hardware
    public int numBursts       = 1;                 // MATLAB numBurst
    public float highTime      = 0.45f;             // seconds
    public float lowTime       = 0.30f;             // seconds

    [Header("Which states trigger reward?")]
    public List<string> rewardStates = new List<string> { "Success" };

    [Header("Test Mode (no pump connected)")]
    public bool testMode = false;

    private IntPtr pumpTask = IntPtr.Zero;

    void Awake()
    {
        if (!testMode)
        {
            // 1) Create & start DO task
            if (NativeDAQmx.DAQmxCreateTask("", out pumpTask) != 0)
                Debug.LogError("Failed to create DAQ task");
            if (NativeDAQmx.DAQmxCreateDOChan(pumpTask, pumpChannel, "",
                    NativeDAQmx.DAQmx_Val_ChanPerLine) != 0)
                Debug.LogError("Failed to create DO channel");
            if (NativeDAQmx.DAQmxStartTask(pumpTask) != 0)
                Debug.LogError("Failed to start DO task");
        }
    }

    // Call this from your existing OnLogEvent hook:
    public void OnLogEvent(string stateName)
    {
        if (rewardStates.Contains(stateName))
            StartCoroutine(DeliverReward());
    }

    IEnumerator DeliverReward()
    {
        bool[] high  = { true };
        bool[] low   = { false };
        int written;

        for (int i = 0; i < numBursts; i++)
        {
            if (testMode)
            {
                Debug.Log("Test Mode: Reward pump ON");
            }
            else
            {
                NativeDAQmx.DAQmxWriteDigitalLines(
                    pumpTask, 1, true,  1.0,
                    NativeDAQmx.DAQmx_Val_GroupByChannel,
                    high, out written, IntPtr.Zero);
            }
            yield return new WaitForSeconds(highTime);

            if (testMode)
            {
                Debug.Log("Test Mode: Reward pump OFF");
            }
            else
            {
                NativeDAQmx.DAQmxWriteDigitalLines(
                    pumpTask, 1, true,  1.0,
                    NativeDAQmx.DAQmx_Val_GroupByChannel,
                    low,  out written, IntPtr.Zero);
            }
            yield return new WaitForSeconds(lowTime);
        }
    }

    void OnDestroy()
    {
        if (!testMode && pumpTask != IntPtr.Zero)
        {
            NativeDAQmx.DAQmxStopTask(pumpTask);
            NativeDAQmx.DAQmxClearTask(pumpTask);
        }
    }
}
