using UnityEngine;

public class DisableLogger : MonoBehaviour
{
    void OnDisable()
    {
        Debug.LogWarning(
            $"[DisableLogger] {gameObject.name} was disabled!\n" +
            StackTraceUtility.ExtractStackTrace(),
            gameObject
        );
    }
}
