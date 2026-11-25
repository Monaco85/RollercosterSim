using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Moving Head fuer Prefabs/Szenen (eigene Datei, damit Unity das Skript speichern kann – MovingHead in FairFx.cs geht dafuer nicht).
    /// Schwenkt, nickt und wechselt langsam die Farbe von Spot und Lichtkegel.
    /// </summary>
    public class BoxautoMovingHead : MonoBehaviour
    {
        public Light spot; public Material beam; public float speed = .35f, offset;
        float ph;
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
}
