using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Achterbahn;
// LAN-Test im Editor: der Editor ist Gastgeber (Bahn 9), dieses Skript spielt den Gast ueber 127.0.0.1.
// Sendet Gas voll + Startwunsch, prueft Phase und ob Wagen 1 faehrt. Keine Verbindung zur Bruecke/zum Sitz.
public static class NetTest {
  static object gc; static Type T;
  static object F(string n) => T.GetField(n, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gc);
  public static async Task<string> Run() {
    gc = UnityEngine.Object.FindFirstObjectByType<GameController>(); T = gc.GetType();
    var sb = new StringBuilder();
    T.GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gc, new object[] { 8 });
    var net = (BumperNet)F("net"); net.Gastgeben(); sb.AppendLine("Gastgeber: " + net.status);
    var u = new UdpClient(0); var host = new IPEndPoint(IPAddress.Loopback, BumperNet.Port);
    void Send(Action<BinaryWriter> f) { var ms = new MemoryStream(); var w = new BinaryWriter(ms); w.Write(Encoding.ASCII.GetBytes("ABX1")); f(w); w.Flush(); var b = ms.ToArray(); u.Send(b, b.Length, host); }
    int anfr = 0; byte phase = 255; float x1 = 0, z1 = 0, x1s = 0, z1s = 0; int snaps = 0; bool startGesendet = false;
    for (int k = 0; k < 400; k++) {
      if (k % 30 == 0) Send(w => { w.Write((byte)1); w.Write("Testgast"); });
      if (k == 60 && !startGesendet) { anfr++; startGesendet = true; }
      Send(w => { w.Write((byte)3); w.Write(1f); w.Write(0.3f); w.Write(anfr); });
      while (u.Available > 0) {
        IPEndPoint from = null; var d = u.Receive(ref from);
        var r = new BinaryReader(new MemoryStream(d, 4, d.Length - 4));
        if (r.ReadByte() != 4) continue;
        byte ph = r.ReadByte(); double t = r.ReadDouble(); r.ReadBoolean(); r.ReadBoolean(); r.ReadInt32(); int n = r.ReadByte();
        for (int i = 0; i < n; i++) { float x = r.ReadSingle(), z = r.ReadSingle(); for (int q = 0; q < 8; q++) r.ReadSingle(); if (i == 1) { x1 = x; z1 = z; } }
        if (ph != phase) { sb.AppendLine("Phase " + ph + " bei t=" + t.ToString("F1") + " Wagen1 " + x1.ToString("F2") + "," + z1.ToString("F2")); phase = ph; if (ph == 2) { x1s = x1; z1s = z1; } }
        snaps++;
      }
      await Task.Delay(16);
    }
    sb.AppendLine("Pakete " + snaps + ", Status Gastgeber: " + net.status + ", Wagen1 in Fahrt bewegt: " + new Vector2(x1 - x1s, z1 - z1s).magnitude.ToString("F2") + " m");
    var br = (BumperRide)T.GetField("cur", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gc).GetType().GetField("bumper").GetValue(F("cur"));
    sb.AppendLine("Host: Wagen1 ai=" + br.cars[1].ai + " gas=" + br.cars[1].gas.ToString("F2") + " state=" + F("state"));
    Send(w => w.Write((byte)5)); u.Close(); net.Stop();
    T.GetMethod("ToMenu", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gc, null);
    return sb.ToString();
  }

  // Editor als Gast, dieses Skript als Gastgeber: schickt Phase 1 -> 2 und einen fahrenden Wagen 0, liest die Eingaben des Gasts
  public static async Task<string> Gast() {
    gc = UnityEngine.Object.FindFirstObjectByType<GameController>(); T = gc.GetType();
    var sb = new StringBuilder();
    T.GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gc, new object[] { 8 });
    var net = (BumperNet)F("net");
    var u = new UdpClient(new IPEndPoint(IPAddress.Loopback, BumperNet.Port));
    net.Suchen(); net.Beitreten(new IPEndPoint(IPAddress.Loopback, BumperNet.Port), "Testhost");
    IPEndPoint gast = null; int eing = 0; float gasGesehen = 0;
    double t = 0; float x0 = 3f;
    for (int k = 0; k < 300; k++) {
      while (u.Available > 0) { IPEndPoint from = null; var d = u.Receive(ref from); gast = from; var r = new BinaryReader(new MemoryStream(d, 4, d.Length - 4)); if (r.ReadByte() == 3) { eing++; gasGesehen = r.ReadSingle(); } }
      if (gast != null) {
        byte ph = (byte)(k < 60 ? 0 : k < 120 ? 1 : 2); t += .016;
        var ms = new MemoryStream(); var w = new BinaryWriter(ms); w.Write(Encoding.ASCII.GetBytes("ABX1"));
        w.Write((byte)4); w.Write(ph); w.Write(t); w.Write(false); w.Write(false); w.Write(0); w.Write((byte)3);
        for (int i = 0; i < 3; i++) { float x = i == 0 ? x0 + k * .02f : i == 1 ? 3f : -2f; float[] v = { x, i == 2 ? 25f : 18f, 0, i == 0 ? 1.2f : 0, 0, 0, 0, 0, 0, 0 }; foreach (var f in v) w.Write(f); }
        w.Flush(); var b = ms.ToArray(); u.Send(b, b.Length, gast);
      }
      if (k == 100 || k == 299) sb.AppendLine("k=" + k + " Gast-Zustand " + F("state") + " Status: " + net.status);
      await Task.Delay(16);
    }
    var br = (BumperRide)F("cur").GetType().GetField("bumper").GetValue(F("cur"));
    sb.AppendLine("Eingaben vom Gast: " + eing + ", me=" + br.me + " replica=" + br.replica + " Wagen0 x=" + br.cars[0].x.ToString("F2") + " (soll ~" + (x0 + 299 * .02f).ToString("F2") + ")");
    sb.AppendLine("Sichtbarer eigener Wagen steht bei " + FairWorld.JoustCars[0].position.ToString("F1") + ", Wagen1 der Rechnung " + br.cars[1].x.ToString("F1") + "," + br.cars[1].z.ToString("F1"));
    u.Close(); net.Stop();
    T.GetMethod("ToMenu", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gc, null);
    return sb.ToString();
  }

  public static string Lenkung() {
    var s = new StringBuilder(); int alt = BumperInput.Lenkart;
    for (int m = 0; m < 3; m++) { BumperInput.Lenkart = m; s.Append(BumperInput.LenkartNamen[m] + ": "); foreach (var w in new[] { 45, 90, 137, 180, 270, 360, 450 }) s.Append(w + "°→" + BumperInput.RadWinkel(w).ToString("F0") + "° "); s.AppendLine(); }
    BumperInput.Lenkart = alt; return s.ToString();
  }
}
