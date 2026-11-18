using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Burganlage der Ritterburg: Torhaus ueber der Station, Ringmauer mit Wehrgang und Tuermen, Vorburg mit Palas,
    /// Kapelle, Stall, Schmiede und Brunnen, Bergfried mit Erkern, Wassergraben und Zugbruecke. Nur Optik.
    /// Koordinaten in Unity-Metern; die Bahn laeuft in der Station bei z = 0, y = 4 entlang x.
    /// </summary>
    public static class CastleBuilder
    {
        class B
        {
            public MeshBuilder wall = new MeshBuilder { tile = 3f }, plinth = new MeshBuilder { tile = 3f }, keep = new MeshBuilder { tile = 3f },
                hall = new MeshBuilder { tile = 3f }, roof = new MeshBuilder { tile = 1.1f }, wood = new MeshBuilder { tile = 2f },
                dark = new MeshBuilder(), iron = new MeshBuilder(), gold = new MeshBuilder(), cloth = new MeshBuilder(),
                cobble = new MeshBuilder { tile = 2.2f }, thatch = new MeshBuilder { tile = 1.4f }, water = new MeshBuilder { tile = 6f };
        }

        static readonly Vector3 up = Vector3.up;

        static Material Lit(Color c, float smooth = .3f, float metal = 0f, bool twoSided = false)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (twoSided) m.SetFloat("_Cull", 0);
            return m;
        }
        static Material Tint(Material m, Color c) { if (m == null) return null; var x = new Material(m); x.SetColor("_BaseColor", c); return x; }

        // ---------- Bausteine ----------

        /// <summary>Wehrmauer: schraeger Sockel, Mauerkoerper, Kragsteine, Brustwehr mit Zinnen und Scharten, innere Bruestung.</summary>
        static void Wall(B b, Vector3 a, Vector3 c, float h, float t, Vector3 outward, bool body = true)
        {
            Vector3 d = c - a; float len = d.magnitude; if (len < .2f) return;
            Vector3 f = d / len; var q = Quaternion.LookRotation(f, up); Vector3 mid = (a + c) * .5f;
            outward = Vector3.ProjectOnPlane(outward, up).normalized;
            if (body)
            {
                b.plinth.Box(mid + up * .7f + outward * .3f, q, new Vector3(t + .6f, 1.4f, len));
                b.plinth.Box(mid + up * 1.65f + outward * .15f, q, new Vector3(t + .3f, .5f, len));
                b.wall.Box(mid + up * (h / 2), q, new Vector3(t, h, len));
                b.wall.Box(mid - outward * (t / 2 - .15f) + up * (h + .3f), q, new Vector3(.3f, .6f, len));   // innere Bruestung
            }
            for (float s = .45f; s < len - .3f; s += 1.05f)
                b.wall.Box(a + f * s + outward * (t / 2 + .22f) + up * (h - .55f), q, new Vector3(.44f, .4f, .42f));
            b.wall.Box(mid + outward * (t / 2 + .1f) + up * (h + .25f), q, new Vector3(.7f, 1.3f, len));
            int k = 0;
            for (float s = .7f; s < len - .5f; s += 2f, k++)
            {
                Vector3 p = a + f * s + outward * (t / 2 + .1f) + up * (h + 1.5f);
                b.wall.Box(p, q, new Vector3(.7f, 1.2f, 1.1f));
                if (k % 2 == 0) b.dark.Box(p + outward * .36f, Quaternion.LookRotation(outward), new Vector3(.14f, .75f, .05f));
            }
        }

        static void Tower(B b, Transform parent, Vector3 c, float r, float h, bool roof, Material flagMat, float flagYaw)
        {
            b.plinth.Taper(c, r * 1.3f, r, 3.2f, 24);
            b.wall.Cylinder(c + up * 3.2f, r, h - 3.2f, 24, true);
            int nc = Mathf.Max(10, Mathf.RoundToInt(2 * Mathf.PI * r / 1.0f));
            for (int i = 0; i < nc; i++)
            {
                float a = i / (float)nc * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                b.wall.Box(c + d * (r + .2f) + up * (h - .55f), Quaternion.LookRotation(d), new Vector3(.4f, .4f, .45f));
            }
            b.wall.Cylinder(c + up * (h - .4f), r + .45f, 1.3f, 24, false);
            b.wall.Cylinder(c + up * h, r - .1f, .9f, 20, false);
            int nm = Mathf.Max(8, Mathf.RoundToInt(2 * Mathf.PI * (r + .45f) / 1.9f));
            for (int i = 0; i < nm; i++)
            {
                float a = (i + .5f) / nm * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                b.wall.Box(c + d * (r + .45f) + up * (h + 1.5f), Quaternion.LookRotation(d), new Vector3(1.0f, 1.2f, .6f));
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90 + 45) * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                foreach (float y in new[] { h * .45f, h * .75f })
                    b.dark.Box(c + d * (r + .02f) + up * y, Quaternion.LookRotation(d), new Vector3(.22f, 1.1f, .06f));
            }
            Vector3 poleBase;
            if (roof)
            {
                float rh = r * 2.3f;
                b.roof.Cone(c + up * (h + .9f), r + .95f, rh, 28);
                poleBase = c + up * (h + .9f + rh - .2f);
                b.gold.Box(poleBase + up * .25f, Quaternion.identity, new Vector3(.16f, .5f, .16f));
            }
            else poleBase = c + up * (h + .1f);
            b.iron.Box(poleBase + up * 1.7f, Quaternion.identity, new Vector3(.07f, 3.4f, .07f));
            FlagWave.Create(parent, poleBase + up * 3.35f, 2.0f, 1.25f, flagMat, flagYaw);
        }

        /// <summary>Rundbogen in einer Mauer: Bogensteine plus Zwickel bis zur Mauerkrone.</summary>
        static void Arch(B b, Vector3 c, Vector3 along, float half, float spring, float top, float thick)
        {
            along = along.normalized;
            const int n = 13;
            for (int i = 0; i < n; i++)
            {
                float th = i / (n - 1f) * Mathf.PI;
                Vector3 radial = along * Mathf.Cos(th) + up * Mathf.Sin(th), tan = -along * Mathf.Sin(th) + up * Mathf.Cos(th);
                b.wall.Box(c + radial * (half + .27f) + up * spring, Quaternion.LookRotation(tan, radial), new Vector3(thick + .16f, .55f, .62f));
            }
            for (float s = -half - .54f; s < half + .54f; s += .36f)
            {
                float sc = Mathf.Clamp(s + .18f, -(half + .54f), half + .54f);
                float ya = Mathf.Abs(sc) < half + .54f ? spring + Mathf.Sqrt(Mathf.Max(0, (half + .54f) * (half + .54f) - sc * sc)) : spring;
                float hh = top - ya; if (hh <= .05f) continue;
                b.wall.Box(c + along * sc + up * (ya + hh / 2 - .05f), Quaternion.LookRotation(along), new Vector3(thick, hh + .1f, .38f));
            }
        }

        static void Portcullis(B b, Vector3 c, Vector3 along, float half, float spring)
        {
            along = along.normalized;
            for (float s = -half + .15f; s <= half - .1f; s += .37f)
            {
                float topY = spring + Mathf.Sqrt(Mathf.Max(0, half * half - s * s)) - .05f, botY = spring - .55f;
                b.iron.Box(c + along * s + up * ((topY + botY) / 2), Quaternion.LookRotation(along), new Vector3(.08f, topY - botY, .08f));
            }
            foreach (float y in new[] { -.35f, .4f, 1.1f })
            {
                float w = 2 * Mathf.Sqrt(Mathf.Max(0, half * half - Mathf.Max(0, y) * Mathf.Max(0, y))) - .2f;
                b.iron.Box(c + up * (spring + y), Quaternion.LookRotation(along), new Vector3(.07f, .07f, w));
            }
        }

        static void Banner(B b, Vector3 wallFace, Vector3 outward, float w, float h)
        {
            var q = Quaternion.LookRotation(outward);
            b.cloth.Box(wallFace + outward * .04f, q, new Vector3(w, h, .04f));
            b.gold.Box(wallFace + outward * .08f + up * (h / 2 + .05f), q, new Vector3(w + .3f, .12f, .12f));
            b.gold.Box(wallFace + outward * .07f + up * (h * .12f), q, new Vector3(w * .38f, w * .45f, .03f));
        }

        static Vector3 Out(Vector3 a, Vector3 c, Vector3 center)
        {
            Vector3 f = (c - a).normalized, n = Vector3.Cross(up, f);
            return Vector3.Dot(n, (a + c) * .5f - center) >= 0 ? n : -n;
        }

        // ---------- Gesamtanlage ----------

        public static void Build(TrackDef def, Track tr, Transform parent, ThemeAssets ta, Scenery.Clearance clr, Ground g)
        {
            var b = new B();
            var flagMat = Lit(def.accent, .1f, 0, true);
            var torches = new List<(Vector3 p, Vector3 o, bool light)>();
            var fires = new List<Vector3>();
            var smokes = new List<Vector3>();

            // --- Torhaus ueber der Station ---
            float x0 = -14, x1 = 14, zw = 5.5f, h = 11f, spring = 6.6f, half = 2f;
            Wall(b, new Vector3(x0, 0, -zw), new Vector3(x1, 0, -zw), h, 2f, -Vector3.forward);
            Wall(b, new Vector3(x0, 0, zw), new Vector3(x1, 0, zw), h, 2f, Vector3.forward);
            foreach (float x in new[] { x0, x1 })
            {
                Vector3 o = new Vector3(Mathf.Sign(x), 0, 0);
                Wall(b, new Vector3(x, 0, -zw), new Vector3(x, 0, -half - .54f), h, 2f, o);
                Wall(b, new Vector3(x, 0, half + .54f), new Vector3(x, 0, zw), h, 2f, o);
                Wall(b, new Vector3(x, 0, -half - .54f), new Vector3(x, 0, half + .54f), h, 2f, o, false);
                Arch(b, new Vector3(x, 0, 0), Vector3.forward, half, spring, h, 2f);
                Portcullis(b, new Vector3(x - Mathf.Sign(x) * .35f, 0, 0), Vector3.forward, half, spring);
                foreach (float z in new[] { -2.15f, 2.15f })
                {
                    Vector3 gp = new Vector3(x - Mathf.Sign(x) * 2.1f, 5.4f, z);
                    b.wood.Box(gp, Quaternion.identity, new Vector3(2.0f, 3.8f, .14f));
                    foreach (float y in new[] { -1.1f, 1.1f }) b.iron.Box(gp + new Vector3(0, y, Mathf.Sign(z) * .08f), Quaternion.identity, new Vector3(1.9f, .1f, .03f));
                }
            }
            float[] gtYaw = { 200, 340, 160, 20 }; int gi = 0;
            foreach (float x in new[] { x0, x1 }) foreach (float z in new[] { -zw, zw }) Tower(b, parent, new Vector3(x, 0, z), 2.9f, 16f, true, flagMat, gtYaw[gi++]);
            foreach (float z in new[] { -zw, zw })
            {
                Vector3 o = new Vector3(0, 0, Mathf.Sign(z));
                foreach (float x in new[] { -7f, 7f }) Banner(b, new Vector3(x, 7.8f, z + Mathf.Sign(z) * 1.0f), o, 1.5f, 3.4f);
                foreach (float x in new[] { -10f, -4f, 4f, 10f })
                    torches.Add((new Vector3(x, 6.4f, z - Mathf.Sign(z) * 1.0f), -o, Mathf.Abs(x) < 5));
            }
            b.cobble.Box(new Vector3(2, 1.75f, 0), Quaternion.identity, new Vector3(46f, 3.5f, 7f));
            clr.Block(new Rect(x0 - 5, -zw - 5, (x1 - x0) + 10, 2 * zw + 10));
            clr.Block(new Rect(-24, -4, 50, 8));

            // --- Ringmauer der Vorburg (suedlich der Station, dort kreuzt keine Bahn) ---
            Vector3 bc = new Vector3(2, 0, 28);
            Vector3 G1 = new Vector3(x0, 0, zw), G2 = new Vector3(x1, 0, zw);
            Vector3 C1 = new Vector3(-30, 0, 9), C2 = new Vector3(-30, 0, 48), C3 = new Vector3(34, 0, 48), C4 = new Vector3(34, 0, 9);
            Vector3 S1 = new Vector3(-3, 0, 48), S2 = new Vector3(7, 0, 48), W = new Vector3(-30, 0, 28), E = new Vector3(34, 0, 28);
            const float ch = 10f, ct = 2.4f;
            void Seg(Vector3 a, Vector3 c) => Wall(b, a, c, ch, ct, Out(a, c, bc));
            Seg(G1, C1); Seg(C1, W); Seg(W, C2); Seg(C2, S1); Seg(S2, C3); Seg(C3, E); Seg(E, C4); Seg(C4, G2);
            // Suedtor mit Bogen zwischen zwei Tortuermen
            Vector3 sg = new Vector3(2, 0, 48); float sh = 1.8f, ss = 4.2f;
            Wall(b, S1, sg - Vector3.right * (sh + .54f), ch, ct, Vector3.forward);
            Wall(b, sg + Vector3.right * (sh + .54f), S2, ch, ct, Vector3.forward);
            Wall(b, sg - Vector3.right * (sh + .54f), sg + Vector3.right * (sh + .54f), ch, ct, Vector3.forward, false);
            Arch(b, sg, Vector3.right, sh, ss, ch, ct);
            Portcullis(b, sg - Vector3.forward * .4f, Vector3.right, sh, ss);
            foreach (float sx in new[] { -1f, 1f })
                b.wood.Box(sg + new Vector3(sx * (sh + .1f), 2.1f, -ct / 2 - 1.0f), Quaternion.Euler(0, sx * 90, 0), new Vector3(1.9f, 4.0f, .14f));
            torches.Add((sg + new Vector3(-2.8f, 3.2f, ct / 2), Vector3.forward, false));
            torches.Add((sg + new Vector3(2.8f, 3.2f, ct / 2), Vector3.forward, false));
            Tower(b, parent, C1, 3.4f, 14f, false, flagMat, 120); Tower(b, parent, C4, 3.4f, 14f, false, flagMat, 60);
            Tower(b, parent, C2, 3.6f, 16f, true, flagMat, 150); Tower(b, parent, C3, 3.6f, 16f, true, flagMat, 30);
            Tower(b, parent, W, 3.0f, 13f, true, flagMat, 180); Tower(b, parent, E, 3.0f, 13f, true, flagMat, 0);
            Tower(b, parent, S1, 3.0f, 14f, true, flagMat, 100); Tower(b, parent, S2, 3.0f, 14f, true, flagMat, 80);
            foreach (var p in new[] { new Vector3(-30, 0, 18), new Vector3(-30, 0, 38) })
                Banner(b, p + new Vector3(-ct / 2 - .05f, 7f, 0), -Vector3.right, 1.4f, 3f);
            b.cobble.Box(new Vector3(2, .05f, 27.5f), Quaternion.identity, new Vector3(62f, .1f, 38f));
            clr.Block(new Rect(-38, 2, 80, 54));
            g.dirtLines.Add(new Vector4(2, 49, 2, 64));
            g.dirtSpots.Add(new Vector3(2, 58, 5));

            // --- Palas (Rittersaal) an der Suedmauer ---
            {
                Vector3 c = new Vector3(4, 0, 39.5f); float w = 24, d = 11, hh = 9;
                b.plinth.Box(c + up * .6f, Quaternion.identity, new Vector3(w + .6f, 1.2f, d + .6f));
                b.hall.Box(c + up * (hh / 2), Quaternion.identity, new Vector3(w, hh, d));
                float front = c.z - d / 2;
                for (float bx = c.x - w / 2; bx <= c.x + w / 2 + .01f; bx += 4)
                {
                    b.hall.Box(new Vector3(bx, 3.2f, front - .55f), Quaternion.identity, new Vector3(1.0f, 6.4f, 1.1f));   // Strebepfeiler
                    b.hall.Box(new Vector3(bx, 6.7f, front - .3f), Quaternion.Euler(30, 0, 0), new Vector3(1.0f, 1.2f, .7f));
                }
                for (float wx = c.x - w / 2 + 2; wx < c.x + w / 2; wx += 4)
                {
                    if (Mathf.Abs(wx - c.x) < 1.5f) continue;   // Platz fuer das Portal
                    Vector3 wp = new Vector3(wx, 5.0f, front);
                    b.hall.Box(wp - Vector3.forward * .03f, Quaternion.identity, new Vector3(1.5f, 3.6f, .1f));
                    b.dark.Box(wp - Vector3.forward * .09f, Quaternion.identity, new Vector3(1.0f, 3.0f, .05f));
                    b.iron.Box(wp - Vector3.forward * .12f, Quaternion.identity, new Vector3(.05f, 3.0f, .03f));
                }
                b.wood.Box(new Vector3(c.x, 1.6f, front - .07f), Quaternion.identity, new Vector3(2.0f, 3.2f, .12f));
                Arch(b, new Vector3(c.x, 0, front - .1f), Vector3.right, 1.0f, 3.2f, 4.6f, .3f);
                b.roof.Gable(c + up * hh, Quaternion.Euler(0, 90, 0), d, w, 6.5f, .5f);
                foreach (float x in new[] { -6f, 10f }) b.hall.Box(new Vector3(c.x + x, hh + 4.5f, c.z + 2.2f), Quaternion.identity, new Vector3(1.2f, 4f, 1.2f));
                foreach (float x in new[] { -3f, 3f }) Banner(b, new Vector3(c.x + x, 6.5f, front - .06f), -Vector3.forward, 1.1f, 2.8f);
                torches.Add((new Vector3(c.x - 1.8f, 3.4f, front - .1f), -Vector3.forward, false));
                torches.Add((new Vector3(c.x + 1.8f, 3.4f, front - .1f), -Vector3.forward, false));
                smokes.Add(new Vector3(c.x + 10, hh + 6.7f, c.z + 2.2f));
            }

            // --- Kapelle mit Glockenturm ---
            {
                Vector3 c = new Vector3(-19, 0, 24.5f); float w = 8, d = 14, hh = 7.5f;
                b.plinth.Box(c + up * .5f, Quaternion.identity, new Vector3(w + .5f, 1.0f, d + .5f));
                b.wall.Box(c + up * (hh / 2), Quaternion.identity, new Vector3(w, hh, d));
                foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -3.5f, 0f, 3.5f })
                {
                    Vector3 wp = c + new Vector3(sx * (w / 2), 4.2f, z);
                    b.wall.Box(wp + Vector3.right * sx * .04f, Quaternion.identity, new Vector3(.1f, 2.9f, 1.05f));
                    b.dark.Box(wp + Vector3.right * sx * .1f, Quaternion.identity, new Vector3(.05f, 2.5f, .65f));
                }
                b.dark.Box(c + new Vector3(0, 6.4f, d / 2 + .05f), Quaternion.Euler(0, 0, 45), new Vector3(1.2f, 1.2f, .05f));   // Rosette
                b.roof.Gable(c + up * hh, Quaternion.identity, w, d, 5f, .45f);
                Vector3 bt = new Vector3(c.x, 0, c.z - d / 2 - 2.2f); const float bh = 16f;
                b.plinth.Box(bt + up * .7f, Quaternion.identity, new Vector3(5.2f, 1.4f, 5.2f));
                b.wall.Box(bt + up * (bh / 2), Quaternion.identity, new Vector3(4.4f, bh, 4.4f));
                for (int s = 0; s < 4; s++)
                {
                    Vector3 n = Quaternion.Euler(0, 90 * s, 0) * Vector3.forward;
                    b.dark.Box(bt + n * 2.23f + up * (bh - 2.2f), Quaternion.LookRotation(n), new Vector3(1.2f, 2.0f, .05f));
                    b.dark.Box(bt + n * 2.23f + up * (bh * .5f), Quaternion.LookRotation(n), new Vector3(.25f, 1.1f, .05f));
                }
                b.wall.Box(bt + up * (bh - .2f), Quaternion.identity, new Vector3(4.9f, .4f, 4.9f));
                b.roof.Cone(bt + up * bh, 3.4f, 8f, 4, 45f);
                Vector3 tip = bt + up * (bh + 8.1f);
                b.gold.Box(tip + up * .8f, Quaternion.identity, new Vector3(.12f, 1.6f, .12f));
                b.gold.Box(tip + up * 1.1f, Quaternion.identity, new Vector3(.8f, .12f, .12f));
                b.wood.Box(bt + new Vector3(0, 1.5f, -2.25f), Quaternion.identity, new Vector3(1.6f, 3.0f, .1f));
            }

            // --- Stall als Pultdach an der Ostmauer ---
            {
                float xi = 34 - ct / 2, xo = 26.5f, z0 = 15, z1 = 41, yi = 5.8f, yo = 3.3f;
                float run = xi - xo, ang = Mathf.Atan2(yi - yo, run) * Mathf.Rad2Deg, wdt = Mathf.Sqrt(run * run + (yi - yo) * (yi - yo));
                b.thatch.Box(new Vector3((xi + xo) / 2, (yi + yo) / 2, (z0 + z1) / 2), Quaternion.Euler(0, 0, ang), new Vector3(wdt + .6f, .25f, z1 - z0 + .6f));
                for (float z = z0; z <= z1 + .01f; z += 3.25f)
                {
                    b.wood.Box(new Vector3(xo + .2f, yo / 2, z), Quaternion.identity, new Vector3(.25f, yo, .25f));
                    b.wood.Box(new Vector3((xi + xo) / 2, 1.1f, z), Quaternion.identity, new Vector3(run, 1.1f, .08f));      // Boxen-Trennwand
                }
                for (int k = 0; k < 6; k++) b.thatch.Box(new Vector3(30f + Random.Range(-1f, 1f), .45f, z0 + 2 + k * 4.2f), Quaternion.Euler(0, Random.Range(0, 30), 0), new Vector3(1.0f, .9f, 1.4f));
                if (ta != null && ta.props != null && ta.props.Length > 0)
                    for (int k = 0; k < 5; k++) Scenery.PlaceProp(ta.props[k % ta.props.Length], parent, new Vector3(24.5f + Random.Range(-.5f, .5f), 0, 18 + k * 5f), Random.Range(0, 360));
            }

            // --- Schmiede mit Esse ---
            {
                Vector3 c = new Vector3(19, 0, 15.5f);
                b.plinth.Box(c + up * .4f, Quaternion.identity, new Vector3(6.4f, .8f, 5.4f));
                b.wall.Box(c + up * 2.1f, Quaternion.identity, new Vector3(6f, 4.2f, 5f));
                b.roof.Gable(c + up * 4.2f, Quaternion.identity, 6f, 5f, 2.6f, .4f);
                b.dark.Box(c + new Vector3(0, 1.4f, -2.53f), Quaternion.identity, new Vector3(2.4f, 2.6f, .06f));
                b.wall.Box(c + new Vector3(2f, 5.6f, 1.2f), Quaternion.identity, new Vector3(1.1f, 3.4f, 1.1f));
                fires.Add(c + new Vector3(0, .6f, -2.3f));
                smokes.Add(c + new Vector3(2f, 7.4f, 1.2f));
                b.iron.Box(c + new Vector3(-1.5f, .55f, -3.6f), Quaternion.identity, new Vector3(.9f, .35f, .35f));      // Amboss
                b.wood.Box(c + new Vector3(-1.5f, .2f, -3.6f), Quaternion.identity, new Vector3(.5f, .4f, .5f));
            }

            // --- Brunnen im Hof ---
            {
                Vector3 c = new Vector3(6, 0, 22);
                b.plinth.Cylinder(c, 1.1f, .95f, 20, false);
                b.water.Disc(c + up * .7f, 1.0f, 20, up);
                foreach (float sx in new[] { -1f, 1f }) b.wood.Box(c + new Vector3(sx * 1.05f, 1.3f, 0), Quaternion.identity, new Vector3(.15f, 2.1f, .15f));
                b.wood.Beam(c + new Vector3(-1.1f, 2.15f, 0), c + new Vector3(1.1f, 2.15f, 0), .1f, up);
                b.roof.Gable(c + up * 2.35f, Quaternion.Euler(0, 90, 0), 1.7f, 2.4f, .8f, .1f);
            }

            // --- Bergfried mit Erkern, Ringmauer, Graben und Zugbruecke ---
            if (def.castleSpots != null)
                foreach (var spot in def.castleSpots)
                {
                    Vector3 c = new V3(spot.x, 0, spot.z).ToUnity();
                    var q = Quaternion.Euler(0, 45, 0); const float kh = 26f;
                    b.plinth.Box(c + up * 1.2f, q, new Vector3(12.4f, 2.4f, 12.4f));
                    b.plinth.Box(c + up * 2.7f, q, new Vector3(11.2f, .7f, 11.2f));
                    b.keep.Box(c + up * (kh / 2), q, new Vector3(10f, kh, 10f));
                    for (int s = 0; s < 4; s++)
                    {
                        var qs = q * Quaternion.Euler(0, 90 * s, 0);
                        Vector3 n = qs * Vector3.forward, a = c + qs * new Vector3(-5, 0, 5), e = c + qs * new Vector3(5, 0, 5);
                        Wall(b, a, e, kh, .1f, n, false);
                        foreach (float wy in new[] { 9f, 15f, 21f }) b.dark.Box(c + n * 5.03f + up * wy, Quaternion.LookRotation(n), new Vector3(.45f, 1.4f, .06f));
                        Vector3 corner = c + qs * new Vector3(5, 0, 5);
                        Vector3 outC = (corner - c).normalized;
                        Vector3 bz = corner + outC * .5f;
                        b.keep.Cylinder(bz + up * (kh - 3.5f), 1.25f, 5.2f, 16, true);
                        for (int k = 0; k < 6; k++) b.keep.Box(bz + up * (kh - 3.8f - k * .25f), Quaternion.identity, new Vector3(2.5f - k * .35f, .25f, 2.5f - k * .35f));
                        b.roof.Cone(bz + up * (kh + 1.7f), 1.65f, 3.4f, 16);
                        b.dark.Box(bz + outC * 1.27f + up * (kh - .8f), Quaternion.LookRotation(outC), new Vector3(.2f, .8f, .05f));
                    }
                    b.roof.Cone(c + up * (kh + .3f), 6.6f, 8f, 4);
                    b.iron.Box(c + up * (kh + 9.8f), Quaternion.identity, new Vector3(.09f, 4f, .09f));
                    FlagWave.Create(parent, c + up * (kh + 11.7f), 2.6f, 1.6f, flagMat, 210);
                    Vector3 dn = q * Vector3.forward;
                    b.wood.Box(c + dn * 5.06f + up * 1.6f, Quaternion.LookRotation(dn), new Vector3(1.6f, 3.0f, .1f));

                    // Ringmauer mit Tor nach +z, Graben, Zugbruecke
                    const int seg = 14; const float rr = 10.5f;
                    for (int k = 0; k < seg; k++)
                    {
                        if (k == 3) continue;
                        float a0 = k / (float)seg * Mathf.PI * 2, a1 = (k + 1) / (float)seg * Mathf.PI * 2;
                        Vector3 pa = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr, pb = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr;
                        Wall(b, pa, pb, 4.5f, 1.2f, Out(pa, pb, c));
                    }
                    float ga = 3.5f / seg * Mathf.PI * 2; Vector3 gd = new Vector3(Mathf.Cos(ga), 0, Mathf.Sin(ga)), gs = Vector3.Cross(up, gd);
                    foreach (float sx in new[] { -1f, 1f }) Tower(b, parent, c + gd * rr + gs * sx * 2.6f, 1.5f, 7f, true, flagMat, 90);
                    b.water.Annulus(c + up * .12f, 11.5f, 13.7f, 56);
                    for (int k = 0; k < 24; k++)
                        foreach (float r2 in new[] { 11.3f, 13.9f })
                        {
                            float a0 = k / 24f * Mathf.PI * 2, a1 = (k + 1) / 24f * Mathf.PI * 2;
                            Vector3 pa = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r2, pb = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r2;
                            b.plinth.Box((pa + pb) * .5f + up * .22f, Quaternion.LookRotation(pb - pa), new Vector3(.4f, .45f, (pb - pa).magnitude + .05f));
                        }
                    b.wood.Box(c + gd * 12.4f + up * .6f, Quaternion.LookRotation(gd), new Vector3(3.0f, .25f, 4.4f));
                    for (float s = 10.4f; s < 14.5f; s += .55f) b.wood.Box(c + gd * s + up * .76f, Quaternion.LookRotation(gd), new Vector3(3.0f, .06f, .08f));
                    foreach (float sx in new[] { -1.35f, 1.35f })
                        b.iron.Beam(c + gd * 10.6f + gs * sx + up * 4.4f, c + gd * 14.5f + gs * sx + up * .75f, .06f, up);
                    clr.Block(new Rect(c.x - 15, c.z - 15, 30, 30));
                }

            // --- Meshes, Fackeln, Feuer, Rauch ---
            var dark = Lit(new Color(.03f, .03f, .035f), .2f);
            b.wall.ToObject("Mauern", ta != null ? ta.castleWall : null, parent);
            b.plinth.ToObject("Mauersockel", ta != null ? Tint(ta.castleWall, new Color(.58f, .6f, .52f)) : null, parent);
            b.keep.ToObject("Bergfried", ta != null ? Tint(ta.stoneBlocks, new Color(.72f, .72f, .7f)) : null, parent);
            b.hall.ToObject("Palas", ta != null ? Tint(ta.stoneBlocks, new Color(.82f, .8f, .76f)) : null, parent);
            b.roof.ToObject("Schieferdaecher", ta != null ? ta.roofSlate : null, parent);
            b.thatch.ToObject("Stall und Heu", ta != null ? ta.roofThatch : null, parent);
            b.wood.ToObject("Holzwerk", ta != null ? ta.woodStructure : null, parent);
            b.cobble.ToObject("Pflaster", ta != null ? ta.cobble : null, parent);
            b.dark.ToObject("Oeffnungen", dark, parent, false);
            var iron = Lit(new Color(.1f, .1f, .11f), .45f, .85f);
            b.iron.ToObject("Eisen", iron, parent);
            b.gold.ToObject("Wappen und Knaeufe", Lit(new Color(.74f, .58f, .2f), .6f, .85f), parent);
            b.cloth.ToObject("Banner", Lit(def.accent * .85f, .12f, 0, true), parent);
            b.water.ToObject("Wasser", Lit(new Color(.07f, .13f, .12f), .95f, 0), parent, false);
            foreach (var t in torches) Fx.Torch(parent, t.p, t.o, ta != null ? ta.woodStructure : null, iron, ta != null ? ta.fire : null, t.light);
            foreach (var f in fires)
            {
                Fx.Fire(parent, f, ta != null ? ta.fire : null, 2.2f);
                var lg = new GameObject("Esse"); lg.transform.SetParent(parent, false); lg.transform.position = f + up * .6f - Vector3.forward * .6f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.color = new Color(1f, .5f, .2f); l.shadows = LightShadows.None;
                lg.AddComponent<Flicker>().baseIntensity = 3f;
            }
            foreach (var s in smokes) Scenery.SmokeAt(parent, s, ta != null ? ta.smoke : null);
        }
    }
}
