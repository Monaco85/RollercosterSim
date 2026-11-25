using UnityEngine;

namespace Achterbahn.Boxauto
{
    /// <summary>Lackfarbe und Startnummer eines einzelnen Wagens (per MaterialPropertyBlock, auch im Editor sichtbar).</summary>
    [ExecuteAlways]
    public class BoxautoFarbe : MonoBehaviour
    {
        public Color lack = new Color(.62f, .02f, .04f);
        public Texture2D nummer;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap");
        MaterialPropertyBlock mpb;

        void OnEnable() => Anwenden();
        void OnValidate() => Anwenden();

        public void Setze(Color c) { lack = c; Anwenden(); }

        public void Anwenden()
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Lack") { r.GetPropertyBlock(mpb); mpb.SetColor(BaseColor, lack); r.SetPropertyBlock(mpb); }
                else if (r.name == "Nummer" && nummer != null) { r.GetPropertyBlock(mpb); mpb.SetTexture(BaseMap, nummer); r.SetPropertyBlock(mpb); }
            }
        }
    }
}
