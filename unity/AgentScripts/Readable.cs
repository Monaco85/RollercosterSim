using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentReadable { public static string Main() { int r = 0, n = 0;
  foreach (var p in System.IO.Directory.GetFiles("Assets/Quaternius/People", "*.glb")) { var go = AssetDatabase.LoadMainAssetAtPath(p.Replace((char)92,(char)47)) as GameObject;
    foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { n++; if (s.sharedMesh.isReadable) r++; } }
  return "lesbar " + r + " von " + n; } }
