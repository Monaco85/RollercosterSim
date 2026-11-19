using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Ein Rummelbesucher (Quaternius-Figur, Animation gebacken in CrowdBank). Laeuft herum, kauft ein, wirft Dosen,
    /// schiesst, steht an, faehrt mit, winkt dem vorbeifahrenden Wagen zu. Sitzen und Arme-hoch sind
    /// eigene gebackene Haltungen.
    /// </summary>
    public class FairPerson : MonoBehaviour
    {
        public enum Act { Wander, Vendor, Customer, Shooter, Thrower, Queue, Rider, Watcher, Eater, Follower }
        public Act act;
        public bool sit, straddle;
        public System.Func<bool> armsUp;
        public Rect zone; public float speed = 1.25f;
        public FairWorld.BoothInfo booth; public Vector3 lookAt;
        public FairPerson leader; public Vector3 followOffset; public float hipHeight = .9f;

        CrowdAnim an; public CrowdAnim Anim { get => an; set => an = value; }
        float timer, waveCheck, shadowCheck; Vector3 target; bool moving, waving; MeshRenderer mr;
        static int camFrame = -1; static Vector3 camPos;
        public Transform HandR { get { if (an != null && an.handR == null) an.handR = NewHand("Hand R"); return an?.handR; } }
        public Transform HandL { get { if (an != null && an.handL == null) an.handL = NewHand("Hand L"); return an?.handL; } }
        Transform NewHand(string n) { var h = new GameObject(n).transform; h.SetParent(transform, false); return h; }

        public static readonly List<Vector3> blockers = new List<Vector3>();   // x, z, Radius

        void Start()
        {
            timer = Random.Range(0f, 4f);
            switch (act)
            {
                case Act.Wander: NewTarget(); break;
                case Act.Shooter: Play("Idle_Gun_Pointing"); break;
                case Act.Rider: Play(straddle ? "Straddle" : "Sit"); break;
                case Act.Queue: Play("Idle_Neutral"); break;
                default: Play(Random.value < .7f ? "Idle" : "Idle_Neutral"); break;
            }
            if (an != null) an.Seek01(Random.value);
        }

        string cur => an != null ? an.Name : null;
        void Play(string clip, float fade = .25f, float spd = 1f) { if (an == null) return; an.speed = spd; an.Play(clip, fade); }
        void Once(string clip, float fade = .15f) { if (an == null) return; an.speed = 1; an.Play(clip, fade, true); }
        bool OnceDone => an == null || an.Done || an.Loop;
        void Face(Vector3 dir, float k)
        {
            dir.y = 0; if (dir.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(k));
        }

        // Wagen in der Naehe? Dann hinschauen und winken
        bool CarNear(float dist)
        {
            var car = FairWorld.car; if (car == null) return false;
            var d = car.position - transform.position; return d.sqrMagnitude < dist * dist;
        }

        void Update()
        {
            float dt = Time.deltaTime; timer -= dt;
            if (an != null) an.Tick(dt);
            shadowCheck -= dt;
            if (shadowCheck < 0)
            {
                shadowCheck = .5f + Random.value * .3f;
                if (Time.frameCount != camFrame) { camFrame = Time.frameCount; var c = Camera.main; camPos = c != null ? c.transform.position : Vector3.zero; }
                if (mr == null) mr = GetComponent<MeshRenderer>();
                bool near = (transform.position - camPos).sqrMagnitude < 45f * 45f;
                var want = near ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                if (mr != null && mr.shadowCastingMode != want) mr.shadowCastingMode = want;
            }
            switch (act)
            {
                case Act.Wander: Wander(dt); break;
                case Act.Rider:
                    if (armsUp != null) Play(armsUp() ? "SitArmsUp" : "Sit", .35f);
                    break;
                case Act.Follower:
                    if (leader == null) { act = Act.Wander; break; }
                    var want = leader.transform.position + leader.transform.rotation * followOffset;
                    var dv = want - transform.position; dv.y = 0;
                    if (dv.magnitude > .25f) { transform.position += dv.normalized * Mathf.Min(dv.magnitude, leader.speed * 1.15f * dt); Face(dv, dt * 6); Play(leader.speed > 2 ? "Run" : "Walk", .2f, Mathf.Max(.5f, leader.speed / 1.25f)); }
                    else { Face(leader.transform.forward, dt * 3); Play("Idle"); }
                    break;
                case Act.Vendor:
                    if (timer < 0) { if (Random.value < .6f) Once("Interact"); else if (Random.value < .2f) Once("Wave"); else Play("Idle"); timer = Random.Range(2f, 5f); }
                    else if (OnceDone && cur != "Idle") Play("Idle");
                    break;
                case Act.Customer:
                case Act.Eater:
                    if (timer < 0) { float r = Random.value; if (act == Act.Customer && r < .45f) Once("Interact"); else if (r < .55f) Play("Idle_Neutral"); else Play("Idle"); timer = Random.Range(2.5f, 7f); }
                    else if (OnceDone && cur == "Interact") Play("Idle");
                    WaveAtCar(dt);
                    break;
                case Act.Shooter:
                    if (timer < 0)
                    {
                        Once(Random.value < .5f ? "Idle_Gun_Shoot" : "Gun_Shoot", .1f);
                        if (booth != null && booth.targets != null && Random.value < .6f) booth.targets.Hit();
                        var hr = HandR; if (hr != null) FairWorld.MuzzleFlash(hr.position + hr.up * .6f * transform.localScale.x);
                        timer = Random.Range(1.6f, 3.5f);
                    }
                    else if (OnceDone && cur != "Idle_Gun_Pointing") Play("Idle_Gun_Pointing");
                    break;
                case Act.Thrower:
                    if (timer < 0)
                    {
                        Once("Punch_Right", .1f); timer = Random.Range(3.5f, 6f);
                        if (booth != null && booth.cans != null && booth.cans.Length > 0) StartCoroutine(Throw(booth.cans[Random.Range(0, booth.cans.Length)]));
                    }
                    else if (OnceDone && cur != "Idle") Play("Idle");
                    break;
                case Act.Queue:
                    if (timer < 0) { if (Random.value < .3f) Once("Interact"); timer = Random.Range(4f, 9f); }
                    else if (OnceDone && cur != "Idle_Neutral") Play("Idle_Neutral");
                    WaveAtCar(dt);
                    break;
                case Act.Watcher:
                    if (lookAt != Vector3.zero && !waving) Face(lookAt - transform.position, dt * 2);
                    if (timer < 0) { if (Random.value < .25f && !waving) Once("Wave"); timer = Random.Range(4f, 10f); }
                    else if (OnceDone && !waving && cur != "Idle") Play("Idle");
                    WaveAtCar(dt);
                    break;
            }
        }

        void WaveAtCar(float dt)
        {
            waveCheck -= dt; if (waveCheck > 0) return; waveCheck = .4f + Random.value * .2f;
            bool near = CarNear(24f);
            if (near && !waving && Random.value < .7f) { waving = true; Play("Wave", .2f); }
            else if (!near && waving) { waving = false; Play("Idle"); }
            if (waving && FairWorld.car != null) Face(FairWorld.car.position - transform.position, 1f);
        }

        System.Collections.IEnumerator Throw(CanStack target)
        {
            yield return new WaitForSeconds(.32f);
            var hr = HandR; if (hr == null || target == null) yield break;
            var b = new GameObject("Wurfball"); b.transform.SetParent(transform.parent, true);
            var mb = new MeshBuilder(); mb.Sphere(Vector3.zero, .05f, 8); mb.ToObject("Ball", FairWorld.BallMat, b.transform, false);
            var tb = b.AddComponent<ThrownBall>(); tb.from = hr.position; tb.to = target.Center; tb.target = target;
        }

        void Wander(float dt)
        {
            if (moving)
            {
                var d = target - transform.position; d.y = 0;
                float dist = d.magnitude;
                if (dist < .3f)
                {
                    moving = false; timer = Random.value < .45f ? Random.Range(1f, 6f) : 0f;
                    float r = Random.value;
                    if (timer > 0) { if (r < .15f) Once("Wave"); else if (r < .3f) Once("Interact"); else Play(Random.value < .5f ? "Idle" : "Idle_Neutral"); }
                }
                else
                {
                    Face(d, dt * 5);
                    transform.position += transform.forward * speed * dt;
                    Play(speed > 2f ? "Run" : "Walk", .25f, speed > 2f ? speed / 3.6f : speed / 1.25f);
                }
            }
            else
            {
                WaveAtCar(dt);
                if (timer <= 0 && !waving && OnceDone) NewTarget();
                else if (OnceDone && !waving && cur != "Idle" && cur != "Idle_Neutral") Play("Idle");
            }
        }

        void NewTarget()
        {
            var p = transform.position;
            for (int tries = 0; tries < 12; tries++)
            {
                var t = new Vector3(Random.Range(zone.xMin, zone.xMax), p.y, Random.Range(zone.yMin, zone.yMax));
                if ((t - p).sqrMagnitude > 40f * 40f) t = p + (t - p).normalized * 40f;
                if (!Blocked(p, t)) { target = t; moving = true; return; }
            }
            timer = 1f;
        }

        public static bool Blocked(Vector3 a, Vector3 b)
        {
            foreach (var c in blockers)
            {
                var ab = new Vector2(b.x - a.x, b.z - a.z); var ac = new Vector2(c.x - a.x, c.y - a.z);
                float t = Mathf.Clamp01(Vector2.Dot(ac, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                var close = new Vector2(a.x, a.z) + ab * t;
                if ((close - new Vector2(c.x, c.y)).sqrMagnitude < c.z * c.z) return true;
            }
            return false;
        }
        public static bool Inside(Vector3 p) { foreach (var c in blockers) if ((new Vector2(p.x - c.x, p.z - c.y)).sqrMagnitude < c.z * c.z) return true; return false; }

    }

    /// <summary>Gegenstand, der der Hand folgt, aber aufrecht bleibt (Ballon an der Schnur).</summary>
    public class FollowHand : MonoBehaviour
    {
        public Transform hand; public Vector3 offset;
        void LateUpdate() { if (hand != null) { transform.position = hand.position + offset + Vector3.up * Mathf.Sin(Time.time * 1.7f + offset.x * 9) * .05f; transform.rotation = Quaternion.identity; } }
    }

    public static partial class FairWorld
    {
        static Material ballMat;
        public static Material BallMat => ballMat != null ? ballMat : (ballMat = Lit(new Color(.9f, .9f, .85f), .5f));

        static Material flashMat;
        /// <summary>Kurzes Muendungsfeuer in der Schiessbude.</summary>
        public static void MuzzleFlash(Vector3 p)
        {
            var go = new GameObject("Muendungsfeuer"); go.transform.position = p;
            var mb = new MeshBuilder(); mb.Sphere(Vector3.zero, .05f, 6);
            if (flashMat == null) flashMat = Emit(new Color(1f, .8f, .4f), 12f);
            mb.ToObject("Blitz", flashMat, go.transform, false);
            Object.Destroy(go, .06f);
        }

        static Transform people; static List<CrowdBank.Template> templates;

        static FairPerson Spawn(Vector3 pos, float yaw, FairPerson.Act act, bool kid = false, Transform parent = null)
        {
            if (templates == null || templates.Count == 0) return null;
            var T = templates[Random.Range(0, templates.Count)];
            var go = new GameObject("Mensch"); go.transform.SetParent(parent != null ? parent : people, false);
            float s = kid ? Random.Range(.6f, .72f) : Random.Range(.94f, 1.05f);
            go.transform.localScale = Vector3.one * s;
            if (parent != null) { go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); }
            else go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.AddComponent<MeshFilter>().sharedMesh = T.mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = T.mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            // Kleidung umfaerben, damit nicht alle gleich aussehen
            var pal = (Vector4[])T.palette.Clone();
            for (int i = 0; i < pal.Length; i++)
            {
                if (!T.cloth[i] || Random.value < .35f) continue;
                Color.RGBToHSV(new Color(pal[i].x, pal[i].y, pal[i].z), out float h, out float sa, out float v);
                var nc = Color.HSVToRGB(Random.value, Mathf.Clamp01(Mathf.Max(sa, .25f) + Random.Range(-.05f, .35f)), Mathf.Clamp(v * Random.Range(.8f, 1.5f), .05f, .85f));
                pal[i] = new Vector4(nc.r, nc.g, nc.b, 1);
            }
            var p = go.AddComponent<FairPerson>(); p.act = act;
            p.Anim = new CrowdAnim(T, mr, pal);
            p.hipHeight = T.hipHeight;
            return p;
        }

        static FairPerson Rider(Transform seat, Vector3 seatLocal, Quaternion faceLocal, bool straddle, System.Func<bool> armsUp = null, bool kid = false)
        {
            var p = Spawn(Vector3.zero, 0, FairPerson.Act.Rider, kid, seat);
            if (p == null) return null;
            float s = p.transform.localScale.x;
            p.transform.localRotation = faceLocal;
            p.transform.localPosition = seatLocal - Vector3.up * (p.hipHeight * s - .04f);
            p.sit = true; p.straddle = straddle; p.armsUp = armsUp;
            return p;
        }
        static void People(Scenery.Clearance clr)
        {
            templates = CrowdBank.Get(ta);
            if (templates == null || templates.Count == 0) return;
            people = new GameObject("Besucher").transform; people.SetParent(root, false);
            var bl = FairPerson.blockers; bl.Clear();
            bl.Add(new Vector3(-30, 52, 13.5f)); bl.Add(new Vector3(42, 58, 14f)); bl.Add(new Vector3(105, 32, 15f)); bl.Add(new Vector3(-62, -58, 7.5f));
            bl.Add(new Vector3(108, -48, 11.5f)); bl.Add(new Vector3(-66, 8, 15.5f)); bl.Add(new Vector3(-104, 17, 2f)); bl.Add(new Vector3(-104, 31, 2f));

            Rect alley = new Rect(-38, 21, 124, 7.8f), south = new Rect(-31, -42.2f, 114, 4.4f), north = new Rect(-95, 39, 225, 48),
                 west = new Rect(-100, -30, 18, 90), east = new Rect(76, -34, 58, 50);
            void Wanderers(Rect z, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = Vector3.zero; int t = 0;
                    do { p = new Vector3(Random.Range(z.xMin, z.xMax), 0, Random.Range(z.yMin, z.yMax)); } while (FairPerson.Inside(p) && ++t < 20);
                    bool kid = Random.value < .16f;
                    var w = Spawn(p, Random.Range(0, 360f), FairPerson.Act.Wander, kid);
                    w.zone = z; w.speed = kid && Random.value < .5f ? Random.Range(2.6f, 3.4f) : Random.Range(1.0f, 1.45f);
                    float r = Random.value;
                    if (r < .25f)
                    {
                        // Paar oder Familie: laeuft nebeneinander
                        var f = Spawn(p + Vector3.right * .7f, 0, FairPerson.Act.Follower, Random.value < .4f);
                        f.leader = w; f.followOffset = new Vector3(Random.value < .5f ? .75f : -.75f, 0, Random.Range(-.4f, .1f));
                        if (Random.value < .3f) { var f2 = Spawn(p - Vector3.right * .7f, 0, FairPerson.Act.Follower, true); f2.leader = w; f2.followOffset = new Vector3(-f.followOffset.x, 0, -.3f); HoldBalloon(f2); }
                    }
                    else if (r < .33f) HoldBalloon(w);
                    else if (r < .45f) HoldSnack(w);
                }
            }
            Wanderers(alley, 46); Wanderers(south, 14); Wanderers(north, 28); Wanderers(west, 7); Wanderers(east, 10);

            // Buden: Verkaeufer innen, Kunden davor, Werfer und Schuetzen
            foreach (var b in booths)
            {
                var face = Quaternion.Euler(0, b.yaw, 0) * Vector3.forward;   // Theke zeigt nach aussen
                float yawOut = b.yaw, yawIn = b.yaw + 180;
                Spawn(b.inside, yawOut, FairPerson.Act.Vendor);
                Vector3 side = Quaternion.Euler(0, b.yaw, 0) * Vector3.right;
                switch (b.kind)
                {
                    case BoothKind.Shooting:
                        for (int i = 0; i < 2; i++)
                        {
                            var s = Spawn(b.front - face * .25f + side * (-1f + i * 1.8f), yawIn, FairPerson.Act.Shooter);
                            s.booth = b; GiveRifle(s);
                        }
                        Spawn(b.front + face * 1.3f + side * .5f, yawIn, FairPerson.Act.Watcher).lookAt = b.inside;
                        break;
                    case BoothKind.Cans:
                        for (int i = 0; i < 2; i++) { var t = Spawn(b.front + side * (-1f + i * 1.8f), yawIn, FairPerson.Act.Thrower); t.booth = b; }
                        Spawn(b.front + face * 1.4f, yawIn, FairPerson.Act.Watcher, true).lookAt = b.inside;
                        break;
                    case BoothKind.Ticket:
                        break;
                    default:
                        int n = Random.Range(0, 3);
                        for (int i = 0; i < n; i++) Spawn(b.front + side * Random.Range(-1.8f, 1.8f) + face * Random.Range(-.1f, .5f), yawIn + Random.Range(-15f, 15f), FairPerson.Act.Customer, Random.value < .2f);
                        break;
                }
            }

            // Warteschlangen
            for (int i = 0; i < 12; i++) Spawn(new Vector3(-29 + i * 1.1f + Random.Range(-.15f, .15f), 0, 13.5f + Random.Range(-.25f, .25f)), 90 + Random.Range(-20f, 20f), FairPerson.Act.Queue, Random.value < .2f);
            for (int i = 0; i < 8; i++) Spawn(new Vector3(105 + Random.Range(-.6f, .6f), 0, 21.5f - i * .9f), 0, FairPerson.Act.Queue, Random.value < .25f);
            // Zuschauer am Bahnzaun (winken dem Wagen) und an den Fahrgeschaeften
            for (int i = 0; i < 14; i++) { var w = Spawn(new Vector3(-28 + i * 7f + Random.Range(-2f, 2f), 0, -37.1f), 0, FairPerson.Act.Watcher, Random.value < .3f); w.lookAt = new Vector3(w.transform.position.x, 6, -20); }
            for (int i = 0; i < 6; i++) { var w = Spawn(new Vector3(71.3f, 0, -30 + i * 5.5f), 270, FairPerson.Act.Watcher, Random.value < .3f); w.lookAt = new Vector3(50, 8, w.transform.position.z); }
            for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 1.2f + 1.2f; var w = Spawn(new Vector3(-62 + Mathf.Cos(a) * 9f, 0, -58 + Mathf.Sin(a) * 9f), 0, FairPerson.Act.Watcher); w.lookAt = new Vector3(-62, 40, -58); }
            for (int i = 0; i < 5; i++) { float a = i / 5f * Mathf.PI + 1.8f; var w = Spawn(new Vector3(-30 + Mathf.Cos(a) * 14.5f, 0, 52 + Mathf.Sin(a) * 14.5f), 0, FairPerson.Act.Watcher, Random.value < .4f); w.lookAt = new Vector3(-30, 2, 52); }
            for (int i = 0; i < 5; i++) { var w = Spawn(new Vector3(100 + i * 4f, 0, -37f), 180, FairPerson.Act.Watcher); w.lookAt = new Vector3(108, 8, -48); }

            // Fahrgaeste
            foreach (var g in wheelSeats) if (Random.value < .6f)
                {
                    bool two = Random.value < .5f;
                    Rider(g, new Vector3(two ? -.35f : 0, -2.36f, .55f), Quaternion.Euler(0, 180, 0), false);
                    if (two) Rider(g, new Vector3(.35f, -2.36f, -.55f), Quaternion.identity, false, null, Random.value < .4f);
                }
            foreach (var s in swingSeats) if (Random.value < .85f) Rider(s, new Vector3(0, -5.03f, .02f), Quaternion.identity, false, null, Random.value < .3f);
            foreach (var h in horses) if (Random.value < .5f) Rider(h, new Vector3(0, .48f, -.12f), Quaternion.identity, true, null, Random.value < .55f);
            if (drop != null)
                for (int i = 0; i < 12; i++)
                {
                    if (Random.value < .3f) continue;
                    float a = i / 12f * Mathf.PI * 2; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var d = drop; Rider(drop.transform, o * 2.85f + Vector3.up * .58f, Quaternion.LookRotation(o), false, () => d.Height > 30f || d.Falling);
                }
            if (shipPivot != null)
                for (float x = -4.5f; x <= 4.6f; x += 1.5f)
                    foreach (float z in new[] { -.6f, .6f })
                    {
                        if (Random.value < .25f || Mathf.Abs(x) < .8f) continue;
                        var sw = shipSwing; Rider(shipPivot, new Vector3(x, -11f + 1.35f + Mathf.Abs(x) * .03f, z), Quaternion.Euler(0, x < 0 ? 90 : 270, 0), false, () => Mathf.Abs(sw.Angle) > 35f);
                    }
            foreach (var sc in scooters) Rider(sc, new Vector3(0, .62f, -.25f), Quaternion.identity, false, null, Random.value < .3f);
        }

        static void HoldBalloon(FairPerson p)
        {
            if (p == null || p.HandR == null) return;
            var b = new GameObject("Ballon").transform; b.SetParent(people, false);
            Balloon(b, Vector3.up * 1.25f, Bright[Random.Range(0, 8)], 1.25f);
            var f = b.gameObject.AddComponent<FollowHand>(); f.hand = Random.value < .5f ? p.HandR : p.HandL; f.offset = new Vector3(Random.Range(-.1f, .1f), 0, Random.Range(-.1f, .1f));
        }

        static void HoldSnack(FairPerson p)
        {
            if (p == null || p.HandR == null) return;
            var s = new GameObject("Snack").transform; s.SetParent(p.HandR, false);
            s.localPosition = new Vector3(0, .09f, .02f); s.localRotation = Quaternion.Euler(180, 0, 0);
            var mb = new MeshBuilder(); float r = Random.value;
            if (r < .4f) { mb.Cylinder(Vector3.zero, .01f, .3f, 5); mb.ToObject("Stiel", Lit(White), s, false); var c = new MeshBuilder(); c.Sphere(Vector3.up * .38f, .14f, 10); c.ToObject("Zuckerwatte", Lit(Random.value < .5f ? new Color(1f, .6f, .8f) : new Color(.6f, .8f, 1f), .2f), s, false); }
            else if (r < .7f) { mb.Spike(Vector3.up * .12f, Vector3.down, .045f, .12f, 8); mb.ToObject("Waffel", Lit(new Color(.8f, .6f, .35f)), s, false); var c = new MeshBuilder(); c.Sphere(Vector3.up * .15f, .05f, 8); c.ToObject("Eis", Lit(Bright[Random.Range(0, 8)], .5f), s, false); }
            else { mb.Cylinder(Vector3.zero, .045f, .16f, 10); mb.ToObject("Becher", Lit(Random.value < .5f ? Red : White, .6f), s, false); }
        }

        static void GiveRifle(FairPerson p)
        {
            if (p == null || p.HandR == null) return;
            var g = new GameObject("Gewehr").transform; g.SetParent(p.HandR, false); g.localPosition = new Vector3(0, .05f, 0); g.localRotation = Quaternion.identity;
            var st = new MeshBuilder(); st.Box(new Vector3(0, -.15f, 0), Quaternion.identity, new Vector3(.05f, .4f, .08f)); st.ToObject("Schaft", Lit(new Color(.35f, .2f, .1f), .5f), g, false);
            var br = new MeshBuilder(); br.Loft(new[] { new Vector3(0, .05f, .02f), new Vector3(0, .6f, .02f) }, new[] { new Vector2(.014f, .014f), new Vector2(.014f, .014f) }, 1, 6, true, true, Vector3.forward); br.ToObject("Lauf", Lit(Dark, .6f, .8f), g, false);
        }
    }
}
