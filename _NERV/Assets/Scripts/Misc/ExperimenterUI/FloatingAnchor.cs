using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingAnchor : MonoBehaviour {
  public float amplitude = 5f, period = 1f;
  Vector3 start;
  void Awake() => start = transform.localPosition;
  void Update() {
    float y = Mathf.Sin(Time.time * 2*Mathf.PI/period) * amplitude;
    transform.localPosition = start + Vector3.up * y;
  }
}
