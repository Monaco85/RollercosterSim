using System;

namespace Achterbahn
{
    public struct Raw { public double pitch, roll, surge, sway, heave; }
    public struct CueOut { public double pitch, roll, surge, sway, heave, pitchRate, rollRate; }

    public struct Level
    {
        public string label; public double gain, hz, pitch, roll, acc, rate;
    }

    /// <summary>Bewegungsfilter und Begrenzer, 1:1 aus der HTML-Fassung (Klasse qr). Ueber den Sitz-Winkel entscheidet allein dieser Code.</summary>
    public class Cues
    {
        public static readonly string[] LevelKeys = { "sanft", "normal", "kraeftig" };
        public static Level GetLevel(string key)
        {
            switch (key)
            {
                case "normal": return new Level { label = "Normal", gain = .6, hz = 2, pitch = 12, roll = 10, acc = 7, rate = 18 };
                case "kraeftig": return new Level { label = "Kräftig", gain = 2, hz = 3, pitch = 36, roll = 30, acc = 22, rate = 60 };
                default: return new Level { label = "Sanft", gain = .35, hz = 1.2, pitch = 7, roll = 6, acc = 4, rate = 10 };
            }
        }

        class Lp2
        {
            double a, b;
            public double Step(double x, double hz, double dt)
            {
                double s = Math.Min(1, 2 * Math.PI * hz * dt);
                a += (x - a) * s; b += (a - b) * s;
                return b;
            }
        }

        static double Es(double i, double t) => t * Math.Tanh(i / t);
        static double Clamp(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));

        readonly Lp2 chPitch = new Lp2(), chRoll = new Lp2(), chSurge = new Lp2(), chSway = new Lp2(), chHeave = new Lp2();
        public double master, target, power = 1;
        public bool wild;
        public string level = "sanft";
        double sp, sr;
        public CueOut o;

        public double Lead()
        {
            var n = GetLevel(level);
            double hz = wild && level == "kraeftig" ? 6 : n.hz;
            return 2 / (2 * Math.PI * hz) + .08;
        }

        public CueOut Update(Raw t, double e)
        {
            var n = GetLevel(level);
            double s = power;
            bool r = wild && level == "kraeftig";
            double gain = n.gain * s, hz = r ? 6 : n.hz, pitchLim = n.pitch * s, rollLim = n.roll * s, acc = n.acc * s, rate = n.rate * s * (r ? 2 : 1);
            double step = e / 2.5;
            master += Clamp(target - master, -step, step);
            double c = master * master * (3 - 2 * master);
            CueOut l = o;
            double Lim(double p, double E) => E + Clamp(p - E, -rate * e, rate * e);
            sp = Lim(Es(t.pitch * gain, pitchLim) * c, sp);
            sr = Lim(Es(t.roll * gain, rollLim) * c, sr);
            double d = chPitch.Step(sp, hz, e);
            double f = chRoll.Step(sr, hz, e);
            double g = chSurge.Step(Es(t.surge * gain, acc) * c, hz, e);
            double x = chSway.Step(Es(t.sway * gain, acc) * c, hz, e);
            double m = chHeave.Step(Es(t.heave * gain, acc) * c, hz, e);
            o = new CueOut { pitch = d, roll = f, surge = g, sway = x, heave = m, pitchRate = (d - l.pitch) / e, rollRate = (f - l.roll) / e };
            return o;
        }
    }

    /// <summary>Kipp-Koordination fuer den 2DOF-Sitz (Funktion Sc der HTML-Fassung) und das Rumpeln der Holzbahn (Uh).</summary>
    public static class MotionMap
    {
        const double T0 = 180 / Math.PI;
        static double hp, last = 1; public static bool init;
        public static void ResetFilter() { init = false; }

        static double SmoothStep(double x, double min, double max)
        {
            if (x <= min) return 0; if (x >= max) return 1;
            x = (x - min) / (max - min); return x * x * (3 - 2 * x);
        }

        public static Raw Sc(SeatState i, double dt)
        {
            double fU = i.gUp, fT = i.gFwd, fR = i.gSide, fu = Math.Max(fU, .35);
            double pSf = -Math.Atan2(fT, fu) * T0, rSf = -Math.Atan2(fR, fu) * T0;
            double p = -Math.Asin(Math.Max(-1, Math.Min(1, i.T.y))) * T0;
            double pAtt = p * Math.Abs(p) / (Math.Abs(p) + 6);
            double L = SmoothStep(1 - fU, .15, .7);
            if (!init) { last = fU; hp = 0; init = true; }
            double tau = .6; hp = (hp + fU - last) * (tau / (tau + dt)); last = fU;
            double stoss = -Math.Max(-2, Math.Min(2, hp)) * 7;
            double pitch = pSf * (1 - L) + pAtt * .8 * L + stoss, roll = -rSf * (1 - .5 * L);
            return new Raw { pitch = pitch, roll = roll, surge = i.surge, sway = -fR * 9.81, heave = (fU - 1) * 9.81 };
        }

        public static Raw Rough(Raw i, double t, double e, double n)
        {
            double s = n * Math.Min(1, e / 14);
            double r = Math.Sin(t * .71), a = Math.Sin(t * 1.13 + 1), o = Math.Sin(t * 1.73 + 2.1), c = Math.Sin(t * .93 + 4.2);
            return new Raw { pitch = i.pitch + s * (5 * r + 3 * o), roll = i.roll + s * (6 * a + 3 * c), heave = i.heave + s * (5 * o + 3 * a), sway = i.sway + s * (3 * c), surge = i.surge };
        }
    }
}
