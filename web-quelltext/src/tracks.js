// Die drei Bahnen. Punkte: [x, y(Hoehe), z]. Station liegt immer bei x=0..., Fahrtrichtung +x.
// Looping wird aus der Physik gerechnet: Kruemmung so, dass der Sitzdruck der Wunschkurve folgt
// (unten rund 3,2 G, oben leicht). So gibt es keinen G-Stoss wie beim Kreis.
function loopPts(x0, z0, side, y0, v0, gBottom = 3.2, gTop = 0.5) {
  const g = 9.81, ds = 0.1, half = [];
  let x = 0, y = 0, th = 0, s = 0;
  while (th < Math.PI && s < 300) {
    const v2 = v0 * v0 - 2 * g * y;
    const nTarget = gTop + (gBottom - gTop) * (1 + Math.cos(th)) / 2;
    const n = 1 + (nTarget - 1) * Math.min(1, s / 16);
    th += ds * g * (n - Math.cos(th)) / v2;
    x += Math.cos(th) * ds; y += Math.sin(th) * ds; s += ds;
    half.push([x, y]);
  }
  const xt = x, yt = y, pts = [], step = 40;
  const D = 2 * xt;
  const up = half.filter((_, i) => i % step === 0);
  const all = [...up, [xt, yt], ...up.slice().reverse().map(([a, b]) => [2 * xt - a, b])];
  all.forEach(([a, b], i) => { const u = i / (all.length - 1), e = u * u * (3 - 2 * u); pts.push([x0 + a, y0 + b, z0 + side * e]); });
  loopPts.last = { D, height: yt, exitX: x0 + D };
  return pts;
}
function helixPts(cx, cz, r, y0, y1, turns, start, dir) {
  const pts = [], n = Math.round(turns * 10);
  for (let i = 0; i <= n; i++) {
    const a = start + dir * (i / n) * turns * Math.PI * 2;
    pts.push([cx + Math.cos(a) * r, y0 + (y1 - y0) * (i / n), cz + Math.sin(a) * r]);
  }
  return pts;
}

