using UnityEngine;
public static class AgentWhere { public static string Main() { var s = GameObject.Find("Stegosaurus"); return s == null ? "keiner" : s.transform.position.ToString("F1") + " yaw " + s.transform.eulerAngles.y.ToString("F0"); } }
