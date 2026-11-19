using System;
using System.Text;
using Achterbahn;

// Wilde Maus: wo entstehen die G-Spitzen?
public static class AgentCheckMaus
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var defs = TrackLibrary.All(); var def = defs[defs.Length - 1];
        var track = new Track(def); int cars = def.trainCars;
        var ride = new Ride(track, def.plan(track), cars * 2.4, (cars - 1) / 2.0 * 2.4 - .1, .009, .5 * 1.225 * 1.1 * 1.7 / (cars * 580.0));
        Ride.AutoBank(track, ride, def.bankFactor, def.bankMax);
        ride.Reset();
        double t = 0, last = -10;
        while (!ride.done && t < 600)
        {
            ride.Step(1.0 / 240); t += 1.0 / 240;
            var st = ride.SeatStateAt(0);
            if ((st.gUp > 2.6 || st.gUp < -.6 || Math.Abs(st.gSide) > 1.1) && t - last > .5)
            { last = t; var p = track.At(ride.s).p; sb.AppendFormat("t={0:0.0} s={1:0} p=({2:0},{3:0.0},{4:0}) v={5:0.0} gUp={6:0.00} gSide={7:0.00}\n", t, ride.s, p.x, p.y, p.z, ride.v, st.gUp, st.gSide); }
        }
        return sb.ToString().Replace(',', '.');
    }
}
