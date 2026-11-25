using System; using System.Text; using System.Reflection; using Achterbahn;
public static class AgentLoopEnd { public static string Main() {
  var sb = new StringBuilder();
  var m = typeof(TrackLibrary).GetMethod("Loop", BindingFlags.NonPublic | BindingFlags.Static);
  foreach (double s in new[] { 20.0, 21.0, 22.0, 23.2 }) {
    var l = (System.Collections.Generic.List<V3>)m.Invoke(null, new object[] { 0.0, 0.0, 7.0, 3.0, s, 3.2, .5 });
    double ymax = 0; foreach (var p in l) ymax = Math.Max(ymax, p.y);
    var e = l[l.Count - 1]; sb.AppendFormat("s={0} n={1} end=({2:0.0},{3:0.0},{4:0.0}) top={5:0.0}\n", s, l.Count, e.x, e.y, e.z, ymax);
  }
  return sb.ToString().Replace(',', '.'); } }
