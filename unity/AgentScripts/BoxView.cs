using UnityEngine;
using Achterbahn.Boxauto;
// Prueft die Autoscooter-Vorschau: Kameramodus, Winkel, Hoehe, Abstand, Wagen
public static class BoxView {
  public static string Set(int modus, float winkel, float hoehe, float abstand, int wagen) {
    var v = Object.FindFirstObjectByType<BoxautoVorschau>(); if (v == null) return "keine Vorschau";
    v.enabled = true; v.modus = modus; v.winkel = winkel; v.hoehe = hoehe; v.abstand = abstand; v.wagenNr = wagen; v.autoDrehen = 0; v.hilfe = false;
    v.kamera.fieldOfView = modus == 2 ? 72f : modus == 0 ? 45f : 50f;
    return "ok";
  }
  public static string Cam(float x, float y, float z, float tx, float ty, float tz, float fov) {
    var v = Object.FindFirstObjectByType<BoxautoVorschau>(); v.enabled = false; v.hilfe = false;
    v.kamera.transform.position = new Vector3(x, y, z); v.kamera.transform.LookAt(new Vector3(tx, ty, tz)); v.kamera.fieldOfView = fov;
    return "ok";
  }
  public static string Info() {
    var d = Object.FindFirstObjectByType<BoxautoDemo>(); string s = "";
    for (int i = 0; i < d.Anzahl; i++) s += d.Wagen(i).position.ToString("F1") + " ";
    return s + " fps " + (1f / Time.smoothDeltaTime).ToString("F0");
  }
}
