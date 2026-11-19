using UnityEngine; using System.Linq;
public static class AgentPerf {
  public static string Toggle(string name, bool on) { var root = GameObject.Find("Bahn Wilde Maus"); if (root == null) return "kein root";
    int n = 0; foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(name) && t.parent == root.transform) { t.gameObject.SetActive(on); n++; } return name + " " + on + " x" + n; }
  public static string Lights(bool on) { int n = 0; foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type != LightType.Directional) { l.enabled = on; n++; } return "lights " + on + " " + n; }
  public static string Halos(bool on) { int n = 0; foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.EndsWith(" Hof")) { r.enabled = on; n++; } return "halos " + on + " " + n; }
  public static string Stats() { var smr = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None); var mr = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None); var an = Object.FindObjectsByType<Animation>(FindObjectsSortMode.None);
    return "smr " + smr.Length + " mr " + mr.Length + " anim " + an.Length + " fps " + (1f / Time.smoothDeltaTime).ToString("0") + " screen " + Screen.width + "x" + Screen.height; }
}
public static class AgentPerf2 {
  public static string NoRiders() { int n = 0; foreach (var p in Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None)) { p.gameObject.SetActive(false); n++; } return "riders off " + n; }
  public static string Groups() { var root = GameObject.Find("Bahn Wilde Maus"); var s = new System.Text.StringBuilder();
    foreach (Transform t in root.transform) { int r = t.GetComponentsInChildren<Renderer>(true).Length; if (r > 20) s.Append(t.name + ":" + r + " "); } return s.ToString(); }
  public static string Off(string prefix) { var root = GameObject.Find("Bahn Wilde Maus"); int n = 0; foreach (Transform t in root.transform) if (t.name.StartsWith(prefix)) { t.gameObject.SetActive(false); n++; } return prefix + " off " + n; }
}
public static class AgentPerf3 {
  public static string AllOn() { int n = 0; foreach (var p in Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { p.gameObject.SetActive(true); n++; } var root = GameObject.Find("Bahn Wilde Maus"); foreach (Transform t in root.transform) t.gameObject.SetActive(true); return "on " + n; }
  public static string Anim(bool on) { int n = 0; foreach (var a in Object.FindObjectsByType<Animation>(FindObjectsSortMode.None)) { a.enabled = on; n++; } foreach (var p in Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None)) p.enabled = on; return "anim " + on + " " + n; }
}
public static class AgentPerf4 {
  public static string People(bool on) { var b = GameObject.Find("Besucher"); if (b == null) return "kein"; foreach (Transform t in b.transform) t.gameObject.SetActive(on); return "besucher " + on; }
  public static string Prof() { var s = new System.Text.StringBuilder(); s.Append("fps " + (1f / Time.smoothDeltaTime).ToString("0") + " dt " + (Time.smoothDeltaTime * 1000).ToString("0.0") + "ms"); return s.ToString(); }
}
public static class AgentPerf5 {
  public static string Shadows(bool on) { int n = 0; foreach (var p in Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None)) { p.GetComponent<MeshRenderer>().shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; n++; } return "schatten " + on + " " + n; }
  public static string Scripts(bool on) { int n = 0; foreach (var p in Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None)) { p.enabled = on; n++; } return "skripte " + on + " " + n; }
}
