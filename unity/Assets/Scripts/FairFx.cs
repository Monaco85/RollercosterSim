using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>Feuerwerk ueber dem Rummel: einzelne Raketen und regelmaessig eine Show mit Finale. Mit Lichtblitz und Knall (schallverzoegert).</summary>
    public class Fireworks : MonoBehaviour
    {
        public Material spark; public AudioClip bang, bangBig, crackle;
        public Vector3 siteMin, siteMax;
        readonly List<ParticleSystem> peony = new List<ParticleSystem>(), willow = new List<ParticleSystem>();
        readonly List<Light> flashes = new List<Light>();
        int pi, wi, fi; float next, showEnd, nextShow;
        static readonly Color[] Cols = { new Color(1f, .2f, .2f), new Color(.2f, .5f, 1f), new Color(.3f, 1f, .35f), new Color(1f, .8f, .25f), new Color(1f, .3f, .9f), new Color(.4f, 1f, 1f), new Color(1f, 1f, 1f), new Color(1f, .5f, .1f) };

        void Start()
        {
            for (int i = 0; i < 10; i++) peony.Add(MakeBurst(false));
            for (int i = 0; i < 4; i++) willow.Add(MakeBurst(true));
            for (int i = 0; i < 4; i++)
            {
                var l = new GameObject("Feuerwerkslicht").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.type = LightType.Point; l.range = 220; l.intensity = 0; l.shadows = LightShadows.None; flashes.Add(l);
            }
            next = 4f; nextShow = Time.time + 25f;
        }

        ParticleSystem MakeBurst(bool isWillow)
        {
            var go = new GameObject(isWillow ? "Goldregen" : "Bukett"); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.playOnAwake = false; m.loop = true; m.duration = 5; m.maxParticles = 3000;
            m.startLifetime = isWillow ? new ParticleSystem.MinMaxCurve(2.6f, 3.6f) : new ParticleSystem.MinMaxCurve(1.3f, 2.1f);
            m.startSpeed = isWillow ? new ParticleSystem.MinMaxCurve(14, 19) : new ParticleSystem.MinMaxCurve(20, 28);
            m.startSize = isWillow ? 1.6f : 2.2f; m.gravityModifier = isWillow ? .55f : .3f; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .2f;
            var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = isWillow ? 1.3f : .9f; lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, .15f), new GradientColorKey(isWillow ? new Color(1f, .55f, .2f) : Color.white, 1) },
                       new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .25f));
            var tr = ps.trails; tr.enabled = true; tr.ratio = isWillow ? 1f : .55f; tr.lifetime = isWillow ? .7f : .35f; tr.minVertexDistance = .3f;
            tr.widthOverTrail = new ParticleSystem.MinMaxCurve(isWillow ? .35f : .45f, AnimationCurve.Linear(0, 1, 1, 0)); tr.dieWithParticles = true; tr.inheritParticleColor = true;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = spark; r.trailMaterial = spark; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();   // laeuft dauerhaft (ohne eigene Emission), damit per Emit erzeugte Funken simuliert werden
            return ps;
        }

        void Update()
        {
            if (Time.time >= nextShow && showEnd < Time.time) { showEnd = Time.time + Random.Range(22f, 30f); nextShow = showEnd + Random.Range(40f, 60f); }
            bool show = Time.time < showEnd;
            if (Time.time >= next)
            {
                bool finale = show && showEnd - Time.time < 4f;
                int n = finale ? Random.Range(3, 6) : 1;
                for (int i = 0; i < n; i++) Launch();
                next = Time.time + (show ? (finale ? Random.Range(.25f, .5f) : Random.Range(.5f, 1.4f)) : Random.Range(7f, 14f));
            }
            foreach (var l in flashes) if (l.intensity > 0) l.intensity = Mathf.Max(0, l.intensity - Time.deltaTime * 14f);
        }

        void Launch()
        {
            var start = new Vector3(Random.Range(siteMin.x, siteMax.x), 0, Random.Range(siteMin.z, siteMax.z));
            var go = new GameObject("Rakete"); go.transform.SetParent(transform, false); go.transform.position = start;
            var rk = go.AddComponent<Rocket>(); rk.owner = this; rk.vel = new Vector3(Random.Range(-4f, 4f), Random.Range(44f, 56f), Random.Range(-4f, 4f)); rk.fuse = Random.Range(1.5f, 2.1f);
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main; m.startLifetime = .7f; m.startSpeed = new ParticleSystem.MinMaxCurve(.2f, 1.2f); m.startSize = .35f; m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = .1f;
            m.startColor = new Color(1f, .7f, .35f);
            var em = ps.emission; em.rateOverTime = 0; em.rateOverDistance = 4;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .05f;
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .5f, .2f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); col.color = gr;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = spark; r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            if (crackle != null) FairSound.PlayAt(crackle, start, .25f, 20, 300, 0, Random.Range(1.3f, 1.7f));
        }

        public void Burst(Vector3 p)
        {
            bool isWillow = Random.value < .25f;
            var ps = isWillow ? willow[wi++ % willow.Count] : peony[pi++ % peony.Count];
            ps.transform.position = p;
            Color a = isWillow ? new Color(1f, .75f, .3f) : Cols[Random.Range(0, Cols.Length)], b = Random.value < .4f ? Cols[Random.Range(0, Cols.Length)] : a;
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            if (!isWillow && Random.value < .25f)
            {
                // Ring: Partikel in einer schraegen Ebene
                var q = Quaternion.Euler(Random.Range(-60f, 60f), Random.Range(0, 360f), 0);
                for (int i = 0; i < 90; i++) { float an = i / 90f * Mathf.PI * 2; ep.velocity = q * new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)) * 16f; ep.startColor = a; ps.Emit(ep, 1); }
                ep = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            }
            ep.startColor = a; ps.Emit(ep, isWillow ? 220 : 190);
            if (b != a) { ep.startColor = b; ps.Emit(ep, 100); }
            var l = flashes[fi++ % flashes.Count]; l.transform.position = p; l.color = Color.Lerp(a, Color.white, .3f); l.intensity = 9f;
            var cam = Camera.main; float delay = cam != null ? Vector3.Distance(cam.transform.position, p) / 340f : 0;
            // gedaempft: leiser, faellt mit der Entfernung ab, grosser Knall nur selten
            FairSound.PlayAt(Random.value < .15f && bangBig != null ? bangBig : bang, p, .28f, 40, 700, delay, Random.Range(.8f, 1.0f), true);
        }
    }

    public class Rocket : MonoBehaviour
    {
        public Fireworks owner; public Vector3 vel; public float fuse; float t;
        void Update()
        {
            t += Time.deltaTime; vel += Physics.gravity * .35f * Time.deltaTime; transform.position += vel * Time.deltaTime;
            if (t >= fuse)
            {
                if (owner != null) owner.Burst(transform.position);
                var ps = GetComponent<ParticleSystem>(); var em = ps.emission; em.enabled = false;
                Destroy(gameObject, 1f); enabled = false;
            }
        }
    }

    /// <summary>Himmelsstrahler: schwenkt langsam ueber den Abendhimmel.</summary>
    public class Searchlight : MonoBehaviour
    {
        public float pan = 50, tilt = 18, speed = .12f, baseTilt = -65, baseYaw; float ph;
        void Start() { ph = Random.value * 30f; }
        void Update()
        {
            float t = Time.time * speed + ph;
            transform.localRotation = Quaternion.Euler(baseTilt + Mathf.Sin(t * 1.3f) * tilt, baseYaw + Mathf.Sin(t) * pan, 0);
        }
    }

    /// <summary>Bewegter Scheinwerfer (Moving Head): echter Spot plus sichtbarer Kegel, Farbe wechselt.</summary>
    public class MovingHead : MonoBehaviour
    {
        public Light spot; public Material beam; public float speed = .35f, offset; float ph;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        void Start() { ph = Random.value * 20f; }
        void Update()
        {
            float t = Time.time * speed + ph;
            transform.localRotation = Quaternion.Euler(55 + Mathf.Sin(t * 1.7f) * 25f, Mathf.Sin(t) * 110f + offset, 0);
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * .05f + offset / 360f, 1f), .85f, 1f);
            if (spot != null) spot.color = c;
            if (beam != null) beam.SetColor(ColorId, c);
        }
    }

    /// <summary>Lautstaerke der Schiffschaukel-Schreie folgt dem Ausschlag.</summary>
    public class SwingScream : MonoBehaviour
    {
        public Swing swing; AudioSource src;
        void Start() { src = GetComponent<AudioSource>(); }
        void Update() { if (src != null && swing != null) src.volume = Mathf.Clamp01((Mathf.Abs(swing.Angle) - 25f) / 35f) * .8f; }
    }

    /// <summary>Ton an einem Ort abspielen (3D, mit Verzoegerung fuer Schall aus der Ferne).</summary>
    public static class FairSound
    {
        public static AudioSource Loop(Transform parent, Vector3 pos, AudioClip clip, float vol, float minD, float maxD)
        {
            if (clip == null) return null;
            var go = new GameObject("Ton " + clip.name); go.transform.SetParent(parent, false); go.transform.position = pos;
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.volume = vol; s.spatialBlend = 1; s.minDistance = minD; s.maxDistance = maxD;
            s.rolloffMode = AudioRolloffMode.Logarithmic; s.dopplerLevel = 0; s.playOnAwake = false;
            if (clip.loadType != AudioClipLoadType.Streaming) s.time = Random.Range(0f, Mathf.Max(0, clip.length - 1));
            s.Play();
            return s;
        }
        public static void PlayAt(AudioClip clip, Vector3 pos, float vol, float minD, float maxD, float delay, float pitch = 1, bool muffled = false)
        {
            if (clip == null) return;
            var go = new GameObject("Knall"); go.transform.position = pos;
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.volume = vol; s.spatialBlend = 1; s.minDistance = minD; s.maxDistance = maxD;
            s.rolloffMode = AudioRolloffMode.Logarithmic; s.dopplerLevel = 0; s.pitch = pitch;
            if (muffled) go.AddComponent<AudioLowPassFilter>().cutoffFrequency = 2500;   // ferner Knall: weniger scharf
            s.PlayDelayed(delay);
            Object.Destroy(go, delay + clip.length / Mathf.Max(.1f, pitch) + .2f);
        }
    }

    public static partial class FairWorld
    {
        static void Effects()
        {
            if (ta == null) return;
            // Feuerwerk hinter dem Riesenrad und hinter dem Freifallturm
            var fw = new GameObject("Feuerwerk").AddComponent<Fireworks>(); fw.transform.SetParent(root, false);
            fw.spark = ta.spark; fw.bang = ta.bang; fw.bangBig = ta.bangBig; fw.crackle = null;
            fw.siteMin = new Vector3(-20, 0, 98); fw.siteMax = new Vector3(140, 0, 118);

            // Himmelsstrahler
            Vector3[] sl = { new Vector3(-125, 0, -70), new Vector3(165, 0, 95), new Vector3(40, 0, 115), new Vector3(-120, 0, 90) };
            for (int i = 0; i < sl.Length; i++)
            {
                P(Dark).Cylinder(sl[i], .9f, 1.2f, 14);
                var head = new GameObject("Himmelsstrahler").transform; head.SetParent(root, false); head.position = sl[i] + up * 1.6f;
                var s = head.gameObject.AddComponent<Searchlight>(); s.baseYaw = Mathf.Atan2(18 - sl[i].x, 0 - sl[i].z) * Mathf.Rad2Deg; s.pan = 40; s.speed = .1f + i * .02f;
                var hm = new MeshBuilder(); hm.Loft(new[] { new Vector3(0, 0, -.6f), new Vector3(0, 0, .6f) }, new[] { new Vector2(.7f, .7f), new Vector2(.75f, .75f) }, 1, 16, true, false, up);
                hm.ToObject("Gehaeuse", Lit(Dark, .5f, .7f), head);
                var bm = new Material(ta.beam); bm.SetColor("_Color", new Color(.85f, .9f, 1f)); bm.SetFloat("_Intensity", .22f);
                Cone(head, new Vector3(0, 0, .6f), .65f, 7f, 380f, bm);
            }

            // Moving Heads an der Autoscooter-Halle und an der Gasse
            Vector3[] mh = { new Vector3(-78, 6.2f, -1), new Vector3(-54, 6.2f, -1), new Vector3(-78, 6.2f, 17), new Vector3(-54, 6.2f, 17), new Vector3(-12, 7.2f, 25), new Vector3(36, 7.2f, 25), new Vector3(84, 7.2f, 25) };
            for (int i = 0; i < mh.Length; i++)
            {
                P(Dark).Box(mh[i] + up * .25f, Quaternion.identity, new Vector3(.5f, .3f, .5f));
                if (i >= 4) P(Dark).Cylinder(new Vector3(mh[i].x, 0, mh[i].z), .12f, mh[i].y + .2f, 8);
                var head = new GameObject("Moving Head").transform; head.SetParent(root, false); head.position = mh[i];
                var m = head.gameObject.AddComponent<MovingHead>(); m.offset = i * 51f; m.speed = .3f + i * .03f;
                var spot = new GameObject("Spot").AddComponent<Light>(); spot.transform.SetParent(head, false);
                spot.type = LightType.Spot; spot.range = 45; spot.spotAngle = 22; spot.innerSpotAngle = 8; spot.intensity = 9; spot.shadows = LightShadows.None; lightCount++;
                var bm = new Material(ta.beam); bm.SetFloat("_Intensity", .5f);
                Cone(head, new Vector3(0, 0, .2f), .15f, 4.2f, 22f, bm);
                m.spot = spot; m.beam = bm;
                var hm = new MeshBuilder(); hm.Loft(new[] { new Vector3(0, 0, -.25f), new Vector3(0, 0, .2f) }, new[] { new Vector2(.18f, .18f), new Vector2(.2f, .2f) }, 1, 12, true, true, up);
                hm.ToObject("Kopf", Lit(Dark, .5f, .6f), head);
            }
        }

        /// <summary>Lichtkegel entlang +Z (lokal), uv.y = 0 an der Lampe.</summary>
        static void Cone(Transform parent, Vector3 start, float r0, float r1, float len, Material mat)
        {
            const int S = 24;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= S; i++)
            {
                float a = i / (float)S * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v.Add(start + d * r0); n.Add(d); uv.Add(new Vector2(i / (float)S, 0));
                v.Add(start + d * r1 + Vector3.forward * len); n.Add(d); uv.Add(new Vector2(i / (float)S, 1));
            }
            for (int i = 0; i < S; i++) { int a = i * 2; t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3); }
            var m = new Mesh { name = "Lichtkegel" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            var go = new GameObject("Lichtkegel"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        }

        static void ScooterSparks(Vector3 center, Vector3 size)
        {
            if (ta == null || ta.spark == null) return;
            var go = new GameObject("Funken"); go.transform.SetParent(root, false); go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .45f); m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f); m.startSize = new ParticleSystem.MinMaxCurve(.04f, .1f);
            m.gravityModifier = 1.5f; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.7f, .85f, 1f), new Color(1f, .9f, .6f)); m.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 6, 14, 30, .17f) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = ta.spark; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .06f; r.lengthScale = 1f;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void Sounds()
        {
            if (ta == null) return;
            var amb = new GameObject("Rummel-Atmosphaere").AddComponent<AudioSource>(); amb.transform.SetParent(root, false);
            amb.clip = ta.fairAmbience; amb.loop = true; amb.volume = .5f; amb.spatialBlend = 0; amb.playOnAwake = false;
            if (amb.clip != null) amb.Play();
            FairSound.Loop(root, new Vector3(-30, 4, 52), ta.organ, .9f, 8, 160);
            FairSound.Loop(root, new Vector3(42, 4, 58), ta.carouselMusic, .7f, 8, 140);
            FairSound.Loop(root, new Vector3(-66, 3, 8), ta.bumperCars, .9f, 10, 160);
            var ship = FairSound.Loop(root, new Vector3(108, 5, -48), ta.screams, 0f, 12, 220);
            if (ship != null && shipSwing != null) { var ss = ship.gameObject.AddComponent<SwingScream>(); ss.swing = shipSwing; }
            if (drop != null && ta.screams != null)
            {
                var s = drop.gameObject.AddComponent<AudioSource>(); s.clip = ta.screams; s.spatialBlend = 1; s.minDistance = 15; s.maxDistance = 260; s.volume = 1; s.playOnAwake = false; s.dopplerLevel = 0;
                drop.scream = s;
            }
        }
    }

    /// <summary>Hintergrundmusik passend zum Motto der Bahn: blendet ein, waehrend der Fahrt etwas leiser.</summary>
    public class ThemeMusic : MonoBehaviour
    {
        public float menuVolume = .38f, rideVolume = .24f; AudioSource src;
        public static void Add(Transform root, TrackDef def)
        {
            var ta = ThemeAssets.Get(); if (ta == null) return;
            AudioClip clip = def.id switch
            {
                "raupe" => ta.musicRaupe, "bergblitz" => ta.musicBerg, "drache" => ta.musicDino,
                "monster" => ta.musicMonster, "ritterburg" => ta.musicBurg, "wikinger" => ta.musicBurg, "boxauto" => ta.musicBurg, _ => null,
            };
            if (clip == null) return;
            var go = new GameObject("Musik " + def.name); go.transform.SetParent(root, false);
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.spatialBlend = 0; s.volume = 0; s.playOnAwake = false; s.priority = 64;
            go.AddComponent<ThemeMusic>().src = s;
            s.Play();
        }
        void Update()
        {
            if (src == null) return;
            float want = GameController.Riding ? rideVolume : menuVolume;
            src.volume = Mathf.MoveTowards(src.volume, want, Time.unscaledDeltaTime * .25f);
        }
    }

    /// <summary>
    /// Fahrgeraeusche fuer alle Bahnen: Fahrtwind und Schienenrollen (berechnet, aus Rauschen), Kette am Lift.
    /// Liest nur Anzeige-Werte aus dem GameController, schickt nichts an den Sitz.
    /// </summary>
    public class RideAudio : MonoBehaviour
    {
        AudioSource wind, roll, chain;
        AudioLowPassFilter windLp, rollLp;
        void Start()
        {
            wind = Make("Fahrtwind", Noise(false), out windLp);
            roll = Make("Rollen", Noise(true), out rollLp);
            var ta = ThemeAssets.Get();
            chain = gameObject.AddComponent<AudioSource>(); chain.clip = ta != null ? ta.chainLift : null; chain.loop = true; chain.spatialBlend = 0; chain.volume = 0; chain.playOnAwake = false;
            if (chain.clip != null) chain.Play();
        }
        AudioSource Make(string n, AudioClip clip, out AudioLowPassFilter lp)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.volume = 0; s.spatialBlend = 0; s.playOnAwake = false; s.Play();
            lp = go.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 400;
            return s;
        }
        static AudioClip Noise(bool brown)
        {
            const int rate = 44100, len = rate * 3; var d = new float[len]; var rnd = new System.Random(brown ? 3 : 5); float last = 0;
            for (int i = 0; i < len; i++)
            {
                float w = (float)rnd.NextDouble() * 2 - 1;
                if (brown) { last = (last + .02f * w) / 1.02f; d[i] = last * 3.5f; } else d[i] = w * .5f;
            }
            // weicher Uebergang am Schleifenende
            for (int i = 0; i < 2000; i++) { float k = i / 2000f; d[len - 1 - i] = d[len - 1 - i] * k + d[i] * (1 - k); }
            var c = AudioClip.Create(brown ? "Rollen" : "Wind", len, 1, rate, false); c.SetData(d, 0); return c;
        }
        void Update()
        {
            float v = GameController.RideSpeed; bool on = GameController.Onboard;
            float k = on ? Mathf.Clamp01(v / 28f) : 0;
            wind.volume = Mathf.Lerp(wind.volume, k * k * .55f, Time.deltaTime * 4); windLp.cutoffFrequency = 350 + v * 140;
            roll.volume = Mathf.Lerp(roll.volume, on ? Mathf.Clamp01(v / 6f) * .45f : 0, Time.deltaTime * 4); rollLp.cutoffFrequency = 120 + v * 22;
            roll.pitch = .8f + Mathf.Clamp01(v / 30f) * .6f;
            if (chain.clip != null) chain.volume = Mathf.Lerp(chain.volume, on && GameController.OnLift ? .7f : 0, Time.deltaTime * 5);
        }
    }
}
