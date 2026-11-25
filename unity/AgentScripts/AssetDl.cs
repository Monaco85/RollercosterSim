using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AssetDl
{
    static Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).FirstOrDefault(t => t != null);

    public static string Inspect()
    {
        var sc = T("UnityEditor.PackageManager.UI.Internal.ServicesContainer");
        var dm = T("UnityEditor.PackageManager.UI.Internal.AssetStoreDownloadManager");
        string s = "sc=" + sc + " dm=" + dm + "\n";
        if (dm != null)
            foreach (var m in dm.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                s += m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")\n";
        if (sc != null)
            foreach (var m in sc.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                s += "SC." + m.Name + "\n";
        return s;
    }

    static object Svc(Type t)
    {
        var sc = T("UnityEditor.PackageManager.UI.Internal.ServicesContainer");
        var inst = sc.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(null);
        var res = sc.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First(m => m.Name == "Resolve" && m.IsGenericMethod).MakeGenericMethod(t);
        return res.Invoke(inst, null);
    }

    public static string Download()
    {
        var dmT = T("UnityEditor.PackageManager.UI.Internal.AssetStoreDownloadManager");
        var dm = Svc(dmT);
        var m = dmT.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First(x => x.Name == "Download");
        var ps = m.GetParameters();
        object arg = ps[0].ParameterType == typeof(long) ? (object)29140L : new long[] { 29140L };
        var all = new object[ps.Length]; all[0] = arg;
        for (int i = 1; i < ps.Length; i++) all[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
        var r = m.Invoke(dm, all);
        return "Download aufgerufen: " + m + " -> " + r;
    }

    public static string State()
    {
        var dmT = T("UnityEditor.PackageManager.UI.Internal.AssetStoreDownloadManager");
        var dm = Svc(dmT);
        var g = dmT.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == "GetDownloadOperation");
        if (g == null) return "kein GetDownloadOperation";
        var op = g.Invoke(dm, new object[] { 29140L });
        if (op == null) return "keine Operation";
        string s = "";
        foreach (var p in op.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            try { s += p.Name + "=" + p.GetValue(op) + "\n"; } catch { }
        return s;
    }
}
