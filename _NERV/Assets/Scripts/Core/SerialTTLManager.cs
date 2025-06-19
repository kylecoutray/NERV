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

    private string logFilePath;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        

        // Prepare CSV
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")); 
        string logsDir = Path.Combine(projectRoot, "TTL_Logs");
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
        LogEvent(label, -1); // -1 signals: no byte to send
    }

    public void LogEvent(string label, int ttlCode)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string status;
        string byteSent = "No Byte Sent";

        if (ttlCode >= 1 && ttlCode <= 8)
        {
            byte val = (label == "StartEndBlock") ? (byte)255 : (byte)(1 << (ttlCode - 1));
            if (testMode)
            {
                status = "TEST_ONLY";
            }
            else if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.Write(new byte[] { val }, 0, 1);
                    status = "SENT";
                    byteSent = val.ToString();
                }
                catch
                {
                    status = "FAILED";
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

        string line = $"{timestamp},{status},{label},{byteSent}";

        if (status == "FAILED") Debug.LogWarning(line);
        else Debug.Log($"{line} [SerialTTL]");

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
