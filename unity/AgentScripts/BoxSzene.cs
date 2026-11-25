using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class BoxSzene {
  public static string Info() {
    var ps = EditorSceneManager.playModeStartScene;
    return "aktiv: " + SceneManager.GetActiveScene().path + " | startScene: " + (ps != null ? UnityEditor.AssetDatabase.GetAssetPath(ps) : "null");
  }
  public static string Oeffnen() { EditorSceneManager.OpenScene("Assets/Boxauto/Boxauto_Vorschau.unity", OpenSceneMode.Single); return Info(); }
  public static string Spiel() { EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); return Info(); }
}
