using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Gelaende: flach unter Bahn, Station und Burg (Stuetzen stehen auf y = 0), sanfte Huegel weiter weg.
    /// Malt Erdwege, ausgetretene Stellen und Waldboden und streut Grasbueschel nah an der Bahn.
    /// </summary>
    public class Ground
    {
        public Terrain terrain;
        readonly Vector3 origin; readonly float size;
        readonly List<Vector2> trackPts = new List<Vector2>();
        readonly List<Vector2> flatPts = new List<Vector2>();   // weitere flache Stellen (Burg)
        public readonly List<Vector3> dirtSpots = new List<Vector3>();      // x, z, Radius
        public readonly List<Vector4> dirtLines = new List<Vector4>();      // x0, z0, x1, z1
        public readonly List<Vector3> forestSpots = new List<Vector3>();
        readonly ThemeAssets ta;

        public Ground(TrackDef def, Track tr, Vector3 center, float radius, Transform parent, ThemeAssets ta)
        {
            this.ta = ta;
            int step = Mathf.Max(1, Mathf.RoundToInt(2f / (float)tr.ds));
            for (int i = 0; i < tr.n; i += step) { var p = tr.pos[i].ToUnity(); trackPts.Add(new Vector2(p.x, p.z)); }
            if (def.castleSpots != null) foreach (var c in def.castleSpots) { var u = new V3(c.x, 0, c.z).ToUnity(); flatPts.Add(new Vector2(u.x, u.z)); }
            if (def.flatSpots != null) foreach (var c in def.flatSpots) { var u = new V3(c.x, 0, c.z).ToUnity(); flatPts.Add(new Vector2(u.x, u.z)); }
            size = Mathf.Max(1400f, radius * 4.4f);
            origin = new Vector3(center.x - size / 2, 0, center.z - size / 2);
            if (ta == null || ta.terrain == null || ta.layers == null) return;

            const int hr = 513;
            var td = new TerrainData { heightmapResolution = hr };
            float offset = def.lake != null ? def.lake[4] + 1.5f : 0f;   // Gelaende darf fuer den See unter 0 gehen
            float sizeY = (def.volcano != null ? def.volcano[3] + 20f : 45f) + offset;
            td.size = new Vector3(size, sizeY, size);
            if (def.lake != null)
            {
                var lu = new V3(def.lake[0], 0, def.lake[1]).ToUnity();
                lakeC = new Vector2(lu.x, lu.z); lakeR = new Vector2(def.lake[2], def.lake[3]); lakeDepth = def.lake[4]; hasLake = true;
            }
            baseOffset = offset;
            Vector2 vc = Vector2.zero; float vr = 1, vh = 0;
            if (def.volcano != null) { var u = new V3(def.volcano[0], 0, def.volcano[1]).ToUnity(); vc = new Vector2(u.x, u.z); vr = def.volcano[2]; vh = def.volcano[3]; }
            // Abstandsfeld grob (129 x 129), dann fein interpoliert
            const int cr = 129; var dist = new float[cr, cr];
            for (int y = 0; y < cr; y++) for (int x = 0; x < cr; x++)
            {
                Vector2 w = new Vector2(origin.x + x / (cr - 1f) * size, origin.z + y / (cr - 1f) * size);
                float d = 1e9f;
                foreach (var q in trackPts) { float dd = (q - w).sqrMagnitude; if (dd < d) d = dd; }
                d = Mathf.Sqrt(d);
                foreach (var q in flatPts) d = Mathf.Min(d, Mathf.Max(0, (q - w).magnitude - 20f));
                dist[y, x] = d;
            }
            var h = new float[hr, hr];
            for (int y = 0; y < hr; y++) for (int x = 0; x < hr; x++)
            {
                float fx = x / (hr - 1f), fy = y / (hr - 1f);
                float gx = fx * (cr - 1), gy = fy * (cr - 1); int ix = Mathf.Min(cr - 2, (int)gx), iy = Mathf.Min(cr - 2, (int)gy);
                float tx = gx - ix, ty = gy - iy;
                float d = Mathf.Lerp(Mathf.Lerp(dist[iy, ix], dist[iy, ix + 1], tx), Mathf.Lerp(dist[iy + 1, ix], dist[iy + 1, ix + 1], tx), ty);
                float wx = origin.x + fx * size, wz = origin.z + fy * size;
                float n = Mathf.PerlinNoise(wx / 260f + 31.7f, wz / 260f + 11.3f) * .7f + Mathf.PerlinNoise(wx / 95f + 3.1f, wz / 95f + 7.9f) * .3f;
                float mask = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(32f, 110f, d));
                float edge = Mathf.Min(Mathf.Min(fx, 1 - fx), Mathf.Min(fy, 1 - fy));
                mask *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0f, .08f, edge));
                float m = Mathf.Clamp01(n * n * 1.1f) * mask * 45f;   // Meter
                if (vh > 0)
                {
                    float r = new Vector2(wx - vc.x, wz - vc.y).magnitude / vr;
                    float ridge = 1 + .12f * (Mathf.PerlinNoise(wx / 30f, wz / 30f) - .5f) + .06f * Mathf.Sin(Mathf.Atan2(wz - vc.y, wx - vc.x) * 9f);
                    float cone = r < 1.4f ? Mathf.Pow(Mathf.Clamp01(1 - r / 1.4f), 1.7f) * vh * ridge : 0;
                    if (r < .1f) cone = vh * .93f - (.1f - r) * vh * 1.2f;   // Krater
                    m = Mathf.Max(m, cone * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0f, .08f, edge)));
                }
                if (hasLake)
                {
                    float e = LakeE(wx, wz) * (1 + .07f * (Mathf.PerlinNoise(wx / 18f + 40, wz / 18f + 9) - .5f));
                    if (e < 1.3f)
                    {
                        float bottom = -lakeDepth * (1 - e * e * .55f);
                        m = Mathf.Lerp(bottom, m, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1.3f, e)));
                    }
                }
                float edgeF = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0f, .08f, edge));
                h[y, x] = Mathf.Clamp01((m + offset) * edgeF / sizeY);
            }
            td.SetHeights(0, 0, h);
            td.terrainLayers = ta.layers;
            td.alphamapResolution = 1024;
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Gelaende"; go.transform.SetParent(parent, false); go.transform.position = origin - Vector3.up * offset;
            terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = ta.terrain;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 3; terrain.basemapDistance = 2000;
            var col = go.GetComponent<TerrainCollider>(); if (col != null) Object.Destroy(col);
        }

        public float Height(float x, float z) => terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;

        // ---- See ----
        public bool hasLake; public Vector2 lakeC, lakeR; public float lakeDepth, baseOffset;
        public const float WaterLevel = -.6f;
        /// <summary>Elliptischer Abstand zum Seemittelpunkt: 0 = Mitte, 1 = Uferlinie.</summary>
        public float LakeE(float x, float z) => hasLake ? new Vector2((x - lakeC.x) / lakeR.x, (z - lakeC.y) / lakeR.y).magnitude : 99f;
        public bool InWater(float x, float z) => hasLake && Height(x, z) < WaterLevel + .15f && LakeE(x, z) < 1.4f;

        /// <summary>Texturen malen: Wiese, Graswechsel, Erdwege, ausgetretene Streifen unter der Bahn, Waldboden.</summary>
        public void Paint(bool volcanicRock = false, bool jungle = false)
        {
            if (terrain == null) return;
            var td = terrain.terrainData; int R = td.alphamapResolution; int L = td.terrainLayers.Length;
            var a = new float[R, R, L];
            float cell = size / R;
            for (int y = 0; y < R; y++) for (int x = 0; x < R; x++)
            {
                float wx = origin.x + (x + .5f) * cell, wz = origin.z + (y + .5f) * cell;
                float m = Mathf.Clamp01((Mathf.PerlinNoise(wx / 40f + 5, wz / 40f + 9) - .45f) * 2.2f);
                a[y, x, 0] = 1 - m * .7f; a[y, x, 1] = m * .7f;
                if (jungle) { a[y, x, 0] *= .35f; a[y, x, 1] *= .6f; a[y, x, 3] = 1 - a[y, x, 0] - a[y, x, 1]; }
                float hw = td.GetInterpolatedHeight((x + .5f) / R, (y + .5f) / R) + terrain.transform.position.y;
                if (hasLake && hw < 1.2f && LakeE(wx, wz) < 1.45f)
                {
                    float mud = Mathf.Clamp01((1.2f - hw) / 1.6f);   // Schlammufer und Seegrund
                    for (int k = 0; k < L; k++) a[y, x, k] *= 1 - mud;
                    a[y, x, 2] += mud;
                }
                if (volcanicRock && L > 4)
                {
                    float hgt = hw;
                    float rk = Mathf.Clamp01((hgt - 14f) / 22f + (Mathf.PerlinNoise(wx / 25f, wz / 25f) - .5f) * .6f);
                    for (int k = 0; k < L; k++) a[y, x, k] *= 1 - rk;
                    a[y, x, 4] += rk;
                }
            }
            void Splat(float wx, float wz, float rad, int layer, float strength)
            {
                int cx = Mathf.RoundToInt((wx - origin.x) / cell), cz = Mathf.RoundToInt((wz - origin.z) / cell), r = Mathf.CeilToInt(rad / cell) + 1;
                for (int y = cz - r; y <= cz + r; y++) for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= R || y >= R) continue;
                    float d = new Vector2((x + .5f) * cell + origin.x - wx, (y + .5f) * cell + origin.z - wz).magnitude;
                    float w = Mathf.Clamp01(1 - d / rad) * strength;
                    if (w <= 0) continue;
                    w *= .75f + .5f * Mathf.PerlinNoise(x * .35f, y * .35f);
                    w = Mathf.Clamp01(w);
                    for (int k = 0; k < L; k++) a[y, x, k] *= 1 - w;
                    a[y, x, layer] += w;
                }
            }
            for (int i = 0; i < trackPts.Count; i++) Splat(trackPts[i].x, trackPts[i].y, 3.2f, 2, .55f);
            foreach (var f in forestSpots) Splat(f.x, f.y, f.z, 3, .9f);
            foreach (var s in dirtSpots) Splat(s.x, s.y, s.z, 2, .95f);
            foreach (var l in dirtLines)
            {
                Vector2 p0 = new Vector2(l.x, l.y), p1 = new Vector2(l.z, l.w); float len = (p1 - p0).magnitude;
                for (float s = 0; s < len; s += 1.2f) { var p = Vector2.Lerp(p0, p1, s / len); Splat(p.x, p.y, 2.2f, 2, .9f); }
            }
            td.SetAlphamaps(0, 0, a);
        }

        /// <summary>Grasbueschel bis 34 m neben der Bahn, in Kacheln fuer die Sichtbarkeitspruefung.</summary>
        public void Grass(Transform parent, Scenery.Clearance clr)
        {
            if (ta == null || ta.grassCard == null) return;
            var chunks = new Dictionary<Vector2Int, MeshBuilder>();
            const float range = 34f, chunk = 48f;
            var rnd = new System.Random(5);
            float R() => (float)rnd.NextDouble();
            for (int i = 0; i < trackPts.Count; i++)
            {
                Vector2 c = trackPts[i];
                for (int k = 0; k < 60; k++)
                {
                    float ang = R() * Mathf.PI * 2, d = 2.6f + R() * range;
                    Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d;
                    if (!clr.Free(p, 2.4f)) continue;
                    var key = new Vector2Int(Mathf.FloorToInt(p.x / chunk), Mathf.FloorToInt(p.y / chunk));
                    if (!chunks.TryGetValue(key, out var mb)) chunks[key] = mb = new MeshBuilder();
                    float y = Height(p.x, p.y) - .03f, hgt = .45f + R() * .4f, wid = 1.0f + R() * .6f;
                    if (hasLake && y < WaterLevel + .25f) continue;
                    Vector3 at = new Vector3(p.x, y, p.y);
                    float rot = R() * 180f;
                    for (int q = 0; q < 3; q++)
                    {
                        float a = (rot + q * 60f) * Mathf.Deg2Rad;
                        Vector3 side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        mb.Card(at, Vector3.up, side, hgt, wid, Vector3.up);
                    }
                }
            }
            foreach (var kv in chunks)
            {
                var go = kv.Value.ToObject("Gras", ta.grassCard, parent, false);
                go.GetComponent<MeshRenderer>().receiveShadows = true;
            }
        }
    }
}
