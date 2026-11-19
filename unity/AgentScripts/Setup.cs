using UnityEngine; using UnityEditor;
public static class AgentSetup { public static string Main() {
  AssetDatabase.ImportAsset("Assets/AmbientCG", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
  AssetDatabase.ImportAsset("Assets/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
  EditorApplication.ExecuteMenuItem("Achterbahn/Assets einrichten");
  var ta = AssetDatabase.LoadAssetAtPath<Achterbahn.ThemeAssets>("Assets/Resources/ThemeAssets.asset");
  string N(Object o) => o == null ? "FEHLT" : o.name;
  return "duskSky " + N(ta.duskSky) + " sky " + N(ta.sky) + " asphalt " + N(ta.asphalt) + " canvas " + N(ta.canvas) + " wood " + N(ta.paintedWood) + " corr " + N(ta.corrugated) + " lacq " + N(ta.lacquer)
   + " bulb " + N(ta.bulb) + " halo " + N(ta.halo) + " beam " + N(ta.beam) + " neon " + N(ta.neon) + " spark " + N(ta.spark) + " people " + (ta.people == null ? 0 : ta.people.Length)
   + " audio " + N(ta.fairAmbience) + "," + N(ta.organ) + "," + N(ta.chainLift) + "," + N(ta.bang) + "," + N(ta.screams); } }
