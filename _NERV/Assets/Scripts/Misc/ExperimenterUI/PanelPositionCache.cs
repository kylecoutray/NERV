using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelPositionCache : MonoBehaviour {
  [SerializeField] string key;
  RectTransform rt;
  void Awake() {
    rt = GetComponent<RectTransform>();
    if(PlayerPrefs.HasKey(key + "_x")) {
      float x = PlayerPrefs.GetFloat(key + "_x"),
            y = PlayerPrefs.GetFloat(key + "_y");
      rt.anchoredPosition = new Vector2(x,y);
    }
  }
  public void SavePosition() {
    PlayerPrefs.SetFloat(key + "_x", rt.anchoredPosition.x);
    PlayerPrefs.SetFloat(key + "_y", rt.anchoredPosition.y);
  }
}
