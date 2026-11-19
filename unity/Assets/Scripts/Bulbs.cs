using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>Muster fuer Bulb/BulbHalo-Shader (siehe BulbPattern.hlsl).</summary>
    public static class BulbMode
    {
        public const float Steady = 0, Chase = 1, Twinkle = 2, Rainbow = 3, Alternate = 4, Wave = 5, Show = 6, Breathe = 7, Rise = 8;
    }

    /// <summary>
    /// Sammelt Gluehbirnen fuer ein Bauteil und erzeugt daraus zwei Meshes: die Birnen selbst und die Lichthoefe.
    /// Das Blinken rechnet der Shader, pro Lampe gibt es kein Skript und keine Lichtquelle.
    /// </summary>
    public class BulbSet
    {
        readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<Vector4> d = new List<Vector4>();
        readonly List<int> t = new List<int>();
        readonly List<Vector3> hv = new List<Vector3>();
        readonly List<Color> hc = new List<Color>();
        readonly List<Vector2> huv = new List<Vector2>(), hsz = new List<Vector2>();
        readonly List<Vector4> hd = new List<Vector4>();
        readonly List<int> ht = new List<int>();
        float maxHalo;
        public int Count { get; private set; }

        static readonly Vector3[] dirs;
        static readonly int[] tris;
        static BulbSet()
        {
            // kleine Kugel: 4 Ringe x 8 + Pole; Umlaufsinn wird pro Dreieck nach aussen gedreht
            const int RINGS = 4, SEG = 8;
            var dl = new List<Vector3> { Vector3.up };
            for (int r = 1; r <= RINGS; r++)
            {
                float th = r / (float)(RINGS + 1) * Mathf.PI;
                for (int i = 0; i < SEG; i++) { float ph = (i + (r % 2) * .5f) / SEG * Mathf.PI * 2; dl.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph))); }
            }
            dl.Add(Vector3.down);
            dirs = dl.ToArray();
            int last = dl.Count - 1;
            var tl = new List<int>();
            void T(int a, int b, int c)
            {
                Vector3 nn = Vector3.Cross(dirs[b] - dirs[a], dirs[c] - dirs[a]);
                if (Vector3.Dot(nn, dirs[a] + dirs[b] + dirs[c]) < 0) { tl.Add(a); tl.Add(c); tl.Add(b); } else { tl.Add(a); tl.Add(b); tl.Add(c); }
            }
            int R(int ring, int i) => 1 + ring * SEG + ((i % SEG) + SEG) % SEG;
            for (int i = 0; i < SEG; i++) T(0, R(0, i), R(0, i + 1));
            for (int r = 0; r < RINGS - 1; r++)
                for (int i = 0; i < SEG; i++)
                {
                    // versetzte Ringe: Dreiecksstreifen
                    if (r % 2 == 1) { T(R(r, i), R(r, i + 1), R(r + 1, i)); T(R(r, i + 1), R(r + 1, i + 1), R(r + 1, i)); }
                    else { T(R(r, i), R(r, i + 1), R(r + 1, i + 1)); T(R(r, i), R(r + 1, i + 1), R(r + 1, i)); }
                }
            for (int i = 0; i < SEG; i++) T(last, R(RINGS - 1, i), R(RINGS - 1, i + 1));            tris = tl.ToArray();
        }

        /// <param name="phase">Lage in der Kette (0..1) oder Winkel/2pi</param>
        /// <param name="aux">Anzahl Lampen der Kette (Lauflicht) oder Radius 0..1 (Show)</param>
        /// <param name="halo">Radius des Lichthofs in Metern (0 = keiner)</param>
        public void Add(Vector3 p, float r, Color col, float mode, float phase = 0, float aux = 1, float halo = -1, float seed = -1)
        {
            if (seed < 0) seed = Random.value * 100f;
            if (halo < 0) halo = r * 7f;
            var data = new Vector4(phase, mode, seed, aux);
            int b = v.Count;
            foreach (var dd in dirs) { v.Add(p + dd * r); n.Add(dd); c.Add(col); d.Add(data); }
            foreach (int i in tris) t.Add(b + i);
            if (halo > 0)
            {
                int h = hv.Count;
                for (int k = 0; k < 4; k++) { hv.Add(p); hc.Add(col); hd.Add(data); hsz.Add(new Vector2(halo, 0)); }
                huv.Add(new Vector2(-1, -1)); huv.Add(new Vector2(1, -1)); huv.Add(new Vector2(1, 1)); huv.Add(new Vector2(-1, 1));
                ht.Add(h); ht.Add(h + 1); ht.Add(h + 2); ht.Add(h); ht.Add(h + 2); ht.Add(h + 3);
                maxHalo = Mathf.Max(maxHalo, halo);
            }
            Count++;
        }

        /// <summary>Lampenreihe von a nach b (Lauflicht laeuft von a nach b).</summary>
        public void Line(Vector3 a, Vector3 b, float spacing, float r, Color col, float mode, float halo = -1)
        {
            int cnt = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / spacing));
            for (int i = 0; i <= cnt; i++) Add(Vector3.Lerp(a, b, i / (float)cnt), r, col, mode, i / (float)cnt, cnt, halo, i);
        }

        public void Build(string name, Transform parent)
        {
            if (Count == 0) return;
            var ta = ThemeAssets.Get();
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetUVs(1, d); m.SetTriangles(t, 0); m.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = ta != null ? ta.bulb : null;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (hv.Count == 0) return;
            var hm = new Mesh { name = name + " Hof", indexFormat = IndexFormat.UInt32 };
            hm.SetVertices(hv); hm.SetColors(hc); hm.SetUVs(0, huv); hm.SetUVs(1, hd); hm.SetUVs(2, hsz); hm.SetTriangles(ht, 0);
            hm.RecalculateBounds(); var bb = hm.bounds; bb.Expand(maxHalo * 2 + 4); hm.bounds = bb;
            var hg = new GameObject(name + " Hof"); hg.transform.SetParent(parent, false);
            hg.AddComponent<MeshFilter>().sharedMesh = hm;
            var hr = hg.AddComponent<MeshRenderer>(); hr.sharedMaterial = ta != null ? ta.halo : null;
            hr.shadowCastingMode = ShadowCastingMode.Off; hr.receiveShadows = false; hr.lightProbeUsage = LightProbeUsage.Off; hr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }
}
