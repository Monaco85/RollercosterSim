using UnityEngine; using System.Linq;
public static class AgentMusic { public static string Main() { var m = Object.FindObjectsByType<Achterbahn.ThemeMusic>(FindObjectsSortMode.None).FirstOrDefault(); if (m == null) return "keine Musik";
  var s = m.GetComponent<AudioSource>(); return m.name + " clip=" + s.clip.name + " spielt=" + s.isPlaying + " vol=" + s.volume.ToString("F2") + " t=" + s.time.ToString("F1"); } }
