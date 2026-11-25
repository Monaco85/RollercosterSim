using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Die Boxautobahn als Vorfuehrszene: Podest mit Stahlplatten und Riffelblech-Laufsteg, Treppe, Bande mit Gummigurt,
    /// Stuetzen mit Lauflichtern, Dach mit Fahrdraht-Netz und LED-Leisten, Front mit Neon-Schriftzug "AUTOSCOOTER" und Lichtkrone,
    /// Spiegelwand, Moving Heads, Kasse, Laternen, Asphaltplatz unter Nachthimmel, 8 Wagen mit Demo-Fahrt.
    /// </summary>
    public static partial class BoxautoErzeugen
    {
        static float HX = 12f, HZ = 7f, ER = 2.5f;     // Fahrflaeche (Innenkante Bande), je nach Fassung gesetzt
        const float WALK = 1.8f, GROUND = -.5f;         // Laufsteg-Breite, Hoehe des Platzes
        const float NETZ = 3.6f, DACH = 5.3f;

        // Abgerundetes Rechteck: Punkte und Aussen-Normalen, gleiche Indizes fuer jeden Abstand (Offset = p + n * d)
        static (Vector3[] p, Vector3[] n) Rund(float hx, float hz, float r, float schritt = .4f, int bogen = 12)
        {
            var P = new List<Vector3>(); var N = new List<Vector3>();
            Vector2[] cc = { new Vector2(hx - r, hz - r), new Vector2(hx - r, -(hz - r)), new Vector2(-(hx - r), -(hz - r)), new Vector2(-(hx - r), hz - r) };
            float[] a0 = { 90, 0, -90, -180 };
            for (int c = 0; c < 4; c++)
            {
                for (int k = 0; k <= bogen; k++)
                {
                    float a = (a0[c] - 90f * k / bogen) * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    P.Add(new Vector3(cc[c].x + d.x * r, 0, cc[c].y + d.y * r)); N.Add(new Vector3(d.x, 0, d.y));
                }
                // gerade Kante bis zur naechsten Ecke
                Vector3 e0 = P[P.Count - 1], nn = N[N.Count - 1];
                var nc = cc[(c + 1) % 4]; float a1 = a0[(c + 1) % 4] * Mathf.Deg2Rad;
                Vector3 e1 = new Vector3(nc.x + Mathf.Cos(a1) * r, 0, nc.y + Mathf.Sin(a1) * r);
                int m = Mathf.Max(1, Mathf.RoundToInt((e1 - e0).magnitude / schritt));
                for (int k = 1; k < m; k++) { P.Add(Vector3.Lerp(e0, e1, k / (float)m)); N.Add(nn); }
            }
            return (P.ToArray(), N.ToArray());
        }
        static Vector3[] Off((Vector3[] p, Vector3[] n) r, float d, float y)
        {
            var o = new Vector3[r.p.Length];
            for (int i = 0; i < o.Length; i++) o[i] = r.p[i] + r.n[i] * d + Vector3.up * y;
            return o;
        }
        static Vector3[,] Band(Vector3[] a, Vector3[] b)
        {
            var g = new Vector3[a.Length, 2];
            for (int i = 0; i < a.Length; i++) { g[i, 0] = a[i]; g[i, 1] = b[i]; }
            return g;
        }

        // Strich-Buchstaben im Kasten 0,6 x 1
        static Vector2[][] Buchstabe(char ch)
        {
            Vector2 V(float x, float y) => new Vector2(x, y);
            switch (ch)
            {
                case 'A': return new[] { new[] { V(0, 0), V(.3f, 1), V(.6f, 0) }, new[] { V(.1f, .34f), V(.5f, .34f) } };
                case 'U': return new[] { new[] { V(0, 1), V(0, .2f), V(.15f, 0), V(.45f, 0), V(.6f, .2f), V(.6f, 1) } };
                case 'T': return new[] { new[] { V(0, 1), V(.6f, 1) }, new[] { V(.3f, 1), V(.3f, 0) } };
                case 'O': return new[] { new[] { V(0, .15f), V(0, .85f), V(.15f, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .15f), V(.45f, 0), V(.15f, 0), V(0, .15f) } };
                case 'S': return new[] { new[] { V(.6f, .85f), V(.45f, 1), V(.15f, 1), V(0, .85f), V(0, .65f), V(.15f, .52f), V(.45f, .48f), V(.6f, .35f), V(.6f, .15f), V(.45f, 0), V(.15f, 0), V(0, .15f) } };
                case 'C': return new[] { new[] { V(.6f, .85f), V(.45f, 1), V(.15f, 1), V(0, .85f), V(0, .15f), V(.15f, 0), V(.45f, 0), V(.6f, .15f) } };
                case 'E': return new[] { new[] { V(.6f, 1), V(0, 1), V(0, 0), V(.6f, 0) }, new[] { V(0, .52f), V(.45f, .52f) } };
                case 'R': return new[] { new[] { V(0, 0), V(0, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .65f), V(.45f, .52f), V(0, .52f) }, new[] { V(.3f, .52f), V(.6f, 0) } };
                case 'K': return new[] { new[] { V(0, 0), V(0, 1) }, new[] { V(.6f, 1), V(0, .45f) }, new[] { V(.2f, .62f), V(.6f, 0) } };
                default: return new Vector2[0][];
            }
        }

        /// <summary>Neon-Schriftzug: je Buchstabe ein eigenes Objekt (fuer das Flackern). Zeigt nach -z.</summary>
        static Transform Schrift(string text, Vector3 mitteUnten, float hoehe, Material[] farben, float r, Transform parent, string name)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            float w = .6f * hoehe, gap = .32f * hoehe, total = text.Length * w + (text.Length - 1) * gap;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                var g = new GlattMesh(); float x0 = mitteUnten.x - total / 2 + i * (w + gap);
                foreach (var line in Buchstabe(text[i]))
                    for (int k = 0; k < line.Length; k++)
                    {
                        Vector3 p = new Vector3(x0 + line[k].x * hoehe, mitteUnten.y + line[k].y * hoehe, mitteUnten.z);
                        g.Sphere(p, r, 12, 8);
                        if (k + 1 < line.Length) g.Rod(p, new Vector3(x0 + line[k + 1].x * hoehe, mitteUnten.y + line[k + 1].y * hoehe, mitteUnten.z), r, 10, false);
                    }
                Teil(text[i] + " " + (i + 1), M(g, name + "_" + i), farben[i % farben.Length], root, false);
            }
            return root;
        }

        static Dictionary<string, Material> BahnMaterialien(Dictionary<string, Material> d, Tex t)
        {
            var ta = ThemeAssets.Get();
            var rf = Lit("Bahn_Riffelblech", Color.white, .9f, 1f);
            rf.SetTexture("_BaseMap", t.riffel.c); rf.SetTexture("_BumpMap", t.riffel.n); rf.EnableKeyword("_NORMALMAP"); rf.SetFloat("_BumpScale", 1f);
            rf.SetFloat("_SmoothnessTextureChannel", 1); rf.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"); d["Riffel"] = rf;
            d["Sockel"] = Lack("Bahn_Sockel_Blau", new Color(.03f, .05f, .3f), .5f, .7f, t.flocken);
            d["Fries"] = Lack("Bahn_Fries_Violett", new Color(.12f, .02f, .32f), .55f, .65f, t.flocken);
            d["Stuetze"] = Lack("Bahn_Stuetze", new Color(.06f, .06f, .08f), .7f, .6f, null);
            d["Spiegel"] = Lit("Bahn_Spiegel", new Color(.38f, .38f, .42f), 1f, .985f);
            d["NeonGelb"] = Glow("Bahn_Neon_Gelb", new Color(.2f, .15f, 0), new Color(1f, .65f, .1f) * 5f);
            d["NeonRot"] = Glow("Bahn_Neon_Rot", new Color(.2f, 0, 0), new Color(1f, .08f, .05f) * 5f);
            d["Fenster"] = Glow("Bahn_Kassenfenster", new Color(.1f, .08f, .05f), new Color(1f, .75f, .45f) * 1.4f, .95f);
            d["KasseRot"] = Lack("Bahn_Kasse_Rot", new Color(.6f, .03f, .05f), .4f, .7f, t.flocken);
            d["KasseCreme"] = Lack("Bahn_Kasse_Creme", new Color(.85f, .8f, .68f), .1f, .6f, null);
            d["Box"] = Lit("Bahn_Lautsprecher", new Color(.02f, .02f, .022f), 0, .25f);
            d["Laterne"] = Glow("Bahn_Laterne", new Color(.9f, .8f, .6f), new Color(1f, .78f, .45f) * 4f);
            d["Asphalt"] = ta != null && ta.asphalt != null ? ta.asphalt : Lit("Bahn_Asphalt", new Color(.12f, .12f, .13f), 0, .4f);
            if (ta != null && ta.beam != null)
                for (int i = 0; i < 6; i++) { var b = Mat("Bahn_MovingHead_" + i, ta.beam.shader.name); b.CopyPropertiesFromMaterial(ta.beam); b.SetFloat("_Intensity", .45f); d["Beam" + i] = b; }
            if (ta != null && ta.duskSky != null)
            {
                var sky = Mat("Bahn_Nachthimmel", ta.duskSky.shader.name); sky.CopyPropertiesFromMaterial(ta.duskSky);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", .22f);
                d["Himmel"] = sky;
            }
            foreach (var (m, p) in neueMats) AssetDatabase.CreateAsset(m, p);
            neueMats.Clear();
            foreach (var m in d.Values) EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            var neu = new Dictionary<string, Material>();
            foreach (var kv in d)
            {
                string p = AssetDatabase.GetAssetPath(kv.Value);
                if (p.StartsWith(MatDir)) { AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); neu[kv.Key] = AssetDatabase.LoadAssetAtPath<Material>(p); }
                else neu[kv.Key] = kv.Value;
            }
            return neu;
        }


        /// <summary>Hallenteile, die Vorschau und Spiel gemeinsam haben.</summary>
        class HalleTeile { public Transform licht; public ReflectionProbe sonde; }

        /// <summary>
        /// Baut die Halle (Podest, Bande, Stuetzen, Dach, Netz, Front, Rueckwand, Kasse, Laternen, Licht, Musik) unter "bahn".
        /// Groesse aus HX/HZ/ER. hind = runde Hindernisse in der Fahrflaeche (je x, z, Radius, lokal), null = keine.
        /// </summary>
        static HalleTeile Halle(Transform bahn, Dictionary<string, Material> mats, float[] hind, float musikLaut)
        {
            var up = Vector3.up;
            var rr = Rund(HX, HZ, ER);
            int n = rr.p.Length;

            // ---- Platz und Podest ----
            var mb = new MeshBuilder { tile = 4f };
            mb.Box(new Vector3(0, GROUND - .05f, -10f), Quaternion.identity, new Vector3(140, .1f, 140));
            Teil("Platz", Mm(mb, "Bahn_Platz"), mats["Asphalt"], bahn).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var boden = new GlattMesh(); boden.Fan(Vector3.zero, rr.p, up, 2f);
            Teil("Stahlboden", M(boden, "Bahn_Stahlboden"), mats["Boden"], bahn).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var steg = new GlattMesh(); steg.Grid(Band(Off(rr, .3f, 0), Off(rr, WALK, 0)), true, false, n / 8, 0, up, .3f);
            Teil("Laufsteg", M(steg, "Bahn_Laufsteg"), mats["Riffel"], bahn);
            var unterBande = new GlattMesh(); unterBande.Grid(Band(Off(rr, 0, 0), Off(rr, .3f, 0)), true, false, n / 8, 0, up);
            Teil("Unter der Bande", M(unterBande, "Bahn_UnterBande"), mats["Stahl"], bahn);
            var sockel = new GlattMesh(); sockel.Grid(Band(Off(rr, WALK, 0), Off(rr, WALK, GROUND)), true, false, n / 8, 0, rr.n[n / 8], 1f);
            Teil("Sockel", M(sockel, "Bahn_Sockel"), mats["Sockel"], bahn);
            var kante = new GlattMesh(); kante.Tube(Off(rr, WALK, -.02f), true, GlattMesh.Kreis(.025f, .025f, 8), up);
            Teil("Sockelkante", M(kante, "Bahn_Sockelkante"), mats["Chrom"], bahn);
            // Treppe vorn (3 Stufen, gelbe Stufenkanten)
            var tr = new MeshBuilder { tile = .3f }; var gelb = new MeshBuilder();
            float zf = -(HZ + WALK);
            for (int s = 0; s < 3; s++)
            {
                float top = -(s + 1) * (-GROUND / 3f);
                float zc = zf - .25f - s * .45f, h = top - GROUND;
                if (h < .01f) continue;
                tr.Box(new Vector3(0, GROUND + h / 2, zc), Quaternion.identity, new Vector3(7f, h, .45f));
                gelb.Box(new Vector3(0, top + .004f, zc - .2f), Quaternion.identity, new Vector3(7f, .01f, .05f));
            }
            Teil("Treppe", Mm(tr, "Bahn_Treppe"), mats["Riffel"], bahn);
            Teil("Stufenkanten", Mm(gelb, "Bahn_Stufenkanten"), mats["BandeGelb"], bahn);

            // ---- Bande ----
            var gp = new Vector2[28];
            for (int q = 0; q < 28; q++) { float a = q / 28f * Mathf.PI * 2; gp[q] = new Vector2(.09f * Mathf.Sign(Mathf.Cos(a)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(a)), .5f), .16f * Mathf.Sign(Mathf.Sin(a)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), .5f)); }
            {
                var gg = new GlattMesh(); gg.Tube(Off(rr, .09f, .3f), true, gp, up, true, .5f); gg.Tube(Off(rr, .16f, .07f), true, GlattMesh.Kreis(.05f, .07f, 12), up);
                Teil("Prallgurt", M(gg, "Bahn_Prallgurt"), mats["BandeGummi"], bahn);
                var gl = new GlattMesh(); gl.Tube(Off(rr, .22f, 1.0f), true, GlattMesh.Kreis(.045f, .045f, 14), up);
                Teil("Handlauf", M(gl, "Bahn_Handlauf"), mats["BandeGelb"], bahn);
                var ge = new GlattMesh(); ge.Tube(Off(rr, .2f, .935f), true, GlattMesh.Kreis(.012f, .012f, 8), up);
                Teil("LED-Band", M(ge, "Bahn_LEDBand"), mats["LED"], bahn, false);
                var gpf = new GlattMesh(); var pfo = Off(rr, .24f, 0); var gu = Off(rr, .09f, .3f);
                for (int i = 0; i < n; i += 4) { gpf.Rod(pfo[i], pfo[i] + up * 1f, .035f, 10); gpf.Rod(pfo[i] + up * .3f, gu[i] + up * 0, .022f, 8, false); }
                Teil("Pfosten", M(gpf, "Bahn_Pfosten"), mats["Chrom"], bahn);
            }

            // ---- Stuetzen, Dach, Netz ----
            var stuetzen = new List<Vector3>();
            float sx = HX + WALK - .35f, sz = HZ + WALK - .35f, xa = HX - 4f;
            int nb = Mathf.Max(2, Mathf.RoundToInt(2f * xa / 4f) + 1);
            for (int k = 0; k < nb; k++) stuetzen.Add(new Vector3(-xa + k * 2f * xa / (nb - 1), 0, sz));
            foreach (float x in new[] { -xa, xa }) stuetzen.Add(new Vector3(x, 0, -sz));
            foreach (float z in new[] { -HZ * .37f, HZ * .37f }) { stuetzen.Add(new Vector3(sx, 0, z)); stuetzen.Add(new Vector3(-sx, 0, z)); }
            float ecr = ER + WALK - .35f;
            foreach (var c in new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) })
                stuetzen.Add(new Vector3(c.x * (HX - ER + ecr * .7071f), 0, c.y * (HZ - ER + ecr * .7071f)));
            var st = new MeshBuilder(); var bands = new GlattMesh();
            var B = new BulbSet();
            Color[] bunt = { new Color(1f, .8f, .45f), new Color(1f, .3f, .3f), new Color(.4f, .8f, 1f), new Color(1f, .5f, .9f) };
            for (int i = 0; i < stuetzen.Count; i++)
            {
                var p = stuetzen[i];
                st.Box(p + up * DACH / 2, Quaternion.identity, new Vector3(.24f, DACH, .24f));
                foreach (float y in new[] { .15f, 2.6f, DACH - .2f }) bands.Lathe(p + up * y, up, new[] { new Vector2(0, -.06f), new Vector2(.19f, -.06f), new Vector2(.19f, .06f), new Vector2(0, .06f) }, 4);
                // Lauflicht an der Innenseite der Stuetze
                Vector3 innen = new Vector3(-p.x, 0, -p.z).normalized * .13f;
                B.Line(p + innen + up * .5f, p + innen + up * (DACH - .4f), .28f, .03f, bunt[i % bunt.Length], BulbMode.Chase);
            }
            Teil("Stuetzen", Mm(st, "Bahn_Stuetzen"), mats["Stuetze"], bahn);
            Teil("Stuetzenringe", M(bands, "Bahn_Stuetzenringe"), mats["Chrom"], bahn);
            var dach = new MeshBuilder();
            dach.Box(new Vector3(0, DACH + .2f, 0), Quaternion.identity, new Vector3(2 * (HX + WALK + .6f), .4f, 2 * (HZ + WALK + .6f)));
            float tsx = 2f * HX / Mathf.Max(1f, Mathf.Round(2f * HX / 4f));
            for (float x = -HX; x <= HX + .01f; x += tsx) dach.Box(new Vector3(x, NETZ + .16f, 0), Quaternion.identity, new Vector3(.1f, .14f, 2 * HZ + .4f));
            for (float x = -HX; x <= HX + .01f; x += tsx) foreach (float z in new[] { -HZ, 0, HZ }) dach.Box(new Vector3(x, (NETZ + DACH) / 2 + .1f, z), Quaternion.identity, new Vector3(.04f, DACH - NETZ, .04f));
            Teil("Dach", Mm(dach, "Bahn_Dach"), mats["Traeger"], bahn);
            var netz = new MeshBuilder();
            for (float x = -HX; x <= HX + .001f; x += .3f) netz.Box(new Vector3(x, NETZ + .005f, 0), Quaternion.identity, new Vector3(.006f, .006f, 2 * HZ));
            for (float z = -HZ; z <= HZ + .001f; z += .3f) netz.Box(new Vector3(0, NETZ, z), Quaternion.identity, new Vector3(2 * HX, .006f, .006f));
            Teil("Fahrdraht-Netz", Mm(netz, "Bahn_Netz"), mats["Netz"], bahn);
            var leds = new MeshBuilder();
            for (float z = -(HZ - 1f); z <= HZ - 1f + .01f; z += 2f) leds.Box(new Vector3(0, DACH - .02f, z), Quaternion.identity, new Vector3(2 * HX - 2f, .04f, .08f));
            Teil("LED-Leisten", Mm(leds, "Bahn_LEDLeisten"), mats["LED"], bahn, false);

            // ---- Hindernisse in der Fahrflaeche: Mittelinsel mit Lichtsaeule, gepolsterte Pfeiler ----
            if (hind != null)
            {
                var gummi = new GlattMesh(); var lack = new GlattMesh(); var gelbR = new GlattMesh(); var chrom = new GlattMesh(); var saeule = new GlattMesh(); var krone = new GlattMesh();
                for (int k = 0; k + 2 < hind.Length; k += 3)
                {
                    var c = new Vector3(hind[k], 0, hind[k + 1]); float r = hind[k + 2];
                    if (r > 1f)
                    {
                        // Insel: Prallgurt wie an der Bande (Aussenkante = Radius aus der Physik), blaues Podest, Handlauf, Lichtsaeule bis zum Dach
                        var ring = new List<Vector3>(); var ring2 = new List<Vector3>(); var ring3 = new List<Vector3>();
                        for (int q = 0; q < 64; q++)
                        {
                            float a = -q / 64f * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            ring.Add(c + d * (r - .09f) + up * .3f); ring2.Add(c + d * (r - .16f) + up * .07f); ring3.Add(c + d * (r - .3f) + up * 1.0f);
                        }
                        gummi.Tube(ring, true, gp, up, true, .5f); gummi.Tube(ring2, true, GlattMesh.Kreis(.05f, .07f, 12), up);
                        lack.Lathe(c, up, new[] { new Vector2(0, .96f), new Vector2(r - .35f, .96f), new Vector2(r - .2f, .9f), new Vector2(r - .2f, 0) }, 48);
                        gelbR.Tube(ring3, true, GlattMesh.Kreis(.045f, .045f, 14), up);
                        for (int q = 0; q < 8; q++) { float a = q / 8f * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); chrom.Rod(c + d * (r - .3f) + up * .9f, c + d * (r - .3f) + up * 1.0f, .03f, 8); }
                        saeule.Lathe(c, up, new[] { new Vector2(0, DACH), new Vector2(.2f, DACH), new Vector2(.24f, 1.3f), new Vector2(.42f, 1.05f), new Vector2(.42f, .96f) }, 24);
                        foreach (float y in new[] { 1.4f, 2.6f, DACH - .25f }) chrom.Lathe(c + up * y, up, new[] { new Vector2(0, -.05f), new Vector2(.3f, -.05f), new Vector2(.3f, .05f), new Vector2(0, .05f) }, 24);
                        // Lichtkrone unter dem Netz
                        krone.Lathe(c + up * 2.9f, up, new[] { new Vector2(0, .32f), new Vector2(.6f, .3f), new Vector2(1.25f, .08f), new Vector2(1.3f, 0), new Vector2(1.2f, -.04f), new Vector2(0, -.02f) }, 48);
                        for (int q = 0; q < 28; q++) { float a = q / 28f * Mathf.PI * 2; B.Add(c + new Vector3(Mathf.Cos(a) * 1.33f, 2.92f, Mathf.Sin(a) * 1.33f), .045f, q % 2 == 0 ? new Color(1f, .8f, .45f) : new Color(1f, .4f, .8f), BulbMode.Chase); }
                        for (int q = 0; q < 6; q++)
                        {
                            float a = q / 6f * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            B.Line(c + d * .27f + up * 1.45f, c + d * .23f + up * 2.8f, .22f, .03f, bunt[q % bunt.Length], BulbMode.Chase);
                        }
                        var il = Licht("Insellicht", bahn, LightType.Point, new Color(1f, .7f, .55f), 4f, 7f); il.transform.localPosition = c + up * 2.6f;
                    }
                    else
                    {
                        // Pfeiler: Gummipolster mit gelbem Ring, Stahlrohr bis zum Dach mit Lauflicht
                        gummi.Lathe(c, up, new[] { new Vector2(0, 1.12f), new Vector2(r - .05f, 1.12f), new Vector2(r, 1.05f), new Vector2(r, .05f), new Vector2(r - .05f, 0) }, 32);
                        gelbR.Lathe(c, up, new[] { new Vector2(r + .015f, .82f), new Vector2(r + .015f, .97f) }, 32);
                        saeule.Lathe(c, up, new[] { new Vector2(0, DACH), new Vector2(.12f, DACH), new Vector2(.12f, 1.1f) }, 16);
                        chrom.Lathe(c + up * 1.13f, up, new[] { new Vector2(0, -.03f), new Vector2(.2f, -.03f), new Vector2(.2f, .03f), new Vector2(0, .03f) }, 20);
                        foreach (float s in new[] { -1f, 1f })
                            B.Line(c + new Vector3(s * .13f, 1.3f, 0), c + new Vector3(s * .13f, DACH - .4f, 0), .28f, .03f, bunt[(k / 3) % bunt.Length], BulbMode.Chase);
                    }
                }
                Teil("Hindernisse Gummi", M(gummi, "Bahn_HindGummi"), mats["BandeGummi"], bahn);
                Teil("Insel Podest", M(lack, "Bahn_InselPodest"), mats["Sockel"], bahn);
                Teil("Hindernisse Gelb", M(gelbR, "Bahn_HindGelb"), mats["BandeGelb"], bahn);
                Teil("Hindernisse Chrom", M(chrom, "Bahn_HindChrom"), mats["Chrom"], bahn);
                Teil("Saeulen", M(saeule, "Bahn_Saeulen"), mats["Stuetze"], bahn);
                Teil("Lichtkrone", M(krone, "Bahn_Lichtkrone"), mats["Fries"], bahn);
            }

            // ---- Front: Fries mit Krone, Neon-Schriftzug, Glühbirnen ----
            float zFr = -(HZ + WALK + .55f), fw = HX + WALK + .6f;
            var fr = new MeshBuilder();
            fr.Box(new Vector3(0, DACH + 1.05f, zFr), Quaternion.identity, new Vector3(2 * fw, 2.1f, .25f));
            var kr = new GlattMesh(); var bogenV = new List<Vector3>(); var bogenH = new List<Vector3>();
            for (int k = 0; k <= 24; k++) { float a = Mathf.PI * k / 24f; var q = new Vector3(Mathf.Cos(a) * 6.5f, DACH + 2.1f + Mathf.Sin(a) * 2.2f, zFr - .12f); bogenV.Add(q); bogenH.Add(q + Vector3.forward * .24f); }
            kr.Fan(new Vector3(0, DACH + 2.1f, zFr - .12f), bogenV, Vector3.back); kr.Fan(new Vector3(0, DACH + 2.1f, zFr + .12f), bogenH, Vector3.forward);
            var kg = new Vector3[bogenV.Count, 2]; for (int k = 0; k < bogenV.Count; k++) { kg[k, 0] = bogenV[k]; kg[k, 1] = bogenH[k]; }
            kr.Grid(kg, false, false, 12, 0, Vector3.up);
            Teil("Fries", Mm(fr, "Bahn_Fries"), mats["Fries"], bahn);
            Teil("Krone", M(kr, "Bahn_Krone"), mats["Fries"], bahn);
            var zier = new GlattMesh();
            zier.Tube(new[] { new Vector3(-fw, DACH + 2.08f, zFr - .14f), new Vector3(fw, DACH + 2.08f, zFr - .14f) }, false, GlattMesh.Kreis(.035f, .035f, 10), up);
            zier.Tube(new[] { new Vector3(-fw, DACH + .02f, zFr - .14f), new Vector3(fw, DACH + .02f, zFr - .14f) }, false, GlattMesh.Kreis(.035f, .035f, 10), up);
            var zb = new List<Vector3>(); for (int k = 0; k <= 24; k++) zb.Add(bogenV[k] + Vector3.back * .03f);
            zier.Tube(zb, false, GlattMesh.Kreis(.035f, .035f, 10), Vector3.back);
            Teil("Zierleisten", M(zier, "Bahn_Zierleisten"), mats["Messing"], bahn);
            var schrift = Schrift("AUTOSCOOTER", new Vector3(0, DACH + .42f, zFr - .2f), 1.25f, new[] { mats["NeonPink"], mats["NeonPink"], mats["NeonPink"], mats["NeonPink"], mats["NeonCyan"], mats["NeonCyan"], mats["NeonCyan"], mats["NeonCyan"], mats["NeonCyan"], mats["NeonCyan"], mats["NeonCyan"] }, .05f, bahn, "Schriftzug");
            schrift.gameObject.AddComponent<NeonFlimmern>().schwacherBuchstabe = 9;
            B.Line(new Vector3(-fw + .2f, DACH + 1.92f, zFr - .16f), new Vector3(fw - .2f, DACH + 1.92f, zFr - .16f), .3f, .04f, new Color(1f, .78f, .4f), BulbMode.Chase);
            B.Line(new Vector3(-fw + .2f, DACH + .2f, zFr - .16f), new Vector3(fw - .2f, DACH + .2f, zFr - .16f), .3f, .04f, new Color(1f, .78f, .4f), BulbMode.Alternate);
            for (int k = 1; k < 12; k++)
            {
                float a = Mathf.PI * k / 12f; Vector3 c0 = new Vector3(0, DACH + 2.1f, zFr - .26f);
                B.Line(c0 + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .6f, c0 + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.95f, .22f, .045f, k % 2 == 0 ? new Color(1f, .4f, .8f) : new Color(1f, .85f, .4f), BulbMode.Chase);
            }
            for (int k = 0; k <= 24; k += 1) B.Add(bogenV[k] + Vector3.back * .1f, .045f, new Color(1f, .8f, .45f), BulbMode.Twinkle);
            B.Line(new Vector3(-HX - WALK + .4f, -.08f, zf - .03f), new Vector3(HX + WALK - .4f, -.08f, zf - .03f), .35f, .035f, new Color(1f, .75f, .35f), BulbMode.Alternate);
            foreach (float s in new[] { -1f, 1f })
                B.Line(new Vector3(s * (fw + .02f), DACH + .2f, zFr + .3f), new Vector3(s * (fw + .02f), DACH + .2f, HZ + WALK + .6f), .35f, .035f, new Color(.5f, .8f, 1f), BulbMode.Twinkle);
            B.Build("Gluehbirnen", bahn);

            // ---- Rueckwand mit Spiegeln und Neonpfeilen ----
            float zR = HZ + WALK - .1f;
            var wand = new MeshBuilder(); wand.Box(new Vector3(0, DACH / 2, zR + .1f), Quaternion.identity, new Vector3(2 * (HX + WALK - 1.3f), DACH, .12f));
            Teil("Rueckwand", Mm(wand, "Bahn_Rueckwand"), mats["Wand"], bahn);
            var spieg = new MeshBuilder(); var npP = new GlattMesh(); var npC = new GlattMesh();
            int ms = Mathf.Max(1, Mathf.FloorToInt((2f * HX + 1f) / 4.6f));
            for (int k = 0; k < ms; k++)
            {
                float xm = (k - (ms - 1) / 2f) * 4.6f;
                spieg.Box(new Vector3(xm, 2.55f, zR), Quaternion.identity, new Vector3(3.4f, 3.2f, .04f));
                if (k < ms - 1)
                    for (int a = 0; a < 2; a++)
                    {
                        float x = xm + 2.0f + a * .45f; var g = a == 0 ? npP : npC;
                        Vector3 p0 = new Vector3(x, 1.4f, zR - .06f), p1 = new Vector3(x + .35f, 2.55f, zR - .06f), p2 = new Vector3(x, 3.7f, zR - .06f);
                        g.Rod(p0, p1, .03f, 8, false); g.Rod(p1, p2, .03f, 8, false); g.Sphere(p0, .03f, 8, 6); g.Sphere(p1, .03f, 8, 6); g.Sphere(p2, .03f, 8, 6);
                    }
            }
            npP.Rod(new Vector3(-HX, 4.5f, zR - .06f), new Vector3(HX, 4.5f, zR - .06f), .035f, 8);
            Teil("Spiegel", Mm(spieg, "Bahn_Spiegel"), mats["Spiegel"], bahn);
            Teil("Neon Pfeile Pink", M(npP, "Bahn_NeonPfeileP"), mats["NeonPink"], bahn, false);
            Teil("Neon Pfeile Cyan", M(npC, "Bahn_NeonPfeileC"), mats["NeonCyan"], bahn, false);

            // ---- Lautsprecher in den Ecken ----
            var lb = new MeshBuilder();
            foreach (var c in new[] { new Vector3(-HX, 4.6f, -HZ), new Vector3(HX, 4.6f, -HZ), new Vector3(-HX, 4.6f, HZ), new Vector3(HX, 4.6f, HZ) })
            { lb.Box(c, Quaternion.LookRotation(new Vector3(-c.x, -2, -c.z)), new Vector3(.55f, .8f, .45f)); lb.Box(c + up * .55f, Quaternion.identity, new Vector3(.05f, .5f, .05f)); }
            Teil("Lautsprecher", Mm(lb, "Bahn_Lautsprecher"), mats["Box"], bahn);

            // ---- Kasse vorn links ----
            {
                var kp = new Vector3(-HX + 1.5f, GROUND, zf - 3.2f);
                var kr2 = new MeshBuilder(); var kc = new MeshBuilder(); var fe = new MeshBuilder();
                kr2.Box(kp + up * 1.3f + Vector3.forward * .3f, Quaternion.identity, new Vector3(2.4f, 2.6f, 1.6f));
                kc.Box(kp + up * 2.75f + Vector3.forward * .2f, Quaternion.identity, new Vector3(2.9f, .3f, 2.1f));
                kc.Box(kp + up * 1.0f + Vector3.back * .58f, Quaternion.identity, new Vector3(2.5f, .08f, .3f));
                fe.Box(kp + up * 1.6f + Vector3.back * .51f, Quaternion.identity, new Vector3(1.8f, 1.0f, .02f));
                Teil("Kasse", Mm(kr2, "Bahn_Kasse"), mats["KasseRot"], bahn);
                Teil("Kassendach", Mm(kc, "Bahn_Kassendach"), mats["KasseCreme"], bahn);
                Teil("Kassenfenster", Mm(fe, "Bahn_Kassenfenster"), mats["Fenster"], bahn, false);
                Schrift("KASSE", kp + new Vector3(0, 3.0f, -.85f), .42f, new[] { mats["NeonGelb"] }, .025f, bahn, "Kassenschild");
                var KB = new BulbSet();
                KB.Line(kp + new Vector3(-1.42f, 2.62f, -.86f), kp + new Vector3(1.42f, 2.62f, -.86f), .2f, .03f, new Color(1f, .8f, .45f), BulbMode.Chase);
                KB.Build("Kassenlichter", bahn);
                var kl = Licht("Kassenlicht", bahn, LightType.Point, new Color(1f, .75f, .45f), 3f, 5f); kl.transform.localPosition = kp + new Vector3(0, 1.7f, -1.2f);
            }
            // ---- Laternen auf dem Platz ----
            var lat = new GlattMesh(); var latG = new GlattMesh();
            foreach (var p in new[] { new Vector3(-HX * .5f, GROUND, zf - 4.5f), new Vector3(HX * .5f, GROUND, zf - 4.5f), new Vector3(HX + 4f, GROUND, -4f), new Vector3(-HX - 4f, GROUND, 3f) })
            {
                lat.Lathe(p, up, new[] { new Vector2(0, 0), new Vector2(.16f, 0), new Vector2(.12f, .3f), new Vector2(.06f, .4f), new Vector2(.05f, 3.4f), new Vector2(0, 3.4f) }, 14);
                latG.Blob(p + up * 3.65f, Quaternion.identity, new Vector3(.2f, .28f, .2f), .7f, .7f, 16, 10);
                var l = Licht("Laterne", bahn, LightType.Point, new Color(1f, .75f, .45f), 6f, 12f); l.transform.localPosition = p + up * 3.6f;
            }
            Teil("Laternenmasten", M(lat, "Bahn_Laternen"), mats["Traeger"], bahn);
            Teil("Laternenglas", M(latG, "Bahn_LaternenGlas"), mats["Laterne"], bahn, false);

            // ---- Licht ----
            var licht = new GameObject("Hallenlicht").transform; licht.SetParent(bahn, false);
            Light Spot(string nm, Vector3 pos, Vector3 ziel, Color c, float i, float w, bool sch, float range = 18)
            {
                var l = Licht(nm, licht, LightType.Spot, c, i, range, w, sch); l.transform.localPosition = pos; l.transform.localRotation = Quaternion.LookRotation(ziel - pos); return l;
            }
            int si = 0;
            float[] xs = HX > 13f ? new[] { -HX * .75f, -HX * .25f, HX * .25f, HX * .75f } : new[] { -HX * 2f / 3f, 0f, HX * 2f / 3f };
            foreach (float x in xs)
                foreach (float z in new[] { -HZ * .5f, HZ * .5f })
                    Spot("Deckenstrahler " + (++si), new Vector3(x, DACH - .15f, z), new Vector3(x, 0, z), new Color(1f, .95f, .88f), 32f, 95, xs.Length == 3 ? x == 0f : x == xs[1]);
            Spot("Fassade Pink", new Vector3(-7f, GROUND + .3f, zf - 7f), new Vector3(-4f, DACH + 1.2f, zFr), new Color(1f, .25f, .75f), 60f, 40, false, 30);
            Spot("Fassade Blau", new Vector3(7f, GROUND + .3f, zf - 7f), new Vector3(4f, DACH + 1.2f, zFr), new Color(.25f, .55f, 1f), 60f, 40, false, 30);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + .5f) / 8f * Mathf.PI * 2; var p = new Vector3(Mathf.Sin(a) * (HX - 1.5f), 1.6f, Mathf.Cos(a) * (HZ - 1.2f));
                var l = Licht("Bandenlicht " + (i + 1), licht, LightType.Point, i % 2 == 0 ? new Color(1f, .3f, .75f) : new Color(.25f, .75f, 1f), 4f, 6f); l.transform.localPosition = p;
            }
            // Moving Heads unter der Dachkante vorn
            for (int i = 0; i < 4; i++)
            {
                var head = new GameObject("Moving Head " + (i + 1)).transform; head.SetParent(licht, false); head.localPosition = new Vector3(-HX * .75f + i * HX * .5f, DACH - .35f, -HZ - .6f);
                var mh = head.gameObject.AddComponent<BoxautoMovingHead>(); mh.offset = i * 70f + 180f; mh.speed = .25f + i * .04f;
                var spot = Licht("Spot", head, LightType.Spot, Color.white, 40f, 26f, 18f, false);
                mh.spot = spot;
                var hm = new GlattMesh(); hm.Lathe(new Vector3(0, 0, -.25f), Vector3.forward, new[] { new Vector2(0, 0), new Vector2(.18f, 0), new Vector2(.2f, .4f), new Vector2(.15f, .45f), new Vector2(0, .45f) }, 16);
                Teil("Kopf", M(hm, "Bahn_MHKopf_" + i), mats["Box"], head);
                if (mats.TryGetValue("Beam" + i, out var bm))
                {
                    var g = new GlattMesh(); g.BeamCone(new Vector3(0, 0, .2f), Vector3.forward, .14f, 2.2f, 7f, 24);
                    var c = Teil("Lichtkegel", M(g, "Bahn_MHKegel_" + i), bm, head, false); c.GetComponent<Renderer>().receiveShadows = false;
                    mh.beam = bm;
                }
            }

            // ---- Ton ----
            AudioClip Clip(string f) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + f);
            var musik = new GameObject("Musik").AddComponent<AudioSource>(); musik.transform.SetParent(bahn, false); musik.transform.localPosition = new Vector3(0, 4.5f, 0);
            musik.clip = Clip("autoscooter.mp3"); musik.loop = true; musik.playOnAwake = true; musik.volume = musikLaut; musik.spatialBlend = .6f; musik.minDistance = 8; musik.maxDistance = 80;
            var platz = new GameObject("Rummel-Geraeusche").AddComponent<AudioSource>(); platz.transform.SetParent(bahn, false);
            platz.clip = Clip("rummel_nacht.mp3"); platz.loop = true; platz.playOnAwake = true; platz.volume = .25f; platz.spatialBlend = 0;

            // ---- Spiegelungs-Sonde (Kasten um die Halle) ----
            var pr = new GameObject("Spiegelung").AddComponent<ReflectionProbe>(); pr.transform.SetParent(bahn, false);
            pr.transform.localPosition = new Vector3(0, 2.2f, 0);
            pr.mode = ReflectionProbeMode.Realtime; pr.refreshMode = ReflectionProbeRefreshMode.ViaScripting; pr.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            pr.size = new Vector3(2 * (HX + WALK) + 2, 9, 2 * (HZ + WALK) + 2); pr.center = new Vector3(0, 2f, 0); pr.boxProjection = true; pr.resolution = 256; pr.hdr = true; pr.importance = 2;

            return new HalleTeile { licht = licht, sonde = pr };
        }

        /// <summary>Vorfuehrszene Boxauto_Vorschau.unity: Halle 24 x 14 m, 8 Wagen mit Demo-Fahrt, eigene Kamera.</summary>
        static void Bahn(GameObject prefab, Dictionary<string, Material> mats, Tex tex)
        {
            HX = 12f; HZ = 7f; ER = 2.5f;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            neueMeshes.Clear();
            var bahn = new GameObject("Boxautobahn").transform;
            var h = Halle(bahn, mats, null, .55f);

            // ---- Wagen ----
            Color[] farben = { new Color(.62f, .02f, .04f), new Color(.03f, .12f, .55f), new Color(.12f, .5f, .04f), new Color(.85f, .55f, .02f),
                               new Color(.75f, .05f, .38f), new Color(.9f, .25f, .02f), new Color(.3f, .05f, .55f), new Color(.02f, .02f, .025f) };
            var wagenRoot = new GameObject("Wagen").transform;
            var wagen = new List<Transform>();
            var rnd = new System.Random(11);
            for (int i = 0; i < 8; i++)
            {
                var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wagenRoot);
                car.name = "Boxauto " + (i + 1);
                car.transform.position = new Vector3(-7.5f + (i % 4) * 5f, 0, i < 4 ? -2.8f : 2.8f);
                car.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                var fa = car.AddComponent<BoxautoFarbe>(); fa.lack = farben[i]; fa.nummer = tex.nummern[i + 1]; fa.Anwenden();
                wagen.Add(car.transform);
            }
            var demo = new GameObject("Demo-Fahrt").AddComponent<BoxautoDemo>();
            demo.wagen = wagen.ToArray(); demo.halb = new Vector2(HX, HZ); demo.eckRadius = ER;
            var leute = new GameObject("Menschen").AddComponent<BoxautoLeute>();
            leute.demo = demo; leute.halb = new Vector2(HX, HZ); leute.steg = WALK; leute.platz = GROUND;

            // ---- Umgebung, Nachbearbeitung, Kamera ----
            if (mats.TryGetValue("Himmel", out var himmel)) { RenderSettings.skybox = himmel; RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = .6f; }
            else { RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.12f, .1f, .18f); RenderSettings.ambientEquatorColor = new Color(.06f, .05f, .08f); RenderSettings.ambientGroundColor = new Color(.02f, .02f, .025f); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .012f; RenderSettings.fogColor = new Color(.05f, .04f, .08f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            string profPath = Root + "/Boxauto_Vorschau_Profil.asset";
            AssetDatabase.DeleteAsset(profPath);
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(prof, profPath);
            T Add<T>() where T : VolumeComponent { var c = prof.Add<T>(true); c.name = typeof(T).Name; if (!AssetDatabase.Contains(c)) AssetDatabase.AddObjectToAsset(c, prof); return c; }
            Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bl = Add<Bloom>(); bl.threshold.Override(.85f); bl.intensity.Override(1.3f); bl.scatter.Override(.74f); bl.highQualityFiltering.Override(true);
            var ca = Add<ColorAdjustments>(); ca.postExposure.Override(.35f); ca.contrast.Override(14f); ca.saturation.Override(12f);
            Add<Vignette>().intensity.Override(.28f);
            Add<ChromaticAberration>().intensity.Override(.05f);
            var fg = Add<FilmGrain>(); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(.1f);
            EditorUtility.SetDirty(prof); AssetDatabase.SaveAssets();
            var vol = new GameObject("Nachbearbeitung").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = prof;

            var cam = new GameObject("Vorschau-Kamera").AddComponent<Camera>(); cam.gameObject.tag = "MainCamera";
            cam.clearFlags = himmel != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.01f, .01f, .02f);
            cam.fieldOfView = 45; cam.nearClipPlane = .05f; cam.farClipPlane = 600; cam.allowHDR = true;
            cam.transform.position = new Vector3(-8f, 6.5f, -26f); cam.transform.LookAt(new Vector3(0, 2.5f, 0));
            cam.gameObject.AddComponent<AudioListener>();
            var cd = cam.GetUniversalAdditionalCameraData();
            cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cd.antialiasingQuality = AntialiasingQuality.High; cd.renderShadows = true;

            var vs = new GameObject("Vorschau").AddComponent<BoxautoVorschau>();
            vs.kamera = cam; vs.demo = demo; vs.sonde = h.sonde; vs.hallenLicht = h.licht.gameObject;

            // Alle noch nicht gespeicherten Meshes (auch die der Gluehbirnen) in ein Asset
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (mf.sharedMesh != null && !AssetDatabase.Contains(mf.sharedMesh) && !neueMeshes.Contains(mf.sharedMesh)) neueMeshes.Add(mf.sharedMesh);
            MeshesSpeichern(ModDir + "/Bahn_Meshes.asset");
            AssetDatabase.DeleteAsset(ModDir + "/Halle_Meshes.asset");
            EditorSceneManager.SaveScene(scene, Root + "/Boxauto_Vorschau.unity");
        }

        /// <summary>
        /// Halle fuers Spiel (Bahn 9) als Prefab in Resources: Groesse und Hindernisse genau wie die Fahrflaeche des Fahrmodells
        /// (TrackDefs.Boxauto: 30 x 19 m, Insel r 2,1 in der Mitte, Pfeiler r 0,45 bei +-8 / +-4,5). Ecken fast eckig wie die Bande in der Physik.
        /// Wagen setzt das Spiel selbst ein (FairWorld.Scooter). Nur Optik.
        /// </summary>
        public const float SpielHX = 15f, SpielHZ = 9.5f;
        static void SpielHalle(GameObject wagenPrefab, Dictionary<string, Material> mats, Tex tex)
        {
            HX = SpielHX; HZ = SpielHZ; ER = .35f;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            neueMeshes.Clear();
            var root = new GameObject("Boxautobahn_Spiel");
            SceneManager.MoveGameObjectToScene(root, scene);
            var h = Halle(root.transform, mats, new[] { 0f, 0f, 2.1f, -8f, -4.5f, .45f, 8f, -4.5f, .45f, -8f, 4.5f, .45f, 8f, 4.5f, .45f }, .42f);
            var hc = root.AddComponent<BoxautoHalle>();
            hc.wagenPrefab = wagenPrefab; hc.nummern = tex.nummern; hc.sonde = h.sonde;
            hc.halb = new Vector2(HX, HZ); hc.steg = WALK; hc.platz = GROUND;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && !AssetDatabase.Contains(mf.sharedMesh) && !neueMeshes.Contains(mf.sharedMesh)) neueMeshes.Add(mf.sharedMesh);
            MeshesSpeichern(ModDir + "/Spiel_Meshes.asset");
            System.IO.Directory.CreateDirectory(Root + "/Resources");
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Resources/Boxautobahn_Spiel.prefab");
            EditorSceneManager.CloseScene(scene, true);
            HX = 12f; HZ = 7f; ER = 2.5f;
        }
    }
}
