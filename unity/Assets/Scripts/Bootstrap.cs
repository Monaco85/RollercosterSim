using UnityEngine;

namespace Achterbahn
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;
            new GameObject("Achterbahn").AddComponent<GameController>();
        }
    }
}
