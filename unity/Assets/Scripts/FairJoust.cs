using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>Wippt den sichtbaren Wagen bei Stoessen und laesst Funken an der Stromabnehmer-Stange spruehen. Nur Optik.</summary>
    public class JoustCarFx : MonoBehaviour
    {
        public ParticleSystem sparks; public Transform body;
        public float speed, bounce, tilt, side;
        public System.Action<int> funke;   // Autoscooter-Wagen: Funken mit Blitzlicht (BoxautoLichter)
        float next;
        /// <summary>Rempler: Funkenregen am Stromabnehmer, staerker bei hartem Stoss.</summary>
        public void Hit(float strength)
        {
            int n = Mathf.RoundToInt(15 + Mathf.Clamp(strength, 0, 4) * 20);
            if (funke != null) funke(n); else if (sparks != null) sparks.Emit(n / 2);
        }
        void Update()
        {
            if (body != null) body.localRotation = Quaternion.Euler(-tilt * 2.5f, 0, side * 2.5f);
            if (body != null) body.localPosition = new Vector3(0, bounce, 0);
            if (sparks == null) return;
            if (Time.time > next && speed > .3f)
            {
                next = Time.time + Random.Range(.08f, .9f) / Mathf.Max(.4f, speed);
                sparks.Emit(Random.Range(4, 12));
            }
        }
    }

    /// <summary>Brummen des Elektromotors im eigenen Wagen (Tonhoehe nach Tempo). Nur Ton.</summary>
    public class JoustHum : MonoBehaviour
    {
        AudioSource src;
        void Start()
        {
            src = gameObject.AddComponent<AudioSource>(); src.clip = FairWorld.HumClip(); src.loop = true; src.volume = 0; src.spatialBlend = .6f; src.Play();
        }
        void Update()
        {
            float v = GameController.Riding ? GameController.RideSpeed : 0;
            src.pitch = .7f + v * .28f;
            src.volume = Mathf.MoveTowards(src.volume, GameController.Riding ? .08f + Mathf.Min(1, v / 3.4f) * .22f : 0, Time.deltaTime);
        }
    }

    /// <summary>Boxauto im Burghof der Ritterburg: Turnierzelt mit Fahrflaeche, drei Ritter-Wagen. Nur Optik und Ton.</summary>
    public static partial class FairWorld
    {
        public static Transform[] JoustCars; public static Transform JoustWheel; public static JoustCarFx[] JoustFx;
        public const float JoustGrid = 4.45f;
        public static float JoustFloor = .2f;                                         // Hoehe der Fahrflaeche (Autoscooter-Halle: Podest)
        public static Vector3 JoustEye = new Vector3(0, 1.5f, -.36f);               // Augen im eigenen Wagen
        public static Quaternion JoustWheelRest = Quaternion.Euler(-55, 0, 0);       // Lenkrad in Ruhe, dreht um JoustWheelAxis
        public static Vector3 JoustWheelAxis = Vector3.up;
        public static bool ScooterHall;                                               // Bahn 9 in der Autoscooter-Halle statt im Burghof

        static readonly Color Leather = new Color(.32f, .16f, .08f), Cream = new Color(.93f, .88f, .76f),
            Crimson = new Color(.62f, .07f, .07f), Royal = new Color(.1f, .2f, .58f), Forest = new Color(.07f, .36f, .17f);

        static AudioClip thud, hum;
        /// <summary>Dumpfer Gummi-Stoss (berechnet, kein Fremd-Ton).</summary>
        public static AudioClip ThudClip()
        {
            if (thud != null) return thud;
            int sr = 44100, n = sr * 6 / 10; var d = new float[n]; var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float body = Mathf.Sin(2 * Mathf.PI * (58 + 40 * Mathf.Exp(-t * 30)) * t) * Mathf.Exp(-t * 9);
                float knock = (float)(rnd.NextDouble() * 2 - 1) * Mathf.Exp(-t * 45) * .5f;
                float rattle = Mathf.Sin(2 * Mathf.PI * 190 * t) * Mathf.Exp(-t * 18) * .25f;
                d[i] = Mathf.Clamp((body + knock + rattle) * .8f, -1, 1);
            }
            thud = AudioClip.Create("Rempler", n, 1, sr, false); thud.SetData(d, 0);
            return thud;
        }
        public static AudioClip HumClip()
        {
            if (hum != null) return hum;
            int sr = 44100, n = sr; var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                d[i] = (Mathf.Sin(2 * Mathf.PI * 110 * t) * .5f + Mathf.Sin(2 * Mathf.PI * 220 * t) * .25f + Mathf.Sin(2 * Mathf.PI * 330 * t) * .12f
                    + Mathf.Sin(2 * Mathf.PI * 50 * t) * .3f) * .6f;
            }
            hum = AudioClip.Create("Motor", n, 1, sr, false); hum.SetData(d, 0);
            return hum;
        }

        static GameObject Model(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static void Joust(TrackDef def, Transform parent, ThemeAssets assets, Scenery.Clearance clr)
        {
            root = parent; ta = assets; paint.Clear(); paintMats.Clear(); lightCount = 0; B = new BulbSet();
            CrowdAnim.ReleaseAll();
            JoustFloor = .2f; JoustEye = new Vector3(0, 1.5f, -.36f); JoustWheelRest = Quaternion.Euler(-55, 0, 0); JoustWheelAxis = Vector3.up; ScooterHall = false;
            var bd = def.bumper;
            float cx = (float)bd.cx, cz = (float)bd.cz, hx = (float)bd.hx, hz = (float)bd.hz, Y0 = JoustFloor;
            Vector3 c = new Vector3(cx, 0, cz);

            var oak = new MeshBuilder { tile = 2f }; var planks = new MeshBuilder { tile = 2f }; var stone = new MeshBuilder { tile = 2f };
            var iron = new MeshBuilder(); var gold = new MeshBuilder(); var leather = new MeshBuilder();
            var red = new MeshBuilder { tile = 3f }; var cream = new MeshBuilder { tile = 3f }; var floor = new MeshBuilder(); var seams = new MeshBuilder();

            // ---- Fahrflaeche: Stahlplatten auf einem Holzpodest ----
            planks.Box(c + up * (Y0 * .5f - .02f), Quaternion.identity, new Vector3(2 * hx + 1.4f, Y0, 2 * hz + 1.4f));
            floor.Box(c + up * (Y0 - .005f), Quaternion.identity, new Vector3(2 * hx, .02f, 2 * hz));
            for (float x = cx - hx + 2; x < cx + hx - .1f; x += 2) seams.Box(new Vector3(x, Y0 + .008f, cz), Quaternion.identity, new Vector3(.03f, .004f, 2 * hz));
            for (float z = cz - hz + 2; z < cz + hz - .1f; z += 2) seams.Box(new Vector3(cx, Y0 + .009f, z), Quaternion.identity, new Vector3(2 * hx, .004f, .03f));

            // ---- Bande: Holzplanken, innen ein Lederpolster mit Messingnieten ----
            void Rail(Vector3 a, Vector3 b, Vector3 inward)
            {
                Vector3 d = b - a; float len = d.magnitude; var q = Quaternion.LookRotation(d / len, up); Vector3 m = (a + b) * .5f;
                planks.Box(m - inward * .3f + up * (Y0 + .5f), q, new Vector3(.3f, 1.0f, len + .6f));
                oak.Box(m - inward * .3f + up * (Y0 + 1.05f), q, new Vector3(.42f, .12f, len + .7f));
                leather.Box(m - inward * .08f + up * (Y0 + .34f), q, new Vector3(.16f, .42f, len));
                for (float s = .4f; s < len; s += .6f)
                {
                    Vector3 p = a + d / len * s - inward * .0f + up * (Y0 + .34f);
                    gold.Box(p + inward * .005f, q, new Vector3(.03f, .05f, .05f));
                    gold.Box(p + inward * .005f + up * .14f, q, new Vector3(.03f, .05f, .05f));
                    gold.Box(p + inward * .005f - up * .14f, q, new Vector3(.03f, .05f, .05f));
                }
            }
            Vector3 A = new Vector3(cx - hx, 0, cz - hz), Bp = new Vector3(cx + hx, 0, cz - hz), C = new Vector3(cx + hx, 0, cz + hz), D = new Vector3(cx - hx, 0, cz + hz);
            Rail(A, Bp, Vector3.forward); Rail(Bp, C, Vector3.left); Rail(C, D, Vector3.back); Rail(D, A, Vector3.right);
            // ---- Hindernisse: Brunnen-Insel mit Loewenkoepfen und gepolsterte Eichenpfeiler (gleiche Kreise wie in der Physik) ----
            var water = new MeshBuilder();
            var obs = bd.obstacles ?? new double[0];
            for (int ob = 0; ob + 2 < obs.Length; ob += 3)
            {
                Vector3 o = new Vector3((float)obs[ob], 0, (float)obs[ob + 1]); float r = (float)obs[ob + 2];
                leather.Cylinder(o + up * (Y0 + .12f), r, .44f, Mathf.Max(16, Mathf.RoundToInt(r * 28)), false);   // Polster = Stossflaeche
                int nr = Mathf.Max(8, Mathf.RoundToInt(r * 2 * Mathf.PI / .45f));
                for (int q = 0; q < nr; q++)
                {
                    float a = q / (float)nr * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    gold.Box(o + d * (r + .005f) + up * (Y0 + .34f), Quaternion.LookRotation(d), new Vector3(.05f, .05f, .02f));
                }
                if (r > 1f)
                {
                    stone.Cylinder(o + up * Y0, r - .1f, .85f, 40, true);                       // Brunnenrand
                    water.Disc(o + up * (Y0 + .78f), r - .32f, 40, up);
                    stone.Cylinder(o + up * Y0, .36f, 2.5f, 18, true);                          // Saeule
                    stone.Cylinder(o + up * (Y0 + 2.45f), .55f, .2f, 18, true);
                    gold.Sphere(o + up * (Y0 + 2.9f), .26f, 14);
                    for (int q = 0; q < 4; q++)
                    {
                        Vector3 d = Quaternion.Euler(0, 45 + q * 90, 0) * Vector3.forward;
                        if (ta != null && ta.lionHead != null) Model(ta.lionHead, root, o + d * .36f + up * (Y0 + 1.55f), Quaternion.LookRotation(d), 1.1f);
                    }
                    FlagWave.Create(root, o + up * (Y0 + 4.0f), 1.4f, .7f, Paint(Crimson), 90);
                    iron.Box(o + up * (Y0 + 3.45f), Quaternion.identity, new Vector3(.05f, 1.1f, .05f));
                }
                else
                {
                    oak.Cylinder(o, r - .1f, JoustGrid + .05f, 14, true);                        // traegt das Fahrdraht-Netz
                    stone.Cylinder(o, r + .05f, Y0 + .1f, 14, true);
                    foreach (float y in new[] { 1.6f, 2.9f, 4.0f }) gold.Cylinder(o + up * y, r - .07f, .07f, 14, false);
                    if (ta != null && ta.kiteShield != null)
                        foreach (float yaw in new[] { 0f, 180f })
                            Model(ta.kiteShield, root, o + Quaternion.Euler(0, yaw, 0) * Vector3.forward * (r - .02f) + up * 2.3f, Quaternion.Euler(0, yaw, 0), .5f);
                }
            }
            water.ToObject("Brunnenwasser", Lit(new Color(.08f, .16f, .17f), .95f), root, false);

            // Tor in der vorderen Bande (Fahrgaeste steigen hier ein)
            {
                Vector3 g = new Vector3(cx, 0, cz - hz - .3f);
                foreach (float sx in new[] { -1.2f, 1.2f }) { oak.Box(g + new Vector3(sx, Y0 + .75f, 0), Quaternion.identity, new Vector3(.22f, 1.5f, .45f)); gold.Sphere(g + new Vector3(sx, Y0 + 1.6f, 0), .14f, 10); }
                foreach (float y in new[] { .35f, .85f }) iron.Box(g + new Vector3(0, Y0 + y, -.17f), Quaternion.identity, new Vector3(2.2f, .07f, .02f));
            }

            // ---- Turnierzelt: Eichenpfosten, Rahmen, Stromnetz, gestreiftes Dach ----
            float ox = hx + .8f, oz = hz + .8f, eave = 5.4f, ridge = 9.4f, ex = hx + 2.0f, ez = hz + 2.0f;
            var posts = new List<(Vector3 p, Vector3 inward)>();
            int nx = 7, nz = 4;
            for (int i = 0; i < nx; i++) { float x = cx - ox + 2 * ox * i / (nx - 1); posts.Add((new Vector3(x, 0, cz - oz), Vector3.forward)); posts.Add((new Vector3(x, 0, cz + oz), Vector3.back)); }
            for (int i = 1; i < nz - 1; i++) { float z = cz - oz + 2 * oz * i / (nz - 1); posts.Add((new Vector3(cx - ox, 0, z), Vector3.right)); posts.Add((new Vector3(cx + ox, 0, z), Vector3.left)); }
            foreach (var (p, inw) in posts)
            {
                stone.Box(p + up * .3f, Quaternion.identity, new Vector3(.75f, .6f, .75f));
                oak.Box(p + up * (eave / 2 + .3f), Quaternion.identity, new Vector3(.38f, eave - .6f, .38f));
                // Kopfbaender
                foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    Vector3 q = p + dir; if (Mathf.Abs(q.x - cx) > ox + .1f || Mathf.Abs(q.z - cz) > oz + .1f) continue;
                    oak.Beam(p + up * (eave - 1.3f), p + dir * 1.1f + up * (eave - .1f), .18f, up);
                }
                gold.Box(p + up * (eave - .6f) + inw * .2f, Quaternion.LookRotation(inw), new Vector3(.42f, .1f, .02f));
            }
            // Rahmen ringsum und Zugbalken quer
            foreach (float sz in new[] { -1f, 1f }) oak.Box(new Vector3(cx, eave, cz + sz * oz), Quaternion.identity, new Vector3(2 * ox + .5f, .42f, .32f));
            foreach (float sx in new[] { -1f, 1f }) oak.Box(new Vector3(cx + sx * ox, eave, cz), Quaternion.identity, new Vector3(.32f, .42f, 2 * oz + .5f));
            for (int i = 0; i < nx; i++)
            {
                float x = cx - ox + 2 * ox * i / (nx - 1);
                oak.Box(new Vector3(x, eave + .1f, cz), Quaternion.identity, new Vector3(.3f, .36f, 2 * oz));
                oak.Box(new Vector3(x, (eave + ridge) / 2, cz), Quaternion.identity, new Vector3(.28f, ridge - eave - .2f, .28f));   // Koenigspfosten
                foreach (float sz in new[] { -1f, 1f }) oak.Beam(new Vector3(x, eave + .25f, cz + sz * (oz - .2f)), new Vector3(x, ridge - .35f, cz), .24f, up);   // Sparren
                foreach (float sz in new[] { -1f, 1f }) iron.Beam(new Vector3(x, eave, cz + sz * oz * .6f), new Vector3(x, JoustGrid, cz + sz * oz * .6f), .03f, Vector3.right);
            }
            oak.Box(new Vector3(cx, ridge - .25f, cz), Quaternion.identity, new Vector3(2 * ex, .34f, .3f));   // Firstbalken
            // Fahrdraht-Netz
            for (float x = cx - hx; x <= cx + hx + .01f; x += 1.1f) iron.Box(new Vector3(x, JoustGrid, cz), Quaternion.identity, new Vector3(.025f, .025f, 2 * hz));
            for (float z = cz - hz; z <= cz + hz + .01f; z += 1.1f) iron.Box(new Vector3(cx, JoustGrid + .03f, z), Quaternion.identity, new Vector3(2 * hx, .025f, .025f));
            foreach (float sz in new[] { -1f, 1f }) iron.Box(new Vector3(cx, JoustGrid, cz + sz * hz), Quaternion.identity, new Vector3(2 * hx + .1f, .08f, .08f));
            foreach (float sx in new[] { -1f, 1f }) iron.Box(new Vector3(cx + sx * hx, JoustGrid, cz), Quaternion.identity, new Vector3(.08f, .08f, 2 * hz + .1f));

            // Dach: Bahnen abwechselnd Rot und Leinen, von beiden Seiten sichtbar
            void Two(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 cc, Vector3 o) { mb.Tri(a, b, cc, o); mb.Tri(a, b, cc, -o); }
            int k = 0;
            float step = (2 * ex) / Mathf.Round(2 * ex / 1.5f);
            for (float x = cx - ex; x < cx + ex - .01f; x += step, k++)
            {
                var mb = k % 2 == 0 ? red : cream; float x1 = x + step;
                foreach (float sz in new[] { -1f, 1f })
                {
                    Vector3 o = new Vector3(0, 1, sz * .6f);
                    Vector3 e0 = new Vector3(x, eave + .2f, cz + sz * ez), e1 = new Vector3(x1, eave + .2f, cz + sz * ez), r0 = new Vector3(x, ridge, cz), r1 = new Vector3(x1, ridge, cz);
                    Two(mb, e0, e1, r1, o); Two(mb, e0, r1, r0, o);
                }
            }
            // Giebel: senkrechte Bahnen
            foreach (float sx in new[] { -1f, 1f })
            {
                float gx = cx + sx * ex; int kk = 0;
                float gstep = (2 * ez) / Mathf.Round(2 * ez / 1.5f);
                float H(float z) => eave + .2f + (ridge - eave - .2f) * (1 - Mathf.Abs(z - cz) / ez);
                for (float z = cz - ez; z < cz + ez - .01f; z += gstep, kk++)
                {
                    float z1 = z + gstep; var mb = kk % 2 == 0 ? red : cream;
                    var cuts = new List<float> { z }; if (z < cz && z1 > cz) cuts.Add(cz); cuts.Add(z1);
                    for (int i = 0; i + 1 < cuts.Count; i++)
                    {
                        float za = cuts[i], zb = cuts[i + 1];
                        Vector3 a0 = new Vector3(gx, eave + .2f, za), b0 = new Vector3(gx, eave + .2f, zb), a1 = new Vector3(gx, H(za), za), b1 = new Vector3(gx, H(zb), zb);
                        Two(mb, a0, b0, b1, Vector3.right * sx); Two(mb, a0, b1, a1, Vector3.right * sx);
                    }
                }
                // Firstknauf und Wimpel
                gold.Sphere(new Vector3(gx, ridge + .25f, cz), .25f, 10);
                iron.Box(new Vector3(gx, ridge + 1.6f, cz), Quaternion.identity, new Vector3(.07f, 2.8f, .07f));
                FlagWave.Create(root, new Vector3(gx, ridge + 2.9f, cz), 2.2f, .9f, Paint(Crimson), sx > 0 ? 0 : 180);
            }
            // Zackenbehang an der Traufe
            void Scallops(Vector3 a, Vector3 b, Vector3 outward)
            {
                Vector3 d = b - a; float len = d.magnitude; int n = Mathf.RoundToInt(len / .75f); Vector3 f = d / n;
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = a + f * i;
                    var mb = i % 2 == 0 ? P(new Color(.8f, .6f, .16f)) : red;
                    Two(mb, p, p + f, p + f * .5f - up * .65f, outward);
                }
            }
            foreach (float sz in new[] { -1f, 1f }) Scallops(new Vector3(cx - ex, eave + .2f, cz + sz * ez), new Vector3(cx + ex, eave + .2f, cz + sz * ez), Vector3.forward * sz);
            foreach (float sx in new[] { -1f, 1f }) Scallops(new Vector3(cx + sx * ex, eave + .2f, cz - ez), new Vector3(cx + sx * ex, eave + .2f, cz + ez), Vector3.right * sx);

            // ---- Kronleuchter (Poly Haven) mit Kerzenlicht ----
            for (int i = -2; i <= 2; i++)
            {
                Vector3 p = new Vector3(cx + i * 6.4f, 6.3f, cz);
                iron.Box(new Vector3(p.x, (p.y + ridge - .4f) * .5f, p.z), Quaternion.identity, new Vector3(.04f, ridge - .4f - p.y, .04f));   // Kette
                if (ta != null && ta.chandelier != null) Model(ta.chandelier, root, p, Quaternion.identity, 1.7f);
                else gold.Cylinder(p, .6f, .3f, 12);
                var l = PL(p - up * .8f, new Color(1f, .72f, .42f), 17f, 3.2f);
                l.gameObject.AddComponent<Flicker>().baseIntensity = 3.2f;
            }
            foreach (float sx in new[] { -1f, 1f }) PL(new Vector3(cx + sx * (hx - 2), 3.2f, cz), new Color(1f, .8f, .55f), 12f, 1.4f);

            // ---- Wappenschilde, Waffen, Loewenkoepfe ----
            int si = 0;
            foreach (var (p, inw) in posts)
            {
                if (ta != null && ta.kiteShield != null)
                    Model(ta.kiteShield, root, p + inw * .24f + up * 3.1f, Quaternion.LookRotation(inw) * Quaternion.Euler(0, si % 2 == 0 ? 0 : 0, 0), .75f);
                else gold.Box(p + inw * .22f + up * 3.1f, Quaternion.LookRotation(inw), new Vector3(.55f, .8f, .05f));
                si++;
            }
            foreach (var (p, inw) in posts)
            {
                bool corner = Mathf.Abs(Mathf.Abs(p.x - cx) - ox) < .1f && Mathf.Abs(Mathf.Abs(p.z - cz) - oz) < .1f;
                if (!corner) continue;
                Vector3 outw = -inw;
                if (ta != null && ta.estoc != null) Model(ta.estoc, root, p + outw * .25f + up * 2.0f, Quaternion.LookRotation(outw) * Quaternion.Euler(0, 0, 28), .9f);
                if (ta != null && ta.warHammer != null) Model(ta.warHammer, root, p + outw * .25f + up * 2.0f, Quaternion.LookRotation(outw) * Quaternion.Euler(0, 0, -28), 1.1f);
            }
            if (ta != null && ta.lionHead != null)
                foreach (float sx in new[] { -1f, 1f })
                    Model(ta.lionHead, root, new Vector3(cx + sx * (ox + .25f), eave - .05f, cz), Quaternion.LookRotation(Vector3.right * sx), 1.6f);

            // ---- Vor dem Zelt: Kasse, Feuerschalen, Faesser, Kanonen ----
            {
                Vector3 kb = new Vector3(cx - 11f, 0, cz - hz - 3.1f);
                planks.Box(kb + up * .55f, Quaternion.identity, new Vector3(2.4f, 1.1f, 1.6f));
                oak.Box(kb + up * 1.15f, Quaternion.identity, new Vector3(2.6f, .1f, 1.8f));
                foreach (float sx in new[] { -1.15f, 1.15f }) foreach (float sz in new[] { -.75f, .75f }) oak.Box(kb + new Vector3(sx, 1.5f, sz), Quaternion.identity, new Vector3(.14f, 3f, .14f));
                var th = new MeshBuilder { tile = 1.4f }; th.Gable(kb + up * 3f, Quaternion.identity, 3f, 2.2f, 1.2f, .3f);
                th.ToObject("Kassendach", ta != null ? ta.roofThatch : null, root);
                if (ta != null && ta.chest != null) Model(ta.chest, root, kb + up * 1.2f, Quaternion.Euler(0, 180, 0), .55f);
                PL(kb + up * 2.4f + Vector3.back * .4f, new Color(1f, .7f, .4f), 6f, 1.2f);
                Neon("KASSE", kb + new Vector3(0, 2.6f, -.95f), 0, .07f, new Color(1f, .78f, .35f), 0, null, 1.6f);
            }
            Neon("BOXAUTO-TURNIER", new Vector3(cx, eave + .9f, cz - ez - .05f), 0, .2f, new Color(1f, .75f, .3f), 0, null, 2.2f);
            oak.Box(new Vector3(cx, eave + .9f, cz - ez + .05f), Quaternion.identity, new Vector3(8.6f, 1.1f, .1f));
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 fp = new Vector3(cx + sx * 3.6f, 0, cz - hz - 3.1f);
                if (ta != null && ta.firePit != null) Model(ta.firePit, root, fp, Quaternion.Euler(0, Random.Range(0, 360f), 0), .8f);
                Fx.Fire(root, fp + up * .25f, ta != null ? ta.fire : null, 2.4f);
                var lg = new GameObject("Feuerschale"); lg.transform.SetParent(root, false); lg.transform.position = fp + up * 1.0f;
                var lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 8f; lt.color = new Color(1f, .55f, .22f); lt.shadows = LightShadows.None;
                lg.AddComponent<Flicker>().baseIntensity = 2.6f;
            }
            if (ta != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float sx = i % 2 == 0 ? -1 : 1; float z = cz - hz + 2.5f + (i / 2) * 7f;
                    Vector3 bp = new Vector3(cx + sx * (ox + 1.1f), 0, z);
                    Model(ta.wineBarrel, root, bp, Quaternion.Euler(0, Random.Range(0, 360f), 0), 1f);
                    Model(ta.bucket, root, bp + new Vector3(0, 0, 1.0f), Quaternion.Euler(0, Random.Range(0, 360f), 0), 1f);
                }
                if (ta.cannon != null)
                    foreach (float sx in new[] { -1f, 1f }) Model(ta.cannon, root, new Vector3(cx + (sx < 0 ? -6.8f : 8.5f), 0, cz - hz - 3.1f), Quaternion.Euler(0, 180, 0), 1f);
            }

            // ---- Wagen ----
            Color[] main = { Crimson, Royal, Forest }, trim = { new Color(.85f, .66f, .2f), new Color(.82f, .84f, .88f), new Color(.85f, .66f, .2f) };
            JoustCars = new Transform[3]; JoustFx = new JoustCarFx[3]; JoustWheel = null;
            for (int i = 0; i < 3; i++)
            {
                var car = new GameObject(i == 0 ? "Boxauto Spieler" : "Boxauto Ritter " + i).transform; car.SetParent(root, false);
                var body = new GameObject("Aufbau").transform; body.SetParent(car, false);
                var rub = new MeshBuilder(); var shell = new MeshBuilder(); var tr = new MeshBuilder(); var seat = new MeshBuilder(); var dk = new MeshBuilder();
                // Runde Form: Grundriss folgt der Kapsel aus der Physik (Mittelpunkte +-0,36 m, Gummiring 0,8 m)
                float W(float z, float k) { float a = Mathf.Abs(z) - .36f; return a <= 0 ? .8f * k : Mathf.Sqrt(Mathf.Max(.0f, .64f - a * a)) * k; }
                var ringP = new List<Vector3>(); var ringR = new List<Vector2>(); var shP = new List<Vector3>(); var shR = new List<Vector2>(); var beP = new List<Vector3>(); var beR = new List<Vector2>();
                for (int s = 0; s <= 12; s++)
                {
                    float z = -1.1f + 2.2f * s / 12f;
                    ringP.Add(new Vector3(0, .26f, z)); ringR.Add(new Vector2(Mathf.Max(.16f, W(z, 1f)), .14f));
                    float zb = z * .9f, lift = Mathf.Max(0, zb) * .12f;
                    shP.Add(new Vector3(0, .5f + lift, zb)); shR.Add(new Vector2(Mathf.Max(.12f, W(zb / .92f, .86f)), .27f + lift * .5f));
                    beP.Add(new Vector3(0, .52f + lift, zb)); beR.Add(new Vector2(Mathf.Max(.14f, W(zb / .92f, .86f) + .03f), .035f));
                }
                rub.Loft(ringP, ringR, 3, 28, true, true);
                dk.Box(new Vector3(0, .12f, 0), Quaternion.identity, new Vector3(.9f, .14f, 1.4f));
                shell.Loft(shP, shR, 3, 28, true, true);
                tr.Loft(beP, beR, 3, 28, true, true);   // Zierleiste rundum
                // Armaturen-Wulst, Sitzpolster und Lehne als runde Polster (quer zum Wagen)
                Vector3[] Arc(float y, float z, float bend, float half) => new[] { new Vector3(-half, y, z + bend), new Vector3(-half * .6f, y, z + bend * .35f), new Vector3(0, y, z), new Vector3(half * .6f, y, z + bend * .35f), new Vector3(half, y, z + bend) };
                Vector2[] Pill(float d, float h) => new[] { new Vector2(d * .45f, h * .5f), new Vector2(d * .9f, h * .92f), new Vector2(d, h), new Vector2(d * .9f, h * .92f), new Vector2(d * .45f, h * .5f) };
                shell.Loft(Arc(.8f, .62f, -.12f, .44f), Pill(.24f, .15f), 4, 16, true, true);
                seat.Loft(Arc(.68f, -.36f, .05f, .44f), Pill(.3f, .1f), 4, 16, true, true);
                seat.Loft(Arc(1.02f, -.74f, .2f, .5f), Pill(.11f, .38f), 4, 16, true, true);
                tr.Loft(Arc(1.4f, -.74f, .2f, .48f), Pill(.06f, .06f), 4, 12, true, true);
                foreach (float sx in new[] { -.32f, .32f }) tr.Sphere(new Vector3(sx, .74f, .92f), .07f, 10);
                // Stromabnehmer-Stange hinten mit Wimpel
                float poleH = JoustGrid - Y0 - .05f;
                dk.Cylinder(new Vector3(0, .9f, -.95f), .035f, poleH - .9f, 6);
                dk.Box(new Vector3(0, poleH - .02f, -.95f), Quaternion.identity, new Vector3(.08f, .04f, .25f));
                // Lenkrad (dreht sich mit)
                var wheel = new GameObject("Lenkrad").transform; wheel.SetParent(body, false);
                wheel.localPosition = new Vector3(0, .98f, .26f); wheel.localRotation = Quaternion.Euler(-55, 0, 0);
                var wm = new MeshBuilder(); const int ws = 16;
                for (int s = 0; s < ws; s++)
                {
                    float a0 = s / (float)ws * Mathf.PI * 2, a1 = (s + 1) / (float)ws * Mathf.PI * 2;
                    wm.Beam(new Vector3(Mathf.Cos(a0) * .2f, 0, Mathf.Sin(a0) * .2f), new Vector3(Mathf.Cos(a1) * .2f, 0, Mathf.Sin(a1) * .2f), .035f, Vector3.up);
                }
                for (int s = 0; s < 3; s++) { float a = s / 3f * Mathf.PI * 2 + Mathf.PI / 2; wm.Beam(Vector3.zero, new Vector3(Mathf.Cos(a) * .19f, 0, Mathf.Sin(a) * .19f), .025f, Vector3.up); }
                wm.Cylinder(new Vector3(0, -.03f, 0), .05f, .06f, 10);
                wm.ToObject("Kranz", Lit(Iron, .5f, .6f), wheel, false);
                dk.Beam(new Vector3(0, .7f, .5f), new Vector3(0, .96f, .28f), .04f, Vector3.up);

                rub.ToObject("Gummiring", Lit(new Color(.04f, .04f, .045f), .25f), body);
                dk.ToObject("Fahrwerk", Lit(Iron, .5f, .8f), body);
                shell.ToObject("Karosse", Paint(main[i]), body);
                tr.ToObject("Zierleiste", Lit(trim[i], .7f, .85f), body);
                seat.ToObject("Sitz", Lit(Leather, .35f), body);
                if (ta != null && ta.kiteShield != null) Model(ta.kiteShield, body, new Vector3(0, .68f, 1.05f), Quaternion.Euler(-12, 0, 0), .42f);
                FlagWave.Create(body, new Vector3(0, 2.6f, -.98f), .7f, .4f, Paint(main[i]), 180);
                // Funken unter dem Netz
                var sp = new GameObject("Funken"); sp.transform.SetParent(car, false); sp.transform.localPosition = new Vector3(0, poleH + .02f, -.95f);
                var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var mn = ps.main; mn.loop = false; mn.playOnAwake = false; mn.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .4f);
                mn.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f); mn.startSize = new ParticleSystem.MinMaxCurve(.02f, .05f); mn.gravityModifier = 1.2f;
                mn.startColor = new Color(.75f, .85f, 1f); mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.maxParticles = 60;
                var em = ps.emission; em.rateOverTime = 0;
                var shp = ps.shape; shp.shapeType = ParticleSystemShapeType.Sphere; shp.radius = .03f;
                var psr = sp.GetComponent<ParticleSystemRenderer>(); psr.sharedMaterial = ta != null && ta.spark != null ? ta.spark : Emit(new Color(.7f, .8f, 1f), 6);
                var fx = car.gameObject.AddComponent<JoustCarFx>(); fx.sparks = ps; fx.body = body;
                if (i == 0) { JoustWheel = wheel; car.gameObject.AddComponent<JoustHum>(); }
                JoustCars[i] = car; JoustFx[i] = fx;
            }

            // ---- Zuschauer an der Bande, Ritter in den anderen Wagen ----
            templates = CrowdBank.Get(ta);
            if (templates != null && templates.Count > 0)
            {
                people = new GameObject("Zuschauer").transform; people.SetParent(root, false);
                FairPerson.blockers.Clear();
                for (int i = 1; i < 3; i++) Rider(JoustCars[i].GetChild(0), new Vector3(0, .62f, -.42f), Quaternion.identity, false);
                for (int i = 0; i < 18; i++)
                {
                    Vector3 p; float yaw;
                    int side = i % 4;
                    float t = Random.Range(-.85f, .85f);
                    if (side == 0) { p = new Vector3(cx + t * hx, 0, cz + hz + 1.5f); yaw = 180; }
                    else if (side == 1) { p = new Vector3(cx - hx - 1.6f, 0, cz + t * hz); yaw = 90; }
                    else if (side == 2) { p = new Vector3(cx + hx + 1.6f, 0, cz + t * hz); yaw = 270; }
                    else { float tt = Random.value < .5f ? Random.Range(-.85f, -.2f) : Random.Range(.2f, .5f); p = new Vector3(cx + tt * hx, 0, cz - hz - 1.5f); yaw = 0; }
                    var w = Spawn(p, yaw + Random.Range(-20f, 20f), FairPerson.Act.Watcher, Random.value < .3f);
                    if (w != null) w.lookAt = new Vector3(cx, 1, cz);
                }
            }

            // ---- Toene ----
            if (ta != null) FairSound.Loop(root, c + up * 3, ta.bumperCars, .35f, 6, 60);

            // ---- Meshes ----
            oak.ToObject("Eichenholz", ta != null ? ta.woodStructure : Lit(Oak), root);
            planks.ToObject("Planken", ta != null ? Tint(ta.woodDeck, new Color(.75f, .6f, .45f)) : Lit(Oak), root);
            stone.ToObject("Steinsockel", ta != null ? ta.stoneBlocks : Lit(Color.gray), root);
            iron.ToObject("Eisen", Lit(Iron, .45f, .85f), root);
            gold.ToObject("Messing", Lit(new Color(.78f, .6f, .22f), .7f, .9f), root);
            leather.ToObject("Lederpolster", Lit(Leather, .45f), root);
            red.ToObject("Zelt rot", ta != null && ta.canvas != null ? Tint(ta.canvas, Crimson) : Lit(Crimson, .2f), root);
            cream.ToObject("Zelt Leinen", ta != null && ta.canvas != null ? Tint(ta.canvas, Cream) : Lit(Cream, .2f), root);
            floor.ToObject("Stahlboden", Lit(new Color(.34f, .35f, .37f), .62f, .8f), root);
            seams.ToObject("Plattenfugen", Lit(new Color(.05f, .05f, .05f), .3f), root, false);
            foreach (var kv in paint) kv.Value.ToObject("Turnier " + kv.Key, Paint(kv.Key), root);
            B.Build("Lichter", root);
            clr.Block(new Rect(cx - ex - 1, cz - ez - 1, 2 * ex + 2, 2 * ez + 2));
        }
    }
}
