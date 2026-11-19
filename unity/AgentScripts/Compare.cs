using System;
using System.Text;
using Achterbahn;

public static class AgentCompare
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var defs = TrackLibrary.All();
        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            var track = new Track(def);
            int cars = def.trainCars;
            var ride = new Ride(track, def.plan(track), cars * 2.4, (cars - 1) / 2.0 * 2.4 - .1, def.rough ? .016 : .009, .5 * 1.225 * 1.1 * 1.7 / (cars * 580.0));
            Ride.AutoBank(track, ride, def.bankFactor, def.bankMax);
            ride.Reset();
            double vmax = 0, gmax = -9, gmin = 9, t = 0, next = 10;
            var sm = new StringBuilder();
            int k = 0;
            while (!ride.done && t < 600)
            {
                ride.Step(1.0 / 240); t += 1.0 / 240;
                var st = ride.SeatStateAt(0);
                vmax = Math.Max(vmax, ride.v); gmax = Math.Max(gmax, st.gUp); gmin = Math.Min(gmin, st.gUp);
                if (t >= next && k < 6) { next += 10; k++; sm.AppendFormat("  {0:0.0} {1:0.000} {2:0.0000} {3:0.0000} {4:0.0000} {5:0.0000} {6:0.0000}\n", t, ride.s, ride.v, st.gUp, st.gSide, st.gFwd, st.latMs2); }
            }
            sb.AppendFormat("i={0} len={1:0.000} n={2} time={3:0.000} vmax={4:0.0000} gmax={5:0.0000} gmin={6:0.0000}\n{7}", i, track.length, track.n, t, vmax, gmax, gmin, sm);
        }
        return sb.ToString().Replace(',', '.');
    }
}
