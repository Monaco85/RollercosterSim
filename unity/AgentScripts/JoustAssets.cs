using UnityEngine; using UnityEditor;
// Traegt die neuen Poly-Haven-Modelle (Boxauto-Turnier) in ThemeAssets ein, ohne das ganze "Assets einrichten" neu zu laufen.
public static class AgentJoustAssets { public static string Main() {
  AssetDatabase.ImportAsset("Assets/PolyHaven/Models", ImportAssetOptions.ImportRecursive);
  var ta = AssetDatabase.LoadAssetAtPath<Achterbahn.ThemeAssets>("Assets/Resources/ThemeAssets.asset");
  GameObject G(string id) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyHaven/Models/" + id + "/" + id + ".gltf");
  ta.chandelier = G("lantern_chandelier_01"); ta.kiteShield = G("kite_shield"); ta.warHammer = G("ornate_war_hammer"); ta.estoc = G("antique_estoc");
  ta.chest = G("treasure_chest"); ta.wineBarrel = G("wine_barrel_01"); ta.lionHead = G("lion_head"); ta.firePit = G("stone_fire_pit"); ta.bucket = G("wooden_bucket_01"); ta.cannon = G("cannon_01");
  EditorUtility.SetDirty(ta); AssetDatabase.SaveAssets();
  var sb = new System.Text.StringBuilder();
  foreach (var o in new[] { ta.chandelier, ta.kiteShield, ta.warHammer, ta.estoc, ta.chest, ta.wineBarrel, ta.lionHead, ta.firePit, ta.bucket, ta.cannon })
  {
    if (o == null) { sb.Append("FEHLT; "); continue; }
    var b = new Bounds(); bool f = false;
    foreach (var r in o.GetComponentsInChildren<Renderer>()) { if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); }
    sb.Append(o.name + " " + b.size.ToString("0.00") + " c" + b.center.ToString("0.00") + "; ");
  }
  return sb.ToString(); } }
