using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentInspect { public static string Main() {
  var s = new System.Text.StringBuilder();
  string p = "Assets/Quaternius/People/Casual_Character_kZ3DmIoGip.glb";
  var imp = AssetImporter.GetAtPath(p); s.AppendLine("importer " + (imp == null ? "null" : imp.GetType().FullName));
  var all = AssetDatabase.LoadAllAssetsAtPath(p);
  foreach (var g in all.GroupBy(a => a.GetType().Name)) s.AppendLine(g.Key + ": " + string.Join(", ", g.Take(40).Select(a => a.name)));
  var go = AssetDatabase.LoadMainAssetAtPath(p) as GameObject;
  if (go != null) { var r = go.GetComponentsInChildren<Renderer>(); var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds);
    s.AppendLine("bounds " + b.size.ToString("F2") + " comps " + string.Join(",", go.GetComponents<Component>().Select(c => c.GetType().Name)));
    s.AppendLine("children " + string.Join(",", go.GetComponentsInChildren<Transform>().Take(30).Select(t => t.name)));
    foreach (var m in r) s.AppendLine("r " + m.name + " " + m.GetType().Name + " mats " + string.Join("/", m.sharedMaterials.Select(mm => mm == null ? "null" : mm.name + ":" + mm.shader.name))); }
  var clips = all.OfType<AnimationClip>().ToArray(); if (clips.Length > 0) s.AppendLine("legacy " + clips[0].legacy + " len " + clips[0].length);
  return s.ToString(); } }
