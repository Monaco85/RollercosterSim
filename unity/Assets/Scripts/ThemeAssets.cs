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
        // Rummel in der Daemmerung
        public Material duskSky;
        public Material asphalt, canvas, paintedWood, corrugated, lacquer;   // ambientCG (CC0)
        public Material bulb, halo, beam, neon, spark, crowd;               // eigene Shader
        public GameObject[] people;                                         // Quaternius (CC0), mit Animationen
        public AudioClip fairAmbience, organ, carouselMusic, chainLift, screams, bumperCars, fireworks, bangBig, bang, coasterPass;
        // Hintergrundmusik je Bahn (freesound.org, CC0)
        public AudioClip musicRaupe, musicBerg, musicDino, musicMonster, musicBurg;
        // Boxauto im Burghof (Poly Haven, CC0, glTF)
        public GameObject chandelier, kiteShield, warHammer, estoc, chest, wineBarrel, lionHead, firePit, bucket, cannon;

        static ThemeAssets cached; static bool tried;
        public static ThemeAssets Get()
        {
            if (!tried) { cached = Resources.Load<ThemeAssets>("ThemeAssets"); tried = true; }
            return cached;
        }
    }
}
