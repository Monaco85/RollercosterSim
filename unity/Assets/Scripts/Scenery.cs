using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>Holzgeruest, Burg, Dorf und Natur. Rein optisch, beruehrt weder Physik noch Sitz.</summary>
    public static class Scenery
    {
        static Material Tinted(Material m, Color c, float tileScale = 1f)
        {
            if (m == null) return null;
            var x = new Material(m); x.SetColor("_BaseColor", c);
            if (tileScale != 1f) x.mainTextureScale = new Vector2(tileScale, tileScale);
            return x;
        }

        static Material Plain(Color c, float smooth = .3f, float metal = 0f, bool twoSided = false)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (twoSided) m.SetFloat("_Cull", 0);
            return m;
        }

        // ---------- Liftkette (fuer alle Bahnen) ----------
        public static void LiftChain(Track tr, double from, double to, Transform parent)
        {
            var mb = new MeshBuilder();
            for (double s = from; s < to; s += .5)
            {
                var a = tr.At(s); var b = tr.At(s + .5);
                Vector3 pa = (a.p - a.U * .03).ToUnity(), pb = (b.p - b.U * .03).ToUnity(), u = a.U.ToUnity();
                mb.Box((pa + pb) * .5f, Quaternion.LookRotation(pb - pa, u), new Vector3(.1f, .05f, .52f));
                if (((int)(s * 2)) % 4 == 0) mb.Box(pa + u * .05f, Quaternion.LookRotation(pb - pa, u), new Vector3(.16f, .08f, .08f)); // Mitnehmer
            }
            mb.ToObject("Liftkette", Plain(new Color(.12f, .12f, .13f), .5f, .8f), parent);
        }

        // ---------- Laufsteg mit Gelaender (Holzbahn) ----------
        static void Walkway(Track tr, Transform parent, ThemeAssets ta, System.Func<Vector3, bool> skip)
        {
            var mb = new MeshBuilder { tile = 2f };
            int step = Mathf.Max(1, Mathf.RoundToInt(1.2f / (float)tr.ds));
            for (int i = 0; i < tr.n; i += step)
            {
                int j = (i + step) % tr.n;
                Vector3 a = tr.pos[i].ToUnity(), b = tr.pos[j].ToUnity(), u = tr.U[i].ToUnity(), r = tr.R[i].ToUnity();
                if (u.y < .9f || skip(a) || a.y < 1.5f) continue;
                var q = Quaternion.LookRotation(b - a, u);
                Vector3 mid = (a + b) * .5f;
                mb.Box(mid + r * 1.35f - u * .3f, q, new Vector3(.7f, .06f, (b - a).magnitude + .02f));      // Bohlen
                mb.Box(mid + r * 1.72f + u * .62f, q, new Vector3(.07f, .07f, (b - a).magnitude + .02f));    // Handlauf
                mb.Box(mid + r * 1.72f + u * .2f, q, new Vector3(.05f, .05f, (b - a).magnitude + .02f));     // Knieleiste
                if ((i / step) % 2 == 0) mb.Box(a + r * 1.72f + u * .16f, q, new Vector3(.08f, .95f, .08f)); // Pfosten
            }
            mb.ToObject("Laufsteg", ta != null ? ta.woodStructure : null, parent);
        }

        static Material steelMat;
        public static Material Steel()
        {
            if (steelMat != null) return steelMat;
            var b = Resources.Load<Material>("AchterbahnLit");
            steelMat = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            steelMat.SetColor("_BaseColor", new Color(.62f, .63f, .66f)); steelMat.SetFloat("_Metallic", .55f); steelMat.SetFloat("_Smoothness", .5f);
            return steelMat;
        }

        // ---------- Hilfsdaten: Abstand zur Bahn ----------
        public class Clearance
        {
            readonly List<Vector2> pts = new List<Vector2>();
            readonly List<Rect> blocked = new List<Rect>();
            public Clearance(Track tr)
            {
                int step = Mathf.Max(1, Mathf.RoundToInt(2f / (float)tr.ds));
                for (int i = 0; i < tr.n; i += step) { var p = tr.pos[i].ToUnity(); pts.Add(new Vector2(p.x, p.z)); }
            }
            public void Block(Rect r) => blocked.Add(r);
            public bool Free(Vector2 p, float dist)
            {
                foreach (var r in blocked) if (r.Contains(p)) return false;
                float d2 = dist * dist;
                foreach (var q in pts) if ((q - p).sqrMagnitude < d2) return false;
                return true;
            }
        }

        // ---------- Holzachterbahn: Fahrbahn, Stahlbaender, Geruest ----------
        public static void WoodTrack(Track tr, Transform parent, ThemeAssets ta, System.Func<Vector3, bool> skipBent)
        {
            var deck = new MeshBuilder { tile = 2f };
            var rails = new MeshBuilder { tile = 1f };
            int stride = 3;
            for (int i = 0; i < tr.n; i += stride)
            {
                int j = (i + stride) % tr.n;
                Vector3 a = tr.pos[i].ToUnity(), b = tr.pos[j].ToUnity();
                Vector3 u = tr.U[i].ToUnity(), r = tr.R[i].ToUnity();
                Vector3 mid = (a + b) * .5f; float len = (b - a).magnitude + .03f;
                var q = Quaternion.LookRotation(b - a, u);
                deck.Box(mid - u * .2f, q, new Vector3(1.8f, .3f, len));
                rails.Box(mid + r * .55f - u * .02f, q, new Vector3(.16f, .08f, len));
                rails.Box(mid - r * .55f - u * .02f, q, new Vector3(.16f, .08f, len));
                // Seitenbretter (Laufsteg-Kante)
                deck.Box(mid + r * .98f + u * .05f, q, new Vector3(.08f, .35f, len));
                deck.Box(mid - r * .98f + u * .05f, q, new Vector3(.08f, .35f, len));
            }
            deck.ToObject("Fahrbahn", ta != null ? ta.woodDeck : null, parent);
            rails.ToObject("Stahlbaender", Steel(), parent);

            // Geruest: Boecke alle 2,4 m mit Pfosten, Riegeln, Kreuzstreben und Laengsholmen
            var g = new MeshBuilder { tile = 2.5f };
            int bstep = Mathf.Max(1, Mathf.RoundToInt(2.4f / (float)tr.ds));
            bool havePrev = false; Vector3 pL0 = default, pR0 = default, pLt = default, pRt = default; Vector3 prevPos = default;
            for (int i = 0; i < tr.n; i += bstep)
            {
                Vector3 p = tr.pos[i].ToUnity(), u = tr.U[i].ToUnity(), t = tr.T[i].ToUnity();
                if (u.y < .6f || skipBent(p)) { havePrev = false; continue; }
                Vector3 th = new Vector3(t.x, 0, t.z).normalized; Vector3 rh = Vector3.Cross(Vector3.up, th);
                float top = p.y - .42f; if (top < .6f) { havePrev = false; continue; }
                float spread = 1.0f + top * .06f;
                Vector3 Lt = new Vector3(p.x, top, p.z) - rh * 1.0f, Rt = new Vector3(p.x, top, p.z) + rh * 1.0f;
                Vector3 Lb = new Vector3(p.x, 0, p.z) - rh * spread, Rb = new Vector3(p.x, 0, p.z) + rh * spread;
                g.Beam(Lb, Lt, .24f, th); g.Beam(Rb, Rt, .24f, th);
                g.Beam(Lt, Rt, .22f, th);
                float h0 = 0;
                for (float h = 2.6f; h < top - .4f; h += 2.6f)
                {
                    Vector3 l1 = Vector3.Lerp(Lb, Lt, h / top), r1 = Vector3.Lerp(Rb, Rt, h / top);
                    Vector3 l0 = Vector3.Lerp(Lb, Lt, h0 / top), r0 = Vector3.Lerp(Rb, Rt, h0 / top);
                    g.Beam(l1, r1, .16f, th);
                    if (((int)(h / 2.6f)) % 2 == 0) g.Beam(l0, r1, .12f, th); else g.Beam(r0, l1, .12f, th);
                    h0 = h;
                }
                if (havePrev && (p - prevPos).magnitude < 4.5f)
                {
                    float hmax = Mathf.Min(top, Mathf.Min(pLt.y, pRt.y));
                    for (float h = 2.6f; h < hmax - .3f; h += 5.2f)
                    {
                        g.Beam(Vector3.Lerp(pL0, pLt, h / pLt.y), Vector3.Lerp(Lb, Lt, h / top), .14f, Vector3.up);
                        g.Beam(Vector3.Lerp(pR0, pRt, h / pRt.y), Vector3.Lerp(Rb, Rt, h / top), .14f, Vector3.up);
                    }
                    g.Beam(pLt, Lt, .2f, Vector3.up); g.Beam(pRt, Rt, .2f, Vector3.up);
                }
                havePrev = true; pL0 = Lb; pR0 = Rb; pLt = Lt; pRt = Rt; prevPos = p;
            }
            g.ToObject("Holzgeruest", ta != null ? ta.woodStructure : null, parent);
            Walkway(tr, parent, ta, skipBent);
        }

        // ---------- Burg ----------
        static void Merlons(MeshBuilder m, Vector3 a, Vector3 b, float y, float thick)
        {
            Vector3 d = b - a; float len = d.magnitude; Vector3 f = d / len; var q = Quaternion.LookRotation(f, Vector3.up);
            for (float s = .6f; s < len - .4f; s += 2f) m.Box(a + f * s + Vector3.up * (y + .6f), q, new Vector3(thick, 1.2f, 1f));
        }

        static void Wall(MeshBuilder m, Vector3 a, Vector3 b, float h, float thick, bool crenel = true)
        {
            Vector3 d = b - a; float len = d.magnitude; var q = Quaternion.LookRotation(d / len, Vector3.up);
            m.Box((a + b) * .5f + Vector3.up * (h / 2), q, new Vector3(thick, h, len));
            if (crenel) Merlons(m, a, b, h, thick);
        }

        static void Tower(MeshBuilder stone, MeshBuilder roof, MeshBuilder flags, Vector3 c, float r, float h, float roofH)
        {
            stone.Cylinder(c, r, h, 22, true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                stone.Box(c + d * (r + .15f) + Vector3.up * (h + .5f), Quaternion.LookRotation(d), new Vector3(1.1f, 1f, .7f));
            }
            roof.Cone(c + Vector3.up * (h + 1f), r + .6f, roofH, 22);
            Vector3 tip = c + Vector3.up * (h + 1f + roofH);
            flags.Box(tip + Vector3.up * 1.5f, Quaternion.identity, new Vector3(.08f, 3f, .08f));
            flags.Box(tip + new Vector3(.9f, 2.4f, 0), Quaternion.identity, new Vector3(1.6f, 1f, .04f));
        }

        public static void Castle(TrackDef def, Track tr, Transform parent, ThemeAssets ta, Clearance clr)
        {
            var stone = new MeshBuilder { tile = 3f };
            var blocks = new MeshBuilder { tile = 3f };
            var roof = new MeshBuilder { tile = 2.5f };
            var flags = new MeshBuilder { tile = 1f };
            var cobble = new MeshBuilder { tile = 2f };

            // Torhaus ueber der Station (Bahn laeuft hier bei y = 4 entlang x, z = 0)
            float x0 = -14, x1 = 14, zw = 5.5f, h = 11f;
            Wall(stone, new Vector3(x0, 0, -zw), new Vector3(x1, 0, -zw), h, 2f);
            Wall(stone, new Vector3(x0, 0, zw), new Vector3(x1, 0, zw), h, 2f);
            foreach (float x in new[] { x0, x1 })
            {
                Wall(stone, new Vector3(x, 0, -zw), new Vector3(x, 0, -2f), h, 2f, false);
                Wall(stone, new Vector3(x, 0, 2f), new Vector3(x, 0, zw), h, 2f, false);
                stone.Box(new Vector3(x, 9.5f, 0), Quaternion.identity, new Vector3(2f, 3f, 4f)); // Torbogen-Sturz
                Merlons(stone, new Vector3(x, 0, -zw), new Vector3(x, 0, zw), h, 2f);
            }
            foreach (float x in new[] { x0, x1 }) foreach (float z in new[] { -zw, zw }) Tower(stone, roof, flags, new Vector3(x, 0, z), 2.8f, 15f, 6.5f);
            // Bahnsteig aus Kopfstein unter der Station
            cobble.Box(new Vector3(2, 1.75f, 0), Quaternion.identity, new Vector3(46f, 3.5f, 7f));
            clr.Block(new Rect(x0 - 5, -zw - 5, (x1 - x0) + 10, 2 * zw + 10));
            clr.Block(new Rect(-24, -4, 50, 8));

            // Bergfried mit Ringmauer in der Spirale
            if (def.castleSpots != null)
                foreach (var spot in def.castleSpots)
                {
                    Vector3 c = new V3(spot.x, 0, spot.z).ToUnity();
                    var q = Quaternion.Euler(0, 45, 0);
                    blocks.Box(c + Vector3.up * 13f, q, new Vector3(10f, 26f, 10f));
                    for (int s = 0; s < 4; s++)
                    {
                        Vector3 e0 = c + q * (Quaternion.Euler(0, 90 * s, 0) * new Vector3(-5, 0, 5)), e1 = c + q * (Quaternion.Euler(0, 90 * s, 0) * new Vector3(5, 0, 5));
                        Merlons(blocks, e0 + Vector3.up * 0, e1, 26f, .8f);
                    }
                    roof.Cone(c + Vector3.up * 26.5f, 7.6f, 9f, 4);
                    flags.Box(c + Vector3.up * 37f, Quaternion.identity, new Vector3(.1f, 4f, .1f));
                    flags.Box(c + new Vector3(1.2f, 38.2f, 0), Quaternion.identity, new Vector3(2.2f, 1.4f, .05f));
                    const int seg = 14; float rr = 10.5f;
                    for (int k = 0; k < seg; k++)
                    {
                        float a0 = k / (float)seg * Mathf.PI * 2, a1 = (k + 1) / (float)seg * Mathf.PI * 2;
                        if (k == 3) continue; // Tor in der Ringmauer
                        Wall(stone, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr, 4.5f, 1.2f);
                    }
                    clr.Block(new Rect(c.x - 13, c.z - 13, 26, 26));
                }

            // ---- Details: Schiessscharten, Banner, Fallgitter, Torfluegel, Wassergraben, Bergfried-Fenster ----
            var slits = new MeshBuilder(); var banner = new MeshBuilder(); var gold = new MeshBuilder(); var iron = new MeshBuilder();
            var gate = new MeshBuilder { tile = 2f }; var water = new MeshBuilder { tile = 6f };
            foreach (float z in new[] { -zw, zw })
            {
                float sg = Mathf.Sign(z);
                for (float x = x0 + 3; x <= x1 - 3; x += 3.5f) slits.Box(new Vector3(x, 6.6f, z + sg * 1.03f), Quaternion.identity, new Vector3(.28f, 1.3f, .06f));
                foreach (float x in new[] { -7f, 7f })
                {
                    banner.Box(new Vector3(x, 8.2f, z + sg * 1.05f), Quaternion.identity, new Vector3(1.5f, 3.4f, .04f));
                    gold.Box(new Vector3(x, 9.95f, z + sg * 1.08f), Quaternion.identity, new Vector3(1.8f, .12f, .12f));
                    gold.Box(new Vector3(x, 8.5f, z + sg * 1.08f), Quaternion.identity, new Vector3(.55f, .7f, .03f));
                }
            }
            foreach (float x in new[] { x0, x1 }) foreach (float z in new[] { -zw, zw })
                for (int k = 0; k < 4; k++)
                {
                    float a = (k * 90 + 45) * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    slits.Box(new Vector3(x, 0, z) + d * 2.82f + Vector3.up * 9.5f, Quaternion.LookRotation(d), new Vector3(.26f, 1.2f, .06f));
                }
            foreach (float x in new[] { x0, x1 })
            {
                float inward = x < 0 ? 1 : -1;
                // Fallgitter hochgezogen im Torbogen
                for (float z = -1.85f; z <= 1.86f; z += .37f) iron.Box(new Vector3(x + inward * .3f, 8.25f, z), Quaternion.identity, new Vector3(.08f, 2.1f, .08f));
                foreach (float y in new[] { 7.4f, 8.2f, 9.0f }) iron.Box(new Vector3(x + inward * .3f, y, 0), Quaternion.identity, new Vector3(.07f, .07f, 3.8f));
                // offene Torfluegel an der Innenseite
                foreach (float z in new[] { -2.12f, 2.12f })
                {
                    gate.Box(new Vector3(x + inward * 2.1f, 5.5f, z), Quaternion.identity, new Vector3(2.0f, 3.9f, .14f));
                    foreach (float y in new[] { 4.3f, 6.7f }) iron.Box(new Vector3(x + inward * 2.1f, y, z + Mathf.Sign(z) * .08f), Quaternion.identity, new Vector3(1.9f, .1f, .03f));
                }
            }
            if (def.castleSpots != null)
                foreach (var spot in def.castleSpots)
                {
                    Vector3 c = new V3(spot.x, 0, spot.z).ToUnity();
                    water.Annulus(c + Vector3.up * .12f, 11.5f, 13.7f, 56);
                    const int seg = 20;
                    for (int k = 0; k < seg; k++)
                        foreach (float rr in new[] { 11.3f, 13.9f })
                        {
                            float a0 = k / (float)seg * Mathf.PI * 2, a1 = (k + 1) / (float)seg * Mathf.PI * 2;
                            Wall(stone, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr, .45f, .4f, false);
                        }
                    var q = Quaternion.Euler(0, 45, 0);
                    for (int s = 0; s < 4; s++)
                    {
                        Vector3 nrm = q * (Quaternion.Euler(0, 90 * s, 0) * Vector3.forward);
                        foreach (float wy in new[] { 9f, 15f, 21f }) slits.Box(c + nrm * 5.04f + Vector3.up * wy,Quaternion.LookRotation(nrm), new Vector3(.45f, 1.4f, .06f));
                        if (s == 0) gate.Box(c + nrm * 5.06f + Vector3.up * 1.4f, Quaternion.LookRotation(nrm), new Vector3(1.6f, 2.8f, .1f));
                    }
                }
            slits.ToObject("Schiessscharten", Plain(new Color(.03f, .03f, .035f), .2f), parent, false);
            var bannerMat = Plain(def.accent * .85f, .15f, 0, true);
            banner.ToObject("Banner", bannerMat, parent);
            gold.ToObject("Wappen", Plain(new Color(.78f, .6f, .2f), .6f, .8f), parent);
            iron.ToObject("Eisen", Plain(new Color(.1f, .1f, .11f), .45f, .85f), parent);
            gate.ToObject("Torfluegel", ta != null ? ta.woodStructure : null, parent);
            water.ToObject("Wassergraben", Plain(new Color(.07f, .13f, .12f), .95f, 0), parent, false);

            stone.ToObject("Burgmauern", ta != null ? ta.castleWall : null, parent);
            blocks.ToObject("Bergfried", ta != null ? Tinted(ta.stoneBlocks, new Color(.72f, .72f, .7f)) : null, parent);
            roof.ToObject("Turmdaecher", ta != null ? ta.roofSlate : null, parent);
            cobble.ToObject("Bahnsteig", ta != null ? ta.cobble : null, parent);
            var flagMat = new Material(Resources.Load<Material>("AchterbahnLit") ?? new Material(Shader.Find("Universal Render Pipeline/Lit")));
            flagMat.SetColor("_BaseColor", def.accent); flagMat.SetFloat("_Cull", 0);
            flags.ToObject("Fahnen", flagMat, parent);

            // Fackel-Laternen und Fass-Stapel am Bahnsteig
            if (ta != null)
            {
                if (ta.lantern != null)
                    for (float x = -18; x <= 22; x += 8)
                        foreach (float z in new[] { -3.2f, 3.2f })
                            Place(ta.lantern, parent, new Vector3(x, 3.5f, z), Random.Range(0, 360), 1f);
                if (ta.props != null && ta.props.Length > 0)
                    for (int k = 0; k < 8; k++)
                        Place(ta.props[k % ta.props.Length], parent, new Vector3(-30 + k * 1.6f + Random.Range(-.3f, .3f), 0, 8 + Random.Range(0f, 3f)), Random.Range(0, 360), 1f);
            }
        }

        // ---------- Dorf ----------
        public static void SmokeAt(Transform parent, Vector3 pos, Material mat) => Smoke(parent, pos, mat);
        public static void PlaceProp(GameObject prefab, Transform parent, Vector3 pos, float yaw) => Place(prefab, parent, pos, yaw, 1f);

        static void Smoke(Transform parent, Vector3 pos, Material mat)
        {
            if (mat == null) return;
            var go = new GameObject("Rauch");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, 0, 0));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = 8f; main.startSpeed = .7f;
            main.startSize = new ParticleSystem.MinMaxCurve(.7f, 1.3f); main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 70; main.gravityModifier = -.01f;
            main.startColor = new Color(.82f, .82f, .8f, .5f);
            var em = ps.emission; em.rateOverTime = 4f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = .15f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(.5f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(.25f);
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 4f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.9f, .9f, .9f), 1) },
                       new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.55f, .12f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
        }

        public static void Village(TrackDef def, Track tr, Transform parent, ThemeAssets ta, Clearance clr, Ground g, Vector3 worldCenter, float radius)
        {
            var walls = new MeshBuilder { tile = 3f };
            var thatch = new MeshBuilder { tile = 1.4f };
            var slate = new MeshBuilder { tile = 1.1f };
            var stone = new MeshBuilder { tile = 2f };
            var timber = new MeshBuilder { tile = 2f };
            var glass = new MeshBuilder();
            var clothR = new MeshBuilder(); var clothB = new MeshBuilder(); var clothG = new MeshBuilder();
            var hay = new MeshBuilder { tile = 1.5f };

            // Dorfplatz: freie Stelle nicht zu weit von der Bahn
            Vector2 vc = new Vector2(worldCenter.x, worldCenter.z);
            for (int k = 0; k < 400; k++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), d = Random.Range(radius * .4f, radius * 1.1f);
                var cand = new Vector2(worldCenter.x + Mathf.Cos(ang) * d, worldCenter.z + Mathf.Sin(ang) * d);
                if (clr.Free(cand, 26f)) { vc = cand; break; }
            }
            var houses = new List<Vector2>();
            int smokeLeft = 9;
            for (int tries = 0; tries < 1500 && houses.Count < 30; tries++)
            {
                bool farm = houses.Count >= 22;
                float ang = Random.Range(0f, Mathf.PI * 2), dist = farm ? Random.Range(radius * .5f, radius * 1.5f) : Random.Range(9f, 48f);
                Vector2 p = farm ? new Vector2(worldCenter.x + Mathf.Cos(ang) * dist, worldCenter.z + Mathf.Sin(ang) * dist) : vc + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                float w = Random.Range(5.5f, 8f), len = Random.Range(7f, 11f), hh = Random.Range(3.6f, 5.2f);
                if (!clr.Free(p, Mathf.Max(w, len) * .6f + 4f)) continue;
                float yaw = farm ? Random.Range(0f, 360f) : Mathf.Atan2(vc.x - p.x, vc.y - p.y) * Mathf.Rad2Deg + Random.Range(-15f, 15f);
                var q = Quaternion.Euler(0, yaw, 0);
                float y = g.Height(p.x, p.y);
                Vector3 c = new Vector3(p.x, y, p.y);
                Vector3 L(float x, float yy, float z) => c + q * new Vector3(x, yy, z);
                stone.Box(L(0, -.2f, 0), q, new Vector3(w + .3f, 1.6f, len + .3f));
                walls.Box(L(0, .6f + hh / 2, 0), q, new Vector3(w, hh, len));
                // Fachwerk-Balken an Ecken und als Mittelriegel
                foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                    timber.Box(L(sx * w / 2, .6f + hh / 2, sz * len / 2), q, new Vector3(.24f, hh, .24f));
                timber.Box(L(0, .6f + hh * .5f, len / 2 + .05f), q, new Vector3(w, .2f, .12f));
                timber.Box(L(0, .6f + hh * .5f, -len / 2 - .05f), q, new Vector3(w, .2f, .12f));
                // Fenster mit offenen Laeden an den Laengsseiten
                foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -.25f, .25f })
                {
                    Vector3 wp = L(sx * (w / 2 + .04f), .6f + hh * .62f, sz * len);
                    glass.Box(wp, q, new Vector3(.06f, .85f, .75f));
                    timber.Box(L(sx * (w / 2 + .07f), .6f + hh * .62f, sz * len + .62f), q, new Vector3(.05f, .9f, .42f));
                    timber.Box(L(sx * (w / 2 + .07f), .6f + hh * .62f, sz * len - .62f), q, new Vector3(.05f, .9f, .42f));
                    timber.Box(L(sx * (w / 2 + .06f), .6f + hh * .62f - .5f, sz * len), q, new Vector3(.12f, .08f, .95f));
                }
                // Tuer an der Giebelseite zum Platz
                timber.Box(L(0, .6f + 1.05f, len / 2 + .06f), q, new Vector3(1.15f, 2.1f, .1f));
                timber.Box(L(0, .6f + 2.2f, len / 2 + .1f), q, new Vector3(1.5f, .18f, .16f));
                var roof = Random.value < .6f ? thatch : slate;
                roof.Gable(L(0, .6f + hh, 0), q, w, len, w * .55f, .5f);
                Vector3 chimneyTop = L(w * .25f, .6f + hh + w * .45f + .9f, len * .3f);
                stone.Box(chimneyTop - Vector3.up * .9f, q, new Vector3(.8f, 1.8f, .8f));
                if (smokeLeft-- > 0) Smoke(parent, chimneyTop + Vector3.up * .2f, ta != null ? ta.smoke : null);
                // Zaun um manche Hoefe
                if (farm || Random.value < .3f)
                {
                    float fw = w / 2 + 3f, fl = len / 2 + 3f;
                    Vector3[] cs = { L(-fw, 0, -fl), L(fw, 0, -fl), L(fw, 0, fl), L(-fw, 0, fl) };
                    for (int e = 0; e < 4; e++)
                    {
                        Vector3 a0 = cs[e], a1 = cs[(e + 1) % 4]; float el = (a1 - a0).magnitude;
                        for (float s = 0; s < el; s += 2f)
                        {
                            if (e == 2 && Mathf.Abs(s - el / 2) < 1.6f) continue; // Tor
                            Vector3 pa = Vector3.Lerp(a0, a1, s / el), pb = Vector3.Lerp(a0, a1, Mathf.Min(1, (s + 2f) / el));
                            pa.y = g.Height(pa.x, pa.z); pb.y = g.Height(pb.x, pb.z);
                            timber.Box(pa + Vector3.up * .55f, q, new Vector3(.12f, 1.1f, .12f));
                            timber.Beam(pa + Vector3.up * .45f, pb + Vector3.up * .45f, .07f, Vector3.up);
                            timber.Beam(pa + Vector3.up * .9f, pb + Vector3.up * .9f, .07f, Vector3.up);
                        }
                    }
                }
                // Heuballen oder Faesser neben dem Haus
                Vector3 side = L(w / 2 + 1.6f, 0, Random.Range(-len * .3f, len * .3f)); side.y = g.Height(side.x, side.z);
                if (Random.value < .5f) { var hq = Quaternion.Euler(0, yaw + 90, 0); hay.Box(side + Vector3.up * .45f, hq, new Vector3(1.0f, .9f, 1.4f)); }
                else if (ta != null && ta.props != null && ta.props.Length > 0) Place(ta.props[Random.Range(0, ta.props.Length)], parent, side, Random.Range(0f, 360f), 1f);

                clr.Block(new Rect(p.x - len * .7f, p.y - len * .7f, len * 1.4f, len * 1.4f));
                g.dirtSpots.Add(new Vector3(p.x, p.y, Mathf.Max(w, len) * .75f));
                if (!farm) g.dirtLines.Add(new Vector4(p.x, p.y, vc.x, vc.y));
                houses.Add(p);
            }

            // Dorfplatz mit Brunnen und Marktstaenden
            g.dirtSpots.Add(new Vector3(vc.x, vc.y, 11f));
            g.dirtLines.Add(new Vector4(vc.x, vc.y, 2f, 62f));   // Weg zum Suedtor der Burg
            float vy = g.Height(vc.x, vc.y);
            Vector3 wc = new Vector3(vc.x, vy, vc.y);
            stone.Cylinder(wc - Vector3.up * .3f, 1.0f, 1.15f, 16, false);
            glass.Disc(wc + Vector3.up * .55f, .85f, 16, Vector3.up);
            timber.Box(wc + new Vector3(-.95f, 1.3f, 0), Quaternion.identity, new Vector3(.14f, 2.0f, .14f));
            timber.Box(wc + new Vector3(.95f, 1.3f, 0), Quaternion.identity, new Vector3(.14f, 2.0f, .14f));
            timber.Beam(wc + new Vector3(-1f, 2.1f, 0), wc + new Vector3(1f, 2.1f, 0), .1f, Vector3.up);
            slate.Gable(wc + Vector3.up * 2.3f, Quaternion.Euler(0, 90, 0), 1.6f, 2.2f, .7f, .1f);
            MeshBuilder[] cloths = { clothR, clothB, clothG };
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f + .5f; Vector3 sp = wc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 6.5f; sp.y = g.Height(sp.x, sp.z);
                var q = Quaternion.LookRotation(wc - sp);
                foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -.8f, .8f })
                    timber.Box(sp + q * new Vector3(sx * 1.3f, 1.1f + (sz > 0 ? .25f : 0), sz), q, new Vector3(.1f, 2.2f + (sz > 0 ? .5f : 0), .1f));
                timber.Box(sp + q * new Vector3(0, .85f, .5f), q, new Vector3(2.6f, .08f, .8f));
                cloths[k].Box(sp + q * new Vector3(0, 2.45f, 0) , q * Quaternion.Euler(-12, 0, 0), new Vector3(3.0f, .04f, 2.2f));
                if (ta != null && ta.props != null && ta.props.Length > 0) Place(ta.props[k % ta.props.Length], parent, sp + q * new Vector3(1.8f, 0, -.6f), Random.Range(0f, 360f), .9f);
            }
            clr.Block(new Rect(vc.x - 10, vc.y - 10, 20, 20));

            walls.ToObject("Hauswaende", ta != null ? ta.timberWall : null, parent);
            thatch.ToObject("Strohdaecher", ta != null ? ta.roofThatch : null, parent);
            slate.ToObject("Schieferdaecher", ta != null ? ta.roofSlate : null, parent);
            stone.ToObject("Sockel", ta != null ? ta.stoneBlocks : null, parent);
            timber.ToObject("Fachwerk und Zaeune", ta != null ? ta.woodStructure : null, parent);
            glass.ToObject("Fenster", Plain(new Color(.05f, .06f, .07f), .85f, 0), parent);
            hay.ToObject("Heuballen", ta != null ? ta.roofThatch : null, parent);
            clothR.ToObject("Marktstand rot", Plain(new Color(.62f, .12f, .1f), .1f, 0, true), parent);
            clothB.ToObject("Marktstand blau", Plain(new Color(.15f, .25f, .55f), .1f, 0, true), parent);
            clothG.ToObject("Marktstand gruen", Plain(new Color(.2f, .42f, .18f), .1f, 0, true), parent);
        }

        // ---------- Natur ----------
        static void Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var g = Object.Instantiate(prefab, pos, Quaternion.Euler(0, yaw, 0), parent);
            g.transform.localScale *= scale;
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }

        // Tanne aus Stamm und gekreuzten Zweig-Karten. Kegelform: unten breit, oben spitz.
        static void Fir(MeshBuilder bark, MeshBuilder twigs, Vector3 at, float H)
        {
            bark.Taper(at, .3f * H / 15f, .04f, H, 9);
            float h0 = H * (.15f + Random.value * .08f), step = .62f, off = Random.value * 6f;
            for (float h = h0; h < H - .3f; h += step)
            {
                float f = (h - h0) / (H - h0);
                float R = Mathf.Pow(1 - f, .85f) * H * .26f + .45f;
                int n = 5 + Mathf.RoundToInt(3 * (1 - f));
                Vector3 c = at + Vector3.up * h;
                for (int k = 0; k < n; k++)
                {
                    float a = (k + Random.Range(-.25f, .25f)) / n * Mathf.PI * 2 + off;
                    off += .9f;
                    Vector3 outw = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    Vector3 dir = (outw + Vector3.down * (.22f + .2f * (1 - f))).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, outw);
                    Vector3 nor = (outw * .6f + Vector3.up).normalized;
                    float len = R * Random.Range(.85f, 1.1f);
                    twigs.Card(c, dir, side, len, len * .8f, nor);
                    twigs.Card(c, dir, Vector3.Cross(dir, side), len, len * .5f, nor);
                }
            }
            Vector3 top = at + Vector3.up * (H - .9f);
            for (int k = 0; k < 3; k++) { float a = k * 2.1f; var s = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); twigs.Card(top, Vector3.up, s, 1.4f, .8f, Vector3.up); }
        }

        public static void Nature(Transform parent, ThemeAssets ta, Clearance clr, Ground gr, Vector3 center, float radius, int trees, bool conifersMostly)
        {
            if (ta == null) return;
            var bark = new MeshBuilder { tile = 1.5f };
            var twigs = new MeshBuilder();
            int put = 0;
            for (int tries = 0; tries < trees * 12 && put < trees; tries++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), dist = Mathf.Sqrt(Random.value) * radius * 1.9f;
                Vector2 p = new Vector2(center.x + Mathf.Cos(ang) * dist, center.z + Mathf.Sin(ang) * dist);
                if (!clr.Free(p, 9f) || gr.InWater(p.x, p.y)) continue;
                Fir(bark, twigs, new Vector3(p.x, gr.Height(p.x, p.y) - .15f, p.y), Random.Range(conifersMostly ? 11f : 8f, conifersMostly ? 21f : 15f));
                gr.forestSpots.Add(new Vector3(p.x, p.y, 4.5f));
                clr.Block(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f));
                put++;
            }
            if (put > 0)
            {
                bark.ToObject("Baumstaemme", ta.treeBark, parent);
                twigs.ToObject("Tannenzweige", ta.treeBranch, parent);
            }
            for (int k = 0; k < trees / 2; k++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), dist = Mathf.Sqrt(Random.value) * radius * 1.6f;
                Vector2 p = new Vector2(center.x + Mathf.Cos(ang) * dist, center.z + Mathf.Sin(ang) * dist);
                if (!clr.Free(p, 4f) || gr.InWater(p.x, p.y)) continue;
                var set = Random.value < .75f ? ta.shrubs : ta.rocks;
                if (set == null || set.Length == 0) continue;
                Place(set[Random.Range(0, set.Length)], parent, new Vector3(p.x, gr.Height(p.x, p.y) - .05f, p.y), Random.Range(0f, 360f), Random.Range(.7f, 1.4f));
            }
        }
    }
}
