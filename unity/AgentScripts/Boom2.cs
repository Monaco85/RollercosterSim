using UnityEngine; using System.Linq;
public static class AgentBoom2 { public static string Main() { var f = Object.FindFirstObjectByType<Achterbahn.Fireworks>(); if (f == null) return "kein";
  var s = new System.Text.StringBuilder();
  foreach (var ps in f.GetComponentsInChildren<ParticleSystem>().Take(16)) { var r = ps.GetComponent<ParticleSystemRenderer>();
    s.AppendLine(ps.name + " n=" + ps.particleCount + " playing=" + ps.isPlaying + " pos=" + ps.transform.position.ToString("F0") + " mat=" + (r.sharedMaterial == null ? "null" : r.sharedMaterial.shader.name) + " vis=" + r.isVisible + " bounds=" + r.bounds.size.ToString("F0")); }
  return s.ToString(); } }
