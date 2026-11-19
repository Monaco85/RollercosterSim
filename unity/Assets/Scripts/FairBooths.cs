using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>Gluecksrad: dreht schnell an, laeuft aus, bleibt stehen, dann von vorn.</summary>
    public class FortuneWheel : MonoBehaviour
    {
        float speed, wait; Vector3 axis = Vector3.forward;
        void Start() { wait = Random.Range(0f, 4f); }
        void Update()
        {
            if (speed <= 0) { wait -= Time.deltaTime; if (wait < 0) { speed = Random.Range(500f, 800f); } return; }
            transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
            speed -= Time.deltaTime * Mathf.Max(60f, speed * .35f);
            if (speed <= 0) { speed = 0; wait = Random.Range(3f, 7f); }
        }
    }

    /// <summary>Ziele in der Schiessbude: laufen auf einer Schiene hin und her, fallen bei Treffer um.</summary>
    public class TargetRow : MonoBehaviour
    {
        public float width = 4f, speed = .6f; readonly List<Transform> t = new List<Transform>(); readonly List<float> down = new List<float>();
        public void Add(Transform x) { t.Add(x); down.Add(0); }
        public void Hit() { if (t.Count == 0) return; int i = Random.Range(0, t.Count); down[i] = 1.6f; }
        void Update()
        {
            for (int i = 0; i < t.Count; i++)
            {
                float x = Mathf.Repeat(i / (float)t.Count * width + Time.time * speed, width) - width / 2;
                down[i] = Mathf.Max(0, down[i] - Time.deltaTime);
                t[i].localPosition = new Vector3(x, t[i].localPosition.y, t[i].localPosition.z);
                t[i].localRotation = Quaternion.Euler(down[i] > 0 ? -80f : 0f, 0, 0);
            }
        }
    }

    /// <summary>Dosenpyramide: bei Treffer fliegen die Dosen auseinander und werden nach kurzer Zeit neu gestapelt.</summary>
    public class CanStack : MonoBehaviour
    {
        readonly List<Transform> cans = new List<Transform>(); readonly List<Vector3> home = new List<Vector3>(); readonly List<Vector3> vel = new List<Vector3>(); readonly List<Vector3> spin = new List<Vector3>();
        float reset = -1;
        public Vector3 Center => transform.TransformPoint(new Vector3(0, .35f, 0));
        public void Add(Transform c) { cans.Add(c); home.Add(c.localPosition); vel.Add(Vector3.zero); spin.Add(Vector3.zero); }
        public void Hit(Vector3 dirLocal)
        {
            if (reset > 0) return;
            for (int i = 0; i < cans.Count; i++)
            {
                if (Random.value < .25f && i < 3) continue;   // nicht immer alle
                vel[i] = (dirLocal * Random.Range(1.5f, 3f) + new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2.5f), Random.Range(-.5f, .5f)));
                spin[i] = Random.insideUnitSphere * 720f;
            }
            reset = 3.5f;
        }
        void Update()
        {
            if (reset <= 0) return;
            reset -= Time.deltaTime;
            for (int i = 0; i < cans.Count; i++)
            {
                if (vel[i] == Vector3.zero) continue;
                var p = cans[i].localPosition + vel[i] * Time.deltaTime; var v = vel[i]; v.y -= 9.8f * Time.deltaTime;
                if (p.y < -.35f) { p.y = -.35f; v = new Vector3(v.x * .4f, -v.y * .25f, v.z * .4f); if (v.sqrMagnitude < .05f) { v = Vector3.zero; spin[i] = Vector3.zero; } }
                vel[i] = v; cans[i].localPosition = p; cans[i].Rotate(spin[i] * Time.deltaTime, Space.Self);
            }
            if (reset <= 0) for (int i = 0; i < cans.Count; i++) { cans[i].localPosition = home[i]; cans[i].localRotation = Quaternion.identity; vel[i] = Vector3.zero; spin[i] = Vector3.zero; }
        }
    }

    /// <summary>Wurfball: fliegt im Bogen vom Werfer zur Dosenpyramide und loest den Treffer aus.</summary>
    public class ThrownBall : MonoBehaviour
    {
        public Vector3 from, to; public CanStack target; float t;
        void Update()
        {
            t += Time.deltaTime / .45f;
            var p = Vector3.Lerp(from, to, t); p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * .5f; transform.position = p;
            if (t >= 1f) { if (target != null) target.Hit(target.transform.InverseTransformDirection((to - from).normalized)); Destroy(gameObject); }
        }
    }

    /// <summary>Entchen schwimmen im Kreis durch die Rinne.</summary>
    public class DuckLoop : MonoBehaviour
    {
        public float a = 2f, b = .6f, speed = .25f, phase;
        void Update()
        {
            float t = Time.time * speed + phase; transform.localPosition = new Vector3(Mathf.Cos(t) * a, transform.localPosition.y + Mathf.Sin(Time.time * 3 + phase) * .0005f, Mathf.Sin(t) * b);
            transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(t) * a, 0, Mathf.Cos(t) * b));
        }
    }

    public enum BoothKind { Ticket, Lottery, Crepes, Sausage, CottonCandy, Shooting, Almonds, Beer, Fries, Cans, Ducks, Gingerbread, Popcorn, Slush, Churros, Fish, Arcade }

    public static partial class FairWorld
    {
        static readonly (string name, BoothKind kind)[] BoothList =
        {
            ("LOSE", BoothKind.Lottery), ("CREPES", BoothKind.Crepes), ("BRATWURST", BoothKind.Sausage), ("ZUCKERWATTE", BoothKind.CottonCandy),
            ("SCHIESSEN", BoothKind.Shooting), ("MANDELN", BoothKind.Almonds), ("BIER", BoothKind.Beer), ("POMMES", BoothKind.Fries),
            ("DOSENWERFEN", BoothKind.Cans), ("ENTENANGELN", BoothKind.Ducks), ("LEBKUCHEN", BoothKind.Gingerbread), ("POPCORN", BoothKind.Popcorn),
            ("SLUSH", BoothKind.Slush), ("CHURROS", BoothKind.Churros), ("BACKFISCH", BoothKind.Fish), ("SPIELHALLE", BoothKind.Arcade),
        };

        /// <summary>Bude fuer die Menschen-Verteilung: Lage der Theke, Art, Ziele fuer Werfer und Schuetzen.</summary>
        public class BoothInfo { public Vector3 pos; public float yaw; public BoothKind kind; public Vector3 front, inside; public CanStack[] cans; public TargetRow targets; }
        static readonly List<BoothInfo> booths = new List<BoothInfo>();

        static void Booths()
        {
            int k = 0;
            for (float x = -40; x <= 88; x += 7f) { if (x > -36 && x < -24) continue; var e = BoothList[k % BoothList.Length]; Booth(new Vector3(x, 0, 16), 0, Bright[k % 9], e.name, e.kind); k++; }
            for (float x = -42; x <= 90; x += 7f) { if (x > 30 && x < 54) continue; if (x > -42 && x < -16) continue; var e = BoothList[k % BoothList.Length]; Booth(new Vector3(x, 0, 34), 180, Bright[(k + 3) % 9], e.name, e.kind); k++; }
            for (float x = -30; x <= 84; x += 7f) { var e = BoothList[k % BoothList.Length]; Booth(new Vector3(x, 0, -46), 0, Bright[(k + 1) % 9], e.name, e.kind); k++; }
        }

        static readonly Color[] NeonCols = { new Color(1f, .25f, .55f), new Color(.25f, .85f, 1f), new Color(1f, .85f, .2f), new Color(.4f, 1f, .4f), new Color(1f, .45f, .15f), new Color(.75f, .35f, 1f) };

        static void Booth(Vector3 pos, float yaw, Color a, string name, BoothKind kind)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Vector3 L(float x, float y, float z) => pos + q * new Vector3(x, y, z);
            const float W = 5.2f, D = 3.2f, H = 2.7f;
            var info = new BoothInfo { pos = pos, yaw = yaw, kind = kind, front = L(0, 0, D / 2 + 1.1f), inside = L(0, 0, -.3f) };
            booths.Add(info);

            // Wande aus lackiertem Holz, Rueckwand Wellblech, Theke mit Leuchtkante
            var wood = new MeshBuilder { tile = 2f };
            wood.Box(L(-W / 2, H / 2, 0), q, new Vector3(.1f, H, D));
            wood.Box(L(W / 2, H / 2, 0), q, new Vector3(.1f, H, D));
            wood.Box(L(0, .55f, D / 2 - .2f), q, new Vector3(W, 1.1f, .4f));
            wood.ToObject("Bude " + name, ta != null && ta.paintedWood != null ? Tint(ta.paintedWood, Color.Lerp(a, White, .15f)) : Paint(a), root);
            var back = new MeshBuilder { tile = 1.5f }; back.Box(L(0, H / 2, -D / 2), q, new Vector3(W, H, .1f));
            back.ToObject("Rueckwand", ta != null && ta.corrugated != null ? Tint(ta.corrugated, Color.Lerp(a, Dark, .4f)) : Paint(a), root);
            P(Dark).Box(L(0, 1.12f, D / 2 - .15f), q, new Vector3(W + .1f, .06f, .55f));
            P(Dark).Box(L(0, H + .05f, 0), q, new Vector3(W + .2f, .12f, D + .2f));
            P(new Color(.35f, .25f, .16f)).Box(L(0, .02f, 0), q, new Vector3(W - .2f, .04f, D - .2f));   // Holzboden
            B.Line(L(-W / 2 + .1f, 1.08f, D / 2 + .02f), L(W / 2 - .1f, 1.08f, D / 2 + .02f), .3f, .045f, Warm, BulbMode.Chase, .3f);
            // Markise: Planenstoff gestreift, mit Bogenkante
            var awnA = new MeshBuilder(); var awnB = new MeshBuilder();
            for (int i = 0; i < 10; i++)
            {
                float x = -W / 2 + (i + .5f) * W / 10;
                var mb = i % 2 == 0 ? awnA : awnB;
                mb.Box(L(x, H - .2f, D / 2 + .55f), q * Quaternion.Euler(22, 0, 0), new Vector3(W / 10 + .005f, .04f, 1.3f));
                // Bogen (Halbkreis) als Volant
                Vector3 c0 = L(x, H - .55f, D / 2 + 1.16f);
                for (int j = 0; j < 5; j++)
                {
                    float a0 = j / 5f * Mathf.PI, a1 = (j + 1) / 5f * Mathf.PI, r = W / 20;
                    Vector3 p0 = c0 + q * new Vector3(Mathf.Cos(a0) * r, -Mathf.Sin(a0) * r, 0), p1 = c0 + q * new Vector3(Mathf.Cos(a1) * r, -Mathf.Sin(a1) * r, 0);
                    mb.Tri(c0, p0, p1, q * Vector3.forward); mb.Tri(c0, p0, p1, q * Vector3.back);
                }
                mb.Box(L(x, H - .44f, D / 2 + 1.16f), q, new Vector3(W / 10 + .005f, .22f, .02f));
            }
            Material awnMat(Color c) => ta != null && ta.canvas != null ? Tint(ta.canvas, c) : Paint(c);
            awnA.ToObject("Markise", awnMat(a), root); awnB.ToObject("Markise", awnMat(White), root);
            for (int i = 0; i <= 12; i++) B.Add(L(-W / 2 + i * W / 12, H - .36f, D / 2 + 1.18f), .05f, Warm, BulbMode.Alternate, 0, 1, .35f, i);
            // Schild mit Lauflicht-Rahmen und Neonschrift
            P(Dark).Box(L(0, H + .7f, D / 2 - .1f), q, new Vector3(W - .4f, 1.0f, .1f));
            float sw = W / 2 - .3f;
            B.Line(L(-sw, H + 1.22f, D / 2 - .03f), L(sw, H + 1.22f, D / 2 - .03f), .25f, .055f, BWhite, BulbMode.Chase, .35f);
            B.Line(L(-sw, H + .18f, D / 2 - .03f), L(sw, H + .18f, D / 2 - .03f), .25f, .055f, BWhite, BulbMode.Chase, .35f);
            var nc = NeonCols[Mathf.Abs(name.GetHashCode()) % NeonCols.Length];
            Neon(name, L(0, H + .7f, D / 2 - .02f), yaw + 180f, name.Length > 9 ? .085f : .11f, nc, Random.value < .3f ? 1 : 0);
            // Innenlicht: Leuchtleiste unter dem Dach und warmes Punktlicht
            B.Line(L(-W / 2 + .4f, H - .12f, 0), L(W / 2 - .4f, H - .12f, 0), .3f, .05f, BWhite, BulbMode.Steady, .3f);
            PL(L(0, H - .5f, .6f), new Color(1f, .82f, .6f), 7f, 2.2f);

            // Regale an der Rueckwand
            var shelf = P(new Color(.55f, .4f, .25f));
            if (kind != BoothKind.Shooting && kind != BoothKind.Arcade && kind != BoothKind.Ducks)
            {
                shelf.Box(L(0, 1.6f, -D / 2 + .35f), q, new Vector3(W - .3f, .05f, .5f));
                shelf.Box(L(0, 2.2f, -D / 2 + .35f), q, new Vector3(W - .3f, .05f, .5f));
            }
            BoothGoods(kind, info, q, L, W, D, H, a);
        }

        delegate Vector3 LocalFn(float x, float y, float z);

        static void BoothGoods(BoothKind kind, BoothInfo info, Quaternion q, System.Func<float, float, float, Vector3> L, float W, float D, float H, Color a)
        {
            switch (kind)
            {
                case BoothKind.Ticket:
                    PL(L(0, 1.6f, 0), new Color(1f, .9f, .6f), 5f, 1.5f);
                    break;
                case BoothKind.Lottery:
                case BoothKind.Gingerbread:
                case BoothKind.Shooting:
                    break;
            }
            switch (kind)
            {
                case BoothKind.Lottery:
                {
                    // Gluecksrad mit Lampen und grosse Pluesch-Preise
                    var wheel = new GameObject("Gluecksrad").transform; wheel.SetParent(root, false);
                    wheel.position = L(0, 1.85f, -D / 2 + .12f); wheel.rotation = q;
                    var wm = new MeshBuilder(); var wl = new BulbSet();
                    for (int i = 0; i < 16; i++)
                    {
                        float a0 = i / 16f * Mathf.PI * 2, a1 = (i + 1) / 16f * Mathf.PI * 2;
                        var seg = new MeshBuilder();
                        seg.Tri(Vector3.zero, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * .75f, new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * .75f, Vector3.forward);
                        seg.ToObject("Feld", Paint(Bright[i % 8]), wheel, false);
                        wl.Add(new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), .03f) * .8f, .035f, BYellow, BulbMode.Chase, i / 16f, 16, .2f, i);
                    }
                    wm.Cylinder(new Vector3(0, 0, -.02f), .08f, .06f, 10);
                    wm.ToObject("Achse", Lit(Gold, .8f, .9f), wheel);
                    wl.Build("Radlichter", wheel);
                    wheel.gameObject.AddComponent<FortuneWheel>();
                    for (int i = 0; i < 5; i++) Plush(L(-2f + i * 1f, 2.25f, -D / 2 + .4f), q, Bright[(i + 2) % 8], 1.2f);
                    break;
                }
                case BoothKind.Shooting:
                {
                    // Gewehre auf der Theke, bewegte Zielscheiben, Rosen und Sterne
                    for (int i = 0; i < 4; i++) Rifle(L(-1.8f + i * 1.2f, 1.17f, D / 2 - .2f), q * Quaternion.Euler(0, 180 + Random.Range(-15f, 15f), 90));
                    var row = new GameObject("Ziele").transform; row.SetParent(root, false); row.position = L(0, 1.5f, -D / 2 + .25f); row.rotation = q;
                    var tr = row.gameObject.AddComponent<TargetRow>(); tr.width = W - .8f;
                    for (int i = 0; i < 7; i++)
                    {
                        var t = new GameObject("Ziel").transform; t.SetParent(row, false); t.localPosition = Vector3.zero;
                        var mb = new MeshBuilder(); mb.Disc(new Vector3(0, .18f, .02f), .16f, 14, Vector3.forward); mb.ToObject("Scheibe", Paint(White), t, false);
                        var mr = new MeshBuilder(); mr.Disc(new Vector3(0, .18f, .025f), .1f, 14, Vector3.forward); mr.ToObject("Ring", Paint(Red), t, false);
                        var md = new MeshBuilder(); md.Disc(new Vector3(0, .18f, .03f), .04f, 10, Vector3.forward); md.ToObject("Mitte", Paint(White), t, false);
                        tr.Add(t);
                    }
                    P(Steel).Box(L(0, 1.45f, -D / 2 + .22f), q, new Vector3(W - .4f, .04f, .06f));
                    for (int i = 0; i < 9; i++) Star(L(-2.2f + i * .55f, 2.35f, -D / 2 + .1f), q, Bright[i % 8]);
                    for (int i = 0; i < 12; i++) P(Red).Sphere(L(-2.3f + i * .42f, 1.95f, -D / 2 + .12f), .07f, 6);
                    for (int i = 0; i < 4; i++) Plush(L(-2.2f + i * 1.45f, 2.55f, -D / 2 + .35f), q, Bright[(i + 4) % 8], .9f);
                    info.targets = tr;
                    break;
                }
                case BoothKind.Cans:
                {
                    var stacks = new List<CanStack>();
                    for (int s = 0; s < 3; s++)
                    {
                        var st = new GameObject("Dosen").transform; st.SetParent(root, false); st.position = L(-1.6f + s * 1.6f, 1.55f, -D / 2 + .5f); st.rotation = q;
                        P(new Color(.4f, .3f, .2f)).Box(L(-1.6f + s * 1.6f, 1.2f, -D / 2 + .5f), q, new Vector3(1.0f, .06f, .5f));
                        var cs = st.gameObject.AddComponent<CanStack>();
                        int row = 0; float y = -.32f;
                        foreach (int n in new[] { 3, 2, 1 })
                        {
                            for (int i = 0; i < n; i++)
                            {
                                var can = new GameObject("Dose").transform; can.SetParent(st, false);
                                can.localPosition = new Vector3((i - (n - 1) / 2f) * .15f, y, 0);
                                var mb = new MeshBuilder(); mb.Cylinder(new Vector3(0, 0, 0), .065f, .15f, 10);
                                mb.ToObject("Blech", Lit(new Color(.75f, .75f, .78f), .7f, .9f), can, false);
                                var lb = new MeshBuilder(); lb.Cylinder(new Vector3(0, .03f, 0), .067f, .09f, 10, false);
                                lb.ToObject("Etikett", Paint(Bright[(i + row) % 8]), can, false);
                                cs.Add(can);
                            }
                            y += .15f; row++;
                        }
                        stacks.Add(cs);
                    }
                    for (int i = 0; i < 6; i++) P(Bright[i % 8]).Sphere(L(-.8f + i * .3f, 1.22f, D / 2 - .25f), .06f, 8);
                    for (int i = 0; i < 4; i++) Plush(L(-2f + i * 1.3f, 2.3f, -D / 2 + .35f), q, Bright[(i + 1) % 8], 1f);
                    info.cans = stacks.ToArray();
                    break;
                }
                case BoothKind.Ducks:
                {
                    // Wasserrinne mit kreisenden Entchen
                    var trough = new MeshBuilder(); trough.Box(L(0, .8f, .3f), q, new Vector3(W - .6f, .6f, 1.8f));
                    trough.ToObject("Rinne", Paint(Blue), root);
                    var water = new MeshBuilder(); water.Box(L(0, 1.11f, .3f), q, new Vector3(W - .8f, .02f, 1.6f));
                    water.ToObject("Wasser", Lit(new Color(.1f, .4f, .7f), .95f), root, false);
                    var pond = new GameObject("Teich").transform; pond.SetParent(root, false); pond.position = L(0, 1.16f, .3f); pond.rotation = q;
                    for (int i = 0; i < 14; i++)
                    {
                        var d = new GameObject("Entchen").transform; d.SetParent(pond, false);
                        var mb = new MeshBuilder(); mb.Sphere(new Vector3(0, .02f, 0), .09f, 8); mb.Sphere(new Vector3(0, .12f, .06f), .055f, 8);
                        mb.ToObject("Ente", Paint(Yellow), d, false);
                        var bk = new MeshBuilder(); bk.Spike(new Vector3(0, .12f, .1f), Vector3.forward, .025f, .05f, 5); bk.ToObject("Schnabel", Paint(Orange), d, false);
                        var dl = d.gameObject.AddComponent<DuckLoop>(); dl.a = W / 2 - .7f; dl.b = .55f; dl.speed = .3f; dl.phase = i / 14f * Mathf.PI * 2;
                    }
                    for (int i = 0; i < 6; i++) Plush(L(-2.2f + i * .88f, 2.2f, -D / 2 + .35f), q, Bright[(i + 3) % 8], .8f);
                    break;
                }
                case BoothKind.Gingerbread:
                {
                    // Lebkuchenherzen an Schnueren
                    for (int r = 0; r < 3; r++)
                        for (int i = 0; i < 7; i++)
                        {
                            Vector3 p = L(-2.1f + i * .7f, 2.35f - r * .45f, D / 2 - .5f - r * .7f);
                            Heart(p, q, .17f + (i % 3) * .03f, Bright[(i + r) % 8]);
                            P(White).Beam(p + up * .15f, p + up * .5f, .008f, q * Vector3.forward);
                        }
                    break;
                }
                case BoothKind.CottonCandy:
                {
                    var mach = new MeshBuilder(); mach.Cylinder(L(0, 1.15f, .5f), .45f, .3f, 18); mach.ToObject("Kessel", Lit(Steel, .8f, .9f), root);
                    var spin = new GameObject("Wattekopf").transform; spin.SetParent(root, false); spin.position = L(0, 1.4f, .5f);
                    var cm = new MeshBuilder(); for (int i = 0; i < 9; i++) cm.Sphere(new Vector3(Mathf.Cos(i) * .25f, Random.Range(0f, .1f), Mathf.Sin(i) * .25f), .14f, 8);
                    cm.ToObject("Watte", Lit(new Color(1f, .7f, .85f), .2f), spin, false); spin.gameObject.AddComponent<Spin>().speed = 300f;
                    for (int i = 0; i < 8; i++) { var p = L(-2.1f + i * .6f, 1.9f, -D / 2 + .4f); P(White).Cylinder(p - up * .3f, .015f, .3f, 5); P(i % 2 == 0 ? Pink : new Color(.6f, .8f, 1f)).Sphere(p + up * .12f, .2f, 10); }
                    for (int i = 0; i < 10; i++) P(Bright[i % 8]).Box(L(-2f + i * .45f, 2.4f, -D / 2 + .35f), q, new Vector3(.3f, .25f, .25f));
                    break;
                }
                case BoothKind.Sausage:
                case BoothKind.Fish:
                {
                    // Schwenkgrill mit Glut, Wuerste/Fisch, Rauch
                    var grill = new MeshBuilder(); grill.Cylinder(L(0, 1.15f, .3f), .7f, .08f, 18);
                    for (int i = -3; i <= 3; i++) grill.Box(L(i * .2f, 1.25f, .3f), q, new Vector3(.02f, .02f, 1.3f));
                    grill.ToObject("Rost", Lit(Dark, .5f, .8f), root);
                    for (int i = 0; i < 26; i++) B.Add(L(Random.Range(-.55f, .55f), 1.17f, .3f + Random.Range(-.5f, .5f)), .05f, new Color(1f, .3f, .05f), BulbMode.Twinkle, 0, 1, .25f);
                    var food = P(kind == BoothKind.Sausage ? new Color(.55f, .25f, .12f) : new Color(.85f, .6f, .3f));
                    for (int i = 0; i < 8; i++) food.Loft(new[] { L(-.6f + i * .17f, 1.3f, -.1f), L(-.6f + i * .17f, 1.3f, .7f) }, new[] { new Vector2(.035f, .035f), new Vector2(.035f, .035f) }, 1, 8, true, true, up);
                    if (ta != null && ta.smoke != null) Scenery.SmokeAt(root, L(0, 1.4f, .3f), ta.smoke);
                    for (int i = 0; i < 3; i++) { P(Red).Cylinder(L(1.6f + i * .15f, 1.15f, D / 2 - .2f), .04f, .2f, 8); P(Yellow).Cylinder(L(1.65f + i * .15f, 1.15f, D / 2 - .35f), .04f, .2f, 8); }
                    break;
                }
                case BoothKind.Beer:
                {
                    for (int i = 0; i < 3; i++) { P(Gold).Cylinder(L(-.8f + i * .8f, 1.15f, D / 2 - .3f), .05f, .45f, 8); P(Steel).Box(L(-.8f + i * .8f, 1.55f, D / 2 - .22f), q, new Vector3(.04f, .04f, .2f)); }
                    for (int i = 0; i < 10; i++) { var p = L(-2.2f + i * .48f, 1.65f, -D / 2 + .35f); P(new Color(1f, .75f, .2f)).Cylinder(p, .06f, .2f, 8); P(White).Cylinder(p + up * .2f, .062f, .04f, 8); }
                    if (ta != null && ta.props != null && ta.props.Length > 0) Scenery.PlaceProp(ta.props[0], root, L(-1.8f, 0, -.6f), info.yaw);
                    for (int i = 0; i < 2; i++) { P(Dark).Cylinder(L(-1.5f + i * 3f, 0, D / 2 + 2.6f), .04f, 1.05f, 6); P(new Color(.5f, .35f, .2f)).Cylinder(L(-1.5f + i * 3f, 1.05f, D / 2 + 2.6f), .4f, .05f, 14); }
                    break;
                }
                case BoothKind.Popcorn:
                {
                    var glass = new MeshBuilder(); glass.Box(L(0, 1.55f, .4f), q, new Vector3(.8f, .8f, .6f));
                    var gm = Lit(new Color(.8f, .9f, 1f, .25f), .97f); glass.ToObject("Glas", gm, root, false);
                    var pc = P(new Color(1f, .93f, .6f)); for (int i = 0; i < 70; i++) pc.Sphere(L(Random.Range(-.35f, .35f), 1.2f + Random.Range(0f, .3f), .4f + Random.Range(-.25f, .25f)), .04f, 5);
                    B.Add(L(0, 1.92f, .4f), .08f, BYellow, BulbMode.Steady, 0, 1, .8f);
                    P(Red).Box(L(0, 1.1f, .4f), q, new Vector3(.9f, .1f, .7f));
                    for (int i = 0; i < 10; i++) { var p = L(-2.2f + i * .48f, 1.65f, -D / 2 + .35f); P(i % 2 == 0 ? Red : White).Cylinder(p, .1f, .28f, 8); pc.Sphere(p + up * .3f, .09f, 6); }
                    break;
                }
                case BoothKind.Slush:
                {
                    Color[] sc = { new Color(1f, .15f, .2f), new Color(.15f, .5f, 1f), new Color(.4f, 1f, .2f) };
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = L(-1.2f + i * 1.2f, 1.15f, .3f);
                        P(Steel).Box(p + up * .1f, q, new Vector3(.5f, .2f, .5f));
                        B.Add(p + up * .5f, .2f, sc[i], BulbMode.Breathe, 0, 1, .9f, i);   // leuchtender Tank
                        var sp = new GameObject("Ruehrer").transform; sp.SetParent(root, false); sp.position = p + up * .45f;
                        var sm = new MeshBuilder(); sm.Box(Vector3.zero, Quaternion.identity, new Vector3(.3f, .04f, .04f)); sm.ToObject("Fluegel", Lit(White, .5f), sp, false);
                        sp.gameObject.AddComponent<Spin>().speed = 160f;
                    }
                    break;
                }
                case BoothKind.Arcade:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = L(-1.6f + i * 1.6f, 0, -D / 2 + .55f);
                        P(Bright[(i + 4) % 8]).Box(p + up * .9f, q, new Vector3(.8f, 1.8f, .7f));
                        var scr = new BulbSet();
                        for (int y = 0; y < 4; y++) for (int x = 0; x < 5; x++) scr.Add(p + q * new Vector3(-.24f + x * .12f, 1.25f + y * .1f, .36f), .045f, Bulbs[(x + y + i) % Bulbs.Length], BulbMode.Twinkle, 0, 1, .15f);
                        scr.Build("Bildschirm", root);
                    }
                    break;
                }
                case BoothKind.Crepes:
                case BoothKind.Churros:
                case BoothKind.Fries:
                case BoothKind.Almonds:
                {
                    // Kochplatte bzw. Fritteuse/Kupferkessel, Speisekarte
                    if (kind == BoothKind.Almonds) { P(new Color(.8f, .45f, .25f)).Cylinder(L(0, 1.15f, .3f), .5f, .35f, 18); for (int i = 0; i < 10; i++) B.Add(L(Random.Range(-.3f, .3f), 1.2f, .3f + Random.Range(-.3f, .3f)), .04f, new Color(1f, .4f, .1f), BulbMode.Twinkle, 0, 1, .2f); }
                    else { P(Steel).Box(L(-.8f, 1.2f, .3f), q, new Vector3(1f, .15f, .7f)); P(Dark).Box(L(.8f, 1.25f, .3f), q, new Vector3(.9f, .25f, .6f)); }
                    if (ta != null && ta.smoke != null && kind != BoothKind.Crepes) Scenery.SmokeAt(root, L(.8f, 1.5f, .3f), ta.smoke);
                    var menu = P(Dark); menu.Box(L(0, 2.0f, -D / 2 + .1f), q, new Vector3(2.4f, 1.0f, .04f));
                    for (int i = 0; i < 5; i++) P(White).Box(L(-.6f, 2.3f - i * .15f, -D / 2 + .13f), q, new Vector3(1f, .04f, .01f));
                    for (int i = 0; i < 5; i++) P(Yellow).Box(L(.85f, 2.3f - i * .15f, -D / 2 + .13f), q, new Vector3(.3f, .04f, .01f));
                    Color food = kind == BoothKind.Crepes ? new Color(.95f, .8f, .5f) : kind == BoothKind.Fries ? new Color(1f, .85f, .3f) : kind == BoothKind.Churros ? new Color(.85f, .55f, .25f) : new Color(.6f, .35f, .15f);
                    for (int i = 0; i < 8; i++) P(food).Sphere(L(-1.9f + i * .2f, 1.2f, D / 2 - .3f), .06f, 6);
                    break;
                }
            }
        }

        static void Plush(Vector3 p, Quaternion q, Color c, float s)
        {
            var mb = P(c);
            mb.Sphere(p + up * .12f * s, .17f * s, 10);
            mb.Sphere(p + up * .36f * s, .13f * s, 10);
            mb.Sphere(p + up * .48f * s + q * new Vector3(-.09f, 0, 0) * s, .05f * s, 6); mb.Sphere(p + up * .48f * s + q * new Vector3(.09f, 0, 0) * s, .05f * s, 6);
            P(Dark).Sphere(p + up * .38f * s + q * new Vector3(-.045f, 0, .11f) * s, .018f * s, 5); P(Dark).Sphere(p + up * .38f * s + q * new Vector3(.045f, 0, .11f) * s, .018f * s, 5);
        }

        static void Star(Vector3 p, Quaternion q, Color c)
        {
            var mb = P(c); Vector3 n = q * Vector3.forward;
            for (int i = 0; i < 5; i++)
            {
                float a0 = i / 5f * Mathf.PI * 2 + Mathf.PI / 2, a1 = a0 + Mathf.PI / 5, a2 = a0 - Mathf.PI / 5;
                Vector3 tip = p + q * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * .17f;
                Vector3 l = p + q * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * .07f, r = p + q * new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0) * .07f;
                mb.Tri(p, l, tip, n); mb.Tri(p, tip, r, n); mb.Tri(p, l, tip, -n); mb.Tri(p, tip, r, -n);
            }
        }

        static void Heart(Vector3 p, Quaternion q, float s, Color icing)
        {
            var mb = P(new Color(.45f, .24f, .12f)); var ic = P(icing); Vector3 n = q * Vector3.forward;
            Vector3 H(float t, float k) { float x = 16 * Mathf.Pow(Mathf.Sin(t), 3), y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t); return p + q * new Vector3(x, y, 0) * (s / 16f * k); }
            const int N = 24;
            for (int i = 0; i < N; i++)
            {
                float t0 = i / (float)N * Mathf.PI * 2, t1 = (i + 1) / (float)N * Mathf.PI * 2;
                mb.Tri(p, H(t0, 1), H(t1, 1), n); mb.Tri(p, H(t0, 1), H(t1, 1), -n);
                ic.Beam(H(t0, .82f) + n * .012f, H(t1, .82f) + n * .012f, .012f, n);
            }
        }

        static void Rifle(Vector3 p, Quaternion q)
        {
            P(new Color(.35f, .2f, .1f)).Box(p, q, new Vector3(.06f, .5f, .08f));
            P(Dark).Loft(new[] { p + q * new Vector3(0, .25f, 0), p + q * new Vector3(0, .75f, 0) }, new[] { new Vector2(.015f, .015f), new Vector2(.015f, .015f) }, 1, 6, true, true, q * Vector3.forward);
        }
    }
}
