// Alle Geraeusche sind synthetisch, es gibt keine Audiodateien.
export class Sound {
  constructor() { this.ctx = null; this.vol = 0.8; }
  unlock() {
    if (this.ctx) { this.ctx.resume?.(); return; }
    const A = window.AudioContext || window.webkitAudioContext; if (!A) return;
    const c = this.ctx = new A();
    this.master = c.createGain(); this.master.gain.value = this.vol; this.master.connect(c.destination);
    const len = c.sampleRate * 2, buf = c.createBuffer(1, len, c.sampleRate), d = buf.getChannelData(0);
    for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    this.noise = buf;
    const loop = (type, freq) => {
      const s = c.createBufferSource(); s.buffer = buf; s.loop = true;
      const f = c.createBiquadFilter(); f.type = type; f.frequency.value = freq;
      const g = c.createGain(); g.gain.value = 0; s.connect(f); f.connect(g); g.connect(this.master); s.start();
      return { f, g };
    };
    this.rumble = loop('lowpass', 120); this.wind = loop('bandpass', 700); this.brakeN = loop('highpass', 2600);
    this.nextClack = 0;
  }
  tone(freq, dur, type = 'sine', gain = 0.25, at = 0) {
    if (!this.ctx) return;
    const c = this.ctx, t = c.currentTime + at, o = c.createOscillator(), g = c.createGain();
    o.type = type; o.frequency.value = freq; g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(gain, t + 0.01); g.gain.exponentialRampToValueAtTime(0.001, t + dur);
    o.connect(g); g.connect(this.master); o.start(t); o.stop(t + dur + 0.02);
  }
  clack() {
    if (!this.ctx) return;
    const c = this.ctx, t = c.currentTime, s = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
    s.buffer = this.noise; f.type = 'bandpass'; f.frequency.value = 1500; f.Q.value = 3;
    g.gain.setValueAtTime(0.55, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.05);
    s.connect(f); f.connect(g); g.connect(this.master); s.start(t, Math.random()); s.stop(t + 0.06);
    this.tone(180, 0.06, 'square', 0.1);
  }
  bell() { for (const [f, a] of [[880, 0], [1320, 0], [880, 0.35], [1320, 0.35]]) this.tone(f, 0.5, 'triangle', 0.16, a); }
  beep(hi) { this.tone(hi ? 880 : 520, hi ? 0.5 : 0.18, 'square', 0.14); }
  whoosh() {
    if (!this.ctx) return;
    const c = this.ctx, t = c.currentTime, o = c.createOscillator(), g = c.createGain();
    o.type = 'sawtooth'; o.frequency.setValueAtTime(70, t); o.frequency.exponentialRampToValueAtTime(600, t + 3);
    g.gain.setValueAtTime(0.001, t); g.gain.linearRampToValueAtTime(0.13, t + 0.3); g.gain.exponentialRampToValueAtTime(0.001, t + 3.2);
    o.connect(g); g.connect(this.master); o.start(t); o.stop(t + 3.3);
  }
  fanfare() { [523, 659, 784, 1047].forEach((f, i) => this.tone(f, 0.45, 'triangle', 0.2, i * 0.14)); }
  update(v, seg, gUp, dt) {
    if (!this.ctx) return;
    const t = this.ctx.currentTime, k = 0.08;
    this.rumble.g.gain.setTargetAtTime(Math.min(0.75, v * 0.05), t, k);
    this.rumble.f.frequency.setTargetAtTime(70 + v * 9, t, k);
    this.wind.g.gain.setTargetAtTime(Math.min(0.5, v * v * 0.0011), t, k);
    this.wind.f.frequency.setTargetAtTime(350 + v * 45, t, k);
    const braking = seg && seg.type === 'brake' && v > seg.speed + 0.3;
    this.brakeN.g.gain.setTargetAtTime(braking ? 0.12 : 0, t, 0.05);
    if (seg && seg.lift) { this.nextClack -= dt; if (this.nextClack <= 0) { this.clack(); this.nextClack = 0.17; } }
  }
  silence() { if (!this.ctx) return; const t = this.ctx.currentTime; for (const n of [this.rumble, this.wind, this.brakeN]) n.g.gain.setTargetAtTime(0, t, 0.1); }
}
