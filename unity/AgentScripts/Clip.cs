using System.Reflection;
using UnityEngine;

// Sucht Stellen, an denen Gelaende oder Berg-Modell ueber die Schiene ragt (Play-Modus, Bahn geladen)
public static class AgentClip
{
    public static string Run()
    {
        var gc = Object.FindFirstObjectByType<Achterbahn.GameController>();
        var cur = typeof(Achterbahn.GameController).GetField("cur", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gc);
        var tr = (Achterbahn.Track)cur.GetType().GetField("track").GetValue(cur);
        // Kollider zum Abtasten: Terrain + alle MeshRenderer im Dorf
        var added = new System.Collections.Generic.List<Component>();
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            if (t.GetComponent<TerrainCollider>() == null) { var c = t.gameObject.AddComponent<TerrainCollider>(); c.terrainData = t.terrainData; added.Add(c); }
        var dorf = GameObject.Find("Wikingerdorf");
        if (dorf != null)
            foreach (var mf in dorf.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null && mf.sharedMesh.isReadable) { var c = mf.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = mf.sharedMesh; added.Add(c); }
        Physics.SyncTransforms();
        string s = ""; int bad = 0; int lastI = -100;
        for (int i = 0; i < tr.n; i += 4)
        {
            Vector3 p = tr.pos[i].ToUnity();
            if (Physics.Raycast(p + Vector3.up * 400f, Vector3.down, out var hit, 400f) && hit.point.y > p.y - 1.5f && tr.U[i].y > .3)
            {
                bad++;
                if (i - lastI > 40) s += $"s={i * tr.ds:0}m p={p.ToString("F1")} boden={hit.point.y:F1} ({hit.collider.name})\n";
                lastI = i;
            }
        }
        foreach (var c in added) Object.Destroy(c);
        return $"{bad} Punkte im Boden\n" + s;
    }
}

