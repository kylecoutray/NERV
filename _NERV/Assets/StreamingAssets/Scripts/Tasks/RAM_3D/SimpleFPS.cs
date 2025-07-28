using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPS : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1f;

    [Header("Look Control")]
    public float rotationSpeed = 75f;
    [Tooltip("When true, use mouse. When false, use the GazeCursor UI object.")]
    public bool simulateWithCursor = true;
    [Tooltip("The name of your GazeCursor GameObject in the scene")]
    public string gazeCursorName = "GazeCursor";

    [Tooltip("Drag your TrialManager here")]
    public TrialManagerRAM3D trialManager;

    private CharacterController cc;
    private RectTransform _gazeCursorRect;
    private bool canLook = false;
    private bool canWalk = false;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Camera.main.nearClipPlane = 0.01f;

        // if we’re using gaze, try to find it once at startup
        if (!simulateWithCursor)
        {
            var go = GameObject.Find(gazeCursorName);
            if (go != null)
                _gazeCursorRect = go.GetComponent<RectTransform>();
            else
                Debug.LogWarning($"[SimpleFPS] Could not find GazeCursor named '{gazeCursorName}'");
        }
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

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        if (!canWalk) return;
        Vector3 forward = transform.forward;
        cc.Move(forward * moveSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        if (!canLook) return;

        // determine the screen-space cursor position
        Vector2 cursorPos;
        if (simulateWithCursor)
        {
            cursorPos = Input.mousePosition;
        }
        else
        {
            if (_gazeCursorRect == null) return;    // nothing to look at
            // In Screen-Space Overlay canvases, RectTransform.position is already in screen coords
            cursorPos = _gazeCursorRect.position;
        }

        // compute how far off-center we are on X
        Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        float offsetX = (cursorPos.x - center.x) / center.x;
        offsetX = Mathf.Clamp(offsetX, -1f, 1f);

        // apply yaw
        float yawDelta = offsetX * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, yawDelta, Space.Self);
    }

    void OnLogEvent(string label)
    {
        switch (label)
        {
            case "Walking":
                canLook = true;
                canWalk = true;
                break;
            case "Reset":
            case "Feedback":
                canLook = false;
                canWalk = false;
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var stim = other.GetComponent<StimulusID>();
        if (stim != null)
            trialManager.RegisterCollision(stim.Index);
    }
}
