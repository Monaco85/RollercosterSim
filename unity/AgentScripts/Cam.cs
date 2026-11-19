using UnityEngine;
// Fester Pruefblick: setzt eine Zusatzkamera, die bis Clear() bleibt
public static class AgentCam {
  public static string Look(float x, float y, float z, float tx, float ty, float tz, float fov) {
    var go = GameObject.Find("PruefKamera"); Camera cam;
    if (go == null) { go = new GameObject("PruefKamera"); cam = go.AddComponent<Camera>(); cam.CopyFrom(Camera.main); cam.depth = Camera.main.depth + 10;
      var cd = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam); cd.renderPostProcessing = true; cd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
    else cam = go.GetComponent<Camera>();
    go.transform.position = new Vector3(x, y, z); go.transform.LookAt(new Vector3(tx, ty, tz)); cam.fieldOfView = fov; return "ok"; }
  public static string Clear() { var go = GameObject.Find("PruefKamera"); if (go != null) Object.Destroy(go); return "weg"; }
}
