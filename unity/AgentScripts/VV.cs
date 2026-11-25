using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VV
{
    const string F = "Assets/Viking Village";

    public static string Prefabs()
    {
        var g = AssetDatabase.FindAssets("t:Prefab", new[] { F });
        string s = g.Length + " Prefabs\n";
        foreach (var id in g)
        {
            var p = AssetDatabase.GUIDToAssetPath(id);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var b = new Bounds(); bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            int miss = go.GetComponentsInChildren<Component>(true).Count(c => c == null);
            s += p.Substring(F.Length + 1) + "  size=" + b.size.ToString("F1") + " c=" + b.center.ToString("F1") + (miss > 0 ? " missing=" + miss : "") + "\n";
        }
        return s;
    }

    public static string Shaders()
    {
        var mats = AssetDatabase.FindAssets("t:Material", new[] { F });
        var grp = mats.Select(id => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id)))
            .GroupBy(m => m.shader == null ? "null" : m.shader.name + (m.shader.isSupported ? "" : " (NICHT unterstuetzt)"));
        return mats.Length + " Materialien\n" + string.Join("\n", grp.Select(g2 => g2.Count() + "  " + g2.Key));
    }
}
public static class VV2
{
    public static string Mount()
    {
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Viking Village/Models/Terrain/terrain_01.fbx");
        string s = "";
        foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshFilter>(true)) s += r.name + " " + r.sharedMesh.bounds.size.ToString("F0") + " c=" + r.sharedMesh.bounds.center.ToString("F0") + " v=" + r.sharedMesh.vertexCount + " ls=" + r.transform.lossyScale + "\n";
        return s;
    }
}
public static class VV3
{
    // Hoehenkarte des Fernbereichs: 20x20 Raster, Maximalhoehe je Zelle
    public static string Grid()
    {
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Viking Village/Models/Terrain/terrain_01.fbx");
        var sb = new System.Text.StringBuilder();
        foreach (var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
        {
            var m = mf.sharedMesh; var b = m.bounds; int N = 20; var h = new float[N, N];
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) h[i, j] = -999;
            foreach (var v in m.vertices)
            {
                int i = UnityEngine.Mathf.Clamp((int)((v.x - b.min.x) / b.size.x * N), 0, N - 1), j = UnityEngine.Mathf.Clamp((int)((v.z - b.min.z) / b.size.z * N), 0, N - 1);
                if (v.y > h[i, j]) h[i, j] = v.y;
            }
            sb.AppendLine(mf.name + " min=" + b.min.ToString("F0") + " max=" + b.max.ToString("F0") + " (Zeilen z von oben=max)");
            for (int j = N - 1; j >= 0; j--) { for (int i = 0; i < N; i++) sb.Append(h[i, j] < -900 ? "   ." : ((int)h[i, j]).ToString().PadLeft(4)); sb.AppendLine(); }
        }
        return sb.ToString();
    }
}
public static class VV4
{
    public static string Bad()
    {
        string s = "";
        foreach (var id in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/Viking Village" }))
        {
            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(id); var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p);
            if (m.shader == null || !m.shader.isSupported || m.shader.name.Contains("Error")) s += p + " -> " + (m.shader ? m.shader.name : "null") + "\n";
        }
        foreach (var id in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Viking Village/Book of the Dead" }))
        {
            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(id); var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
            foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true)) foreach (var m in r.sharedMaterials) if (m == null || m.shader.name.Contains("Error")) s += "  " + p + " hat " + (m ? m.name : "null") + "\n";
        }
        return s;
    }
}
