import { Track, v } from '../src/track.js';
import { Ride, autoBank } from '../src/physics.js';
import { TRACKS } from '../src/tracks.js';
import { Cues, rawFromSeat, LEVELS } from '../src/cues.js';
let fail = 0;
for (const def of TRACKS) {
  const t = new Track(def), r = new Ride(t, def.plan(t));
  autoBank(t, r, def.bankFactor, def.bankMax);
  const c = new Cues(); c.level = 'kraeftig'; c.target = 1;
  let gs=0,ss=0,maxV = 0, minVAfterLift = 1e9, gMax = -9, gMin = 9, side = 0, fwd = 0, maxRate = 0, pMax = 0, rMax = 0, sec = 0, stall = false, jerk = 0, pg = null;
  const dt = 1 / 240; let liftDone = false;
  for (let i = 0; i < 240 * 400 && !r.done; i++) {
    r.step(dt);
    const st = r.seatState();
    const o = c.update(rawFromSeat(st), dt);
    maxV = Math.max(maxV, r.v);
    if (r.seg && (r.seg.lift || r.seg.launch)) liftDone = true;
    const free = liftDone && (!r.seg || r.seg.type === 'none');
    if (free) { minVAfterLift = Math.min(minVAfterLift, r.v); }
    if (r.v > 1) { if(st.gUp>gMax)gs=r.s; if(Math.abs(st.gSide)>side)ss=r.s; gMax = Math.max(gMax, st.gUp); gMin = Math.min(gMin, st.gUp); side = Math.max(side, Math.abs(st.gSide)); fwd = Math.max(fwd, Math.abs(st.surge) / 9.81); if (pg !== null) jerk = Math.max(jerk, Math.abs(st.gUp - pg) / dt); pg = st.gUp; }
    maxRate = Math.max(maxRate, Math.abs(o.pitchRate), Math.abs(o.rollRate)); pMax = Math.max(pMax, Math.abs(o.pitch)); rMax = Math.max(rMax, Math.abs(o.roll));
    if (r.v < 0.01 && r.time > 5 && !r.done) { stall = true; break; }
    sec = r.time;
  }
  // Selbstberuehrung pruefen: Streckenteile, die sich zu nahe kommen
  let near = 1e9, nearAt = null;
  for (let i = 0; i < t.n; i += 4) for (let j = i + 200; j < t.n; j += 4) {
    if (t.n - j + i < 200) continue;
    const d = v.len(v.sub(t.pos[i], t.pos[j])); if (d < near) { near = d; nearAt = [(i * t.ds).toFixed(0), (j * t.ds).toFixed(0)]; }
  }
  const minY = Math.min(...t.pos.map(p => p[1])), maxY = Math.max(...t.pos.map(p => p[1]));
  const ok = r.done && !stall && near > 3.2 && gMax < 4.2 && gMin > -0.8;
  if (!ok) fail++;
  console.log(`${ok ? 'OK ' : 'XX '}${def.name}: ${t.length.toFixed(0)} m, ${sec.toFixed(0)} s, vmax ${(maxV * 3.6).toFixed(0)} km/h, vmin frei ${(minVAfterLift * 3.6).toFixed(0)}, G ${gMin.toFixed(2)}..${gMax.toFixed(2)}, seit ${side.toFixed(2)}, längs ${fwd.toFixed(2)}, y ${minY.toFixed(1)}..${maxY.toFixed(1)}, Abstand ${near.toFixed(1)} @${nearAt}, @G ${gs.toFixed(0)} @S ${ss.toFixed(0)}, Ende ${r.done ? 'ja' : 'NEIN s=' + r.s.toFixed(0) + '/' + t.length.toFixed(0)}, Cue max P${pMax.toFixed(1)} R${rMax.toFixed(1)} Rate ${maxRate.toFixed(0)}°/s`);
}
process.exit(fail ? 1 : 0);
