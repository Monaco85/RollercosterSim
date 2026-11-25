using System;
using System.Linq;
using System.Reflection;

public static class Login
{
    public static string Check()
    {
        var t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Connect.UnityConnect")).FirstOrDefault(x => x != null);
        var inst = t.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
        string s = "";
        foreach (var n in new[] { "loggedIn", "online", "userInfo" })
        {
            var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) { var v = p.GetValue(inst); s += n + "=" + v + "\n"; }
        }
        return s;
    }
}
