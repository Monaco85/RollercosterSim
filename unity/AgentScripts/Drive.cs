using System.Reflection;
using UnityEngine;

// Testhilfe im Play-Modus: Bahn laden, Fahrt starten, an Stelle springen, Kamera waehlen, FPS lesen.
public static class AgentDrive
{
    static Achterbahn.GameController G() => Object.FindFirstObjectByType<Achterbahn.GameController>();
    static object Call(string m, params object[] a) => typeof(Achterbahn.GameController).GetMethod(m, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(G(), a);
    static T F<T>(string f) => (T)typeof(Achterbahn.GameController).GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(G());
    static void S(string f, object v) => typeof(Achterbahn.GameController).GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(G(), v);

    public static string Load(int i) { Call("Load", i); return "geladen " + i; }
    public static string Start() { Call("StartRide"); return "fahrt"; }
    public static string View(int v) { S("view", v); return "ansicht " + v; }
    // Freie Kamera fuer Pruefbilder: Position und Blickziel, haelt die Spielkamera bis zum naechsten Frame fest
    public static string Look(float x, float y, float z, float tx, float ty, float tz)
    {
        var c = Camera.main; var go = new GameObject("PruefKamera"); var cam = go.AddComponent<Camera>();
        cam.CopyFrom(c); go.transform.position = new Vector3(x, y, z); go.transform.LookAt(new Vector3(tx, ty, tz));
        cam.depth = c.depth + 10; cam.fieldOfView = 55;
        Object.Destroy(go, 3f);
        return "kamera";
    }

    public static string Fps() => $"fps={1f / Time.smoothDeltaTime:0} tris(scene)? frame={Time.frameCount}";
}
