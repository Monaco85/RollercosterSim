using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Dinosaurier aus geformten Roehren entlang einer Wirbelsaeule. Lokale Achsen: x = vorne, y = oben, z = links/rechts.
    /// Masse orientieren sich an den echten Tieren (T. rex ca. 12 m, Brachiosaurus ca. 22 m, Triceratops ca. 8 m).
    /// </summary>
    public static class Creatures
    {
        static Vector3 V(float x, float y, float z = 0) => new Vector3(x, y, z);
        static Vector2 R(float w, float h) => new Vector2(w, h);

        static Material Lit(Color c, float smooth, float metal = 0)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            return m;
        }
        static Material Skin(ThemeAssets ta, Color tint)
        {
            if (ta == null || ta.dinoSkin == null) return Lit(tint, .3f);
            var m = new Material(ta.dinoSkin); m.SetColor("_BaseColor", tint); return m;
        }

        class Parts
        {
            public MeshBuilder skin = new MeshBuilder(), head = new MeshBuilder(), jaw = new MeshBuilder();
            public MeshBuilder eye = new MeshBuilder(), tooth = new MeshBuilder(), claw = new MeshBuilder();
            public MeshBuilder headTooth = new MeshBuilder(), jawTooth = new MeshBuilder(), headEye = new MeshBuilder();
        }

        static void Leg(MeshBuilder m, Vector3 hip, Vector3 knee, Vector3 ankle, Vector3 foot, float rh, float rk, float ra, float rf, MeshBuilder claws, int toes, float toeLen)
        {
            m.Loft(new[] { hip, knee, ankle, foot }, new[] { R(rh, rh * 1.1f), R(rk, rk), R(ra, ra), R(rf * 1.3f, rf * .7f) }, 6, 14, false, true);
            for (int i = 0; i < toes; i++)
            {
                float spread = (i - (toes - 1) / 2f) * .38f;
                Vector3 dir = new Vector3(Mathf.Cos(spread), -.15f, Mathf.Sin(spread));
                Vector3 tb = foot + dir * rf * .6f;
                m.Loft(new[] { foot, tb + dir * toeLen * .5f, tb + dir * toeLen }, new[] { R(rf * .45f, rf * .35f), R(rf * .3f, rf * .25f), R(rf * .2f, rf * .16f) }, 3, 8, false, true);
                claws.Spike(tb + dir * toeLen, dir + Vector3.down * .5f, rf * .16f, rf * .55f, 6);
            }
        }

        static GameObject Assemble(string name, Transform parent, Parts p, Material skin, Vector3 headPivot, Vector3 jawPivot, out Transform head, out Transform jaw)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var eyeMat = Lit(new Color(.05f, .03f, .01f), .92f);
            var toothMat = Lit(new Color(.88f, .84f, .72f), .45f);
            var clawMat = Lit(new Color(.12f, .1f, .08f), .5f);
            p.skin.ToObject("Koerper", skin, root); p.claw.ToObject("Krallen", clawMat, root);
            head = new GameObject("Kopf").transform; head.SetParent(root, false); head.localPosition = headPivot;
            jaw = new GameObject("Unterkiefer").transform; jaw.SetParent(head, false); jaw.localPosition = jawPivot - headPivot;
            // Kopf-Meshes sind in Koerperkoordinaten gebaut, darum um den Drehpunkt zuruecksetzen
            var hm = p.head.ToObject("Schaedel", skin, head); hm.transform.localPosition = -headPivot;
            var he = p.headEye.ToObject("Augen", eyeMat, head); he.transform.localPosition = -headPivot;
            var ht = p.headTooth.ToObject("Zaehne oben", toothMat, head); ht.transform.localPosition = -headPivot;
            var jm = p.jaw.ToObject("Kiefer", skin, jaw); jm.transform.localPosition = -jawPivot;
            var jt = p.jawTooth.ToObject("Zaehne unten", toothMat, jaw); jt.transform.localPosition = -jawPivot;
            return root.gameObject;
        }

        // ---------------- Tyrannosaurus rex ----------------
        public static GameObject TRex(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-7.6f, 2.3f), V(-5.6f, 3.0f), V(-3.6f, 3.65f), V(-1.6f, 4.05f), V(.3f, 4.1f), V(1.8f, 4.15f), V(2.8f, 4.55f), V(3.5f, 5.0f) },
                        new[] { R(.06f, .06f), R(.36f, .42f), R(.72f, .82f), R(1.05f, 1.2f), R(1.15f, 1.38f), R(1.0f, 1.28f), R(.62f, .72f), R(.5f, .58f) }, 6, 18, true, false);
            // Kopf (Schaedel und Schnauze) und Unterkiefer
            p.head.Loft(new[] { V(3.3f, 5.05f), V(4.1f, 5.35f), V(4.9f, 5.25f), V(5.6f, 5.0f), V(6.3f, 4.82f) },
                        new[] { R(.5f, .6f), R(.62f, .62f), R(.5f, .5f), R(.36f, .38f), R(.2f, .24f) }, 5, 16, false, true);
            p.jaw.Loft(new[] { V(4.0f, 4.85f), V(4.9f, 4.7f), V(5.6f, 4.6f), V(6.15f, 4.55f) },
                       new[] { R(.48f, .26f), R(.4f, .2f), R(.3f, .14f), R(.16f, .08f) }, 4, 12, true, true);
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(4.75f, 5.5f, s * .48f), .09f, 10);
                p.head.Spike(V(4.6f, 5.75f, s * .4f), V(.2f, 1f, s * .2f), .12f, .18f, 6);                 // Augenwuelste
                for (float x = 4.55f; x < 6.2f; x += .16f)
                {
                    float w = Mathf.Lerp(.5f, .2f, (x - 4.55f) / 1.65f);
                    p.headTooth.Spike(V(x, 4.78f, s * (w - .04f)), Vector3.down, .035f, .16f, 5);
                    p.jawTooth.Spike(V(x - .05f, 4.72f, s * (w - .08f)), Vector3.up, .03f, .12f, 5);
                }
                Leg(p.skin, V(-.8f, 3.7f, s * .85f), V(.3f, 2.2f, s * .95f), V(-.55f, .85f, s * .95f), V(.25f, .14f, s * .95f), .62f, .42f, .22f, .2f, p.claw, 3, .45f);
                // kurze Arme mit zwei Fingern
                p.skin.Loft(new[] { V(2.2f, 3.75f, s * .78f), V(2.6f, 3.3f, s * .88f), V(2.95f, 3.35f, s * .86f) }, new[] { R(.16f, .16f), R(.11f, .11f), R(.07f, .07f) }, 4, 8, false, true);
                p.claw.Spike(V(2.98f, 3.34f, s * .82f), V(.6f, -.8f, 0), .03f, .14f, 5);
                p.claw.Spike(V(2.98f, 3.34f, s * .9f), V(.6f, -.8f, 0), .03f, .14f, 5);
            }
            var go = Assemble("Tyrannosaurus", parent, p, Skin(ta, tint), V(3.3f, 5.0f), V(4.0f, 4.85f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 22; a.pitchAmp = 8; a.speed = .45f; a.roarEvery = 9; a.jawOpen = 32;
            return go;
        }

        // ---------------- Brachiosaurus ----------------
        public static GameObject Brachio(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-11.5f, 2.6f), V(-8.5f, 4.4f), V(-5.5f, 6.0f), V(-2.3f, 6.9f), V(.8f, 7.4f), V(3.0f, 7.8f), V(4.2f, 8.8f) },
                        new[] { R(.08f, .08f), R(.5f, .55f), R(1.15f, 1.25f), R(1.9f, 1.85f), R(2.0f, 2.15f), R(1.6f, 1.7f), R(.95f, .95f) }, 6, 20, true, false);
            p.head.Loft(new[] { V(4.0f, 8.5f), V(4.9f, 10.4f), V(5.6f, 12.4f), V(6.1f, 14.0f), V(6.5f, 14.8f), V(7.1f, 15.05f), V(7.9f, 14.85f) },
                        new[] { R(1.0f, 1.0f), R(.7f, .7f), R(.52f, .52f), R(.42f, .42f), R(.38f, .4f), R(.36f, .34f), R(.2f, .18f) }, 6, 16, false, true);
            p.jaw.Loft(new[] { V(6.9f, 14.75f), V(7.8f, 14.7f) }, new[] { R(.28f, .12f), R(.16f, .08f) }, 3, 10, true, true);
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(6.95f, 15.2f, s * .3f), .08f, 10);
                Leg(p.skin, V(2.7f, 7.0f, s * 1.35f), V(2.8f, 4.2f, s * 1.4f), V(2.7f, 1.2f, s * 1.4f), V(2.75f, .25f, s * 1.4f), .85f, .6f, .55f, .6f, p.claw, 1, .2f);
                Leg(p.skin, V(-2.6f, 6.4f, s * 1.35f), V(-2.0f, 3.6f, s * 1.45f), V(-2.4f, 1.1f, s * 1.45f), V(-2.2f, .25f, s * 1.45f), 1.0f, .68f, .55f, .62f, p.claw, 3, .25f);
            }
            var go = Assemble("Brachiosaurus", parent, p, Skin(ta, tint), V(4.0f, 8.5f), V(6.9f, 14.75f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 9; a.pitchAmp = 6; a.speed = .18f; a.roarEvery = 0; a.jawOpen = 10;
            return go;
        }

        // ---------------- Triceratops ----------------
        public static GameObject Trike(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-4.6f, 1.1f), V(-3.0f, 1.55f), V(-1.4f, 2.0f), V(.5f, 2.2f), V(1.9f, 2.0f), V(2.6f, 1.85f) },
                        new[] { R(.07f, .07f), R(.42f, .45f), R(1.0f, .95f), R(1.2f, 1.1f), R(.95f, .9f), R(.65f, .65f) }, 6, 18, true, false);
            p.head.Loft(new[] { V(2.4f, 1.9f), V(3.1f, 1.95f), V(3.7f, 1.75f), V(4.25f, 1.5f) }, new[] { R(.62f, .66f), R(.55f, .58f), R(.4f, .42f), R(.16f, .2f) }, 4, 14, false, true);
            // Nackenschild: flache Scheibe mit Rand
            p.head.Loft(new[] { V(2.55f, 2.35f), V(2.62f, 2.42f) }, new[] { R(1.45f, 1.25f), R(1.45f, 1.25f) }, 1, 24, true, true, Vector3.up);
            for (int i = 0; i < 12; i++)
            {
                float ang = Mathf.Lerp(-1.2f, 1.2f, i / 11f);
                Vector3 dir = new Vector3(-.25f, Mathf.Cos(ang), Mathf.Sin(ang)).normalized;
                p.claw.Spike(V(2.55f, 2.35f) + new Vector3(0, Mathf.Cos(ang) * 1.2f, Mathf.Sin(ang) * 1.4f), dir, .1f, .22f, 5);
            }
            p.jaw.Loft(new[] { V(3.0f, 1.55f), V(3.8f, 1.38f), V(4.15f, 1.3f) }, new[] { R(.4f, .18f), R(.28f, .12f), R(.12f, .06f) }, 3, 10, true, true);
            var horn = new MeshBuilder();
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(3.2f, 2.25f, s * .43f), .07f, 10);
                p.headTooth.Spike(V(3.3f, 2.45f, s * .32f), V(.9f, .55f, s * .1f), .13f, 1.1f, 8);   // Stirnhoerner
                Leg(p.skin, V(1.4f, 1.7f, s * .85f), V(1.7f, 1.0f, s * 1.0f), V(1.5f, .45f, s * 1.0f), V(1.6f, .14f, s * 1.0f), .4f, .32f, .25f, .27f, p.claw, 3, .15f);
                Leg(p.skin, V(-1.4f, 1.8f, s * .85f), V(-1.1f, 1.0f, s * .95f), V(-1.4f, .45f, s * .95f), V(-1.3f, .14f, s * .95f), .48f, .36f, .26f, .28f, p.claw, 3, .15f);
            }
            p.headTooth.Spike(V(3.95f, 1.85f), V(.5f, 1f, 0), .1f, .45f, 7);   // Nasenhorn
            var go = Assemble("Triceratops", parent, p, Skin(ta, tint), V(2.4f, 1.9f), V(3.0f, 1.55f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 12; a.pitchAmp = 10; a.speed = .3f; a.roarEvery = 0; a.jawOpen = 12;
            return go;
        }

        // ---------------- Stegosaurus ----------------
        public static GameObject Stego(Transform parent, ThemeAssets ta, Color tint, Color plateTint)
        {
            var p = new Parts(); var plates = new MeshBuilder();
            Vector3[] sp = { V(-4.9f, 1.4f), V(-3.1f, 2.35f), V(-1.2f, 3.15f), V(.6f, 2.95f), V(1.9f, 2.25f), V(2.6f, 1.75f) };
            Vector2[] sr = { R(.06f, .06f), R(.36f, .42f), R(.92f, 1.02f), R(.95f, .95f), R(.6f, .62f), R(.32f, .32f) };
            p.skin.Loft(sp, sr, 6, 18, true, false);
            p.head.Loft(new[] { V(2.45f, 1.68f), V(3.0f, 1.42f), V(3.55f, 1.2f) }, new[] { R(.3f, .3f), R(.24f, .22f), R(.12f, .1f) }, 4, 12, false, true);
            p.jaw.Loft(new[] { V(2.9f, 1.25f), V(3.45f, 1.08f) }, new[] { R(.18f, .08f), R(.1f, .05f) }, 3, 8, true, true);
            // Zwei Reihen Rueckenplatten, versetzt, in der Mitte am groessten
            // Rueckenlinie (Oberkante des Koerpers) aus den Kontrollpunkten
            float TopAt(float qx)
            {
                for (int k = 0; k < sp.Length - 1; k++)
                    if (qx >= sp[k].x && qx <= sp[k + 1].x)
                    {
                        float tt = (qx - sp[k].x) / (sp[k + 1].x - sp[k].x); tt = tt * tt * (3 - 2 * tt);
                        return Mathf.Lerp(sp[k].y + sr[k].y, sp[k + 1].y + sr[k + 1].y, tt);
                    }
                return sp[0].y;
            }
            for (int i = 0; i < 17; i++)
            {
                float f = i / 16f, x = Mathf.Lerp(2.1f, -3.9f, f);
                float y = TopAt(x) - .12f;
                float size = .25f + Mathf.Sin(Mathf.Pow(f, .8f) * Mathf.PI) * .7f;
                float s = i % 2 == 0 ? 1 : -1;
                plates.Spike(V(x, y, s * .16f), V(-.15f, 1f, s * .12f), size * .55f, size * 1.15f, 8, .14f, Vector3.forward);   // duenn quer, breit laengs
            }
            foreach (float s in new[] { -1f, 1f })
            {
                p.claw.Spike(V(-4.2f, 1.75f, s * .12f), V(-.5f, .6f, s * 1f), .07f, .85f, 7);   // Schwanzstacheln
                p.claw.Spike(V(-3.75f, 1.98f, s * .15f), V(-.3f, .7f, s * 1f), .07f, .8f, 7);
                p.headEye.Sphere(V(3.05f, 1.55f, s * .2f), .05f, 8);
                Leg(p.skin, V(1.5f, 2.0f, s * .62f), V(1.65f, 1.05f, s * .66f), V(1.5f, .4f, s * .66f), V(1.6f, .13f, s * .66f), .34f, .26f, .2f, .22f, p.claw, 3, .12f);
                Leg(p.skin, V(-1.2f, 2.7f, s * .78f), V(-.75f, 1.5f, s * .82f), V(-1.2f, .55f, s * .82f), V(-1.0f, .14f, s * .82f), .55f, .4f, .26f, .26f, p.claw, 3, .15f);
            }
            var go = Assemble("Stegosaurus", parent, p, Skin(ta, tint), V(2.45f, 1.68f), V(2.9f, 1.25f), out var head, out var jaw);
            plates.ToObject("Rueckenplatten", Skin(ta, plateTint), go.transform);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 38; a.pitchAmp = 12; a.speed = .55f; a.roarEvery = 0; a.jawOpen = 14;
            return go;
        }

        // ---------------- Parasaurolophus (trinkend) ----------------
        public static GameObject Para(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-5.2f, 2.1f), V(-3.4f, 2.7f), V(-1.4f, 3.1f), V(.6f, 3.0f), V(1.9f, 2.6f), V(2.7f, 2.5f) },
                        new[] { R(.06f, .06f), R(.38f, .46f), R(.9f, 1.05f), R(.92f, 1.0f), R(.55f, .6f), R(.32f, .35f) }, 6, 16, true, false);
            p.head.Loft(new[] { V(2.6f, 2.55f), V(3.2f, 2.75f), V(3.8f, 2.6f), V(4.3f, 2.35f) }, new[] { R(.3f, .34f), R(.28f, .32f), R(.2f, .2f), R(.16f, .1f) }, 4, 12, false, true);
            p.head.Loft(new[] { V(3.3f, 3.0f), V(2.6f, 3.4f), V(1.6f, 3.65f), V(1.1f, 3.7f) }, new[] { R(.1f, .14f), R(.09f, .12f), R(.08f, .1f), R(.05f, .06f) }, 4, 10, true, true);  // Roehrenkamm
            p.jaw.Loft(new[] { V(3.3f, 2.45f), V(4.2f, 2.2f) }, new[] { R(.2f, .1f), R(.12f, .05f) }, 3, 8, true, true);
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(3.3f, 2.85f, s * .26f), .055f, 8);
                Leg(p.skin, V(1.4f, 2.4f, s * .6f), V(1.6f, 1.4f, s * .62f), V(1.5f, .5f, s * .62f), V(1.6f, .12f, s * .62f), .26f, .2f, .15f, .17f, p.claw, 3, .12f);
                Leg(p.skin, V(-1.3f, 2.7f, s * .7f), V(-.6f, 1.6f, s * .76f), V(-1.2f, .6f, s * .76f), V(-.9f, .14f, s * .76f), .62f, .42f, .24f, .24f, p.claw, 3, .18f);
            }
            var go = Assemble("Parasaurolophus", parent, p, Skin(ta, tint), V(2.6f, 2.55f), V(3.3f, 2.45f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 8; a.pitchAmp = 8; a.speed = .35f; a.pitchBias = -28; a.jawOpen = 10;
            return go;
        }

        // ---------------- Spinosaurus (mit Segel, fischend) ----------------
        public static GameObject Spino(Transform parent, ThemeAssets ta, Color tint, Color sailTint)
        {
            var p = new Parts(); var sail = new MeshBuilder();
            p.skin.Loft(new[] { V(-8.5f, 2.2f), V(-6f, 3.0f), V(-3.6f, 3.7f), V(-1.4f, 4.05f), V(.6f, 4.1f), V(2.2f, 4.15f), V(3.3f, 4.6f), V(4.1f, 5.0f) },
                        new[] { R(.08f, .14f), R(.3f, .55f), R(.62f, .85f), R(.9f, 1.1f), R(.95f, 1.2f), R(.8f, 1.05f), R(.45f, .5f), R(.36f, .4f) }, 6, 16, true, false);
            p.head.Loft(new[] { V(3.9f, 5.0f), V(4.8f, 5.15f), V(5.8f, 4.95f), V(6.9f, 4.75f), V(7.5f, 4.7f) },
                        new[] { R(.36f, .42f), R(.32f, .34f), R(.22f, .24f), R(.17f, .17f), R(.13f, .12f) }, 5, 12, false, true);
            p.jaw.Loft(new[] { V(4.4f, 4.75f), V(5.8f, 4.62f), V(7.4f, 4.5f) }, new[] { R(.28f, .14f), R(.18f, .1f), R(.11f, .06f) }, 4, 10, true, true);
            sail.Loft(new[] { V(-3.2f, 4.3f), V(-1.6f, 5.2f), V(.2f, 5.6f), V(1.8f, 5.25f), V(2.9f, 4.7f) },
                      new[] { R(.05f, .2f), R(.07f, 1.1f), R(.07f, 1.5f), R(.07f, 1.1f), R(.05f, .2f) }, 6, 10, true, true);
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(4.9f, 5.35f, s * .26f), .07f, 8);
                for (float x = 4.7f; x < 7.4f; x += .18f) p.headTooth.Spike(V(x, 4.66f, s * .12f), Vector3.down, .025f, .12f, 5);
                Leg(p.skin, V(-1.0f, 3.6f, s * .8f), V(.0f, 2.1f, s * .88f), V(-.6f, .75f, s * .88f), V(.2f, .13f, s * .88f), .55f, .38f, .2f, .18f, p.claw, 3, .4f);
                p.skin.Loft(new[] { V(2.6f, 3.7f, s * .7f), V(3.1f, 3.0f, s * .85f), V(3.6f, 2.9f, s * .85f) }, new[] { R(.18f, .18f), R(.13f, .13f), R(.08f, .08f) }, 4, 8, false, true);
                p.claw.Spike(V(3.62f, 2.88f, s * .82f), V(.4f, -.9f, 0), .04f, .22f, 5);
            }
            var go = Assemble("Spinosaurus", parent, p, Skin(ta, tint), V(3.9f, 5.0f), V(4.4f, 4.75f), out var head, out var jaw);
            sail.ToObject("Segel", Skin(ta, sailTint), go.transform);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 18; a.pitchAmp = 10; a.speed = .4f; a.pitchBias = -12; a.roarEvery = 14; a.jawOpen = 26;
            return go;
        }

        // ---------------- Ankylosaurus (Panzer, Schwanzkeule) ----------------
        public static GameObject Ankylo(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-4.2f, .9f), V(-2.8f, 1.2f), V(-1.3f, 1.55f), V(.4f, 1.6f), V(1.6f, 1.4f), V(2.2f, 1.2f) },
                        new[] { R(.12f, .12f), R(.4f, .35f), R(1.3f, .72f), R(1.35f, .76f), R(1.0f, .6f), R(.5f, .42f) }, 6, 18, true, false);
            p.skin.Sphere(V(-4.35f, .9f), .42f, 14);   // Schwanzkeule
            p.head.Loft(new[] { V(2.1f, 1.2f), V(2.6f, 1.15f), V(3.0f, 1.0f) }, new[] { R(.48f, .38f), R(.42f, .3f), R(.24f, .16f) }, 4, 12, false, true);
            p.jaw.Loft(new[] { V(2.5f, .92f), V(2.95f, .88f) }, new[] { R(.26f, .08f), R(.15f, .05f) }, 3, 8, true, true);
            for (int i = 0; i < 9; i++)
            {
                float x = Mathf.Lerp(1.6f, -2.8f, i / 8f), w = Mathf.Lerp(1.0f, .45f, Mathf.Abs(i - 4) / 5f) + .3f;
                foreach (float s in new[] { -1f, 1f }) p.claw.Spike(V(x, 1.15f, s * w), V(0, .15f, s), .1f, .42f, 6);
                for (int k = -2; k <= 2; k++) p.claw.Spike(V(x, 1.6f + .65f - Mathf.Abs(k) * .12f, k * .32f), V(0, 1, k * .2f), .08f, .16f, 5);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(2.55f, 1.32f, s * .36f), .05f, 8);
                p.headTooth.Spike(V(2.3f, 1.42f, s * .42f), V(-.4f, .3f, s), .07f, .3f, 6);
                Leg(p.skin, V(1.3f, 1.2f, s * .9f), V(1.4f, .7f, s * 1.0f), V(1.3f, .3f, s * 1.0f), V(1.35f, .1f, s * 1.0f), .3f, .25f, .22f, .22f, p.claw, 3, .1f);
                Leg(p.skin, V(-1.4f, 1.3f, s * .9f), V(-1.2f, .7f, s * 1.0f), V(-1.35f, .3f, s * 1.0f), V(-1.3f, .1f, s * 1.0f), .36f, .28f, .23f, .23f, p.claw, 3, .1f);
            }
            var go = Assemble("Ankylosaurus", parent, p, Skin(ta, tint), V(2.1f, 1.2f), V(2.5f, .92f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 14; a.pitchAmp = 8; a.speed = .3f; a.pitchBias = -8; a.jawOpen = 10;
            return go;
        }

        // ---------------- Velociraptor (Rudel, lauernd) ----------------
        public static GameObject Raptor(Transform parent, ThemeAssets ta, Color tint)
        {
            var p = new Parts();
            p.skin.Loft(new[] { V(-2.6f, 1.05f), V(-1.6f, 1.15f), V(-.6f, 1.25f), V(.3f, 1.3f), V(.9f, 1.45f), V(1.25f, 1.75f) },
                        new[] { R(.03f, .03f), R(.12f, .14f), R(.28f, .33f), R(.3f, .36f), R(.2f, .22f), R(.13f, .14f) }, 6, 14, true, false);
            p.head.Loft(new[] { V(1.2f, 1.75f), V(1.55f, 1.9f), V(1.95f, 1.86f), V(2.3f, 1.78f) }, new[] { R(.14f, .15f), R(.15f, .14f), R(.1f, .1f), R(.05f, .06f) }, 4, 12, false, true);
            p.jaw.Loft(new[] { V(1.5f, 1.74f), V(2.25f, 1.7f) }, new[] { R(.1f, .05f), R(.05f, .03f) }, 3, 8, true, true);
            foreach (float s in new[] { -1f, 1f })
            {
                p.headEye.Sphere(V(1.66f, 1.96f, s * .12f), .03f, 8);
                for (float x = 1.65f; x < 2.25f; x += .07f) p.headTooth.Spike(V(x, 1.76f, s * .07f), Vector3.down, .01f, .05f, 4);
                Leg(p.skin, V(-.3f, 1.15f, s * .24f), V(.15f, .7f, s * .27f), V(-.25f, .3f, s * .27f), V(.1f, .06f, s * .27f), .2f, .12f, .06f, .06f, p.claw, 2, .14f);
                p.claw.Spike(V(.02f, .14f, s * .27f), V(.4f, .6f, 0), .025f, .14f, 5);   // Sichelkralle
                p.skin.Loft(new[] { V(.75f, 1.3f, s * .2f), V(.95f, 1.05f, s * .3f), V(1.25f, 1.1f, s * .28f) }, new[] { R(.06f, .06f), R(.05f, .05f), R(.03f, .03f) }, 4, 8, false, true);
                foreach (float o in new[] { -.03f, .03f }) p.claw.Spike(V(1.27f, 1.1f, s * .28f + o), V(.3f, -.9f, 0), .012f, .09f, 4);
            }
            var go = Assemble("Velociraptor", parent, p, Skin(ta, tint), V(1.2f, 1.75f), V(1.5f, 1.74f), out var head, out var jaw);
            var a = go.AddComponent<DinoAnim>(); a.head = head; a.jaw = jaw; a.yawAmp = 30; a.pitchAmp = 10; a.speed = 1.1f; a.roarEvery = 6.5f; a.jawOpen = 30;
            return go;
        }

        // ---------------- Plesiosaurus (schwimmend im See) ----------------
        public static GameObject Plesio(Transform parent, ThemeAssets ta, Color tint, Vector3 lakeCenter, Vector2 radius, float waterY)
        {
            var root = new GameObject("Plesiosaurus").transform; root.SetParent(parent, false);
            var mb = new MeshBuilder();
            mb.Loft(new[] { V(-3.2f, -.7f), V(-1.8f, -.55f), V(0, -.45f), V(1.6f, -.5f), V(2.5f, -.25f), V(3.4f, .9f), V(4.0f, 2.6f), V(4.5f, 4.0f), V(5.2f, 4.6f), V(5.9f, 4.45f) },
                    new[] { R(.05f, .05f), R(.4f, .3f), R(.95f, .6f), R(.85f, .55f), R(.35f, .35f), R(.27f, .27f), R(.22f, .22f), R(.2f, .2f), R(.2f, .19f), R(.08f, .07f) }, 6, 14, true, true);
            foreach (float s in new[] { -1f, 1f }) foreach (float x in new[] { 1.0f, -1.2f })
                    mb.Spike(V(x, -.6f, s * .6f), V(-.3f, -.1f, s * 1f), .38f, 1.9f, 8, .18f, Vector3.up);   // Flossen
            var eye = new MeshBuilder(); foreach (float s in new[] { -1f, 1f }) eye.Sphere(V(5.35f, 4.68f, s * .17f), .045f, 8);
            mb.ToObject("Koerper", Skin(ta, tint), root); eye.ToObject("Augen", Lit(new Color(.05f, .03f, .01f), .92f), root);
            var sw = root.gameObject.AddComponent<SwimLoop>(); sw.center = lakeCenter; sw.radius = radius; sw.y = waterY; sw.speed = .035f;
            root.localScale = Vector3.one * 1.4f;
            return root.gameObject;
        }

        // ---------------- Pteranodon (fliegend) ----------------
        public static GameObject Ptero(Transform parent, ThemeAssets ta, Color tint, Vector3 center, float radius, float height, float speed)
        {
            var root = new GameObject("Pteranodon").transform; root.SetParent(parent, false);
            var body = new MeshBuilder();
            body.Loft(new[] { V(-.9f, 0), V(-.3f, .05f), V(.4f, .08f), V(.75f, .2f), V(1.0f, .25f), V(1.8f, .15f) },
                      new[] { R(.05f, .05f), R(.22f, .24f), R(.2f, .22f), R(.1f, .1f), R(.13f, .15f), R(.02f, .02f) }, 5, 12, true, true);
            body.Spike(V(1.0f, .32f), V(-1f, .35f, 0), .08f, 1.0f, 6, .25f, Vector3.forward);    // Kopfkamm
            var skin = Skin(ta, tint);
            var b = body.ToObject("Koerper", skin, root);
            var wings = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? 1 : -1;
                var w = new GameObject(i == 0 ? "Fluegel links" : "Fluegel rechts").transform; w.SetParent(root, false); w.localPosition = V(.3f, .1f, s * .15f);
                var mb = new MeshBuilder();
                // Flughaut als duenne Flaeche, zur Spitze schmaler
                mb.Loft(new[] { V(0, 0, 0), V(-.1f, 0, s * 1.8f), V(-.3f, 0, s * 3.4f), V(-.7f, 0, s * 4.4f) },
                        new[] { R(.6f, .04f), R(.5f, .03f), R(.32f, .025f), R(.05f, .01f) }, 4, 6, true, true, Vector3.up);
                var go = mb.ToObject("Haut", skin, w); go.GetComponent<MeshRenderer>().material.SetFloat("_Cull", 0);
                wings[i] = w;
            }
            var f = root.gameObject.AddComponent<PteroFly>();
            f.center = center; f.radius = radius; f.height = height; f.speed = speed; f.wings = wings; f.phase = Random.value * 6.28f;
            root.localScale = Vector3.one * 1.6f;
            return root.gameObject;
        }
    }

    /// <summary>Atmen, Kopf-Schwenk, gelegentliches Bruellen (Maul auf).</summary>
    public class DinoAnim : MonoBehaviour
    {
        public Transform head, jaw;
        public float yawAmp = 15, pitchAmp = 6, speed = .4f, roarEvery = 0, jawOpen = 25, pitchBias = 0;
        float seed; Vector3 baseScale; Quaternion headRest;
        void Start() { seed = Random.value * 100f; baseScale = transform.localScale; if (head != null) headRest = head.localRotation; }
        void Update()
        {
            float t = Time.time * speed + seed;
            float breathe = 1 + .012f * Mathf.Sin(Time.time * 1.3f + seed);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * breathe, baseScale.z * breathe);
            float roar = 0;
            if (roarEvery > 0)
            {
                float c = (Time.time + seed) % roarEvery;
                roar = c < .4f ? c / .4f : c < 2.2f ? 1 : c < 2.8f ? 1 - (c - 2.2f) / .6f : 0;
            }
            if (head != null)
            {
                float yaw = (Mathf.PerlinNoise(t, seed) - .5f) * 2 * yawAmp, pitch = (Mathf.PerlinNoise(seed, t) - .5f) * 2 * pitchAmp - roar * 10f;
                head.localRotation = headRest * Quaternion.Euler(0, yaw, pitch + pitchBias + roar * 6f);
            }
            if (jaw != null)
            {
                float open = roar * jawOpen + Mathf.Max(0, Mathf.Sin(t * 1.7f)) * 3f;
                jaw.localRotation = Quaternion.Euler(0, 0, -open);
            }
        }
    }

    /// <summary>Schwimmt langsam auf einer Ellipse durch den See, wiegt sich in den Wellen.</summary>
    public class SwimLoop : MonoBehaviour
    {
        public Vector3 center; public Vector2 radius; public float y, speed = .04f; float phase;
        void Start() { phase = Random.value * 6.28f; }
        void Update()
        {
            float a = Time.time * speed + phase;
            Vector3 p = new Vector3(center.x + Mathf.Cos(a) * radius.x, y + Mathf.Sin(Time.time * .9f) * .12f, center.z + Mathf.Sin(a) * radius.y);
            Vector3 vel = new Vector3(-Mathf.Sin(a) * radius.x, 0, Mathf.Cos(a) * radius.y);
            transform.position = p;
            if (vel.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(vel) * Quaternion.Euler(Mathf.Sin(Time.time * .7f) * 2f, -90, Mathf.Sin(Time.time * .5f) * 3f);
        }
    }

    /// <summary>Kreist am Himmel, schlaegt mit den Fluegeln, gleitet zwischendurch.</summary>
    public class PteroFly : MonoBehaviour
    {
        public Vector3 center; public float radius = 60, height = 50, speed = .08f, phase; public Transform[] wings;
        void Update()
        {
            float a = Time.time * speed + phase;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, height + Mathf.Sin(a * 2.3f) * 6f, Mathf.Sin(a) * radius * .8f);
            Vector3 vel = new Vector3(-Mathf.Sin(a) * radius, Mathf.Cos(a * 2.3f) * 6f * 2.3f, Mathf.Cos(a) * radius * .8f);
            transform.position = p;
            if (vel.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(vel, Vector3.up) * Quaternion.Euler(0, -90, -Mathf.Sign(speed) * 25f);
            float glide = Mathf.SmoothStep(0, 1, Mathf.PerlinNoise(a * .7f, phase));
            float flap = Mathf.Sin(Time.time * 5.5f + phase) * 35f * (1 - glide) + 4f;
            if (wings != null && wings.Length == 2)
            {
                wings[0].localRotation = Quaternion.Euler(flap, 0, 0);
                wings[1].localRotation = Quaternion.Euler(-flap, 0, 0);
            }
        }
    }
}
