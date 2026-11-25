using System.Reflection;
using UnityEngine;

// Testhilfe Bahn 9 in der Autoscooter-Halle (nur Play-Modus im Editor; schickt nichts an den Sitz, der ist im Editor gesperrt)
public static class HalleTest
{
    static Achterbahn.GameController G() => Object.FindFirstObjectByType<Achterbahn.GameController>();
    static object Call(string m, params object[] a) => typeof(Achterbahn.GameController).GetMethod(m, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(G(), a);
    static void S(string f, object v) => typeof(Achterbahn.GameController).GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(G(), v);
    static object F(string f) => typeof(Achterbahn.GameController).GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(G());

    public static string Sitz() => "editorSitzFreigabe=" + PlayerPrefs.GetInt("editorSitzFreigabe", 0);
    public static string Load() { Call("Load", 8); return Info(); }
    public static string Start() { Call("StartRide"); return "fahrt"; }
    public static string View(int v) { S("view", v); return "ansicht " + v; }
    public static string State() => "state=" + F("state") + " view=" + F("view");

    public static string Info()
    {
        var h = GameObject.Find("Autoscooter-Halle");
        var cars = Achterbahn.FairWorld.JoustCars;
        string s = "halle=" + (h != null ? h.transform.position.ToString() : "FEHLT") + " scooter=" + Achterbahn.FairWorld.ScooterHall;
        if (cars != null) foreach (var c in cars) s += " | " + (c != null ? c.name + c.position : "null");
        s += " | leute=" + (Achterbahn.Boxauto.BoxautoLeute.Aktiv != null);
        return s;
    }

    // Feste Pruefkamera (bleibt bis Frei())
    public static string Cam(float x, float y, float z, float tx, float ty, float tz, float fov)
    {
        var c = Camera.main; var go = GameObject.Find("PruefKamera"); if (go == null) go = new GameObject("PruefKamera");
        var cam = go.GetComponent<Camera>(); if (cam == null) cam = go.AddComponent<Camera>();
        cam.CopyFrom(c); cam.depth = c.depth + 10; cam.fieldOfView = fov;
        go.transform.position = new Vector3(x, y, z); go.transform.LookAt(new Vector3(tx, ty, tz));
        var d = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam); d.renderPostProcessing = true;
        return "kamera";
    }
    public static string Frei() { var go = GameObject.Find("PruefKamera"); if (go != null) Object.Destroy(go); return "frei"; }
    public static string Fps() => $"fps={1f / Time.smoothDeltaTime:0}";
}
