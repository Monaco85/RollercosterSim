using System;

namespace Achterbahn
{
    /// <summary>
    /// Schiffsschaukel: starres Pendel mit Antrieb an der Achse. Eigenes Modell nur fuer die Schaukel
    /// (die Achterbahn-Physik in RideModel.cs bleibt unberuehrt). Liefert denselben SeatState wie Ride,
    /// damit Sitz-Filter (MotionMap.Sc, Cues) und SimHub-Paket unveraendert bleiben.
    /// Koordinaten wie die Bahndaten: Schwingebene x/y, Achse entlang z, Drehpunkt in pivot.
    /// </summary>
    public class PendelRide
    {
        public const double G = 9.81;
        const double Damp = .025;      // Luft- und Lagerreibung (1/s)
        const double UMax = 1.8;       // staerkste Antriebs- bzw. Bremsbeschleunigung am Sitz (m/s^2)

        public readonly V3 pivot;
        public readonly double arm, amp, tUp, tHold, tDown;
        public readonly double seatX, seatR;   // Sitz im Boot: entlang Boot, Abstand unter der Achse

        public double th, om, al, u, time;
        public bool done;
        public bool Driving => u > .05 && !done;
        public bool Braking => u < -.05 && !done;
        public double Total => tUp + tHold + tDown + 8;

        public PendelRide(V3 pivot, double arm, double ampDeg, double tUp, double tHold, double tDown, double seatX, double seatR)
        {
            this.pivot = pivot; this.arm = arm; amp = ampDeg * Math.PI / 180; this.tUp = tUp; this.tHold = tHold; this.tDown = tDown;
            this.seatX = seatX; this.seatR = seatR;
            Reset();
        }

        public void Reset() { th = 0; om = 0; al = 0; u = 0; time = 0; done = false; }

        static double Smooth(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }

        /// <summary>Soll-Ausschlag: aufschaukeln, halten, abbremsen.</summary>
        public double Target(double t)
        {
            if (t < tUp) return amp * Smooth(t / tUp);
            if (t < tUp + tHold) return amp;
            return amp * (1 - Smooth((t - tUp - tHold) / tDown));
        }

        public void Step(double dt)
        {
            if (done) { u = 0; al = 0; return; }
            // Energie je Masse am Arm (Bezug: tiefster Punkt)
            double E = .5 * arm * arm * om * om + G * arm * (1 - Math.Cos(th));
            double A = Target(time), Et = G * arm * (1 - Math.Cos(A));
            double dir = Math.Abs(om) > 1e-4 ? Math.Sign(om) : 1;
            u = Math.Max(-UMax, Math.Min(UMax, 1.2 * (Et - E) / arm));
            if (time > tUp + tHold + tDown) u = -UMax * Math.Min(1, Math.Abs(om) * 3);   // Endbremse
            al = -(G / arm) * Math.Sin(th) - Damp * om + u * dir / arm;
            om += al * dt; th += om * dt; time += dt;
            if (time > tUp + tHold + tDown && Math.Abs(th) < .01 && Math.Abs(om) < .01) { th = 0; om = 0; al = 0; u = 0; done = true; }
        }

        static V3 Rot(double c, double s, double x, double y) => new V3(x * c - y * s, x * s + y * c, 0);

        /// <summary>Kraefte am Sitz, L = Vorausschau in Sekunden (wird mit einer Kopie vorgerechnet).</summary>
        public SeatState SeatAt(double L = 0)
        {
            var p = this;
            if (L > 0)
            {
                p = (PendelRide)MemberwiseClone();
                const double h = 1.0 / 240; for (double t = 0; t < L; t += h) p.Step(h);
            }
            double c = Math.Cos(p.th), s = Math.Sin(p.th);
            V3 r = Rot(c, s, seatX, -seatR);                 // Sitz relativ zur Achse
            V3 perp = new V3(-r.y, r.x, 0);                    // Richtung der Bahnbewegung * Radius
            V3 vel = perp * p.om;
            V3 acc = r * (-p.om * p.om) + perp * p.al;
            V3 T = Rot(c, s, 1, 0), U = Rot(c, s, 0, 1), R = V3.Cross(T, U);
            V3 f = acc + new V3(0, G, 0);
            double dir = Math.Abs(p.om) > 1e-4 ? Math.Sign(p.om) : 1;
            return new SeatState
            {
                p = pivot + r, T = T, U = U, R = R, K = default, f = f,
                gUp = V3.Dot(f, U) / G, gSide = V3.Dot(f, R) / G, gFwd = V3.Dot(f, T) / G,
                surge = p.u * dir, latMs2 = 0, vy = vel.y, v = vel.Len,
            };
        }

        public double Speed => Math.Abs(om) * Math.Sqrt(seatX * seatX + seatR * seatR);
        public double AngleDeg => th * 180 / Math.PI;
    }
}
