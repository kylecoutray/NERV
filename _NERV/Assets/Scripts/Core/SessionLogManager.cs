using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Threading.Tasks;


public class SessionLogManager : MonoBehaviour
{
    public static SessionLogManager Instance { get; private set; }

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
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Wait for any trial scene to load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // locate TrialManagerXxx in scene
        var tm = FindObjectsOfType<MonoBehaviour>()
                    .FirstOrDefault(m => m.GetType().Name.StartsWith("TrialManager"));

        if (tm == null)
            return; // not our trial scene

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
        allWriter.WriteLine("Time,TrialID,Event,Details");
        ttlWriter.WriteLine("Time,TrialID,Event,Code");
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
        string time = DateTime.Now.ToString("HH:mm:ss.fff");
        allWriter.WriteLine($"{time},{trialID},{evt},{details}");
        allWriter.Flush();
        Debug.Log($"[ALL_LOGS] {time}, {trialID}, {evt}, {details}");

        // bump the seen‐count for this state
        int seen = 0;
        _stateCaptureCounts.TryGetValue(evt, out seen);
        seen++;
        _stateCaptureCounts[evt] = seen;

        // capture only during the first N trials
        if (enableScreenshots && seen <= trialsToCapture)
            StartCoroutine(CaptureStateScreenshotCoroutine(evt));

    }

    /// <summary>
    /// Send a TTL pulse if code in 1–8, and also log to TTL_CSV.
    /// </summary>
    public void LogTTL(string trialID, string evt, int ttlCode)
    {
        if (ttlWriter == null) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
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

        // write TTL CSV
        ttlWriter.WriteLine($"{timestamp},{trialID},{evt},{ttlCode}");
        ttlWriter.Flush();

        // console feedback
        string line = $"{timestamp},{status},{evt},{byteSent}";
        if (status == "FAILED") Debug.LogWarning($"[TTL] {line}");
        else Debug.Log($"[TTL] {line}");
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
        // wait for the frame where everything (world + UI) is drawn
        yield return new WaitForEndOfFrame();

        // optional extra delays if your spawns happen one frame later
        for (int i = 1; i < screenshotFrameDelay; i++)
            yield return new WaitForEndOfFrame();

        // copy the full screen (including UI) into our RT
        Graphics.Blit(null, _screenshotRT);

        // now do the async readback + encode + background write
        AsyncGPUReadback.Request(_screenshotRT, 0, request =>
        {
            if (request.hasError) { Debug.LogWarning($"[Screenshot] GPU readback error for {stateName}"); return; }
            _screenshotTex.LoadRawTextureData(request.GetData<byte>());
            _screenshotTex.Apply(false, false);
            FlipVertical(_screenshotTex);
            byte[] jpg = _screenshotTex.EncodeToJPG(jpgQuality);
            string folder = Path.Combine(taskFolder, "StatesCaptured");
            Directory.CreateDirectory(folder);
            string fileName = $"{stateName}_{DateTime.Now:HHmmss}.jpg";
            string fullPath = Path.Combine(folder, fileName);
            Task.Run(() => File.WriteAllBytes(fullPath, jpg));
            Debug.Log($"<color=yellow>[Screenshot]</color> {stateName} → {fullPath}");
        });
    }
    void FlipVertical(Texture2D tex)
    {
        var pix = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        for (int y = 0; y < h/2; y++)
        {
            int yOpp = h - 1 - y;
            for (int x = 0; x < w; x++)
            {
                int i1 = y*w + x, i2 = yOpp*w + x;
                var tmp = pix[i1];
                pix[i1] = pix[i2];
                pix[i2] = tmp;
            }
        }
        tex.SetPixels32(pix);
        tex.Apply(false, false);
    }


}