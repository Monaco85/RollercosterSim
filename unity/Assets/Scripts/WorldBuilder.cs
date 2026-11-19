using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>Baut Bahn, Stuetzen, Boden und Zug aus den Streckendaten. Rein optisch, ohne Einfluss auf Physik oder Sitz.</summary>
    public static class WorldBuilder
    {
        static Material baseMat;
        static Material Mat(Color c, float smooth = 0.25f)
        {
            if (baseMat == null) baseMat = Resources.Load<Material>("AchterbahnLit");
            Material m;
            if (baseMat != null) m = new Material(baseMat);
            else
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            return m;
        }

        static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public class World
        {
            public GameObject root;
            public Transform[] cars;
            public Vector3 center; public float radius;
        }

        public static World Build(TrackDef def, Track track)
        {
            var w = new World { root = new GameObject("Bahn " + def.name) };
            var root = w.root.transform;

            var ta = ThemeAssets.Get();
            int seed = 17; foreach (char ch in def.id) seed = seed * 31 + ch;
            Random.InitState(seed);

            // Boden: Poly-Haven-Wiese, alle 4 m wiederholt
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Boden"; ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(300, 1, 300);
            ground.transform.localPosition = new Vector3(0, def.lake != null ? -(def.lake[4] + 2f) : -.05f, 0);
            Material gm;
            if (ta != null && ta.grass != null)
            {
                // Luftbild-Wiese: eine Wiederholung je 12 m, leicht ins Gruene gezogen
                gm = new Material(ta.grass); gm.mainTextureScale = new Vector2(250, 250);
                Color tint = new Color(.82f, 1f, .7f);
                if (def.theme == "stahl") tint = Color.Lerp(tint, def.ground, .25f);
                gm.SetColor("_BaseColor", tint);
            }
            else gm = Mat(def.ground, 0f);
            ground.GetComponent<Renderer>().sharedMaterial = gm;
            Object.Destroy(ground.GetComponent<Collider>());

            // Ausdehnung fuer die Menue-Kamera
            Vector3 mn = new Vector3(1e9f, 1e9f, 1e9f), mx = -mn;
            foreach (var p in track.pos) { var u = p.ToUnity(); mn = Vector3.Min(mn, u); mx = Vector3.Max(mx, u); }
            w.center = (mn + mx) * .5f;
            w.radius = Mathf.Max(mx.x - mn.x, mx.z - mn.z) * .5f + 40f;

            var clr = new Scenery.Clearance(track);
            var gr = new Ground(def, track, w.center, w.radius, root, ta);
            if (def.theme == "mittelalter")
            {
                Scenery.WoodTrack(track, root, ta, p => (Mathf.Abs(p.x - 2) < 24f && Mathf.Abs(p.z) < 4f));
                CastleBuilder.Build(def, track, root, ta, clr, gr);
                Scenery.Village(def, track, root, ta, clr, gr, w.center, w.radius);
                Scenery.Nature(root, ta, clr, gr, w.center, w.radius, 220, true);
            }
            else if (def.theme == "dino")
            {
                BuildRails(track, def, root);
                DinoWorld.Supports(track, root, ta);
                DinoWorld.Station(def, root, ta, clr);
                DinoWorld.Lake(def, root, ta, clr, gr);
                DinoWorld.Dinos(root, ta, clr, gr, w.center);
                DinoWorld.Jungle(root, ta, clr, gr, w.center, w.radius, track);
                DinoWorld.Volcano(def, root, gr);
            }
            else if (def.theme == "rummel")
            {
                BuildRails(track, def, root);
                BuildSupports(track, def, root);
                FairWorld.Build(def, track, root, ta, clr, gr, w.center, w.radius);
                Scenery.Nature(root, ta, clr, gr, w.center, w.radius, 160, false);
            }
            else if (def.theme == "holz")
            {
                Scenery.WoodTrack(track, root, ta, p => false);
                Scenery.Nature(root, ta, clr, gr, w.center, w.radius, 200, true);
            }
            else
            {
                BuildRails(track, def, root);
                BuildSupports(track, def, root);
                Scenery.Nature(root, ta, clr, gr, w.center, w.radius, 150, def.id == "bergblitz");
            }
            // Liftkette dort, wo der Plan einen Kettenlift hat
            foreach (var sgm in def.plan(track)) if (sgm.lift) Scenery.LiftChain(track, sgm.from, sgm.to, root);
            gr.Paint(def.volcano != null, def.theme == "dino");
            gr.Grass(root, clr);

            // Zug
            w.cars = new Transform[def.trainCars];
            for (int i = 0; i < w.cars.Length; i++) w.cars[i] = TrainBuilder.Car(def, i, root, ta);
            FairWorld.car = def.theme == "rummel" && w.cars.Length > 0 ? w.cars[0] : null;

            // Spiegelungen von Himmel und Umgebung (einmal gerendert)
            var probeGo = new GameObject("Spiegelung"); probeGo.transform.SetParent(root, false);
            probeGo.transform.position = w.center + Vector3.up * 15f;
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(4000, 800, 4000); probe.resolution = 256; probe.importance = 1;
            probe.RenderProbe();
            return w;
        }

        static void BuildRails(Track track, TrackDef def, Transform parent)
        {
            Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Mesh cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            var rails = new List<CombineInstance>();
            var ties = new List<CombineInstance>();
            const int stride = 3; // jeder dritte Abtastpunkt = 0,75 m
            bool have = false; Vector3 pl = default, pr = default;
            int count = track.n / stride + 1;
            for (int k = 0; k <= count; k++)
            {
                int i = (k * stride) % track.n;
                Vector3 p = track.pos[i].ToUnity(), u = track.U[i].ToUnity(), t = track.T[i].ToUnity(), r = track.R[i].ToUnity();
                Vector3 l = p - r * .55f + u * .05f, rr = p + r * .55f + u * .05f;
                if (have)
                {
                    rails.Add(Tube(cyl, pl, l, .07f));
                    rails.Add(Tube(cyl, pr, rr, .07f));
                }
                have = true; pl = l; pr = rr;
                if (k % 2 == 0)
                    ties.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p, Quaternion.LookRotation(t, u), new Vector3(1.3f, .08f, .18f)) });
            }
            var railMat = Mat(def.rail, .7f); railMat.SetFloat("_Metallic", .6f);
            MeshObject("Schienen", Combine(rails), railMat, parent);
            MeshObject("Schwellen", Combine(ties), Mat(def.support, .2f), parent);
        }

        static CombineInstance Tube(Mesh cyl, Vector3 a, Vector3 b, float r)
        {
            Vector3 d = b - a; float len = d.magnitude;
            if (len < 1e-4f) d = Vector3.up;
            var m = Matrix4x4.TRS((a + b) * .5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(r * 2f, len * .5f + .01f, r * 2f));
            return new CombineInstance { mesh = cyl, transform = m };
        }

        static void BuildSupports(Track track, TrackDef def, Transform parent)
        {
            Mesh cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            var list = new List<CombineInstance>();
            int step = Mathf.Max(1, Mathf.RoundToInt(7f / (float)track.ds));
            for (int i = 0; i < track.n; i += step)
            {
                Vector3 p = track.pos[i].ToUnity();
                float h = p.y - .2f;
                if (h < .6f || track.U[i].y < .5) continue;   // im Looping keine Stuetze durch die Bahn
                list.Add(new CombineInstance { mesh = cyl, transform = Matrix4x4.TRS(new Vector3(p.x, h * .5f, p.z), Quaternion.identity, new Vector3(.35f, h * .5f, .35f)) });
            }
            MeshObject("Stuetzen", Combine(list), Mat(def.support, .1f), parent);
        }

        static Mesh Combine(List<CombineInstance> parts)
        {
            var mesh = new Mesh { name = "combined", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
