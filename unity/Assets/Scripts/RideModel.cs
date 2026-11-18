using System;

namespace Achterbahn
{
    public struct SeatState
    {
        public V3 p, T, U, R, K, f;
        public double gUp, gSide, gFwd, surge, latMs2, vy, v;
    }

    /// <summary>Zugphysik, 1:1 aus der HTML-Fassung (Klasse Xr): Energieerhaltung entlang der Strecke plus Antrieb, Bremse, Reibung.</summary>
    public class Ride
    {
        public const double G = 9.81;
        public const double RollDefault = .012, DragDefault = 9e-4;

        public readonly Track t;
        public readonly Seg[] segs;
        public readonly double trainLen, seat, mu, cd;
        public double s, v, time, a, driveA;
        public bool done;
        public Seg seg;

        public Ride(Track track, Seg[] segments, double trainLen, double seat, double mu, double cd)
        {
            t = track; segs = segments; this.trainLen = trainLen; this.seat = seat; this.mu = mu; this.cd = cd;
            Reset();
        }

        public void Reset() { s = .5; v = 0; time = 0; a = 0; driveA = 0; done = false; seg = null; }

        public Seg SegAt(double x)
        {
            foreach (var g in segs) if (x >= g.from && x < g.to) return g;
            return null;
        }

        // Mittlere Steigung ueber die ganze Zuglaenge
        double Slope(double x)
        {
            double e = trainLen, s2 = 0; const int n = 6;
            for (int r = 0; r <= n; r++) s2 += t.At(x - e / 2 + e * r / n).T.y;
            return s2 / (n + 1);
        }

        public void Step(double dt)
        {
            if (done) { driveA = 0; a = 0; return; }
            Seg e = SegAt(s); seg = e;
            double n = v, sl = Slope(s);
            double r = -G * sl;
            double fric = n > .05 ? -(mu * G + cd * n * n) : 0;
            double o = 0, c = r + fric;
            if (e != null)
            {
                if (e.type == Seg.Kind.Drive)
                {
                    double h = e.speed - n;
                    if (h > 0) o = Math.Min(e.accel, h / dt) - c;
                    else if (e.hold) o = Math.Max(-e.accel, h / dt) - c;
                }
                else if (e.type == Seg.Kind.Brake)
                {
                    if (n > e.speed) o = -Math.Min(e.accel, Math.Min(1.5 * (n - e.speed) + .3, (n - e.speed) / dt));
                }
                else
                {
                    double h = Math.Max(0, e.to - s - .05);
                    double u = Math.Min(e.speed, Math.Sqrt(2 * e.accel * h));
                    o = (u - n) / dt - c;
                    if (h < .02 || (u < .02 && n < .05)) { v = 0; done = true; driveA = 0; a = 0; return; }
                }
            }
            if (e != null && e.type == Seg.Kind.Stop) o = Math.Max(o, -e.accel * 1.5 - c);
            double l = c + o;
            v = Math.Max(0, n + l * dt);
            s += (n + v) / 2 * dt;
            a = l;
            driveA = (e != null && e.type == Seg.Kind.Drive && !e.felt) ? fric : o + fric;
            time += dt;
        }

        /// <summary>Kraefte am Sitz. L = Vorausschau in Sekunden.</summary>
        public SeatState SeatStateAt(double L = 0)
        {
            double V = Math.Max(0, v + a * L);
            Sample st = t.At(s + seat + v * L + .5 * a * L * L);
            V3 e = st.K * (V * V);
            V3 nn = st.T * a;
            V3 f = e + nn + new V3(0, G, 0);
            int r = t.Index(s + seat + v * L);
            V3 aa = V3.Cross(t.T[r], t.U0[r]);
            double o = Math.Max(0, Math.Min(1, (t.U0[r].y - .5) / .35));
            double l = o * o * (3 - 2 * o) * V3.Dot(st.K, aa) * V * V;
            return new SeatState
            {
                p = st.p, T = st.T, U = st.U, R = st.R, K = st.K, f = f,
                gUp = V3.Dot(f, st.U) / G, gSide = V3.Dot(f, st.R) / G, gFwd = V3.Dot(f, st.T) / G,
                surge = driveA, latMs2 = l, vy = V * st.T.y, v = V,
            };
        }

        /// <summary>Schraeglage aus der gefahrenen Geschwindigkeit (Funktion Ih der HTML-Fassung).</summary>
        public static void AutoBank(Track track, Ride ride, double factor, double maxDeg)
        {
            int n = track.n;
            var speed = new double[n];
            ride.Reset();
            for (int u = 0; u < 400000 && !ride.done; u++)
            {
                ride.Step(1.0 / 60);
                int k = track.Index(ride.s);
                speed[k] = Math.Max(speed[k], ride.v);
            }
            double last = 0;
            for (int u = 0; u < n; u++) { if (speed[u] == 0) speed[u] = last; last = speed[u]; }
            ride.Reset();
            var o = new double[n];
            double c = maxDeg * Math.PI / 180;
            for (int u = 0; u < n; u++)
            {
                V3 d = track.K[u];
                V3 f = V3.Cross(track.T[u], track.U0[u]);
                double g = V3.Dot(d, f) * speed[u] * speed[u];
                double m = track.U0[u].y > .5 ? Math.Atan2(g, G) * factor : 0;
                o[u] = Math.Max(-c, Math.Min(c, m));
            }
            double[] l = o;
            int h = (int)Math.Floor(9 / track.ds + .5);
            for (int u = 0; u < 3; u++)
            {
                var d = new double[n]; double f = 0;
                for (int g = -h; g <= h; g++) f += l[track.Wrap(g)];
                for (int g = 0; g < n; g++)
                {
                    d[g] = f / (2 * h + 1);
                    f += l[track.Wrap(g + h + 1)] - l[track.Wrap(g - h)];
                }
                l = d;
            }
            track.bank = l;
            track.ApplyBank();
        }
    }
}
