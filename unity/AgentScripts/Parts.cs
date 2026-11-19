using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentParts { public static string Main() { var s = new System.Text.StringBuilder();
  foreach (var p in System.IO.Directory.GetFiles("Assets/Quaternius/People", "*.glb")) { var go = AssetDatabase.LoadMainAssetAtPath(p.Replace((char)92,(char)47)) as GameObject;
    s.AppendLine(System.IO.Path.GetFileNameWithoutExtension(p) + ": " + string.Join(", ", go.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.name + "/" + r.sharedMesh.name))); }
  return s.ToString(); } }
