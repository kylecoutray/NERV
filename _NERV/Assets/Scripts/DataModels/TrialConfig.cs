using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TrialConfig
{
    public string TrialID;
    public int BlockCount;
    public float DisplaySampleDuration;
    public float PostSampleDelayDuration;
    public float DisplayPostSampleDistractorsDuration;
    public float PreTargetDelayDuration;
    public Vector3 SampleStimLocation;
    public int[] SearchStimIndices;
    public Vector3[] SearchStimLocations;
    public int[] SearchStimTokenReward;
    public int[] PostSampleDistractorStimIndices;
    public Vector3[] PostSampleDistractorStimLocations;
}
