using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentInspect2 { public static string Main() {
  var s = new System.Text.StringBuilder();
  foreach (var p in System.IO.Directory.GetFiles("Assets/Quaternius/People", "*.glb")) {
    var all = AssetDatabase.LoadAllAssetsAtPath(p.Replace((char)92,(char)47));
    var clips = all.OfType<AnimationClip>().Select(c => c.name.Split('|').Last()).OrderBy(n => n);
    var go = AssetDatabase.LoadMainAssetAtPath(p.Replace((char)92,(char)47)) as GameObject;
    var r = go.GetComponentsInChildren<Renderer>(); var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds);
    int tris = go.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(m => m.sharedMesh.triangles.Length / 3);
    s.AppendLine(System.IO.Path.GetFileNameWithoutExtension(p) + " h=" + b.size.y.ToString("F2") + " tris=" + tris + " smr=" + r.Length + " : " + string.Join(",", clips)); }
  return s.ToString(); } }
