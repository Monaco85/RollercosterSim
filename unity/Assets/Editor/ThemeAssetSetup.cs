using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Achterbahn.EditorTools
{
    /// <summary>Import-Einstellungen fuer Poly-Haven-Dateien und Aufbau von Resources/ThemeAssets.asset.</summary>
    public class PolyHavenImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            bool longClip = assetPath.Contains("rummel_nacht") || assetPath.Contains("drehorgel") || assetPath.Contains("autoscooter") || assetPath.Contains("feuerwerk");
            s.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .6f;
            ai.defaultSampleSettings = s; ai.loadInBackground = true;
        }

        void OnPreprocessTexture()
        {
            if (assetPath.Contains("/AmbientCG/"))
            {
                var t = (TextureImporter)assetImporter;
                if (assetPath.Contains("_NormalGL")) t.textureType = TextureImporterType.NormalMap;
                if (!assetPath.Contains("_Color")) t.sRGBTexture = assetPath.Contains("_NormalGL") ? t.sRGBTexture : false;
                t.maxTextureSize = 2048;
                return;
            }
            if (!assetPath.Contains("/PolyHaven/")) return;
            var ti = (TextureImporter)assetImporter;
            string f = Path.GetFileName(assetPath).ToLowerInvariant();
            if (f.Contains("_nor_gl") || f.Contains("_nor_")) ti.textureType = TextureImporterType.NormalMap;
            if (f.EndsWith(".hdr")) { ti.maxTextureSize = 4096; ti.mipmapEnabled = false; ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp; }
            else ti.maxTextureSize = 2048;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.Contains("/PolyHaven/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = false;
        }

        // Blaetter und Nadeln: Alpha-Ausschnitt statt undurchsichtiger Flaechen
        void OnPostprocessMaterial(Material m)
        {
            if (!assetPath.Contains("/PolyHaven/Models/")) return;
            m.enableInstancing = true;
            var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            string tn = (tex != null ? tex.name : "") + " " + m.name;
            tn = tn.ToLowerInvariant();
            if (tn.Contains("leaf") || tn.Contains("leaves") || tn.Contains("needle") || tn.Contains("branch") || tn.Contains("twig") || tn.Contains("foliage") || tn.Contains("shrub"))
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .45f); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
        }
    }

    public static class ThemeAssetSetup
    {
        const string Root = "Assets/PolyHaven";
        const string Out = "Assets/Resources/ThemeAssets.asset";

        [MenuItem("Achterbahn/Assets einrichten")]
        public static void Setup()
        {
            Directory.CreateDirectory(Root + "/Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var ta = AssetDatabase.LoadAssetAtPath<ThemeAssets>(Out);
            if (ta == null) { ta = ScriptableObject.CreateInstance<ThemeAssets>(); AssetDatabase.CreateAsset(ta, Out); }

            ta.sky = SkyMat();
            ta.grass = Mat("aerial_grass_rock", .05f) ?? Mat("leafy_grass", .1f);
            ta.path = Mat("stony_dirt_path", .1f);
            ta.cobble = Mat("cobblestone_floor_01", .2f);
            ta.castleWall = Mat("castle_wall_slates", .15f);
            ta.stoneBlocks = Mat("medieval_blocks_02", .15f);
            ta.timberWall = Mat("beam_wall_01", .1f);
            ta.roofSlate = Mat("red_slate_roof_tiles_01", .3f);
            ta.roofThatch = Mat("thatch_roof_angled", .05f);
            ta.woodStructure = Mat("weathered_planks", .1f);
            ta.woodDeck = Mat("wood_planks", .15f);
            ta.treeBark = TexMat("fir_bark", Root + "/TreeParts/fir_tree_01/fir_tree_01_bark_diff_2k.png", Root + "/TreeParts/fir_tree_01/fir_tree_01_bark_nor_gl_2k.png", .1f, false);
            ta.treeBranch = TexMat("fir_branch", BranchTexture(), null, .08f, true);
            ta.layers = new[]
            {
                Layer("aerial_grass_rock", 14f, new Vector4(.66f, .92f, .5f, 1)), Layer("leafy_grass", 5f, new Vector4(.62f, .9f, .45f, 1)),
                Layer("stony_dirt_path", 4f, new Vector4(.9f, .88f, .82f, 1)), Layer("forrest_ground_01", 4f, new Vector4(.75f, .9f, .65f, 1)),
                Layer("aerial_ground_rock", 18f, new Vector4(.5f, .46f, .43f, 1)),
            };
            ta.terrain = TerrainMat();
            ta.grassCard = TexMat("grass_card", GrassTexture(), null, .05f, true);
            ta.smoke = SmokeMat();
            ta.fire = FireMat();
            var skin = SkinTextures();
            ta.dinoSkin = TexMat("dino_skin", skin.Item1, skin.Item2, .32f, false);
            if (ta.dinoSkin != null) ta.dinoSkin.SetFloat("_BumpScale", 1.6f);
            ta.palmBark = Mat("palm_bark", .1f);
            ta.rockWall = Mat("rock_wall_10", .12f);
            ta.frond = TexMat("frond", FrondTexture(), null, .15f, true);
            ta.leaves = TexMat("jungle_leaves", LeavesTexture(), null, .25f, true);
            ta.water = WaterMat();
            ta.conifers = new GameObject[0];
            ta.broadleaf = new GameObject[0];
            ta.shrubs = Models("shrub_02", "shrub_04");
            ta.rocks = Models("boulder_01", "rock_moss_set_01");
            ta.props = Models("wooden_barrels_01", "wooden_crate_01");
            ta.castleDoor = Models("large_castle_door").FirstOrDefault();
            ta.lantern = Models("wooden_lantern_01").FirstOrDefault();
            FairSetup(ta);
            EditorUtility.SetDirty(ta);
            AssetDatabase.SaveAssets();
            Debug.Log("Achterbahn: ThemeAssets eingerichtet (" + ta.conifers.Length + " Nadelbaeume, " + ta.broadleaf.Length + " Laubbaeume)");
        }

        // ---------------- Rummel: Daemmerung, ambientCG, Lichter, Menschen, Geraeusche ----------------
        static void FairSetup(ThemeAssets ta)
        {
            ta.duskSky = SkyMat("qwantani_dusk_2_puresky", "sky_dusk", .4f, 205f);
            ta.asphalt = AcgMat("Asphalt031", .55f, 1f);
            ta.canvas = AcgMat("Fabric066", .35f, 1f);
            ta.paintedWood = AcgMat("PaintedWood009C", .6f, 1f);
            ta.corrugated = AcgMat("CorrugatedSteel005", .8f, 1f);
            ta.lacquer = AcgMat("Plastic010", .95f, .6f);
            ta.bulb = ShaderMat("bulb", "Achterbahn/Bulb", null);
            ta.halo = ShaderMat("bulb_halo", "Achterbahn/BulbHalo", null);
            ta.beam = ShaderMat("light_beam", "Achterbahn/LightBeam", null);
            ta.neon = ShaderMat("neon_text", "Achterbahn/NeonText", null);
            ta.spark = ShaderMat("spark", "Achterbahn/Additive", SparkTexture());
            ta.crowd = ShaderMat("crowd", "Achterbahn/Crowd", null);
            ta.people = Directory.GetFiles("Assets/Quaternius/People", "*.glb").OrderBy(f => f)
                .Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).Where(g => g != null).ToArray();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + n + ".mp3");
            ta.fairAmbience = A("rummel_nacht"); ta.organ = A("drehorgel"); ta.carouselMusic = A("karussell_musik");
            ta.chainLift = A("kettenlift"); ta.screams = A("schreie"); ta.bumperCars = A("autoscooter");
            ta.fireworks = A("feuerwerk"); ta.bangBig = A("knall_gross"); ta.bang = A("knall"); ta.coasterPass = A("achterbahn_vorbei");
        }

        static Material ShaderMat(string name, string shader, Texture tex)
        {
            string p = Root + "/Materials/" + name + ".mat";
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogWarning("Shader fehlt: " + shader); return null; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
            m.shader = sh;
            if (tex != null) m.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ambientCG-Material: Farbe, Normalen und Glaette (aus Rauheit, ggf. Metall) in eine Maske gepackt
        static Material AcgMat(string id, float smooth, float bump)
        {
            string dir = "Assets/AmbientCG/" + id, pre = $"{dir}/{id}_2K-JPG_";
            var dif = AssetDatabase.LoadAssetAtPath<Texture2D>(pre + "Color.jpg");
            if (dif == null) { Debug.LogWarning("Textur fehlt: " + id); return null; }
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(pre + "NormalGL.jpg");
            string mask = "Assets/Generated/" + id + "_mask.png";
            if (!File.Exists(mask) && File.Exists(pre + "Roughness.jpg"))
            {
                Directory.CreateDirectory("Assets/Generated");
                var r = new Texture2D(2, 2); r.LoadImage(File.ReadAllBytes(pre + "Roughness.jpg"));
                Texture2D met = null; if (File.Exists(pre + "Metalness.jpg")) { met = new Texture2D(2, 2); met.LoadImage(File.ReadAllBytes(pre + "Metalness.jpg")); }
                Texture2D ao = null; if (File.Exists(pre + "AmbientOcclusion.jpg")) { ao = new Texture2D(2, 2); ao.LoadImage(File.ReadAllBytes(pre + "AmbientOcclusion.jpg")); }
                int w = r.width / 2, h = r.height / 2;   // 1K reicht fuer die Maske
                var o = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float u = (x + .5f) / w, v = (y + .5f) / h;
                    float rough = r.GetPixelBilinear(u, v).r;
                    px[y * w + x] = new Color(met != null ? met.GetPixelBilinear(u, v).r : 0f, ao != null ? ao.GetPixelBilinear(u, v).r : 1f, 0, 1f - rough);
                }
                o.SetPixels(px); o.Apply();
                File.WriteAllBytes(mask, o.EncodeToPNG());
                AssetDatabase.ImportAsset(mask);
                var ti = (TextureImporter)AssetImporter.GetAtPath(mask); ti.sRGBTexture = false; ti.SaveAndReimport();
            }
            string p = Root + "/Materials/acg_" + id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", dif); m.SetColor("_BaseColor", Color.white);
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); }
            var mt = AssetDatabase.LoadAssetAtPath<Texture2D>(mask);
            if (mt != null)
            {
                m.SetTexture("_MetallicGlossMap", mt); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetTexture("_OcclusionMap", mt); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_OcclusionStrength", .8f);
                m.SetFloat("_SmoothnessTextureChannel", 0);
            }
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // Weicher runder Leuchtpunkt fuer Funken und Feuerwerk
        static Texture2D SparkTexture()
        {
            const string path = "Assets/Generated/spark.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory("Assets/Generated");
                const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x + .5f) / S * 2 - 1, dy = (y + .5f) / S * 2 - 1, r2 = dx * dx + dy * dy;
                    float a = Mathf.Clamp01(Mathf.Exp(-r2 * 5f) * (1 - r2) + Mathf.Exp(-r2 * 40f));
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
                }
                t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material SkyMat() => SkyMat("kloofendal_48d_partly_cloudy_puresky", "sky", 1f, 0f);

        static Material SkyMat(string hdrName, string matName, float exposure, float rotation)
        {
            string hdr = Directory.GetFiles(Root + "/HDRI", hdrName + "*.hdr").FirstOrDefault();
            if (hdr == null) return null;
            string p = Root + "/Materials/" + matName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(hdr.Replace('\\', '/')));
            m.SetFloat("_Mapping", 1); m.SetFloat("_ImageType", 0); m.SetFloat("_Exposure", exposure); m.SetFloat("_Rotation", rotation);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Mat(string id, float smooth)
        {
            string dir = Root + "/Textures/" + id;
            var dif = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_diffuse_2k.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_nor_gl_2k.jpg");
            if (dif == null) { Debug.LogWarning("Textur fehlt: " + id); return null; }
            string p = Root + "/Materials/" + id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", dif); m.SetColor("_BaseColor", Color.white);
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material TexMat(string name, string difPath, string norPath, float smooth, bool cutout)
        {
            var dif = AssetDatabase.LoadAssetAtPath<Texture2D>(difPath);
            if (dif == null) { Debug.LogWarning("Textur fehlt: " + difPath); return null; }
            string p = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", dif); m.SetColor("_BaseColor", Color.white);
            var nor = norPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(norPath) : null;
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0);
            if (cutout)
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest; m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // Eigene Tannenzweig-Textur mit Alpha (der Poly-Haven-Zweigatlas ist undurchsichtig). Farben aus dem Fichten-Scan.
        static string BranchTexture()
        {
            const string path = "Assets/Generated/fir_branch.png";
            if (File.Exists(path)) return path;
            Directory.CreateDirectory("Assets/Generated");
            const int S = 512;
            var col = new Color[S * S];
            var bg = new Color(.22f, .30f, .13f, 0f);
            for (int i = 0; i < col.Length; i++) col[i] = bg;
            var rnd = new System.Random(7);
            Color[] needle = { new Color(.13f, .21f, .09f), new Color(.19f, .29f, .12f), new Color(.27f, .37f, .17f), new Color(.33f, .43f, .21f) };
            Color stem = new Color(.30f, .22f, .14f);
            void Dot(float x, float y, float r, Color c)
            {
                int x0 = Mathf.Max(0, (int)(x - r - 1)), x1 = Mathf.Min(S - 1, (int)(x + r + 1)), y0 = Mathf.Max(0, (int)(y - r - 1)), y1 = Mathf.Min(S - 1, (int)(y + r + 1));
                for (int yy = y0; yy <= y1; yy++) for (int xx = x0; xx <= x1; xx++)
                {
                    float d = Mathf.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)); float a = Mathf.Clamp01(r - d + .5f);
                    if (a <= 0) continue;
                    int k = yy * S + xx; var o = col[k];
                    col[k] = new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), Mathf.Max(o.a, a));
                }
            }
            void Line(Vector2 a, Vector2 b, float r, Color c) { int n = Mathf.CeilToInt((b - a).magnitude * 1.5f); for (int i = 0; i <= n; i++) { var p = Vector2.Lerp(a, b, i / (float)n); Dot(p.x, p.y, r, c); } }
            void Needles(Vector2 a, Vector2 b, float len)
            {
                Vector2 d = (b - a).normalized; float L = (b - a).magnitude;
                for (float s = 2; s < L; s += 3.2f)
                    foreach (int side in new[] { -1, 1 })
                    {
                        float ang = side * (55 + (float)rnd.NextDouble() * 20) * Mathf.Deg2Rad;
                        Vector2 nd = new Vector2(d.x * Mathf.Cos(ang) - d.y * Mathf.Sin(ang), d.x * Mathf.Sin(ang) + d.y * Mathf.Cos(ang));
                        Vector2 p = a + d * s; float l = len * (0.75f + (float)rnd.NextDouble() * .5f) * Mathf.Lerp(1f, .55f, s / L);
                        Line(p, p + nd * l, 1.1f, needle[rnd.Next(needle.Length)]);
                    }
            }
            Vector2 baseP = new Vector2(S / 2f, 4), tipP = new Vector2(S / 2f, S - 14);
            for (float y = 30; y < S - 40; y += 26)
                foreach (int side in new[] { -1, 1 })
                {
                    float f = y / S; float len = Mathf.Lerp(190, 50, f) * (.85f + (float)rnd.NextDouble() * .3f);
                    float ang = (38 + (float)rnd.NextDouble() * 14) * Mathf.Deg2Rad;
                    Vector2 a = new Vector2(S / 2f, y), b = a + new Vector2(side * Mathf.Sin(ang), Mathf.Cos(ang)) * len;
                    b.x = Mathf.Clamp(b.x, 8, S - 8); b.y = Mathf.Min(b.y, S - 8);
                    Needles(a, b, 17);
                    Line(a, b, 1.6f, stem);
                }
            Needles(baseP, tipP, 20);
            Line(baseP, tipP, 2.6f, stem);
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels(col); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return path;
        }

        static TerrainLayer Layer(string id, float tile, Vector4 tint)
        {
            string dir = Root + "/Textures/" + id;
            var dif = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_diffuse_2k.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_nor_gl_2k.jpg");
            string p = Root + "/Materials/" + id + ".terrainlayer";
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
            if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, p); }
            l.diffuseTexture = dif; l.normalMapTexture = nor; l.tileSize = new Vector2(tile, tile); l.normalScale = 1f;
            l.smoothness = 0; l.metallic = 0;
            l.diffuseRemapMin = Vector4.zero; l.diffuseRemapMax = tint;   // Farbton (Gelaende-Shader multipliziert damit)
            EditorUtility.SetDirty(l);
            return l;
        }

        static Material TerrainMat()
        {
            string p = Root + "/Materials/terrain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.EnableKeyword("_NORMALMAP");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // Rauchwolke: weicher, leicht wolkiger Kreis mit Alpha
        static Material SmokeMat()
        {
            const string tp = "Assets/Generated/smoke_puff.png";
            if (!File.Exists(tp))
            {
                Directory.CreateDirectory("Assets/Generated");
                const int S = 128; var c = new Color[S * S];
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2f) / (S / 2f), dy = (y - S / 2f) / (S / 2f), r = Mathf.Sqrt(dx * dx + dy * dy);
                    float n = Mathf.PerlinNoise(x * .09f, y * .09f) * .6f + Mathf.PerlinNoise(x * .2f + 7, y * .2f + 3) * .4f;
                    float a = Mathf.Clamp01(1 - r) ; a = a * a * (0.55f + .45f * n);
                    c[y * S + x] = new Color(1, 1, 1, a);
                }
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false); t.SetPixels(c); t.Apply();
                File.WriteAllBytes(tp, t.EncodeToPNG()); AssetDatabase.ImportAsset(tp);
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            }
            string p = Root + "/Materials/smoke.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
            m.SetColor("_BaseColor", new Color(.78f, .78f, .76f, .55f));
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }

        // Reptilienhaut: kachelbare Voronoi-Schuppen. U = Laenge, V = einmal um den Koerper (V 0 = Bauch hell, V 0,5 = Ruecken dunkel, mit Streifen).
        static (string, string) SkinTextures()
        {
            const string ap = "Assets/Generated/dino_skin.png", np = "Assets/Generated/dino_skin_n.png";
            if (File.Exists(ap) && File.Exists(np)) return (ap, np);
            Directory.CreateDirectory("Assets/Generated");
            const int S = 512; var rnd = new System.Random(3);
            int nPts = 340; var px = new Vector2[nPts]; var shade = new float[nPts];
            for (int i = 0; i < nPts; i++) { px[i] = new Vector2((float)rnd.NextDouble() * S, (float)rnd.NextDouble() * S); shade[i] = (float)rnd.NextDouble(); }
            var height = new float[S * S]; var col = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float d1 = 1e9f, d2 = 1e9f; int best = 0;
                for (int i = 0; i < nPts; i++)
                {
                    float dx = Mathf.Abs(x - px[i].x), dy = Mathf.Abs(y - px[i].y);
                    if (dx > S / 2) dx = S - dx; if (dy > S / 2) dy = S - dy;
                    float d = dx * dx + dy * dy;
                    if (d < d1) { d2 = d1; d1 = d; best = i; } else if (d < d2) d2 = d;
                }
                d1 = Mathf.Sqrt(d1); d2 = Mathf.Sqrt(d2);
                float edge = d2 - d1;
                float dome = Mathf.Clamp01(edge / 7f); dome = Mathf.Sqrt(dome);
                height[y * S + x] = dome;
                float v = y / (float)S, u = x / (float)S;
                float back = .5f - .5f * Mathf.Cos(2 * Mathf.PI * v);
                float stripes = Mathf.Clamp01(Mathf.Sin(2 * Mathf.PI * (u * 4 + .35f * Mathf.Sin(2 * Mathf.PI * v * 2))) * 2.2f - .6f) * Mathf.SmoothStep(.35f, .9f, back);
                float tone = Mathf.Lerp(.86f, .5f, back) * (.88f + .2f * shade[best]) * (1 - .32f * stripes) * Mathf.Lerp(.55f, 1f, dome);
                col[y * S + x] = new Color(tone * 1.02f, tone, tone * .9f, 1);
            }
            var nrm = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float hl = height[y * S + (x + S - 1) % S], hr = height[y * S + (x + 1) % S], hd = height[((y + S - 1) % S) * S + x], hu = height[((y + 1) % S) * S + x];
                Vector3 n = new Vector3((hl - hr) * 2.5f, (hd - hu) * 2.5f, 1).normalized;
                nrm[y * S + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
            }
            var ta = new Texture2D(S, S, TextureFormat.RGBA32, false); ta.SetPixels(col); ta.Apply(); File.WriteAllBytes(ap, ta.EncodeToPNG());
            var tn = new Texture2D(S, S, TextureFormat.RGBA32, false, true); tn.SetPixels(nrm); tn.Apply(); File.WriteAllBytes(np, tn.EncodeToPNG());
            AssetDatabase.ImportAsset(ap); AssetDatabase.ImportAsset(np);
            var ti = (TextureImporter)AssetImporter.GetAtPath(np); ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport();
            return (ap, np);
        }

        // Farn- bzw. Palmwedel: Mittelrippe mit gefiederten Blaettchen, Alpha
        static string FrondTexture()
        {
            const string path = "Assets/Generated/frond.png";
            if (File.Exists(path)) return path;
            Directory.CreateDirectory("Assets/Generated");
            const int W = 256, H = 512; var col = new Color[W * H];
            for (int i = 0; i < col.Length; i++) col[i] = new Color(.2f, .32f, .1f, 0);
            var rnd = new System.Random(21);
            void Dot(float x, float y, float r, Color c)
            {
                for (int yy = (int)(y - r - 1); yy <= (int)(y + r + 1); yy++) for (int xx = (int)(x - r - 1); xx <= (int)(x + r + 1); xx++)
                {
                    if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)) + .5f); if (a <= 0) continue;
                    int k = yy * W + xx; var o = col[k];
                    col[k] = new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), Mathf.Max(o.a, a));
                }
            }
            for (int y = 8; y < H - 6; y += 7)
            {
                float f = y / (float)H, len = Mathf.Sin(Mathf.PI * Mathf.Clamp01(f * 1.05f)) * 115f * (.9f + (float)rnd.NextDouble() * .2f);
                Color c = Color.Lerp(new Color(.16f, .3f, .08f), new Color(.34f, .5f, .16f), (float)rnd.NextDouble());
                foreach (int side in new[] { -1, 1 })
                    for (float s = 0; s < len; s += 1.2f)
                    {
                        float t = s / len, x = W / 2f + side * s, yy = y + s * .55f + t * t * 10f, r = Mathf.Lerp(3.2f, .8f, t);
                        Dot(x, yy, r, Color.Lerp(c, c * 1.25f, t));
                    }
            }
            for (int y = 0; y < H - 4; y++) Dot(W / 2f, y, Mathf.Lerp(3f, 1f, y / (float)H), new Color(.36f, .4f, .18f));
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.SetPixels(col); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG()); AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return path;
        }

        // Wellen-Normalkarte: Summe vieler Sinuswellen mit ganzzahligen Wellenvektoren -> nahtlos kachelbar
        static Material WaterMat()
        {
            const string np = "Assets/Generated/water_normal.png";
            if (!File.Exists(np))
            {
                Directory.CreateDirectory("Assets/Generated");
                const int S = 512; var rnd = new System.Random(9);
                int nw = 48; var kx = new int[nw]; var ky = new int[nw]; var amp = new float[nw]; var ph = new float[nw];
                for (int i = 0; i < nw; i++)
                {
                    float ang = (float)rnd.NextDouble() * Mathf.PI * 2, f = 2 + (float)rnd.NextDouble() * 14;
                    kx[i] = Mathf.RoundToInt(Mathf.Cos(ang) * f); ky[i] = Mathf.RoundToInt(Mathf.Sin(ang) * f);
                    if (kx[i] == 0 && ky[i] == 0) kx[i] = 1;
                    amp[i] = 1f / Mathf.Pow(Mathf.Sqrt(kx[i] * kx[i] + ky[i] * ky[i]), 1.3f); ph[i] = (float)rnd.NextDouble() * 6.28f;
                }
                var c = new Color[S * S];
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S, dx = 0, dy = 0;
                    for (int i = 0; i < nw; i++)
                    {
                        float a = 2 * Mathf.PI * (kx[i] * u + ky[i] * v) + ph[i], cs = Mathf.Cos(a) * amp[i] * 2 * Mathf.PI;
                        dx += cs * kx[i]; dy += cs * ky[i];
                    }
                    Vector3 n = new Vector3(-dx * .012f, -dy * .012f, 1).normalized;
                    c[y * S + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                }
                var t = new Texture2D(S, S, TextureFormat.RGBA32, true, true); t.SetPixels(c); t.Apply();
                File.WriteAllBytes(np, t.EncodeToPNG()); AssetDatabase.ImportAsset(np);
                var ti = (TextureImporter)AssetImporter.GetAtPath(np); ti.textureType = TextureImporterType.NormalMap; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
            }
            string p = Root + "/Materials/water.mat";
            var sh = Shader.Find("Achterbahn/Water");
            if (sh == null) { Debug.LogWarning("Wasser-Shader fehlt"); return null; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
            m.shader = sh;
            m.SetTexture("_NormalTex", AssetDatabase.LoadAssetAtPath<Texture2D>(np));
            m.SetFloat("_NormalScale", .42f); m.SetFloat("_Tiling", .028f); m.SetFloat("_Speed", .016f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Urwald-Blaetter: viele ovale Blaetter mit Mittelrippe in verschiedenen Gruentoenen, Alpha
        static string LeavesTexture()
        {
            const string path = "Assets/Generated/jungle_leaves.png";
            if (File.Exists(path)) return path;
            Directory.CreateDirectory("Assets/Generated");
            const int S = 512; var col = new Color[S * S];
            for (int i = 0; i < col.Length; i++) col[i] = new Color(.18f, .3f, .1f, 0);
            var rnd = new System.Random(33);
            for (int k = 0; k < 95; k++)
            {
                float cx = 30 + (float)rnd.NextDouble() * (S - 60), cy = 30 + (float)rnd.NextDouble() * (S - 60);
                float len = 34 + (float)rnd.NextDouble() * 46, wid = len * (.28f + (float)rnd.NextDouble() * .14f), ang = (float)rnd.NextDouble() * Mathf.PI * 2;
                Color baseC = Color.Lerp(new Color(.1f, .24f, .06f), new Color(.3f, .46f, .12f), (float)rnd.NextDouble());
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                int r = (int)len + 2;
                for (int y = (int)cy - r; y <= cy + r; y++) for (int x = (int)cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    float lx = (x - cx) * ca + (y - cy) * sa, ly = -(x - cx) * sa + (y - cy) * ca;
                    float e = (lx * lx) / (len * len) + (ly * ly) / (wid * wid * (1 - Mathf.Abs(lx) / (len * 1.15f)));
                    if (e > 1) continue;
                    float a = Mathf.Clamp01((1 - e) * 6f);
                    float rib = Mathf.Abs(ly) < 1.3f ? .25f : 0;
                    float shade = .8f + .25f * (ly / wid) + .1f * Mathf.Sin(lx * .5f);
                    Color cc = baseC * shade + new Color(rib, rib, rib * .5f) * .6f;
                    int i2 = y * S + x; var o = col[i2];
                    col[i2] = new Color(Mathf.Lerp(o.r, cc.r, a), Mathf.Lerp(o.g, cc.g, a), Mathf.Lerp(o.b, cc.b, a), Mathf.Max(o.a, a));
                }
            }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); tex.SetPixels(col); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG()); AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return path;
        }

        static Material FireMat()
        {
            string p = Root + "/Materials/fire.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/smoke_puff.png"));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2);   // additiv
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }

        // Grasbueschel: einzelne gebogene Halme mit Alpha, unten dunkler, Spitzen heller
        static string GrassTexture()
        {
            const string path = "Assets/Generated/grass_blades.png";
            if (File.Exists(path)) return path;
            Directory.CreateDirectory("Assets/Generated");
            const int S = 256; var col = new Color[S * S];
            for (int i = 0; i < col.Length; i++) col[i] = new Color(.3f, .42f, .16f, 0);
            var rnd = new System.Random(11);
            for (int b = 0; b < 70; b++)
            {
                float x0 = 12 + (float)rnd.NextDouble() * (S - 24), h = S * (.45f + (float)rnd.NextDouble() * .5f), bend = ((float)rnd.NextDouble() - .5f) * 60f;
                float w0 = 3.2f + (float)rnd.NextDouble() * 2.2f;
                Color baseC = Color.Lerp(new Color(.16f, .26f, .08f), new Color(.30f, .40f, .13f), (float)rnd.NextDouble());
                Color tipC = Color.Lerp(new Color(.45f, .55f, .22f), new Color(.62f, .62f, .30f), (float)rnd.NextDouble());
                int steps = (int)h;
                for (int s = 0; s < steps; s++)
                {
                    float f = s / h, x = x0 + bend * f * f, y = s, w = w0 * (1 - f) + .6f;
                    Color cc = Color.Lerp(baseC, tipC, f);
                    for (int xx = (int)(x - w - 1); xx <= (int)(x + w + 1); xx++)
                    {
                        if (xx < 0 || xx >= S || y >= S) continue;
                        float a = Mathf.Clamp01(w - Mathf.Abs(xx - x) + .5f);
                        if (a <= 0) continue;
                        int k = (int)y * S + xx; var o = col[k];
                        col[k] = new Color(Mathf.Lerp(o.r, cc.r, a), Mathf.Lerp(o.g, cc.g, a), Mathf.Lerp(o.b, cc.b, a), Mathf.Max(o.a, a));
                    }
                }
            }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); tex.SetPixels(col); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG()); AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return path;
        }

        static GameObject[] Models(params string[] ids) =>
            ids.Select(id => AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{id}/{id}.fbx")).Where(g => g != null).ToArray();
    }
}
