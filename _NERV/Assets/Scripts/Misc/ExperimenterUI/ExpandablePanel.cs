using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpandablePanel : MonoBehaviour {
  public GameObject collapsedView; // the cube
  public GameObject expandedView;  // full panel UI
  public Vector2 collapsedSize, expandedSize;
  RectTransform rt;
  bool isExpanded;
  void Awake() => rt = GetComponent<RectTransform>();
  public void Toggle() {
    isExpanded = !isExpanded;
    collapsedView.SetActive(!isExpanded);
    expandedView.SetActive(isExpanded);
    rt.sizeDelta = isExpanded ? expandedSize : collapsedSize;
  }
}
