using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        public float Angle { get; private set; }
        void Start() { b = transform.localRotation; ph = Random.value * 50f; }
        void Update()
        {
            float t = Time.time + ph;
            float env = cycle > 0 ? Mathf.Clamp01(Mathf.Sin(t / cycle * Mathf.PI * 2) * 1.6f + .4f) : 1f;
            Angle = amp * env * Mathf.Sin(t / period * Mathf.PI * 2);
            transform.localRotation = b * Quaternion.AngleAxis(Angle, axis);
        }
    }

    public class Bob : MonoBehaviour
    {
        public float amp = .25f, speed = 2f, phase; Vector3 b;
        void Start() { b = transform.localPosition; }
        void Update() { transform.localPosition = b + Vector3.up * amp * Mathf.Sin(Time.time * speed + phase); }
    }

    /// <summary>Freifallturm: langsam hoch, kurz warten, fallen, abbremsen, unten warten. Blitzlicht und Schreie beim Fall.</summary>
    public class DropRide : MonoBehaviour
    {
        public float top = 45f, bottom = 1.2f; public Light strobe; public AudioSource scream; float ph; bool fell;
        public float Height => transform.localPosition.y;
        public bool Falling { get; private set; }
        void Start() { ph = Random.value * 20f; }
        void Update()
        {
            float t = (Time.time + ph) % 20f, y;
            if (t < 9f) y = Mathf.Lerp(bottom, top, Mathf.SmoothStep(0, 1, t / 9f));
            else if (t < 12f) y = top;
            else if (t < 14.5f) { float k = (t - 12f) / 2.5f; y = top - (top - bottom - 4f) * Mathf.Min(1, k * k); }
            else if (t < 16f) y = Mathf.Lerp(bottom + 4f, bottom, Mathf.SmoothStep(0, 1, (t - 14.5f) / 1.5f));
            else y = bottom;
            Falling = t >= 12f && t < 14.5f;
            var p = transform.localPosition; p.y = y; transform.localPosition = p;
            if (strobe != null) strobe.intensity = Falling ? (Mathf.Repeat(Time.time * 12f, 1f) < .5f ? 14f : 0f) : (t > 10.5f && t < 12f ? 3f : 0f);
            if (Falling && !fell && scream != null) { scream.time = Random.Range(0f, Mathf.Max(0, scream.clip.length - 4f)); scream.Play(); }
            if (!Falling && fell && scream != null) scream.Stop();
            fell = Falling;
        }
    }

    /// <summary>Lauflicht: zwei Lampengruppen wechseln sich ab (fuer klassische Materiallampen).</summary>
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

    /// <summary>Lichtquelle wechselt langsam die Farbe (Riesenrad, Karussell).</summary>
    public class ColorCycle : MonoBehaviour
    {
        public float speed = .08f, offset; public float sat = .8f; Light l;
        void Start() { l = GetComponent<Light>(); }
        void Update() { if (l != null) l.color = Color.HSVToRGB(Mathf.Repeat(Time.time * speed + offset, 1f), sat, 1f); }
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

    /// <summary>Rummelplatz rund um die Wilde Maus, in der Daemmerung. Nur Optik und Ton, ohne Einfluss auf Physik oder Sitz.</summary>
    public static partial class FairWorld
    {
        static readonly Vector3 up = Vector3.up;
        static Transform root; static ThemeAssets ta; static BulbSet B;
        /// <summary>Der Wagen der Wilden Maus (fuer winkende Zuschauer). Wird von WorldBuilder gesetzt.</summary>
        public static Transform car;
        static readonly Dictionary<Color, MeshBuilder> paint = new Dictionary<Color, MeshBuilder>();
        static readonly Dictionary<Color, Material> paintMats = new Dictionary<Color, Material>();
        static int lightCount;

        static readonly Color Red = new Color(.85f, .08f, .08f), White = new Color(.95f, .94f, .9f), Yellow = new Color(.98f, .78f, .1f),
            Blue = new Color(.1f, .3f, .8f), Green = new Color(.1f, .6f, .25f), Pink = new Color(.95f, .4f, .65f), Orange = new Color(1f, .5f, .08f),
            Gold = new Color(.85f, .65f, .2f), Dark = new Color(.12f, .12f, .13f), Steel = new Color(.62f, .64f, .67f), Purple = new Color(.5f, .2f, .7f),
            Turq = new Color(.1f, .7f, .7f);
        static readonly Color[] Bright = { Red, Yellow, Blue, Green, Pink, Orange, Purple, White, Turq };
        // Lampenfarben in HDR-Grundton (Staerke kommt vom Shader)
        static readonly Color Warm = new Color(1f, .78f, .42f), BRed = new Color(1f, .22f, .15f), BBlue = new Color(.25f, .5f, 1f), BGreen = new Color(.35f, 1f, .35f),
            BPink = new Color(1f, .35f, .85f), BYellow = new Color(1f, .9f, .3f), BCyan = new Color(.3f, .95f, 1f), BWhite = new Color(1f, .95f, .88f);
        static readonly Color[] Bulbs = { Warm, BRed, BBlue, BGreen, BPink, BYellow, BCyan };

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
        /// <summary>Lackierte Flaeche (ambientCG-Lack, eingefaerbt), je Farbe ein Material.</summary>
        static Material Paint(Color c)
        {
            if (paintMats.TryGetValue(c, out var m)) return m;
            m = c == Steel ? Lit(Steel, .6f, .85f) : (ta != null && ta.lacquer != null ? Tint(ta.lacquer, c) : Lit(c, .7f));
            return paintMats[c] = m;
        }

        static MeshBuilder P(Color c) { if (!paint.TryGetValue(c, out var b)) paint[c] = b = new MeshBuilder(); return b; }

        static Light PL(Vector3 p, Color c, float range, float intensity, Transform parent = null)
        {
            var go = new GameObject("Licht"); go.transform.SetParent(parent != null ? parent : root, false); go.transform.localPosition = p;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            lightCount++;
            return l;
        }

        static Font font;
        /// <summary>Leuchtschrift (Neon) mit eigenem Shader; mode 0 ruhig, 1 flackert ab und zu, 2 blinkt.</summary>
        static GameObject Neon(string s, Vector3 pos, float yaw, float size, Color c, float mode = 1, Transform parent = null, float intensity = 3.5f)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Neon " + s); go.transform.SetParent(parent != null ? parent : root, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font; tm.text = s; tm.fontSize = 64; tm.characterSize = size * .6f; tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (ta != null && ta.neon != null)
            {
                var m = new Material(ta.neon); m.SetTexture("_MainTex", font.material.mainTexture);
                m.SetColor("_Color", c); m.SetFloat("_Intensity", intensity); m.SetFloat("_Seed", Random.value * 10f); m.SetFloat("_Mode", mode);
                mr.sharedMaterial = m;
            }
            else mr.sharedMaterial = font.material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ================================================================
        public static void Build(TrackDef def, Track tr, Transform parent, ThemeAssets assets, Scenery.Clearance clr, Ground g, Vector3 center, float radius)
        {
            root = parent; ta = assets; paint.Clear(); paintMats.Clear(); lightCount = 0; B = new BulbSet();
            booths.Clear(); horses.Clear(); wheelSeats.Clear(); swingSeats.Clear(); scooters.Clear(); drop = null; shipPivot = null; shipSwing = null;
            CrowdAnim.ReleaseAll();
            Plaza(clr);
            Station(clr);
            TrackLights(tr);
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
            Balloons();
            Effects();
            People(clr);
            Sounds();

            foreach (var kv in paint) kv.Value.ToObject("Rummel " + kv.Key, Paint(kv.Key), root);
            B.Build("Gluehbirnen", root);
            Debug.Log("Rummel: " + lightCount + " Lichtquellen, " + B.Count + " Gluehbirnen (statisch)");
        }

        // ---------- Platz: Asphalt mit Pfuetzen ----------
        static void Plaza(Scenery.Clearance clr)
        {
            var mb = new MeshBuilder { tile = 5f };
            mb.Box(new Vector3(18, -.04f, 0), Quaternion.identity, new Vector3(250, .14f, 200));
            var mat = ta != null && ta.asphalt != null ? Tint(ta.asphalt, new Color(.62f, .6f, .62f)) : Lit(new Color(.2f, .2f, .22f), .5f);
            mb.ToObject("Festplatz", mat, root).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            clr.Block(new Rect(-108, -100, 252, 200));
            // Pfuetzen: spiegeln Himmel und Lichter
            var puddle = new MeshBuilder();
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector3(Random.Range(-95f, 135f), .035f, Random.Range(-90f, 90f));
                if (p.x > -34 && p.x < 72 && p.z > -36 && p.z < 4) continue;
                float r = Random.Range(.4f, 1.5f); int lobes = Random.Range(2, 4);
                for (int k = 0; k < lobes; k++) puddle.Disc(p + new Vector3(Random.Range(-r, r), k * .001f, Random.Range(-r * .6f, r * .6f)), r * Random.Range(.5f, 1f), 18, up);
            }
            for (float x = -40; x <= 88; x += 7f) if (Random.value < .35f) puddle.Disc(new Vector3(x + Random.Range(-2f, 2f), .035f, Random.Range(21.5f, 28.5f)), Random.Range(.4f, 1.0f), 16, up);
            var pm = Lit(new Color(.015f, .015f, .02f), .9f, 0); pm.SetFloat("_EnvironmentReflections", 1f);
            puddle.ToObject("Pfuetzen", pm, root, false);
            // Absperrung um die Bahn
            var fence = P(Steel);
            float[] xs = { -32, 70 }; float[] zs = { -36, 4 };
            for (float x = xs[0]; x < xs[1]; x += 2.4f)
                foreach (float z in zs) { if (z > 0 && x < 10) continue; fence.Box(new Vector3(x + 1.2f, 1.0f, z), Quaternion.identity, new Vector3(2.4f, .05f, .05f)); fence.Box(new Vector3(x, .5f, z), Quaternion.identity, new Vector3(.05f, 1.05f, .05f)); }
            for (float z = zs[0]; z < zs[1]; z += 2.4f)
                foreach (float x in xs) { if (x == xs[0] && z > -8) continue; fence.Box(new Vector3(x, 1.0f, z + 1.2f), Quaternion.identity, new Vector3(.05f, .05f, 2.4f)); fence.Box(new Vector3(x, .5f, z), Quaternion.identity, new Vector3(.05f, 1.05f, .05f)); }
        }

        // ---------- Lichter entlang der Bahn ----------
        static void TrackLights(Track tr)
        {
            int stride = Mathf.Max(1, Mathf.RoundToInt(1.1f / (float)tr.ds));
            int cnt = tr.n / stride;
            for (int k = 0; k < cnt; k++)
            {
                int i = k * stride;
                Vector3 p = tr.pos[i].ToUnity(), r = tr.R[i].ToUnity(), u = tr.U[i].ToUnity();
                if (p.y < 1.2f) continue;
                var col = (k / 18) % 3 == 0 ? BRed : Warm;
                B.Add(p + r * .82f - u * .05f, .055f, col, BulbMode.Chase, k / (float)cnt, cnt, .5f, k);
                B.Add(p - r * .82f - u * .05f, .055f, col, BulbMode.Chase, k / (float)cnt, cnt, .5f, k);
            }
        }

        // ---------- Station der Wilden Maus ----------
        static void Station(Scenery.Clearance clr)
        {
            var deck = new MeshBuilder { tile = 2f };
            deck.Box(new Vector3(-6, 1.33f, 3.6f), Quaternion.identity, new Vector3(26f, 2.66f, 4f));
            deck.Box(new Vector3(-6, 1.33f, -3.6f), Quaternion.identity, new Vector3(26f, 2.66f, 3f));
            deck.ToObject("Bahnsteig", ta != null ? ta.woodDeck : Lit(Gold), root);
            for (int i = 0; i < 7; i++) P(Steel).Box(new Vector3(-6, .19f + i * .38f, 7.4f - i * .32f), Quaternion.identity, new Vector3(3, .1f, .4f));
            // Dach: Saeulen und rot-weiss gestreiftes Satteldach, darunter Lichterleisten
            for (float x = -18; x <= 6; x += 6)
                foreach (float z in new[] { -5f, 5.5f }) { P(Steel).Cylinder(new Vector3(x, 2.66f, z), .12f, 4f, 10); B.Line(new Vector3(x, 2.9f, z + .14f), new Vector3(x, 6.4f, z + .14f), .35f, .05f, Warm, BulbMode.Rise, .4f); }
            for (int i = 0; i < 26; i++)
            {
                float x = -19 + i + .5f;
                P(i % 2 == 0 ? Red : White).Gable(new Vector3(x, 6.66f, .25f), Quaternion.Euler(0, 90, 0), 11.5f, 1f, 1.6f, 0f);
            }
            B.Line(new Vector3(-19, 6.55f, 6.05f), new Vector3(7, 6.55f, 6.05f), .4f, .07f, Warm, BulbMode.Chase);
            B.Line(new Vector3(-19, 6.55f, -5.55f), new Vector3(7, 6.55f, -5.55f), .4f, .07f, Warm, BulbMode.Chase);
            PL(new Vector3(-6, 6f, 3.5f), new Color(1f, .8f, .55f), 16f, 3.5f);
            PL(new Vector3(-6, 6f, -3.5f), new Color(1f, .8f, .55f), 14f, 2.5f);
            // Schild mit Lichterrahmen, Neonschrift und Maus-Kopf
            P(Dark).Box(new Vector3(-6, 9.3f, 6.2f), Quaternion.identity, new Vector3(13, 2.4f, .3f));
            P(Steel).Cylinder(new Vector3(-11, 6.6f, 6.2f), .1f, 1.6f, 8); P(Steel).Cylinder(new Vector3(-1, 6.6f, 6.2f), .1f, 1.6f, 8);
            B.Line(new Vector3(-12.3f, 10.55f, 6.4f), new Vector3(.3f, 10.55f, 6.4f), .3f, .09f, BYellow, BulbMode.Chase);
            B.Line(new Vector3(-12.3f, 8.05f, 6.4f), new Vector3(.3f, 8.05f, 6.4f), .3f, .09f, BYellow, BulbMode.Chase);
            B.Line(new Vector3(-12.3f, 8.05f, 6.4f), new Vector3(-12.3f, 10.55f, 6.4f), .3f, .09f, BYellow, BulbMode.Chase);
            B.Line(new Vector3(.3f, 8.05f, 6.4f), new Vector3(.3f, 10.55f, 6.4f), .3f, .09f, BYellow, BulbMode.Chase);
            Neon("WILDE MAUS", new Vector3(-6, 9.3f, 6.38f), 180f, .26f, new Color(1f, .25f, .5f), 1);
            Neon("WILDE MAUS", new Vector3(-6, 9.3f, 6.02f), 0f, .26f, new Color(1f, .25f, .5f), 1);
            var head = P(new Color(.55f, .55f, .58f));
            head.Sphere(new Vector3(-6, 12.2f, 6.1f), 1.5f, 18);
            head.Sphere(new Vector3(-7.4f, 13.5f, 6.1f), .85f, 14); head.Sphere(new Vector3(-4.6f, 13.5f, 6.1f), .85f, 14);
            P(Pink).Sphere(new Vector3(-7.4f, 13.5f, 6.75f), .55f, 12); P(Pink).Sphere(new Vector3(-4.6f, 13.5f, 6.75f), .55f, 12);
            P(Pink).Sphere(new Vector3(-6, 11.9f, 7.55f), .3f, 10);
            B.Add(new Vector3(-6.55f, 12.6f, 7.35f), .2f, BCyan, BulbMode.Breathe, 0, 1, 1.2f); B.Add(new Vector3(-5.45f, 12.6f, 7.35f), .2f, BCyan, BulbMode.Breathe, 0, 1, 1.2f);
            P(Steel).Cylinder(new Vector3(-6, 10.5f, 6.1f), .15f, .8f, 8);
            // Warteschlange mit Gelaender
            for (float x = -30; x < -12; x += 2.4f) foreach (float z in new[] { 12.6f, 14.4f }) { P(Steel).Box(new Vector3(x + 1.2f, .95f, z), Quaternion.identity, new Vector3(2.4f, .05f, .05f)); P(Steel).Box(new Vector3(x, .5f, z), Quaternion.identity, new Vector3(.05f, 1f, .05f)); }
            Booth(new Vector3(-22, 0, 9), 0, Yellow, "KASSE", BoothKind.Ticket);
            clr.Block(new Rect(-34, -8, 44, 18));
        }

        // ---------- Riesenrad ----------
        static Transform wheelPivot;
        static readonly List<Transform> wheelSeats = new List<Transform>();
        static void FerrisWheel(Vector3 c)
        {
            const float R = 21f, H = 25.6f, half = 1.3f;
            var legs = P(White);
            foreach (float x in new[] { -half - 1.2f, half + 1.2f })
            {
                legs.Beam(new Vector3(c.x + x, 0, c.z - 12), new Vector3(c.x + x, H, c.z), .6f, Vector3.right);
                legs.Beam(new Vector3(c.x + x, 0, c.z + 12), new Vector3(c.x + x, H, c.z), .6f, Vector3.right);
                legs.Beam(new Vector3(c.x + x, 8, c.z - 8), new Vector3(c.x + x, 8, c.z + 8), .3f, Vector3.right);
                // Lauflichter an den Beinen
                B.Line(new Vector3(c.x + x * 1.12f, .5f, c.z - 11.6f), new Vector3(c.x + x * 1.12f, H - 1, c.z - .5f), .5f, .07f, BWhite, BulbMode.Rise, .45f);
                B.Line(new Vector3(c.x + x * 1.12f, .5f, c.z + 11.6f), new Vector3(c.x + x * 1.12f, H - 1, c.z + .5f), .5f, .07f, BWhite, BulbMode.Rise, .45f);
            }
            P(Steel).Loft(new[] { new Vector3(c.x - 3, H, c.z), new Vector3(c.x + 3, H, c.z) }, new[] { new Vector2(.5f, .5f), new Vector2(.5f, .5f) }, 1, 14, true, true, Vector3.up);
            P(Red).Box(new Vector3(c.x, .5f, c.z), Quaternion.identity, new Vector3(9, 1, 10));
            for (int i = 0; i < 3; i++) P(Steel).Box(new Vector3(c.x, .15f + i * .3f, c.z - 5.6f - i * .3f), Quaternion.identity, new Vector3(3, .1f, .4f));
            B.Line(new Vector3(c.x - 4.5f, 1.02f, c.z - 5.02f), new Vector3(c.x + 4.5f, 1.02f, c.z - 5.02f), .3f, .06f, BYellow, BulbMode.Chase, .35f);
            P(Dark).Box(new Vector3(c.x, 3.2f, c.z - 5.2f), Quaternion.identity, new Vector3(8, 1.6f, .2f));
            Neon("RIESENRAD", new Vector3(c.x, 3.2f, c.z - 5.33f), 0f, .2f, new Color(.3f, .7f, 1f), 0);

            var pivot = new GameObject("Riesenrad").transform; pivot.SetParent(root, false); pivot.position = new Vector3(c.x, H, c.z);
            wheelPivot = pivot;
            pivot.gameObject.AddComponent<Spin>().axis = Vector3.right; pivot.GetComponent<Spin>().speed = 5f;
            var w = new MeshBuilder(); var hub = new MeshBuilder(); var wb = new BulbSet();
            const int N = 48;
            Vector3 Pt(float side, float i, float r) { float a = i / N * Mathf.PI * 2; return new Vector3(side, Mathf.Sin(a) * r, Mathf.Cos(a) * r); }
            foreach (float s in new[] { -half, half })
            {
                for (int i = 0; i < N; i++)
                {
                    w.Beam(Pt(s, i, R), Pt(s, i + 1, R), .32f, Vector3.right);
                    w.Beam(Pt(s, i, R * .82f), Pt(s, i + 1, R * .82f), .2f, Vector3.right);
                    if (i % 2 == 0) { w.Beam(Pt(s * .4f, 0, 0) + new Vector3(s * .6f, 0, 0), Pt(s, i, R), .14f, Vector3.right); w.Beam(Pt(s, i, R * .82f), Pt(s, i + 1, R), .12f, Vector3.right); }
                }
                // Show-Lichter: Felge aussen und innen, Speichen von der Nabe nach aussen
                float so = s * 1.1f;
                for (int i = 0; i < N * 2; i++) wb.Add(Pt(so, i * .5f, R), .17f, BWhite, BulbMode.Show, i / (float)(N * 2), 1f, 1.1f, i);
                for (int i = 0; i < N; i++) wb.Add(Pt(so, i, R * .82f), .13f, BWhite, BulbMode.Show, i / (float)N, .82f, .8f, i + 300);
                for (int k = 0; k < 24; k++)
                    for (int j = 1; j <= 12; j++) { float rr = j / 12f; wb.Add(Pt(so, k * 2, R * rr * .98f), .1f, BWhite, BulbMode.Show, k / 24f, rr, .6f, k * 20 + j); }
            }
            for (int i = 0; i < N; i += 2) w.Beam(Pt(-half, i, R), Pt(half, i, R), .16f, Vector3.up);
            hub.Loft(new[] { new Vector3(-half - .4f, 0, 0), new Vector3(half + .4f, 0, 0) }, new[] { new Vector2(1.4f, 1.4f), new Vector2(1.4f, 1.4f) }, 1, 20, true, true, Vector3.up);
            for (int i = 0; i < 16; i++) foreach (float s in new[] { -half - .42f, half + .42f }) wb.Add(Pt(s, i * 3, 1.15f), .12f, BYellow, BulbMode.Chase, i / 16f, 16, .7f, i);
            w.ToObject("Speichen", Lit(White, .6f, .3f), pivot);
            hub.ToObject("Nabe", Paint(Red), pivot);
            wb.Build("Riesenrad-Lichter", pivot);
            // Farbiges Licht vom Rad auf den Platz
            var l1 = PL(new Vector3(c.x - 6, H - 4, c.z), Color.white, 38f, 3f); l1.gameObject.AddComponent<ColorCycle>().speed = .05f;
            var l2 = PL(new Vector3(c.x + 6, H - 4, c.z), Color.white, 38f, 3f); var cc = l2.gameObject.AddComponent<ColorCycle>(); cc.speed = .05f; cc.offset = .5f;

            // Gondeln: offen, mit Dachlampen, haengen unten und bleiben waagrecht
            wheelSeats.Clear();
            for (int k = 0; k < 24; k++)
            {
                var gb = new MeshBuilder(); var roof = new MeshBuilder(); var gp = new MeshBuilder(); var gl = new BulbSet();
                Color col = Bright[k % 7];
                gp.Beam(new Vector3(0, 0, 0), new Vector3(0, -1.0f, 0), .08f, Vector3.forward);
                gb.Box(new Vector3(0, -2.65f, 0), Quaternion.identity, new Vector3(1.8f, .1f, 1.9f));        // Boden
                gb.Box(new Vector3(0, -2.3f, .92f), Quaternion.identity, new Vector3(1.8f, .6f, .06f));       // Bruestung
                gb.Box(new Vector3(0, -2.3f, -.92f), Quaternion.identity, new Vector3(1.8f, .6f, .06f));
                gb.Box(new Vector3(.87f, -2.3f, 0), Quaternion.identity, new Vector3(.06f, .6f, 1.9f));
                gb.Box(new Vector3(-.87f, -2.3f, 0), Quaternion.identity, new Vector3(.06f, .6f, 1.9f));
                gb.Box(new Vector3(0, -2.4f, .55f), Quaternion.identity, new Vector3(1.5f, .08f, .45f));      // Baenke
                gb.Box(new Vector3(0, -2.4f, -.55f), Quaternion.identity, new Vector3(1.5f, .08f, .45f));
                foreach (float x in new[] { -.85f, .85f }) foreach (float z in new[] { -.9f, .9f }) gp.Box(new Vector3(x, -1.85f, z), Quaternion.identity, new Vector3(.05f, 1.3f, .05f));
                gp.Box(new Vector3(0, -1.45f, 0), Quaternion.identity, new Vector3(1.9f, .1f, 2.0f));
                roof.Cone(new Vector3(0, -1.2f, 0), 1.4f, .7f, 8, 22.5f);
                roof.Disc(new Vector3(0, -1.2f, 0), 1.4f, 8, Vector3.down);
                for (int i = 0; i < 8; i++) { float a = (i + .5f) / 8f * Mathf.PI * 2; gl.Add(new Vector3(Mathf.Cos(a) * 1.3f, -1.25f, Mathf.Sin(a) * 1.3f), .06f, Bulbs[k % Bulbs.Length], BulbMode.Alternate, 0, 1, .35f, i); }
                var go = new GameObject("Gondel " + (k + 1)).transform; go.SetParent(pivot, false);
                float a0 = k / 24f * Mathf.PI * 2; go.localPosition = new Vector3(0, Mathf.Sin(a0) * R, Mathf.Cos(a0) * R);
                gb.ToObject("Kabine", Paint(col), go); roof.ToObject("Dach", Paint(White), go); gp.ToObject("Gehaenge", Paint(Steel), go);
                gl.Build("Gondellichter", go);
                go.gameObject.AddComponent<Upright>();
                wheelSeats.Add(go);
            }
        }

        // ---------- Pferdekarussell ----------
        static readonly List<Transform> horses = new List<Transform>();
        static void Carousel(Vector3 c)
        {
            var rot = new GameObject("Karussell").transform; rot.SetParent(root, false); rot.position = c;
            rot.gameObject.AddComponent<Spin>().speed = 24f;
            var floor = new MeshBuilder(); var gold = new MeshBuilder(); var canA = new MeshBuilder(); var canB = new MeshBuilder();
            var core = new MeshBuilder(); var mirror = new MeshBuilder(); var cb = new BulbSet();
            floor.Cylinder(new Vector3(0, 0, 0), 10f, .55f, 40);
            core.Cylinder(new Vector3(0, .55f, 0), 2.2f, 4.6f, 16);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2;
                gold.Box(new Vector3(Mathf.Cos(a) * 2.25f, 2.8f, Mathf.Sin(a) * 2.25f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.08f, 3.8f, .6f));
                float a2 = (i + .5f) / 16f * Mathf.PI * 2;
                mirror.Box(new Vector3(Mathf.Cos(a2) * 2.23f, 2.8f, Mathf.Sin(a2) * 2.23f), Quaternion.Euler(0, 90 - a2 * Mathf.Rad2Deg, 0), new Vector3(.75f, 3.2f, .02f));
                for (int j = 0; j < 7; j++) cb.Add(new Vector3(Mathf.Cos(a) * 2.32f, 1.1f + j * .55f, Mathf.Sin(a) * 2.32f), .06f, Warm, BulbMode.Rise, j / 7f, 7, .4f, i);
            }
            // gestreiftes Kegeldach mit Bogenkante, darunter Lichterspeichen
            const int S = 24; Vector3 top = new Vector3(0, 8.2f, 0);
            for (int i = 0; i < S; i++)
            {
                float a0 = i / (float)S * Mathf.PI * 2, a1 = (i + 1) / (float)S * Mathf.PI * 2;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 10.5f, 5.2f, Mathf.Sin(a0) * 10.5f), p1 = new Vector3(Mathf.Cos(a1) * 10.5f, 5.2f, Mathf.Sin(a1) * 10.5f);
                var mb = i % 2 == 0 ? canA : canB;
                mb.Tri(top, p0, p1, up); mb.Tri(top, p0, p1, -up);
                Vector3 m = (p0 + p1) * .5f, o = new Vector3(m.x, 0, m.z).normalized;
                mb.Box(m + o * .02f - up * .45f, Quaternion.LookRotation(o), new Vector3((p1 - p0).magnitude, .9f, .06f));
                cb.Add(m + o * .1f - up * .9f, .14f, Warm, BulbMode.Alternate, 0, 1, .9f, i);
                cb.Add(m + o * .1f - up * .25f, .09f, Warm, BulbMode.Alternate, 0, 1, .5f, i + 1);
                cb.Add(m * .6f + top * .4f + o * .1f, .12f, Warm, BulbMode.Breathe, 0, 1, .7f, i * .3f);
                // Speiche unter dem Dach
                Vector3 inner = new Vector3(Mathf.Cos(a0) * 2.6f, 5.0f, Mathf.Sin(a0) * 2.6f), outer = new Vector3(Mathf.Cos(a0) * 10.2f, 5.0f, Mathf.Sin(a0) * 10.2f);
                for (int j = 0; j <= 9; j++) cb.Add(Vector3.Lerp(inner, outer, j / 9f), .07f, Warm, BulbMode.Wave, i / (float)S, S * 6, .45f, j);
            }
            gold.Sphere(top + up * .3f, .5f, 12); gold.Cylinder(top, .08f, 1.6f, 6);
            floor.ToObject("Boden", Tint(ta != null ? ta.woodDeck : null, new Color(.9f, .7f, .55f)), rot);
            core.ToObject("Mittelsaeule", Lit(new Color(.95f, .9f, .75f), .7f), rot);
            mirror.ToObject("Spiegel", Lit(new Color(.85f, .88f, .9f), .97f, 1f), rot);
            gold.ToObject("Gold", Lit(Gold, .8f, .9f), rot);
            canA.ToObject("Dach rot", Tint(ta != null ? ta.canvas : null, Red), rot); canB.ToObject("Dach weiss", Tint(ta != null ? ta.canvas : null, White), rot);
            cb.Build("Karussell-Lichter", rot);
            PL(new Vector3(c.x, 4.2f, c.z), new Color(1f, .78f, .5f), 22f, 4f);

            // Pferde auf zwei Ringen
            var horse = HorseMesh();
            var poleMat = Lit(Gold, .85f, .9f);
            int idx = 0; horses.Clear();
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
                    horses.Add(h);
                }
            }
            for (int i = 0; i < 40; i++) { float a = i / 40f * Mathf.PI * 2; if (i % 10 == 0) continue; P(Gold).Box(c + new Vector3(Mathf.Cos(a) * 12.5f, .5f, Mathf.Sin(a) * 12.5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.06f, 1f, 2f)); }
        }

        static List<KeyValuePair<Mesh, int>> horseCache;
        static List<KeyValuePair<Mesh, int>> HorseMesh()
        {
            if (horseCache != null) return horseCache;
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
            return horseCache = new List<KeyValuePair<Mesh, int>> {
                new KeyValuePair<Mesh, int>(body.ToMesh("Pferd"), 0), new KeyValuePair<Mesh, int>(saddle.ToMesh("Sattel"), 1), new KeyValuePair<Mesh, int>(gold.ToMesh("Zaum"), 2) };
        }

        // ---------- Kettenkarussell ----------
        static readonly List<Transform> swingSeats = new List<Transform>();
        static void WaveSwinger(Vector3 c)
        {
            P(new Color(.2f, .45f, .75f)).Cylinder(c, 6.5f, .4f, 32);
            P(White).Cylinder(c + up * .4f, .8f, 11f, 16);
            P(Gold).Sphere(c + up * 15.4f, .5f, 10);
            for (int k = 0; k < 6; k++) { float ak = k / 6f * Mathf.PI * 2; B.Line(c + new Vector3(Mathf.Cos(ak) * .86f, .6f, Mathf.Sin(ak) * .86f), c + new Vector3(Mathf.Cos(ak) * .86f, 11f, Mathf.Sin(ak) * .86f), .4f, .06f, BCyan, BulbMode.Rise, .4f); }
            for (int i = 0; i < 64; i++) { float ai = i / 64f * Mathf.PI * 2; B.Add(c + new Vector3(Mathf.Cos(ai) * 6.5f, .42f, Mathf.Sin(ai) * 6.5f), .07f, BBlue, BulbMode.Rainbow, i / 64f, 64, .45f, i); }
            var tilt = new GameObject("Kettenkarussell").transform; tilt.SetParent(root, false); tilt.position = c + up * 11.4f;
            var sw = tilt.gameObject.AddComponent<Swing>(); sw.axis = Vector3.right; sw.amp = 7f; sw.period = 8f;
            var rotor = new GameObject("Rotor").transform; rotor.SetParent(tilt, false);
            rotor.gameObject.AddComponent<Spin>().speed = 55f;
            var a = new MeshBuilder(); var b = new MeshBuilder(); var gl = new BulbSet();
            const int S = 20; Vector3 top = up * 3.6f;
            for (int i = 0; i < S; i++)
            {
                float a0 = i / (float)S * Mathf.PI * 2, a1 = (i + 1) / (float)S * Mathf.PI * 2;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 7.4f, 0, Mathf.Sin(a0) * 7.4f), p1 = new Vector3(Mathf.Cos(a1) * 7.4f, 0, Mathf.Sin(a1) * 7.4f);
                var mb = i % 2 == 0 ? a : b; mb.Tri(top, p0, p1, up); mb.Tri(top, p0, p1, -up);
                Vector3 m = (p0 + p1) * .5f, o = new Vector3(m.x, 0, m.z).normalized;
                mb.Box(m - up * .4f, Quaternion.LookRotation(o), new Vector3((p1 - p0).magnitude, .8f, .05f));
                for (int j = 0; j < 4; j++) gl.Add(Vector3.Lerp(p0, p1, j / 4f) + o * .08f - up * .75f, .1f, BWhite, BulbMode.Rainbow, (i * 4 + j) / (S * 4f), 1, .6f);
                for (int j = 1; j < 6; j++) gl.Add(Vector3.Lerp(top, m, j / 6f) + o * .1f, .09f, BWhite, BulbMode.Wave, j / 6f, 12, .5f);
            }
            a.ToObject("Dach blau", Paint(Blue), rotor); b.ToObject("Dach gelb", Paint(Yellow), rotor);
            gl.Build("Lichter", rotor);
            var ul = PL(new Vector3(0, -1.5f, 0), Color.white, 26f, 3f, rotor); ul.gameObject.AddComponent<ColorCycle>().speed = .12f;
            // Sitze an Ketten, fliehen nach aussen
            var chain = Lit(Steel, .7f, .9f);
            swingSeats.Clear();
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
                ch.ToObject("Kette", chain, piv, false); seat.ToObject("Sessel", Paint(Bright[i % 6]), piv);
                swingSeats.Add(piv);
            }
            Neon("WELLENFLUG", c + new Vector3(0, 2.4f, -1f), 0, .12f, BYellow, 2);
        }

        // ---------- Freifallturm ----------
        static DropRide drop;
        static void DropTower(Vector3 c)
        {
            const float H = 58f;
            P(Dark).Cylinder(c, 6f, .5f, 24);
            var tower = new MeshBuilder();
            tower.Cylinder(c + up * .5f, 1.5f, H, 16);
            for (float y = 4; y < H; y += 3f) for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + .3f; tower.Box(c + new Vector3(Mathf.Cos(a) * 1.55f, y, Mathf.Sin(a) * 1.55f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(.1f, 2.6f, .3f)); }
            tower.ToObject("Turm", Lit(Steel, .6f, .7f), root);
            P(Red).Cylinder(c + up * H, 2.4f, 3f, 20); P(Yellow).Cone(c + up * (H + 3), 2.4f, 2.5f, 20);
            for (int i = 0; i < 40; i++) { float a = i / 40f * Mathf.PI * 2; B.Add(c + new Vector3(Mathf.Cos(a) * 2.45f, H + 1.5f, Mathf.Sin(a) * 2.45f), .14f, BWhite, BulbMode.Rainbow, i / 40f, 40, 1f, i); }
            for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2; B.Add(c + new Vector3(Mathf.Cos(a) * 2.45f, H + .2f, Mathf.Sin(a) * 2.45f), .12f, BRed, BulbMode.Chase, i / 24f, 24, .8f, i); }
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + Mathf.PI / 4; Vector3 o = new Vector3(Mathf.Cos(a) * 1.58f, 0, Mathf.Sin(a) * 1.58f); B.Line(c + o + up * 2, c + o + up * (H - 1), .9f, .1f, BRed, BulbMode.Rise, .9f); }
            B.Add(c + up * (H + 5.6f), .3f, BRed, BulbMode.Alternate, 0, 1, 3f, 1);   // Flugwarnlicht
            var ring = new GameObject("Fallring").transform; ring.SetParent(root, false); ring.position = c + up * 1.2f;
            drop = ring.gameObject.AddComponent<DropRide>(); drop.top = H - 4f; drop.bottom = 1.2f;
            var rb = new MeshBuilder(); var seats = new MeshBuilder(); var rl = new BulbSet();
            rb.Cylinder(new Vector3(0, 0, 0), 2.4f, 1.0f, 24);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var q = Quaternion.LookRotation(o);
                seats.Box(o * 2.75f + up * .5f, q, new Vector3(.6f, .12f, .55f));
                seats.Box(o * 2.5f + up * 1.1f, q, new Vector3(.6f, 1.2f, .12f));
                seats.Box(o * 2.75f + up * 1.45f, q * Quaternion.Euler(30, 0, 0), new Vector3(.5f, .12f, .4f));
            }
            for (int i = 0; i < 36; i++) { float a = i / 36f * Mathf.PI * 2; rl.Add(new Vector3(Mathf.Cos(a) * 2.42f, .5f, Mathf.Sin(a) * 2.42f), .08f, BYellow, BulbMode.Chase, i / 36f, 36, .5f, i); }
            rb.ToObject("Ring", Paint(Yellow), ring); seats.ToObject("Sitze", Lit(Dark, .4f), ring); rl.Build("Ringlichter", ring);
            drop.strobe = PL(new Vector3(0, -1.5f, 0), new Color(.9f, .95f, 1f), 30f, 0f, ring);
            PL(c + up * (H + 2), new Color(1f, .3f, .2f), 25f, 2.5f);
            Neon("FREEFALL", c + new Vector3(0, H + 1.5f, 2.5f), 180f, .14f, new Color(1f, .25f, .2f), 1);
            Neon("FREEFALL", c + new Vector3(0, 4.5f, -1.8f), 0f, .16f, new Color(1f, .25f, .2f), 2);
        }

        // ---------- Schiffschaukel ----------
        static Transform shipPivot; static Swing shipSwing;
        static void PirateShip(Vector3 c)
        {
            const float H = 15f, arm = 11f;
            foreach (float z in new[] { -4f, 4f })
            {
                P(Steel).Beam(c + new Vector3(-8, 0, z), c + new Vector3(0, H, z), .5f, Vector3.forward);
                P(Steel).Beam(c + new Vector3(8, 0, z), c + new Vector3(0, H, z), .5f, Vector3.forward);
                B.Line(c + new Vector3(-7.6f, .4f, z * 1.08f), c + new Vector3(-.4f, H - .6f, z * 1.08f), .5f, .07f, BYellow, BulbMode.Wave, .45f);
                B.Line(c + new Vector3(7.6f, .4f, z * 1.08f), c + new Vector3(.4f, H - .6f, z * 1.08f), .5f, .07f, BYellow, BulbMode.Wave, .45f);
            }
            P(Steel).Loft(new[] { c + new Vector3(0, H, -4.5f), c + new Vector3(0, H, 4.5f) }, new[] { new Vector2(.35f, .35f), new Vector2(.35f, .35f) }, 1, 12, true, true, Vector3.up);
            P(Dark).Box(c + up * .4f, Quaternion.identity, new Vector3(14, .8f, 6));
            var piv = new GameObject("Schiffschaukel").transform; piv.SetParent(root, false); piv.position = c + up * H;
            shipPivot = piv;
            shipSwing = piv.gameObject.AddComponent<Swing>(); shipSwing.axis = Vector3.forward; shipSwing.amp = 62f; shipSwing.period = 7f; shipSwing.cycle = 40f;
            var hull = new MeshBuilder { tile = 1.5f }; var deco = new MeshBuilder(); var sail = new MeshBuilder(); var sl = new BulbSet();
            var ctrl = new[] { new Vector3(-7.5f, -arm + 1.6f, 0), new Vector3(-5, -arm + .3f, 0), new Vector3(0, -arm, 0), new Vector3(5, -arm + .3f, 0), new Vector3(7.5f, -arm + 1.6f, 0) };
            var rad = new[] { new Vector2(.6f, .8f), new Vector2(1.5f, 1.1f), new Vector2(1.8f, 1.2f), new Vector2(1.5f, 1.1f), new Vector2(.6f, .8f) };
            hull.Loft(ctrl, rad, 5, 20, true, true, Vector3.up);
            var rail = new[] { ctrl[0] + up * .7f, ctrl[1] + up * 1f, ctrl[2] + up * 1.05f, ctrl[3] + up * 1f, ctrl[4] + up * .7f };
            deco.Loft(rail, new[] { new Vector2(.7f, .12f), new Vector2(1.6f, .12f), new Vector2(1.9f, .12f), new Vector2(1.6f, .12f), new Vector2(.7f, .12f) }, 5, 16, true, true, Vector3.up);
            foreach (float z in new[] { -1.3f, 1.3f }) { deco.Beam(Vector3.zero + new Vector3(0, 0, z * 2.6f), new Vector3(-5, -arm + 1f, z), .3f, Vector3.forward); deco.Beam(new Vector3(0, 0, z * 2.6f), new Vector3(5, -arm + 1f, z), .3f, Vector3.forward); }
            deco.Cylinder(new Vector3(0, -arm + .8f, 0), .15f, 6f, 8);
            sail.Box(new Vector3(0, -arm + 4.2f, 0), Quaternion.identity, new Vector3(4.2f, 3.6f, .05f));
            // Lichter an der Reling (beidseitig) und an den Armen
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f; int seg = Mathf.Min(3, (int)(t * 4)); float f = t * 4 - seg;
                Vector3 p = Vector3.Lerp(rail[seg], rail[seg + 1], f); float w = Mathf.Lerp(rad[seg].x, rad[seg + 1].x, f) + .08f;
                sl.Add(p + new Vector3(0, .16f, w), .08f, Warm, BulbMode.Chase, t, 40, .5f, i); sl.Add(p + new Vector3(0, .16f, -w), .08f, Warm, BulbMode.Chase, t, 40, .5f, i);
            }
            foreach (float z in new[] { -3.4f, 3.4f }) { sl.Line(new Vector3(0, -.6f, z * 1.02f), new Vector3(-4.8f, -arm + 1.3f, z * .45f), .5f, .07f, BRed, BulbMode.Rise, .4f); sl.Line(new Vector3(0, -.6f, z * 1.02f), new Vector3(4.8f, -arm + 1.3f, z * .45f), .5f, .07f, BRed, BulbMode.Rise, .4f); }
            hull.ToObject("Rumpf", Tint(ta != null ? ta.woodDeck : null, new Color(.7f, .35f, .25f)), piv);
            deco.ToObject("Reling", Lit(Gold, .7f, .7f), piv); sail.ToObject("Segel", Tint(ta != null ? ta.canvas : null, White), piv);
            sl.Build("Schiffslichter", piv);
            Neon("PIRATENSCHIFF", c + new Vector3(0, 1.4f, -3.2f), 0f, .12f, BCyan, 1);
            PL(c + new Vector3(0, 3, -6), new Color(1f, .6f, .3f), 20f, 2.5f);
        }

        // ---------- Autoscooter-Halle ----------
        static readonly List<Transform> scooters = new List<Transform>();
        static void BumperCars(Vector3 c)
        {
            const float W = 26, D = 18, H = 5.2f;
            var floor = new MeshBuilder(); floor.Box(c + up * .15f, Quaternion.identity, new Vector3(W, .3f, D));
            floor.ToObject("Fahrflaeche", Lit(new Color(.18f, .2f, .24f), .9f, .6f), root);
            for (float x = -W / 2; x <= W / 2 + .01f; x += W / 4) foreach (float z in new[] { -D / 2, D / 2 }) P(Steel).Cylinder(c + new Vector3(x, .3f, z), .2f, H, 10);
            var roofM = new MeshBuilder { tile = 2f }; roofM.Box(c + up * (H + .5f), Quaternion.identity, new Vector3(W + 1, .3f, D + 1));
            roofM.ToObject("Dach", ta != null && ta.corrugated != null ? Tint(ta.corrugated, new Color(.5f, .5f, .55f)) : Lit(Dark), root);
            var ceil = new MeshBuilder(); ceil.Box(c + up * (H + .3f), Quaternion.identity, new Vector3(W, .05f, D));
            ceil.ToObject("Fahrdraht-Netz", Lit(new Color(.4f, .4f, .42f), .6f, .8f), root);
            // Deckenlichter (bunt funkelnd) und farbige Strahler
            for (float x = -W / 2 + 1.5f; x < W / 2; x += 2.2f) for (float z = -D / 2 + 1.5f; z < D / 2; z += 2.2f)
                    B.Add(c + new Vector3(x, H + .18f, z), .1f, Bulbs[Random.Range(0, Bulbs.Length)], BulbMode.Twinkle, 0, 1, .5f);
            for (int i = 0; i < 4; i++) { var l = PL(c + new Vector3(-W / 2 + 3.25f + i * 6.5f, H - .3f, 0), Color.white, 13f, 3.5f); var cc = l.gameObject.AddComponent<ColorCycle>(); cc.speed = .2f; cc.offset = i * .25f; }
            foreach (float z in new[] { -D / 2 - .5f, D / 2 + .5f })
            {
                P(Purple).Box(c + new Vector3(0, H + 1.3f, z), Quaternion.identity, new Vector3(W + 1, 1.6f, .15f));
                B.Line(c + new Vector3(-W / 2, H + .55f, z + Mathf.Sign(z) * .12f), c + new Vector3(W / 2, H + .55f, z + Mathf.Sign(z) * .12f), .45f, .1f, BYellow, BulbMode.Chase, .6f);
                B.Line(c + new Vector3(-W / 2, H + 2.05f, z + Mathf.Sign(z) * .12f), c + new Vector3(W / 2, H + 2.05f, z + Mathf.Sign(z) * .12f), .45f, .1f, BPink, BulbMode.Chase, .6f);
                Neon("AUTOSCOOTER", c + new Vector3(0, H + 1.3f, z + Mathf.Sign(z) * .1f), z > 0 ? 180f : 0f, .22f, new Color(.2f, 1f, .6f), 1);
            }
            P(Yellow).Box(c + new Vector3(0, .5f, -D / 2 + .2f), Quaternion.identity, new Vector3(W, .4f, .3f));
            P(Yellow).Box(c + new Vector3(0, .5f, D / 2 - .2f), Quaternion.identity, new Vector3(W, .4f, .3f));
            P(Yellow).Box(c + new Vector3(-W / 2 + .2f, .5f, 0), Quaternion.identity, new Vector3(.3f, .4f, D));
            P(Yellow).Box(c + new Vector3(W / 2 - .2f, .5f, 0), Quaternion.identity, new Vector3(.3f, .4f, D));
            var area = new Rect(c.x - W / 2 + 1.6f, c.z - D / 2 + 1.6f, W - 3.2f, D - 3.2f);
            scooters.Clear();
            for (int i = 0; i < 12; i++)
            {
                var car = new GameObject("Scooter").transform; car.SetParent(root, false);
                car.localPosition = new Vector3(Random.Range(area.xMin, area.xMax), .3f, Random.Range(area.yMin, area.yMax));
                var body = new MeshBuilder(); var rub = new MeshBuilder(); var pole = new MeshBuilder(); var cl = new BulbSet();
                body.Loft(new[] { new Vector3(0, .45f, -.9f), new Vector3(0, .5f, 0), new Vector3(0, .45f, .9f) }, new[] { new Vector2(.6f, .25f), new Vector2(.75f, .3f), new Vector2(.6f, .25f) }, 4, 14, true, true);
                body.Box(new Vector3(0, .8f, -.62f), Quaternion.identity, new Vector3(1.0f, .5f, .2f));
                rub.Loft(new[] { new Vector3(0, .25f, -1.05f), new Vector3(0, .25f, 0), new Vector3(0, .25f, 1.05f) }, new[] { new Vector2(.75f, .14f), new Vector2(.9f, .14f), new Vector2(.75f, .14f) }, 4, 14, true, true);
                pole.Cylinder(new Vector3(0, .7f, -.75f), .03f, H - .7f, 5);
                cl.Add(new Vector3(-.35f, .62f, .92f), .07f, BWhite, BulbMode.Steady, 0, 1, .5f); cl.Add(new Vector3(.35f, .62f, .92f), .07f, BWhite, BulbMode.Steady, 0, 1, .5f);
                cl.Add(new Vector3(0, H - .15f, -.75f), .05f, BCyan, BulbMode.Twinkle, 0, 1, .6f);
                body.ToObject("Karosse", Paint(Bright[i % 7]), car); rub.ToObject("Gummi", Lit(Dark, .3f), car); pole.ToObject("Stange", Lit(Steel, .7f, .9f), car, false);
                cl.Build("Scheinwerfer", car);
                car.gameObject.AddComponent<Scooter>().area = area;
                scooters.Add(car);
            }
            ScooterSparks(c + up * (H + .2f), new Vector3(W - 2, .1f, D - 2));
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
                    // warmes Licht unter den Ketten, damit Besucher und Boden Licht abbekommen
                    PL(new Vector3(x + 6, 5.2f, (z0 + z1) * .5f), new Color(1f, .78f, .55f), cross ? 11f : 9f, cross ? 1.6f : 1.2f);
                    Vector3[] ends = cross ? new[] { a, b, new Vector3(x, 6.5f, z1), new Vector3(x + 12, 6.5f, z0) } : new[] { a, b };
                    for (int e = 0; e + 1 < ends.Length; e += 2)
                    {
                        Vector3 p0 = ends[e], p1 = ends[e + 1]; float len = (p1 - p0).magnitude; int n = Mathf.RoundToInt(len / .7f);
                        Vector3 prev = p0;
                        for (int i = 1; i <= n; i++)
                        {
                            float t = i / (float)n; var p = Vector3.Lerp(p0, p1, t) - up * Mathf.Sin(t * Mathf.PI) * 1.1f;
                            P(Dark).Beam(prev, p, .02f, Vector3.up); prev = p;
                            if (i < n) B.Add(p - up * .1f, .085f, Bulbs[(c++) % Bulbs.Length], cross ? BulbMode.Twinkle : BulbMode.Steady, 0, 1, .55f);
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
            int li = 0;
            foreach (var p in spots)
            {
                P(Dark).Cylinder(p, .12f, 5f, 8);
                P(Dark).Beam(p + up * 4.9f, p + up * 4.9f + Vector3.right * .6f, .08f, Vector3.up);
                B.Add(p + up * 4.7f + Vector3.right * .6f, .22f, new Color(1f, .82f, .55f), BulbMode.Steady, 0, 1, 1.4f);
                if (li++ % 2 == 0) PL(p + up * 4.4f + Vector3.right * .6f, new Color(1f, .78f, .5f), 16f, 2.2f);
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
                foreach (float x in new[] { -.75f, .75f }) B.Line(c + new Vector3(x, .5f, z), c + new Vector3(x, 8.8f, z), .4f, .08f, BWhite, BulbMode.Rise, .5f);
                B.Add(c + new Vector3(0, 11.2f, z), .22f, BRed, BulbMode.Alternate, 0, 1, 1.5f, z > 0 ? 1 : 0);
            }
            P(Blue).Box(c + new Vector3(0, 8f, 0), Quaternion.identity, new Vector3(.6f, 2.4f, 15f));
            foreach (float x in new[] { .35f, -.35f })
            {
                B.Line(c + new Vector3(x, 9.3f, -6.5f), c + new Vector3(x, 9.3f, 6.5f), .35f, .09f, BWhite, BulbMode.Rainbow, .55f);
                B.Line(c + new Vector3(x, 6.7f, -6.5f), c + new Vector3(x, 6.7f, 6.5f), .35f, .09f, BWhite, BulbMode.Rainbow, .55f);
            }
            Neon("RUMMEL", c + new Vector3(.33f, 8f, 0), 270f, .32f, new Color(1f, .3f, .7f), 1, null, 4.5f);
            Neon("RUMMEL", c + new Vector3(-.33f, 8f, 0), 90f, .32f, new Color(1f, .3f, .7f), 1, null, 4.5f);
            PL(c + new Vector3(4, 6, 0), new Color(1f, .5f, .8f), 18f, 3f);
        }

        static void Balloons()
        {
            for (int i = 0; i < 16; i++)
            {
                var b = new GameObject("Ballon").transform; b.SetParent(root, false);
                b.position = new Vector3(Random.Range(-38f, 86f), Random.Range(2.6f, 3.4f), Random.Range(22f, 28f));
                Balloon(b, Vector3.zero, Bright[i % 7], 1.4f);
                var bob = b.gameObject.AddComponent<Bob>(); bob.amp = .12f; bob.speed = 1.3f; bob.phase = i;
            }
        }

        static void Balloon(Transform parent, Vector3 at, Color c, float stringLen)
        {
            var mb = new MeshBuilder(); mb.Loft(new[] { at, at + up * .25f, at + up * .5f }, new[] { new Vector2(.05f, .05f), new Vector2(.28f, .28f), new Vector2(.05f, .05f) }, 4, 10, true, true, Vector3.forward);
            mb.ToObject("Huelle", Lit(c, .92f, .1f), parent, false);
            var s = new MeshBuilder(); s.Beam(at, at + Vector3.down * stringLen, .01f, Vector3.forward); s.ToObject("Schnur", Lit(White), parent, false);
        }
    }
}
