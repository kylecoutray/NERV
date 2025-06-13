using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;

public class SerialTTLManager : MonoBehaviour
{
    public string portName = "COM3";
    public int    baudRate = 115200;
    public bool   testMode = false;

    private SerialPort serialPort;
    public static SerialTTLManager Instance { get; private set; }

    // Only these events will actually send a byte over serial
    private static readonly HashSet<string> ttlEnabledEvents = new HashSet<string>
    {
        "TrialOn","SampleOn","SampleOff",
        "DistractorOn","TargetOn","Choice","StartEndBlock"
    };

    // Map labels → 1-indexed TTL codes
    private static readonly Dictionary<string,int> eventCodes = new Dictionary<string,int>
    {
        { "TrialOn",       1 },
        { "SampleOn",      2 },
        { "SampleOff",     3 },
        { "DistractorOn",  4 },
        { "TargetOn",      5 },
        { "DistractorOff", 6 },
        { "Choice",        7 },
        { "StartEndBlock", 8 }
    };

    private string logFilePath;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Prepare CSV
        string logsDir = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logsDir);
        logFilePath = Path.Combine(logsDir,
            $"TTL_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        // Write exactly the header row you showed
        File.WriteAllText(logFilePath,
            "Timestamp,Status,Event,ByteSent\n");

        // Open serial port unless in test mode
        if (!testMode)
        {
            serialPort = new SerialPort(portName, baudRate);
            try { serialPort.Open(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerialTTL] Failed opening {portName}: {e}");
            }
        }
    }

    /// <summary>
    /// Call this on *every* event.  Whitelisted ones get a TTL pulse;
    /// all of them get logged to CSV and the console.
    /// </summary>
    public void LogEvent(string label)
    {
        // Legacy timestamp format
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string status, byteSent = "";

        // Decide whether to send and what to record
        if (ttlEnabledEvents.Contains(label))
        {
            if (testMode)
            {
                status = "TEST_ONLY";
            }
            else if (serialPort != null && serialPort.IsOpen)
            {
                int code = eventCodes[label];
                byte val = (label=="StartEndBlock")? (byte)255 : (byte)(1 << (code-1));
                try
                {
                    serialPort.Write(new byte[]{ val }, 0, 1);
                    status   = "SENT";
                    byteSent = val.ToString();
                }
                catch
                {
                    status   = "FAILED";
                }
            }
            else
            {
                status = "FAILED";
            }
        }
        else
        {
            status = "LOG_ONLY";
        }

        // Build the CSV line: Timestamp,Status,Event,ByteSent
        if (string.IsNullOrEmpty(byteSent))
            byteSent = "No Byte Sent";
        string line = $"{timestamp},{status},{label},{byteSent}";

        // Mirror to console exactly
        if (status=="FAILED")
            Debug.LogWarning(line);
        else
            Debug.Log(line);

        // And append to CSV
        try
        {
            File.AppendAllText(logFilePath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SerialTTL] Could not write log: {e}");
        }
    }

    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
}
