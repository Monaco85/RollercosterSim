using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Startet die Bruecke zu SimHub (Go-Programm, unveraendert) unsichtbar mit, damit nur die Unity-EXE gestartet werden muss.
    /// - Nur in der gebauten EXE, nie im Editor und nie mit -ohnesitz (Sitz-Sicherheit).
    /// - Laeuft schon eine Bruecke (Port 8765 belegt, z. B. per "Bruecke fuer Unity starten.bat"), wird nichts gestartet.
    /// - Erwartet: &lt;Spielordner&gt;\Bruecke\Achterbahn-Bruecke.exe. Ausgaben der Bruecke landen in Bruecke\bruecke.log.
    /// - Beim Beenden: die Bruecke bekommt keine Daten mehr und blendet die Bewegung selbst auf null (wie bisher).
    ///   Erst nach 4 s wird sie beendet (von einem kleinen unsichtbaren Helfer, damit das Spiel sofort zugeht).
    /// </summary>
    public static class BridgeStarter
    {
        public const string Ordner = "Bruecke", Exe = "Achterbahn-Bruecke.exe";
        static Process proc;
        static StreamWriter log;

        public static string Status { get; private set; } = "";
        public static bool Gestartet => proc != null && !proc.HasExited;

        static bool PortBelegt()
        {
            try
            {
                using (var c = new TcpClient())
                {
                    var ar = c.BeginConnect("127.0.0.1", 8765, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(250) && c.Connected;
                    if (ok) c.EndConnect(ar);
                    return ok;
                }
            }
            catch { return false; }
        }

        public static void Starten()
        {
            if (Application.isEditor) return;
            try
            {
                if (PortBelegt()) { Status = "Brücke lief schon"; return; }
                string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Ordner), exe = Path.Combine(dir, Exe);
                if (!File.Exists(exe)) { Status = "Brücke nicht gefunden"; UnityEngine.Debug.Log("Achterbahn: " + exe + " fehlt, Bruecke bitte selbst starten."); return; }
                var psi = new ProcessStartInfo(exe, "-ohnefenster")
                {
                    WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                };
                try { log = new StreamWriter(Path.Combine(dir, "bruecke.log"), false) { AutoFlush = true }; log.WriteLine("Start " + DateTime.Now); } catch { log = null; }
                proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) Schreib(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) Schreib(e.Data); };
                proc.Start(); proc.BeginOutputReadLine(); proc.BeginErrorReadLine();
                Status = "Brücke gestartet";
                UnityEngine.Debug.Log("Achterbahn: Bruecke gestartet (PID " + proc.Id + ")");
            }
            catch (Exception ex) { Status = "Brücke-Start fehlgeschlagen"; UnityEngine.Debug.Log("Achterbahn: Bruecke-Start fehlgeschlagen: " + ex.Message); }
        }

        static void Schreib(string z) { try { lock (typeof(BridgeStarter)) log?.WriteLine(z); } catch { } }

        /// <summary>Beim Beenden des Spiels: Bruecke nach 4 s beenden (vorher blendet sie die Bewegung selbst aus).</summary>
        public static void Beenden()
        {
            if (proc == null) return;
            try
            {
                if (!proc.HasExited)
                {
                    string sys = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
                    var psi = new ProcessStartInfo(Path.Combine(sys, @"System32\cmd.exe"), $"/c ping -n 5 127.0.0.1 >nul & taskkill /PID {proc.Id} /F >nul")
                    { UseShellExecute = false, CreateNoWindow = true };
                    Process.Start(psi);
                }
            }
            catch { }
            try { log?.Flush(); } catch { }
        }
    }
}
