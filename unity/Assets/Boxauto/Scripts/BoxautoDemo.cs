using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Vorfuehr-Fahrt: die Wagen fahren selbststaendig ueber die Bahn und rempeln sich an. Einfaches Kreis-Modell (zwei Kreise pro Wagen),
    /// nur fuer die Optik der Vorschau – hat nichts mit dem Fahrmodell des Spiels (BumperRide) zu tun.
    /// </summary>
    public class BoxautoDemo : MonoBehaviour
    {
        public Transform[] wagen;
        public Vector2 halb = new Vector2(12f, 7f);   // Innenkante der Bande
        public float eckRadius = 2.5f;
        public bool fahren = true;
        public float tempo = 2.6f;                    // m/s (gut 9 km/h)
        [Range(0, 1)] public float rummsLautstaerke = .55f;

        const float R = .745f, OFF = .405f;           // Gummiring: zwei Kreise vorn/hinten

        class W
        {
            public Transform t, body, rad; public BoxautoLichter li; public AudioSource ton;
            public Vector2 p, v, ziel; public float yaw, w, steer, gas, timer;
            public Quaternion radStart; public float bp, bpv, br, brv;
        }
        W[] ws;
        AudioClip rumms;

        public Transform Wagen(int i) => ws != null && ws.Length > 0 ? ws[((i % ws.Length) + ws.Length) % ws.Length].t : null;
        public int Anzahl => ws != null ? ws.Length : 0;

        void Start()
        {
            rumms = MacheRumms();
            ws = new W[wagen.Length];
            for (int i = 0; i < wagen.Length; i++)
            {
                var w = new W { t = wagen[i] };
                w.p = new Vector2(w.t.position.x, w.t.position.z); w.yaw = w.t.eulerAngles.y * Mathf.Deg2Rad;
                w.body = w.t.Find("Aufbau"); w.li = w.t.GetComponent<BoxautoLichter>();
                if (w.li != null) { w.li.lenkradWackeln = false; w.rad = w.li.lenkrad; if (w.rad != null) w.radStart = w.rad.localRotation; }
                w.ton = w.t.gameObject.AddComponent<AudioSource>(); w.ton.spatialBlend = 1; w.ton.minDistance = 2; w.ton.maxDistance = 40; w.ton.playOnAwake = false;
                w.timer = Random.Range(0f, 1.5f);
                ws[i] = w;
            }
        }

        static Vector2 Fwd(float yaw) => new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        float Sd(Vector2 p)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halb - Vector2.one * eckRadius);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - eckRadius;
        }
        Vector2 SdN(Vector2 p)
        {
            const float e = .01f;
            return new Vector2(Sd(p + new Vector2(e, 0)) - Sd(p - new Vector2(e, 0)), Sd(p + new Vector2(0, e)) - Sd(p - new Vector2(0, e))).normalized;
        }

        void Update()
        {
            if (ws == null) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (fahren)
            {
                int n = Mathf.CeilToInt(dt / .004f); float h = dt / n;
                for (int k = 0; k < n; k++) Schritt(h);
            }
            foreach (var w in ws)
            {
                w.bpv += (-w.bp * 140f - w.bpv * 7f) * dt; w.bp += w.bpv * dt;
                w.brv += (-w.br * 140f - w.brv * 7f) * dt; w.br += w.brv * dt;
                w.t.SetPositionAndRotation(new Vector3(w.p.x, 0, w.p.y), Quaternion.Euler(0, w.yaw * Mathf.Rad2Deg, 0));
                if (w.body != null) w.body.localRotation = Quaternion.Euler(w.bp, 0, w.br);
                if (w.rad != null) w.rad.localRotation = w.radStart * Quaternion.Euler(0, 0, -w.steer * 120f);
            }
        }

        void Schritt(float h)
        {
            // ---- Fahrer: Ziel waehlen, hinlenken, Gas geben ----
            foreach (var w in ws)
            {
                w.timer -= h;
                if (w.timer < 0)
                {
                    w.ziel = new Vector2(Random.Range(-halb.x + 2.5f, halb.x - 2.5f), Random.Range(-halb.y + 2.2f, halb.y - 2.2f));
                    w.gas = Random.value < .12f ? -.6f : Random.Range(.55f, 1f);
                    w.timer = Random.Range(1.5f, 4.5f);
                }
                Vector2 f = Fwd(w.yaw), to = w.ziel - w.p;
                float want = Mathf.Atan2(to.x, to.y);
                float diff = Mathf.DeltaAngle(w.yaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                if (w.gas < 0) diff = -diff * .5f;
                w.steer = Mathf.MoveTowards(w.steer, Mathf.Clamp(diff * 1.4f, -1f, 1f), h * 2.5f);

                Vector2 lat = new Vector2(f.y, -f.x);
                float vl = Vector2.Dot(w.v, f), vs = Vector2.Dot(w.v, lat);
                vl = Mathf.MoveTowards(vl, tempo * w.gas, h * 1.6f);
                vs *= Mathf.Exp(-h * 2.5f);                   // seitlich rutscht der Wagen nach einem Stoss noch etwas
                w.v = f * vl + lat * vs;
                w.w = Mathf.Lerp(w.w, w.steer * 1.5f, 1 - Mathf.Exp(-h * 3f));   // Drehung (auch im Stand, wie beim echten Antriebsrad)
                w.yaw += w.w * h; w.p += w.v * h;
            }

            // ---- Bande ----
            foreach (var w in ws)
            {
                Vector2 f = Fwd(w.yaw);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 off = f * OFF * s, c = w.p + off;
                    float d = Sd(c);
                    if (d <= -R) continue;
                    Vector2 n = SdN(c);
                    w.p -= n * (d + R);
                    float vn = Vector2.Dot(w.v, n);
                    if (vn > 0)
                    {
                        Vector2 J = -n * vn * 1.55f;
                        w.v += J; w.w += Cross(off, J) * 1.4f;
                        if (vn > .5f) Stoss(w, vn, new Vector3(n.x, 0, n.y));
                        w.timer = Mathf.Min(w.timer, Random.Range(.2f, .8f));
                    }
                }
            }

            // ---- Wagen untereinander ----
            for (int i = 0; i < ws.Length; i++)
                for (int j = i + 1; j < ws.Length; j++)
                {
                    W a = ws[i], b = ws[j];
                    if ((a.p - b.p).sqrMagnitude > 9f) continue;
                    Vector2 fa = Fwd(a.yaw), fb = Fwd(b.yaw);
                    for (int sa = -1; sa <= 1; sa += 2)
                        for (int sb = -1; sb <= 1; sb += 2)
                        {
                            Vector2 oa = fa * OFF * sa, ob = fb * OFF * sb, d = (b.p + ob) - (a.p + oa);
                            float dist = d.magnitude; if (dist >= 2 * R || dist < 1e-4f) continue;
                            Vector2 n = d / dist; float pen = 2 * R - dist;
                            a.p -= n * pen * .5f; b.p += n * pen * .5f;
                            float rel = Vector2.Dot(a.v - b.v, n);
                            if (rel <= 0) continue;
                            Vector2 J = n * rel * (1f + .7f) * .5f;
                            a.v -= J; b.v += J;
                            a.w += Cross(oa, -J) * 1.6f; b.w += Cross(ob, J) * 1.6f;
                            if (rel > .4f) { Stoss(a, rel, new Vector3(n.x, 0, n.y)); Stoss(b, rel, -new Vector3(n.x, 0, n.y)); }
                        }
                }
        }

        void Stoss(W w, float staerke, Vector3 richtungWelt)
        {
            // richtungWelt zeigt von der Mitte des Wagens zur Stossstelle
            Vector3 nl = w.t.InverseTransformDirection(richtungWelt);
            w.bpv += nl.z * staerke * 22f; w.brv -= nl.x * staerke * 22f;
            if (w.li != null && Random.value < .7f) w.li.Funke(Mathf.RoundToInt(15 + staerke * 25));
            if (w.ton != null && rumms != null) { w.ton.pitch = Random.Range(.8f, 1.15f); w.ton.PlayOneShot(rumms, Mathf.Clamp01(staerke / 3f) * rummsLautstaerke); }
        }

        /// <summary>Dumpfer Gummi-Rumms, im Programm erzeugt (tiefer Ton mit Rauschen, schnell abklingend).</summary>
        static AudioClip MacheRumms()
        {
            const int rate = 44100; int n = rate / 3; var d = new float[n];
            var rnd = new System.Random(3); float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 18f);
                lp += (((float)rnd.NextDouble() * 2 - 1) - lp) * .08f;
                d[i] = (Mathf.Sin(2 * Mathf.PI * (70f - 30f * t) * t) * .8f + lp * 1.6f) * env * .7f;
            }
            var c = AudioClip.Create("Rumms", n, 1, rate, false); c.SetData(d, 0); return c;
        }
    }
}
