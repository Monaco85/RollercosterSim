using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Achterbahn.EditorTools
{
    public static class AchterbahnMenu
    {
        const string MatPath = "Assets/Resources/AchterbahnLit.mat";

        // Grundmaterial als Asset, damit der URP-Lit-Shader sicher mit in die EXE kommt.
        [InitializeOnLoadMethod]
        static void EnsureMaterial()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(MatPath) != null) return;
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) return;
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(new Material(sh), MatPath);
                AssetDatabase.SaveAssets();
            };
        }

        const string SeatMenu = "Achterbahn/Sitz im Editor verbinden";

        [MenuItem(SeatMenu)]
        static void ToggleSeat()
        {
            bool on = PlayerPrefs.GetInt("editorSitzFreigabe", 0) == 1;
            if (!on && !EditorUtility.DisplayDialog("Sitz im Editor verbinden",
                "Der Play-Modus schickt dann Bewegungsdaten an die laufende Brücke und kann den echten Sitz bewegen.\n\nNiemand auf dem Sitz?", "Ja, verbinden", "Abbrechen"))
                return;
            PlayerPrefs.SetInt("editorSitzFreigabe", on ? 0 : 1); PlayerPrefs.Save();
        }

        [MenuItem(SeatMenu, true)]
        static bool ToggleSeatCheck() { Menu.SetChecked(SeatMenu, PlayerPrefs.GetInt("editorSitzFreigabe", 0) == 1); return true; }

        [MenuItem("Achterbahn/Programm bauen")]
        public static void BuildExe()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MatPath) == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")), MatPath);
                AssetDatabase.SaveAssets();
            }
            string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build");
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = Path.Combine(outDir, "Achterbahn.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            PlayerSettings.productName = "Achterbahn";
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Achterbahn-Build: " + report.summary.result + " -> " + opts.locationPathName);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Build fehlgeschlagen: " + report.summary.result);
        }
    }
}
