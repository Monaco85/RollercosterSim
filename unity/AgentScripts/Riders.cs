using UnityEngine; using System.Linq;
public static class AgentRiders { public static string Main() {
  var s = new System.Text.StringBuilder();
  var all = Object.FindObjectsByType<Achterbahn.FairPerson>(FindObjectsSortMode.None);
  s.AppendLine("personen " + all.Length + " fahrgaeste " + all.Count(p => p.act == Achterbahn.FairPerson.Act.Rider));
  foreach (var p in all.Where(p => p.act == Achterbahn.FairPerson.Act.Rider).Take(6)) s.AppendLine(p.transform.parent.name + " local " + p.transform.localPosition.ToString("F2") + " world " + p.transform.position.ToString("F1") + " anim " + (p.Anim == null ? "null" : p.Anim.Name) + " vis " + p.GetComponent<Renderer>().isVisible);
  var sc = GameObject.Find("Scooter"); if (sc != null) s.AppendLine("scooter kinder: " + string.Join(",", sc.GetComponentsInChildren<Transform>().Select(t => t.name)));
  return s.ToString(); } }
