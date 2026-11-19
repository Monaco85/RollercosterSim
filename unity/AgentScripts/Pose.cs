using UnityEngine; using UnityEditor; using System.Linq;
public static class AgentPose { public static string Main() {
  var s = new System.Text.StringBuilder();
  var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quaternius/People/Casual_Character_kZ3DmIoGip.glb");
  var go = Object.Instantiate(pf);
  foreach (var t in go.GetComponentsInChildren<Transform>()) { int d = 0; for (var p = t; p != go.transform; p = p.parent) d++; if (!t.name.Contains("Index") && !t.name.Contains("Middle") && !t.name.Contains("Ring") && !t.name.Contains("Pinky") && !t.name.Contains("Thumb")) s.AppendLine(new string(' ', d) + t.name); }
  var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>()[1]; s.AppendLine("bones of " + smr.name + ": " + string.Join(",", smr.bones.Select(b => b.name)));
  Object.DestroyImmediate(go); return s.ToString(); } }
