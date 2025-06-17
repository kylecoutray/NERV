using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds one row of your CSV, with dynamic per‐state entries.
/// </summary>
[Serializable]
public class TrialData
{
    public string  TrialID;
    public int     BlockCount;

    // Any number of states → int[] or Vector3[] or float
    public Dictionary<string,int[]>    StimIndices    = new Dictionary<string,int[]>();
    public Dictionary<string,Vector3[]> StimLocations = new Dictionary<string,Vector3[]>();
    public Dictionary<string,float>     Durations      = new Dictionary<string,float>();

    // Helpers for easy access:
    public int[]    GetStimIndices(string state)
        => StimIndices.TryGetValue(state, out var a) ? a : Array.Empty<int>();

    public Vector3[] GetStimLocations(string state)
        => StimLocations.TryGetValue(state, out var v) ? v : Array.Empty<Vector3>();

    public float    GetDuration(string state)
        => Durations.TryGetValue(state, out var d) ? d : 0f;
}
