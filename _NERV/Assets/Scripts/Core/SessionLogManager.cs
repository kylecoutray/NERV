using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Reflection;
using System.Linq;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System.Diagnostics;

using Debug = UnityEngine.Debug;



public class SessionLogManager : MonoBehaviour
{
    public static SessionLogManager Instance { get; private set; }

    // start a global stopwatch at launch
    private static readonly Stopwatch sw = Stopwatch.StartNew();
    StreamWriter allWriter;
    StreamWriter ttlWriter;
    string taskFolder;
    string acr;

    // --- serial TTL fields ---
    [Header("Serial TTL Settings (in-session pulses)")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public bool testMode = false;
    private SerialPort serialPort;

    [Header("Screenshot Settings")]
    [Tooltip("How many trials’ first‐pass of each state to capture")]
    public int trialsToCapture = 2;

    [Tooltip("Frames to wait after render before capture")]
    public int screenshotFrameDelay = 1;

    [Header("Async Screenshot Settings")]
    [Tooltip("Width/height for low-res capture")]
    public int screenshotWidth = 256;
    public int screenshotHeight = 144;

    [Tooltip("Disable all async screenshots when false")]
    public bool enableScreenshots = true;
    [Range(10, 100), Tooltip("JPEG quality (10–100)")]
    public int jpgQuality = 50;

    // pooled buffers
    private RenderTexture _screenshotRT;
    private Texture2D _screenshotTex;


    // track how many times each state has appeared
    private Dictionary<string, int> _stateCaptureCounts = new Dictionary<string, int>();

    // —————————————————————————————————————————————
    // Session-manifest state
    private long _sessionStartEpoch;
    private string _sessionStartHuman;
    private bool _manifestWritten = false;

    // holds the active TrialManager instance and its acronym
    private object _currentTrialManager = null;
    private string _currentTaskAcronym = null;


    public void InitializeSerialPort()
    {
        if (!testMode)
        {
            serialPort = new SerialPort(portName, baudRate);
            try { serialPort.Open(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SessionLogManager] Failed opening TTL port {portName}: {e}");
            }
        }
    }

    void Awake()
    {
        // Singleton guard

        if (Instance != null)
        {
            Debug.Log($"[SessionLogManager] Destroying duplicate from scene {gameObject.scene.name}");
            Destroy(gameObject);
            return;
        }
        Debug.Log($"[SessionLogManager] Initialized and persisting from scene {gameObject.scene.name}");


        Instance = this;

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // record session start
        _sessionStartEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _sessionStartHuman = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");


        // Wait for any trial scene to load
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // locate TrialManagerXxx in scene
        var tm = FindObjectsOfType<MonoBehaviour>()
                    .FirstOrDefault(m => m.GetType().Name.StartsWith("TrialManager"));

        if (tm == null)
            return; // not our trial scene

        // register our TrialProgressDisplay with our TrialManager
        var ui = FindObjectOfType<TrialProgressDisplay>();
        if (ui != null)
            ui.RegisterTrialManager(tm, acr);

        // close any prior logs if they exist
        if (allWriter != null)
        {
            allWriter.Close();
            allWriter = null;
        }
        if (ttlWriter != null)
        {
            ttlWriter.Close();
            ttlWriter = null;
        }

        // extract acronym
        acr = tm.GetType().Name.Substring("TrialManager".Length);

        // grab SessionManager
        var sess = SessionManager.Instance;
        if (sess == null || string.IsNullOrEmpty(sess.SessionFolder))
        {
            Debug.LogError("SessionLogManager: SessionManager missing or SessionFolder empty → disabled");
            enabled = false;
            return;
        }

        // emit MANIFEST.json in your session root
        WriteManifest(sess.SessionFolder);

        // make per-task folder
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        taskFolder = Path.Combine(sess.SessionFolder, $"{acr}_{ts}");
        Directory.CreateDirectory(taskFolder);

        if (enableScreenshots)
        {
            _screenshotRT = new RenderTexture(screenshotWidth, screenshotHeight, 0, RenderTextureFormat.ARGB32);
            _screenshotRT.Create();
            // match the 4-channel RT
            _screenshotTex = new Texture2D(screenshotWidth, screenshotHeight, TextureFormat.RGBA32, false);
        }

        // 1) HEADER file
        var configFields = tm.GetType()
                         .GetFields(BindingFlags.Public | BindingFlags.Instance)
                         .Where(f => f.FieldType == typeof(float)
                                  || f.FieldType == typeof(int)
                                  || f.FieldType == typeof(bool));
        string headerPath = Path.Combine(taskFolder, "LOGS_HEADER.txt");
        using (var hw = new StreamWriter(headerPath, false))
        {
            hw.WriteLine($"# Run parameters for {tm.GetType().Name}");
            foreach (var f in configFields)
                hw.WriteLine($"{f.Name}={f.GetValue(tm)}");
        }

        // 2) open CSVs (allow file‐share so we don’t collide)
        var allPath = Path.Combine(taskFolder, "ALL_LOGS.csv");
        var ttlPath = Path.Combine(taskFolder, "TTL_LOGS.csv");
        allWriter = new StreamWriter(new FileStream(
            allPath,
            FileMode.Create,          // overwrite or create
            FileAccess.Write,
            FileShare.ReadWrite       // allow sharing
        ));
        ttlWriter = new StreamWriter(new FileStream(
            ttlPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite
        ));

        // 3) write column headers
        allWriter.WriteLine("Frame,UnityTime,StopwatchTime,TrialID,Event,Details");
        ttlWriter.WriteLine("Frame,UnityTime,StopwatchTime,TrialID,Event,Code");

        allWriter.Flush();
        ttlWriter.Flush();

        // Archive TrialManagers, as well as the .csv definitions. 
        string sourceRoot = Application.isEditor
            ? Application.dataPath                           // “…/YourProject/Assets”
            : Application.streamingAssetsPath;               // “…/MyGame_Data/StreamingAssets”

        // 1) TrialManager code
        string codeSrc = Path.Combine(
            sourceRoot,
            "Scripts", "Tasks", acr,
            $"TrialManager{acr}.cs"
        );

        // 2) Trial‐Def CSV
        string defCsvSrc = Path.Combine(
            sourceRoot,
            Application.isEditor ? "Resources/Configs" : "Configs",
            acr,
            $"{acr}_Trial_Def.csv"
        );

        // 3) Stim-Index CSV
        string stimCsvSrc = Path.Combine(
            sourceRoot,
            Application.isEditor ? "Resources/Configs" : "Configs",
            acr,
            $"{acr}_Stim_Index.csv"
        );
        Copy(codeSrc, Path.Combine(taskFolder, $"TrialManager{acr}_CODE.txt"));
        Copy(defCsvSrc, Path.Combine(taskFolder, $"{acr}_Trial_Def.csv"));
        Copy(stimCsvSrc, Path.Combine(taskFolder, $"{acr}_Stim_Index.csv"));

        // ---- append file hashes to manifest ----
        var manifestPath = Path.Combine(sess.SessionFolder, "MANIFEST.json");
        if (File.Exists(manifestPath))
        {
            // read and deserialize
            string j = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<SessionManifest>(j);

            // list of the just-copied files
            var files = new string[]
            {
                Path.Combine(taskFolder, $"TrialManager{acr}_CODE.txt"),
                Path.Combine(taskFolder, $"{acr}_Trial_Def.csv"),
                Path.Combine(taskFolder, $"{acr}_Stim_Index.csv")
            };

            foreach (var f in files)
            {
                if (File.Exists(f))
                {
                    manifest.fileHashes.Add(new FileHash
                    {
                        fileName = Path.GetFileName(f),
                        hash = ComputeSHA256(f)
                    });
                }
            }

            // write back updated manifest
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
        }


        // clear old hash set for screenshots
        _stateCaptureCounts.Clear();
    }

    void Copy(string src, string dst)
    {
        if (File.Exists(src))
            File.Copy(src, dst, true);
    }

    /// <summary>
    /// Full‐detail logging.
    /// </summary>
    public void LogAll(string trialID, string evt, string details)
    {
        if (allWriter == null) return;

        int frame = UnityEngine.Time.frameCount;
        float unityTime = UnityEngine.Time.realtimeSinceStartup;
        double stopwatchTime = sw.Elapsed.TotalSeconds;

        // CSV: Frame, UnityTime(s), StopwatchTime(s), TrialID, Event, Details
        allWriter.WriteLine(
            $"{frame},{unityTime:F4},{stopwatchTime:F6}," +
            $"{trialID},{evt},{details}"
        );
        allWriter.Flush();

        Debug.Log(
            $"<b>[ALL_LOGS]</b> TrialID: {trialID}, Event: {evt}, Unity Time: {unityTime:F4}s, Stopwatch Time: {stopwatchTime:F6}s, " +
            $"Frame: {frame}, {details}"
        );

    }

    /// <summary>
    /// Send a TTL pulse if code in 1–8, and also log to TTL_CSV.
    /// </summary>
    public void LogTTL(string trialID, string evt, int ttlCode)
    {
        if (ttlWriter == null) return;

        int frame = UnityEngine.Time.frameCount;
        float unityTime = UnityEngine.Time.realtimeSinceStartup;
        double stopwatchTime = sw.Elapsed.TotalSeconds;

        string status, byteSent = "NoByte";

        if (ttlCode >= 1 && ttlCode <= 8)
        {
            byte val = (evt == "StartEndBlock") ? (byte)255 : (byte)(1 << (ttlCode - 1));
            if (testMode)
            {
                status = "TEST";
            }
            else if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.Write(new byte[] { val }, 0, 1);
                    status = "SENT"; byteSent = val.ToString();
                }
                catch
                {
                    status = "FAILED";
                }
            }
            else status = "FAILED";
        }
        else
        {
            status = "LOG_ONLY";
        }

