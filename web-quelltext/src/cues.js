// Motion-Cues: macht aus echten Achterbahnkraeften Signale, die ein 2DOF-Sitz sicher fahren kann.
// Keine Winkelspruenge (Looping!), harte Obergrenzen, sanftes Ein- und Ausblenden.
export const LEVELS = {
  sanft:   { label: 'Sanft',   gain: 0.35, hz: 1.2, pitch: 7,  roll: 6,  acc: 4, rate: 10 },
  normal:  { label: 'Normal',  gain: 0.6,  hz: 2.0, pitch: 12, roll: 10, acc: 7, rate: 18 },
  kraeftig:{ label: 'Kräftig', gain: 2.0,  hz: 3.0, pitch: 36, roll: 30, acc: 22, rate: 60 },
};
const RAD = 180 / Math.PI;
const soft = (x, lim) => lim * Math.tanh(x / lim);

class Lp2 { // kritisch gedaempfter Tiefpass 2. Ordnung, ohne Ueberschwingen
  constructor() { this.a = 0; this.b = 0; }
  step(x, hz, dt) {
    const k = Math.min(1, 2 * Math.PI * hz * dt);
    this.a += (x - this.a) * k; this.b += (this.a - this.b) * k;
    return this.b;
  }
  reset() { this.a = this.b = 0; }
}

export class Cues {
  constructor() {
    this.ch = { pitch: new Lp2(), roll: new Lp2(), surge: new Lp2(), sway: new Lp2(), heave: new Lp2() };
    this.master = 0; this.target = 0; this.power = 1; this.sp = 0; this.sr = 0; this.level = 'normal';
    this.out = { pitch: 0, roll: 0, surge: 0, sway: 0, heave: 0, pitchRate: 0, rollRate: 0 };
  }
  // target 1 = Fahrt, 0 = auf Neutral ausblenden. Immer ueber 2,5 s, nie schlagartig
  update(raw, dt) {
    // Der Kraft-Regler skaliert alles gemeinsam: Staerke, Obergrenzen und Stellgeschwindigkeit
    const B = LEVELS[this.level], k = this.power;
    const L = { gain: B.gain * k, hz: B.hz, pitch: B.pitch * k, roll: B.roll * k, acc: B.acc * k, rate: B.rate * k };
    const step = dt / 2.5;
    this.master += Math.max(-step, Math.min(step, this.target - this.master));
    const m = this.master * this.master * (3 - 2 * this.master);
    const prev = this.out;
    const c = this.ch;
    // Winkel zusaetzlich in der Geschwindigkeit begrenzt: die Wischermotoren sollen nicht schlagen
    const lim = (x, was) => was + Math.max(-L.rate * dt, Math.min(L.rate * dt, x - was));
    this.sp = lim(soft(raw.pitch * L.gain, L.pitch) * m, this.sp);
    this.sr = lim(soft(raw.roll * L.gain, L.roll) * m, this.sr);
    const pitch = c.pitch.step(this.sp, L.hz, dt);
    const roll = c.roll.step(this.sr, L.hz, dt);
    const surge = c.surge.step(soft(raw.surge * L.gain, L.acc) * m, L.hz, dt);
    const sway = c.sway.step(soft(raw.sway * L.gain, L.acc) * m, L.hz, dt);
    const heave = c.heave.step(soft(raw.heave * L.gain, L.acc) * m, L.hz, dt);
    this.out = { pitch, roll, surge, sway, heave, pitchRate: (pitch - prev.pitch) / dt, rollRate: (roll - prev.roll) / dt };
    return this.out;
  }
}

// Rohsignale aus dem Sitz-Zustand. asin statt atan2: laeuft durch den Looping ohne Sprung.
export function rawFromSeat(st) {
  return {
    pitch: -Math.asin(Math.max(-1, Math.min(1, st.T[1]))) * RAD,   // SimHub: positiv = Nase runter
    // Kippt nach aussen, gleichsinnig mit Sway. Die sichtbare Schraeglage kippt nach innen und wuerde dagegen arbeiten.
    roll: -(st.latMs2 / 9.81) * 14,                               // SimHub: positiv = nach rechts geneigt
    surge: st.surge,
    sway: st.latMs2,
    heave: (st.gUp - 1) * 9.81,
  };
}
