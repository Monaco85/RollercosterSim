using UnityEditor;

public static class Refresh
{
    public static string Run()
    {
        EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        return "Refresh geplant";
    }

    public static string Now()
    {
        AssetDatabase.Refresh(ImportAssetOptions.Default);
        return "Refresh fertig: " + AssetDatabase.FindAssets("t:Material", new[] { "Assets/Viking Village" }).Length + " Materialien";
    }
}
