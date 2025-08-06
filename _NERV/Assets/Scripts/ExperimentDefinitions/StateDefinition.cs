using System;

[Serializable]
public class StateDefinition
{
    public string Name;
    public bool IsTTL;
    public bool IsStimulus;
    public bool IsDelay;
    public bool IsChoice;
    public bool IsFeedback;
    public bool IsClearAll;
    public bool IsSample;
    public int TTLCode;
    public float PostStateDelay;
}
