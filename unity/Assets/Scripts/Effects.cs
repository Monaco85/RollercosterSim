using UnityEngine;

namespace Achterbahn
{
    /// <summary>Wehende Fahne: kleines Gitter-Mesh, das sich wie Stoff im Wind bewegt. Linke Kante haengt am Mast.</summary>
    public class FlagWave : MonoBehaviour
    {
        public float width = 2.2f, height = 1.4f, speed = 3.2f, phase;
        Mesh mesh; Vector3[] baseV, v;
        const int NX = 12, NY = 7;

        public static FlagWave Create(Transform parent, Vector3 poleTop, float w, float h, Material mat, float yaw)
        {
            var go = new GameObject("Fahne");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(poleTop, Quaternion.Euler(0, yaw, 0));
            var f = go.AddComponent<FlagWave>(); f.width = w; f.height = h; f.phase = Random.value * 10f;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            f.Build();
            return f;
        }

        void Build()
        {
            mesh = new Mesh { name = "Fahne" }; mesh.MarkDynamic();
            baseV = new Vector3[NX * NY]; v = new Vector3[NX * NY]; var uv = new Vector2[NX * NY];
            for (int y = 0; y < NY; y++) for (int x = 0; x < NX; x++)
            {
                int i = y * NX + x; float fx = x / (NX - 1f), fy = y / (NY - 1f);
                baseV[i] = new Vector3(fx * width, -fy * height, 0); uv[i] = new Vector2(fx, 1 - fy);
            }
            var t = new int[(NX - 1) * (NY - 1) * 6]; int k = 0;
            for (int y = 0; y < NY - 1; y++) for (int x = 0; x < NX - 1; x++)
            {
                int i = y * NX + x;
                t[k++] = i; t[k++] = i + 1; t[k++] = i + NX; t[k++] = i + 1; t[k++] = i + NX + 1; t[k++] = i + NX;
            }
            mesh.vertices = baseV; mesh.uv = uv; mesh.triangles = t; mesh.RecalculateNormals();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        }

        void Update()
        {
            if (mesh == null) return;
            float tt = Time.time * speed + phase;
            for (int i = 0; i < baseV.Length; i++)
            {
                var b = baseV[i]; float fx = b.x / width;
                float wave = Mathf.Sin(tt + b.x * 2.4f) * .16f + Mathf.Sin(tt * 1.7f + b.x * 4.1f - b.y * 1.3f) * .06f;
                v[i] = new Vector3(b.x * (1 - .04f * fx), b.y - fx * fx * .12f, wave * fx);
            }
            mesh.vertices = v; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
    }

    /// <summary>Flackerndes Fackellicht.</summary>
    public class Flicker : MonoBehaviour
    {
        public float baseIntensity = 2.2f; float seed; Light l;
        void Start() { l = GetComponent<Light>(); seed = Random.value * 100f; }
        void Update()
        {
            if (l == null) return;
            float t = Time.time * 9f + seed;
            l.intensity = baseIntensity * (.78f + .22f * Mathf.PerlinNoise(t, seed) + .06f * Mathf.Sin(t * 3.1f));
        }
    }

    public static class Fx
    {
        public static void Fire(Transform parent, Vector3 pos, Material mat, float scale = 1f)
        {
            if (mat == null) return;
            var go = new GameObject("Feuer");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, 0, 0));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.6f * scale, 1.2f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(.18f * scale, .34f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .55f, .15f, 1f), new Color(1f, .8f, .35f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 28f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = .05f * scale;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, .2f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(new Color(1f, .9f, .5f), 0), new GradientColorKey(new Color(1f, .35f, .08f), 1) },
                       new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
            col.color = gr;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
        }

        public static void Torch(Transform parent, Vector3 wallPoint, Vector3 outward, Material wood, Material iron, Material fire, bool withLight)
        {
            var mb = new MeshBuilder(); var ib = new MeshBuilder();
            Vector3 b = wallPoint + outward * .05f;
            ib.Box(b, Quaternion.LookRotation(outward), new Vector3(.16f, .3f, .1f));
            Vector3 tip = b + outward * .38f + Vector3.up * .32f;
            mb.Beam(b + outward * .1f - Vector3.up * .05f, tip, .07f, Vector3.up);
            ib.Box(tip, Quaternion.LookRotation(outward), new Vector3(.14f, .12f, .14f));
            mb.ToObject("Fackel", wood, parent, false); ib.ToObject("Halter", iron, parent, false);
            Fire(parent, tip + Vector3.up * .06f, fire, .8f);
            if (withLight)
            {
                var lg = new GameObject("Fackellicht"); lg.transform.SetParent(parent, false); lg.transform.position = tip + Vector3.up * .3f + outward * .2f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 7f; l.color = new Color(1f, .62f, .3f); l.shadows = LightShadows.None;
                lg.AddComponent<Flicker>().baseIntensity = 2.4f;
            }
        }
    }
}
