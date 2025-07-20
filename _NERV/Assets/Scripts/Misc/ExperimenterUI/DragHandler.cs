using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler {
  RectTransform rt; Vector2 start;
  void Awake() => rt = GetComponent<RectTransform>();
  public void OnBeginDrag(PointerEventData e) {
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out start);
  }
  public void OnDrag(PointerEventData e) {
    Vector2 pos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, e.position, e.pressEventCamera, out pos);
    rt.anchoredPosition = pos - start;
  }
}
