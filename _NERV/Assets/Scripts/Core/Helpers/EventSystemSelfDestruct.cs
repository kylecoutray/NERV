using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
public class EventSystemSelfDestruct : MonoBehaviour
{
    void Awake()
    {
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        if (systems.Length > 1)
        {
            Debug.Log($"[EventSystemSelfDestruct] Destroying extra EventSystem on '{gameObject.name}'");
            Destroy(gameObject);
        }
    }
}
