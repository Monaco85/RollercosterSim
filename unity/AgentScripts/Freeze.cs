using UnityEngine;
public static class AgentFreeze { public static string Set(float s) { Time.timeScale = s; return "zeit " + s; } }
