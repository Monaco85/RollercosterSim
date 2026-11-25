using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// LAN-Mehrspieler fuers Boxauto (zwei PCs im selben Netz). Der Gastgeber rechnet die Fahrt (BumperRide) und schickt 60-mal pro
    /// Sekunde den Zustand aller Wagen; der Gast schickt seine Lenk-/Pedal-Eingaben und rechnet zwischen zwei Paketen selbst weiter
    /// (gleiches Modell), damit sein Sitz und sein Bild fluessig bleiben. Gefunden wird der Gastgeber per Rundruf im Netz.
    /// Nur UDP, keine Fremdbibliothek. Hat nichts mit der Bruecke zum Sitz zu tun.
    /// </summary>
    public class BumperNet : IDisposable
    {
        public enum Rolle { Aus, Gastgeber, Gast }
        public const int Port = 47811, SuchPort = 47812;
        const string Kennung = "ABX1";

        public Rolle rolle { get; private set; }
        public string status = "";
        public string partnerName = "";
        public readonly List<(string name, IPEndPoint ep, float seen)> gefunden = new List<(string, IPEndPoint, float)>();

        // Gastgeber: Eingaben des Gasts
        public double gastGas, gastDelta;
        public int gastAnfragen;              // Zaehler, steigt bei "Start/Pause"-Wunsch des Gasts
        int gastAnfragenGesehen;
        // Gast: letzter Zustand vom Gastgeber
        public Snapshot letzter; public bool neuerZustand;
        int meineAnfragen;

        UdpClient sock, such;
        IPEndPoint partner;
        float lastRx = -99, lastBeacon, lastHello;
        readonly byte[] buf = new byte[2048];

        public struct CarState { public double x, z, h, vx, vz, w, bz, bv, gas, delta; }
        public class Snapshot
        {
            public byte phase; public double time; public bool powerOff, done; public int aus;   // aus: Bitmaske der ausgeschalteten Wagen
            public CarState[] cars = new CarState[0];
        }

        public bool Verbunden => partner != null && Time.realtimeSinceStartup - lastRx < 3f;
        public bool IstGast => rolle == Rolle.Gast;
        public bool IstGastgeber => rolle == Rolle.Gastgeber;

        static string MeinName() { try { return Environment.MachineName; } catch { return "PC"; } }

        static UdpClient Offen(int port)
        {
            var u = new UdpClient(AddressFamily.InterNetwork);
            u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            u.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            u.EnableBroadcast = true;
            // Windows: ein ICMP "Port nicht erreichbar" soll den UDP-Empfang nicht abbrechen
            try { const int SIO_UDP_CONNRESET = -1744830452; u.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
            return u;
        }

        public void Gastgeben()
        {
            Stop();
            try { sock = Offen(Port); rolle = Rolle.Gastgeber; status = "Wartet auf Mitspieler … (" + EigeneIp() + ")"; }
            catch (Exception e) { status = "Öffnen ging nicht: " + e.Message; Stop(); }
        }

        public void Suchen()
        {
            Stop();
            try { such = Offen(SuchPort); sock = Offen(0); rolle = Rolle.Gast; status = "Suche Gastgeber im Netz …"; }
            catch (Exception e) { status = "Suchen ging nicht: " + e.Message; Stop(); }
        }

        public void Beitreten(IPEndPoint ep, string name)
        {
            partner = ep; partnerName = name; lastRx = Time.realtimeSinceStartup; lastHello = -99;
            status = "Verbinde mit " + name + " …";
        }

        public void Stop()
        {
            if (partner != null && sock != null) try { Send(w => w.Write((byte)5)); } catch { }
            sock?.Close(); such?.Close(); sock = such = null; partner = null; rolle = Rolle.Aus; status = ""; partnerName = ""; gefunden.Clear(); letzter = null;
        }
        public void Dispose() => Stop();

        public static string EigeneIp()
        {
            try
            {
                foreach (var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) return a.ToString();
            }
            catch { }
            return "?";
        }

        void Send(Action<BinaryWriter> fill)
        {
            if (sock == null || partner == null) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Encoding.ASCII.GetBytes(Kennung)); fill(w); w.Flush();
                var b = ms.ToArray(); sock.Send(b, b.Length, partner);
            }
        }

        /// <summary>Einmal pro Bild aufrufen: Pakete lesen, Rundruf/Hallo schicken.</summary>
        public void Update()
        {
            if (rolle == Rolle.Aus) return;
            float now = Time.realtimeSinceStartup;
            try
            {
                if (rolle == Rolle.Gastgeber && now - lastBeacon > 1f)
                {
                    lastBeacon = now;
                    var b = Encoding.UTF8.GetBytes(Kennung + "|" + MeinName());
                    sock.Send(b, b.Length, new IPEndPoint(IPAddress.Broadcast, SuchPort));
                }
                if (rolle == Rolle.Gast && partner != null && (!Verbunden || now - lastHello > 1f) && now - lastHello > .5f)
                {
                    lastHello = now; Send(w => { w.Write((byte)1); w.Write(MeinName()); });
                }
                while (such != null && such.Available > 0)
                {
                    IPEndPoint from = null; var d = such.Receive(ref from);
                    var s = Encoding.UTF8.GetString(d);
                    if (!s.StartsWith(Kennung + "|")) continue;
                    var ep = new IPEndPoint(from.Address, Port); string name = s.Substring(5);
                    int k = gefunden.FindIndex(g => g.ep.Equals(ep));
                    if (k >= 0) gefunden[k] = (name, ep, now); else gefunden.Add((name, ep, now));
                }
                gefunden.RemoveAll(g => now - g.seen > 4f);
                while (sock != null && sock.Available > 0)
                {
                    IPEndPoint from = null; var d = sock.Receive(ref from);
                    if (d.Length < 5 || Encoding.ASCII.GetString(d, 0, 4) != Kennung) continue;
                    using (var r = new BinaryReader(new MemoryStream(d, 4, d.Length - 4))) Lesen(r, from, now);
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            if (partner != null && !Verbunden && Time.realtimeSinceStartup - lastRx > 3f && rolle == Rolle.Gastgeber)
            {
                status = "Mitspieler weg – wartet auf Mitspieler … (" + EigeneIp() + ")"; partner = null; partnerName = "";
            }
            if (rolle == Rolle.Gast && partner != null && !Verbunden) status = "Verbindung zu " + partnerName + " unterbrochen …";
        }

        void Lesen(BinaryReader r, IPEndPoint from, float now)
        {
            byte typ = r.ReadByte();
            if (rolle == Rolle.Gastgeber)
            {
                if (typ == 1)
                {
                    string name = r.ReadString();
                    if (partner == null || !Verbunden || partner.Equals(from)) { partner = from; partnerName = name; lastRx = now; status = "Mitspieler: " + name; }
                }
                else if (partner != null && partner.Equals(from))
                {
                    lastRx = now;
                    if (typ == 3) { gastGas = r.ReadSingle(); gastDelta = r.ReadSingle(); gastAnfragen = r.ReadInt32(); }
                    else if (typ == 5) { partner = null; partnerName = ""; status = "Mitspieler hat beendet – wartet … (" + EigeneIp() + ")"; }
                }
            }
            else if (rolle == Rolle.Gast && partner != null && from.Address.Equals(partner.Address))
            {
                lastRx = now;
                if (typ == 4)
                {
                    var s = new Snapshot { phase = r.ReadByte(), time = r.ReadDouble(), powerOff = r.ReadBoolean(), done = r.ReadBoolean(), aus = r.ReadInt32() };
                    int n = r.ReadByte(); s.cars = new CarState[n];
                    for (int i = 0; i < n; i++)
                        s.cars[i] = new CarState { x = r.ReadSingle(), z = r.ReadSingle(), h = r.ReadSingle(), vx = r.ReadSingle(), vz = r.ReadSingle(), w = r.ReadSingle(), bz = r.ReadSingle(), bv = r.ReadSingle(), gas = r.ReadSingle(), delta = r.ReadSingle() };
                    letzter = s; neuerZustand = true; status = "Verbunden mit " + partnerName;
                }
                else if (typ == 5) { status = partnerName + " hat das Spiel geschlossen"; partner = null; }
            }
        }

        // ---------------- Gastgeber ----------------
        /// <summary>Phase: 0 Bahnwahl, 1 Countdown, 2 Fahrt, 3 Pause, 4 Ende.</summary>
        public void SendeZustand(byte phase, BumperRide br)
        {
            if (rolle != Rolle.Gastgeber || partner == null) return;
            Send(w =>
            {
                int maske = 0; for (int i = 0; i < br.cars.Length; i++) if (br.cars[i].aus) maske |= 1 << i;
                w.Write((byte)4); w.Write(phase); w.Write(br.time); w.Write(br.powerOff); w.Write(br.done); w.Write(maske);
                w.Write((byte)br.cars.Length);
                foreach (var c in br.cars)
                {
                    w.Write((float)c.x); w.Write((float)c.z); w.Write((float)c.h); w.Write((float)c.vx); w.Write((float)c.vz); w.Write((float)c.w);
                    w.Write((float)c.bz); w.Write((float)c.bv); w.Write((float)c.gas); w.Write((float)c.delta);
                }
            });
        }
        /// <summary>true, wenn der Gast seit dem letzten Aufruf Start/Pause gedrueckt hat.</summary>
        public bool GastWillWas() { bool b = gastAnfragen != gastAnfragenGesehen; gastAnfragenGesehen = gastAnfragen; return b && Verbunden; }

        // ---------------- Gast ----------------
        public void SendeEingabe(double gas, double delta) => Send(w => { w.Write((byte)3); w.Write((float)gas); w.Write((float)delta); w.Write(meineAnfragen); });
        public void Anfrage() => meineAnfragen++;

        /// <summary>Zustand vom Gastgeber in die eigene Rechnung uebernehmen. Den eigenen Wagen nur sanft nachziehen.</summary>
        public static void Uebernehmen(Snapshot s, BumperRide br)
        {
            br.time = s.time; br.powerOff = s.powerOff; br.done = s.done;
            for (int i = 0; i < br.cars.Length && i < s.cars.Length; i++)
            {
                var c = br.cars[i]; var n = s.cars[i];
                c.aus = (s.aus & (1 << i)) != 0;
                double k = i == br.me && (Math.Abs(c.x - n.x) + Math.Abs(c.z - n.z)) < .5 ? .35 : 1;
                c.x += (n.x - c.x) * k; c.z += (n.z - c.z) * k;
                double dh = n.h - c.h; while (dh > Math.PI) dh -= 2 * Math.PI; while (dh < -Math.PI) dh += 2 * Math.PI; c.h += dh * k;
                c.vx += (n.vx - c.vx) * k; c.vz += (n.vz - c.vz) * k; c.w += (n.w - c.w) * k;
                if (i != br.me) { c.bz = n.bz; c.bv = n.bv; c.gas = n.gas; c.delta = n.delta; }
            }
        }
    }
}
