using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Menue "Achterbahn/Boxauto-Asset erzeugen": baut Texturen, Materialien, Meshes, das Prefab Boxauto.prefab
    /// und die Vorfuehrszene Boxauto_Vorschau.unity (Stueck Boxautobahn mit Stahlboden, Fahrdraht-Netz, Bande, Hallenlicht).
    /// </summary>
    public static partial class BoxautoErzeugen
    {
        const string Root = "Assets/Boxauto";
        const string TexDir = Root + "/Texturen", MatDir = Root + "/Materialien", ModDir = Root + "/Modell";
        public const int Startnummer = 7;

        [MenuItem("Achterbahn/Boxauto-Asset erzeugen")]
        public static void Erzeugen()
        {
            foreach (var d in new[] { TexDir, MatDir, ModDir }) Directory.CreateDirectory(d);
            var tex = Texturen();
            var mats = Materialien(tex);
            var prefab = Prefab(mats);
            var bahnMats = BahnMaterialien(new Dictionary<string, Material>(mats), tex);
            Bahn(prefab, bahnMats, tex);
            SpielHalle(prefab, bahnMats, tex);
            Debug.Log("Boxauto: Asset, Vorschauszene und Spiel-Halle erzeugt (" + Root + ")");
        }

        // =====================================================================
        // Texturen
        // =====================================================================
        internal class Tex { public Texture2D flocken, nummer, flagge, bodenC, bodenN, bodenM; public Texture2D[] nummern; public (Texture2D c, Texture2D n) riffel; }

        static uint H(int x, int y, int s) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + s * 982451653); h = (h ^ (h >> 13)) * 1274126177u; return h ^ (h >> 16); } }
        static float R(int x, int y, int s) => (H(x, y, s) & 0xFFFFFF) / 16777216f;
        static float VN(float x, float y, int per, int s)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int X0 = ((x0 % per) + per) % per, Y0 = ((y0 % per) + per) % per, X1 = (X0 + 1) % per, Y1 = (Y0 + 1) % per;
            return Mathf.Lerp(Mathf.Lerp(R(X0, Y0, s), R(X1, Y0, s), fx), Mathf.Lerp(R(X0, Y1, s), R(X1, Y1, s), fx), fy);
        }
        static float Fbm(float u, float v, int per, int okt, int s)
        {
            float a = .5f, sum = 0, norm = 0;
            for (int o = 0; o < okt; o++) { sum += a * VN(u * per, v * per, per, s + o * 17); norm += a; a *= .5f; per *= 2; }
            return sum / norm;
        }

        static Texture2D Speichern(Texture2D t, string name, bool normal = false, bool linear = false, bool clamp = false, FilterMode fm = FilterMode.Trilinear)
        {
            string p = TexDir + "/" + name + ".png";
            File.WriteAllBytes(p, t.EncodeToPNG());
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !(normal || linear);
            imp.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            imp.filterMode = fm; imp.anisoLevel = 8; imp.mipmapEnabled = true;
            imp.alphaIsTransparency = clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        static Tex Texturen()
        {
            var r = new Tex();
            // --- Metallic-Flocken unter dem Klarlack (Normal Map, Rauschen pro Pixel) ---
            {
                const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
                var px = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        float nx = (R(x, y, 1) - .5f) * .7f, ny = (R(x, y, 2) - .5f) * .7f; float nz = Mathf.Sqrt(Mathf.Max(0, 1 - nx * nx - ny * ny));
                        px[y * N + x] = new Color(nx * .5f + .5f, ny * .5f + .5f, nz * .5f + .5f, 1);
                    }
                t.SetPixels(px); t.Apply(); r.flocken = Speichern(t, "Lack_Flocken_n", normal: true);
            }
            // --- Startnummer: weisser Kreis, schwarzer Rand, schwarze Ziffer ---
            r.nummer = NummerTextur(Startnummer, "Startnummer");
            r.nummern = new Texture2D[9];
            for (int k = 1; k <= 8; k++) r.nummern[k] = NummerTextur(k, "Startnummer_" + k);
            r.riffel = RiffelblechTextur();
            // --- Zielflagge ---
            {
                const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { bool w = ((x / 32) + (y / 32)) % 2 == 0; px[y * N + x] = w ? new Color(.92f, .92f, .9f) : new Color(.03f, .03f, .035f); }
                t.SetPixels(px); t.Apply(); r.flagge = Speichern(t, "Zielflagge", fm: FilterMode.Bilinear);
            }
            // --- Stahlboden der Bahn: 2 x 2 m, zwei Platten 1 x 2 m, Nieten, Laengsschliff, Reifenabrieb ---
            {
                const int N = 1024; var col = new Color[N * N]; var msk = new Color[N * N]; var hgt = new float[N * N];
                var rnd = new System.Random(5);
                var arcs = new List<(Vector2 c, float r, float w, float a)>();
                for (int k = 0; k < 9; k++) arcs.Add((new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()), .25f + (float)rnd.NextDouble() * .6f, .003f + (float)rnd.NextDouble() * .006f, .12f + (float)rnd.NextDouble() * .2f));
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float u = (x + .5f) / N, v = (y + .5f) / N;
                        float wear = Fbm(u, v, 4, 4, 11);
                        float brushed = Fbm(u * 1f, v * .03f + u * .0f, 64, 2, 23) * .6f + R(x, y / 6, 31) * .4f;
                        float h = 0;
                        // Fugen (u = 0 und .5, v = 0)
                        float du = Mathf.Min(Mathf.Abs(u - .5f), Mathf.Min(u, 1 - u)) * N, dv = Mathf.Min(v, 1 - v) * N;
                        float fuge = Mathf.Clamp01(1.6f - Mathf.Min(du, dv) / 2.2f);
                        h -= fuge * .8f;
                        // Nieten entlang der Fugen
                        float niete = 0;
                        for (int s = 0; s < 3; s++)
                        {
                            float ux = s == 0 ? 0 : s == 1 ? .5f : 1f;
                            float vv = Mathf.Round(v * 8) / 8f;
                            float d1 = new Vector2((u - ux - .012f) * N, (v - vv) * N).magnitude, d2 = new Vector2((u - ux + .012f) * N, (v - vv) * N).magnitude;
                            niete = Mathf.Max(niete, Mathf.Clamp01(1 - Mathf.Min(d1, d2) / 6f));
                        }
                        for (int s = 0; s < 3; s++)
                        {
                            float vx = s == 0 ? 0 : s == 1 ? 1f : 0f; float uu = Mathf.Round(u * 8) / 8f;
                            float d1 = new Vector2((u - uu) * N, (v - vx - .012f) * N).magnitude, d2 = new Vector2((u - uu) * N, (v - vx + .012f) * N).magnitude;
                            niete = Mathf.Max(niete, Mathf.Clamp01(1 - Mathf.Min(d1, d2) / 6f));
                        }
                        h += Mathf.Sqrt(niete) * .9f;
                        h += (Fbm(u, v, 16, 3, 41) - .5f) * .25f;
                        // Gummiabrieb der Boxautos (dunkle Boegen)
                        float scuff = 0;
                        foreach (var a in arcs)
                        {
                            Vector2 d = new Vector2(u, v) - a.c; d.x -= Mathf.Round(d.x); d.y -= Mathf.Round(d.y);
                            float e = Mathf.Abs(d.magnitude - a.r);
                            scuff = Mathf.Max(scuff, a.a * Mathf.Clamp01(1 - e / a.w) * Mathf.Clamp01(Fbm(u, v, 8, 3, 77) * 3f - 1.2f));
                        }
                        float g = Mathf.Lerp(.30f, .44f, brushed) * Mathf.Lerp(.8f, 1.05f, wear);
                        g = Mathf.Lerp(g, .06f, scuff * .8f); g *= 1 - fuge * .45f; g = Mathf.Lerp(g, .5f, niete * .4f);
                        col[y * N + x] = new Color(g, g * 1.01f, g * 1.04f, 1);
                        float sm = Mathf.Lerp(.5f, .78f, brushed) * Mathf.Lerp(.85f, 1f, wear) - scuff * .35f - fuge * .4f;
                        msk[y * N + x] = new Color(Mathf.Lerp(.9f, .3f, scuff), 1 - fuge * .6f, 0, Mathf.Clamp01(sm));
                        hgt[y * N + x] = h;
                    }
                var nrm = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float hx = hgt[y * N + (x + 1) % N] - hgt[y * N + (x - 1 + N) % N], hy = hgt[((y + 1) % N) * N + x] - hgt[((y - 1 + N) % N) * N + x];
                        var n = new Vector3(-hx * 1.2f, -hy * 1.2f, 1).normalized;
                        nrm[y * N + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                    }
                var t1 = new Texture2D(N, N, TextureFormat.RGBA32, false); t1.SetPixels(col); t1.Apply(); r.bodenC = Speichern(t1, "Stahlboden_Farbe");
                var t2 = new Texture2D(N, N, TextureFormat.RGBA32, false); t2.SetPixels(nrm); t2.Apply(); r.bodenN = Speichern(t2, "Stahlboden_Normal", normal: true);
                var t3 = new Texture2D(N, N, TextureFormat.RGBA32, false); t3.SetPixels(msk); t3.Apply(); r.bodenM = Speichern(t3, "Stahlboden_Maske", linear: true);
            }
            return r;
        }

        static Texture2D NummerTextur(int nummer, string name)
        {
            const int N = 512; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color[N * N];
            var segs = new List<(Vector2, Vector2)>();
            string s = nummer.ToString(); float gw = .30f, gh = .50f, gap = .07f;
            float total = s.Length * gw + (s.Length - 1) * gap, x0 = .5f - total / 2;
            for (int k = 0; k < s.Length; k++)
                foreach (var line in Ziffer(s[k] - '0'))
                    for (int q = 0; q + 1 < line.Length; q++)
                    {
                        Vector2 Map(Vector2 g) => new Vector2(x0 + k * (gw + gap) + g.x / .6f * gw, .5f - gh / 2 + g.y * gh);
                        segs.Add((Map(line[q]), Map(line[q + 1])));
                    }
            float pw = 1.5f / N;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    var p = new Vector2((x + .5f) / N, (y + .5f) / N);
                    float d = (p - new Vector2(.5f, .5f)).magnitude;
                    float alpha = Mathf.Clamp01((.49f - d) / pw);
                    float ring = Mathf.Clamp01((d - .435f) / pw);
                    float dmin = 9; foreach (var (a, b) in segs) dmin = Mathf.Min(dmin, SegDist(p, a, b));
                    float ink = Mathf.Max(ring, Mathf.Clamp01((.04f - dmin) / pw));
                    float c = Mathf.Lerp(.93f, .03f, ink);
                    px[y * N + x] = new Color(c, c, c * .97f, alpha);
                }
            t.SetPixels(px); t.Apply(); return Speichern(t, name, clamp: true);
        }

        /// <summary>Riffelblech (Aluminium, Linsenmuster) fuer den Laufsteg: Farbe in RGB, Glaette in A; dazu Normal Map.</summary>
        static (Texture2D c, Texture2D n) RiffelblechTextur()
        {
            const int N = 512, Z = 8; var hg = new float[N * N]; var col = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + .5f) / N * Z, v = (y + .5f) / N * Z;
                    int cx = Mathf.FloorToInt(u), cy = Mathf.FloorToInt(v);
                    float fx = u - cx - .5f, fy = v - cy - .5f;
                    float ang = ((cx + cy) % 2 == 0 ? 45f : -45f) * Mathf.Deg2Rad;
                    float a = fx * Mathf.Cos(ang) + fy * Mathf.Sin(ang), b = -fx * Mathf.Sin(ang) + fy * Mathf.Cos(ang);
                    float e = (a / .36f) * (a / .36f) + (b / .08f) * (b / .08f);
                    float h = Mathf.Clamp01(1.2f - e) ; h = h * h * (3 - 2 * h);
                    hg[y * N + x] = h;
                    float g = .62f + (Fbm(x / (float)N, y / (float)N, 8, 3, 91) - .5f) * .12f + h * .06f;
                    float sm = Mathf.Clamp01(.55f + h * .2f - (Fbm(x / (float)N, y / (float)N, 4, 3, 97) - .5f) * .3f);
                    col[y * N + x] = new Color(g, g, g * 1.02f, sm);
                }
            var nrm = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float hx = hg[y * N + (x + 1) % N] - hg[y * N + (x - 1 + N) % N], hy = hg[((y + 1) % N) * N + x] - hg[((y - 1 + N) % N) * N + x];
                    var n = new Vector3(-hx * 2.5f, -hy * 2.5f, 1).normalized;
                    nrm[y * N + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                }
            var t1 = new Texture2D(N, N, TextureFormat.RGBA32, false); t1.SetPixels(col); t1.Apply();
            var t2 = new Texture2D(N, N, TextureFormat.RGBA32, false); t2.SetPixels(nrm); t2.Apply();
            return (Speichern(t1, "Riffelblech_Farbe"), Speichern(t2, "Riffelblech_Normal", normal: true));
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>Strich-Ziffern im Kasten 0,6 x 1.</summary>
        static Vector2[][] Ziffer(int d)
        {
            Vector2 V(float x, float y) => new Vector2(x, y);
            switch (d)
            {
                case 0: return new[] { new[] { V(0, .15f), V(0, .85f), V(.15f, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .15f), V(.45f, 0), V(.15f, 0), V(0, .15f) } };
                case 1: return new[] { new[] { V(.12f, .78f), V(.34f, 1), V(.34f, 0) }, new[] { V(.12f, 0), V(.56f, 0) } };
                case 2: return new[] { new[] { V(0, .85f), V(.15f, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .62f), V(0, 0), V(.6f, 0) } };
                case 3: return new[] { new[] { V(0, .85f), V(.15f, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .65f), V(.45f, .52f), V(.2f, .52f) }, new[] { V(.45f, .52f), V(.6f, .38f), V(.6f, .15f), V(.45f, 0), V(.15f, 0), V(0, .15f) } };
                case 4: return new[] { new[] { V(.45f, 0), V(.45f, 1), V(0, .3f), V(.6f, .3f) } };
                case 5: return new[] { new[] { V(.6f, 1), V(0, 1), V(0, .55f), V(.45f, .58f), V(.6f, .42f), V(.6f, .15f), V(.45f, 0), V(.15f, 0), V(0, .15f) } };
                case 6: return new[] { new[] { V(.55f, .9f), V(.4f, 1), V(.15f, 1), V(0, .85f), V(0, .15f), V(.15f, 0), V(.45f, 0), V(.6f, .15f), V(.6f, .42f), V(.45f, .56f), V(.15f, .56f), V(0, .42f) } };
                case 7: return new[] { new[] { V(0, 1), V(.6f, 1), V(.18f, 0) }, new[] { V(.14f, .5f), V(.5f, .5f) } };
                case 8: return new[] { new[] { V(.15f, .52f), V(0, .65f), V(0, .85f), V(.15f, 1), V(.45f, 1), V(.6f, .85f), V(.6f, .65f), V(.45f, .52f), V(.15f, .52f), V(0, .38f), V(0, .15f), V(.15f, 0), V(.45f, 0), V(.6f, .15f), V(.6f, .38f), V(.45f, .52f) } };
                default: return new[] { new[] { V(.05f, .1f), V(.2f, 0), V(.45f, 0), V(.6f, .15f), V(.6f, .85f), V(.45f, 1), V(.15f, 1), V(0, .85f), V(0, .58f), V(.15f, .44f), V(.45f, .44f), V(.6f, .58f) } };
            }
        }

        // =====================================================================
        // Materialien
        // =====================================================================
        // Neue Materialien erst fertig einstellen und dann als Asset anlegen (sonst laedt der Import den leeren Stand zurueck)
        static readonly List<(Material m, string p)> neueMats = new List<(Material, string)>();
        static Material Mat(string name, string shader = "Universal Render Pipeline/Lit")
        {
            string p = MatDir + "/" + name + ".mat";
            var sh = Shader.Find(shader) ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(sh) { name = name }; neueMats.Add((m, p)); }
            else if (m.shader != sh) m.shader = sh;
            m.shaderKeywords = new string[0];
            return m;
        }
        static Material Lit(string name, Color c, float metal, float smooth)
        {
            var m = Mat(name); m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); return m;
        }
        static Material Lack(string name, Color c, float metal, float smooth, Texture2D flocken)
        {
            var m = Mat(name, "Universal Render Pipeline/Complex Lit");
            m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_ClearCoat", 1); m.SetFloat("_ClearCoatMask", 1); m.SetFloat("_ClearCoatSmoothness", .96f); m.EnableKeyword("_CLEARCOAT");
            if (flocken != null) { m.SetTexture("_BumpMap", flocken); m.SetFloat("_BumpScale", .35f); m.EnableKeyword("_NORMALMAP"); m.SetTextureScale("_BaseMap", new Vector2(6, 6)); }
            return m;
        }
        static Material Glow(string name, Color basis, Color emis, float smooth = .9f)
        {
            var m = Lit(name, basis, 0, smooth);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }

        static Dictionary<string, Material> Materialien(Tex t)
        {
            var d = new Dictionary<string, Material>();
            d["Lack"] = Lack("Boxauto_Lack", new Color(.62f, .02f, .04f), .55f, .6f, t.flocken);
            d["Akzent"] = Lack("Boxauto_Perlweiss", new Color(.86f, .85f, .82f), .25f, .7f, t.flocken);
            d["Chrom"] = Lit("Boxauto_Chrom", new Color(.96f, .96f, .97f), 1, .94f);
            d["Stahl"] = Lit("Boxauto_Stahl", new Color(.2f, .2f, .22f), .85f, .55f);
            d["Gummi"] = Lit("Boxauto_Gummi", new Color(.028f, .028f, .03f), 0, .36f);
            d["Wulst"] = Lit("Boxauto_Kunstleder_Schwarz", new Color(.03f, .03f, .035f), 0, .62f);
            d["Polster"] = Lit("Boxauto_Kunstleder_Creme", new Color(.86f, .8f, .68f), 0, .55f);
            d["Innen"] = Lit("Boxauto_Innen", new Color(.06f, .06f, .07f), 0, .3f);
            d["Matte"] = Lit("Boxauto_Bodenmatte", new Color(.05f, .05f, .05f), 0, .12f);
            d["Messing"] = Lit("Boxauto_Messing", new Color(.95f, .72f, .36f), 1, .82f);
            d["Griff"] = Lit("Boxauto_Lenkradgriff", new Color(.04f, .035f, .03f), 0, .45f);
            d["Scheinwerfer"] = Glow("Boxauto_Scheinwerferglas", new Color(.9f, .9f, .86f), new Color(1f, .93f, .8f) * 6f, .95f);
            d["Ruecklicht"] = Glow("Boxauto_Ruecklicht", new Color(.5f, 0, 0), new Color(1f, .05f, .03f) * 3f);
            d["Unterboden"] = Glow("Boxauto_Unterboden", new Color(.1f, .1f, .1f), new Color(.1f, .8f, 1f) * 4f);
            d["KontrolleGruen"] = Glow("Boxauto_Lampe_Gruen", new Color(.1f, .4f, .1f), new Color(.2f, 1f, .3f) * 3f);
            d["KontrolleGelb"] = Glow("Boxauto_Lampe_Gelb", new Color(.5f, .35f, .05f), new Color(1f, .7f, .1f) * .3f);
            d["Birne"] = Glow("Boxauto_Gluehbirne", new Color(.32f, .29f, .25f), new Color(1f, .8f, .5f) * 4f, .96f);
            var fl = Lit("Boxauto_Zielflagge", Color.white, 0, .25f); fl.SetTexture("_BaseMap", t.flagge); d["Fahne"] = fl;
            var nr = Lit("Boxauto_Startnummer", Color.white, 0, .85f); nr.SetTexture("_BaseMap", t.nummer);
            nr.SetFloat("_AlphaClip", 1); nr.SetFloat("_Cutoff", .5f); nr.EnableKeyword("_ALPHATEST_ON"); nr.renderQueue = 2450; d["Nummer"] = nr;

            // Halle
            var bo = Lit("Halle_Stahlboden", Color.white, 1, 1);
            bo.SetTexture("_BaseMap", t.bodenC); bo.SetTexture("_BumpMap", t.bodenN); bo.SetFloat("_BumpScale", .8f); bo.EnableKeyword("_NORMALMAP");
            bo.SetTexture("_MetallicGlossMap", t.bodenM); bo.EnableKeyword("_METALLICSPECGLOSSMAP"); bo.SetFloat("_SmoothnessTextureChannel", 0);
            d["Boden"] = bo;
            d["Netz"] = Lit("Halle_Fahrdraht", new Color(.5f, .48f, .44f), .9f, .6f);
            d["Traeger"] = Lit("Halle_Traeger", new Color(.05f, .05f, .06f), .6f, .35f);
            d["Wand"] = Lit("Halle_Wand", new Color(.025f, .025f, .035f), 0, .25f);
            d["BandeGummi"] = Lit("Halle_Bande_Gummi", new Color(.03f, .03f, .032f), 0, .4f);
            d["BandeGelb"] = Lack("Halle_Bande_Gelb", new Color(.9f, .62f, .02f), .2f, .6f, null);
            d["LED"] = Glow("Halle_LED_Warm", new Color(.1f, .1f, .1f), new Color(1f, .85f, .6f) * 3.5f);
            d["Softbox"] = Glow("Halle_Softbox", new Color(.1f, .1f, .1f), new Color(1f, .97f, .92f) * 2.2f);
            d["NeonPink"] = Glow("Halle_Neon_Pink", new Color(.1f, 0, .05f), new Color(1f, .1f, .6f) * 5f);
            d["NeonCyan"] = Glow("Halle_Neon_Cyan", new Color(0, .05f, .1f), new Color(.1f, .8f, 1f) * 5f);
            var beamSh = Shader.Find("Achterbahn/LightBeam");
            if (beamSh != null)
            {
                var bm = Mat("Halle_Lichtkegel", "Achterbahn/LightBeam"); bm.SetColor("_Color", new Color(1f, .95f, .85f)); bm.SetFloat("_Intensity", .16f); d["Kegel"] = bm;
                var bw = Mat("Boxauto_Scheinwerferkegel", "Achterbahn/LightBeam"); bw.SetColor("_Color", new Color(1f, .93f, .8f)); bw.SetFloat("_Intensity", .12f); d["KegelAuto"] = bw;
            }
            var ta = ThemeAssets.Get();
            if (ta != null && ta.spark != null) d["Funken"] = ta.spark;
            foreach (var (m, p) in neueMats) AssetDatabase.CreateAsset(m, p);
            neueMats.Clear();
            foreach (var m in d.Values) EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            // Speicherstand neu einlesen, damit die Keywords im Editor sicher dem Dateistand entsprechen
            var neu = new Dictionary<string, Material>();
            foreach (var kv in d)
            {
                string p = AssetDatabase.GetAssetPath(kv.Value);
                if (p.StartsWith(MatDir)) { AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); neu[kv.Key] = AssetDatabase.LoadAssetAtPath<Material>(p); }
                else neu[kv.Key] = kv.Value;
            }
            return neu;
        }

        // =====================================================================
        // Prefab
        // =====================================================================
        static readonly List<Mesh> neueMeshes = new List<Mesh>();

        static GameObject Teil(string name, Mesh mesh, Material mat, Transform parent, bool schatten = true)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = schatten ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
        static Mesh M(GlattMesh g, string name) { var m = g.ToMesh(name); neueMeshes.Add(m); return m; }

        static void MeshesSpeichern(string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(neueMeshes[0], path);
            for (int i = 1; i < neueMeshes.Count; i++) AssetDatabase.AddObjectToAsset(neueMeshes[i], path);
            AssetDatabase.SaveAssets();
            neueMeshes.Clear();
        }

        static Light Licht(string name, Transform parent, LightType typ, Color c, float intens, float range, float winkel = 60, bool schatten = false)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light>(); l.type = typ; l.color = c; l.intensity = intens; l.range = range;
            if (typ == LightType.Spot) { l.spotAngle = winkel; l.innerSpotAngle = winkel * .55f; }
            l.shadows = schatten ? LightShadows.Soft : LightShadows.None;
            if (schatten) l.shadowResolution = LightShadowResolution.VeryHigh;
            return l;
        }

        static GameObject Prefab(Dictionary<string, Material> mats)
        {
            var T = BoxautoModell.Baue(3.55f);
            neueMeshes.Clear();
            var root = new GameObject("Boxauto");
            var aufbau = new GameObject("Aufbau").transform; aufbau.SetParent(root.transform, false);
            var lackR = new List<Renderer>();
            var R = new Dictionary<string, Renderer>();
            foreach (var kv in T.aufbau)
            {
                if (kv.Value.Count == 0) continue;
                var go = Teil(kv.Key, M(kv.Value, "Boxauto_" + kv.Key), mats[kv.Key], aufbau, kv.Key != "Unterboden");
                R[kv.Key] = go.GetComponent<Renderer>();
                if (kv.Key == "Lack") lackR.Add(go.GetComponent<Renderer>());
            }
            var lenk = new GameObject("Lenkrad").transform; lenk.SetParent(aufbau, false);
            lenk.localPosition = T.lenkradPose.position; lenk.localRotation = T.lenkradPose.rotation;
            foreach (var kv in T.lenkrad) Teil(kv.Key, M(kv.Value, "Boxauto_Lenkrad_" + kv.Key), mats[kv.Key], lenk);

            var birnenRoot = new GameObject("Gluehbirnen").transform; birnenRoot.SetParent(aufbau, false);
            var birneMesh = M(T.birne, "Boxauto_Birne");
            var birnen = new List<Renderer>();
            for (int i = 0; i < T.birnen.Count; i++)
            {
                var go = Teil("Birne " + (i + 1), birneMesh, mats["Birne"], birnenRoot, false);
                go.transform.localPosition = T.birnen[i].position; go.transform.localRotation = T.birnen[i].rotation;
                birnen.Add(go.GetComponent<Renderer>());
            }

            var sw = new List<Light>();
            for (int i = 0; i < T.scheinwerfer.Count; i++)
            {
                var l = Licht("Scheinwerfer " + (i == 0 ? "links" : "rechts"), aufbau, LightType.Spot, new Color(1f, .9f, .75f), 3.2f, 9, 62, false);
                l.transform.localPosition = T.scheinwerfer[i].position; l.transform.localRotation = T.scheinwerfer[i].rotation * Quaternion.Euler(6, 0, 0);
                sw.Add(l);
                if (mats.TryGetValue("KegelAuto", out var km))
                {
                    var g = new GlattMesh(); g.BeamCone(Vector3.zero, Vector3.forward, .06f, .9f, 3.2f, 24);
                    var c = Teil("Lichtkegel", M(g, "Boxauto_Lichtkegel_" + i), km, l.transform, false);
                    c.GetComponent<Renderer>().receiveShadows = false;
                }
            }
            var rl = Licht("Ruecklicht-Schein", aufbau, LightType.Point, new Color(1f, .08f, .05f), .9f, 1.6f);
            rl.transform.localPosition = new Vector3(0, .5f, -1.3f);

            var ul = new List<Light>();
            foreach (var p in T.unterlicht) { var l = Licht("Unterbodenlicht", aufbau, LightType.Spot, new Color(.1f, .8f, 1f), 2.5f, 1.8f, 165f); l.transform.localPosition = p + Vector3.up * .06f; l.transform.localRotation = Quaternion.Euler(90, 0, 0); ul.Add(l); }

            // Funken am Stromabnehmer
            var sp = new GameObject("Funken"); sp.transform.SetParent(root.transform, false); sp.transform.localPosition = T.stangeOben;
            var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var mn = ps.main; mn.loop = false; mn.playOnAwake = false; mn.duration = 1;
            mn.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .55f); mn.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 5f);
            mn.startSize = new ParticleSystem.MinMaxCurve(.012f, .035f); mn.gravityModifier = 1.3f;
            mn.startColor = new ParticleSystem.MinMaxGradient(new Color(.75f, .85f, 1f), new Color(1f, .9f, .6f));
            mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = .05f; sh.rotation = new Vector3(90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .7f, .3f), .5f), new GradientColorKey(new Color(1f, .35f, .1f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .6f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .2f));
            var ebene = new GameObject("Funken-Boden").transform; ebene.SetParent(root.transform, false);
            var coll = ps.collision; coll.enabled = true; coll.type = ParticleSystemCollisionType.Planes; coll.SetPlane(0, ebene);
            coll.dampen = .5f; coll.bounce = .35f; coll.lifetimeLoss = .2f;
            var psr = sp.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Stretch; psr.velocityScale = .035f; psr.lengthScale = 2f;
            if (mats.TryGetValue("Funken", out var fm)) psr.sharedMaterial = fm;
            var fl = Licht("Funkenlicht", sp.transform, LightType.Point, new Color(.6f, .75f, 1f), 0, 6f); fl.enabled = false;

            var li = root.AddComponent<BoxautoLichter>();
            li.birnen = birnen.ToArray(); li.scheinwerfer = sw.ToArray(); li.unterLichter = ul.ToArray();
            R.TryGetValue("Scheinwerfer", out li.scheinwerferGlas); R.TryGetValue("Ruecklicht", out li.ruecklichtGlas);
            R.TryGetValue("Unterboden", out li.unterboden); R.TryGetValue("KontrolleGruen", out li.lampeGruen); R.TryGetValue("KontrolleGelb", out li.lampeGelb);
            li.funken = ps; li.funkenLicht = fl; li.lenkrad = lenk;

            MeshesSpeichern(ModDir + "/Boxauto_Meshes.asset");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Boxauto.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Mesh Mm(MeshBuilder b, string name) { var m = b.ToMesh(name); neueMeshes.Add(m); return m; }
    }
}
