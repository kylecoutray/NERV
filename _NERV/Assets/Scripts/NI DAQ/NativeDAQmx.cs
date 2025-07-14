using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

public static class NativeDAQmx
{
    // Terminal configuration and units
    public const int DAQmx_Val_RSE = 10083; // Referenced Single-Ended
    public const int DAQmx_Val_Volts = 10348;
    public const int DAQmx_Val_GroupByChannel = 0;

    // for DAQmxCreateDOChan:
    public const int DAQmx_Val_ChanPerLine = 1;    // one channel per line

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
        
    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxCreateDOChan(
        IntPtr taskHandle,
        string lines,                 // e.g. "Dev1/port0/line0"
        string nameToAssignToLines,
        int lineGrouping);            // e.g. DAQmx_Val_ChanPerLine

    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    public static extern int DAQmxWriteDigitalLines(
        IntPtr taskHandle,
        int numSampsPerChan,          // usually 1
        bool autoStart,               // you can auto-start on first write
        double timeout,               // seconds
        int dataLayout,               // DAQmx_Val_GroupByChannel
        [In] bool[] writeArray,       // values to write
        out int sampsPerChanWritten,
        IntPtr reserved);
}
