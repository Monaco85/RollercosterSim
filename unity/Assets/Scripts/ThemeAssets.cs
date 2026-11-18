using UnityEngine;

namespace Achterbahn
{
    /// <summary>Verweise auf die Poly-Haven-Materialien und -Modelle. Liegt in Resources, damit alles mit in die EXE kommt.</summary>
    [CreateAssetMenu(menuName = "Achterbahn/Theme Assets")]
    public class ThemeAssets : ScriptableObject
    {
        public Material sky;
        public Material grass, path, cobble;
        public Material castleWall, stoneBlocks, timberWall, roofSlate, roofThatch;
        public Material woodStructure, woodDeck;
        public Material treeBark, treeBranch;
        public Material terrain, grassCard, smoke, fire;
        public TerrainLayer[] layers;   // 0 Wiese, 1 Gras/Laub, 2 Erdweg, 3 Waldboden, 4 Vulkangestein
        public Material dinoSkin, palmBark, frond, rockWall;
        public Material water, leaves;
        public GameObject[] conifers, broadleaf, shrubs, rocks, props;
        public GameObject castleDoor, lantern;

        static ThemeAssets cached; static bool tried;
        public static ThemeAssets Get()
        {
            if (!tried) { cached = Resources.Load<ThemeAssets>("ThemeAssets"); tried = true; }
            return cached;
        }
    }
}
