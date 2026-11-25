using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Lichtshow am Boxauto: Lauflicht-Birnen (mehrere Muster), Scheinwerfer, Rueckleuchten, Unterbodenlicht,
    /// Funken am Stromabnehmer mit Blitzlicht und kurzem Flackern der Lampen. Nur Optik.
    /// </summary>
    public class BoxautoLichter : MonoBehaviour
    {
        public Renderer[] birnen;
        public Light[] scheinwerfer;
        public Renderer scheinwerferGlas, ruecklichtGlas, unterboden, lampeGruen, lampeGelb;
        public Light[] unterLichter;
        public ParticleSystem funken;
        public Light funkenLicht;
        public Transform lenkrad;

        [Header("Einstellungen")]
        public bool an = true;
        [Tooltip("-1 = Muster wechseln automatisch")] public int muster = -1;
        public float funkenProSekunde = .6f;
        public float birnenHelligkeit = 9f;
        public bool lenkradWackeln = true;

        public static readonly string[] MusterNamen = { "Lauflicht", "Wechselblinker", "Funkeln", "Regenbogen", "Alle an" };
        static readonly Color[] Farben =
        {
            new Color(1f, .78f, .38f), new Color(1f, .25f, .2f), new Color(1f, .85f, .2f),
            new Color(.3f, .95f, .4f), new Color(.3f, .6f, 1f), new Color(1f, .35f, .85f)
        };
        static readonly int Emis = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock mpb;
        float blitz, naechsterFunke, flacker;
        float[] funkel;
        Quaternion lenkradStart;

        public int AktivesMuster => muster >= 0 ? muster : (int)(Time.time / 8f) % MusterNamen.Length;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            funkel = new float[birnen != null ? birnen.Length : 0];
            naechsterFunke = Time.time + 1f;
            if (lenkrad != null) lenkradStart = lenkrad.localRotation;
            // Sicherheitshalber: Leucht-Materialien brauchen das Emissions-Keyword
            var alle = new System.Collections.Generic.List<Renderer> { scheinwerferGlas, ruecklichtGlas, unterboden, lampeGruen, lampeGelb };
            if (birnen != null && birnen.Length > 0) alle.Add(birnen[0]);
            foreach (var r in alle) if (r != null && r.sharedMaterial != null) r.sharedMaterial.EnableKeyword("_EMISSION");
        }

        void Set(Renderer r, Color c)
        {
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb); mpb.SetColor(Emis, c); r.SetPropertyBlock(mpb);
        }

        public void Funke(int menge = 40)
        {
            if (funken != null) funken.Emit(menge);
            blitz = 1f; flacker = .25f;
        }

        void Update()
        {
            float t = Time.time, dt = Time.deltaTime;

            // Funken: zufaellige Kontakte am Fahrdraht-Netz
            if (an && funkenProSekunde > 0 && t > naechsterFunke)
            {
                Funke(Random.Range(12, 45));
                naechsterFunke = t + Random.Range(.2f, 2f) / funkenProSekunde;
                if (Random.value < .3f) naechsterFunke = t + Random.Range(.05f, .12f);   // Doppelfunke
            }
            blitz = Mathf.Max(0, blitz - dt * 9f); flacker = Mathf.Max(0, flacker - dt);
            if (funkenLicht != null) { funkenLicht.intensity = blitz * blitz * 14f * Random.Range(.6f, 1f); funkenLicht.enabled = blitz > .01f; }
            // Spannungseinbruch beim Funken: Lampen flackern kurz
            float volt = an ? (flacker > 0 ? Random.Range(.55f, 1f) : 1f) : 0f;

            // Lauflicht
            int m = AktivesMuster;
            if (birnen != null)
            {
                int n = birnen.Length;
                for (int i = 0; i < n; i++)
                {
                    Color c = Farben[i % Farben.Length]; float k;
                    switch (m)
                    {
                        case 0: { float ph = Mathf.Repeat(i / 3f - t * 6f, 1f); k = ph < .34f ? 1f : .06f; break; }
                        case 1: k = ((i + (int)(t * 2.5f)) % 2 == 0) ? 1f : .05f; break;
                        case 2:
                            if (funkel[i] <= 0 && Random.value < dt * 1.6f) funkel[i] = 1f;
                            funkel[i] = Mathf.Max(0, funkel[i] - dt * 2.2f);
                            k = .12f + funkel[i] * funkel[i]; c = Color.Lerp(new Color(1f, .8f, .5f), Color.white, funkel[i]); break;
                        case 3: c = Color.HSVToRGB(Mathf.Repeat(i / (float)n - t * .25f, 1f), .85f, 1f); k = .9f; break;
                        default: c = new Color(1f, .82f, .55f); k = .9f; break;
                    }
                    Set(birnen[i], c * (k * birnenHelligkeit * volt));
                }
            }

            Set(scheinwerferGlas, new Color(1f, .93f, .8f) * 7f * volt);
            if (scheinwerfer != null) foreach (var l in scheinwerfer) if (l != null) { l.enabled = an; l.intensity = 3.2f * volt; }
            float bremse = .6f + .4f * Mathf.Sin(t * 1.3f);
            Set(ruecklichtGlas, new Color(1f, .05f, .03f) * (2.2f + 2.5f * bremse) * volt);
            Color glow = Color.HSVToRGB(Mathf.Repeat(.52f + .12f * Mathf.Sin(t * .4f), 1f), .9f, 1f);
            Set(unterboden, glow * 4f * volt);
            if (unterLichter != null) foreach (var l in unterLichter) if (l != null) { l.enabled = an; l.color = glow; l.intensity = 2.5f * volt; }
            Set(lampeGruen, new Color(.2f, 1f, .3f) * 3f * volt);
            Set(lampeGelb, new Color(1f, .7f, .1f) * (blitz > .05f ? 6f : .3f) * volt);

            if (lenkrad != null && lenkradWackeln)
                lenkrad.localRotation = lenkradStart * Quaternion.Euler(0, 0, Mathf.Sin(t * .7f) * 35f + Mathf.Sin(t * 2.3f) * 8f);
        }
    }
}
