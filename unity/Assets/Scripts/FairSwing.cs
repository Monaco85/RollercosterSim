using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    // Mittelalterliche Schiffsschaukel fuer die Fahrt „Schiffsschaukel“ (ersetzt dort das Piratenschiff).
    // Eichengerueste auf Steinsockeln, eiserne Achse und Arme, Kogge mit Kastellen, Wappenschilden und Kreuzsegel,
    // Fackeln, Feuerkoerbe, Banner, Palisade. Texturen: Poly Haven (CC0). Rein optisch, die Bewegung kommt aus PendelRide.
    public static partial class FairWorld
    {
        /// <summary>Drehpunkt der fahrbaren Schaukel (nur gesetzt, wenn die Schiffsschaukel geladen ist).</summary>
        public static Transform RideSwing;

        static readonly Color Oak = new Color(.46f, .33f, .22f), OakDark = new Color(.3f, .21f, .14f), Iron = new Color(.16f, .16f, .17f),
            Heraldic = new Color(.62f, .08f, .06f), GoldLeaf = new Color(.85f, .62f, .2f), Linen = new Color(.86f, .8f, .66f);

        static void MedievalSwing(Vector3 c, float H, float arm)
        {
            var oak = Tint(ta != null ? ta.woodStructure : null, Oak);
            var oakDark = Tint(ta != null ? ta.woodStructure : null, OakDark);
            var plank = Tint(ta != null ? ta.woodDeck : null, new Color(.62f, .45f, .3f));
            var stone = Tint(ta != null ? ta.stoneBlocks : null, new Color(.75f, .72f, .68f));
            var iron = Lit(Iron, .45f, .85f);
            var red = Lit(Heraldic, .35f); var gold = Lit(GoldLeaf, .6f, .8f);
            var linen = Tint(ta != null ? ta.canvas : null, Linen);

            var wood = new MeshBuilder { tile = 1.4f }; var dark = new MeshBuilder { tile = 1.4f };
            var st = new MeshBuilder { tile = 1.6f }; var fe = new MeshBuilder(); var ground = new MeshBuilder { tile = 1.8f };

            // ---- Boden: Pflaster und Strohstreu im Palisadenrund ----
            ground.Box(c + up * .02f, Quaternion.identity, new Vector3(28, .08f, 19));
            ground.ToObject("Schaukel-Pflaster", Tint(ta != null ? ta.cobble : null, new Color(.7f, .66f, .6f)), root).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // ---- Zwei A-Gerueste aus Eichenbalken auf Steinsockeln ----
            const float foot = 9f, zf = 4.4f;
            foreach (float z in new[] { -zf, zf })
            {
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector3 f = c + new Vector3(sx * foot, 0, z);
                    st.Box(f + up * .6f, Quaternion.identity, new Vector3(2.2f, 1.2f, 2.2f));
                    st.Box(f + up * 1.3f, Quaternion.identity, new Vector3(1.7f, .25f, 1.7f));
                    wood.Beam(f + up * 1.4f, c + new Vector3(sx * .55f, H - .2f, z), .72f, Vector3.forward);
                    // Strebe nach aussen gegen Umkippen in Schwingrichtung
                    dark.Beam(c + new Vector3(sx * foot * .55f, 6.6f, z), c + new Vector3(sx * (foot + 2.6f), .2f, z * 1.55f), .38f, Vector3.up);
                    st.Box(c + new Vector3(sx * (foot + 2.6f), .3f, z * 1.55f), Quaternion.identity, new Vector3(1.1f, .6f, 1.1f));
                    // Eisenbaender und Bolzen an den Fuessen
                    for (int k = 0; k < 2; k++)
                    {
                        float t = .08f + k * .06f;
                        Vector3 b = Vector3.Lerp(f + up * 1.4f, c + new Vector3(sx * .55f, H - .2f, z), t);
                        fe.Box(b, Quaternion.LookRotation(Vector3.forward, (c + new Vector3(0, H, z) - f).normalized), new Vector3(.82f, .12f, .82f));
                    }
                }
                // Riegel und Andreaskreuz
                dark.Beam(c + new Vector3(-foot * .72f, 4.2f, z), c + new Vector3(foot * .72f, 4.2f, z), .42f, Vector3.forward);
                dark.Beam(c + new Vector3(-foot * .38f, 9.4f, z), c + new Vector3(foot * .38f, 9.4f, z), .38f, Vector3.forward);
                dark.Beam(c + new Vector3(-foot * .7f, 4.4f, z), c + new Vector3(foot * .37f, 9.2f, z), .26f, Vector3.forward);
                dark.Beam(c + new Vector3(foot * .7f, 4.4f, z), c + new Vector3(-foot * .37f, 9.2f, z), .26f, Vector3.forward);
                // Lagerbock mit Eisenbeschlag oben
                wood.Box(c + new Vector3(0, H - .1f, z), Quaternion.identity, new Vector3(1.9f, 1.2f, 1.0f));
                fe.Box(c + new Vector3(0, H + .55f, z), Quaternion.identity, new Vector3(2.0f, .14f, 1.1f));
                foreach (float bx in new[] { -.7f, .7f }) foreach (float bz in new[] { -.35f, .35f })
                        fe.Cylinder(c + new Vector3(bx, H + .6f, z + bz), .07f, .12f, 6);
                // Bannerstange mit Wimpel oben auf dem Geruest
                dark.Cylinder(c + new Vector3(0, H + .6f, z), .09f, 4.2f, 8);
                fe.Sphere(c + new Vector3(0, H + 4.85f, z), .16f, 8);
                var flagMat = Lit(Heraldic, .2f); flagMat.SetFloat("_Cull", 0);
                FlagWave.Create(root, c + new Vector3(0, H + 4.7f, z), 2.6f, 1.2f, flagMat, z > 0 ? 200f : 160f);
            }
            // Eiserne Achse ueber beide Gerueste
            fe.Loft(new[] { c + new Vector3(0, H, -zf - .9f), c + new Vector3(0, H, zf + .9f) }, new[] { new Vector2(.32f, .32f), new Vector2(.32f, .32f) }, 1, 14, true, true, Vector3.up);

            // ---- Pendel: Arme und Kogge, alles am Drehpunkt ----
            var piv = new GameObject("Schiffsschaukel").transform; piv.SetParent(root, false); piv.position = c + up * H;
            RideSwing = piv;
            var pw = new MeshBuilder { tile = 1.2f }; var pd = new MeshBuilder { tile = 1.2f }; var pf = new MeshBuilder();
            var pr = new MeshBuilder(); var pg = new MeshBuilder(); var ps = new MeshBuilder();
            float by = -arm;   // Bootsboden
            // Arme: je Seite zwei eisenbeschlagene Holme zur Achse
            foreach (float z in new[] { -2.9f, 2.9f })
            {
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector3 a = new Vector3(sx * .25f, 0, z), b = new Vector3(sx * 4.6f, by + 2.2f, z * .62f);
                    pd.Beam(a, b, .34f, Vector3.forward);
                    for (int k = 1; k <= 4; k++) pf.Box(Vector3.Lerp(a, b, k / 5f), Quaternion.LookRotation(Vector3.forward, (a - b).normalized), new Vector3(.42f, .1f, .42f));
                }
                pd.Beam(new Vector3(-3.2f, by + 4.5f, z * .8f), new Vector3(3.2f, by + 4.5f, z * .8f), .26f, Vector3.forward);
                pf.Loft(new[] { new Vector3(0, 0, z - .35f), new Vector3(0, 0, z + .35f) }, new[] { new Vector2(.5f, .5f), new Vector2(.5f, .5f) }, 1, 12, true, true, Vector3.up);
            }
            // Rumpf der Kogge (hoch gezogene Enden), Planken quer
            var ctrl = new[] { new Vector3(-7.6f, by + 2.6f, 0), new Vector3(-6.2f, by + .9f, 0), new Vector3(-3, by + .15f, 0), new Vector3(0, by, 0), new Vector3(3, by + .15f, 0), new Vector3(6.2f, by + .9f, 0), new Vector3(7.6f, by + 2.6f, 0) };
            var rad = new[] { new Vector2(.5f, .9f), new Vector2(1.5f, 1.25f), new Vector2(2.0f, 1.3f), new Vector2(2.1f, 1.3f), new Vector2(2.0f, 1.3f), new Vector2(1.5f, 1.25f), new Vector2(.5f, .9f) };
            pw.Loft(ctrl, rad, 5, 22, true, true, Vector3.up);
            // Kiel und Steven
            pd.Loft(new[] { ctrl[0] + new Vector3(-.5f, .9f, 0), ctrl[1] + new Vector3(0, -1.1f, 0), ctrl[3] + new Vector3(0, -1.25f, 0), ctrl[5] + new Vector3(0, -1.1f, 0), ctrl[6] + new Vector3(.5f, .9f, 0) },
                new[] { new Vector2(.18f, .2f), new Vector2(.2f, .2f), new Vector2(.22f, .22f), new Vector2(.2f, .2f), new Vector2(.18f, .2f) }, 5, 8, true, true, Vector3.forward);
            // Reling
            var rail = new Vector3[ctrl.Length]; var railR = new Vector2[ctrl.Length];
            for (int i = 0; i < ctrl.Length; i++) { rail[i] = ctrl[i] + up * (1.25f - Mathf.Abs(i - 3) * .05f); railR[i] = new Vector2(rad[i].x + .06f, .1f); }
            pd.Loft(rail, railR, 5, 18, true, true, Vector3.up);
            // Deck und Baenke
            pw.Box(new Vector3(0, by + .55f, 0), Quaternion.identity, new Vector3(9.5f, .12f, 3.4f));
            for (int i = -3; i <= 3; i++) { if (i == 0) continue; pd.Box(new Vector3(i * 1.25f, by + 1.0f, 0), Quaternion.identity, new Vector3(.45f, .1f, 3.3f)); pd.Box(new Vector3(i * 1.25f, by + .78f, 0), Quaternion.identity, new Vector3(.3f, .45f, .3f)); }
            // Vorder- und Achterkastell mit Zinnen
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 k = new Vector3(sx * 6.0f, by + 2.3f, 0);
                pw.Box(k, Quaternion.identity, new Vector3(2.2f, .14f, 2.6f));
                for (int i = 0; i < 4; i++) foreach (float z in new[] { -1.25f, 1.25f }) pd.Box(k + new Vector3(-.85f + i * .57f, .45f, z), Quaternion.identity, new Vector3(.34f, .65f, .16f));
                for (int i = 0; i < 3; i++) pd.Box(k + new Vector3(sx * 1.05f, .45f, -.85f + i * .85f), Quaternion.identity, new Vector3(.16f, .65f, .34f));
                pd.Box(k + new Vector3(0, .1f, 0), Quaternion.identity, new Vector3(2.3f, .12f, 2.7f));
            }
            // Wappenschilde an beiden Bordwaenden (rot mit goldenem Balken)
            for (int i = -2; i <= 2; i++)
                foreach (float z in new[] { -1f, 1f })
                {
                    Vector3 sp = new Vector3(i * 1.55f, by + 1.05f, z * (2.12f - Mathf.Abs(i) * .05f));
                    var q = Quaternion.LookRotation(new Vector3(0, 0, z));
                    pr.Box(sp, q, new Vector3(.8f, 1.0f, .06f));
                    pg.Box(sp + new Vector3(0, 0, z * .04f), q, new Vector3(.82f, .16f, .04f));
                    pg.Box(sp + new Vector3(0, 0, z * .04f), q, new Vector3(.12f, 1.02f, .04f));
                }
            // Galionsfigur: Loewenkopf am Bug (+x)
            pg.Sphere(new Vector3(8.2f, by + 3.4f, 0), .42f, 10);
            pg.Spike(new Vector3(8.45f, by + 3.3f, 0), new Vector3(1, -.25f, 0), .26f, .55f, 7);
            pg.Spike(new Vector3(8.1f, by + 3.75f, 0), new Vector3(.3f, 1, 0), .14f, .45f, 6);
            // Fahnenmast auf dem Vorderkastell mit Wimpel (Mitte bleibt frei fuer den Blick der Fahrgaeste)
            pd.Cylinder(new Vector3(6.3f, by + 2.4f, 0), .1f, 3.4f, 8);
            pg.Sphere(new Vector3(6.3f, by + 5.85f, 0), .13f, 8);
            var pen = Lit(Heraldic, .2f); pen.SetFloat("_Cull", 0);
            FlagWave.Create(piv, piv.position + new Vector3(6.3f, by + 5.7f, 0), 1.6f, .7f, pen, 0f);
            // Gemaltes Segeltuch als Sonnendach ueber dem Achterkastell (Leinen mit rotem Kreuz)
            ps.Box(new Vector3(-6.0f, by + 3.75f, 0), Quaternion.identity, new Vector3(2.0f, .04f, 2.6f));
            pr.Box(new Vector3(-6.0f, by + 3.78f, 0), Quaternion.identity, new Vector3(2.0f, .03f, .4f));
            pr.Box(new Vector3(-6.0f, by + 3.78f, 0), Quaternion.identity, new Vector3(.4f, .03f, 2.6f));
            foreach (float sx in new[] { -6.9f, -5.1f }) foreach (float z in new[] { -1.2f, 1.2f }) pd.Cylinder(new Vector3(sx, by + 2.4f, z), .06f, 1.35f, 6);

            pw.ToObject("Kogge", plank, piv); pd.ToObject("Kogge Balken", oakDark, piv); pf.ToObject("Kogge Eisen", iron, piv);
            pr.ToObject("Wappen", red, piv); pg.ToObject("Blattgold", gold, piv);
            var sail = ps.ToObject("Segel", linen, piv, false); sail.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cull", 0);
            // Laternen am Boot (warmes Kerzenlicht, ohne eigene Lichtquelle)
            var lb = new BulbSet();
            foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -1.3f, 1.3f }) lb.Add(new Vector3(sx * 6.9f, by + 3.3f, z), .12f, Warm, BulbMode.Twinkle, 0, 1, .8f);
            for (int i = -3; i <= 3; i++) foreach (float z in new[] { -1f, 1f }) lb.Add(new Vector3(i * 1.1f, by + 1.42f, z * (2.2f - Mathf.Abs(i) * .1f)), .06f, Warm, BulbMode.Twinkle, i * .1f, 1, .5f);
            lb.Build("Bootslaternen", piv);
            PL(new Vector3(0, by + 4.5f, 0), new Color(1f, .7f, .4f), 14f, 1.8f, piv);
            PL(new Vector3(0, by + 1.9f, 0), new Color(1f, .66f, .38f), 9f, 2.2f, piv);   // Kerzenschein im Boot

            // ---- Palisade mit Eingang, Fackeln, Feuerkoerbe ----
            var stakes = new MeshBuilder { tile = 1.2f };
            int ns = 74;
            for (int i = 0; i < ns; i++)
            {
                float a = i / (float)ns * Mathf.PI * 2;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * 14.5f, 0, Mathf.Sin(a) * 10.2f);
                if (Mathf.Sin(a) < -.93f) continue;   // Eingang zur Kamera-Seite (-z)
                float h = 2.1f + Mathf.PerlinNoise(i * .7f, 3) * .5f;
                stakes.Cylinder(p, .17f, h, 7);
                stakes.Cone(p + up * h, .17f, .35f, 7);
            }
            for (int i = 0; i < ns; i += 2)
            {
                float a = i / (float)ns * Mathf.PI * 2, a2 = (i + 2) / (float)ns * Mathf.PI * 2;
                if (Mathf.Sin(a) < -.93f || Mathf.Sin(a2) < -.93f) continue;
                stakes.Beam(c + new Vector3(Mathf.Cos(a) * 14.35f, 1.4f, Mathf.Sin(a) * 10.05f), c + new Vector3(Mathf.Cos(a2) * 14.35f, 1.4f, Mathf.Sin(a2) * 10.05f), .12f, Vector3.up);
            }
            stakes.ToObject("Palisade", oakDark, root);
            // Torbogen am Eingang mit Schild
            Vector3 g0 = c + new Vector3(0, 0, -10.4f);
            foreach (float sx in new[] { -2.3f, 2.3f }) wood.Beam(g0 + new Vector3(sx, 0, 0), g0 + new Vector3(sx, 4.6f, 0), .4f, Vector3.forward);
            wood.Beam(g0 + new Vector3(-2.8f, 4.4f, 0), g0 + new Vector3(2.8f, 4.4f, 0), .42f, Vector3.forward);
            dark.Box(g0 + new Vector3(0, 3.55f, -.25f), Quaternion.identity, new Vector3(4.2f, .9f, .1f));
            Neon("SCHIFFSSCHAUKEL", g0 + new Vector3(0, 3.55f, -.32f), 0f, .11f, new Color(1f, .72f, .35f), 0, null, 2.2f);
            Fx.Torch(root, g0 + new Vector3(-2.55f, 3.2f, -.2f), Vector3.back, oak, iron, ta != null ? ta.fire : null, true);
            Fx.Torch(root, g0 + new Vector3(2.55f, 3.2f, -.2f), Vector3.back, oak, iron, ta != null ? ta.fire : null, false);
            // Fackeln an den Gerueststaendern
            foreach (float z in new[] { -zf, zf }) foreach (float sx in new[] { -1f, 1f })
                {
                    Vector3 leg = Vector3.Lerp(c + new Vector3(sx * foot, 1.4f, z), c + new Vector3(sx * .55f, H - .2f, z), .22f);
                    Fx.Torch(root, leg + new Vector3(0, 0, Mathf.Sign(z) * .38f), new Vector3(0, 0, Mathf.Sign(z)), oak, iron, ta != null ? ta.fire : null, sx > 0);
                }
            // Feuerkoerbe an vier Ecken
            foreach (var o in new[] { new Vector3(-12, 0, -7), new Vector3(12, 0, -7), new Vector3(-12, 0, 7), new Vector3(12, 0, 7) })
            {
                Vector3 p = c + o;
                fe.Cylinder(p, .06f, 1.1f, 6); fe.Cone(p + up * 1.05f, .5f, .45f, 10, 0);
                foreach (float a in new[] { 0f, 2.1f, 4.2f }) fe.Beam(p + new Vector3(Mathf.Cos(a) * .55f, 0, Mathf.Sin(a) * .55f), p + up * .9f, .06f, Vector3.up);
                Fx.Fire(root, p + up * 1.5f, ta != null ? ta.fire : null, 1.8f);
                var l = PL(p + up * 2.2f, new Color(1f, .58f, .25f), 11f, 2.6f); l.gameObject.AddComponent<Flicker>().baseIntensity = 2.6f;
            }
            // Kasse: kleine Holzbude mit Strohdach
            {
                Vector3 k = c + new Vector3(-6.5f, 0, -11.2f);
                wood.Box(k + up * 1.2f, Quaternion.identity, new Vector3(2.6f, 2.4f, 1.8f));
                var th = new MeshBuilder { tile = 1.2f }; th.Gable(k + up * 2.4f, Quaternion.Euler(0, 90, 0), 2.2f, 3.0f, 1.2f, .35f);
                th.ToObject("Kassendach", ta != null ? ta.roofThatch : Lit(new Color(.6f, .5f, .3f)), root);
                dark.Box(k + new Vector3(0, 1.3f, -.92f), Quaternion.identity, new Vector3(1.4f, .8f, .06f));
            }

            wood.ToObject("Schaukelgeruest", oak, root); dark.ToObject("Schaukelstreben", oakDark, root);
            st.ToObject("Steinsockel", stone, root); fe.ToObject("Eisenbeschlag", iron, root);
        }
    }
}
