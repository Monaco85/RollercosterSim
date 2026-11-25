using System;
using System.Text;
using Achterbahn;

// Prueft die Schiffsschaukel (Pendelmodell): Ablauf, G-Kraefte, Sitzwinkel je Stufe. Laeuft ohne Bruecke.
public static class AgentCheckSwing
{
    public static string Main()
    {
        var sb = new StringBuilder();
        foreach (var def in TrackLibrary.All())
        {
            if (def.swing == null) continue;
            var w = def.swing;
            foreach (var lv in Cues.LevelKeys)
            {
                var p = new PendelRide(w.pivot, w.arm, w.ampDeg, w.tUp, w.tHold, w.tDown, w.seatX, w.seatR);
                MotionMap.ResetFilter();
                var cu = new Cues { level = lv, power = 1, target = 1 };
                double lead = cu.Lead(), t = 0, gmax = -9, gmin = 9, fmax = 0, smax = 0, vmax = 0, angMax = 0, mp = 0, mr = 0, umax = 0, sur = 0;
                int zero = 0; double lastTh = 0, firstCross = -1, lastCross = -1;
                while (!p.done && t < 300)
                {
                    p.Step(1.0 / 240); t += 1.0 / 240;
                    var st = p.SeatAt(0);
                    gmax = Math.Max(gmax, st.gUp); gmin = Math.Min(gmin, st.gUp); fmax = Math.Max(fmax, Math.Abs(st.gFwd)); smax = Math.Max(smax, Math.Abs(st.gSide));
                    vmax = Math.Max(vmax, st.v); angMax = Math.Max(angMax, Math.Abs(p.AngleDeg)); umax = Math.Max(umax, Math.Abs(p.u)); sur = Math.Max(sur, Math.Abs(st.surge));
                    if (lastTh < 0 && p.th >= 0) { zero++; if (t > w.tUp && firstCross < 0) firstCross = t; if (t < w.tUp + w.tHold) lastCross = t; }
                    lastTh = p.th;
                    var o = cu.Update(MotionMap.Sc(p.SeatAt(lead), 1.0 / 240), 1.0 / 240);
                    mp = Math.Max(mp, Math.Abs(o.pitch)); mr = Math.Max(mr, Math.Abs(o.roll));
                }
                if (lv == "sanft")
                    sb.AppendFormat("{0}: fertig={1} zeit={2:0.0}s max {3:0.0} Grad, v {4:0.0} km/h, gUp {5:0.00}..{6:0.00}, gVor max {7:0.00}, gSeite {8:0.00}, Antrieb max {9:0.00} m/s2, Schwuenge {10}\n",
                        def.name, p.done, t, angMax, vmax * 3.6, gmin, gmax, fmax, smax, umax, zero);
                sb.AppendFormat("   Stufe {0,-9} pitch max {1:0.0}  roll max {2:0.0}  surge max {3:0.00}\n", lv, mp, mr, sur);
            }
        }
        return sb.ToString().Replace(',', '.');
    }
}
