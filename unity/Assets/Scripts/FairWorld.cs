using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    // ---------------- kleine Bewegungs-Bausteine (nur Optik) ----------------
    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up; public float speed = 20f;
        void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); }
    }

    public class Upright : MonoBehaviour
    {
        Quaternion r; void Start() { r = transform.rotation; }
        void LateUpdate() { transform.rotation = r; }
    }

    public class Swing : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward; public float amp = 30f, period = 6f, cycle = 0f; Quaternion b; float ph;
        void Start() { b = transform.localRotation; ph = Random.value * 50f; }
        void Update()
        {
            float t = Time.time + ph;
            float env = cycle > 0 ? Mathf.Clamp01(Mathf.Sin(t / cycle * Mathf.PI * 2) * 1.6f + .4f) : 1f;
            transform.localRotation = b * Quaternion.AngleAxis(amp * env * Mathf.Sin(t / period * Mathf.PI * 2), axis);
        }
    }

    public class Bob : MonoBehaviour
    {
        public float amp = .25f, speed = 2f, phase; Vector3 b;
        void Start() { b = transform.localPosition; }
        void Update() { transform.localPosition = b + Vector3.up * amp * Mathf.Sin(Time.time * speed + phase); }
    }

    /// <summary>Freifallturm: langsam hoch, kurz warten, fallen, abbremsen, unten warten.</summary>
    public class DropRide : MonoBehaviour
    {
        public float top = 45f, bottom = 1.2f; float ph;
        void Start() { ph = Random.value * 20f; }
        void Update()
        {
            float t = (Time.time + ph) % 20f, y;
            if (t < 9f) y = Mathf.Lerp(bottom, top, Mathf.SmoothStep(0, 1, t / 9f));
            else if (t < 12f) y = top;
            else if (t < 14.5f) { float k = (t - 12f) / 2.5f; y = top - (top - bottom - 4f) * Mathf.Min(1, k * k); }
            else if (t < 16f) y = Mathf.Lerp(bottom + 4f, bottom, Mathf.SmoothStep(0, 1, (t - 14.5f) / 1.5f));
            else y = bottom;
            var p = transform.localPosition; p.y = y; transform.localPosition = p;
        }
    }

    /// <summary>Lauflicht: zwei Lampengruppen wechseln sich ab.</summary>
    public class Chaser : MonoBehaviour
    {
        public Material a, b; public Color ca, cb; public float k = 4f, rate = 2.5f;
        void Update()
        {
            bool on = Mathf.Repeat(Time.time * rate, 2f) < 1f;
            if (a != null) a.SetColor("_EmissionColor", ca * (on ? k : k * .15f));
            if (b != null) b.SetColor("_EmissionColor", cb * (on ? k * .15f : k));
        }
    }

    /// <summary>Autoscooter: faehrt in der Halle herum und dreht bei Wand oder zufaellig ab.</summary>
    public class Scooter : MonoBehaviour
    {
        public Rect area; float yaw, speed, turn, next;
        void Start() { yaw = Random.value * 360f; speed = Random.Range(2.2f, 3.6f); }
        void Update()
        {
            if (Time.time > next) { turn = Random.Range(-90f, 90f); next = Time.time + Random.Range(.6f, 2.2f); }
            yaw += turn * Time.deltaTime;
            var p = transform.localPosition; var d = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            p += d * speed * Time.deltaTime;
            if (p.x < area.xMin || p.x > area.xMax) { yaw = -yaw; p.x = Mathf.Clamp(p.x, area.xMin, area.xMax); }
            if (p.z < area.yMin || p.z > area.yMax) { yaw = 180f - yaw; p.z = Mathf.Clamp(p.z, area.yMin, area.yMax); }
            transform.localPosition = p; transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
    }

    /// <summary>Besucher schlendert zwischen zwei Punkten hin und her.</summary>
    public class Walker : MonoBehaviour
    {
        public Vector3 a, b; public float speed = 1.1f; float s, dir = 1, ph;
        void Start() { s = Random.value; ph = Random.value * 9f; }
        void Update()
        {
            float len = Mathf.Max(.1f, (b - a).magnitude);
            s += dir * speed * Time.deltaTime / len;
            if (s > 1) { s = 1; dir = -1; } else if (s < 0) { s = 0; dir = 1; }
            var p = Vector3.Lerp(a, b, s); p.y += Mathf.Abs(Mathf.Sin((Time.time + ph) * 5.5f)) * .04f;
            transform.position = p;
            transform.rotation = Quaternion.LookRotation((dir > 0 ? b - a : a - b).normalized, Vector3.up);
        }
    }

    /// <summary>Rummelplatz rund um die Wilde Maus. Nur Optik, ohne Einfluss auf Physik oder Sitz.</summary>
    public static class FairWorld
    {
        static readonly Vector3 up = Vector3.up;
        static Transform root; static ThemeAssets ta;
        static readonly Dictionary<Color, MeshBuilder> paint = new Dictionary<Color, MeshBuilder>();
        static readonly Dictionary<Color, MeshBuilder> glow = new Dictionary<Color, MeshBuilder>();

        static readonly Color Red = new Color(.85f, .08f, .08f), White = new Color(.95f, .94f, .9f), Yellow = new Color(.98f, .78f, .1f),
            Blue = new Color(.1f, .3f, .8f), Green = new Color(.1f, .6f, .25f), Pink = new Color(.95f, .4f, .65f), Orange = new Color(1f, .5f, .08f),
            Gold = new Color(.85f, .65f, .2f), Dark = new Color(.12f, .12f, .13f), Steel = new Color(.62f, .64f, .67f), Purple = new Color(.5f, .2f, .7f);
        static readonly Color[] Bright = { Red, Yellow, Blue, Green, Pink, Orange, Purple, White };
        static readonly Color[] Bulbs = { new Color(1f, .85f, .45f), new Color(1f, .3f, .2f), new Color(.3f, .6f, 1f), new Color(.4f, 1f, .4f), new Color(1f, .45f, .9f) };

        static Material Lit(Color c, float smooth = .4f, float metal = 0)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            return m;
        }
        static Material Emit(Color c, float k)
        {
            var b = Resources.Load<Material>("AchterbahnEmit");
            var m = b != null ? new Material(b) : Lit(c);
            m.SetColor("_BaseColor", c * .9f); m.SetFloat("_Smoothness", .8f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }
        static Material Tint(Material m, Color c) { if (m == null) return Lit(c); var x = new Material(m); x.SetColor("_BaseColor", c); return x; }

        static MeshBuilder P(Color c) { if (!paint.TryGetValue(c, out var b)) paint[c] = b = new MeshBuilder(); return b; }
        static MeshBuilder G(Color c) { if (!glow.TryGetValue(c, out var b)) glow[c] = b = new MeshBuilder(); return b; }

        // ================================================================
        public static void Build(TrackDef def, Track tr, Transform parent, ThemeAssets assets, Scenery.Clearance clr, Ground g, Vector3 center, float radius)
        {
            root = parent; ta = assets; paint.Clear(); glow.Clear();
            Plaza(clr);
            Station(clr);
            FerrisWheel(new Vector3(105, 0, 32));
            Carousel(new Vector3(-30, 0, 52));
            WaveSwinger(new Vector3(42, 0, 58));
            DropTower(new Vector3(-62, 0, -58));
            PirateShip(new Vector3(108, 0, -48));
            BumperCars(new Vector3(-66, 0, 8));
            Booths();
            LightStrings();
            Lamps();
            Entrance(new Vector3(-104, 0, 24));
            Visitors(clr);
            Balloons();

            foreach (var kv in paint) kv.Value.ToObject("Rummel " + kv.Key, Lit(kv.Key, .55f, kv.Key == Steel ? .8f : .05f), root);
            int gi = 0;
            foreach (var kv in glow) kv.Value.ToObject("Gluehbirnen " + gi++, Emit(kv.Key, 3.5f), root, false);
        }

        // ---------- Platz: Schotter mit Wegen ----------
        static void Plaza(Scenery.Clearance clr)
        {
            var mb = new MeshBuilder { tile = 3f };
            mb.Box(new Vector3(18, -.04f, 0), Quaternion.identity, new Vector3(250, .14f, 200));
            mb.ToObject("Festplatz", Tint(ta != null ? ta.path : null, new Color(1.15f, 1.08f, .98f)), root).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            clr.Block(new Rect(-108, -100, 252, 200));
            // Absperrung um die Bahn
            var fence = P(Steel);
            float[] xs = { -32, 70 }; float[] zs = { -36, 4 };
            for (float x = xs[0]; x < xs[1]; x += 2.4f)
                foreach (float z in zs) { if (z > 0 && x < 10) continue; fence.Box(new Vector3(x + 1.2f, 1.0f, z), Quaternion.identity, new Vector3(2.4f, .05f, .05f)); fence.Box(new Vector3(x, .5f, z), Quaternion.identity, new Vector3(.05f, 1.05f, .05f)); }
            for (float z = zs[0]; z < zs[1]; z += 2.4f)
                foreach (float x in xs) { if (x == xs[0] && z > -8) continue; fence.Box(new Vector3(x, 1.0f, z + 1.2f), Quaternion.identity, new Vector3(.05f, .05f, 2.4f)); fence.Box(new Vector3(x, .5f, z), Quaternion.identity, new Vector3(.05f, 1.05f, .05f)); }
        }

        // ---------- Station der Wilden Maus ----------
        static void Station(Scenery.Clearance clr)
        {
            var deck = new MeshBuilder { tile = 2f };
            deck.Box(new Vector3(-6, 1.33f, 3.6f), Quaternion.identity, new Vector3(26f, 2.66f, 4f));
            deck.Box(new Vector3(-6, 1.33f, -3.6f), Quaternion.identity, new Vector3(26f, 2.66f, 3f));
            deck.ToObject("Bahnsteig", ta != null ? ta.woodDeck : Lit(Gold), root);
            // Treppe
            for (int i = 0; i < 7; i++) P(Steel).Box(new Vector3(-6, .19f + i * .38f, 7.4f - i * .32f), Quaternion.identity, new Vector3(3, .1f, .4f));
            // Dach: Saeulen und rot-weiss gestreiftes Satteldach
            for (float x = -18; x <= 6; x += 6)
                foreach (float z in new[] { -5f, 5.5f }) P(Steel).Cylinder(new Vector3(x, 2.66f, z), .12f, 4f, 10);
            for (int i = 0; i < 26; i++)
            {
                float x = -19 + i + .5f;
                P(i % 2 == 0 ? Red : White).Gable(new Vector3(x, 6.66f, .25f), Quaternion.Euler(0, 90, 0), 11.5f, 1f, 1.6f, 0f);
            }
            // Schild mit Lichterrahmen und Maus-Kopf
            P(Dark).Box(new Vector3(-6, 9.3f, 6.2f), Quaternion.identity, new Vector3(13, 2.4f, .3f));
            P(Steel).Cylinder(new Vector3(-11, 6.6f, 6.2f), .1f, 1.6f, 8); P(Steel).Cylinder(new Vector3(-1, 6.6f, 6.2f), .1f, 1.6f, 8);
            for (float x = -12.3f; x <= .3f; x += .6f) { G(Bulbs[0]).Sphere(new Vector3(x, 10.55f, 6.4f), .09f, 6); G(Bulbs[0]).Sphere(new Vector3(x, 8.05f, 6.4f), .09f, 6); }
            Text("WILDE MAUS", new Vector3(-6, 9.3f, 6.38f), 180f, .26f, Yellow);
            var head = P(new Color(.55f, .55f, .58f));
            head.Sphere(new Vector3(-6, 12.2f, 6.1f), 1.5f, 18);
            head.Sphere(new Vector3(-7.4f, 13.5f, 6.1f), .85f, 14); head.Sphere(new Vector3(-4.6f, 13.5f, 6.1f), .85f, 14);
            P(Pink).Sphere(new Vector3(-7.4f, 13.5f, 6.75f), .55f, 12); P(Pink).Sphere(new Vector3(-4.6f, 13.5f, 6.75f), .55f, 12);
            P(Pink).Sphere(new Vector3(-6, 11.9f, 7.55f), .3f, 10);
            P(Dark).Sphere(new Vector3(-6.55f, 12.6f, 7.35f), .2f, 10); P(Dark).Sphere(new Vector3(-5.45f, 12.6f, 7.35f), .2f, 10);
            P(Steel).Cylinder(new Vector3(-6, 10.5f, 6.1f), .15f, .8f, 8);
            // Kassenhaeuschen
            Booth(new Vector3(-22, 0, 9), 0, Yellow, Red, "KASSE", false);
            clr.Block(new Rect(-34, -8, 44, 18));
        }

        static void Text(string s, Vector3 pos, float yaw, float size, Color c)
        {
            var go = new GameObject("Schrift " + s); go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font; tm.text = s; tm.fontSize = 64; tm.characterSize = size * .6f; tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = c;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        // ---------- Riesenrad ----------
        static void FerrisWheel(Vector3 c)
        {
            const float R = 21f, H = 25.6f, half = 1.3f;
            var legs = P(White);
            foreach (float x in new[] { -half - 1.2f, half + 1.2f })
            {
                legs.Beam(new Vector3(c.x + x, 0, c.z - 12), new Vector3(c.x + x, H, c.z), .6f, Vector3.right);
                legs.Beam(new Vector3(c.x + x, 0, c.z + 12), new Vector3(c.x + x, H, c.z), .6f, Vector3.right);
                legs.Beam(new Vector3(c.x + x, 8, c.z - 8), new Vector3(c.x + x, 8, c.z + 8), .3f, Vector3.right);
            }
            P(Steel).Loft(new[] { new Vector3(c.x - 3, H, c.z), new Vector3(c.x + 3, H, c.z) }, new[] { new Vector2(.5f, .5f), new Vector2(.5f, .5f) }, 1, 14, true, true, Vector3.up);
            P(Red).Box(new Vector3(c.x, .5f, c.z), Quaternion.identity, new Vector3(9, 1, 10));
            for (int i = 0; i < 3; i++) P(Steel).Box(new Vector3(c.x, .15f + i * .3f, c.z - 5.6f - i * .3f), Quaternion.identity, new Vector3(3, .1f, .4f));

            var pivot = new GameObject("Riesenrad").transform; pivot.SetParent(root, false); pivot.position = new Vector3(c.x, H, c.z);
            pivot.gameObject.AddComponent<Spin>().axis = Vector3.right; pivot.GetComponent<Spin>().speed = 5f;
            var w = new MeshBuilder(); var hub = new MeshBuilder(); var la = new MeshBuilder(); var lb = new MeshBuilder();
            const int N = 48;
            Vector3 Pt(float side, int i, float r) { float a = i / (float)N * Mathf.PI * 2; return new Vector3(side, Mathf.Sin(a) * r, Mathf.Cos(a) * r); }
            foreach (float s in new[] { -half, half })
            {
                for (int i = 0; i < N; i++)
                {
                    w.Beam(Pt(s, i, R), Pt(s, i + 1, R), .32f, Vector3.right);
                    w.Beam(Pt(s, i, R * .82f), Pt(s, i + 1, R * .82f), .2f, Vector3.right);
                    if (i % 2 == 0) { w.Beam(Pt(s * .4f, 0, 0) + new Vector3(s * .6f, 0, 0), Pt(s, i, R), .14f, Vector3.right); w.Beam(Pt(s, i, R * .82f), Pt(s, i + 1, R), .12f, Vector3.right); }
                    (i % 2 == 0 ? la : lb).Sphere(Pt(s * 1.08f, i, R), .2f, 6);
                    (i % 2 == 0 ? lb : la).Sphere(Pt(s * 1.08f, i, R * .82f), .14f, 6);
                }
            }
            for (int i = 0; i < N; i += 2) w.Beam(Pt(-half, i, R), Pt(half, i, R), .16f, Vector3.up);
            hub.Loft(new[] { new Vector3(-half - .4f, 0, 0), new Vector3(half + .4f, 0, 0) }, new[] { new Vector2(1.4f, 1.4f), new Vector2(1.4f, 1.4f) }, 1, 20, true, true, Vector3.up);
            w.ToObject("Speichen", Lit(White, .6f, .3f), pivot);
            hub.ToObject("Nabe", Lit(Red, .6f), pivot);
            Color ca = Bulbs[0], cb = Bulbs[1];
            var ma = Emit(ca, 4); var mbm = Emit(cb, 4);
            la.ToObject("Lichter A", ma, pivot, false); lb.ToObject("Lichter B", mbm, pivot, false);
            var ch = pivot.gameObject.AddComponent<Chaser>(); ch.a = ma; ch.b = mbm; ch.ca = ca; ch.cb = cb; ch.rate = 1.5f;

            // Gondeln: haengen unten, bleiben waagrecht
            for (int k = 0; k < 24; k++)
            {
                var gb = new MeshBuilder(); var roof = new MeshBuilder(); var gp = new MeshBuilder();
                Color col = Bright[k % 7];
                gp.Beam(new Vector3(0, 0, 0), new Vector3(0, -1.0f, 0), .08f, Vector3.forward);
                gb.Box(new Vector3(0, -2.1f, 0), Quaternion.identity, new Vector3(1.8f, 1.2f, 1.9f));
                gp.Box(new Vector3(0, -1.45f, 0), Quaternion.identity, new Vector3(1.9f, .1f, 2.0f));
                roof.Cone(new Vector3(0, -1.2f, 0), 1.4f, .7f, 8, 22.5f);
                roof.Disc(new Vector3(0, -1.2f, 0), 1.4f, 8, Vector3.down);
                var go = new GameObject("Gondel " + (k + 1)).transform; go.SetParent(pivot, false);
                float a = k / 24f * Mathf.PI * 2; go.localPosition = new Vector3(0, Mathf.Sin(a) * R, Mathf.Cos(a) * R);
                gb.ToObject("Kabine", Lit(col, .7f, .1f), go); roof.ToObject("Dach", Lit(White, .6f), go); gp.ToObject("Gehaenge", Lit(Steel, .6f, .8f), go);
                go.gameObject.AddComponent<Upright>();
            }
        }

        // ---------- Pferdekarussell ----------
        static void Carousel(Vector3 c)
        {
            var rot = new GameObject("Karussell").transform; rot.SetParent(root, false); rot.position = c;
            rot.gameObject.AddComponent<Spin>().speed = 24f;
            var floor = new MeshBuilder(); var gold = new MeshBuilder(); var canA = new MeshBuilder(); var canB = new MeshBuilder();
            var la = new MeshBuilder(); var lb = new MeshBuilder(); var core = new MeshBuilder();
            floor.Cylinder(new Vector3(0, 0, 0), 10f, .55f, 40);
            core.Cylinder(new Vector3(0, .55f, 0), 2.2f, 4.6f, 16);
            for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2; gold.Box(new Vector3(Mathf.Cos(a) * 2.25f, 2.8f, Mathf.Sin(a) * 2.25f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.08f, 3.8f, .6f)); }
            // gestreiftes Kegeldach mit Bogenkante
            const int S = 24; Vector3 top = new Vector3(0, 8.2f, 0);
            for (int i = 0; i < S; i++)
            {
                float a0 = i / (float)S * Mathf.PI * 2, a1 = (i + 1) / (float)S * Mathf.PI * 2;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 10.5f, 5.2f, Mathf.Sin(a0) * 10.5f), p1 = new Vector3(Mathf.Cos(a1) * 10.5f, 5.2f, Mathf.Sin(a1) * 10.5f);
                var mb = i % 2 == 0 ? canA : canB;
                mb.Tri(top, p0, p1, up); mb.Tri(top, p0, p1, -up);
                Vector3 m = (p0 + p1) * .5f, o = new Vector3(m.x, 0, m.z).normalized;
                mb.Box(m + o * .02f - up * .45f, Quaternion.LookRotation(o), new Vector3((p1 - p0).magnitude, .9f, .06f));
                (i % 2 == 0 ? la : lb).Sphere(m + o * .1f - up * .9f, .14f, 6);
                (i % 2 == 0 ? lb : la).Sphere(m * .6f + top * .4f + o * .1f, .12f, 6);
            }
            gold.Sphere(top + up * .3f, .5f, 12); gold.Cylinder(top, .08f, 1.6f, 6);
            floor.ToObject("Boden", Tint(ta != null ? ta.woodDeck : null, new Color(.9f, .7f, .55f)), rot);
            core.ToObject("Mittelsaeule", Lit(new Color(.95f, .9f, .75f), .7f), rot);
            gold.ToObject("Gold", Lit(Gold, .8f, .9f), rot);
            canA.ToObject("Dach rot", Lit(Red, .5f), rot); canB.ToObject("Dach weiss", Lit(White, .5f), rot);
            var ma = Emit(Bulbs[0], 4); var mb2 = Emit(Bulbs[0], 4);
            la.ToObject("Lichter A", ma, rot, false); lb.ToObject("Lichter B", mb2, rot, false);
            var chs = rot.gameObject.AddComponent<Chaser>(); chs.a = ma; chs.b = mb2; chs.ca = chs.cb = Bulbs[0]; chs.rate = 3f;

            // Pferde auf zwei Ringen
            var horse = HorseMesh();
            var poleMat = Lit(Gold, .85f, .9f);
            int idx = 0;
            foreach (float r in new[] { 5.2f, 8.0f })
            {
                int n = r < 6 ? 10 : 14;
                for (int i = 0; i < n; i++, idx++)
                {
                    float a = (i + (r < 6 ? .5f : 0)) / n * Mathf.PI * 2;
                    var pos = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    var pole = new MeshBuilder(); pole.Cylinder(pos + up * .55f, .05f, 4.65f, 6);
                    pole.ToObject("Stange", poleMat, rot, false);
                    var h = new GameObject("Pferd").transform; h.SetParent(rot, false);
                    h.localPosition = pos + up * 1.6f;
                    h.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
                    foreach (var part in horse)
                    {
                        var go = new GameObject(part.Key.name); go.transform.SetParent(h, false);
                        go.AddComponent<MeshFilter>().sharedMesh = part.Key;
                        var col = part.Value == 0 ? (idx % 3 == 0 ? new Color(.55f, .35f, .2f) : White) : part.Value == 1 ? Bright[idx % 6] : Gold;
                        go.AddComponent<MeshRenderer>().sharedMaterial = Lit(col, .75f, part.Value == 2 ? .8f : 0);
                    }
                    var bob = h.gameObject.AddComponent<Bob>(); bob.amp = .35f; bob.speed = 2.4f; bob.phase = idx * 1.3f;
                }
            }
            // Kasse und Zaun
            for (int i = 0; i < 40; i++) { float a = i / 40f * Mathf.PI * 2; if (i % 10 == 0) continue; P(Gold).Box(c + new Vector3(Mathf.Cos(a) * 12.5f, .5f, Mathf.Sin(a) * 12.5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.06f, 1f, 2f)); }
        }

        static List<KeyValuePair<Mesh, int>> horseCache;
        static List<KeyValuePair<Mesh, int>> HorseMesh()
        {
            var body = new MeshBuilder(); var saddle = new MeshBuilder(); var gold = new MeshBuilder();
            body.Loft(new[] { new Vector3(0, 0, -.7f), new Vector3(0, .05f, 0), new Vector3(0, .1f, .55f) }, new[] { new Vector2(.34f, .4f), new Vector2(.4f, .46f), new Vector2(.32f, .38f) }, 4, 12, true, true);
            body.Loft(new[] { new Vector3(0, .2f, .5f), new Vector3(0, .7f, .8f), new Vector3(0, .85f, 1.1f) }, new[] { new Vector2(.2f, .26f), new Vector2(.15f, .2f), new Vector2(.12f, .16f) }, 4, 10, true, true);
            body.Loft(new[] { new Vector3(0, .9f, .95f), new Vector3(0, .75f, 1.4f) }, new[] { new Vector2(.15f, .2f), new Vector2(.1f, .12f) }, 3, 10, true, true);
            foreach (float s in new[] { -.18f, .18f })
            {
                body.Loft(new[] { new Vector3(s, -.1f, .4f), new Vector3(s, -.4f, .75f), new Vector3(s, -.85f, .7f) }, new[] { new Vector2(.07f, .07f), new Vector2(.06f, .06f), new Vector2(.05f, .05f) }, 3, 6, false, true);
                body.Loft(new[] { new Vector3(s, -.15f, -.5f), new Vector3(s, -.5f, -.85f), new Vector3(s, -.8f, -1.05f) }, new[] { new Vector2(.08f, .08f), new Vector2(.06f, .06f), new Vector2(.05f, .05f) }, 3, 6, false, true);
                body.Spike(new Vector3(s * .5f, 1.05f, 1.0f), new Vector3(0, 1, -.2f), .05f, .18f, 5);
            }
            body.Loft(new[] { new Vector3(0, .1f, -.7f), new Vector3(0, -.3f, -1.0f), new Vector3(0, -.7f, -1.1f) }, new[] { new Vector2(.08f, .1f), new Vector2(.12f, .12f), new Vector2(.06f, .06f) }, 3, 6, false, true);
            saddle.Loft(new[] { new Vector3(0, .38f, -.3f), new Vector3(0, .44f, .1f) }, new[] { new Vector2(.44f, .12f), new Vector2(.44f, .12f) }, 2, 10, true, true);
            saddle.Box(new Vector3(0, .5f, .95f), Quaternion.Euler(-30, 0, 0), new Vector3(.08f, .6f, .1f));
            gold.Box(new Vector3(0, .3f, .62f), Quaternion.identity, new Vector3(.5f, .06f, .06f));
            return horseCache ?? (horseCache = new List<KeyValuePair<Mesh, int>> {
                new KeyValuePair<Mesh, int>(body.ToMesh("Pferd"), 0), new KeyValuePair<Mesh, int>(saddle.ToMesh("Sattel"), 1), new KeyValuePair<Mesh, int>(gold.ToMesh("Zaum"), 2) });
        }

        // ---------- Kettenkarussell ----------
        static void WaveSwinger(Vector3 c)
        {
            P(new Color(.2f, .45f, .75f)).Cylinder(c, 6.5f, .4f, 32);
            P(White).Cylinder(c + up * .4f, .8f, 11f, 16);
            P(Gold).Sphere(c + up * 15.4f, .5f, 10);
            var tilt = new GameObject("Kettenkarussell").transform; tilt.SetParent(root, false); tilt.position = c + up * 11.4f;
            var sw = tilt.gameObject.AddComponent<Swing>(); sw.axis = Vector3.right; sw.amp = 7f; sw.period = 8f;
            var rotor = new GameObject("Rotor").transform; rotor.SetParent(tilt, false);
            rotor.gameObject.AddComponent<Spin>().speed = 55f;
            var a = new MeshBuilder(); var b = new MeshBuilder(); var la = new MeshBuilder(); var gold = new MeshBuilder();
            const int S = 20; Vector3 top = up * 3.6f;
            for (int i = 0; i < S; i++)
            {
                float a0 = i / (float)S * Mathf.PI * 2, a1 = (i + 1) / (float)S * Mathf.PI * 2;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 7.4f, 0, Mathf.Sin(a0) * 7.4f), p1 = new Vector3(Mathf.Cos(a1) * 7.4f, 0, Mathf.Sin(a1) * 7.4f);
                var mb = i % 2 == 0 ? a : b; mb.Tri(top, p0, p1, up); mb.Tri(top, p0, p1, -up);
                Vector3 m = (p0 + p1) * .5f, o = new Vector3(m.x, 0, m.z).normalized;
                mb.Box(m - up * .4f, Quaternion.LookRotation(o), new Vector3((p1 - p0).magnitude, .8f, .05f));
                la.Sphere(m + o * .08f - up * .75f, .13f, 6); la.Sphere(m * .5f + top * .5f + o * .1f, .11f, 6);
            }
            a.ToObject("Dach blau", Lit(Blue, .55f), rotor); b.ToObject("Dach gelb", Lit(Yellow, .55f), rotor);
            la.ToObject("Lichter", Emit(Bulbs[0], 4), rotor, false);
            // Sitze an Ketten, fliehen nach aussen
            var chain = Lit(Steel, .7f, .9f);
            for (int i = 0; i < 16; i++)
            {
                float ang = i / 16f * Mathf.PI * 2; var o = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                var piv = new GameObject("Sitz").transform; piv.SetParent(rotor, false); piv.localPosition = o * 6.6f - up * .6f;
                var tangent = Vector3.Cross(up, o);
                piv.localRotation = Quaternion.FromToRotation(Vector3.down, (Vector3.down * Mathf.Cos(.7f) + o * Mathf.Sin(.7f)).normalized) * Quaternion.LookRotation(-tangent, up);
                var ch = new MeshBuilder(); var seat = new MeshBuilder();
                ch.Beam(new Vector3(-.25f, 0, 0), new Vector3(-.25f, -5f, 0), .03f, Vector3.forward); ch.Beam(new Vector3(.25f, 0, 0), new Vector3(.25f, -5f, 0), .03f, Vector3.forward);
                seat.Box(new Vector3(0, -5.1f, 0), Quaternion.identity, new Vector3(.6f, .1f, .55f));
                seat.Box(new Vector3(0, -4.75f, -.25f), Quaternion.identity, new Vector3(.6f, .7f, .08f));
                ch.ToObject("Kette", chain, piv, false); seat.ToObject("Sessel", Lit(Bright[i % 6], .6f), piv);
            }
        }

        // ---------- Freifallturm ----------
        static void DropTower(Vector3 c)
        {
            const float H = 58f;
            P(Dark).Cylinder(c, 6f, .5f, 24);
            var tower = new MeshBuilder();
            tower.Cylinder(c + up * .5f, 1.5f, H, 16);
            for (float y = 4; y < H; y += 3f) for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + .3f; tower.Box(c + new Vector3(Mathf.Cos(a) * 1.55f, y, Mathf.Sin(a) * 1.55f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.1f, 2.6f, .3f)); }
            tower.ToObject("Turm", Lit(Steel, .6f, .7f), root);
            P(Red).Cylinder(c + up * H, 2.4f, 3f, 20); P(Yellow).Cone(c + up * (H + 3), 2.4f, 2.5f, 20);
            for (int i = 0; i < 30; i++) { float a = i / 30f * Mathf.PI * 2; G(Bulbs[(i / 3) % 5]).Sphere(c + new Vector3(Mathf.Cos(a) * 2.45f, H + 1.5f, Mathf.Sin(a) * 2.45f), .14f, 6); }
            for (float y = 2; y < H; y += 2f) G(Bulbs[1]).Sphere(c + new Vector3(0, y, -1.6f), .1f, 5);
            var ring = new GameObject("Fallring").transform; ring.SetParent(root, false); ring.position = c + up * 1.2f;
            var dr = ring.gameObject.AddComponent<DropRide>(); dr.top = H - 4f; dr.bottom = 1.2f;
            var rb = new MeshBuilder(); var seats = new MeshBuilder();
            rb.Cylinder(new Vector3(0, 0, 0), 2.4f, 1.0f, 24);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var q = Quaternion.LookRotation(o);
                seats.Box(o * 2.75f + up * .5f, q, new Vector3(.6f, .12f, .55f));
                seats.Box(o * 2.5f + up * 1.1f, q, new Vector3(.6f, 1.2f, .12f));
                seats.Box(o * 2.75f + up * 1.2f, q * Quaternion.Euler(30, 0, 0), new Vector3(.5f, .5f, .1f));
            }
            rb.ToObject("Ring", Lit(Yellow, .6f, .4f), ring); seats.ToObject("Sitze", Lit(Dark, .4f), ring);
            Text("FREEFALL", c + new Vector3(0, H + 1.5f, 2.5f), 180f, .14f, White);
        }

        // ---------- Schiffschaukel ----------
        static void PirateShip(Vector3 c)
        {
            const float H = 15f, arm = 11f;
            foreach (float z in new[] { -4f, 4f })
            {
                P(Steel).Beam(c + new Vector3(-8, 0, z), c + new Vector3(0, H, z), .5f, Vector3.forward);
                P(Steel).Beam(c + new Vector3(8, 0, z), c + new Vector3(0, H, z), .5f, Vector3.forward);
            }
            P(Steel).Loft(new[] { c + new Vector3(0, H, -4.5f), c + new Vector3(0, H, 4.5f) }, new[] { new Vector2(.35f, .35f), new Vector2(.35f, .35f) }, 1, 12, true, true, Vector3.up);
            P(Dark).Box(c + up * .4f, Quaternion.identity, new Vector3(14, .8f, 6));
            var piv = new GameObject("Schiffschaukel").transform; piv.SetParent(root, false); piv.position = c + up * H;
            var sw = piv.gameObject.AddComponent<Swing>(); sw.axis = Vector3.forward; sw.amp = 62f; sw.period = 7f; sw.cycle = 40f;
            var hull = new MeshBuilder { tile = 1.5f }; var deco = new MeshBuilder(); var sail = new MeshBuilder();
            var ctrl = new[] { new Vector3(-7.5f, -arm + 1.6f, 0), new Vector3(-5, -arm + .3f, 0), new Vector3(0, -arm, 0), new Vector3(5, -arm + .3f, 0), new Vector3(7.5f, -arm + 1.6f, 0) };
            var rad = new[] { new Vector2(.6f, .8f), new Vector2(1.5f, 1.1f), new Vector2(1.8f, 1.2f), new Vector2(1.5f, 1.1f), new Vector2(.6f, .8f) };
            hull.Loft(ctrl, rad, 5, 20, true, true, Vector3.up);
            deco.Loft(new[] { ctrl[0] + up * .7f, ctrl[1] + up * 1f, ctrl[2] + up * 1.05f, ctrl[3] + up * 1f, ctrl[4] + up * .7f }, new[] { new Vector2(.7f, .12f), new Vector2(1.6f, .12f), new Vector2(1.9f, .12f), new Vector2(1.6f, .12f), new Vector2(.7f, .12f) }, 5, 16, true, true, Vector3.up);
            foreach (float z in new[] { -1.3f, 1.3f }) { deco.Beam(Vector3.zero + new Vector3(0, 0, z * 2.6f), new Vector3(-5, -arm + 1f, z), .3f, Vector3.forward); deco.Beam(new Vector3(0, 0, z * 2.6f), new Vector3(5, -arm + 1f, z), .3f, Vector3.forward); }
            deco.Cylinder(new Vector3(0, -arm + .8f, 0), .15f, 6f, 8);
            sail.Box(new Vector3(0, -arm + 4.2f, 0), Quaternion.identity, new Vector3(4.2f, 3.6f, .05f));
            hull.ToObject("Rumpf", Tint(ta != null ? ta.woodDeck : null, new Color(.7f, .35f, .25f)), piv);
            deco.ToObject("Reling", Lit(Gold, .7f, .7f), piv); sail.ToObject("Segel", Lit(White, .3f), piv);
        }

        // ---------- Autoscooter-Halle ----------
        static void BumperCars(Vector3 c)
        {
            const float W = 26, D = 18, H = 5.2f;
            var floor = new MeshBuilder(); floor.Box(c + up * .15f, Quaternion.identity, new Vector3(W, .3f, D));
            floor.ToObject("Fahrflaeche", Lit(new Color(.18f, .2f, .24f), .85f, .6f), root);
            for (float x = -W / 2; x <= W / 2 + .01f; x += W / 4) foreach (float z in new[] { -D / 2, D / 2 }) P(Steel).Cylinder(c + new Vector3(x, .3f, z), .2f, H, 10);
            P(Dark).Box(c + up * (H + .5f), Quaternion.identity, new Vector3(W + 1, .3f, D + 1));
            var ceil = new MeshBuilder(); ceil.Box(c + up * (H + .3f), Quaternion.identity, new Vector3(W, .05f, D));
            ceil.ToObject("Fahrdraht-Netz", Lit(new Color(.4f, .4f, .42f), .6f, .8f), root);
            foreach (float z in new[] { -D / 2 - .5f, D / 2 + .5f })
            {
                P(Purple).Box(c + new Vector3(0, H + 1.3f, z), Quaternion.identity, new Vector3(W + 1, 1.6f, .15f));
                for (float x = -W / 2; x <= W / 2; x += .5f) G(Bulbs[(int)((x + 20) / .5f) % 5]).Sphere(c + new Vector3(x, H + .55f, z + Mathf.Sign(z) * .12f), .1f, 5);
                Text("AUTOSCOOTER", c + new Vector3(0, H + 1.3f, z + Mathf.Sign(z) * .1f), z > 0 ? 180f : 0f, .2f, Yellow);
            }
            // Bande
            P(Yellow).Box(c + new Vector3(0, .5f, -D / 2 + .2f), Quaternion.identity, new Vector3(W, .4f, .3f));
            P(Yellow).Box(c + new Vector3(0, .5f, D / 2 - .2f), Quaternion.identity, new Vector3(W, .4f, .3f));
            P(Yellow).Box(c + new Vector3(-W / 2 + .2f, .5f, 0), Quaternion.identity, new Vector3(.3f, .4f, D));
            P(Yellow).Box(c + new Vector3(W / 2 - .2f, .5f, 0), Quaternion.identity, new Vector3(.3f, .4f, D));
            var area = new Rect(c.x - W / 2 + 1.6f, c.z - D / 2 + 1.6f, W - 3.2f, D - 3.2f);
            for (int i = 0; i < 12; i++)
            {
                var car = new GameObject("Scooter").transform; car.SetParent(root, false);
                car.localPosition = new Vector3(Random.Range(area.xMin, area.xMax), .3f, Random.Range(area.yMin, area.yMax));
                var body = new MeshBuilder(); var rub = new MeshBuilder(); var pole = new MeshBuilder();
                body.Loft(new[] { new Vector3(0, .45f, -.9f), new Vector3(0, .5f, 0), new Vector3(0, .45f, .9f) }, new[] { new Vector2(.6f, .25f), new Vector2(.75f, .3f), new Vector2(.6f, .25f) }, 4, 14, true, true);
                body.Box(new Vector3(0, .8f, -.45f), Quaternion.identity, new Vector3(1.0f, .5f, .2f));
                rub.Loft(new[] { new Vector3(0, .25f, -1.05f), new Vector3(0, .25f, 0), new Vector3(0, .25f, 1.05f) }, new[] { new Vector2(.75f, .14f), new Vector2(.9f, .14f), new Vector2(.75f, .14f) }, 4, 14, true, true);
                pole.Cylinder(new Vector3(0, .7f, -.75f), .03f, H - .7f, 5);
                body.ToObject("Karosse", Lit(Bright[i % 7], .85f, .2f), car); rub.ToObject("Gummi", Lit(Dark, .3f), car); pole.ToObject("Stange", Lit(Steel, .7f, .9f), car, false);
                car.gameObject.AddComponent<Scooter>().area = area;
            }
        }

        // ---------- Buden ----------
        static readonly string[] BoothNames = { "LOSE", "CREPES", "BRATWURST", "ZUCKERWATTE", "SCHIESSEN", "MANDELN", "PILS", "POMMES", "DOSENWERFEN", "ENTENANGELN", "LEBKUCHEN", "POPCORN", "SLUSH", "CHURROS", "BACKFISCH", "AUTOMATEN" };
        static void Booths()
        {
            int k = 0;
            for (float x = -40; x <= 88; x += 7f) { if (x > -36 && x < -24) continue; Booth(new Vector3(x, 0, 16), 0, Bright[k % 7], White, BoothNames[k % BoothNames.Length], true); k++; }
            for (float x = -42; x <= 90; x += 7f) { if (x > 30 && x < 54) continue; if (x > -42 && x < -16) continue; Booth(new Vector3(x, 0, 34), 180, Bright[(k + 3) % 7], White, BoothNames[k % BoothNames.Length], true); k++; }
            for (float x = -30; x <= 84; x += 7f) { Booth(new Vector3(x, 0, -46), 0, Bright[(k + 1) % 7], White, BoothNames[k % BoothNames.Length], true); k++; }
        }

        static void Booth(Vector3 pos, float yaw, Color a, Color b, string name, bool goods)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Vector3 L(float x, float y, float z) => pos + q * new Vector3(x, y, z);
            const float W = 5.2f, D = 3.2f, H = 2.7f;
            var wall = P(a);
            wall.Box(L(0, H / 2, -D / 2), q, new Vector3(W, H, .1f));
            wall.Box(L(-W / 2, H / 2, 0), q, new Vector3(.1f, H, D));
            wall.Box(L(W / 2, H / 2, 0), q, new Vector3(.1f, H, D));
            P(White).Box(L(0, .55f, D / 2 - .2f), q, new Vector3(W, 1.1f, .4f));
            P(Dark).Box(L(0, 1.12f, D / 2 - .15f), q, new Vector3(W + .1f, .06f, .55f));
            P(Dark).Box(L(0, H + .05f, 0), q, new Vector3(W + .2f, .12f, D + .2f));
            // gestreifte Markise
            for (int i = 0; i < 10; i++)
            {
                float x = -W / 2 + (i + .5f) * W / 10;
                P(i % 2 == 0 ? a : b).Box(L(x, H - .2f, D / 2 + .55f), q * Quaternion.Euler(22, 0, 0), new Vector3(W / 10 + .005f, .04f, 1.3f));
                P(i % 2 == 0 ? a : b).Box(L(x, H - .62f, D / 2 + 1.16f), q, new Vector3(W / 10 + .005f, .3f, .03f));
            }
            // Schild mit Lichtern
            P(Dark).Box(L(0, H + .7f, D / 2 - .1f), q, new Vector3(W - .4f, 1.0f, .1f));
            for (float x = -W / 2 + .4f; x <= W / 2 - .3f; x += .45f) G(Bulbs[0]).Sphere(L(x, H + 1.25f, D / 2 - .05f), .07f, 5);
            Text(name, L(0, H + .7f, D / 2 - .02f), yaw + 180f, name.Length > 9 ? .085f : .11f, Yellow);
            if (!goods) return;
            // Waren in den Regalen: Plueschtiere, Baelle, Glaeser
            P(new Color(.55f, .4f, .25f)).Box(L(0, 1.6f, -D / 2 + .35f), q, new Vector3(W - .3f, .05f, .5f));
            P(new Color(.55f, .4f, .25f)).Box(L(0, 2.2f, -D / 2 + .35f), q, new Vector3(W - .3f, .05f, .5f));
            for (float y = 1.6f; y < 2.5f; y += .6f)
                for (float x = -W / 2 + .45f; x < W / 2 - .3f; x += .5f)
                {
                    var col = Bright[Mathf.Abs((int)(x * 7 + y * 13 + pos.x)) % Bright.Length];
                    P(col).Sphere(L(x, y + .2f, -D / 2 + .35f), .18f, 8);
                    P(col).Sphere(L(x, y + .45f, -D / 2 + .38f), .12f, 8);
                }
            G(new Color(1f, .95f, .8f)).Box(L(0, H - .1f, 0), q, new Vector3(W - .6f, .05f, .12f));
        }

        // ---------- Lichterketten ueber der Gasse ----------
        static void LightStrings()
        {
            var poles = P(Dark); int c = 0;
            foreach (var row in new[] { new Vector2(20.5f, 29.5f), new Vector2(-38f, -38f) })
            {
                bool cross = row.x != row.y;
                for (float x = -36; x <= 86; x += 12f)
                {
                    float z0 = cross ? row.x : -38f, z1 = cross ? row.y : -38f;
                    Vector3 a = new Vector3(x, 6.5f, z0), b = cross ? new Vector3(x + 12, 6.5f, z1) : new Vector3(x + 12, 6.5f, z0);
                    poles.Cylinder(new Vector3(x, 0, z0), .1f, 6.6f, 6);
                    if (cross) poles.Cylinder(new Vector3(x, 0, z1), .1f, 6.6f, 6);
                    Vector3[] ends = cross ? new[] { a, b, new Vector3(x, 6.5f, z1), new Vector3(x + 12, 6.5f, z0) } : new[] { a, b };
                    for (int e = 0; e + 1 < ends.Length; e += 2)
                    {
                        Vector3 p0 = ends[e], p1 = ends[e + 1]; float len = (p1 - p0).magnitude; int n = Mathf.RoundToInt(len / .9f);
                        Vector3 prev = p0;
                        for (int i = 1; i <= n; i++)
                        {
                            float t = i / (float)n; var p = Vector3.Lerp(p0, p1, t) - up * Mathf.Sin(t * Mathf.PI) * 1.1f;
                            P(Dark).Beam(prev, p, .02f, Vector3.up); prev = p;
                            if (i < n) G(Bulbs[(c++) % Bulbs.Length]).Sphere(p - up * .1f, .09f, 6);
                        }
                    }
                }
            }
            // Wimpelketten am Eingang der Gasse
            for (float x = -36; x <= 84; x += 12f)
                for (int i = 0; i < 14; i++)
                {
                    float t = (i + .5f) / 14f; var p = new Vector3(x + t * 12, 7.6f - Mathf.Sin(t * Mathf.PI) * .9f, 25);
                    var mb = P(Bright[i % 6]);
                    mb.Tri(p + new Vector3(-.3f, 0, 0), p + new Vector3(.3f, 0, 0), p + new Vector3(0, -.6f, 0), Vector3.forward);
                    mb.Tri(p + new Vector3(-.3f, 0, 0), p + new Vector3(.3f, 0, 0), p + new Vector3(0, -.6f, 0), Vector3.back);
                }
        }

        static void Lamps()
        {
            var spots = new List<Vector3>();
            for (float x = -96; x <= 136; x += 20) { spots.Add(new Vector3(x, 0, 92)); spots.Add(new Vector3(x, 0, -92)); }
            for (float z = -80; z <= 80; z += 20) { spots.Add(new Vector3(-100, 0, z)); spots.Add(new Vector3(138, 0, z)); }
            foreach (var p in spots)
            {
                P(Dark).Cylinder(p, .12f, 5f, 8);
                P(Dark).Beam(p + up * 4.9f, p + up * 4.9f + Vector3.right * .6f, .08f, Vector3.up);
                G(new Color(1f, .9f, .7f)).Sphere(p + up * 4.7f + Vector3.right * .6f, .25f, 10);
            }
            // Baenke und Muelleimer an der Gasse
            for (float x = -30; x <= 80; x += 18)
            {
                P(new Color(.45f, .3f, .18f)).Box(new Vector3(x + 6, .45f, 21.5f), Quaternion.identity, new Vector3(1.8f, .08f, .45f));
                P(Dark).Box(new Vector3(x + 6, .22f, 21.5f), Quaternion.identity, new Vector3(1.6f, .44f, .3f));
                P(Green).Cylinder(new Vector3(x + 9, 0, 28.5f), .3f, .9f, 10);
            }
        }

        static void Entrance(Vector3 c)
        {
            foreach (float z in new[] { -7f, 7f })
            {
                P(Red).Box(c + new Vector3(0, 4.5f, z), Quaternion.identity, new Vector3(1.4f, 9f, 1.4f));
                P(Yellow).Cone(c + new Vector3(0, 9f, z), 1.1f, 2f, 8, 22.5f);
                for (float y = .5f; y < 9; y += .5f) G(Bulbs[0]).Sphere(c + new Vector3(.75f, y, z), .08f, 5);
            }
            P(Blue).Box(c + new Vector3(0, 8f, 0), Quaternion.identity, new Vector3(.6f, 2.4f, 15f));
            for (float z = -6.5f; z <= 6.5f; z += .45f) { G(Bulbs[1]).Sphere(c + new Vector3(.35f, 9.3f, z), .08f, 5); G(Bulbs[1]).Sphere(c + new Vector3(.35f, 6.7f, z), .08f, 5); }
            Text("RUMMEL", c + new Vector3(.33f, 8f, 0), 270f, .32f, Yellow);
            Text("RUMMEL", c + new Vector3(-.33f, 8f, 0), 90f, .32f, Yellow);
        }

        // ---------- Besucher ----------
        static void Visitors(Scenery.Clearance clr)
        {
            var templates = new List<GameObject>();
            Color[] skins = { new Color(.96f, .8f, .68f), new Color(.86f, .66f, .5f), new Color(.55f, .38f, .26f), new Color(.98f, .86f, .76f) };
            Color[] hair = { new Color(.15f, .1f, .06f), new Color(.45f, .3f, .15f), new Color(.8f, .65f, .35f), new Color(.08f, .08f, .08f), new Color(.6f, .6f, .6f) };
            Color[] pants = { new Color(.15f, .2f, .35f), new Color(.2f, .2f, .22f), new Color(.45f, .38f, .28f), new Color(.1f, .12f, .25f) };
            var holder = new GameObject("Besucher-Vorlagen"); holder.SetActive(false); holder.transform.SetParent(root, false);
            for (int v = 0; v < 12; v++)
            {
                var go = new GameObject("Mensch"); go.transform.SetParent(holder.transform, false);
                var cloth = new MeshBuilder(); var leg = new MeshBuilder(); var skin = new MeshBuilder(); var hr = new MeshBuilder();
                foreach (float s in new[] { -.1f, .1f })
                {
                    leg.Loft(new[] { new Vector3(s, 0, 0), new Vector3(s, .45f, 0), new Vector3(s, .9f, 0) }, new[] { new Vector2(.08f, .09f), new Vector2(.09f, .1f), new Vector2(.11f, .12f) }, 2, 8, true, true, Vector3.forward);
                    cloth.Loft(new[] { new Vector3(s * 2.4f, 1.42f, 0), new Vector3(s * 2.7f, 1.12f, .02f), new Vector3(s * 2.6f, .85f, .05f) }, new[] { new Vector2(.06f, .06f), new Vector2(.055f, .055f), new Vector2(.05f, .05f) }, 2, 6, false, false, Vector3.forward);
                    skin.Sphere(new Vector3(s * 2.6f, .8f, .05f), .05f, 6);
                }
                cloth.Loft(new[] { new Vector3(0, .88f, 0), new Vector3(0, 1.2f, 0), new Vector3(0, 1.45f, 0) }, new[] { new Vector2(.2f, .13f), new Vector2(.21f, .13f), new Vector2(.2f, .12f) }, 3, 10, true, true, Vector3.forward);
                skin.Sphere(new Vector3(0, 1.62f, 0), .12f, 10);
                skin.Cylinder(new Vector3(0, 1.45f, 0), .05f, .08f, 6);
                hr.Sphere(new Vector3(0, 1.67f, -.02f), .125f, 10);
                cloth.ToObject("Kleidung", Lit(Bright[v % Bright.Length] * .9f, .3f), go.transform);
                leg.ToObject("Hose", Lit(pants[v % pants.Length], .25f), go.transform);
                skin.ToObject("Haut", Lit(skins[v % skins.Length], .35f), go.transform);
                hr.ToObject("Haar", Lit(hair[v % hair.Length], .3f), go.transform);
                templates.Add(go);
            }
            var people = new GameObject("Besucher").transform; people.SetParent(root, false);
            void Put(Vector3 p, float yaw, bool walk, Vector3 to)
            {
                var g = Object.Instantiate(templates[Random.Range(0, templates.Count)], people);
                float s = Random.value < .2f ? Random.Range(.65f, .8f) : Random.Range(.93f, 1.08f);
                g.transform.localScale = Vector3.one * s; g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
                if (walk) { var w = g.AddComponent<Walker>(); w.a = p; w.b = to; w.speed = Random.Range(.8f, 1.4f); }
            }
            // Gasse zwischen den Buden
            for (int i = 0; i < 110; i++)
            {
                float x = Random.Range(-38f, 86f), z = Random.Range(21f, 29f);
                bool walk = Random.value < .55f;
                Put(new Vector3(x, 0, z), Random.Range(0, 360f), walk, new Vector3(x + Random.Range(-25f, 25f), 0, Random.Range(21f, 29f)));
            }
            // vor den Buden stehend
            for (float x = -40; x <= 88; x += 7f) if (Random.value < .6f) Put(new Vector3(x + Random.Range(-1.5f, 1.5f), 0, 20.2f), 180, false, default);
            for (float x = -30; x <= 84; x += 7f) if (Random.value < .6f) Put(new Vector3(x + Random.Range(-1.5f, 1.5f), 0, -41.8f), 180, false, default);
            // Schlangen und Zuschauer an den Fahrgeschaeften
            for (int i = 0; i < 10; i++) Put(new Vector3(-22 + i * .7f, 0, 13.5f), 270, false, default);
            for (int i = 0; i < 8; i++) Put(new Vector3(105 + Random.Range(-5f, 5f), 0, 32 - 9f - i * .7f), 0, false, default);
            for (int i = 0; i < 40; i++)
            {
                float x = Random.Range(-95f, 130f), z = Random.Range(-85f, 85f);
                if (x > -34 && x < 72 && z > -38 && z < 8) continue;   // nicht unter die Bahn
                Put(new Vector3(x, 0, z), Random.Range(0, 360f), Random.value < .6f, new Vector3(x + Random.Range(-20f, 20f), 0, z + Random.Range(-10f, 10f)));
            }
        }

        static void Balloons()
        {
            for (int i = 0; i < 16; i++)
            {
                var b = new GameObject("Ballon").transform; b.SetParent(root, false);
                b.position = new Vector3(Random.Range(-38f, 86f), Random.Range(2.6f, 3.4f), Random.Range(22f, 28f));
                var mb = new MeshBuilder(); mb.Loft(new[] { new Vector3(0, 0, 0), new Vector3(0, .25f, 0), new Vector3(0, .5f, 0) }, new[] { new Vector2(.05f, .05f), new Vector2(.28f, .28f), new Vector2(.05f, .05f) }, 4, 10, true, true, Vector3.forward);
                mb.ToObject("Huelle", Lit(Bright[i % 7], .9f, .1f), b, false);
                var s = new MeshBuilder(); s.Beam(Vector3.zero, Vector3.down * 1.4f, .01f, Vector3.forward); s.ToObject("Schnur", Lit(White), b, false);
                var bob = b.gameObject.AddComponent<Bob>(); bob.amp = .12f; bob.speed = 1.3f; bob.phase = i;
            }
        }
    }
}
