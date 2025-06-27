using UnityEngine;
using System.Runtime.InteropServices;

public class DllTest : MonoBehaviour {
    [DllImport("nicaiu.dll", CharSet = CharSet.Ansi)]
    static extern int DAQmxGetSysNIDAQMajorVersion(out int version);


    void Start() {
        if (DAQmxGetSysNIDAQMajorVersion(out int ver) == 0)
            Debug.Log($"NI-DAQmx major version = {ver}");
        else
            Debug.LogError("Failed to get DAQmx version");
    }
}
