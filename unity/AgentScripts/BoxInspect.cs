using UnityEngine;
using System.Text;
using Achterbahn.Boxauto;
public static class BoxInspect {
  public static string Run() {
    var sb = new StringBuilder();
    var li = Object.FindFirstObjectByType<BoxautoLichter>();
    sb.AppendLine("lichter: " + (li != null) + " enabled " + (li != null && li.enabled) + " birnen " + (li != null && li.birnen != null ? li.birnen.Length : -1));
    if (li != null && li.birnen != null && li.birnen.Length > 0) {
      var r = li.birnen[0]; var m = r.sharedMaterial;
      sb.AppendLine("mat " + m.name + " shader " + m.shader.name + " kw: " + string.Join(",", m.shaderKeywords) + " emis " + m.GetColor("_EmissionColor"));
      sb.AppendLine("isKw " + m.IsKeywordEnabled("_EMISSION") + " enabled: " + string.Join(",", System.Array.ConvertAll(m.enabledKeywords, k => k.name)) + " gi " + m.globalIlluminationFlags);
      var g = li.scheinwerferGlas; sb.AppendLine("glas " + (g != null ? g.sharedMaterial.name + " " + g.sharedMaterial.IsKeywordEnabled("_EMISSION") : "null"));
      var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb); sb.AppendLine("mpb emis " + mpb.GetColor("_EmissionColor") + " empty " + mpb.isEmpty);
    }
    // letzte Konsolenfehler
    var t = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
    int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null);
    var et = System.Type.GetType("UnityEditor.LogEntry,UnityEditor"); var e = System.Activator.CreateInstance(et);
    for (int i = Mathf.Max(0, n - 6); i < n; i++) { t.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, e }); sb.AppendLine("LOG: " + et.GetField("message").GetValue(e).ToString().Split('\n')[0]); }
    t.GetMethod("EndGettingEntries").Invoke(null, null);
    return sb.ToString();
  }
}
