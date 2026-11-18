using System;
using System.Collections.Generic;

namespace Achterbahn
{
    public struct Sample { public V3 p, T, U, R, K; }

    /// <summary>Streckengeometrie, 1:1 aus der HTML-Fassung (Klasse Wr): Kontrollpunkte, glatte Kurve, gleichmaessige Abtastung, Rahmen T/U/R.</summary>
    public class Track
    {
        public const double Step = 0.25;

        public readonly TrackDef def;
        public V3[] pos, T, K, U0, U, R;
        public double[] bank;
        public int n;
        public double length, ds;

        public Track(TrackDef d)
        {
            def = d;
            pos = BuildPolyline(d.points, d.smooth);
            n = pos.Length;
            double L = 0;
            for (int i = 0; i < n; i++) L += (pos[(i + 1) % n] - pos[i]).Len;
            length = L; ds = L / n;
            Tangents();
            bank = new double[n];
            Frames();
        }

        static double Round(double x) => Math.Floor(x + 0.5); // wie Math.round in JS

        public int Wrap(int i) => ((i % n) + n) % n;

        static V3 Catmull(V3 a, V3 b, V3 c, V3 d, double t)
        {
            double r = t * t, q = r * t;
            double F(double p0, double p1, double p2, double p3) =>
                .5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * r + (3 * p1 - p0 - 3 * p2 + p3) * q);
            return new V3(F(a.x, b.x, c.x, d.x), F(a.y, b.y, c.y, d.y), F(a.z, b.z, c.z, d.z));
        }

        static V3[] BuildPolyline(V3[] points, int smooth)
        {
            int e = points.Length;
            var raw = new List<V3>();
            for (int r = 0; r < e; r++)
            {
                V3 a = points[(r - 1 + e) % e], o = points[r], c = points[(r + 1) % e], l = points[(r + 2) % e];
                int h = Math.Max(8, (int)Math.Ceiling((c - o).Len / .2));
                for (int u = 0; u < h; u++) raw.Add(Catmull(a, o, c, l, u / (double)h));
            }
            V3[] s = Resample(raw.ToArray(), Step);
            var buf = new V3[s.Length];
            for (int pass = 0; pass < smooth; pass++)
            {
                int cnt = s.Length;
                for (int c = 0; c < cnt; c++)
                {
                    V3 l = s[(c - 1 + cnt) % cnt], h = s[c], u = s[(c + 1) % cnt];
                    buf[c] = new V3((l.x + 2 * h.x + u.x) / 4, (l.y + 2 * h.y + u.y) / 4, (l.z + 2 * h.z + u.z) / 4);
                }
                var tmp = s; s = buf; buf = tmp;
            }
            return Resample(s, Step);
        }

        static V3[] Resample(V3[] pts, double ds)
        {
            int e = pts.Length;
            var cum = new double[e + 1];
            for (int c = 0; c < e; c++) cum[c + 1] = cum[c] + (pts[(c + 1) % e] - pts[c]).Len;
            double s = cum[e];
            int r = (int)Round(s / ds);
            var outp = new V3[r];
            int o = 0;
            for (int c = 0; c < r; c++)
            {
                double l = (double)c / r * s;
                while (cum[o + 1] < l) o++;
                double den = cum[o + 1] - cum[o]; if (den == 0) den = 1;
                double h = (l - cum[o]) / den;
                outp[c] = V3.Lerp(pts[o], pts[(o + 1) % e], h);
            }
            return outp;
        }

        void Tangents()
        {
            T = new V3[n]; K = new V3[n];
            for (int i = 0; i < n; i++)
            {
                int s = (int)Round(1 / ds);
                T[i] = V3.Norm(pos[Wrap(i + 1)] - pos[Wrap(i - 1)]);
                V3 r = pos[Wrap(i - s)], a = pos[i], o = pos[Wrap(i + s)];
                double c = s * ds;
                K[i] = (r - a * 2 + o) * (1 / (c * c));
            }
        }

        void Frames()
        {
            U0 = new V3[n];
            V3 u = new V3(0, 1, 0);
            for (int pass = 0; pass < 2; pass++)
                for (int r = 0; r < n; r++)
                {
                    V3 a = T[r];
                    u = V3.Norm(u - a * V3.Dot(u, a));
                    double o = 1 - Math.Min(1, Math.Abs(a.y) / .75);
                    if (u.y > .2)
                    {
                        V3 c = new V3(0 - a.y * a.x, 1 - a.y * a.y, 0 - a.y * a.z);
                        double l = .05 * o * o;
                        u = V3.Norm(u * (1 - l) + V3.Norm(c) * l);
                    }
                    U0[r] = u;
                }
            ApplyBank();
        }

        public void ApplyBank()
        {
            U = new V3[n]; R = new V3[n];
            for (int s = 0; s < n; s++)
            {
                double r = bank[s], a = Math.Cos(r), o = Math.Sin(r);
                V3 c = V3.Cross(T[s], U0[s]);
                V3 l = V3.Norm(U0[s] * a + c * o);
                U[s] = l; R[s] = V3.Cross(T[s], l);
            }
        }

        public int Index(double s) => Wrap((int)Round(s / ds));

        public Sample At(double s)
        {
            double x = ((s % length) + length) % length / ds;
            int i = (int)Math.Floor(x) % n, j = (i + 1) % n;
            double a = x - Math.Floor(x);
            V3 Tt = V3.Norm(V3.Lerp(T[i], T[j], a));
            V3 ur = V3.Lerp(U[i], U[j], a);
            V3 Uu = V3.Norm(ur - Tt * V3.Dot(ur, Tt));
            return new Sample { p = V3.Lerp(pos[i], pos[j], a), T = Tt, U = Uu, R = V3.Cross(Tt, Uu), K = V3.Lerp(K[i], K[j], a) };
        }

        /// <summary>Meter-Stand der Kontrollpunkt-Nummer k (naechster Abtastpunkt).</summary>
        public double SAtPoint(int k)
        {
            V3 p = def.points[k]; int best = 0; double bd = 1e9;
            for (int i = 0; i < n; i++) { double d = (pos[i] - p).Len; if (d < bd) { bd = d; best = i; } }
            return best * ds;
        }
    }
}
