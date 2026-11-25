using UnityEngine;
// Pruefkamera fest am Boxauto (faehrt mit), damit Nahbilder trotz Bewegung passen.
public static class AgentBoxCam {
  public static string Car(int i, float ang, float dist, float h) {
    var car = Achterbahn.FairWorld.JoustCars[i];
    var go = GameObject.Find("PruefKamera"); Camera cam;
    if (go == null) { go = new GameObject("PruefKamera"); cam = go.AddComponent<Camera>(); cam.CopyFrom(Camera.main); cam.depth = Camera.main.depth + 10; }
    else cam = go.GetComponent<Camera>();
    go.transform.SetParent(car, false);
    go.transform.localPosition = Quaternion.Euler(0, ang, 0) * Vector3.forward * dist + Vector3.up * h;
    go.transform.LookAt(car.position + Vector3.up * .6f); cam.fieldOfView = 45;
    return "ok " + car.name; }
  public static string Go() { var go = GameObject.Find("PruefKamera"); if (go != null) Object.Destroy(go); return "weg"; }
}
