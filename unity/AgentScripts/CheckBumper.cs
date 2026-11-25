using System; using System.Text; using Achterbahn;
// Prueft das Boxauto ohne Bruecke: Spieler faehrt mit festen Mustern gegen Bande und Wagen; G-Spitzen und Sitzwinkel je Stufe.
public static class AgentCheckBumper { public static string Main() {
  var sb = new StringBuilder();
  foreach (var def in TrackLibrary.All())
  {
    if (def.bumper == null) continue; var b = def.bumper;
    foreach (var lv in Cues.LevelKeys)
    {
      var r = new BumperRide(b.cx, b.cz, b.hx, b.hz, b.duration, b.obstacles);
      MotionMap.ResetFilter();
      var cu = new Cues { level = lv, power = 1, target = 1, wild = def.rough };
      double t = 0, vmax = 0, gf = 0, gs = 0, gu0 = 9, gu1 = -9, mp = 0, mr = 0; int steps = 0;
      while (!r.done && t < 200)
      {
        // Spieler: Vollgas, alle 4 s anders lenken (gerade, rechts, gerade, voll rueckwaerts)
        int ph = (int)(t / 4) % 5; double d = ph == 1 ? .5 : ph == 3 ? -.4 : ph == 4 ? Math.PI : 0;
        r.Step(1.0 / 240, 1, d); t += 1.0 / 240; steps++;
        var st = r.SeatAt(0, 1, d);
        vmax = Math.Max(vmax, r.Speed); gf = Math.Max(gf, Math.Abs(st.gFwd)); gs = Math.Max(gs, Math.Abs(st.gSide)); gu0 = Math.Min(gu0, st.gUp); gu1 = Math.Max(gu1, st.gUp);
        var raw = MotionMap.Rough(MotionMap.Sc(r.SeatAt(Math.Min(cu.Lead(), .2), 1, d), 1.0 / 240), r.cars[0].odo * 4, r.Speed * 4, .35);
        var o = cu.Update(raw, 1.0 / 240);
        mp = Math.Max(mp, Math.Abs(o.pitch)); mr = Math.Max(mr, Math.Abs(o.roll));
        if (double.IsNaN(st.gFwd)) { sb.Append("NaN bei t=" + t); break; }
      }
      if (lv == "sanft") sb.AppendFormat("{0}: fertig={1} {2:0.0}s v max {3:0.0} km/h, Rempler {4}, haertester {5:0.00} G, gVor {6:0.00} gSeite {7:0.00} gUp {8:0.00}..{9:0.00}\n",
          def.name, r.done, t, vmax * 3.6, r.hits, r.hardest, gf, gs, gu0, gu1);
      sb.AppendFormat("   Stufe {0,-9} pitch max {1:0.0}  roll max {2:0.0}\n", lv, mp, mr);
      // Lage der KI-Wagen am Ende (in der Flaeche?)
      if (lv == "kraeftig") foreach (var c in r.cars) sb.AppendFormat("   Wagen x {0:0.0} z {1:0.0} (Flaeche x {2}..{3}, z {4}..{5})\n", c.x, c.z, b.cx - b.hx, b.cx + b.hx, b.cz - b.hz, b.cz + b.hz);
    }
  }
  return sb.ToString().Replace(',', '.'); } }
