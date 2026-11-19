using UnityEditor;
using UnityEngine;

// Legt Assets/Resources/AchterbahnEmit.mat an (Lit mit eingeschalteter Emission), damit die Leucht-Variante im Build enthalten ist.
public static class AgentMakeEmit
{
    public static string Main()
    {
        var b = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/AchterbahnLit.mat");
        if (b == null) return "AchterbahnLit fehlt";
        var m = new Material(b);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.white * 2f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(m, "Assets/Resources/AchterbahnEmit.mat");
        AssetDatabase.SaveAssets();
        return "ok";
    }
}