        // write TTL CSV: Frame, UnityTime, StopwatchTime, TrialID, Event, Code
        ttlWriter.WriteLine(
            $"{frame},{unityTime:F4},{stopwatchTime:F6}," +
            $"{trialID},{evt},{ttlCode}"
        );
        ttlWriter.Flush();

        // console feedback
        string line = $"<b>[TTL]</b> Event: {evt}, Code: {ttlCode}, Byte: {byteSent},Unity Time: {unityTime:F4}s, Stopwatch Time: {stopwatchTime:F6}s, Frame: {frame}, " +
            $"{status}";

        if (status == "FAILED") Debug.LogWarning($"FAILED TTL!");
        else Debug.Log($"{line}");

        // bump the seen‐count for this state
        int seen = 0;
        _stateCaptureCounts.TryGetValue(evt, out seen);
        seen++;
        _stateCaptureCounts[evt] = seen;

        // capture only during the first N trials
        if (enableScreenshots && seen <= trialsToCapture)
            StartCoroutine(CaptureStateScreenshotCoroutine(evt));
    }


    void OnApplicationQuit()
    {
        allWriter?.Close();
        ttlWriter?.Close();
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }

    /// <summary>
    /// Immediately write one raw byte out the COM port. Used for testing the connection.
    /// </summary>
    public void SendRawByte(byte b)
    {
        if (testMode)
        {
            Debug.Log($"[SerialTTL] (test only) would have sent 0x{b:X2}");
            return;
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.Write(new byte[] { b }, 0, 1);
                Debug.Log($"[SerialTTL] Sent raw 0x{b:X2}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerialTTL] Raw send failed: {e}");
            }
        }
        else
        {
            Debug.LogWarning($"[SerialTTL] Raw send failed: port not open");
        }
    }
    private IEnumerator CaptureStateScreenshotCoroutine(string stateName)
    {
        // wait until end of frame so the backbuffer is fully rendered
        yield return new WaitForEndOfFrame();

        // optional extra delays if your spawns happen one frame later
        for (int i = 1; i < screenshotFrameDelay; i++)
            yield return new WaitForEndOfFrame();

        // capture & downscale in one GPU pass
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_screenshotRT);

        // allow one more frame for the GPU to finish the blit
        yield return new WaitForEndOfFrame();

        // now async readback as before
        AsyncGPUReadback.Request(_screenshotRT, 0, request =>
        {
            if (request.hasError)
            {
                Debug.LogWarning($"[Screenshot] GPU readback error for {stateName}");
                return;
            }

            _screenshotTex.LoadRawTextureData(request.GetData<byte>());
            _screenshotTex.Apply(false, false);
            FlipVertical(_screenshotTex);

            byte[] jpg = _screenshotTex.EncodeToJPG(jpgQuality);
            string folder = Path.Combine(taskFolder, "StatesCaptured");
            Directory.CreateDirectory(folder);
            string fileName = $"{DateTime.Now:HHmmss}_{stateName}.jpg";
            string fullPath = Path.Combine(folder, fileName);
            Task.Run(() => File.WriteAllBytes(fullPath, jpg));
            Debug.Log($"<color=yellow>[Screenshot]</color> {stateName} → {fullPath}");
        });
    }

    void FlipVertical(Texture2D tex)
    {
        var pix = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        for (int y = 0; y < h / 2; y++)
        {
            int yOpp = h - 1 - y;
            for (int x = 0; x < w; x++)
            {
                int i1 = y * w + x, i2 = yOpp * w + x;
                var tmp = pix[i1];
                pix[i1] = pix[i2];
                pix[i2] = tmp;
            }
        }
        tex.SetPixels32(pix);
        tex.Apply(false, false);
    }
    [Serializable]
    private class SessionManifest
    {
        public long startEpoch;
        public string startTime;
        public string sessionName;
        public string os;
        public string gpu;
        public string unityVersion;
        public int displayRefresh;
        public int targetFrameRate;
        public string gitCommit;
        public List<FileHash> fileHashes = new List<FileHash>();

    }

    private void WriteManifest(string sessionFolder)
    {
        if (_manifestWritten) return;

        string gitHash = Resources.Load<TextAsset>("git_commit")?.text ?? "unknown";



        var m = new SessionManifest()
        {
            startEpoch = _sessionStartEpoch,
            startTime = _sessionStartHuman,
            sessionName = SessionManager.Instance.SessionName,
            os = SystemInfo.operatingSystem,
            gpu = SystemInfo.graphicsDeviceName,
            unityVersion = Application.unityVersion,
            displayRefresh = Screen.currentResolution.refreshRate,
            targetFrameRate = Application.targetFrameRate,
            gitCommit = gitHash

        };

        string json = JsonUtility.ToJson(m, true);
        File.WriteAllText(Path.Combine(sessionFolder, "MANIFEST.json"), json);
        _manifestWritten = true;
    }

    [Serializable]
    private class FileHash
    {
        public string fileName;
        public string hash;
    }

    string ComputeSHA256(string filePath)
    {
        using (var sha = SHA256.Create())
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash)
                            .Replace("-", "")
                            .ToLowerInvariant();
        }
    }

    [Serializable]
    public class TaskSummary
    {
        public int TrialsTotal;
        public float Accuracy;
        public float MeanRT_ms;
    }
    [Serializable]
    public class TrialDetail
    {
        public int TrialIndex;
        public bool Correct;
        public float ReactionTimeMs;
    }

    /// <summary>
    /// Writes a per‐task SUMMARY.json  SUMMARY.csv in the task folder,
    /// then appends one row to _NERV/MASTER_LOGS/SESSION_SUMMARY.csv.
    /// </summary>
    public void WriteTaskSummary(string taskAcronym, TaskSummary summary, List<TrialDetail> trialDetails)
    {
        Debug.Log("Writing task summary");
        // --- 1) SUMMARY.csv in this task folder ---
        string csvPath = Path.Combine(taskFolder, "SUMMARY.csv");
        bool isNew = !File.Exists(csvPath);
        using (var w = new StreamWriter(csvPath, false))
        {
            // — overall summary —
            w.WriteLine("TrialsTotal,Accuracy,MeanRT_ms");
            w.WriteLine($"{summary.TrialsTotal},{summary.Accuracy},{summary.MeanRT_ms}");
            w.WriteLine(); // blank line

            // — trial‐level details —
            w.WriteLine("TrialIndex,Correct,ReactionTimeMs");
            foreach (var d in trialDetails)
                w.WriteLine($"{d.TrialIndex},{d.Correct},{d.ReactionTimeMs}");
        }

        // --- 2) Append to lab-wide Session_SUMMARY.csv ---
        Debug.Log("Writing session summary");
        string sessionFolder = SessionManager.Instance.SessionFolder
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string parentDir = Path.GetDirectoryName(sessionFolder);
        Directory.CreateDirectory(parentDir);
        string masterPath = Path.Combine(parentDir, "_MASTER_SUMMARY.csv");
        bool masterNew = !File.Exists(masterPath);
        using (var mw = new StreamWriter(masterPath, true))
        {
            if (masterNew)
                mw.WriteLine("Session,Task,TrialsTotal,Accuracy,MeanRT_ms,Timestamp");

            // derive session folder name, e.g. "20250705_153012"
            string sessionName = Path.GetFileName(
                SessionManager.Instance.SessionFolder.TrimEnd(Path.DirectorySeparatorChar)
            );
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            mw.WriteLine(
                $"{sessionName},{taskAcronym}," +
                $"{summary.TrialsTotal},{summary.Accuracy}," +
                $"{summary.MeanRT_ms}," +
                $"{timestamp}"
            );
        }

        Debug.Log($"[SessionLogManager] Wrote SUMMARY for {taskAcronym}: " +
                  $"{summary.TrialsTotal} trials, {summary.Accuracy:P1} accuracy, " +
                  $"{summary.MeanRT_ms:F1}ms RT");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (_currentTrialManager == null) return;

        var tmType = _currentTrialManager.GetType();

        // 1) get the summary
        var miSum = tmType.GetMethod("GetTaskSummary",
                    BindingFlags.Public | BindingFlags.Instance);
        var summary = (TaskSummary)miSum.Invoke(_currentTrialManager, null);

        // 2) get the per-trial list
        var miDet = tmType.GetMethod("GetTaskDetails",
                    BindingFlags.Public | BindingFlags.Instance);
        var details = (List<TrialDetail>)miDet.Invoke(_currentTrialManager, null);

        // 3) write both
        WriteTaskSummary(_currentTaskAcronym, summary, details);

        // clear for the next scene
        _currentTrialManager = null;
        _currentTaskAcronym = null;
    }


    /// <summary>
    /// TrialManager calls this in Awake to register itself and its acronym.
    /// </summary>
    public void RegisterTrialManager(object manager, string acronym)
    {
        _currentTrialManager = manager;
        _currentTaskAcronym = acronym;
    }
    

    /// <summary>
    /// Append a single experimenter comment (already timestamped) to experimenter_comments.txt
    /// in the current task’s folder.
    /// </summary>
    public void LogExperimenterComment(string commentLine)
    {
        // ensure taskFolder is set (it’s set in OnSceneLoaded)
        if (string.IsNullOrEmpty(taskFolder))
        {
            Debug.LogError("SessionLogManager: taskFolder not initialized; cannot log comment.");
            return;
        }
        string path = Path.Combine(taskFolder, "experimenter_comments.txt");
        File.AppendAllText(path, commentLine + System.Environment.NewLine);
    }

}