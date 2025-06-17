using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[CreateAssetMenu(menuName = "Experiment/Definition")]
public class ExperimentDefinition : ScriptableObject
{
    [Header("Identifier")]
    public string Acronym;               // e.g. "FWM" for FeatureWM

    [Header("State setup")]
    public List<StateDefinition> States = new List<StateDefinition>();

    //[Header("Global timing defaults")]
    //public float DefaultIdleDuration = 2f;
    
    

}