export const TRACKS = [
  {
    id: 'raupe', name: 'Kleine Raupe', tag: 'Für den Anfang', color: 0x35c46b, accent: 0xffc531, supports: 0xf4f0e4,
    ground: [0x8fd45e, 0x7cc653, 0x6aa84f], trees: [0x4fae4a, 0x72c24f, 0xf2a03d], hills: 34, pines: false, snow: false,
    sky: [0x57b6ff, 0xd8f1ff], fog: 0xcfeeff, sun: 0xfff2d6, sunPos: [120, 200, 80],
    blurb: 'Gemütlich, keine Überschlag-Stelle, ein Tunnel und viele kleine Hügel.',
    bankFactor: 0.9, bankMax: 28, trainCars: 4,
    points: [
      [-14, 3, 0], [0, 3, 0], [12, 3, 0], [22, 3.4, 0], [34, 6.2, 0], [46, 9.2, 0], [56, 11.2, 0], [64, 11.4, 1],
      [72, 10, 6], [76, 8, 15], [72, 6, 24], [62, 4.2, 27], [50, 6.3, 28], [40, 3.6, 30], [28, 5.5, 31], [16, 3, 33],
      [2, 3.8, 38], [-10, 6, 46], [-24, 3.4, 48], [-38, 3, 40], [-44, 3.4, 26], [-40, 3.2, 12], [-30, 3, 4], [-22, 3, 0.8],
    ],
    plan: t => [
      { type: 'drive', from: 0, to: 22, speed: 3, accel: 1 },
      { type: 'drive', from: 22, to: t.sAtPoint(6) + 2, speed: 3, accel: 1, lift: true },
      { type: 'brake', from: t.sAtPoint(21), to: t.length - 22, speed: 4, accel: 2.2 },
      { type: 'stop', from: t.length - 22, to: t.length, speed: 3, accel: 0.8 },
    ],
    tunnels: [[0.56, 0.64]],
  },
  {
    id: 'bergblitz', name: 'Bergblitz', tag: 'Mittel', color: 0x2f7bff, accent: 0xff6a3d, supports: 0xdfe6f2,
    ground: [0x76b96a, 0x5ea65f, 0x7f8a8c], trees: [0x2f7d4f, 0x3f9160], hills: 120, pines: true, snow: true,
    sky: [0x2e86e6, 0xcfe9ff], fog: 0xd7ecff, sun: 0xffffff, sunPos: [-100, 220, 120],
    blurb: 'Hoher Lifthügel, freier Fall, Kamelhöcker und eine Spirale nach unten.',
    bankFactor: 0.85, bankMax: 55, trainCars: 5,
    points: [
      [-20, 4, 0], [0, 4, 0], [14, 4, 0], [24, 4.6, 0], [40, 12, 0], [56, 20, 0], [70, 27, 0], [80, 30, 0], [88, 30, 0],
      [96, 27.5, 0], [106, 18, 0], [116, 8, 0], [128, 4, 0], [142, 9, 1], [156, 19, 3], [168, 22, 8],
      [176, 19, 18], [176, 13, 30], [166, 9, 40], [152, 12, 44], [138, 16.5, 45], [124, 11, 46], [110, 5, 47], [96, 8, 48], [80, 13, 48.5], [66, 13.4, 49],
      ...helixPts(50, 69, 20, 13.2, 6.4, 1, -Math.PI / 2, -1),
      [30, 5.5, 46], [8, 4.6, 38], [-14, 5, 26], [-38, 5, 13], [-50, 4.4, 3], [-38, 4, 0.2],
    ],
    plan: t => [
      { type: 'drive', from: 0, to: 24, speed: 3.5, accel: 1 },
      { type: 'drive', from: 24, to: t.sAtPoint(7) + 2, speed: 3.5, accel: 1, lift: true },
      { type: 'brake', from: t.length - 62, to: t.length - 24, speed: 4, accel: 3 },
      { type: 'stop', from: t.length - 24, to: t.length, speed: 3.5, accel: 0.8 },
    ],
    tunnels: [],
  },
  {
    id: 'drache', name: 'Looping-Drache', tag: 'Wild', color: 0xe23a4e, accent: 0x7a3dff, supports: 0xa07fd0,
    ground: [0xc2a35a, 0xad8f4e, 0x8c5a4a], trees: [0xd9622b, 0xc9452f, 0xe8a23a], hills: 70, pines: false, snow: false,
    sky: [0x5b4bb8, 0xffb36b], fog: 0xffc58f, sun: 0xffc48a, sunPos: [200, 90, -60],
    blurb: 'Abschuss statt Kettenlift, ein Looping und enge Steilkurven.',
    bankFactor: 0.85, bankMax: 60, trainCars: 4,
    points: [
      [-24, 3, 0], [0, 3, 0], [20, 3, 0], [44, 3, 0], [66, 3, 0], [84, 3, 0],
      [100, 3, 0],
      ...loopPts(104, 0, 7, 3, 23.2),
      [160, 3, 7], [176, 3.6, 7], [190, 8.5, 7.5], [202, 14.5, 9], [211, 16, 17], [208, 13.5, 30], [194, 8, 40], [176, 4.2, 44],
      [156, 8.5, 45], [138, 12.5, 46], [120, 8, 47], [100, 4.5, 48], [82, 8, 48], [66, 11, 48], [50, 6.5, 47], [28, 4.2, 44], [6, 4, 36], [-16, 3.6, 25], [-38, 3.2, 13], [-48, 3, 3.5], [-38, 3, 0.2],
    ],
    plan: t => [
      { type: 'drive', from: 0, to: 20, speed: 3, accel: 1 },
      { type: 'drive', from: 20, to: 92, speed: 23.5, accel: 5, launch: true, felt: true },
      { type: 'brake', from: t.length - 70, to: t.length - 24, speed: 4, accel: 3.2 },
      { type: 'stop', from: t.length - 24, to: t.length, speed: 3.5, accel: 0.8 },
    ],
    tunnels: [],
  },
];
