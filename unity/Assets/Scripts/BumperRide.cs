using System;
using System.Collections.Generic;

namespace Achterbahn
{
    /// <summary>Ein Boxauto: Lage, Geschwindigkeit, Drehung, Eingaben, Stoss-Zustand und (fuer die KI) ein eigenes Gedaechtnis.</summary>
    public class BumperCar
    {
        public double x, z, h;            // Mitte (Unity-Meter, Boden), Fahrtrichtung h (0 = +z, positiv = nach rechts drehen)
        public double vx, vz, w;          // Geschwindigkeit (m/s) und Drehrate (rad/s)
        public double ax, az, aw;         // Beschleunigung aus dem letzten Schritt
        public double gas, delta;         // Pedal 0..1 (negativ = rueckwaerts), Winkel des Antriebsrads (rad, 0 = geradeaus)
        public double contactF;           // Summe der Stossfedern in diesem Schritt (N)
        public double bz, bv, ba;         // Wippen des Wagens auf den Federn nach einem Stoss (m, m/s, m/s^2)
        public double odo;                // gefahrene Strecke (fuer das Bodenrumpeln)
        public bool ai;
        public bool aus;                  // ausgeschaltet (Option „KI-Wagen aus“): steht still, unsichtbar, rechnet nicht mit
        // KI
        public uint seed; public double tx, tz, timer, stuckT, backT, chase, eager;
        public BumperCar Copy() => (BumperCar)MemberwiseClone();
        public double Fx => Math.Sin(h);
        public double Fz => Math.Cos(h);
        public double Speed => Math.Sqrt(vx * vx + vz * vz);
    }

    /// <summary>
    /// Autoscooter wie auf dem Rummel: Antriebsrad vorne, das sich mit dem Lenkrad mitdreht (voll eingeschlagen faehrt der Wagen
    /// rueckwaerts), feste Hinterraeder, rutschiger Stahlboden. Wagen sind Kapseln mit Gummiring; Stoesse sind weiche Federn mit
    /// Daempfung (etwa 0,25 s Kontakt), daraus entstehen die Kraefte am Sitz. Eigenes Modell nur fuer diese Fahrt; die Achterbahn-Physik
    /// bleibt unberuehrt. Liefert denselben SeatState, damit Sitz-Filter (MotionMap.Sc, Cues) und SimHub-Paket unveraendert bleiben.
    /// </summary>
    public class BumperRide
    {
        public const double G = 9.81;
        // Wagen
        const double Mass = 290, Inertia = 290 * .42;       // Wagen 210 kg + Fahrer 80 kg
        const double Half = .36, Rad = .8;                  // Kapsel: Mittelpunkte +-0,36 m, Gummiring-Radius 0,8 m -> 2,32 x 1,6 m
        const double FrontA = .62, RearB = .55;             // Antriebsrad vorne, Hinterachse
        const double DriveF = 520, VMax = 3.4;              // Motorkraft (N) und Hoechstgeschwindigkeit (m/s, ~12 km/h)
        const double Mu = .38, LatStiff = 3800, Roll = .018; // Reibwert Rad/Stahlboden, Seitenfuehrung, Rollwiderstand
        // Gummiring: Feder und Daempfer (Kontaktzeit ~0,25 s, Stosszahl ~0,55)
        const double KWall = 46000, CWall = 1350, KCar = 46000, CCar = 950, MuRub = .45;
        // Wippen auf den Federn
        const double BounceW = 2 * Math.PI * 4.2, BounceZ = .28, BounceK = .11;

        public readonly double cx, cz, hx, hz;              // Fahrflaeche (Innenkante der Bande)
        public readonly double duration;
        public BumperCar[] cars;
        public double time; public bool done, powerOff;
        public int hits; public double hardest;              // Rempler des Spielers, haertester Stoss (G)
        bool touching;                                        // Spieler hatte im letzten Schritt Kontakt
        bool isClone;
        /// <summary>Welcher Wagen wird hier gesteuert und gefuehlt (Sitz). Einzelspieler und LAN-Gastgeber: 0, LAN-Gast: 1.</summary>
        public int me = 0;
        /// <summary>LAN-Gast: die anderen Wagen bekommen ihre Eingaben vom Gastgeber, hier denkt keine KI.</summary>
        public bool replica;
        public readonly List<(int a, int b, double strength, double x, double z)> events = new List<(int, int, double, double, double)>();

