using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>Neon-Schriftzug: Buchstaben brummen leicht, ab und zu flackert eine Roehre, eine ist "altersschwach".</summary>
    public class NeonFlimmern : MonoBehaviour
    {
        public int schwacherBuchstabe = -1;
        Renderer[] r; Color[] basis; float[] aus, naechst;
        MaterialPropertyBlock mpb;
        static readonly int Emis = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            r = GetComponentsInChildren<Renderer>();
            basis = new Color[r.Length]; aus = new float[r.Length]; naechst = new float[r.Length];
            for (int i = 0; i < r.Length; i++) { basis[i] = r[i].sharedMaterial.GetColor(Emis); naechst[i] = Time.time + Random.Range(2f, 20f); }
            mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < r.Length; i++)
            {
                if (t > naechst[i])
                {
                    aus[i] = Random.Range(.04f, i == schwacherBuchstabe ? .6f : .15f);
                    naechst[i] = t + (i == schwacherBuchstabe ? Random.Range(.1f, 3f) : Random.Range(6f, 30f));
                }
                aus[i] -= Time.deltaTime;
                float k = aus[i] > 0 ? (Random.value < .5f ? .05f : .5f) : .93f + .07f * Mathf.Sin(t * 120f + i);
                r[i].GetPropertyBlock(mpb); mpb.SetColor(Emis, basis[i] * k); r[i].SetPropertyBlock(mpb);
            }
        }
    }
}
