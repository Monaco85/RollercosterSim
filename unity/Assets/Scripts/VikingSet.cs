using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Verweise auf Prefabs aus „Viking Village“ (Unity Asset Store, Standard-EULA).
    /// Die Instanz liegt unter Assets/Viking Village/Resources und ist damit nicht im oeffentlichen Repo.
    /// Fehlt sie, baut das Thema „wikinger“ das prozedurale Ersatzdorf.
    /// </summary>
    public class VikingSet : ScriptableObject
    {
        public GameObject[] bigHouses, houses, storages, smithies;
        public GameObject tower, gate, crane, wallPanel, wallCorner;
        public GameObject[] boats;
        public GameObject dragonhead;
        public GameObject[] barrels, logs, planks, fences, sheds, shields, weapons, menhirs, boulders, misc;
        public GameObject plankPath, scaffold, skullStick, fishStick, runeStone;
        public GameObject[] plants, grass;
        public GameObject mountains;

        static VikingSet cached; static bool tried;
        public static VikingSet Get()
        {
            if (!tried) { cached = Resources.Load<VikingSet>("VikingSet"); tried = true; }
            return cached;
        }
    }
}