        public readonly double[] obstacles;                  // runde Hindernisse: je x, z, Radius (Unity-Meter)

        public BumperRide(double cx, double cz, double hx, double hz, double duration, double[] obstacles = null)
        {
            this.obstacles = obstacles ?? new double[0];
            this.cx = cx; this.cz = cz; this.hx = hx; this.hz = hz; this.duration = duration;
            cars = new BumperCar[3];
            Reset();
        }

        public bool Driving => !powerOff && cars[me].gas > .05;
        public bool Braking => powerOff && !done;
        public double Total => duration;
        public double Speed => cars[me].Speed;

        public void Reset()
        {
            time = 0; done = false; powerOff = false; hits = 0; hardest = 0; touching = false; events.Clear();
            // Startplaetze: Spieler vorn in der Mitte, die beiden anderen versetzt gegenueber
            cars[0] = new BumperCar { x = cx, z = cz - hz + 2.6, h = 0, seed = 1 };
            cars[1] = new BumperCar { x = cx - hx * .3, z = cz + hz * .62, h = Math.PI * .85, ai = true, seed = 0x9E3779B9, eager = .45 };
            cars[2] = new BumperCar { x = cx + hx * .62, z = cz - hz * .05, h = -Math.PI * .7, ai = true, seed = 0x7F4A7C15, eager = .3 };
            foreach (var c in cars) { c.tx = c.x; c.tz = c.z; c.timer = .5; }
        }

        public BumperRide Clone()
        {
            var r = (BumperRide)MemberwiseClone();
            r.cars = new BumperCar[cars.Length];
            for (int i = 0; i < cars.Length; i++) r.cars[i] = cars[i].Copy();
            r.isClone = true;
            return r;
        }

        // ---------------- Zufall (deterministisch, damit die Vorausschau dasselbe rechnet) ----------------
        static double Rnd(BumperCar c) { c.seed = c.seed * 1664525u + 1013904223u; return (c.seed >> 8) / 16777216.0; }

        // ---------------- KI ----------------
        void Think(BumperCar c, BumperCar player, double dt, bool chasePlayer)
        {
            c.timer -= dt;
            if (c.backT > 0)
            {
                // Rueckwaerts frei fahren (Rad steht nach hinten)
                c.backT -= dt; c.gas = .9; c.delta = Math.PI + (c.seed % 2 == 0 ? .5 : -.5); return;
            }
            if (c.timer <= 0)
            {
                c.chase = chasePlayer && Rnd(c) < c.eager ? 1 : 0;
                for (int k = 0; k < 8; k++)   // Ziel nicht in ein Hindernis legen
                {
                    c.tx = cx + (Rnd(c) * 2 - 1) * (hx - 2.2); c.tz = cz + (Rnd(c) * 2 - 1) * (hz - 2.2);
                    if (!NearObstacle(c.tx, c.tz, 2.2)) break;
                }
                c.timer = 1.8 + Rnd(c) * 3.2;
            }
            if (c.chase > 0) { c.tx = player.x + player.vx * .5; c.tz = player.z + player.vz * .5; }
            double dx = c.tx - c.x, dz = c.tz - c.z;
            double want = Math.Atan2(dx, dz), err = Wrap(want - c.h);
            // Hindernis voraus: seitlich vorbeilenken
            for (int k = 0; k + 2 < obstacles.Length; k += 3)
            {
                double ox = obstacles[k] - c.x, oz = obstacles[k + 1] - c.z, r = obstacles[k + 2];
                double ahead = ox * c.Fx + oz * c.Fz, side = ox * c.Fz - oz * c.Fx;
                if (ahead > 0 && ahead < r + 3.5 && Math.Abs(side) < r + 1.3 && c.chase == 0) err -= Math.Sign(side == 0 ? 1 : side) * .9 * (1 - ahead / (r + 3.5));
            }
            c.delta = Math.Max(-1.25, Math.Min(1.25, err * 1.3));
            c.gas = Math.Sqrt(dx * dx + dz * dz) < 1.5 && c.chase == 0 ? .3 : .8 + .2 * c.eager;
            // Festgefahren an Bande oder Wagen -> kurz zuruecksetzen
            if (c.Speed < .35 && c.gas > .2) c.stuckT += dt; else c.stuckT = Math.Max(0, c.stuckT - dt * 2);
            if (c.stuckT > 1.1) { c.stuckT = 0; c.backT = .8 + Rnd(c) * .6; c.timer = 0; }
        }

