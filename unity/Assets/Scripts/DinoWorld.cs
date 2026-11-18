using UnityEngine;

namespace Achterbahn
{
    /// <summary>Urzeit-Tal fuer den Dino-Looping: Station mit Stammtor, Felsstuetzen, See, Urwald, Dinosaurier, Vulkan. Nur Optik.</summary>
    public static class DinoWorld
    {
        static readonly Vector3 up = Vector3.up;

        static Material Lit(Color c, float smooth = .3f, float metal = 0)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            return m;
        }
        static Material Tint(Material m, Color c) { if (m == null) return null; var x = new Material(m); x.SetColor("_BaseColor", c); return x; }

        // ---------- Felsstuetzen ----------
        public static void Supports(Track track, Transform parent, ThemeAssets ta)
        {
            var mb = new MeshBuilder { tile = 2.5f };
            int step = Mathf.Max(1, Mathf.RoundToInt(7f / (float)track.ds));
            for (int i = 0; i < track.n; i += step)
            {
                Vector3 p = track.pos[i].ToUnity();
                float h = p.y - .25f;
                if (h < .6f || track.U[i].y < .5) continue;
                mb.Taper(new Vector3(p.x, 0, p.z), .62f, .42f, h, 10);
                mb.Taper(new Vector3(p.x, -.2f, p.z), 1.1f, .7f, .9f, 10);
            }
            mb.ToObject("Felsstuetzen", ta != null ? Tint(ta.rockWall, new Color(.78f, .74f, .68f)) : null, parent);
        }

        // ---------- Station: Plattform, Palisade, Stammtor ----------
        public static void Station(TrackDef def, Transform parent, ThemeAssets ta, Scenery.Clearance clr)
        {
            var wood = new MeshBuilder { tile = 2f }; var logs = new MeshBuilder { tile = 1.5f }; var rock = new MeshBuilder { tile = 2f };
            var bone = new MeshBuilder();
            var iron = Lit(new Color(.1f, .1f, .11f), .45f, .85f);
            wood.Box(new Vector3(-12, 1.33f, 0), Quaternion.identity, new Vector3(32f, 2.66f, 7f));
            for (float x = -27.5f; x <= 3.6f; x += 2.5f) foreach (float z in new[] { -3.4f, 3.4f })
                    logs.Cylinder(new Vector3(x, 2.66f, z), .09f, 1.05f, 6, true);
            foreach (float z in new[] { -3.4f, 3.4f }) logs.Loft(new[] { new Vector3(-28, 3.65f, z), new Vector3(4, 3.65f, z) }, new[] { new Vector2(.08f, .08f), new Vector2(.08f, .08f) }, 1, 6, true, true);
            for (float x = -32; x <= 2.2f; x += .46f)
                foreach (float z in new[] { -5.4f, 5.4f })
                {
                    float h = 4.4f + Mathf.PerlinNoise(x * .7f, z) * .9f;
                    logs.Cylinder(new Vector3(x, 0, z + Random.Range(-.06f, .06f)), .23f, h, 7, false);
                    logs.Spike(new Vector3(x, h, z), up, .23f, .55f, 7);
                }
            float gx = 6f;
            foreach (float z in new[] { -4.4f, 4.4f })
            {
                rock.Box(new Vector3(gx, 1.0f, z), Quaternion.Euler(0, 12, 0), new Vector3(2.6f, 2.2f, 2.6f));
                logs.Loft(new[] { new Vector3(gx, 0, z), new Vector3(gx + .1f, 7, z), new Vector3(gx, 13.5f, z) }, new[] { new Vector2(.78f, .78f), new Vector2(.66f, .66f), new Vector2(.6f, .6f) }, 4, 14, false, true);
                bone.Spike(new Vector3(gx, 13.4f, z), new Vector3(0, 1f, Mathf.Sign(z) * .55f), .32f, 2.4f, 10);
                Fx.Torch(parent, new Vector3(gx - .75f, 6.5f, z), Vector3.left, ta != null ? ta.woodStructure : null, iron, ta != null ? ta.fire : null, true);
                Fx.Torch(parent, new Vector3(gx + .75f, 6.5f, z), Vector3.right, ta != null ? ta.woodStructure : null, iron, ta != null ? ta.fire : null, false);
            }
            logs.Loft(new[] { new Vector3(gx, 11.6f, -6.2f), new Vector3(gx, 11.8f, 0), new Vector3(gx, 11.6f, 6.2f) }, new[] { new Vector2(.55f, .55f), new Vector2(.6f, .6f), new Vector2(.55f, .55f) }, 4, 12, true, true);
            logs.Loft(new[] { new Vector3(gx, 9.8f, -5.2f), new Vector3(gx, 9.8f, 5.2f) }, new[] { new Vector2(.4f, .4f), new Vector2(.4f, .4f) }, 2, 10, true, true);
            wood.Box(new Vector3(gx - .35f, 10.75f, 0), Quaternion.Euler(0, 0, 2), new Vector3(.25f, 1.5f, 6.6f));
            foreach (float z in new[] { -2.2f, 0f, 2.2f }) bone.Spike(new Vector3(gx - .5f, 10.75f, z), Vector3.left, .14f, .5f, 6);
            // Lianen am Tor
            for (int k = 0; k < 6; k++)
            {
                float z = Random.Range(-5f, 5f), L = Random.Range(2.5f, 5f);
                logs.Loft(new[] { new Vector3(gx, 11.6f, z), new Vector3(gx + .2f, 11.6f - L * .6f, z + .2f), new Vector3(gx + .1f, 11.6f - L, z) },
                          new[] { new Vector2(.04f, .04f), new Vector2(.035f, .035f), new Vector2(.02f, .02f) }, 4, 5, false, true);
            }
            wood.ToObject("Bahnsteig", ta != null ? ta.woodDeck : null, parent);
            logs.ToObject("Staemme", ta != null ? ta.palmBark : null, parent);
            rock.ToObject("Felssockel", ta != null ? ta.rockWall : null, parent);
            bone.ToObject("Hoerner", Lit(new Color(.86f, .82f, .7f), .4f), parent);
            clr.Block(new Rect(-34, -7, 44, 14));
        }

