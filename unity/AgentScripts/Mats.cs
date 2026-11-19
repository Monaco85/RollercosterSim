using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentMats { public static string Main() {
  var s = new System.Text.StringBuilder(); var seen = new System.Collections.Generic.HashSet<string>();
  foreach (var p in System.IO.Directory.GetFiles("Assets/Quaternius/People", "*.glb")) {
    var go = AssetDatabase.LoadMainAssetAtPath(p.Replace((char)92,(char)47)) as GameObject;
    foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { s.Append(r.sharedMesh.vertexCount + "v/" + r.sharedMesh.subMeshCount + "s "); foreach (var m in r.sharedMaterials) { if (!seen.Add(m.name)) continue;
      var tex = m.GetTexturePropertyNames().Where(n => m.GetTexture(n) != null).ToArray();
      s.AppendLine(); s.Append("  " + m.name + " col " + (m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor").ToString("F2") : "-") + " tex " + string.Join(",", tex)); } }
    s.AppendLine(" <- " + System.IO.Path.GetFileName(p)); }
  return s.ToString(); } }
