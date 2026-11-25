using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
// Release-Build OHNE Viking Village (Asset-Store-EULA): Resources-Ordner kurz umbenennen, bauen, zuruecklegen, Bericht pruefen.
public static class ReleaseBuild {
  const string Res = "Assets/Viking Village/Resources", Aus = "Assets/Viking Village/Resources_ReleaseAus";
  public static string V14() => Run("C:/AchterBahnUnity/Release/Achterbahn-Unity-v1.4");
  public static string Run(string outDir) {
    string log = "";
    if (AssetDatabase.IsValidFolder(Res)) { var e = AssetDatabase.MoveAsset(Res, Aus); if (!string.IsNullOrEmpty(e)) return "Umbenennen fehlgeschlagen: " + e; log += "VV-Resources ausgelagert. "; }
    try {
      Directory.CreateDirectory(outDir);
      var opts = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = Path.Combine(outDir, "Achterbahn.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.DetailedBuildReport };
      PlayerSettings.productName = "Achterbahn";
      var rep = BuildPipeline.BuildPlayer(opts);
      log += "Ergebnis " + rep.summary.result + ", " + (rep.summary.totalSize / 1048576) + " MB. ";
      Achterbahn.EditorTools.AchterbahnMenu.BrueckeMitkopieren(outDir);
      log += File.Exists(Path.Combine(outDir, "Bruecke", "Achterbahn-Bruecke.exe")) ? "Bruecke dabei. " : "BRUECKE FEHLT. ";
      var vv = rep.packedAssets.SelectMany(p => p.contents).Select(c => c.sourceAssetPath).Where(p => p != null && p.Contains("Viking Village")).Distinct().ToList();
      log += "Viking-Village-Dateien im Build: " + vv.Count + (vv.Count > 0 ? " z. B. " + string.Join(", ", vv.Take(5)) : "");
    } finally {
      if (AssetDatabase.IsValidFolder(Aus)) { AssetDatabase.MoveAsset(Aus, Res); log += " | zurueckgelegt."; }
    }
    return log;
  }
}
