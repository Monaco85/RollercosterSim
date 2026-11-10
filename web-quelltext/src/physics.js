// Zugphysik: Energieerhaltung entlang der Strecke + Antrieb, Bremsen, Reibung.
import { v } from './track.js';
export const G = 9.81;
const ROLL = 0.012, DRAG = 0.0009;

export class Ride {
  constructor(track, segs, cfg = {}) {
    this.t = track; this.segs = segs;
    this.trainLen = cfg.trainLen ?? 9; this.seat = cfg.seat ?? 4; // Sitz vor dem Zugschwerpunkt
    this.reset();
  }
  reset() { this.s = 0.5; this.v = 0; this.time = 0; this.a = 0; this.driveA = 0; this.done = false; this.seg = null; this.hold = 0; }
  segAt(s) { for (const g of this.segs) if (s >= g.from && s < g.to) return g; return null; }
  // Mittlere Steigung des ganzen Zugs: Der Zug haengt ueber Kuppen, deshalb fuehlt sich ein Abgrund vorn zaeh, hinten wild an
  slope(s) {
    const L = this.trainLen, m = 6; let y = 0;
    for (let i = 0; i <= m; i++) y += this.t.at(s - L / 2 + (L * i) / m).T[1];
    return y / (m + 1);
  }
  step(dt) {
    if (this.done) { this.driveA = 0; this.a = 0; return; }
    const g = this.segAt(this.s); this.seg = g;
    const v0 = this.v, sl = this.slope(this.s);
    let grav = -G * sl, fric = v0 > 0.05 ? -(ROLL * G + DRAG * v0 * v0) : 0, drive = 0;
    const pass = grav + fric;
    if (g) {
      if (g.type === 'drive') {            // Kettenlift, Stationsantrieb, Abschuss
        const dv = g.speed - v0;
        if (dv > 0) drive = Math.min(g.accel, dv / dt) - pass;
        else if (g.hold) drive = Math.max(-g.accel, dv / dt) - pass;
      } else if (g.type === 'brake') {     // Trimmbremse
        if (v0 > g.speed) drive = Math.max(-g.accel, (g.speed - v0) / dt);
      } else if (g.type === 'stop') {      // Einfahrt: weich exakt am Streckenende anhalten
        const left = Math.max(0, g.to - this.s - 0.05);
        const vt = Math.min(g.speed, Math.sqrt(2 * g.accel * left));
        drive = (vt - v0) / dt - pass;
        if (left < 0.02 || vt < 0.02 && v0 < 0.05) { this.v = 0; this.done = true; this.driveA = 0; this.a = 0; return; }
      }
    }
    if (g && g.type === 'stop') drive = Math.max(drive, -g.accel * 1.5 - pass);
    const a = pass + drive;
    this.v = Math.max(0, v0 + a * dt);
    this.s += ((v0 + this.v) / 2) * dt;
    this.a = a;
    // fuehlbarer Laengsanteil ohne Schwerkraft. Kettenlift bewusst nicht: den zeigt die Neigung
    this.driveA = (g && g.type === 'drive' && !g.felt) ? fric : drive + fric;
    this.time += dt;
  }
  // Kraefte am Sitz. f ist die spezifische Kraft (was man im Sitz spuert), in m/s2
  seatState() {
    const st = this.t.at(this.s + this.seat);
    const an = v.mul(st.K, this.v * this.v);
    const atw = v.mul(st.T, this.a);
    const f = v.add(v.add(an, atw), [0, G, 0]);
    // Seitenkraft wie ohne Ueberhoehung: So spuert man die Kurve auf dem Sitz, obwohl die Schiene sie wegneigt.
    const i = this.t.index(this.s + this.seat), R0 = v.cross(this.t.T[i], this.t.U0[i]);
    const uy = Math.max(0, Math.min(1, (this.t.U0[i][1] - 0.5) / 0.35)), fade = uy * uy * (3 - 2 * uy);   // im Looping ausblenden
    const latMs2 = fade * v.dot(st.K, R0) * this.v * this.v;
    return { ...st, f, gUp: v.dot(f, st.U) / G, gSide: v.dot(f, st.R) / G, gFwd: v.dot(f, st.T) / G, surge: this.driveA, latMs2 };
  }
}

// Schraeglage aus der gefahrenen Geschwindigkeit, damit Kurven ohne viel Seitendruck laufen
export function autoBank(track, ride, factor, maxDeg) {
  const n = track.n, speed = new Float64Array(n);
  ride.reset();
  for (let i = 0; i < 400000 && !ride.done; i++) { ride.step(1 / 60); speed[track.index(ride.s)] = Math.max(speed[track.index(ride.s)], ride.v); }
  let last = 0; for (let i = 0; i < n; i++) { if (speed[i] === 0) speed[i] = last; last = speed[i]; }
  ride.reset();
  const out = new Float64Array(n), max = maxDeg * Math.PI / 180;
  for (let i = 0; i < n; i++) {
    const K = track.K[i], R0 = v.cross(track.T[i], track.U0[i]);
    const lat = v.dot(K, R0) * speed[i] * speed[i];                  // seitliche Beschleunigung ohne Schraeglage
    const up = track.U0[i][1];
    const b = up > 0.5 ? Math.atan2(lat, G) * factor : 0;
    out[i] = Math.max(-max, Math.min(max, b));
  }
  // lange Glaettung, sonst dreht die Schraeglage ruckartig ein
  let cur = out;
  const w = Math.round(9 / track.ds);
  for (let p = 0; p < 3; p++) {
    const nx = new Float64Array(n); let sum = 0;
    for (let i = -w; i <= w; i++) sum += cur[track.wrap(i)];
    for (let i = 0; i < n; i++) { nx[i] = sum / (2 * w + 1); sum += cur[track.wrap(i + w + 1)] - cur[track.wrap(i - w)]; }
    cur = nx;
  }
  track.bank = cur; track.applyBank();
}
