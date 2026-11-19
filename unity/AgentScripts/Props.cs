using UnityEngine; using System.Linq;
public static class AgentProps {
  public static string Shooter() { var p = Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None).FirstOrDefault(x => x.act == Achterbahn.FairPerson.Act.Shooter); return Aim(p, -1.8f, 1.2f, -1.2f); }
  public static string Snack() { var p = Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None).FirstOrDefault(x => x.HandR != null && x.HandR.childCount > 0 && x.act == Achterbahn.FairPerson.Act.Wander); return Aim(p, 1.2f, .3f, 2.2f); }
  public static string Balloon() { var b = GameObject.Find("Besucher").GetComponentsInChildren<Achterbahn.FollowHand>().FirstOrDefault(); if (b == null) return "kein"; var p = b.hand.GetComponentInParent<Achterbahn.FairPerson>(); return Aim(p, 1.5f, .6f, 2.8f); }
  static string Aim(Achterbahn.FairPerson p, float dx, float dy, float dz) { if (p == null) return "keiner"; Time.timeScale = 0;
    var go = GameObject.Find("PruefKamera"); var c = p.transform.position + Vector3.up * 1.2f; go.transform.position = c + p.transform.forward * dz + p.transform.right * dx + Vector3.up * dy; go.transform.LookAt(c + Vector3.up * .2f); return p.act + " " + p.transform.position.ToString("F1"); }
}
