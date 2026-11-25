using System;
using System.Collections.Generic;
using UnityEngine;

namespace Achterbahn
{
    public class Seg
    {
        public enum Kind { Drive, Brake, Stop }
        public Kind type;
        public double from, to, speed, accel;
        public bool lift, launch, felt, hold;
    }

    public class TrackDef
    {
        public string id, name, tag, blurb;
        public Color rail, accent, support, ground, sky, fog;
        public V3[] points;
        public double bankFactor, bankMax;
        public int trainCars;
        public bool rough;
        public int smooth = 900;
        public Func<Track, Seg[]> plan;
        public string theme = "stahl";       // "stahl" oder "mittelalter" (nur Optik)
        public V2[] castleSpots;              // Mittelpunkte fuer Burg-Bauten (nur Optik)
        public V2[] flatSpots;                // weitere ebene Flaechen im Gelaende (nur Optik)
        public float[] volcano;               // Vulkan: x, z (Bahn-Koordinaten), Radius, Hoehe (nur Optik)
        public float[] lake;                  // See: x, z (Bahn-Koordinaten), Halbachse x, Halbachse z, Tiefe (nur Optik)
        public SwingDef swing;                // gesetzt = Schiffsschaukel (eigenes Pendelmodell), die Bahn dient nur als Kulisse
        public BumperDef bumper;              // gesetzt = Boxauto (eigenes Fahrmodell), die Bahn dient nur als Kulisse
    }

    /// <summary>Boxauto: Fahrflaeche (Unity-Meter, Innenkante der Bande) und Fahrzeit in Sekunden.</summary>
    public class BumperDef
    {
        public double cx, cz, hx, hz, duration;
        public double[] obstacles;   // runde Hindernisse: je x, z, Radius (Unity-Meter)
    }

    /// <summary>Schiffsschaukel: Drehpunkt (Bahn-Koordinaten), Arm, Ausschlag, Ablauf in Sekunden, Sitzplatz im Boot.</summary>
    public class SwingDef
    {
        public V3 pivot; public double arm, ampDeg, tUp, tHold, tDown, seatX, seatR;
    }

    public struct V2 { public double x, z; public V2(double x, double z) { this.x = x; this.z = z; } }

