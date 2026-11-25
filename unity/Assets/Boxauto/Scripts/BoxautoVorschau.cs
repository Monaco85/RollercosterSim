using UnityEngine;
using UnityEngine.InputSystem;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Vorfuehrszene der Boxautobahn: Kameras (Rundflug, Wagen ansehen, Mitfahren, Verfolger), Wagenwahl, Lackfarbe,
    /// Lichtmuster, Funken, Hallenlicht, Pause der Demo-Fahrt. Haelt die Spiegelungs-Sonde aktuell.
    /// </summary>
    public class BoxautoVorschau : MonoBehaviour
    {
        public Camera kamera;
        public BoxautoDemo demo;
        public ReflectionProbe sonde;
        public GameObject hallenLicht;
        public Vector3 bahnMitte = new Vector3(0, 1.2f, 0);

        public int modus;                   // 0 Rundflug, 1 Wagen ansehen, 2 Mitfahren, 3 Verfolger
        public int wagenNr;
        public float abstand = 19f, hoehe = 7f, winkel = 200f, autoDrehen = 5f;
        public bool hilfe = true;

        static readonly string[] Modi = { "Rundflug", "Wagen ansehen", "Mitfahren", "Verfolger" };
        static readonly (string name, Color c)[] Lacke =
        {
            ("Kirschrot", new Color(.62f, .02f, .04f)), ("Königsblau", new Color(.03f, .12f, .55f)), ("Giftgrün", new Color(.12f, .5f, .04f)),
            ("Sonnengelb", new Color(.85f, .55f, .02f)), ("Pink", new Color(.75f, .05f, .38f)), ("Schwarz", new Color(.02f, .02f, .025f)),
        };
        BoxautoLichter[] alleLichter;
        BoxautoLeute leute;
        float sondeT, stillSeit = -99f;
        Vector3 folgePos; bool folgeInit;

        void Start()
        {
            // Leuchtende Materialien: Emissions-Keyword sicherstellen
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.HasProperty("_EmissionColor") && m.shader.name.StartsWith("Universal Render Pipeline/") && m.GetColor("_EmissionColor").maxColorComponent > .01f)
                        m.EnableKeyword("_EMISSION");
            alleLichter = FindObjectsByType<BoxautoLichter>(FindObjectsSortMode.None);
            if (sonde != null) sonde.RenderProbe();
        }

        Transform Gewaehlt => demo != null ? demo.Wagen(wagenNr) : null;

        void SetzeModus(int m)
        {
            modus = (m + Modi.Length) % Modi.Length; folgeInit = false;
            if (modus == 0) { abstand = 19f; hoehe = 7f; }
            if (modus == 1) { abstand = 4.2f; hoehe = 1.6f; }
            kamera.fieldOfView = modus == 2 ? 72f : modus == 0 ? 45f : 50f;
        }

        void Update()
        {
            var kb = Keyboard.current; var ms = Mouse.current;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame) SetzeModus(modus + 1);
                if (kb.eKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) { wagenNr++; folgeInit = false; }
                if (kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) { wagenNr--; folgeInit = false; }
                if (kb.pKey.wasPressedThisFrame && demo != null) demo.fahren = !demo.fahren;
                if (kb.spaceKey.wasPressedThisFrame && alleLichter.Length > 0)
                {
                    int next = (alleLichter[0].AktivesMuster + 1) % BoxautoLichter.MusterNamen.Length;
                    foreach (var l in alleLichter) l.muster = next;
                }
                if (kb.lKey.wasPressedThisFrame) foreach (var l in alleLichter) l.an = !l.an;
                if (kb.fKey.wasPressedThisFrame) { var g = Gewaehlt; if (g != null) g.GetComponent<BoxautoLichter>()?.Funke(90); }
                if (kb.nKey.wasPressedThisFrame && hallenLicht != null) hallenLicht.SetActive(!hallenLicht.activeSelf);
                if (kb.rKey.wasPressedThisFrame) autoDrehen = autoDrehen == 0 ? 5f : 0f;
                if (kb.hKey.wasPressedThisFrame) hilfe = !hilfe;
                Key[] ziffern = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
                for (int k = 0; k < Lacke.Length; k++)
                    if (kb[ziffern[k]].wasPressedThisFrame) { var g = Gewaehlt; if (g != null) g.GetComponent<BoxautoFarbe>()?.Setze(Lacke[k].c); }
            }
            if (ms != null && (modus == 0 || modus == 1))
            {
                Vector2 dm = ms.delta.ReadValue();
                if (ms.leftButton.isPressed) { winkel += dm.x * .25f; hoehe = Mathf.Clamp(hoehe - dm.y * (modus == 0 ? .04f : .006f), .3f, modus == 0 ? 25f : 5f); stillSeit = Time.time; }
                float sc = ms.scroll.ReadValue().y;
                if (sc != 0) abstand = Mathf.Clamp(abstand * (1f - Mathf.Sign(sc) * .1f), 1.4f, 45f);
            }
            if (Time.time - stillSeit > 3f) winkel += autoDrehen * Time.deltaTime;
            Kamera();
            if (sonde != null && Time.time > sondeT) { sonde.RenderProbe(); sondeT = Time.time + .15f; }
        }

        void Kamera()
        {
            if (kamera == null) return;
            var w = Gewaehlt;
            // Beim Mitfahren den eigenen Fahrer ausblenden (sonst sitzt die Kamera in seinem Kopf)
            if (leute == null) leute = FindFirstObjectByType<BoxautoLeute>();
            if (leute != null && demo != null && demo.Anzahl > 0)
            {
                int sel = ((wagenNr % demo.Anzahl) + demo.Anzahl) % demo.Anzahl;
                for (int i = 0; i < demo.Anzahl; i++) { var r = leute.Fahrer(i); if (r != null) r.enabled = !(modus == 2 && i == sel); }
            }
            if (modus == 0 || (modus == 1 && w != null))
            {
                Vector3 fokus = modus == 0 ? bahnMitte : w.position + Vector3.up * .6f;
                Vector3 dir = Quaternion.Euler(0, winkel, 0) * Vector3.forward;
                Vector3 ziel = fokus + dir * abstand + Vector3.up * (hoehe - (modus == 0 ? bahnMitte.y : .6f));
                kamera.transform.position = ziel; kamera.transform.LookAt(fokus);
            }
            else if (modus == 2 && w != null)
            {
                // Augen des Fahrers: hinter dem Lenkrad, leicht nach unten geneigt; Aufbau wippt mit
                var body = w.Find("Aufbau") ?? w;
                kamera.transform.SetPositionAndRotation(body.TransformPoint(new Vector3(0, 1.22f, -.36f)), body.rotation * Quaternion.Euler(9, 0, 0));
            }
            else if (modus == 3 && w != null)
            {
                Vector3 soll = w.position - w.forward * 4.8f + Vector3.up * 2.3f;
                if (!folgeInit) { folgePos = soll; folgeInit = true; }
                folgePos = Vector3.Lerp(folgePos, soll, 1 - Mathf.Exp(-Time.deltaTime * 3f));
                kamera.transform.position = folgePos; kamera.transform.LookAt(w.position + Vector3.up * .8f + w.forward * 1.5f);
            }
        }

        void OnGUI()
        {
            if (!hilfe) return;
            var st = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, richText = true };
            st.normal.textColor = Color.white;
            string muster = alleLichter != null && alleLichter.Length > 0 ? BoxautoLichter.MusterNamen[alleLichter[0].AktivesMuster] : "-";
            int nr = demo != null && demo.Anzahl > 0 ? ((wagenNr % demo.Anzahl) + demo.Anzahl) % demo.Anzahl + 1 : 0;
            GUI.Box(new Rect(12, 12, 390, 250),
                "<b>Autoscooter – Vorschau</b>\n" +
                "C: Kamera  (" + Modi[modus] + ")\n" +
                "Q / E oder ← / →: Wagen wählen  (Nr. " + nr + ")\n" +
                "Maus ziehen: drehen und Höhe   Rad: Zoom\n" +
                "1–6: Lackfarbe des Wagens   F: Funken\n" +
                "Leertaste: Lichtmuster  (" + muster + ")\n" +
                "L: Wagenlicht   N: Hallenlicht   P: Fahrt anhalten\n" +
                "R: Automatisch drehen   H: Hilfe aus", st);
        }
    }
}
