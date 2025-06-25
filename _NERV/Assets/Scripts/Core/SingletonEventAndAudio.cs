using UnityEngine;
using UnityEngine.EventSystems;

public class SingletonEventAndAudio : MonoBehaviour
{
    void Awake()
    {
        // Handle EventSystem
        var eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Handle AudioListener
        var audioListeners = FindObjectsOfType<AudioListener>();
        if (audioListeners.Length > 1)
        {
            // Remove the one attached to this GameObject, if present
            AudioListener thisListener = GetComponent<AudioListener>();
            if (thisListener != null)
            {
                Destroy(thisListener);
            }
            else
            {
                Destroy(gameObject); // Fallback: destroy entire object if unknown source
            }
            return;
        }

    }
}
