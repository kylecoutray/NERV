using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;

public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

    private StreamWriter _writer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Prepare log file with safe timestamp (no colons or dots)
            string fileTs = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logsDir = Path.Combine(Application.dataPath, "Logs");
            Directory.CreateDirectory(logsDir);
            string path = Path.Combine(logsDir, $"Session_{fileTs}.csv");

            _writer = new StreamWriter(path, false);
            _writer.WriteLine("Time,TrialID,Event,Details");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Logs an event with optional details.
    /// </summary>
    public void LogEvent(string eventType, string trialID, string details = "")
    {
        if (_writer == null) return;

        // Match TTL timestamp convention
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        _writer.WriteLine($"{time},{trialID},{eventType},{details}");
        _writer.Flush();

        // Mirror to console
        Debug.Log($"{time},{trialID},{eventType},{details}");
    }

    void OnApplicationQuit()
    {
        _writer?.Close();
    }
}
