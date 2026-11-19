public static class AgentResolve { public static string Main() { UnityEditor.PackageManager.Client.Resolve(); UnityEditor.AssetDatabase.Refresh(); return "ok"; } }
