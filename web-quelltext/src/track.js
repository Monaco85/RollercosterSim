// Streckengeometrie: Kontrollpunkte -> glatte Kurve -> gleichmaessige Abtastung mit Rahmen (T, U, R).
// Reines JS ohne Abhaengigkeiten, damit es auch im Node-Test laeuft.
export const v = {
  add: (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]],
  sub: (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]],
  mul: (a, k) => [a[0] * k, a[1] * k, a[2] * k],
  dot: (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2],
  cross: (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]],
  len: a => Math.hypot(a[0], a[1], a[2]),
  norm: a => { const l = Math.hypot(a[0], a[1], a[2]) || 1; return [a[0] / l, a[1] / l, a[2] / l]; },
};

function catmull(p0, p1, p2, p3, t) {
  const t2 = t * t, t3 = t2 * t;
  const f = i => 0.5 * (2 * p1[i] + (p2[i] - p0[i]) * t + (2 * p0[i] - 5 * p1[i] + 4 * p2[i] - p3[i]) * t2 + (3 * p1[i] - p0[i] - 3 * p2[i] + p3[i]) * t3);
  return [f(0), f(1), f(2)];
}

export const STEP = 0.25; // Abtastabstand in Metern

// Raumkurve geschlossen abtasten, glaetten, auf gleiche Bogenlaenge bringen
function buildPolyline(points, smooth) {
  const n = points.length, raw = [];
  for (let i = 0; i < n; i++) {
    const p0 = points[(i - 1 + n) % n], p1 = points[i], p2 = points[(i + 1) % n], p3 = points[(i + 2) % n];
    const m = Math.max(8, Math.ceil(v.len(v.sub(p2, p1)) / 0.2));
    for (let k = 0; k < m; k++) raw.push(catmull(p0, p1, p2, p3, k / m));
  }
  let pts = resample(raw, STEP);
  // Binomial-Glaettung: nimmt die Kruemmungsspruenge aus der Spline, sonst gibt es G-Spitzen
  for (let pass = 0; pass < smooth; pass++) {
    const c = pts.length, out = new Array(c);
    for (let i = 0; i < c; i++) {
      const a = pts[(i - 1 + c) % c], b = pts[i], d = pts[(i + 1) % c];
      out[i] = [(a[0] + 2 * b[0] + d[0]) / 4, (a[1] + 2 * b[1] + d[1]) / 4, (a[2] + 2 * b[2] + d[2]) / 4];
    }
    pts = out;
  }
  return resample(pts, STEP);
}

function resample(pts, ds) {
  const n = pts.length;
  const cum = new Float64Array(n + 1);
  for (let i = 0; i < n; i++) cum[i + 1] = cum[i] + v.len(v.sub(pts[(i + 1) % n], pts[i]));
  const L = cum[n], count = Math.round(L / ds), out = [];
  let j = 0;
  for (let i = 0; i < count; i++) {
    const s = (i / count) * L;
    while (cum[j + 1] < s) j++;
    const t = (s - cum[j]) / (cum[j + 1] - cum[j] || 1);
    const a = pts[j], b = pts[(j + 1) % n];
    out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]);
  }
  return out;
}

export class Track {
  constructor(def) {
    this.def = def;
    this.pos = buildPolyline(def.points, def.smooth ?? 900);
    this.n = this.pos.length;
    let L = 0;
    for (let i = 0; i < this.n; i++) L += v.len(v.sub(this.pos[(i + 1) % this.n], this.pos[i]));
    this.length = L; this.ds = L / this.n;
    this._tangents();
    this.bank = new Float64Array(this.n);
    this._frames();
  }
  wrap(i) { return ((i % this.n) + this.n) % this.n; }
  _tangents() {
    const { pos, n } = this;
    this.T = new Array(n); this.K = new Array(n);
    for (let i = 0; i < n; i++) {
      // ueber +-1 m gemittelt, damit die Kruemmung nicht rauscht
      const w = Math.round(1 / this.ds);
      this.T[i] = v.norm(v.sub(pos[this.wrap(i + 1)], pos[this.wrap(i - 1)]));
      const a = pos[this.wrap(i - w)], b = pos[i], c = pos[this.wrap(i + w)];
      const h = w * this.ds;
      this.K[i] = v.mul(v.add(v.sub(a, v.mul(b, 2)), c), 1 / (h * h)); // Kruemmungsvektor
    }
  }
  // Rahmen: Oben-Vektor per Paralleltransport, in normalen Lagen sanft zum Welt-Oben gezogen.
  // Im Looping (Oben zeigt nach unten) wird nicht gezogen, so bleibt der Rahmen stetig.
  _frames() {
    const { T, n } = this;
    this.U0 = new Array(n);
    let u = [0, 1, 0];
    for (let pass = 0; pass < 2; pass++) {
      for (let i = 0; i < n; i++) {
        const t = T[i];
        u = v.norm(v.sub(u, v.mul(t, v.dot(u, t))));
        const flat = 1 - Math.min(1, Math.abs(t[1]) / 0.75);
        if (u[1] > 0.2) {
          const w = [0 - t[1] * t[0], 1 - t[1] * t[1], 0 - t[1] * t[2]];
          const k = 0.05 * flat * flat;
          u = v.norm(v.add(v.mul(u, 1 - k), v.mul(v.norm(w), k)));
        }
        this.U0[i] = u;
      }
    }
    this.applyBank();
  }
  applyBank() {
    const { n, T, U0 } = this;
    this.U = new Array(n); this.R = new Array(n);
    for (let i = 0; i < n; i++) {
      const b = this.bank[i], c = Math.cos(b), s = Math.sin(b);
      const r0 = v.cross(T[i], U0[i]);
      // positive Schraeglage = rechte Seite nach unten
      const u = v.norm(v.add(v.mul(U0[i], c), v.mul(r0, s)));
      this.U[i] = u;
      this.R[i] = v.cross(T[i], u);
    }
  }
  index(s) { return this.wrap(Math.round(s / this.ds)); }
  // lineare Interpolation, fuer Darstellung und Messung
  at(s) {
    const x = ((s % this.length) + this.length) % this.length;
    const f = x / this.ds, i = Math.floor(f) % this.n, j = (i + 1) % this.n, t = f - Math.floor(f);
    const l = (a, b) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
    const T = v.norm(l(this.T[i], this.T[j])), Ur = l(this.U[i], this.U[j]);
    const U = v.norm(v.sub(Ur, v.mul(T, v.dot(Ur, T))));
    return { p: l(this.pos[i], this.pos[j]), T, U, R: v.cross(T, U), K: l(this.K[i], this.K[j]) };
  }
  // Abschnitt am Streckenanteil (Index der Kontrollpunkte) -> Meter
  sAtPoint(k) {
    const p = this.def.points[k]; let best = 0, bd = 1e9;
    for (let i = 0; i < this.n; i++) { const d = v.len(v.sub(this.pos[i], p)); if (d < bd) { bd = d; best = i; } }
    return best * this.ds;
  }
}
