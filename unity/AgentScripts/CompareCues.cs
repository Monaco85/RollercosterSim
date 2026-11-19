using System;
using System.Text;
using Achterbahn;

public static class AgentCompareCues
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var defs = TrackLibrary.All();
        var cfgs = new[] { ("kraeftig", 1.0), ("sanft", 2.5) };
        foreach (var (lvl, pw) in cfgs)
            for (int i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                var track = new Track(def);
                int cars = def.trainCars;
                var ride = new Ride(track, def.plan(track), cars * 2.4, (cars - 1) / 2.0 * 2.4 - .1, def.rough ? .016 : .009, .5 * 1.225 * 1.1 * 1.7 / (cars * 580.0));
                Ride.AutoBank(track, ride, def.bankFactor, def.bankMax);
                ride.Reset(); MotionMap.ResetFilter();
                var cu = new Cues { level = lvl, power = pw, wild = def.rough, target = 1 };
                double lead = cu.Lead(), t = 0, next = 7, mp = 0, mr = 0; int k = 0;
                var sm = new StringBuilder();
                while (!ride.done && t < 600)
                {
                    ride.Step(1.0 / 240); t += 1.0 / 240;
                    var raw = MotionMap.Sc(ride.SeatStateAt(lead), 1.0 / 240);
                    var g = ride.seg;
                    bool ta = def.rough && !(g != null && g.lift) && !(g != null && g.type == Seg.Kind.Stop) && !(g != null && g.type == Seg.Kind.Drive && !g.launch);
                    if (ta) raw = MotionMap.Rough(raw, ride.s, ride.v, 1);
                    var o = cu.Update(raw, 1.0 / 240);
                    mp = Math.Max(mp, Math.Abs(o.pitch)); mr = Math.Max(mr, Math.Abs(o.roll));
                    if (t >= next && k < 5) { next += 11; k++; sm.AppendFormat("  {0:0.0} {1:0.0000} {2:0.0000} {3:0.0000} {4:0.0000} {5:0.0000} {6:0.0000}\n", t, o.pitch, o.roll, o.surge, o.sway, o.heave, o.pitchRate); }
                }
                sb.AppendFormat("{0}/{1} i={2} maxP={3:0.0000} maxR={4:0.0000}\n{5}", lvl, pw, i, mp, mr, sm);
            }
        return sb.ToString().Replace(',', '.');
    }
}
