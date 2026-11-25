using UnityEngine;
using System.Text;
using System.Collections.Generic;
public static class BoxErr {
  public static string Run() {
    var sb = new StringBuilder(); var seen = new HashSet<string>();
    var t = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
    int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null);
    var et = System.Type.GetType("UnityEditor.LogEntry,UnityEditor"); var e = System.Activator.CreateInstance(et);
    for (int i = 0; i < n; i++) { t.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, e }); var m = et.GetField("message").GetValue(e).ToString(); var k = m.Length > 400 ? m.Substring(0, 400) : m; if (seen.Add(k)) sb.AppendLine("LOG: " + k); }
    t.GetMethod("EndGettingEntries").Invoke(null, null);
    var d = Object.FindFirstObjectByType<Achterbahn.Boxauto.BoxautoDemo>();
    sb.AppendLine("demo " + (d != null) + " wagen " + (d != null && d.wagen != null ? d.wagen.Length : -1) + " anzahl " + (d != null ? d.Anzahl : -1));
    return sb.ToString();
  }
}
