using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Haengt an der Spiel-Halle (Resources/Boxautobahn_Spiel.prefab): kennt das Wagen-Prefab und die Startnummern,
    /// stellt die Leucht-Keywords sicher und rendert die Spiegelungs-Sonde. Nur Optik.
    /// </summary>
    public class BoxautoHalle : MonoBehaviour
    {
        public GameObject wagenPrefab;
        public Texture2D[] nummern;
        public ReflectionProbe sonde;
        public Vector2 halb = new Vector2(15f, 9.5f);
        public float steg = 1.8f, platz = -.5f;

        float sondeT;

        void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.HasProperty("_EmissionColor") && m.shader.name.StartsWith("Universal Render Pipeline/") && m.GetColor("_EmissionColor").maxColorComponent > .01f)
                        m.EnableKeyword("_EMISSION");
            sondeT = Time.time + .3f;   // erst wenn die Wagen stehen
        }

        void Update()
        {
            // Spiegelung selten auffrischen (Wagen fahren durch die Halle); kostet sonst zu viel
            if (sonde != null && Time.time > sondeT) { sonde.RenderProbe(); sondeT = Time.time + 2f; }
        }

        /// <summary>Startnummer 1..8 als Textur (oder null).</summary>
        public Texture2D Nummer(int nr) => nummern != null && nr >= 0 && nr < nummern.Length ? nummern[nr] : null;
    }
}
