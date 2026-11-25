using UnityEngine;
using UnityEditor;
using Achterbahn.Boxauto;
public static class BoxKw {
  public static string Run() {
    var li = Object.FindFirstObjectByType<BoxautoLichter>(); li.enabled = false;
    foreach (var r in li.birnen) r.SetPropertyBlock(null);
    li.scheinwerferGlas.SetPropertyBlock(null);
    return "mpb weg";
  }
  public static string Mpb() {
    var li = Object.FindFirstObjectByType<BoxautoLichter>();
    var m = new MaterialPropertyBlock(); m.SetColor("_EmissionColor", new Color(0, 4, 0));
    foreach (var r in li.birnen) r.SetPropertyBlock(m);
    return "gruen";
  }
}
