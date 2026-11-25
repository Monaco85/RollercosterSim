using UnityEngine;
using Achterbahn;
// Prueft "KI-Wagen aus" und die Lenkkurve "Leicht" (nur Editor, Sitz bleibt gesperrt)
public static class KiTest {
  static object GC => Object.FindFirstObjectByType<GameController>();
  static object F(string n) { var g = GC; return g.GetType().GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(g); }
  public static string Kurve() {
    int alt = BumperInput.Lenkart; BumperInput.Lenkart = 1; string s = "Leicht @" + BumperInput.RangeDeg + ": ";
    foreach (int a in new[] { 5, 50, 100, 150, 200, 225, 250, 300, 350, 400, 450 }) s += a + "°→" + BumperInput.RadWinkel(a).ToString("F0") + "  ";
    BumperInput.Lenkart = alt; return s + " (Lenkart jetzt " + alt + ")";
  }
  public static string Ki(int aus) { PlayerPrefs.SetInt("kiAus", aus); return "kiAus=" + aus + " sitz=" + PlayerPrefs.GetInt("editorSitzFreigabe", 0); }
  public static string Info() {
    var cur = F("cur"); var br = (BumperRide)cur.GetType().GetField("bumper").GetValue(cur);
    string s = "state=" + F("state") + " ";
    for (int i = 0; i < br.cars.Length; i++) s += $"[{i} aus={br.cars[i].aus} x={br.cars[i].x:F1} z={br.cars[i].z:F1} v={br.cars[i].Speed:F2}] ";
    var cars = FairWorld.JoustCars; if (cars != null) for (int i = 0; i < cars.Length; i++) s += " vis" + i + "=" + (cars[i] != null && cars[i].gameObject.activeSelf);
    return s;
  }
}
