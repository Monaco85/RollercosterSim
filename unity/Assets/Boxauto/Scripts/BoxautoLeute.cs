using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Menschen fuer die Autoscooter-Vorschau: ein Fahrer pro Wagen (sitzend, ab und zu Arme hoch),
    /// Zuschauer auf dem Laufsteg und vor dem Podest (schauen den Wagen nach, winken), eine Schlange an der Kasse.
    /// Nutzt die Quaternius-Figuren aus CrowdBank (wie auf dem Rummel). Nur Optik.
    /// </summary>
    public class BoxautoLeute : MonoBehaviour
    {
        public BoxautoDemo demo;
        public Transform[] wagen;                      // im Spiel statt demo: die Wagen, die das Fahrmodell bewegt
        public static BoxautoLeute Aktiv;              // fuer die Mitfahr-Kamera des Spiels
        public Vector2 halb = new Vector2(12f, 7f);
        public float steg = 1.8f, platz = -.5f;
        public int zuschauerSteg = 16, zuschauerPlatz = 8;
        [Range(0, 1)] public float kinderAnteil = .35f;

        List<CrowdBank.Template> templates;
        readonly List<FairPerson> zuschauer = new List<FairPerson>();
        readonly List<Renderer> fahrer = new List<Renderer>();
        float blickT;

        /// <summary>Fahrer-Renderer je Wagen (Index wie in demo.wagen), fuer die Mitfahr-Kamera.</summary>
        public Renderer Fahrer(int i) => i >= 0 && i < fahrer.Count ? fahrer[i] : null;

        FairPerson Mensch(Transform parent, Vector3 pos, float yaw, FairPerson.Act act, bool kind)
        {
            var T = templates[Random.Range(0, templates.Count)];
            var go = new GameObject(act == FairPerson.Act.Rider ? "Fahrer" : "Zuschauer");
            go.transform.SetParent(parent, false);
            float s = kind ? Random.Range(.62f, .74f) : Random.Range(.94f, 1.05f);
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.AddComponent<MeshFilter>().sharedMesh = T.mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = T.mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
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

        int Anzahl => demo != null ? demo.Anzahl : wagen != null ? wagen.Length : 0;
        Transform Wagen(int i) => demo != null ? demo.Wagen(i) : wagen[i];

        void OnDestroy() { if (Aktiv == this) Aktiv = null; }

        void Start()
        {
            Aktiv = this;
            templates = CrowdBank.Get(ThemeAssets.Get());
            if (templates == null || templates.Count == 0) { Debug.LogWarning("BoxautoLeute: keine Figuren (ThemeAssets/CrowdBank)"); return; }

            // ---- Fahrer ----
            var liste = demo != null ? demo.wagen : wagen;
            if (liste != null)
                foreach (var w in liste)
                {
                    var sitz = w.Find("Aufbau") ?? w;
                    bool kind = Random.value < kinderAnteil;
                    var p = Mensch(sitz, Vector3.zero, 0, FairPerson.Act.Rider, kind);
                    float s = p.transform.localScale.x;
                    p.transform.localPosition = new Vector3(0, .44f, kind ? -.26f : -.32f) - Vector3.up * (p.hipHeight * s - .04f);
                    p.sit = true;
                    float ph = Random.value * 100f;
                    p.armsUp = () => Mathf.Sin(Time.time * .37f + ph) > .94f;   // ab und zu: Juhu!
                    fahrer.Add(p.GetComponent<Renderer>());
                }

            // ---- Zuschauer auf dem Laufsteg (hinter der Bande) ----
            var root = new GameObject("Zuschauer").transform; root.SetParent(transform, false);
            float d = .8f;
            for (int i = 0; i < zuschauerSteg; i++)
            {
                Vector3 pos; int seite = i % 4; float t = Random.Range(-.75f, .75f);
                if (seite == 0) pos = new Vector3(t * halb.x, 0, -(halb.y + d));
                else if (seite == 1) pos = new Vector3(t * halb.x, 0, halb.y + d);
                else if (seite == 2) pos = new Vector3(halb.x + d, 0, t * halb.y * .8f);
                else pos = new Vector3(-(halb.x + d), 0, t * halb.y * .8f);
                float yaw = Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg + Random.Range(-25f, 25f);
                var p = Mensch(root, pos, yaw, FairPerson.Act.Watcher, Random.value < kinderAnteil);
                zuschauer.Add(p);
            }
            // ---- vor dem Podest auf dem Platz ----
            for (int i = 0; i < zuschauerPlatz; i++)
            {
                var pos = new Vector3(Random.Range(-(halb.x - 2f), halb.x - 2f), platz, -(halb.y + steg + Random.Range(2.2f, 5f)));
                if (Mathf.Abs(pos.x) < 4f) pos.x += Mathf.Sign(pos.x + .01f) * 4f;      // Treppe frei lassen
                var p = Mensch(root, pos, Random.Range(-20f, 20f), FairPerson.Act.Watcher, Random.value < kinderAnteil);
                p.lookAt = transform.TransformPoint(new Vector3(0, 1, 0));
                zuschauer.Add(p);
            }
            // ---- Schlange an der Kasse ----
            var kasseT = transform.parent != null ? transform.parent.Find("Kasse") : null;
            var kasse = kasseT != null ? kasseT.gameObject : GameObject.Find("Kasse");
            if (kasse != null)
            {
                var b = kasse.GetComponent<Renderer>().bounds;
                for (int i = 0; i < 4; i++)
                {
                    var pos = transform.InverseTransformPoint(new Vector3(b.center.x + Random.Range(-.15f, .15f), 0, b.min.z - .7f - i * .85f)); pos.y = platz;
                    Mensch(root, pos, Random.Range(-10f, 10f), FairPerson.Act.Queue, Random.value < kinderAnteil);
                }
            }
        }

        void Update()
        {
            // Zuschauer schauen dem naechsten Wagen nach
            if (Anzahl == 0 || Time.time < blickT) return;
            blickT = Time.time + .5f;
            foreach (var p in zuschauer)
            {
                if (p == null || p.transform.localPosition.y < -.1f && Random.value < .5f) continue;
                Transform best = null; float bd = 1e9f;
                for (int i = 0; i < Anzahl; i++) { var w = Wagen(i); if (w == null) continue; float dd = (w.position - p.transform.position).sqrMagnitude; if (dd < bd) { bd = dd; best = w; } }
                if (best != null) p.lookAt = best.position;
            }
        }
    }
}
