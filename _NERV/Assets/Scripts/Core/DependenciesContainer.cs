using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DependenciesContainer : MonoBehaviour
{
    [Header("Core References")]
    public Camera                MainCamera;
    public StimulusSpawner       Spawner;
    public GenericConfigManager  ConfigManager;

    [Header("UI Text")]
    public TMP_Text              FeedbackText;
    public TMP_Text              ScoreText;

    [Header("Coin UI")]
    public GameObject            CoinUI;
    
    [Header("Block Pause")]
    public BlockPauseController  PauseController;
}
