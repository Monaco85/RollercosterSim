using System.Reflection;
public static class AgentStart {
  public static string Main() {
    var g = UnityEngine.Object.FindFirstObjectByType<Achterbahn.GameController>();
    if (g == null) return "kein GameController";
    typeof(Achterbahn.GameController).GetMethod("StartRide", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(g, null);
    return "gestartet";
  }
}