        // ---------- See ----------
        public static void Lake(TrackDef def, Transform parent, ThemeAssets ta, Scenery.Clearance clr, Ground g)
        {
            if (!g.hasLake) return;
            var mb = new MeshBuilder();
            mb.Disc(new Vector3(g.lakeC.x, Ground.WaterLevel, g.lakeC.y), Mathf.Max(g.lakeR.x, g.lakeR.y) * 1.45f, 96, up);
            var w = mb.ToObject("See", ta != null && ta.water != null ? ta.water : Lit(new Color(.1f, .25f, .25f), .95f), parent, false);
            w.GetComponent<MeshRenderer>().receiveShadows = true;
            // Schilf und Steine am Ufer
            var reeds = new MeshBuilder();
            for (int k = 0; k < 2600; k++)
            {
                float a = Random.Range(0f, Mathf.PI * 2), e = Random.Range(.88f, 1.16f);
                float x = g.lakeC.x + Mathf.Cos(a) * g.lakeR.x * e, z = g.lakeC.y + Mathf.Sin(a) * g.lakeR.y * e;
                float y = g.Height(x, z);
                if (y < Ground.WaterLevel - .7f || y > .4f) continue;
                if (!clr.Free(new Vector2(x, z), 1.5f)) continue;
                float hgt = Random.Range(1.2f, 2.4f), rot = Random.Range(0f, 180f);
                for (int q = 0; q < 2; q++)
                {
                    float r = (rot + q * 90) * Mathf.Deg2Rad;
                    reeds.Card(new Vector3(x, y - .1f, z), up, new Vector3(Mathf.Cos(r), 0, Mathf.Sin(r)), hgt, .7f, up);
                }
            }
            if (ta != null && ta.grassCard != null) reeds.ToObject("Schilf", Tint(ta.grassCard, new Color(.75f, .85f, .6f)), parent, false);
            if (ta != null && ta.rocks != null && ta.rocks.Length > 0)
                for (int k = 0; k < 30; k++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2), e = Random.Range(.9f, 1.1f);
                    float x = g.lakeC.x + Mathf.Cos(a) * g.lakeR.x * e, z = g.lakeC.y + Mathf.Sin(a) * g.lakeR.y * e;
                    if (!clr.Free(new Vector2(x, z), 3f)) continue;
                    var go = Object.Instantiate(ta.rocks[Random.Range(0, ta.rocks.Length)], new Vector3(x, g.Height(x, z) - .4f, z), Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
                    go.transform.localScale *= Random.Range(1.5f, 3.5f);
                }
        }

