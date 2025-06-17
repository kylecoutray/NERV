using System;
using UnityEngine;

[Serializable]
public class TrialDefinitionTemplate
{
    public string  TrialID;
    public int     BlockCount;

    // for stimulus
    public int[]   StimIndices;
    public int[]   ExcludedIndices;
    public Vector3[] StimLocations;

    // for choice/feedback
    public int     CorrectIndex;
}
