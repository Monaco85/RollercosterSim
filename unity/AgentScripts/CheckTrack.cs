using System;
using System.Text;
using Achterbahn;

// Prueft eine Bahn: Faehrt sie durch? Welche G-Kraefte und welche Sitzwinkel entstehen?
public static class AgentCheckTrack
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
            ride.Reset(); MotionMap.ResetFilter();
            var cu = new Cues { level = "normal", power = 1, wild = def.rough, target = 1 };
            double t = 0, vmax = 0, gmax = -9, gmin = 9, smax = 0, vmin = 99, mp = 0, mr = 0, lead = cu.Lead(); double sAtVmin = 0;
            while (!ride.done && t < 600)
            {
                ride.Step(1.0 / 240); t += 1.0 / 240;
                var st = ride.SeatStateAt(0);
                vmax = Math.Max(vmax, ride.v); gmax = Math.Max(gmax, st.gUp); gmin = Math.Min(gmin, st.gUp); smax = Math.Max(smax, Math.Abs(st.gSide));
                if (t > 30 && ride.s < track.length - 90 && ride.v < vmin) { vmin = ride.v; sAtVmin = ride.s; }
                var o = cu.Update(MotionMap.Sc(ride.SeatStateAt(lead), 1.0 / 240), 1.0 / 240);
                mp = Math.Max(mp, Math.Abs(o.pitch)); mr = Math.Max(mr, Math.Abs(o.roll));
            }
            sb.AppendFormat("{0,-16} len={1:0} done={2} zeit={3:0.0}s vmax={4:0.0}km/h gUp {5:0.00}..{6:0.00} gSeite max {7:0.00} vmin(frei)={8:0.0}m/s bei s={9:0} | Normal: pitch {10:0.0} roll {11:0.0}\n",
                def.name, track.length, ride.done, t, vmax * 3.6, gmin, gmax, smax, vmin, sAtVmin, mp, mr);
        }
        return sb.ToString().Replace(',', '.');
    }
}
