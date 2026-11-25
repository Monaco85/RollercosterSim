using System; using System.Text; using Achterbahn;
public static class AgentCheckWik { public static string Main() {
  var sb = new StringBuilder();
  var defs = TrackLibrary.All(); var def = defs[defs.Length - 1];
  var track = new Track(def); int cars = def.trainCars;
  var ride = new Ride(track, def.plan(track), cars * 2.4, (cars - 1) / 2.0 * 2.4 - .1, .009, .5 * 1.225 * 1.1 * 1.7 / (cars * 580.0));
  Ride.AutoBank(track, ride, def.bankFactor, def.bankMax);
  ride.Reset(); MotionMap.ResetFilter();
  var cu = new Cues { level = "normal", power = 1, wild = false, target = 1 };
  double[][] marks = { new double[]{104,3,0}, new double[]{268,3.5,7}, new double[]{428,3.5,14} };
  var ms = new double[3];
  for (int k = 0; k < 3; k++) { double best = 1e9; for (double s = 0; s < track.length; s += .5) { var p = track.At(s).p; double d = (p.x-marks[k][0])*(p.x-marks[k][0])+(p.y-marks[k][1])*(p.y-marks[k][1])+(p.z-marks[k][2])*(p.z-marks[k][2]); if (d < best) { best = d; ms[k] = s; } } }
  bool[] hit = new bool[3];
  double t = 0, last = -10, gmax=-9,gmin=9,smax=0,mp=0,mr=0, vmax=0, lead = cu.Lead();
  while (!ride.done && t < 600) {
    ride.Step(1.0/240); t += 1.0/240;
    var st = ride.SeatStateAt(0);
    for (int k=0;k<3;k++) if (!hit[k] && ride.s >= ms[k]) { hit[k]=true; sb.AppendFormat("Looping {0} bei s={1:0}: v={2:0.0} m/s\n", k+1, ms[k], ride.v); }
    gmax=Math.Max(gmax,st.gUp); gmin=Math.Min(gmin,st.gUp); smax=Math.Max(smax,Math.Abs(st.gSide)); vmax=Math.Max(vmax,ride.v);
    var o = cu.Update(MotionMap.Sc(ride.SeatStateAt(lead), 1.0/240), 1.0/240); mp=Math.Max(mp,Math.Abs(o.pitch)); mr=Math.Max(mr,Math.Abs(o.roll));
    if ((st.gUp > 3.2 || st.gUp < -.3 || Math.Abs(st.gSide) > 1.0) && t - last > .4) { last = t; var p = track.At(ride.s).p; sb.AppendFormat("  t={0:0.0} s={1:0} p=({2:0},{3:0.0},{4:0}) v={5:0.0} gUp={6:0.00} gSide={7:0.00}\n", t, ride.s, p.x, p.y, p.z, ride.v, st.gUp, st.gSide); }
  }
  sb.AppendFormat("len={0:0} done={1} zeit={2:0.0}s vmax={3:0}km/h gUp {4:0.00}..{5:0.00} gSeite {6:0.00} | pitch {7:0.0} roll {8:0.0}\n", track.length, ride.done, t, vmax*3.6, gmin, gmax, smax, mp, mr);
  return sb.ToString().Replace(',', '.'); } }
