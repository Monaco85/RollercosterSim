using UnityEngine;

namespace Achterbahn
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;
            // Boxauto-Vorschauszene: kein Spiel, keine Bruecke
            if (Object.FindFirstObjectByType<Achterbahn.Boxauto.BoxautoVorschau>() != null) return;
            new GameObject("Achterbahn").AddComponent<GameController>();
        }
    }
}
