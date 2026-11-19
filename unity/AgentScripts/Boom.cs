using UnityEngine;
public static class AgentBoom { public static string Main() { var f = Object.FindFirstObjectByType<Achterbahn.Fireworks>(); if (f == null) return "kein";
  f.Burst(new Vector3(40, 70, 115)); f.Burst(new Vector3(80, 62, 112)); f.Burst(new Vector3(10, 66, 118)); return "bumm"; } }