        bool NearObstacle(double x, double z, double margin)
        {
            for (int k = 0; k + 2 < obstacles.Length; k += 3) { double dx = x - obstacles[k], dz = z - obstacles[k + 1], r = obstacles[k + 2] + margin; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        static double Wrap(double a) { while (a > Math.PI) a -= 2 * Math.PI; while (a < -Math.PI) a += 2 * Math.PI; return a; }

        // ---------------- Schritt ----------------
        /// <summary>Ein Zeitschritt. playerGas/playerDelta kommen vom Lenkrad; demo = alle drei Wagen fahren selbst (Bahnwahl).</summary>
        public void Step(double dt, double playerGas, double playerDelta, bool demo = false)
        {
            if (done) return;
            time += dt;
            if (!demo && time > duration) powerOff = true;
            var p = cars[me];
            for (int i = 0; i < cars.Length; i++)
            {
                var c = cars[i];
                if (powerOff || c.aus) { c.gas = 0; continue; }
                if (replica) { if (i == me && !demo) { c.gas = playerGas; c.delta = playerDelta; } continue; }   // Rest kommt vom Gastgeber
                if (c.ai || demo) Think(c, cars[0], dt, !demo || i != 0);
                else if (i == me) { c.gas = playerGas; c.delta = playerDelta; }
                // sonst: Mitspieler im LAN, gas/delta setzt der Gastgeber von aussen
            }

            int n = cars.Length;
            var fx = new double[n]; var fz = new double[n]; var tq = new double[n];
            for (int i = 0; i < n; i++) { cars[i].contactF = 0; if (!cars[i].aus) Wheels(cars[i], out fx[i], out fz[i], out tq[i]); }

            // Bande
            for (int i = 0; i < n; i++)
            {
                var c = cars[i];
                if (c.aus) continue;
                for (int e = -1; e <= 1; e += 2)
                {
                    double px = c.x + c.Fx * Half * e, pz = c.z + c.Fz * Half * e;
                    WallContact(c, i, px, pz, 1, 0, cx - hx, ref fx[i], ref fz[i], ref tq[i]);
                    WallContact(c, i, px, pz, -1, 0, -(cx + hx), ref fx[i], ref fz[i], ref tq[i]);
                    WallContact(c, i, px, pz, 0, 1, cz - hz, ref fx[i], ref fz[i], ref tq[i]);
                    WallContact(c, i, px, pz, 0, -1, -(cz + hz), ref fx[i], ref fz[i], ref tq[i]);
                }
            }
            // Hindernisse (Brunnen-Insel, Pfeiler)
            for (int i = 0; i < n; i++) if (!cars[i].aus) for (int k = 0; k + 2 < obstacles.Length; k += 3) ObstacleContact(cars[i], i, obstacles[k], obstacles[k + 1], obstacles[k + 2], ref fx[i], ref fz[i], ref tq[i]);
            // Wagen gegen Wagen
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (!cars[i].aus && !cars[j].aus) CarContact(i, j, fx, fz, tq);

            // Integrieren (halb-implizit)
            for (int i = 0; i < n; i++)
            {
                var c = cars[i];
                if (c.aus) { c.vx = c.vz = c.w = c.ax = c.az = c.aw = 0; continue; }
                c.ax = fx[i] / Mass; c.az = fz[i] / Mass; c.aw = tq[i] / Inertia;
                c.vx += c.ax * dt; c.vz += c.az * dt; c.w += c.aw * dt;
                c.x += c.vx * dt; c.z += c.vz * dt; c.h = Wrap(c.h + c.w * dt);
                c.odo += c.Speed * dt;
                // Wagen wippt auf den Federn (Stoss trifft oberhalb des Schwerpunkts)
                c.ba = -BounceW * BounceW * c.bz - 2 * BounceZ * BounceW * c.bv + BounceK * c.contactF / Mass;
                c.bv += c.ba * dt; c.bz += c.bv * dt;
            }

            // Rempler zaehlen (Spieler)
            bool t = p.contactF > 300;
            if (t && !touching) hits++;
            touching = t;
            hardest = Math.Max(hardest, p.contactF / Mass / G);

            if (powerOff && time > duration + 1.5)
            {
                bool still = true; foreach (var c in cars) if (!c.aus && (c.Speed > .06 || Math.Abs(c.w) > .06)) still = false;
                if (still || time > duration + 8) done = true;
            }
        }

        /// <summary>Raeder: Antrieb und Seitenfuehrung am drehbaren Vorderrad, Seitenfuehrung an der Hinterachse, Rollwiderstand.</summary>
        static void Wheels(BumperCar c, out double fx, out double fz, out double tq)
        {
            double Fx = c.Fx, Fz = c.Fz, Rx = Fz, Rz = -Fx;   // vorn, rechts
            fx = fz = tq = 0;
            double N = Mass * G * .5;
            // Vorderrad (gelenkt, angetrieben)
            {
                double cd = Math.Cos(c.delta), sd = Math.Sin(c.delta);
                double dx = Fx * cd + Rx * sd, dz = Fz * cd + Rz * sd;      // Rollrichtung des Rads
                double nx = dz, nz = -dx;                                    // quer dazu
                double pvx = c.vx + c.w * FrontA * Rx, pvz = c.vz + c.w * FrontA * Rz;
                double vl = pvx * dx + pvz * dz, vq = pvx * nx + pvz * nz;
                double drive = c.gas >= 0 ? c.gas * DriveF * Math.Max(0, 1 - vl / VMax) : c.gas * DriveF * Math.Max(0, 1 + vl / VMax);
                double roll = -Roll * N * Math.Tanh(vl * 4);
                double lat = Math.Max(-Mu * N, Math.Min(Mu * N, -LatStiff * vq));
                double ffx = dx * (drive + roll) + nx * lat, ffz = dz * (drive + roll) + nz * lat;
                // Reibkreis grob: Summe nicht ueber Mu*N
                double m = Math.Sqrt(ffx * ffx + ffz * ffz), lim = Mu * N * 1.1;
                if (m > lim) { ffx *= lim / m; ffz *= lim / m; }
                fx += ffx; fz += ffz; tq += FrontA * (ffx * Rx + ffz * Rz);
            }
            // Hinterachse (zwei feste Raeder)
            {
                double pvx = c.vx - c.w * RearB * Rx, pvz = c.vz - c.w * RearB * Rz;
                double vl = pvx * Fx + pvz * Fz, vq = pvx * Rx + pvz * Rz;
                double roll = -Roll * N * Math.Tanh(vl * 4);
                double lat = Math.Max(-Mu * N, Math.Min(Mu * N, -LatStiff * vq));
                double ffx = Fx * roll + Rx * lat, ffz = Fz * roll + Rz * lat;
                fx += ffx; fz += ffz; tq += -RearB * (ffx * Rx + ffz * Rz);
            }
            // Drehdaempfung (Reifen schmieren beim Drehen auf der Stelle)
            tq -= c.w * 55;
        }

        static void PointVel(BumperCar c, double px, double pz, out double vx, out double vz)
        {
            // v + w x r: Punkt rechts vorn bewegt sich bei w > 0 ... (h waechst = Drehung nach rechts)
            double rx = px - c.x, rz = pz - c.z;
            double u = rx * c.Fx + rz * c.Fz, s = rx * c.Fz - rz * c.Fx;   // vorwaerts, rechts
            double Rx = c.Fz, Rz = -c.Fx;
            vx = c.vx + c.w * (u * Rx - s * c.Fx); vz = c.vz + c.w * (u * Rz - s * c.Fz);
        }

        static double Torque(BumperCar c, double px, double pz, double fx, double fz)
        {
            double rx = px - c.x, rz = pz - c.z;
            double u = rx * c.Fx + rz * c.Fz, s = rx * c.Fz - rz * c.Fx;
            double Rx = c.Fz, Rz = -c.Fx;
            return u * (fx * Rx + fz * Rz) - s * (fx * c.Fx + fz * c.Fz);
        }

        /// <summary>Bande als Ebene n·p >= d (n zeigt in die Fahrflaeche).</summary>
        void WallContact(BumperCar c, int i, double px, double pz, double nx, double nz, double d, ref double fx, ref double fz, ref double tq)
        {
            double dist = px * nx + pz * nz - d, pen = Rad - dist;
            if (pen <= 0) return;
            double qx = px - nx * dist, qz = pz - nz * dist;     // Beruehrpunkt an der Bande
            PointVel(c, qx, qz, out double vx, out double vz);
            double vn = vx * nx + vz * nz, vt = vx * -nz + vz * nx;
            double Fn = Math.Max(0, KWall * pen - CWall * vn);
            double Ft = -Math.Max(-MuRub * Fn, Math.Min(MuRub * Fn, vt * 900));
            double Fx = nx * Fn + -nz * Ft, Fz = nz * Fn + nx * Ft;
            fx += Fx; fz += Fz; tq += Torque(c, qx, qz, Fx, Fz);
            c.contactF += Fn;
            if (!isClone && i >= 0 && Fn > 0 && -vn > .5 && pen < .06) events.Add((i, -1, -vn, qx, qz));
        }

        /// <summary>Rundes, gepolstertes Hindernis (Kreis mit Radius r) gegen die Wagen-Kapsel.</summary>
        void ObstacleContact(BumperCar c, int i, double ox, double oz, double r, ref double fx, ref double fz, ref double tq)
        {
            double ax = c.x - c.Fx * Half, az = c.z - c.Fz * Half, dx = 2 * c.Fx * Half, dz = 2 * c.Fz * Half;
            double t = Clamp01(((ox - ax) * dx + (oz - az) * dz) / (dx * dx + dz * dz));
            double px = ax + dx * t, pz = az + dz * t;
            double nx = px - ox, nz = pz - oz, dist = Math.Sqrt(nx * nx + nz * nz), pen = Rad + r - dist;
            if (pen <= 0) return;
            if (dist < 1e-6) { nx = 1; nz = 0; } else { nx /= dist; nz /= dist; }   // n: vom Hindernis zum Wagen
            double qx = ox + nx * r, qz = oz + nz * r;
            PointVel(c, qx, qz, out double vx, out double vz);
            double vn = vx * nx + vz * nz, vt = vx * -nz + vz * nx;
            double Fn = Math.Max(0, KWall * pen - CWall * vn);
            double Ft = -Math.Max(-MuRub * Fn, Math.Min(MuRub * Fn, vt * 900));
            double Fx = nx * Fn + -nz * Ft, Fz = nz * Fn + nx * Ft;
            fx += Fx; fz += Fz; tq += Torque(c, qx, qz, Fx, Fz);
            c.contactF += Fn;
            if (!isClone && Fn > 0 && -vn > .5 && pen < .06) events.Add((i, -2, -vn, qx, qz));
        }

        void CarContact(int i, int j, double[] fx, double[] fz, double[] tq)
        {
            var a = cars[i]; var b = cars[j];
            double ddx = b.x - a.x, ddz = b.z - a.z;
            if (ddx * ddx + ddz * ddz > (2 * (Half + Rad)) * (2 * (Half + Rad))) return;
            Closest(a.x - a.Fx * Half, a.z - a.Fz * Half, a.x + a.Fx * Half, a.z + a.Fz * Half,
                    b.x - b.Fx * Half, b.z - b.Fz * Half, b.x + b.Fx * Half, b.z + b.Fz * Half,
                    out double pax, out double paz, out double pbx, out double pbz);
            double nx = pbx - pax, nz = pbz - paz, dist = Math.Sqrt(nx * nx + nz * nz);
            double pen = 2 * Rad - dist;
            if (pen <= 0) return;
            if (dist < 1e-6) { nx = 1; nz = 0; } else { nx /= dist; nz /= dist; }   // n: von a nach b
            double qx = (pax + pbx) * .5, qz = (paz + pbz) * .5;
            PointVel(a, qx, qz, out double avx, out double avz);
            PointVel(b, qx, qz, out double bvx, out double bvz);
            double rvx = bvx - avx, rvz = bvz - avz;
            double vn = rvx * nx + rvz * nz, vt = rvx * -nz + rvz * nx;       // vn < 0: naehern sich
            double Fn = Math.Max(0, KCar * pen - CCar * vn);
            double Ft = -Math.Max(-MuRub * Fn, Math.Min(MuRub * Fn, vt * 700));
            double Fx = nx * Fn + -nz * Ft, Fz = nz * Fn + nx * Ft;            // Kraft auf b
            fx[j] += Fx; fz[j] += Fz; tq[j] += Torque(b, qx, qz, Fx, Fz);
            fx[i] -= Fx; fz[i] -= Fz; tq[i] += Torque(a, qx, qz, -Fx, -Fz);
            a.contactF += Fn; b.contactF += Fn;
            if (!isClone && Fn > 0 && -vn > .5 && pen < .06) events.Add((i, j, -vn, qx, qz));
        }

        static void Closest(double a0x, double a0z, double a1x, double a1z, double b0x, double b0z, double b1x, double b1z,
            out double pax, out double paz, out double pbx, out double pbz)
        {
            double d1x = a1x - a0x, d1z = a1z - a0z, d2x = b1x - b0x, d2z = b1z - b0z, rx = a0x - b0x, rz = a0z - b0z;
            double A = d1x * d1x + d1z * d1z, E = d2x * d2x + d2z * d2z, F = d2x * rx + d2z * rz;
            double C = d1x * rx + d1z * rz, B = d1x * d2x + d1z * d2z, den = A * E - B * B;
            double s = den > 1e-9 ? Clamp01((B * F - C * E) / den) : 0;
            double t = (B * s + F) / E;
            if (t < 0) { t = 0; s = Clamp01(-C / A); } else if (t > 1) { t = 1; s = Clamp01((B - C) / A); }
            pax = a0x + d1x * s; paz = a0z + d1z * s; pbx = b0x + d2x * t; pbz = b0z + d2z * t;
        }
        static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

        // ---------------- Sitz ----------------
        /// <summary>Kraefte am Fahrersitz (Wagen 0). L = Vorausschau in s, gerechnet mit einer Kopie und gleichbleibenden Eingaben.</summary>
        public SeatState SeatAt(double L, double playerGas, double playerDelta)
        {
            var r = this;
            if (L > 0 && !done)
            {
                r = Clone();
                const double hh = 1.0 / 240; for (double t = 0; t < L; t += hh) r.Step(hh, playerGas, playerDelta);
            }
            var c = r.cars[me];
            // Bahn-Koordinaten wie die Achterbahn: JS (x, y, z) = Unity (x, y, -z)
            V3 T = new V3(c.Fx, 0, -c.Fz), U = new V3(0, 1, 0), R = V3.Cross(T, U);
            V3 acc = new V3(c.ax, c.ba, -c.az);
            V3 f = acc + new V3(0, G, 0);
            double along = c.ax * c.Fx + c.az * c.Fz;
            return new SeatState
            {
                p = new V3(c.x, 1.0, -c.z), T = T, U = U, R = R, K = default, f = f,
                gUp = V3.Dot(f, U) / G, gSide = V3.Dot(f, R) / G, gFwd = V3.Dot(f, T) / G,
                surge = Math.Max(-20, Math.Min(20, along)), latMs2 = 0, vy = 0, v = c.Speed,
            };
        }
    }
}
