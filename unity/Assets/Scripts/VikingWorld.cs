using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>
    /// Thema „wikinger“ mit dem Viking-Village-Asset: Dorf zwischen Hin- und Rueckweg, Hafen am Fjord mit Langschiffen,
    /// Steinkreis, Feuer, Berge im Hintergrund. Rein optisch.
    /// </summary>
    public static class VikingWorld
    {
        static Transform root;
        static ThemeAssets ta;

        /// <summary>Setzt ein Prefab mit der Unterkante auf den Boden, Mitte auf <paramref name="at"/>.</summary>
        static GameObject Put(GameObject prefab, Ground g, Vector3 at, float yaw, float scale = 1f, float sink = .15f)
        {
            if (prefab == null) return null;
            var inst = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            inst.transform.localScale *= scale;
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Object.Destroy(inst); return null; }
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var pivot = new GameObject(prefab.name).transform;
            pivot.SetParent(root, false);
            float y = at.y;
            if (g != null) y = g.Height(at.x, at.z);
            pivot.SetPositionAndRotation(new Vector3(at.x, y - sink * scale, at.z), Quaternion.Euler(0, yaw, 0));
            inst.transform.SetParent(pivot, false);
            inst.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            return pivot.gameObject;
        }

        static float Size(GameObject prefab)
        {
            if (prefab == null) return 0;
            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return 0;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.size.x, b.size.z);
        }

        static T Pick<T>(T[] a) => a == null || a.Length == 0 ? default : a[Random.Range(0, a.Length)];
        static float Face(Vector2 from, Vector2 to) => Mathf.Atan2(to.x - from.x, to.y - from.y) * Mathf.Rad2Deg;

        public static bool Build(TrackDef def, Track track, Transform parent, ThemeAssets themeAssets, Scenery.Clearance clr, Ground g, Vector3 center, float radius)
        {
            var vs = VikingSet.Get();
            if (vs == null || vs.houses == null || vs.houses.Length == 0) return false;
            root = new GameObject("Wikingerdorf").transform; root.SetParent(parent, false);
            ta = themeAssets;

            Harbor(vs, clr, g);
            Village(vs, clr, g);
            Outskirts(vs, clr, g, center, radius);
            Mountains(vs, center, radius);
            return true;
        }

        // ---------- Dorf zwischen Hinweg (z 0..-21) und Rueckweg (z -72..-87) ----------
        static void Village(VikingSet vs, Scenery.Clearance clr, Ground g)
        {
            var placed = new List<(Vector2 p, float r)>();
            bool Room(Vector2 p, float r)
            {
                if (!clr.Free(p, r + 3f)) return false;
                foreach (var o in placed) if ((o.p - p).magnitude < o.r + r + 2.5f) return false;
                return true;
            }
            var fires = new List<Vector3>();

            // Dorfstrasse bei z = -46, Haeuser beidseitig mit dem Giebel zur Strasse
            const float street = -46.5f;
            for (float x = 14; x < 470; x += Random.Range(13f, 19f))
            {
                foreach (int side in new[] { -1, 1 })
                {
                    if (Random.value < .12f) continue;
                    GameObject pf;
                    float rv = Random.value;
                    if (rv < .3f) pf = Pick(vs.bigHouses);
                    else if (rv < .82f) pf = Pick(vs.houses);
                    else if (rv < .92f) pf = Pick(vs.storages);
                    else pf = Pick(vs.smithies);
                    float r = Size(pf) * .5f;
                    var p = new Vector2(x + Random.Range(-2f, 2f), street + side * (r + 4.5f + Random.Range(0f, 2f)));
                    if (!Room(p, r)) continue;
                    float yaw = side > 0 ? 180f : 0f;
                    yaw += Random.Range(-8f, 8f) + (Random.value < .5f ? 0 : 90f);
                    if (Put(pf, g, new Vector3(p.x, 0, p.y), yaw) == null) continue;
                    placed.Add((p, r));
                    Clutter(vs, g, p, r, placed);
                }
                // Feuerstellen auf der Strasse
                if (Random.value < .22f) fires.Add(new Vector3(x + 6, 0, street + Random.Range(-1.5f, 1.5f)));
            }

            // Wachtuerme an den Enden des Dorfes
            foreach (var tp in new[] { new Vector2(6, -33), new Vector2(6, -60), new Vector2(478, -36), new Vector2(478, -58) })
                if (Room(tp, 3f)) { Put(vs.tower, g, new Vector3(tp.x, 0, tp.y), Random.Range(0, 360f)); placed.Add((tp, 3f)); }
            // Tor an der Dorfstrasse beim Bahnhof
            if (vs.gate != null && Room(new Vector2(0, street), 4f)) { Put(vs.gate, g, new Vector3(0, 0, street), 90f); placed.Add((new Vector2(0, street), 4f)); }

            // Feuer
            foreach (var f in fires)
            {
                var p2 = new Vector2(f.x, f.z);
                if (!clr.Free(p2, 3f)) continue;
                float y = g.Height(f.x, f.z);
                Fx.Fire(root, new Vector3(f.x, y + .25f, f.z), ta != null ? ta.fire : null, 2.4f);
                for (int k = 0; k < 7; k++)
                {
                    float a = k / 7f * Mathf.PI * 2;
                    Put(Pick(vs.boulders), g, new Vector3(f.x + Mathf.Cos(a) * 1.3f, 0, f.z + Mathf.Sin(a) * 1.3f), Random.Range(0, 360f), .35f, .1f);
                }
                var lg = new GameObject("Feuerschein"); lg.transform.SetParent(root, false); lg.transform.position = new Vector3(f.x, y + 1.6f, f.z);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.color = new Color(1f, .6f, .28f); l.shadows = LightShadows.None;
                lg.AddComponent<Flicker>().baseIntensity = 3f;
            }
        }

        // Kleinkram ums Haus: Faesser, Holzstapel, Zaeune, Schilde
        static void Clutter(VikingSet vs, Ground g, Vector2 house, float r, List<(Vector2 p, float r)> placed)
        {
            int n = Random.Range(3, 7);
            for (int k = 0; k < n; k++)
            {
                float a = Random.Range(0f, Mathf.PI * 2), d = r + Random.Range(.8f, 3f);
                var p = house + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                GameObject pf;
                float rv = Random.value;
                if (rv < .35f) pf = Pick(vs.barrels);
                else if (rv < .55f) pf = Pick(vs.logs);
                else if (rv < .68f) pf = Pick(vs.planks);
                else if (rv < .8f) pf = Pick(vs.sheds);
                else if (rv < .9f) pf = Pick(vs.weapons);
                else pf = Pick(vs.plants);
                Put(pf, g, new Vector3(p.x, 0, p.y), Random.Range(0, 360f));
            }
            if (Random.value < .45f)
            {
                float a = Random.Range(0f, Mathf.PI * 2);
                var p = house + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r + 4f);
                Put(Pick(vs.fences), g, new Vector3(p.x, 0, p.y), a * Mathf.Rad2Deg + 90);
            }
        }

        // ---------- Hafen am Fjord (Seeufer bei z ~ 12) ----------
        static void Harbor(VikingSet vs, Scenery.Clearance clr, Ground g)
        {
            if (!g.hasLake) return;
            float shoreZ(float x)
            {
                // Uferlinie suchen: erster Punkt Richtung See unter Wasser
                for (float z = 0; z < 80; z += .5f) if (g.Height(x, z) < Ground.WaterLevel) return z;
                return 40;
            }
            // Stege mit Langschiffen
            float[] piers = { 60, 130, 205, 290, 365, 430 };
            foreach (float px in piers)
            {
                float sz = shoreZ(px);
                float len = 16f;
                var plank = vs.misc != null && vs.misc.Length > 5 ? vs.misc[5] : null;   // pf_plank_02 (4,4 x 3,3)
                for (float d = -3; d < len; d += 3.2f)
                {
                    var at = new Vector3(px, Ground.WaterLevel + 1.0f, sz + d);
                    Put(plank, null, at, 90f, 1f, 0f);
                    if (d > 0 && ((int)(d / 3.2f)) % 2 == 0)
                        foreach (int s in new[] { -1, 1 }) Put(vs.misc[3], null, new Vector3(px + s * 1.9f, Ground.WaterLevel - 2.2f, sz + d), 0, 1f, 0f);   // Pfaehle
                }
                // Langschiffe beidseits des Stegs
                var boat = Pick(vs.boats);
                var b1 = Put(boat, null, new Vector3(px - 6.5f, Ground.WaterLevel - .35f, sz + len * .55f), Random.Range(-6f, 6f), 1f, 0f);
                if (b1 != null) b1.AddComponent<ShipBob>();
                if (Random.value < .7f)
                {
                    var b2 = Put(Pick(vs.boats), null, new Vector3(px + 7f, Ground.WaterLevel - .35f, sz + len * .5f), 180 + Random.Range(-6f, 6f), 1f, 0f);
                    if (b2 != null) b2.AddComponent<ShipBob>();
                }
                // Ufer: Faesser, Fischgestelle
                for (int k = 0; k < 5; k++)
                {
                    float x = px + Random.Range(-12f, 12f), z = sz - Random.Range(2f, 7f);
                    if (!clr.Free(new Vector2(x, z), 2.5f)) continue;
                    Put(Random.value < .4f ? vs.fishStick : Pick(vs.barrels), g, new Vector3(x, 0, z), Random.Range(0, 360f));
                }
            }
            // Kran am mittleren Steg
            {
                float x = 250, sz = shoreZ(x);
                if (clr.Free(new Vector2(x, sz - 4), 6f)) Put(vs.crane, g, new Vector3(x, 0, sz - 3), 180f);
            }
            // Zwei Schiffe draussen auf dem Fjord
            foreach (var bp in new[] { new Vector3(150, 0, 52), new Vector3(330, 0, 60) })
            {
                var b = Put(vs.boats.Length > 1 ? vs.boats[1] : vs.boats[0], null, new Vector3(bp.x, Ground.WaterLevel - .35f, bp.z), Random.Range(60f, 120f), 1.1f, 0f);
                if (b != null) { var bob = b.AddComponent<ShipBob>(); bob.drift = new Vector3(Random.Range(-.5f, .5f), 0, 0); }
            }
        }

        // ---------- Umland: Steinkreis, Runensteine, Felsen, Farne, Hoefe jenseits des Rueckwegs ----------
        static void Outskirts(VikingSet vs, Scenery.Clearance clr, Ground g, Vector3 center, float radius)
        {
            // Steinkreis noerdlich des Rueckwegs
            var sc = new Vector2(250, -122);
            for (int k = 0; k < 11; k++)
            {
                float a = k / 11f * Mathf.PI * 2;
                var p = sc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 11f;
                if (clr.Free(p, 2f)) Put(Pick(vs.menhirs), g, new Vector3(p.x, 0, p.y), Face(p, sc) + Random.Range(-10, 10), Random.Range(.9f, 1.3f), .4f);
            }
            if (clr.Free(sc, 3f)) Put(vs.runeStone, g, new Vector3(sc.x, 0, sc.y), 0, 1.6f, .2f);
            // Hoefe noerdlich
            for (int k = 0, n = 0; k < 200 && n < 10; k++)
            {
                var p = new Vector2(Random.Range(-20f, 520f), Random.Range(-170f, -105f));
                if ((p - sc).magnitude < 25) continue;
                var pf = Random.value < .6f ? Pick(vs.houses) : Pick(vs.storages);
                float r = Size(pf) * .5f;
                if (!clr.Free(p, r + 6f)) continue;
                Put(pf, g, new Vector3(p.x, 0, p.y), Random.Range(0, 360f)); n++;
                clr.Block(new Rect(p.x - r, p.y - r, 2 * r, 2 * r));
                Clutter(vs, g, p, r, null);
            }
            // Runensteine an der Strecke, Totenkopfstangen, Felsen
            for (int k = 0; k < 60; k++)
            {
                float a = Random.Range(0f, Mathf.PI * 2), d = Random.Range(radius * .2f, radius * 1.2f);
                var p = new Vector2(center.x + Mathf.Cos(a) * d * 1.3f, center.z + Mathf.Sin(a) * d * .6f);
                if (!clr.Free(p, 4f) || g.InWater(p.x, p.y)) continue;
                float rv = Random.value;
                var pf = rv < .15f ? vs.runeStone : rv < .25f ? vs.skullStick : Pick(vs.boulders);
                Put(pf, g, new Vector3(p.x, 0, p.y), Random.Range(0, 360f), pf == vs.runeStone ? 1.3f : Random.Range(.8f, 1.6f), .3f);
            }
            // Farne und Bluehpflanzen
            for (int k = 0; k < 420; k++)
            {
                var p = new Vector2(center.x + Random.Range(-radius, radius) * 1.15f, center.z + Random.Range(-radius, radius) * .55f);
                if (!clr.Free(p, 2.5f) || g.InWater(p.x, p.y)) continue;
                var go = Put(Random.value < .7f ? Pick(vs.plants) : Pick(vs.grass), g, new Vector3(p.x, 0, p.y), Random.Range(0, 360f), Random.Range(.8f, 1.3f), .05f);
                if (go != null) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ---------- Berge aus dem Viking-Village-Gelaende als ferne Kulisse ----------
        static void Mountains(VikingSet vs, Vector3 center, float radius)
        {
            if (vs.mountains == null) return;
            // Nur der Fernbereich (terrain_far_01): ein Tal, das zum Meer (+z) offen ist. Die Luecke fuer das Dorf
            // (Modell x 175..356, z 345..474) wird 3,8-fach vergroessert und nimmt die ganze Anlage auf.
            const float s = 3.8f;
            var inst = Object.Instantiate(vs.mountains, root);
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                if (!mf.name.Contains("far")) mf.gameObject.SetActive(false);
            inst.transform.localScale = Vector3.one * s;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.position = new Vector3(center.x, -24f, center.z) - new Vector3(262, 0, 405) * s;
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        }
    }

    /// <summary>Leichtes Schaukeln von Schiffen auf dem Wasser.</summary>
    public class ShipBob : MonoBehaviour
    {
        public Vector3 drift;
        Vector3 p0; Quaternion r0; float seed;
        void Start() { p0 = transform.position; r0 = transform.rotation; seed = Random.value * 10f; }
        void Update()
        {
            float t = Time.time + seed;
            transform.position = p0 + Vector3.up * Mathf.Sin(t * .9f) * .12f + drift * Mathf.Sin(t * .05f) * 20f;
            transform.rotation = r0 * Quaternion.Euler(Mathf.Sin(t * .7f) * 1.6f, 0, Mathf.Sin(t * .55f + 1) * 2.4f);
        }
    }
}