        // ---------- Pflanzen ----------
        static void Frond(MeshBuilder f, Vector3 o, Vector3 outw, float len, float width, float lift)
        {
            Vector3 side = Vector3.Cross(up, outw).normalized;
            Vector3 d1 = (outw + up * lift).normalized, d2 = (outw - up * .55f).normalized;
            Vector3 nor = (outw * .3f + up).normalized;
            f.Card(o, d1, side, len * .5f, width, nor);
            f.Card(o + d1 * len * .5f, d2, side, len * .55f, width * .85f, nor);
        }

        static void Palm(MeshBuilder trunk, MeshBuilder fronds, Vector3 at, float H)
        {
            float a = Random.Range(0f, Mathf.PI * 2); Vector3 bend = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(.08f, .25f) * H;
            Vector3 top = at + bend + up * H;
            trunk.Loft(new[] { at, at + bend * .15f + up * H * .35f, at + bend * .5f + up * H * .7f, top },
                       new[] { new Vector2(.4f, .4f), new Vector2(.28f, .28f), new Vector2(.24f, .24f), new Vector2(.21f, .21f) }, 5, 10, false, true);
            int n = 13; float off = Random.value * 6;
            for (int k = 0; k < n; k++)
            {
                float b = off + k / (float)n * Mathf.PI * 2;
                Frond(fronds, top, new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)), Random.Range(4.5f, 6.5f), 1.2f, Random.Range(.25f, .9f));
            }
        }

        static void TreeFern(MeshBuilder trunk, MeshBuilder fronds, Vector3 at, float H)
        {
            trunk.Taper(at, .25f, .17f, H, 8);
            Vector3 top = at + up * H; float off = Random.value * 6;
            for (int k = 0; k < 14; k++) { float b = off + k / 14f * Mathf.PI * 2; Frond(fronds, top, new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)), Random.Range(2.6f, 3.6f), 1.05f, .45f); }
        }

        static void Cycad(MeshBuilder trunk, MeshBuilder fronds, Vector3 at)
        {
            float H = Random.Range(.8f, 1.8f);
            trunk.Taper(at, .45f, .34f, H, 9);
            Vector3 top = at + up * H; float off = Random.value * 6;
            for (int k = 0; k < 14; k++)
            {
                float b = off + k / 14f * Mathf.PI * 2; Vector3 o = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                fronds.Card(top, (o + up * .9f).normalized, Vector3.Cross(up, o), Random.Range(1.4f, 1.9f), .7f, (o * .3f + up).normalized);
            }
        }

        // Urwaldriese: Stamm mit Brettwurzeln, Aeste, Blaetterbuesche, Lianen
        static void GiantTree(MeshBuilder bark, MeshBuilder leaves, MeshBuilder vines, Vector3 at, float H, Ground g)
        {
            float a0 = Random.Range(0f, 6.28f); Vector3 lean = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * H * .06f;
            Vector3 top = at + up * H + lean;
            bark.Loft(new[] { at - up * .5f, at + up * H * .3f + lean * .2f, at + up * H * .7f + lean * .6f, top },
                      new[] { new Vector2(.95f, .95f), new Vector2(.6f, .6f), new Vector2(.45f, .45f), new Vector2(.3f, .3f) }, 5, 12, false, true);
            for (int k = 0; k < 5; k++)
            {
                float b = a0 + k / 5f * 6.28f + Random.Range(-.3f, .3f); Vector3 d = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                bark.Spike(at + d * .5f + up * 1.8f, (d * 1.2f + Vector3.down).normalized, 1.0f, 2.6f, 6, .14f, up);   // Brettwurzel
            }
            int nb = Random.Range(4, 7);
            for (int k = 0; k < nb; k++)
            {
                float b = a0 + k / (float)nb * 6.28f + Random.Range(-.4f, .4f); Vector3 d = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                float hb = H * Random.Range(.68f, .92f), L = Random.Range(4f, 7f);
                Vector3 s = at + up * hb + lean * (hb / H), e = s + d * L + up * L * .45f;
                bark.Loft(new[] { s, s + d * L * .5f + up * L * .3f, e }, new[] { new Vector2(.28f, .28f), new Vector2(.18f, .18f), new Vector2(.1f, .1f) }, 4, 8, false, true);
                // Blaetterbusch am Astende
                for (int c = 0; c < 16; c++)
                {
                    Vector3 rd = Random.onUnitSphere; rd.y = Mathf.Abs(rd.y) * .6f + .1f; rd.Normalize();
                    Vector3 o = e + Random.insideUnitSphere * 1.6f;
                    leaves.Card(o, rd, Vector3.Cross(rd, Random.onUnitSphere).normalized, Random.Range(2.2f, 3.2f), Random.Range(2.0f, 2.8f), (up + rd * .4f).normalized);
                }
                if (Random.value < .7f)
                {
                    float drop = e.y - g.Height(e.x, e.z) - .2f;
                    vines.Loft(new[] { e, e + d * .6f + Vector3.down * drop * .55f, e + d * .3f + Vector3.down * drop }, new[] { new Vector2(.05f, .05f), new Vector2(.045f, .045f), new Vector2(.035f, .035f) }, 5, 5, false, true);
                }
            }
            for (int c = 0; c < 12; c++)
            {
                Vector3 rd = Random.onUnitSphere; rd.y = Mathf.Abs(rd.y); rd.Normalize();
                leaves.Card(top + Random.insideUnitSphere * 1.8f, rd, Vector3.Cross(rd, Random.onUnitSphere).normalized, 2.8f, 2.6f, (up + rd * .4f).normalized);
            }
        }

        static void FernClump(MeshBuilder fronds, Vector3 at, float s)
        {
            float off = Random.value * 6;
            for (int k = 0; k < 7; k++)
            {
                float b = off + k / 7f * Mathf.PI * 2; Vector3 o = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                fronds.Card(at, (o + up * Random.Range(.5f, 1.1f)).normalized, Vector3.Cross(up, o), Random.Range(.8f, 1.4f) * s, .55f * s, (o * .3f + up).normalized);
            }
        }

        public static void Jungle(Transform parent, ThemeAssets ta, Scenery.Clearance clr, Ground g, Vector3 center, float radius, Track tr)
        {
            if (ta == null) return;
            var palmTrunk = new MeshBuilder { tile = 1.2f }; var fernTrunk = new MeshBuilder { tile = 1.2f }; var fronds = new MeshBuilder();
            var bark = new MeshBuilder { tile = 1.5f }; var leaves = new MeshBuilder(); var vines = new MeshBuilder { tile = 1f };
            bool Ok(Vector2 p, float d) => clr.Free(p, d) && g.LakeE(p.x, p.y) > 1.12f && g.Height(p.x, p.y) < 24f;

            // Urwaldriesen
            int giants = 0;
            for (int tries = 0; tries < 3000 && giants < 120; tries++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), dist = Mathf.Sqrt(Random.value) * radius * 2.1f;
                Vector2 p = new Vector2(center.x + Mathf.Cos(ang) * dist, center.z + Mathf.Sin(ang) * dist);
                if (!Ok(p, 12f)) continue;
                GiantTree(bark, leaves, vines, new Vector3(p.x, g.Height(p.x, p.y), p.y), Random.Range(18f, 30f), g);
                g.forestSpots.Add(new Vector3(p.x, p.y, 8f));
                clr.Block(new Rect(p.x - 4f, p.y - 4f, 8f, 8f));
                giants++;
            }
            // Palmen, Baumfarne, Palmfarne, dicht
            int put = 0;
            for (int tries = 0; tries < 9000 && put < 700; tries++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), dist = Mathf.Sqrt(Random.value) * radius * 2.1f;
                Vector2 p = new Vector2(center.x + Mathf.Cos(ang) * dist, center.z + Mathf.Sin(ang) * dist);
                if (!Ok(p, 6f)) continue;
                Vector3 at = new Vector3(p.x, g.Height(p.x, p.y) - .1f, p.y);
                float r = Random.value;
                if (r < .55f) Palm(palmTrunk, fronds, at, Random.Range(9f, 19f));
                else if (r < .85f) TreeFern(fernTrunk, fronds, at, Random.Range(3f, 6.5f));
                else Cycad(palmTrunk, fronds, at);
                g.forestSpots.Add(new Vector3(p.x, p.y, 4f));
                clr.Block(new Rect(p.x - 1.6f, p.y - 1.6f, 3.2f, 3.2f));
                put++;
            }
            // Bodenfarne entlang der Bahn
            int step = Mathf.Max(1, Mathf.RoundToInt(2.5f / (float)tr.ds));
            for (int i = 0; i < tr.n; i += step)
            {
                var c = tr.pos[i].ToUnity();
                for (int k = 0; k < 6; k++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2), d = Random.Range(3f, 28f);
                    Vector2 p = new Vector2(c.x + Mathf.Cos(ang) * d, c.z + Mathf.Sin(ang) * d);
                    if (!clr.Free(p, 2.6f) || g.LakeE(p.x, p.y) < 1.05f) continue;
                    FernClump(fronds, new Vector3(p.x, g.Height(p.x, p.y) - .05f, p.y), Random.Range(.8f, 1.6f));
                }
            }
            palmTrunk.ToObject("Palmstaemme", ta.palmBark, parent);
            fernTrunk.ToObject("Farnstaemme", ta.treeBark, parent);
            fronds.ToObject("Wedel", ta.frond, parent);
            bark.ToObject("Urwaldstaemme", Tint(ta.treeBark, new Color(.7f, .66f, .6f)), parent);
            leaves.ToObject("Blaetterdach", ta.leaves, parent);
            vines.ToObject("Lianen", Tint(ta.palmBark, new Color(.55f, .6f, .45f)), parent);
            if (ta.rocks != null && ta.rocks.Length > 0)
                for (int k = 0; k < 45; k++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2), dist = Mathf.Sqrt(Random.value) * radius * 1.7f;
                    Vector2 p = new Vector2(center.x + Mathf.Cos(ang) * dist, center.z + Mathf.Sin(ang) * dist);
                    if (!Ok(p, 5f)) continue;
                    var go = Object.Instantiate(ta.rocks[Random.Range(0, ta.rocks.Length)], new Vector3(p.x, g.Height(p.x, p.y) - .3f, p.y), Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
                    go.transform.localScale *= Random.Range(1.2f, 3.2f);
                }
        }

        // ---------- Dinosaurier ----------
        static void Put(GameObject d, Ground g, Scenery.Clearance clr, Vector3 pos, float yaw, float scale, float block)
        {
            pos.y = g.Height(pos.x, pos.z);
            d.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            d.transform.localScale = Vector3.one * scale;
            clr.Block(new Rect(pos.x - block, pos.z - block, block * 2, block * 2));
        }

        // yaw: Schnauze (lokal +x) zeigt nach (cos yaw, 0, -sin yaw); yaw -90 = Richtung +z, 90 = Richtung -z
        static float Face(Vector3 from, Vector3 to) { Vector3 d = to - from; return Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg; }

        public static void Dinos(Transform parent, ThemeAssets ta, Scenery.Clearance clr, Ground g, Vector3 center)
        {
            Vector3 lake = new Vector3(g.lakeC.x, 0, g.lakeC.y);
            // Stegosaurus in der Mitte der Anlage, schaut zur Abschussgeraden
            Vector3 st = new Vector3(center.x, 0, center.z + 2);
            Put(Creatures.Stego(parent, ta, new Color(.52f, .5f, .34f), new Color(.62f, .36f, .24f)), g, clr, st, -90, 1.15f, 8);
            Put(Creatures.Stego(parent, ta, new Color(.46f, .47f, .32f), new Color(.58f, .34f, .22f)), g, clr, new Vector3(center.x - 16, 0, center.z - 6), -60, .6f, 5);
            // T. rex am Looping, Jungtier dahinter
            Put(Creatures.TRex(parent, ta, new Color(.55f, .47f, .34f)), g, clr, new Vector3(120, 0, 15.5f), 95, 1.0f, 9);
            Put(Creatures.TRex(parent, ta, new Color(.42f, .45f, .33f)), g, clr, new Vector3(170, 0, 30f), 150, .55f, 5);
            // Brachiosaurier waten im See, einer am Ufer
            if (g.hasLake)
            {
                Vector3 b1 = new Vector3(g.lakeC.x - g.lakeR.x * .7f, 0, g.lakeC.y - g.lakeR.y * .2f), b2 = new Vector3(g.lakeC.x + g.lakeR.x * .62f, 0, g.lakeC.y + g.lakeR.y * .3f);
                Put(Creatures.Brachio(parent, ta, new Color(.5f, .54f, .5f)), g, clr, b1, Face(b1, lake) + 40, 1f, 10);
                Put(Creatures.Brachio(parent, ta, new Color(.46f, .5f, .44f)), g, clr, b2, Face(b2, lake) - 50, .92f, 10);
                Vector3 b3 = new Vector3(g.lakeC.x + 20, 0, g.lakeC.y + g.lakeR.y * 1.25f);
                Put(Creatures.Brachio(parent, ta, new Color(.55f, .55f, .5f)), g, clr, b3, Face(b3, lake), .85f, 12);
                // Parasaurolophus-Herde trinkt am Ufer zur Bahnseite
                for (int i = 0; i < 6; i++)
                {
                    float a = Mathf.Lerp(-2.25f, -1.25f, i / 5f);
                    Vector3 pp = new Vector3(g.lakeC.x + Mathf.Cos(a) * g.lakeR.x * 1.02f, 0, g.lakeC.y + Mathf.Sin(a) * g.lakeR.y * 1.02f);
                    Put(Creatures.Para(parent, ta, i % 2 == 0 ? new Color(.58f, .5f, .36f) : new Color(.5f, .48f, .38f)), g, clr, pp, Face(pp, lake) + Random.Range(-20, 20), Random.Range(.8f, 1.05f), 5);
                }
                // Spinosaurus fischt am Ostufer
                Vector3 sp = new Vector3(g.lakeC.x + g.lakeR.x * .93f, 0, g.lakeC.y - g.lakeR.y * .25f);
                Put(Creatures.Spino(parent, ta, new Color(.44f, .42f, .36f), new Color(.6f, .28f, .2f)), g, clr, sp, Face(sp, lake), 1.0f, 10);
                // Plesiosaurus schwimmt
                Creatures.Plesio(parent, ta, new Color(.36f, .44f, .44f), lake, new Vector2(g.lakeR.x * .5f, g.lakeR.y * .45f), Ground.WaterLevel);
            }
            // Triceratops-Gruppe
            Put(Creatures.Trike(parent, ta, new Color(.62f, .48f, .3f)), g, clr, new Vector3(-30, 0, 30), 200, 1f, 6);
            Put(Creatures.Trike(parent, ta, new Color(.55f, .44f, .3f)), g, clr, new Vector3(-16, 0, 24), 160, .85f, 6);
            Put(Creatures.Trike(parent, ta, new Color(.6f, .5f, .34f)), g, clr, new Vector3(-40, 0, 20), 240, .6f, 5);
            // Ankylosaurier hinter der Rueckfahrt
            Put(Creatures.Ankylo(parent, ta, new Color(.5f, .44f, .34f)), g, clr, new Vector3(150, 0, -70), 120, 1f, 6);
            Put(Creatures.Ankylo(parent, ta, new Color(.46f, .42f, .33f)), g, clr, new Vector3(178, 0, -62), 70, .85f, 6);
            // Raptoren-Rudel lauert neben dem Tor
            for (int i = 0; i < 6; i++)
            {
                Vector3 rp = new Vector3(-6 + i * 3.6f + Random.Range(-1f, 1f), 0, 12 + (i % 2) * 4 + Random.Range(-1f, 1f));
                Put(Creatures.Raptor(parent, ta, i % 3 == 0 ? new Color(.55f, .45f, .32f) : new Color(.48f, .42f, .32f)), g, clr, rp, Face(rp, new Vector3(0, 0, 0)) + Random.Range(-25, 25), 1.25f, 2.5f);
            }
            // Flugsaurier
            for (int i = 0; i < 8; i++)
                Creatures.Ptero(parent, ta, new Color(.6f, .38f, .28f), new Vector3(60 + i * 14, 0, 10 + i * 9), Random.Range(55f, 110f), Random.Range(36f, 75f), (i % 2 == 0 ? 1 : -1) * Random.Range(.05f, .1f));
        }

        // ---------- Vulkan: Lava, Glut, Rauch ----------
        public static void Volcano(TrackDef def, Transform parent, Ground g)
        {
            if (def.volcano == null) return;
            Vector3 c = new V3(def.volcano[0], 0, def.volcano[1]).ToUnity();
            float top = g.Height(c.x, c.z);
            var lava = Lit(new Color(1f, .35f, .08f), .6f);
            lava.EnableKeyword("_EMISSION"); lava.SetColor("_EmissionColor", new Color(6f, 1.6f, .25f));
            var mb = new MeshBuilder(); mb.Disc(new Vector3(c.x, top - 2.5f, c.z), def.volcano[2] * .09f, 32, up);
            mb.ToObject("Lava", lava, parent, false);
            var lg = new GameObject("Lavaglut"); lg.transform.SetParent(parent, false); lg.transform.position = new Vector3(c.x, top + 6, c.z);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 90; l.color = new Color(1f, .45f, .15f); l.intensity = 6; l.shadows = LightShadows.None;
            lg.AddComponent<Flicker>().baseIntensity = 6;
            var ta = ThemeAssets.Get(); if (ta == null || ta.smoke == null) return;
            var go = new GameObject("Vulkanrauch"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(new Vector3(c.x, top, c.z), Quaternion.Euler(-90, 0, 0));
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.prewarm = true; main.startLifetime = 40f; main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 24f); main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 160; main.startColor = new Color(.5f, .48f, .47f, .6f);
            var em = ps.emission; em.rateOverTime = 3.5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = 8;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(2.5f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(1f);
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 3.5f));
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(new Color(.4f, .38f, .36f), 0), new GradientColorKey(new Color(.8f, .79f, .78f), 1) },
                       new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.7f, .08f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ta.smoke;
            ps.Play();
        }
    }
}
