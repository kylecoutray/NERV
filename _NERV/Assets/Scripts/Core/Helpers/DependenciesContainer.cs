using UnityEngine;
using TMPro;

public class DependenciesContainer : MonoBehaviour
{
    [Header("Core Systems")]
    public Camera MainCamera;
    public StimulusSpawner Spawner;
    public GenericConfigManager ConfigManager;

    [Header("UI Elements")]
    public TMP_Text FeedbackText;
    public TMP_Text ScoreText;
    public GameObject CoinUI;

    [Header("Block‐Pause Screen")]
    public BlockPauseController PauseController;

    [Header("Logging Sub-Container")]
    public LoggingContainer LoggingContainer;
    
}