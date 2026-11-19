using UnityEngine; using System.Linq;
public static class AgentFollow {
  public static string At(string parentName, float dx, float dy, float dz) {
    var p = Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None).FirstOrDefault(x => x.transform.parent != null && x.transform.parent.name.StartsWith(parentName));
    if (p == null) return "keiner";
    var go = GameObject.Find("PruefKamera"); var c = p.transform.position + Vector3.up * .9f;
    go.transform.position = c + p.transform.forward * dz + p.transform.right * dx + Vector3.up * dy; go.transform.LookAt(c); return p.transform.position.ToString("F1"); } }
