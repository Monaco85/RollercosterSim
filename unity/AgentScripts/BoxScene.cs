using UnityEngine;
using System.Text;
public static class BoxScene {
  public static string Run() {
    var sb = new StringBuilder();
    foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
      if (mf.transform.parent != null && mf.transform.parent.name == "Gluehbirnen") continue;
      var m = mf.sharedMesh; var r = mf.GetComponent<Renderer>();
      sb.AppendLine(mf.name + ": " + (m == null ? "KEIN MESH" : m.vertexCount + " v, " + m.bounds.size.ToString("F1")) + " mat " + (r.sharedMaterial ? r.sharedMaterial.name + " kw " + string.Join(",", r.sharedMaterial.shaderKeywords) : "null"));
    }
    return sb.ToString();
  }
}