    /// <summary>Die vier Bahnen, Punkt fuer Punkt aus der gebauten HTML-Fassung (spiel.html).</summary>
    public static class TrackLibrary
    {
        static Color C(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        static V3 P(double x, double y, double z) => new V3(x, y, z);
        static Seg Drive(double a, double b, double speed, double acc, bool lift = false, bool launch = false, bool felt = false) =>
            new Seg { type = Seg.Kind.Drive, from = a, to = b, speed = speed, accel = acc, lift = lift, launch = launch, felt = felt };
        static Seg Brake(double a, double b, double speed, double acc) => new Seg { type = Seg.Kind.Brake, from = a, to = b, speed = speed, accel = acc };
        static Seg Stop(double a, double b, double speed, double acc) => new Seg { type = Seg.Kind.Stop, from = a, to = b, speed = speed, accel = acc };

        // ---- Hilfsfunktionen der HTML-Fassung (Ph, Dh, E0, w0) ----
        static List<V3> Helix(double i, double t, double e, double n, double s, double r, double a, double o)
        {
            var c = new List<V3>(); int l = (int)Math.Floor(r * 10 + 0.5);
            for (int h = 0; h <= l; h++)
            {
                double u = a + o * ((double)h / l) * r * Math.PI * 2;
                c.Add(P(i + Math.Cos(u) * e, n + (s - n) * ((double)h / l), t + Math.Sin(u) * e));
            }
            return c;
        }

        static List<V3> Loop(double i, double t, double e, double n, double s, double r = 3.2, double a = .5)
        {
            var l = new List<double[]>(); double h = 0, u = 0, d = 0, f = 0;
            while (d < Math.PI && f < 300)
            {
                double Pq = s * s - 19.62 * u;
                double A = 1 + (a + (r - a) * (1 + Math.Cos(d)) / 2 - 1) * Math.Min(1, f / 16);
                d += .1 * 9.81 * (A - Math.Cos(d)) / Pq;
                h += Math.Cos(d) * .1; u += Math.Sin(d) * .1; f += .1;
                l.Add(new[] { h, u });
            }
            double g = h, x = u; int p = 40;
            var w = new List<double[]>();
            for (int k = 0; k < l.Count; k++) if (k % p == 0) w.Add(l[k]);
            var v = new List<double[]>(w);
            v.Add(new[] { g, x });
            for (int k = w.Count - 1; k >= 0; k--) v.Add(new[] { 2 * g - w[k][0], w[k][1] });
            var m = new List<V3>();
            for (int A = 0; A < v.Count; A++)
            {
                double R = (double)A / (v.Count - 1), b = R * R * (3 - 2 * R);
                m.Add(P(i + v[A][0], n + v[A][1], t + e * b));
            }
            return m;
        }

        static List<V3> Wave(double i, double t, double e, double n, double s, double r, double a, double o, double c)
        {
            var l = new List<V3>();
            for (int h = 0; h < n * 2; h++)
            {
                double u = h * s / 2;
                double d = Math.Min(1, h / 3.0) * Math.Min(1, (n * 2 - 1 - h) / 3.0);
                l.Add(P(i + e * u, c + 4 + 1.5 * Math.Sin(u * 2 * Math.PI / (o * .9)) + 1.2, t + d * r * Math.Sin(u * 2 * Math.PI / o)));
            }
            return l;
        }

        static List<V3> Hill(double i, double t, double e, double n, double s, double r, double a)
        {
            var o = new List<V3>(); double c = r / 6;
            for (int l = 0; l <= a * 6; l++)
                o.Add(P(i + e * l * c,
                        n + s / 2 * (1 - Math.Cos(l / 6.0 * 2 * Math.PI)) * Math.Min(1, l / 3.0) * Math.Min(1, (a * 6 - l) / 3.0),
                        t - l * .1));
            return o;
        }

        static int IndexOfPoint(Track tr, double x, double z)
        {
            for (int i = 0; i < tr.def.points.Length; i++) if (tr.def.points[i].x == x && tr.def.points[i].z == z) return i;
            return 0;
        }

        static V3[] Pts(params double[][] a)
        {
            var l = new V3[a.Length];
            for (int i = 0; i < a.Length; i++) l[i] = P(a[i][0], a[i][1], a[i][2]);
            return l;
        }
        static double[] D(double x, double y, double z) => new[] { x, y, z };

        public static TrackDef[] All()
        {
            return new[] { Raupe(), Bergblitz(), Drache(), Monster(), Ritterburg(), WildeMaus(), Wikinger(), Schiffsschaukel(), Boxauto() };
        }

        // Neue Bahn: Wikinger-Achterbahn. Abschuss, drei Loopings, schnelle Links-Rechts-Wechsel am Boden.
        // Booster vor Looping 2 und 3 (wie der Abschuss im Schuettelmonster). Physik und Bewegungsfilter unveraendert.
        public static double WikLoop1 = 23.2, WikLoop2 = 23.2, WikLoop3 = 23.2;
        static TrackDef Wikinger()
        {
            var pts = new List<V3>(Pts(D(-24, 3, 0), D(0, 3, 0), D(20, 3, 0), D(44, 3, 0), D(66, 3, 0), D(84, 3, 0), D(100, 3, 0)));
            pts.AddRange(Loop(104, 0, 7, 3, WikLoop1));                                                           // Looping 1
            pts.AddRange(Pts(D(166, 3.4, 7), D(182, 3.6, 3), D(217, 3.8, 11), D(244, 3.6, 7)));                     // Links-rechts
            int iB2 = pts.Count - 1;
            pts.AddRange(Pts(D(256, 3.5, 7)));
            int iL2 = pts.Count;
            pts.AddRange(Loop(268, 7, 7, 3.5, WikLoop2));                                                         // Looping 2
            pts.AddRange(Pts(D(326, 3.6, 14), D(342, 3.8, 10), D(377, 3.8, 18), D(404, 3.6, 14)));                  // Links-rechts
            int iB3 = pts.Count - 1;
            pts.AddRange(Pts(D(416, 3.5, 14)));
            int iL3 = pts.Count;
            pts.AddRange(Loop(428, 14, 7, 3.5, WikLoop3));                                                        // Looping 3
            pts.AddRange(Pts(D(486, 3.8, 21), D(500, 4, 21)));
            pts.AddRange(Helix(515, 51, 30, 4, 4.5, .5, -Math.PI / 2, 1));                                         // Wende
            pts.AddRange(Pts(D(495, 4.4, 81), D(465, 4.2, 75), D(430, 4.2, 87), D(395, 4.1, 75), D(360, 4, 87), D(325, 4, 77),   // schnelle Wechsel
                D(280, 4.2, 81), D(220, 5.5, 81), D(160, 4.4, 81), D(100, 4.4, 79), D(50, 4.4, 72), D(18, 4.2, 54), D(-4, 3.8, 33), D(-30, 3.4, 17.5),
                D(-46, 3.3, 16), D(-50, 3.25, 14.93), D(-52.93, 3.2, 12), D(-54, 3.15, 8), D(-52.93, 3.1, 4), D(-50, 3.05, 1.07), D(-46, 3, 0), D(-36, 3, 0)));
            return new TrackDef
            {
                id = "wikinger", name = "Wikinger", tag = "Drei Loopings",
                blurb = "Abschuss am Fjord, drei Loopings und blitzschnelle Links-Rechts-Wechsel.",
                rail = C(0x5A3A22), accent = C(0xB3261E), support = C(0x4A3A2A), ground = C(0x6F8F55), sky = C(0xC9D9E6), fog = C(0xD3DEE6),
                bankFactor = .85, bankMax = 60, trainCars = 4, points = pts.ToArray(), theme = "wikinger",
                lake = new[] { 250f, -48f, 235f, 36f, 9f },   // Fjord suedlich des Hinwegs (nur Optik)
                plan = t => new[]
                {
                    Drive(0, 20, 3, 1),
                    Drive(20, 92, 23.5, 5, launch: true, felt: true),
                    Drive(t.SAtPoint(iB2), t.SAtPoint(iL2), 23, 5, launch: true, felt: true),
                    Drive(t.SAtPoint(iB3), t.SAtPoint(iL3), 23, 5, launch: true, felt: true),
                    Brake(t.length - 84, t.length - 24, 3.6, 3.2),
                    Stop(t.length - 24, t.length, 3.5, .8),
                },
            };
        }

        // Neue Bahn: Wilde Maus auf dem Rummel. Einzelwagen, Lift, Haarnadelkurven oben fast ohne Querneigung, dann Abfahrt mit Huckeln.
        // Wenig Glaettung (smooth), damit die Kurven eng bleiben. Physik und Bewegungsfilter unveraendert.
        static TrackDef WildeMaus()
        {
            var pts = new List<V3>(Pts(D(-14, 3, 0), D(0, 3, 0), D(12, 3, 0), D(20, 3.4, 0), D(30, 7, 0), D(40, 11, 0), D(48, 14.3, 0), D(54, 15.4, 0)));
            int liftTop = pts.Count - 1;
            pts.Add(P(58, 15.4, 0));
            pts.AddRange(Helix(60, 4.5, 4.5, 15.4, 15.2, .5, -Math.PI / 2, 1));            // Haarnadel 1
            int str1 = pts.Count;
            pts.AddRange(Pts(D(52, 15.05, 9), D(36, 14.6, 9), D(22, 14.25, 9)));
            int hp2 = pts.Count;
            pts.AddRange(Helix(14, 13.5, 4.5, 14.1, 13.9, .5, -Math.PI / 2, -1));         // Haarnadel 2
            pts.AddRange(Pts(D(22, 13.75, 18), D(36, 13.35, 18), D(52, 13.0, 18)));
            int hp3 = pts.Count;
            pts.AddRange(Helix(60, 22.5, 4.5, 12.85, 12.65, .5, -Math.PI / 2, 1));          // Haarnadel 3
            pts.AddRange(Pts(D(52, 12.4, 27), D(46, 11.4, 27), D(40, 8.4, 27), D(34, 5.0, 27), D(27, 3.5, 27), D(19, 5.2, 27), D(11, 3.6, 27),
                D(2, 3.8, 27), D(-8, 4.6, 26), D(-19, 4.1, 21.5), D(-26, 3.6, 13.5), D(-26, 3.25, 6), D(-21, 3.05, 1.2)));
            int iHp2 = hp2, iHp3 = hp3, iLift = liftTop;
            return new TrackDef
            {
                id = "wildemaus", name = "Wilde Maus", tag = "Rummel",
                blurb = "Einzelwagen auf dem Jahrmarkt: enge Haarnadelkurven hoch oben, dann Huckel-Abfahrt.",
                rail = C(0xD62828), accent = C(0xF4C430), support = C(0xF4C430), ground = C(0x8DA35E), sky = C(0xCFE3F5), fog = C(0xD9E6EE),
                bankFactor = .25, bankMax = 12, trainCars = 1, smooth = 260, points = pts.ToArray(), theme = "rummel",
                // Rummelplatz rund um die Bahn eben halten (Bahn-Koordinaten)
                flatSpots = new[] { new V2(-60, -30), new V2(-20, -40), new V2(20, -40), new V2(60, -40), new V2(100, -20), new V2(100, 20), new V2(100, 55),
                                    new V2(60, 60), new V2(20, 60), new V2(-20, 60), new V2(-60, 40), new V2(-70, 0), new V2(30, 10) },
                plan = t => new[]
                {
                    Drive(0, 20, 3, 1),
                    Drive(20, t.SAtPoint(iLift) + 2, 3, 1, lift: true),
                    Brake(t.SAtPoint(iHp2) - 14, t.SAtPoint(iHp2) - 2, 4.2, 3),
                    Brake(t.SAtPoint(iHp3) - 14, t.SAtPoint(iHp3) - 2, 4.2, 3),
                    Brake(t.length - 60, t.length - 22, 3.2, 2.4),
                    Stop(t.length - 22, t.length, 3, .8),
                },
            };
        }

        // Schiffsschaukel auf dem Rummel neben der Wilden Maus. Die Maus-Bahn ist nur Kulisse (Wagen bleiben in der Station),
        // gefahren wird mit dem Pendelmodell PendelRide. Drehpunkt in Unity (108, 15, -48) = Bahn-Koordinaten (108, 15, 48).
        // Boxauto: eigenes Fahrmodell (BumperRide). Kulisse = Autoscooter-Halle bei Nacht (Thema "autoscooter", FairWorld.Scooter);
        // die Ritterburg-Strecke dient nur als unsichtbare Rechengrundlage. Fahrflaeche und Hindernisse unveraendert.
        static TrackDef Boxauto()
        {
            var d = Ritterburg();
            d.id = "boxauto"; d.name = "Boxauto"; d.tag = "Autoscooter";
            d.blurb = "Selbst lenken in der Kirmes-Halle, zwei Wagen rempeln mit.";
            d.theme = "autoscooter";
            d.rough = true;   // kraeftige Stufe: schnelle Stoesse wie im Schuettelmonster
            d.bumper = new BumperDef
            {
                cx = 3, cz = 21, hx = 15, hz = 9.5, duration = 150,
                // Brunnen-Insel in der Mitte, vier gepolsterte Eichenpfeiler
                obstacles = new double[] { 3, 21, 2.1,  -5, 16.5, .45,  11, 16.5, .45,  -5, 25.5, .45,  11, 25.5, .45 },
            };
            return d;
        }

        static TrackDef Schiffsschaukel()
        {
            var d = WildeMaus();
            d.id = "schaukel"; d.name = "Schiffsschaukel"; d.tag = "Mittelalter";
            d.blurb = "Eichenschiff an Eisenarmen, schaukelt bis über die Waagerechte.";
            d.accent = C(0x8E2A1E);
            d.swing = new SwingDef { pivot = P(108, 15, 48), arm = 11, ampDeg = 95, tUp = 32, tHold = 24, tDown = 22, seatX = -3.6, seatR = 10.0 };
            return d;
        }

        // Neue Bahn (nicht aus der HTML-Fassung): Holzachterbahn durch eine Burg.
        // Nutzt unveraendert dieselbe Physik und dieselben Bewegungsfilter wie die anderen Bahnen.
        static TrackDef Ritterburg()
        {
            var pts = new List<V3>(Pts(D(-20, 4, 0), D(0, 4, 0), D(14, 4, 0), D(24, 4.6, 0), D(40, 11, 0), D(56, 18, 0), D(68, 23, 0), D(78, 24.5, 0), D(86, 24, .3),
                D(96, 17, 1.5), D(106, 7, 3), D(116, 4.5, 7), D(128, 7, 15), D(132, 10, 27), D(126, 12, 38), D(112, 11, 45),
                D(98, 6.5, 47), D(86, 10.5, 47.5), D(74, 6.5, 48), D(62, 9, 48), D(50, 9, 47)));
            pts.AddRange(Helix(34, 63, 16, 9, 5, 1, -Math.PI / 2, -1));
            pts.AddRange(Pts(D(18, 5, 46.5), D(2, 5.5, 42), D(-14, 6, 34), D(-30, 5.5, 24), D(-42, 5, 14), D(-42, 4.5, 5), D(-34, 4, .8)));
            return new TrackDef
            {
                id = "ritterburg", name = "Ritterburg", tag = "Mittelalter",
                blurb = "Holzbahn durch das Burgtor, über das Dorf und in Spiralen um den Bergfried.",
                rail = C(0x6B4A2E), accent = C(0xB3261E), support = C(0x8A6A48), ground = C(0x7FA35A), sky = C(0xCFE3F5), fog = C(0xD9E6EE),
                bankFactor = .85, bankMax = 55, trainCars = 5, points = pts.ToArray(), theme = "mittelalter",
                castleSpots = new[] { new V2(34, 63) },
                // Vorburg suedlich der Station (in Bahn-Koordinaten z negativ)
                flatSpots = new[] { new V2(-20, -18), new V2(2, -18), new V2(24, -18), new V2(-20, -38), new V2(2, -38), new V2(24, -38), new V2(2, -55) },
                plan = t => new[]
                {
                    Drive(0, 24, 3.5, 1),
                    Drive(24, t.SAtPoint(7) + 2, 3.5, 1, lift: true),
                    Brake(t.length - 84, t.length - 24, 3.6, 3),
                    Stop(t.length - 24, t.length, 3.5, .8),
                },
            };
        }

        static TrackDef Raupe() => new TrackDef
        {
            id = "raupe", name = "Kleine Raupe", tag = "Für den Anfang",
            blurb = "Gemütlich, keine Überschlag-Stelle, ein Tunnel und viele kleine Hügel.",
            rail = C(3523691), accent = C(16762161), support = C(16052452), ground = C(9426014), sky = C(14217727), fog = C(13627135),
            bankFactor = .9, bankMax = 28, trainCars = 4,
            points = Pts(D(-14, 3, 0), D(0, 3, 0), D(12, 3, 0), D(22, 3.4, 0), D(34, 6.2, 0), D(46, 9.2, 0), D(56, 11.2, 0), D(64, 11.4, 1),
                D(72, 10, 6), D(76, 8, 15), D(72, 6, 24), D(62, 4.2, 27), D(50, 6.3, 28), D(40, 3.6, 30), D(28, 5.5, 31), D(16, 3, 33),
                D(2, 3.8, 38), D(-10, 6, 46), D(-24, 3.4, 48), D(-38, 3, 40), D(-44, 3.4, 26), D(-40, 3.2, 12), D(-30, 3, 4), D(-22, 3, .8)),
            plan = t => new[]
            {
                Drive(0, 22, 3, 1),
                Drive(22, t.SAtPoint(6) + 2, 3, 1, lift: true),
                Brake(t.SAtPoint(19), t.length - 22, 3.2, 2.2),
                Stop(t.length - 22, t.length, 3, .8),
            },
        };

        static TrackDef Bergblitz()
        {
            var pts = new List<V3>(Pts(D(-20, 4, 0), D(0, 4, 0), D(14, 4, 0), D(24, 4.6, 0), D(40, 12, 0), D(56, 20, 0), D(70, 27, 0), D(80, 30, 0), D(88, 30, 0),
                D(96, 27.5, 0), D(106, 18, 0), D(116, 8, 0), D(128, 4, 0), D(142, 9, 1), D(156, 19, 3), D(168, 22, 8), D(176, 19, 18), D(176, 13, 30),
                D(166, 9, 40), D(152, 12, 44), D(138, 16.5, 45), D(124, 11, 46), D(110, 5, 47), D(96, 8, 48), D(80, 13, 48.5), D(66, 13.4, 49)));
            pts.AddRange(Helix(50, 69, 20, 13.2, 6.4, 1, -Math.PI / 2, -1));
            pts.AddRange(Pts(D(30, 5.5, 46), D(8, 4.6, 38), D(-14, 5, 26), D(-30, 5, 17.5), D(-46, 5, 16), D(-50, 4.83, 14.93), D(-52.93, 4.67, 12),
                D(-54, 4.5, 8), D(-52.93, 4.33, 4), D(-50, 4.17, 1.07), D(-46, 4, 0), D(-36, 4, 0)));
            return new TrackDef
            {
                id = "bergblitz", name = "Bergblitz", tag = "Mittel",
                blurb = "Hoher Lifthügel, freier Fall, Kamelhöcker und eine Spirale nach unten.",
                rail = C(3111935), accent = C(16738877), support = C(14673650), ground = C(7780714), sky = C(13625855), fog = C(14150911),
                bankFactor = .85, bankMax = 55, trainCars = 5, points = pts.ToArray(),
                plan = t => new[]
                {
                    Drive(0, 24, 3.5, 1),
                    Drive(24, t.SAtPoint(7) + 2, 3.5, 1, lift: true),
                    Brake(t.length - 84, t.length - 24, 3.6, 3),
                    Stop(t.length - 24, t.length, 3.5, .8),
                },
            };
        }

        static TrackDef Drache()
        {
            var pts = new List<V3>(Pts(D(-24, 3, 0), D(0, 3, 0), D(20, 3, 0), D(44, 3, 0), D(66, 3, 0), D(84, 3, 0), D(100, 3, 0)));
            pts.AddRange(Loop(104, 0, 7, 3, 23.2));
            pts.AddRange(Pts(D(160, 3, 7), D(176, 3.6, 7), D(190, 8.5, 7.5), D(202, 14.5, 9), D(211, 16, 17), D(208, 13.5, 30), D(194, 8, 40), D(176, 4.2, 44),
                D(156, 8.5, 45), D(138, 12.5, 46), D(120, 8, 47), D(100, 4.5, 48), D(82, 8, 48), D(66, 11, 48), D(50, 6.5, 47), D(28, 4.2, 44), D(6, 4, 36),
                D(-16, 3.6, 25), D(-30, 3.3, 17.5), D(-46, 3.3, 16), D(-50, 3.25, 14.93), D(-52.93, 3.2, 12), D(-54, 3.15, 8), D(-52.93, 3.1, 4),
                D(-50, 3.05, 1.07), D(-46, 3, 0), D(-36, 3, 0)));
            return new TrackDef
            {
                // Optik: Dino-Looping im Urzeit-Tal. Strecke und Fahrplan unveraendert aus der HTML-Fassung ("Looping-Drache").
                id = "drache", name = "Dino-Looping", tag = "Wild",
                blurb = "Abschuss durchs Urzeit-Tal, ein Looping am T. rex vorbei und enge Steilkurven.",
                rail = C(0x3E5A2A), accent = C(0x6E8A3A), support = C(0x4A3A2A), ground = C(12755802), sky = C(16757611), fog = C(16762255),
                bankFactor = .85, bankMax = 60, trainCars = 4, points = pts.ToArray(), theme = "dino",
                volcano = new[] { 150f, -330f, 200f, 120f },
                lake = new[] { 45f, -85f, 95f, 55f, 9f },
                plan = t => new[]
                {
                    Drive(0, 20, 3, 1),
                    Drive(20, 92, 23.5, 5, launch: true, felt: true),
                    Brake(t.length - 84, t.length - 24, 3.6, 3.2),
                    Stop(t.length - 24, t.length, 3.5, .8),
                },
            };
        }

        static TrackDef Monster()
        {
            var pts = new List<V3>(Pts(D(-22, 4, 0), D(0, 4, 0), D(14, 4, 0), D(26, 4.8, 0), D(44, 14, 0), D(62, 24, 0), D(78, 33, 0), D(90, 37.5, 0),
                D(100, 37.5, .5), D(110, 33, 2), D(120, 21, 5), D(130, 8.5, 9), D(142, 4.5, 16), D(156, 9, 25), D(168, 17, 37), D(172, 22, 50),
                D(162, 21, 62), D(146, 13, 66)));
            pts.AddRange(Wave(130, 66, -1, 5, 18, 1.8, 4.6, 46, 2.6));
            var helix = Helix(24, 92, 22, 8, 13.5, 1, -Math.PI / 2, -1); helix.RemoveAt(0);
            pts.AddRange(helix);
            pts.AddRange(Pts(D(10, 9.5, 70), D(0, 8.8, 69), D(-6, 8, 64.5), D(-9, 7.4, 57), D(-6, 6.8, 49.5), D(0, 6.3, 45), D(8, 6, 44), D(30, 6, 44)));
            pts.AddRange(Hill(52, 44, 1, 6.5, 4.8, 60, 3));
            pts.AddRange(Pts(D(262, 7, 43), D(288, 6.8, 36), D(302, 6.5, 18), D(296, 6.2, -2), D(272, 6, -16), D(238, 6, -22), D(200, 7.2, -24), D(150, 11, -24),
                D(100, 7, -24), D(50, 6, -24), D(0, 5, -22), D(-30, 4.7, -17.5), D(-46, 4.7, -16), D(-50, 4.58, -14.93), D(-52.93, 4.47, -12),
                D(-54, 4.35, -8), D(-52.93, 4.23, -4), D(-50, 4.12, -1.07), D(-46, 4, -0), D(-36, 4, 0)));
            return new TrackDef
            {
                id = "monster", name = "Schüttelmonster", tag = "Nur für Große",
                blurb = "Raue Holzbahn: 38 m Abgrund, harte Kurvenwechsel, Rütteln ohne Pause.",
                rail = C(16742936), accent = C(9132587), support = C(12159573), ground = C(7311182), sky = C(14725264), fog = C(14202010),
                bankFactor = .85, bankMax = 55, trainCars = 5, rough = true, points = pts.ToArray(), theme = "holz",
                plan = t => new[]
                {
                    Drive(0, 26, 3.5, 1),
                    Drive(26, t.SAtPoint(7) + 4, 3.5, 1, lift: true),
                    Drive(t.SAtPoint(IndexOfPoint(t, 8, 44)), t.SAtPoint(IndexOfPoint(t, 30, 44)) + 8, 26, 5, launch: true, felt: true),
                    Brake(t.length - 96, t.length - 24, 3.6, 3.6),
                    Stop(t.length - 24, t.length, 3.5, .8),
                },
            };
        }
    }
}
