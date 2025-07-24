using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// SimpleFPS.cs
[RequireComponent(typeof(CharacterController))]
public class SimpleFPS : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;

    [Header("Look Control")]
    public float rotationSpeed = 100f;
    public Transform cameraTransform;

    private CharacterController cc;
    private float pitch = 0f;
    private bool canLook = false;  // ← locked until BroadcastMessage arrives

    [Tooltip("Drag your TrialManager here")]
    public TrialManagerRAM_3D trialManager;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        Vector3 inDir = transform.right * Input.GetAxis("Horizontal")
                      + transform.forward * Input.GetAxis("Vertical");
        cc.Move(inDir * moveSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        if (!canLook) return;

        // Get normalized X offset (–1 to +1)
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float offsetX  = ((Vector2)Input.mousePosition - center).x / center.x;
        offsetX = Mathf.Clamp(offsetX, -1f, 1f);

        // Apply yaw only
        float yawDelta = offsetX * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, yawDelta, Space.Self);
    }



    // This will be called by BroadcastMessage from the LogEvent script
    void OnLogEvent(string label)
    {
        if (label == "Walking")
            canLook = true;
    }
    
        void OnEnable()
    {
        if (trialManager != null)
            trialManager.OnLogEventInstance += OnLogEvent;
    }

    void OnDisable()
    {
        if (trialManager != null)
            trialManager.OnLogEventInstance -= OnLogEvent;
    }

}
