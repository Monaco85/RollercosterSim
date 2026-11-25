using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>Alle Teile eines Boxautos, nach Material sortiert, dazu die Anbaupunkte fuer Lampen, Lenkrad und Stromabnehmer.</summary>
    public class BoxautoTeile
    {
        public readonly Dictionary<string, GlattMesh> aufbau = new Dictionary<string, GlattMesh>();
        public readonly Dictionary<string, GlattMesh> lenkrad = new Dictionary<string, GlattMesh>();
        public GlattMesh birne = new GlattMesh();
        public readonly List<Pose> birnen = new List<Pose>();
        public readonly List<Pose> scheinwerfer = new List<Pose>();
        public readonly List<Pose> ruecklichter = new List<Pose>();
        public readonly List<Vector3> unterlicht = new List<Vector3>();
        public Pose lenkradPose;
        public Vector3 stangeFuss, stangeOben;
        public GlattMesh A(string k) { if (!aufbau.TryGetValue(k, out var m)) aufbau[k] = m = new GlattMesh(); return m; }
        public GlattMesh L(string k) { if (!lenkrad.TryGetValue(k, out var m)) lenkrad[k] = m = new GlattMesh(); return m; }
    }

    /// <summary>
    /// Prozedurales Boxauto im Stil klassischer Kirmes-Autoscooter: Wanne aus GFK mit Klarlack, dicker Gummi-Rammring,
    /// Rollpolster-Sitzbank, Lenkrad, Scheinwerfer, Lauflicht-Birnen, Rueckleuchten, Stromabnehmer-Stange mit Feder und Zielflagge.
    /// Masse: 1,5 m breit, 2,3 m lang (mit Gummi), Wanne 0,3-1,04 m hoch. +z = vorn.
    /// </summary>
    public static class BoxautoModell
    {
        public const int NU = 160;          // Spalten rundherum
        public const int S = 4;             // Unterteilung je Profil-Stuetzpunkt
        const float AO = .64f, BO = 1.05f, EO = 3.2f;     // Aussenkontur (Superellipse)
        const float AI = .42f, BI = .50f, EI = 3.2f;      // Cockpit-Oeffnung
        static readonly Vector2 C = new Vector2(0, -.10f); // Mitte der Oeffnung (x, z)

        static float SE(float x, float z, float a, float b, float e) => Mathf.Pow(Mathf.Pow(Mathf.Abs(x / a), e) + Mathf.Pow(Mathf.Abs(z / b), e), 1f / e);
        static float Fs(float w, float e) => Mathf.Sign(w) * Mathf.Pow(Mathf.Abs(w), e);

        /// <summary>Punkt auf einer Superellipse; Winkel von vorn (+z) Richtung rechts (+x).</summary>
        public static Vector3 Plan(float th, float a, float b, float e, float y)
            => new Vector3(a * Fs(Mathf.Sin(th), 2f / e), y, b * Fs(Mathf.Cos(th), 2f / e));

        static Vector3 CR(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3);
        }

        public static Vector3[,] Wanne;     // das Karosseriegitter (fuer Abziehbilder und Anbauteile)
        public static int Zeilen => (14 - 1) * S + 1;

        static Vector3 GN(int i, int j)
        {
            int nu = Wanne.GetLength(0), nv = Wanne.GetLength(1);
            Vector3 du = Wanne[(i + 1) % nu, j] - Wanne[(i - 1 + nu) % nu, j];
            Vector3 dv = Wanne[i, Mathf.Min(nv - 1, j + 1)] - Wanne[i, Mathf.Max(0, j - 1)];
            return Vector3.Cross(du, dv).normalized;   // bei unserer Laufrichtung zeigt das nach aussen
        }

        public static BoxautoTeile Baue(float stangenHoehe = 3.55f)
        {
            var T = new BoxautoTeile();
            int NV = Zeilen;
            var P = new Vector3[NU, NV];
            var ctrl = new Vector3[14];
            var dirs = new Vector3[NU];
            for (int i = 0; i < NU; i++)
            {
                float th = i / (float)NU * Mathf.PI * 2;
                Vector2 d = new Vector2(Mathf.Sin(th), Mathf.Cos(th));
                dirs[i] = new Vector3(d.x, 0, d.y);
                float rIn = 1f / SE(d.x, d.y, AI, BI, EI);
                float lo = rIn, hi = 3f;
                for (int k = 0; k < 40; k++) { float m = (lo + hi) * .5f; if (SE(C.x + d.x * m, C.y + d.y * m, AO, BO, EO) < 1f) lo = m; else hi = m; }
                float L = lo - rIn;
                float f = Mathf.Pow(Mathf.Clamp01(d.y), 2), b = Mathf.Pow(Mathf.Clamp01(-d.y), 2);
                float yb = .30f, ys = .60f + .04f * f + .14f * b, yl = .72f + .20f * f + .32f * b, yf = .22f;
                Vector3 Pt(float frac, float abs, float y) { var q = C + d * (rIn + frac * L + abs); return new Vector3(q.x, y, q.y); }
                ctrl[0] = Pt(.975f, 0, yb);
                ctrl[1] = Pt(1f, .012f, yb + .07f);
                ctrl[2] = Pt(1f, .008f, yb + .15f);
                ctrl[3] = Pt(.99f, 0, ys - .06f);
                ctrl[4] = Pt(.955f, 0, ys);
                ctrl[5] = Pt(.86f, 0, ys + .035f);
                ctrl[6] = Pt(.6f, 0, Mathf.Lerp(ys, yl, .5f) + .04f);
                ctrl[7] = Pt(.25f, 0, Mathf.Lerp(ys, yl, .88f) + .02f);
                ctrl[8] = Pt(0, .035f, yl);
                ctrl[9] = Pt(0, 0, yl - .012f);
                ctrl[10] = Pt(0, -.012f, yl - .05f);
                ctrl[11] = Pt(0, -.004f, yl - .12f);
                ctrl[12] = Pt(0, .01f, yf + .15f);
                ctrl[13] = Pt(0, .03f, yf);
                for (int k = 0; k < 13; k++)
                    for (int s = 0; s < S; s++)
                        P[i, k * S + s] = CR(ctrl[Mathf.Max(0, k - 1)], ctrl[k], ctrl[k + 1], ctrl[Mathf.Min(13, k + 2)], s / (float)S);
                P[i, NV - 1] = ctrl[13];
            }
            Wanne = P;

            // ---- Wanne: unten Zierband, oben Lack, innen dunkel ----
            T.A("Akzent").Grid(P, true, false, NU / 4, S, Vector3.right, .5f, 0, 2 * S);
            T.A("Lack").Grid(P, true, false, NU / 4, 6 * S, new Vector3(.5f, 1, 0), .5f, 2 * S, 9 * S);
            T.A("Innen").Grid(P, true, false, NU / 4, 11 * S, Vector3.left, .5f, 9 * S, NV - 1);
            var boden = new Vector3[NU]; for (int i = 0; i < NU; i++) boden[i] = P[i, NV - 1];
            T.A("Matte").Fan(new Vector3(C.x, .22f, C.y), boden, Vector3.up, .5f);
            // Riffeln der Bodenmatte
            for (int r = -5; r <= 5; r++)
                T.A("Matte").Blob(new Vector3(r * .065f, .224f, .05f), Quaternion.identity, new Vector3(.012f, .006f, .3f), .6f, .4f, 10, 6);

            // Chromleiste zwischen Zierband und Lack
            var leiste = new Vector3[NU];
            for (int i = 0; i < NU; i++) leiste[i] = P[i, 2 * S] + dirs[i] * .004f;
            T.A("Chrom").Tube(leiste, true, GlattMesh.Kreis(.009f, .014f, 12), Vector3.up);
            // Schwarzes Polster rund um die Cockpit-Kante
            var wulst = new Vector3[NU];
            for (int i = 0; i < NU; i++) wulst[i] = P[i, 9 * S] - dirs[i] * .012f + Vector3.up * .006f;
            T.A("Wulst").Tube(wulst, true, GlattMesh.Kreis(.03f, .034f, 16), Vector3.up);

            // ---- Fahrgestell, Gummi-Rammring, Raeder, Unterbodenlicht ----
            var bot = new Vector3[NU]; var top = new Vector3[NU]; var Gs = new Vector3[NU, 2];
            for (int i = 0; i < NU; i++)
            {
                float th = i / (float)NU * Mathf.PI * 2;
                Gs[i, 0] = bot[i] = Plan(th, .70f, 1.10f, 3f, .07f); Gs[i, 1] = top[i] = Plan(th, .70f, 1.10f, 3f, .17f);
            }
            T.A("Stahl").Grid(Gs, true, false, NU / 4, 0, Vector3.right);
            T.A("Stahl").Fan(new Vector3(0, .17f, 0), top, Vector3.up);
            T.A("Stahl").Fan(new Vector3(0, .07f, 0), bot, Vector3.down);

            var ring = new Vector3[NU];
            for (int i = 0; i < NU; i++) ring[i] = Plan(i / (float)NU * Mathf.PI * 2, .745f, 1.15f, 3f, .215f);
            var prof = new Vector2[44];
            for (int q = 0; q < prof.Length; q++)
            {
                float a = q / (float)prof.Length * Mathf.PI * 2;
                var p = new Vector2(.10f * Fs(Mathf.Cos(a), 2f / 2.6f), .11f * Fs(Mathf.Sin(a), 2f / 2.6f));
                if (p.x > 0)
                {
                    float G(float z) => Mathf.Exp(-(z / .008f) * (z / .008f));
                    p.x -= .007f * (G(p.y - .048f) + G(p.y + .048f));
                    p.x += .003f * G(p.y);
                }
                prof[q] = p;
            }
            T.A("Gummi").Tube(ring, true, prof, Vector3.up, true, .25f);

            foreach (var w in new[] { new Vector3(-.45f, .062f, .65f), new Vector3(.45f, .062f, .65f), new Vector3(-.45f, .062f, -.65f), new Vector3(.45f, .062f, -.65f) })
            {
                var rp = new List<Vector2>(); for (int q = 0; q <= 8; q++) { float a = -Mathf.PI / 2 + q / 8f * Mathf.PI; rp.Add(new Vector2(.05f + .012f * Mathf.Cos(a), .025f * Mathf.Sin(a))); }
                rp.Insert(0, new Vector2(0, -.025f)); rp.Add(new Vector2(0, .025f));
                T.A("Gummi").Lathe(w, Vector3.right, rp, 18);
            }
            var glow = new Vector3[NU];
            for (int i = 0; i < NU; i++) glow[i] = Plan(i / (float)NU * Mathf.PI * 2, .66f, 1.06f, 3f, .062f);
            T.A("Unterboden").Tube(glow, true, GlattMesh.Kreis(.012f, .012f, 8), Vector3.up);
            T.unterlicht.Add(new Vector3(0, .04f, .55f)); T.unterlicht.Add(new Vector3(0, .04f, -.55f));

            // ---- Sitzbank mit Rollpolstern ----
            T.A("Innen").Blob(new Vector3(0, .27f, -.33f), Quaternion.identity, new Vector3(.40f, .06f, .18f), .25f, .25f);
            for (int k = 0; k < 5; k++)
                T.A("Polster").Blob(new Vector3(-.26f + .13f * k, .355f, -.32f), Quaternion.identity, new Vector3(.068f, .062f, .17f), .45f, .45f, 24, 14);
            Vector3 piv = new Vector3(0, .40f, -.42f); Quaternion qb = Quaternion.Euler(-10, 0, 0);
            for (int k = 0; k < 5; k++)
                T.A("Polster").Blob(piv + qb * new Vector3(-.24f + .12f * k, .25f, 0), qb, new Vector3(.064f, .22f, .052f), .45f, .45f, 24, 14);
            T.A("Polster").Blob(piv + qb * new Vector3(0, .49f, -.01f), qb, new Vector3(.29f, .04f, .06f), .5f, .4f, 28, 12);

            // ---- Lenkrad (eigenes Objekt, dreht sich) ----
            Vector3 W = new Vector3(0, .86f, .20f), nW = new Vector3(0, .8f, -.6f).normalized;
            T.lenkradPose = new Pose(W, Quaternion.LookRotation(nW, Vector3.forward));
            var rim = new Vector3[48]; for (int k = 0; k < 48; k++) { float a = k / 48f * Mathf.PI * 2; rim[k] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .165f; }
            T.L("Griff").Tube(rim, true, GlattMesh.Kreis(.017f, .021f, 14), Vector3.forward);
            foreach (float deg in new[] { -90f, 10f, 170f })
            {
                float a = deg * Mathf.Deg2Rad; Vector3 dd = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                T.L("Chrom").Tube(new[] { dd * .04f, dd * .1f, dd * .155f }, false, GlattMesh.Kreis(.013f, .006f, 10), Vector3.forward);
            }
            T.L("Chrom").Lathe(Vector3.zero, Vector3.forward, new[] { new Vector2(0, -.035f), new Vector2(.042f, -.035f), new Vector2(.05f, -.01f), new Vector2(.048f, .018f), new Vector2(.038f, .028f), new Vector2(0, .03f) }, 28);
            T.L("Messing").Blob(new Vector3(0, 0, .03f), Quaternion.Euler(90, 0, 0), new Vector3(.028f, .006f, .028f), .4f, 1f, 24, 8);
            // Lenksaeule und Faltenbalg
            T.A("Stahl").Rod(W - nW * .03f, W - nW * .52f, .021f, 14);
            var balg = new List<Vector2>(); for (int q = 0; q <= 16; q++) { float h = q / 16f * .12f; balg.Add(new Vector2(.035f + .008f * Mathf.Abs(Mathf.Sin(h * 110f)) + (.12f - h) * .12f, h)); }
            T.A("Gummi").Lathe(W - nW * .48f, nW, balg, 20);

            // ---- Fusspedal (Strom) ----
            T.A("Chrom").Blob(new Vector3(0, .27f, .29f), Quaternion.Euler(-28, 0, 0), new Vector3(.05f, .011f, .085f), .3f, .3f, 20, 10);
            for (int r = -2; r <= 2; r++) T.A("Gummi").Blob(new Vector3(0, .27f, .29f) + Quaternion.Euler(-28, 0, 0) * new Vector3(0, .012f, r * .03f), Quaternion.Euler(-28, 0, 0), new Vector3(.045f, .004f, .006f), .5f, .5f, 10, 6);

            // ---- Kontrolllampen am Armaturenbrett ----
            foreach (var (x, key) in new[] { (-.2f, "KontrolleGruen"), (.2f, "KontrolleGelb") })
            {
                var lp = new Vector3(x, .80f, .392f);
                T.A("Chrom").Lathe(lp, Vector3.back, new[] { new Vector2(0, -.01f), new Vector2(.024f, -.01f), new Vector2(.026f, .004f), new Vector2(.02f, .008f), new Vector2(0, .008f) }, 20);
                T.A(key).Sphere(lp + Vector3.back * .008f, .015f, 16, 10);
            }

            // ---- Scheinwerfer vorn ----
            int SpalteBei(float x, int von, int bis, int zeile)
            {
                int best = von; float bd = 9;
                for (int i = von; i <= bis; i++) { float dd = Mathf.Abs(P[(i + NU) % NU, zeile].x - x); if (dd < bd) { bd = dd; best = (i + NU) % NU; } }
                return best;
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                int i = sx > 0 ? SpalteBei(.33f, 0, NU / 4, 4 * S) : SpalteBei(-.33f, 3 * NU / 4, NU - 1, 4 * S);
                Vector3 dir = new Vector3(sx * .1f, -.03f, 1f).normalized;
                Vector3 p = P[i, 4 * S] + Vector3.up * .015f - dir * .01f;
                // lackierte Lampentopf-Schale, vorn ein Chromring
                T.A("Lack").Lathe(p, dir, new[] { new Vector2(0, -.07f), new Vector2(.045f, -.07f), new Vector2(.068f, -.035f), new Vector2(.078f, .01f), new Vector2(.081f, .032f) }, 32);
                T.A("Chrom").Lathe(p, dir, new[] { new Vector2(.079f, .03f), new Vector2(.084f, .038f), new Vector2(.083f, .05f), new Vector2(.076f, .056f), new Vector2(.066f, .05f) }, 32);
                T.A("Scheinwerfer").Lathe(p, dir, new[] { new Vector2(.067f, .044f), new Vector2(.06f, .054f), new Vector2(.035f, .062f), new Vector2(0, .065f) }, 32);
                T.scheinwerfer.Add(new Pose(p + dir * .07f, Quaternion.LookRotation(dir)));
            }

            // ---- Rueckleuchten ----
            foreach (int off in new[] { -14, 14 })
            {
                int i = NU / 2 + off; int j = 3 * S;
                Vector3 nn = GN(i, j); var q = Quaternion.LookRotation(nn, Vector3.up);
                T.A("Chrom").Blob(P[i, j] + nn * .002f, q, new Vector3(.068f, .036f, .012f), .3f, .3f, 24, 10);
                T.A("Ruecklicht").Blob(P[i, j] + nn * .009f, q, new Vector3(.058f, .028f, .012f), .3f, .3f, 24, 10);
                T.ruecklichter.Add(new Pose(P[i, j] + nn * .03f, q));
            }

            // ---- Stromabnehmer-Stange ----
            int jp = 4 * S; { float bd = 9; for (int j = 4 * S; j <= 8 * S; j++) { float dd = Mathf.Abs(P[NU / 2, j].z + .86f); if (dd < bd) { bd = dd; jp = j; } } }
            Vector3 fuss = new Vector3(0, P[NU / 2, jp].y - .005f, P[NU / 2, jp].z);
            Vector3 oben = new Vector3(0, stangenHoehe, fuss.z);
            T.stangeFuss = fuss; T.stangeOben = oben;
            T.A("Chrom").Lathe(fuss, Vector3.up, new[] { new Vector2(0, -.02f), new Vector2(.075f, -.02f), new Vector2(.08f, 0), new Vector2(.074f, .012f), new Vector2(.035f, .018f), new Vector2(0, .018f) }, 28);
            var gm = new List<Vector2> { new Vector2(0, .015f) }; for (int q = 0; q <= 20; q++) { float h = .015f + q / 20f * .08f; gm.Add(new Vector2(.03f + .008f * Mathf.Abs(Mathf.Sin(q / 20f * Mathf.PI * 4)) + (.095f - h) * .15f, h)); }
            gm.Add(new Vector2(0, .095f));
            T.A("Gummi").Lathe(fuss, Vector3.up, gm, 24);
            var feder = new List<Vector3>(); const int wind = 9, stp = 20;
            for (int k = 0; k <= wind * stp; k++) { float a = k / (float)stp * Mathf.PI * 2; feder.Add(fuss + new Vector3(Mathf.Cos(a) * .036f, .095f + k / (float)(wind * stp) * .27f, Mathf.Sin(a) * .036f)); }
            T.A("Chrom").Tube(feder, false, GlattMesh.Kreis(.0058f, .0058f, 8), Vector3.up);
            T.A("Stahl").Rod(fuss + Vector3.up * .09f, oben - Vector3.up * .05f, .016f, 14);
            var iso = new List<Vector2> { new Vector2(0, 0) };
            for (int q = 0; q <= 24; q++) { float h = q / 24f * .16f; iso.Add(new Vector2(.026f + .013f * Mathf.Pow(Mathf.Abs(Mathf.Sin(q / 24f * Mathf.PI * 5)), 3), h)); }
            iso.Add(new Vector2(0, .16f));
            T.A("Innen").Lathe(fuss + Vector3.up * .40f, Vector3.up, iso, 24);
            T.A("Messing").Lathe(oben - Vector3.up * .06f, Vector3.up, new[] { new Vector2(0, 0), new Vector2(.022f, 0), new Vector2(.022f, .04f), new Vector2(.08f, .045f), new Vector2(.086f, .052f), new Vector2(.08f, .06f), new Vector2(0, .06f) }, 32);
            // Zielflagge (beidseitig)
            var fl = new Vector3[15, 7];
            for (int u = 0; u < 15; u++)
                for (int vv = 0; vv < 7; vv++)
                {
                    float uu = u / 14f, vf = vv / 6f;
                    float y = oben.y - .55f + (vf - .5f) * .34f * (1 - uu * .75f);
                    fl[u, vv] = new Vector3(Mathf.Sin(uu * 7f) * .035f * uu, y, fuss.z - .02f - uu * .48f);
                }
            T.A("Fahne").Grid(fl, false, false, 7, 3, Vector3.right, .1f);
            T.A("Fahne").Grid(fl, false, false, 7, 3, Vector3.left, .1f);

            // ---- Gluehbirnen ringsum (Lauflicht) ----
            T.birne.Blob(Vector3.zero, Quaternion.Euler(90, 0, 0), new Vector3(.015f, .019f, .015f), 1f, 1f, 14, 10);
            for (int i = 0; i < NU; i += 4)
            {
                int j = 5 * S;
                Vector3 p = P[i, j], nn = GN(i, j);
                bool frei = true;
                foreach (var s in T.scheinwerfer) if ((s.position - p).magnitude < .14f) frei = false;
                if ((new Vector3(fuss.x, p.y, fuss.z) - p).magnitude < .1f) frei = false;
                if (!frei) continue;
                T.A("Chrom").Lathe(p - nn * .004f, nn, new[] { new Vector2(0, 0), new Vector2(.017f, 0), new Vector2(.019f, .006f), new Vector2(.013f, .012f), new Vector2(0, .012f) }, 14);
                T.birnen.Add(new Pose(p + nn * .026f, Quaternion.LookRotation(nn)));
            }

            // ---- Startnummer-Abziehbilder (Haube, Heck, beide Seiten) ----
            Decal(T.A("Nummer"), 0, 6 * S + 1, .30f, Vector3.back);
            Decal(T.A("Nummer"), NU / 2, 3 * S, .24f, Vector3.up);
            Decal(T.A("Nummer"), NU / 4, 3 * S + 1, .16f, Vector3.up);
            Decal(T.A("Nummer"), 3 * NU / 4, 3 * S + 1, .16f, Vector3.up);
            return T;
        }

        /// <summary>Abziehbild: Ausschnitt des Karosseriegitters, leicht angehoben, UV im lokalen Rahmen (quadratisch, Kantenlaenge size).</summary>
        static void Decal(GlattMesh m, int ic, int jc, float size, Vector3 upWelt)
        {
            int nu = Wanne.GetLength(0);
            Vector3 c = Wanne[ic, jc], N = GN(ic, jc);
            Vector3 up = Vector3.ProjectOnPlane(upWelt, N).normalized, right = Vector3.Cross(N, up).normalized;
            const int K = 24;
            int j0 = Mathf.Max(2 * S + 1, jc - K), j1 = Mathf.Min(9 * S - 1, jc + K);
            var idx = new Dictionary<(int, int), int>();
            int Get(int i, int j)
            {
                i = (i + nu) % nu;
                if (idx.TryGetValue((i, j), out int r)) return r;
                Vector3 p = Wanne[i, j], nn = GN(i, j), d = p - c;
                r = m.AddVert(p + nn * .0025f, nn, new Vector2(Vector3.Dot(d, right) / size + .5f, Vector3.Dot(d, up) / size + .5f));
                idx[(i, j)] = r; return r;
            }
            bool In(int i, int j) { var d = Wanne[(i + nu) % nu, j] - c; return Mathf.Abs(Vector3.Dot(d, right)) < size * .62f && Mathf.Abs(Vector3.Dot(d, up)) < size * .62f; }
            for (int i = ic - K; i < ic + K; i++)
                for (int j = j0; j < j1; j++)
                {
                    if (!(In(i, j) && In(i + 1, j) && In(i + 1, j + 1) && In(i, j + 1))) continue;
                    int a = Get(i, j), b = Get(i + 1, j), cc = Get(i + 1, j + 1), d = Get(i, j + 1);
                    m.AddTri(a, b, cc); m.AddTri(a, cc, d);   // gleiche Laufrichtung wie die Wanne (Normale nach aussen)
                }
        }
    }
}
