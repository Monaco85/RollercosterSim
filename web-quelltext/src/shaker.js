// Eigene Tonspur fuer die Bass Shaker: nur Tiefton, links vorne, rechts hinten.
// Laeuft in einem eigenen AudioContext, damit sie auf einem anderen Ausgabegeraet liegen kann als der Fahrsound.
const JOINT_M = 6, AXLE_M = 1.6;   // Abstand der Schienenstoesse, Abstand vorne zu hinten

export class Shaker {
  constructor(ctx) {
    this.ctx = ctx;
    const c = ctx, master = this.master = c.createGain(); master.gain.value = 0;
    const lim = c.createDynamicsCompressor();
    lim.threshold.value = -6; lim.knee.value = 6; lim.ratio.value = 12; lim.attack.value = 0.003; lim.release.value = 0.12;
    const lp = c.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 95; lp.Q.value = 0.7;
    const hp = c.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 18; hp.Q.value = 0.7;
    const merge = this.merge = c.createChannelMerger(2);
    merge.connect(lp); lp.connect(hp); hp.connect(lim); lim.connect(master); master.connect(c.destination);
    this.front = c.createGain(); this.rear = c.createGain();
    this.front.connect(merge, 0, 0); this.rear.connect(merge, 0, 1);

    const len = c.sampleRate * 2, buf = c.createBuffer(1, len, c.sampleRate), d = buf.getChannelData(0);
    for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    const src = c.createBufferSource(); src.buffer = buf; src.loop = true;
    const nf = this.nf = c.createBiquadFilter(); nf.type = 'lowpass'; nf.frequency.value = 45;
    this.rum = c.createGain(); this.rum.gain.value = 0;
    src.connect(nf); nf.connect(this.rum); this.rum.connect(this.front); this.rum.connect(this.rear); src.start();

    const tone = (f, to) => { const o = c.createOscillator(); o.frequency.value = f; const g = c.createGain(); g.gain.value = 0; o.connect(g); to.forEach(t => g.connect(t)); o.start(); return g; };
    this.press = tone(32, [this.front, this.rear]);   // Druck in den Taelern
    this.launch = tone(40, [this.rear]);              // Abschuss schiebt von hinten
    this.brake = tone(52, [this.front]);              // Bremse rattert vorne
    this.dist = 0; this.clack = 0; this.wasAir = false; this.level = 0.6; this.swap = false; this.active = false;
  }
  setLevel(x) { this.level = x; this.master.gain.setTargetAtTime(this.active ? x : 0, this.ctx.currentTime, 0.03); }
  setActive(on) { this.active = on; this.master.gain.setTargetAtTime(on ? this.level : 0, this.ctx.currentTime, 0.03); }
  // Tauscht beide Kanaele komplett, also auch Abschuss und Bremse
  setSwap(on) {
    this.swap = on;
    this.front.disconnect(); this.rear.disconnect();
    this.front.connect(this.merge, 0, on ? 1 : 0); this.rear.connect(this.merge, 0, on ? 0 : 1);
  }
  // kurzer Schlag; ch: 'front' oder 'rear'
  thump(ch, strength, at = 0, freq = 42) {
    const c = this.ctx, t = c.currentTime + at, o = c.createOscillator(), g = c.createGain();
    o.frequency.setValueAtTime(freq * 1.5, t); o.frequency.exponentialRampToValueAtTime(freq, t + 0.05);
    g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(strength, t + 0.006); g.gain.exponentialRampToValueAtTime(0.001, t + 0.11);
    const to = ch === 'front' ? this.front : this.rear;
    o.connect(g); g.connect(to); o.start(t); o.stop(t + 0.13);
  }
  update(v, seg, gUp, dt) {
    const t = this.ctx.currentTime, k = 0.05, set = (p, x) => p.setTargetAtTime(x, t, k);
    const air = gUp < 0.25 && v > 2;
    // bei Luftzeit wird es still, das macht das Schweben spuerbar
    set(this.rum.gain, air ? 0.03 : Math.min(0.5, v * 0.022));
    set(this.nf.frequency, 28 + v * 1.2);
    set(this.press.gain, Math.min(0.75, Math.max(0, gUp - 1.35) * 0.42));
    set(this.launch.gain, seg?.launch ? 0.8 : 0);
    set(this.brake.gain, seg?.type === 'brake' && v > seg.speed + 0.3 ? 0.55 : 0);
    if (this.wasAir && !air) { this.thump('front', 0.95, 0, 34); this.thump('rear', 0.95, 0.04, 34); }
    this.wasAir = air;
    this.dist += v * dt;
    if (this.dist >= JOINT_M && v > 0.5) {
      this.dist %= JOINT_M;
      const s = Math.min(0.55, 0.12 + v * 0.02);
      this.thump('front', s); this.thump('rear', s, Math.min(0.2, AXLE_M / v));
    }
    if (seg?.lift) { this.clack -= dt; if (this.clack <= 0) { this.clack = 0.17; this.thump('front', 0.42, 0, 48); this.thump('rear', 0.42, 0.03, 48); } }
  }
  silence() {
    const t = this.ctx.currentTime;
    for (const g of [this.rum.gain, this.press.gain, this.launch.gain, this.brake.gain]) g.setTargetAtTime(0, t, 0.04);
    this.wasAir = false;
  }
}
