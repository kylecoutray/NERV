using UnityEngine;

public class AudioListenerSelfDestruct : MonoBehaviour
{
    void Awake()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            var thisListener = GetComponent<AudioListener>();
            if (thisListener != null)
            {
                Debug.Log($"[AudioListenerSelfDestruct] Removing extra AudioListener from '{gameObject.name}'");
                Destroy(thisListener); // only remove the listener, keep the camera
            }
        }
    }
}
