using UnityEngine;
public static class AgentRec {
  // Fahrt auf Bahn i, Sitzansicht, nach 'wait' Sekunden Spielzeit 'frames' Bilder
  public static string Ride(int track, string dir, int frames) {
    AgentDrive.Load(track); AgentDrive.View(0); AgentDrive.Start();
    var r = Achterbahn.ClipRecorder.Start(Camera.main, dir, frames); return "laeuft"; }
  public static string Orbit(int track, string dir, int frames, float cx, float cz, float radius, float h) {
    AgentDrive.Load(track);
    var go = new GameObject("PruefKamera"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(Camera.main); cam.enabled = false; cam.fieldOfView = 55;
    var cd = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam); cd.renderPostProcessing = true; cd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    var r = Achterbahn.ClipRecorder.Start(cam, dir, frames); r.orbit = true; r.center = new Vector3(cx, 0, cz); r.radius = radius; r.height3 = h; r.orbitSpeed = .12f; return "laeuft"; }
  public static string State() { var r = Object.FindFirstObjectByType<Achterbahn.ClipRecorder>(); return r == null ? "fertig" : "bild " + r.Done; }
}
