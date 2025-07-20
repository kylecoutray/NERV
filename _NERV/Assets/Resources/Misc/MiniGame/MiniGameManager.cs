using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager I;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public Image playerImage;              // assign your Player UI Image here

    [Header("Game References")]
    public PlayerController playerController;
    public ObstacleSpawner spikeSpawner;
    public ParticleSpawner particleSpawner;

    [Header("Flash Settings")]
    [Tooltip("Total time spent flashing")]
    public float flashDuration = 0.8f;
    [Tooltip("Number of on/off flashes")]
    public int flashCount = 4;
    [Tooltip("Delay after flash before reset")]
    public float resetDelay = 0.5f;

    public int score;
    private bool isGameOver;

    void Awake()
    {
        if (I == null) I = this;
        else          Destroy(gameObject);

        // Auto‑assign playerImage if it wasn't set in the inspector
        if (playerImage == null && playerController != null)
        {
            playerImage = playerController.GetComponent<Image>();
            if (playerImage == null)
                Debug.LogError("[MiniGameManager] No Image on playerController!");
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // stop new spikes
        if (spikeSpawner != null) spikeSpawner.StopSpawning();
        // freeze existing spikes
        foreach (var obs in FindObjectsOfType<ObstacleController>())
            obs.StopMovement();
        // freeze particles
        foreach (var p in FindObjectsOfType<ParticleController>())
            p.StopMovement();

        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        if (playerImage != null)
        {
            for (int i = 0; i < flashCount; i++)
            {
                playerImage.enabled = false;
                yield return new WaitForSeconds(flashDuration / (flashCount * 2));
                playerImage.enabled = true;
                yield return new WaitForSeconds(flashDuration / (flashCount * 2));
            }
        }
        else
        {
            Debug.LogWarning("[MiniGameManager] playerImage is null; skipping flash.");
        }

        yield return new WaitForSeconds(resetDelay);
        ResetGame();
    }

    public void ResetGame()
    {
        // reset score
        isGameOver = false;
        score = 0;
        if (scoreText != null) scoreText.text = "Score: 0";

        // reset player
        if (playerController != null) playerController.ResetPlayer();

        // clear spikes
        foreach (var obs in FindObjectsOfType<ObstacleController>())
            Destroy(obs.gameObject);
        // restart spike spawner
        if (spikeSpawner != null)
        {
            spikeSpawner.enabled = false;
            spikeSpawner.enabled = true;
        }

        // reset particles
        if (particleSpawner != null)
        {
            particleSpawner.ResetPool();
            particleSpawner.enabled = false;
            particleSpawner.enabled = true;
        }
    }

    public void AddPoint()
    {
        if (isGameOver) return;
        score++;
        if (scoreText != null) scoreText.text = "Score: " + score;
    }
}
