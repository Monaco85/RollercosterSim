// 3D-Welt: Gelaende, Deko, Schiene, Stuetzen, Station, Zug.
import * as THREE from 'three';
const V = (a) => new THREE.Vector3(a[0], a[1], a[2]);
const mat = (color, o = {}) => new THREE.MeshStandardMaterial({ color, roughness: 0.75, metalness: 0, flatShading: !!o.flat, ...o });
function rng(seed) { let s = seed; return () => (s = (s * 16807) % 2147483647) / 2147483647; }

function tube(track, offR, offU, radius, sides, step, material, from = 0, to = 1) {
  const rings = Math.round(((to - from) * track.length) / step), pos = [], idx = [];
  const closed = from === 0 && to === 1;
  for (let i = 0; i <= rings; i++) {
    const st = track.at(from * track.length + ((to - from) * track.length * i) / rings);
    for (let k = 0; k < sides; k++) {
      const a = (k / sides) * Math.PI * 2, c = Math.cos(a) * radius, s = Math.sin(a) * radius;
      pos.push(st.p[0] + st.R[0] * (offR + c) + st.U[0] * (offU + s), st.p[1] + st.R[1] * (offR + c) + st.U[1] * (offU + s), st.p[2] + st.R[2] * (offR + c) + st.U[2] * (offU + s));
    }
  }
  for (let i = 0; i < rings; i++) for (let k = 0; k < sides; k++) {
    const a = i * sides + k, b = i * sides + (k + 1) % sides, c = a + sides, d = b + sides;
    idx.push(a, c, b, b, c, d);
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setIndex(idx); g.computeVertexNormals();
  const m = new THREE.Mesh(g, material); m.castShadow = true; return m;
}

export function placeBasis(obj, st, lift = 0) {
  const m = new THREE.Matrix4();
  // Modell: +x vorwaerts, +y oben, +z rechts
  m.makeBasis(V(st.T), V(st.U), V(st.R));
  obj.quaternion.setFromRotationMatrix(m);
  obj.position.set(st.p[0] + st.U[0] * lift, st.p[1] + st.U[1] * lift, st.p[2] + st.U[2] * lift);
}

function box(w, h, d, material, x = 0, y = 0, z = 0) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material); m.position.set(x, y, z); m.castShadow = true; return m;
}

export function buildCar(def, i, count) {
  const g = new THREE.Group();
  const body = mat(i % 2 ? def.accent : def.color, { roughness: 0.4 });
  const dark = mat(0x23262e), seat = mat(0x2a2f3c), white = mat(0xffffff), steel = mat(0xb9c2cc, { metalness: 0.6, roughness: 0.35 });
  const shell = new THREE.Mesh(new THREE.CapsuleGeometry(0.72, 1.0, 6, 14), body);
  shell.rotation.z = Math.PI / 2; shell.scale.set(0.72, 1, 1); shell.position.y = 0.55; shell.castShadow = true; g.add(shell);
  g.add(box(1.0, 0.4, 1.1, seat, -0.1, 0.92));                 // Sitzmulde
  g.add(box(0.16, 0.75, 1.1, seat, -0.62, 1.12));              // Lehne
  const bar = new THREE.Mesh(new THREE.TorusGeometry(0.3, 0.03, 6, 16, Math.PI), steel);
  bar.rotation.y = Math.PI / 2; bar.position.set(0.42, 0.86, 0); g.add(bar);
  for (const [x, z] of [[-0.65, -0.55], [-0.65, 0.55], [0.65, -0.55], [0.65, 0.55]]) {
    const w = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.17, 0.12, 12), dark); w.rotation.x = Math.PI / 2; w.position.set(x, 0.17, z); g.add(w);
  }
  if (i === 0) { // Lok mit Gesicht
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.78, 18, 14), body); head.scale.set(1.0, 0.62, 0.86); head.position.set(1.15, 0.48, 0); head.castShadow = true; g.add(head);
    for (const z of [-0.36, 0.36]) {
      const e = new THREE.Mesh(new THREE.SphereGeometry(0.2, 12, 10), white); e.position.set(1.72, 0.7, z); g.add(e);
      const p = new THREE.Mesh(new THREE.SphereGeometry(0.1, 10, 8), dark); p.position.set(1.87, 0.7, z); g.add(p);
    }
    if (def.id === 'raupe') for (const z of [-0.24, 0.24]) { const a = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.025, 0.55, 6), dark); a.position.set(1.42, 1.1, z); a.rotation.x = z * 0.9; g.add(a); const t = new THREE.Mesh(new THREE.SphereGeometry(0.08, 8, 6), mat(def.accent)); t.position.set(1.42, 1.36, z * 1.45); g.add(t); }
    if (def.id === 'drache') for (const z of [-0.34, 0.34]) { const h = new THREE.Mesh(new THREE.ConeGeometry(0.12, 0.6, 8), mat(0xffe08a)); h.position.set(1.25, 0.95, z * 1.3); h.rotation.z = -0.3; g.add(h); }
    if (def.id === 'bergblitz') { const f = new THREE.Mesh(new THREE.ConeGeometry(0.14, 0.7, 4), mat(def.accent)); f.position.set(1.0, 0.98, 0); f.rotation.z = 0.7; g.add(f); }
  }
  return g;
}

export function buildWorld(scene, def, track, seed = 7) {
  const root = new THREE.Group(); scene.add(root);
  const rand = rng(seed + 11);
  const steel = mat(def.color, { roughness: 0.4, metalness: 0.15 }), spine = mat(def.accent, { roughness: 0.5 }), sup = mat(def.supports, { roughness: 0.65 });
  const rs = []; // Strecke im Grundriss, fuer Abstaende
  for (let i = 0; i < track.n; i += 8) rs.push(track.pos[i]);
  const minD = (x, z) => { let m = 1e9; for (const p of rs) { const d = (p[0] - x) ** 2 + (p[2] - z) ** 2; if (d < m) m = d; } return Math.sqrt(m); };
  let cx = 0, cz = 0; for (const p of rs) { cx += p[0]; cz += p[2]; } cx /= rs.length; cz /= rs.length;
  root.userData.center = [cx, cz];
  root.userData.radius = Math.max(...rs.map(p => Math.hypot(p[0] - cx, p[2] - cz)));

  // Schiene
  root.add(tube(track, -0.55, 0, 0.075, 7, 0.6, steel), tube(track, 0.55, 0, 0.075, 7, 0.6, steel), tube(track, 0, -0.34, 0.15, 8, 0.6, spine));
  const tieG = new THREE.BoxGeometry(0.14, 0.09, 1.2), tieN = Math.floor(track.length / 1.1);
  const ties = new THREE.InstancedMesh(tieG, sup, tieN), d = new THREE.Object3D();
  for (let i = 0; i < tieN; i++) { placeBasis(d, track.at(i * 1.1), -0.17); d.updateMatrix(); ties.setMatrixAt(i, d.matrix); }
  ties.castShadow = true; root.add(ties);

  // Stuetzen
  const colG = new THREE.CylinderGeometry(1, 1, 1, 8), cols = [];
  const addCol = (x, yTop, z, r) => { const dd = new THREE.Object3D(); dd.position.set(x, yTop / 2, z); dd.scale.set(r, yTop, r); dd.updateMatrix(); cols.push(dd.matrix.clone()); };
  for (let s = 6; s < track.length; s += 7) {
    const st = track.at(s), y = st.p[1] - 0.4;
    if (st.U[1] < 0.55 || y < 0.6) continue;
    addCol(st.p[0], y, st.p[2], 0.1 + y * 0.012);
    if (y > 7) { addCol(st.p[0] - st.R[0] * 1.8 * (y / 24), y * 0.5, st.p[2] - st.R[2] * 1.8 * (y / 24), 0.09); addCol(st.p[0] + st.R[0] * 1.8 * (y / 24), y * 0.5, st.p[2] + st.R[2] * 1.8 * (y / 24), 0.09); }
  }
  const colI = new THREE.InstancedMesh(colG, sup, cols.length); cols.forEach((m, i) => colI.setMatrixAt(i, m)); colI.castShadow = true; root.add(colI);

  // Tunnel
  for (const [a, b] of def.tunnels || []) {
    const n = Math.round(((b - a) * track.length) / 1.2);
    const rib = new THREE.TorusGeometry(2.6, 0.32, 8, 18, Math.PI);
    const r1 = mat(def.accent, { roughness: 0.7 }), r2 = mat(def.color, { roughness: 0.7 });
    const i1 = new THREE.InstancedMesh(rib, r1, n), i2 = new THREE.InstancedMesh(rib, r2, n);
    for (let i = 0; i < n; i++) {
      const st = track.at((a + ((b - a) * i) / n) * track.length), o = new THREE.Object3D();
      const m = new THREE.Matrix4().makeBasis(V(st.R), V(st.U), V(st.T).negate());
      o.quaternion.setFromRotationMatrix(m); o.position.set(st.p[0], st.p[1] - 0.5, st.p[2]); o.updateMatrix();
      (i % 2 ? i1 : i2).setMatrixAt(i, o.matrix);
    }
    i1.castShadow = true; root.add(i1, i2);
  }

  // Boden
  const size = 1400, seg = 110, gg = new THREE.PlaneGeometry(size, size, seg, seg); gg.rotateX(-Math.PI / 2);
  const cols2 = [], P = gg.attributes.position, gr1 = new THREE.Color(def.ground[0]), gr2 = new THREE.Color(def.ground[1]), mount = new THREE.Color(def.ground[2]), snow = new THREE.Color(0xffffff);
  const ridge = (x, z) => Math.sin(x * 0.011 + 1.3) * Math.cos(z * 0.013) + 0.6 * Math.sin(x * 0.027 - z * 0.019) + 0.3 * Math.sin(z * 0.05 + x * 0.04);
  for (let i = 0; i < P.count; i++) {
    const x = P.getX(i) + cx, z = P.getZ(i) + cz, r = Math.hypot(x - cx, z - cz), far = THREE.MathUtils.smoothstep(r, 240, 560);
    const h = Math.max(0, far * (ridge(x, z) + 1.2) * def.hills);
    P.setXYZ(i, x, h - 0.02, z);
    const c = gr1.clone().lerp(gr2, 0.5 + 0.5 * Math.sin(x * 0.07) * Math.cos(z * 0.09));
    c.lerp(mount, THREE.MathUtils.smoothstep(h, def.hills * 0.25, def.hills * 0.9));
    if (def.snow) c.lerp(snow, THREE.MathUtils.smoothstep(h, def.hills * 1.15, def.hills * 1.6));
    cols2.push(c.r, c.g, c.b);
  }
  gg.setAttribute('color', new THREE.Float32BufferAttribute(cols2, 3)); gg.computeVertexNormals();
  const ground = new THREE.Mesh(gg, new THREE.MeshStandardMaterial({ vertexColors: true, flatShading: true, roughness: 1 })); ground.receiveShadow = true; root.add(ground);

  // See
  const lake = new THREE.Mesh(new THREE.CircleGeometry(38, 40), mat(0x54b9e8, { roughness: 0.15, metalness: 0.2 }));
  lake.rotation.x = -Math.PI / 2; lake.position.set(cx + 8, 0.04, cz + 120); lake.scale.y = 0.6; root.add(lake);

  // Baeume
  const crownG = new THREE.IcosahedronGeometry(1, 1), pineG = new THREE.ConeGeometry(1, 2.6, 7), trunkG = new THREE.CylinderGeometry(0.16, 0.22, 1.6, 6);
  const palette = def.trees.map(c => mat(c, { flat: true, roughness: 0.9 })), N = 300;
  const crowns = palette.map(m => new THREE.InstancedMesh(def.pines ? pineG : crownG, m, N)), trunks = new THREE.InstancedMesh(trunkG, mat(0x7a5a3c), N * palette.length);
  const used = palette.map(() => 0); let tcount = 0; const o = new THREE.Object3D();
  for (let i = 0; i < N * 3; i++) {
    const a = rand() * Math.PI * 2, r = 20 + Math.sqrt(rand()) * 330, x = cx + Math.cos(a) * r, z = cz + Math.sin(a) * r;
    if (minD(x, z) < 11 || Math.hypot(x - lake.position.x, (z - lake.position.z) / 0.6) < 44) continue;
    const k = Math.floor(rand() * palette.length); if (used[k] >= N) continue;
    const sc = 1.6 + rand() * 2.4;
    o.position.set(x, sc * 1.9, z); o.scale.set(sc, sc * (def.pines ? 1.1 : 0.95), sc); o.rotation.set(0, rand() * 6, 0); o.updateMatrix(); crowns[k].setMatrixAt(used[k]++, o.matrix);
    o.position.set(x, sc * 0.5, z); o.scale.set(sc * .7, sc * .7, sc * .7); o.updateMatrix(); trunks.setMatrixAt(tcount++, o.matrix);
  }
  crowns.forEach((c, k) => { c.count = used[k]; c.castShadow = true; root.add(c); }); trunks.count = tcount; root.add(trunks);

  // Wolken
  const cloudM = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 1, flatShading: true });
  const clouds = new THREE.Group();
  for (let i = 0; i < 26; i++) {
    const c = new THREE.Group(), n = 3 + Math.floor(rand() * 3);
    for (let k = 0; k < n; k++) { const b = new THREE.Mesh(new THREE.IcosahedronGeometry(8 + rand() * 9, 1), cloudM); b.position.set(k * 11 - n * 5, rand() * 4, rand() * 8); b.scale.y = 0.55; c.add(b); }
    c.position.set(cx + (rand() - .5) * 900, 70 + rand() * 60, cz + (rand() - .5) * 900); clouds.add(c);
  }
  root.add(clouds);

  // Riesenrad als Wahrzeichen
  const wheel = new THREE.Group(), wc = [cx - 40, 30, cz + 175], wm = mat(0xf3f3f3, { roughness: .5 }), hub = new THREE.Group();
  hub.add(new THREE.Mesh(new THREE.TorusGeometry(24, 0.5, 6, 40), wm));
  const gondelF = [0xff5a5f, 0xffc531, 0x35c46b, 0x2f7bff, 0xa35cff], gondeln = [];
  for (let i = 0; i < 12; i++) {
    const a = (i / 12) * Math.PI * 2, sp = box(48, 0.25, 0.25, wm); sp.rotation.z = a; hub.add(sp);
    const go = new THREE.Mesh(new THREE.BoxGeometry(2.6, 2.4, 2.6), mat(gondelF[i % 5])); go.position.set(Math.cos(a) * 24, Math.sin(a) * 24, 0); go.userData.a = a; hub.add(go); gondeln.push(go);
  }
  wheel.add(hub); wheel.position.set(...wc); wheel.rotation.y = 0.5; root.add(wheel);
  for (const s of [-1, 1]) { const l = box(1.2, 34, 1.2, wm); l.position.set(wc[0] + s * 8 * Math.cos(.5), 15, wc[2] - s * 8 * Math.sin(.5) ); l.rotation.set(0, .5, s * 0.25); root.add(l); }

  // Ballons
  const balloons = new THREE.Group();
  for (let i = 0; i < 7; i++) {
    const b = new THREE.Group(), s = new THREE.Mesh(new THREE.SphereGeometry(7, 16, 12), mat(gondelF[i % 5], { roughness: .5 })); s.scale.y = 1.2; b.add(s);
    b.add(box(1.8, 1.4, 1.8, mat(0x8a5a2b), 0, -11.5, 0));
    b.position.set(cx + (rand() - .5) * 420, 55 + rand() * 60, cz + (rand() - .5) * 420); balloons.add(b);
  }
  root.add(balloons);

  // Station
  const roofA = mat(0xffffff), roofB = mat(def.color), plat = mat(0xcbbfa6), post = mat(0x5b4a3a);
  for (let i = -6; i <= 6; i++) {
    const st = track.at(track.length + i * 2);
    const seg = new THREE.Group(); const hl = Math.hypot(st.T[0], st.T[2]) || 1, fx = st.T[0] / hl, fz = st.T[2] / hl;
    placeBasis(seg, { p: st.p, T: [fx, 0, fz], U: [0, 1, 0], R: [-fz, 0, fx] }, 0);
    seg.position.y = st.p[1];
    seg.add(box(2.02, 0.2, 7.4, i % 2 ? roofA : roofB, 0, 4.7, 0));
    seg.add(box(2.02, 0.5, 2.5, plat, 0, -0.35, 2.6), box(2.02, 0.5, 2.5, plat, 0, -0.35, -2.6));
    if (Math.abs(i) % 3 === 0) seg.add(box(0.22, 4.9, 0.22, post, 0, 2.2, 3.5), box(0.22, 4.9, 0.22, post, 0, 2.2, -3.5));
    root.add(seg);
  }

  root.userData.tick = (t, dt) => {
    hub.rotation.z = t * 0.06;
    gondeln.forEach(g => g.rotation.z = -hub.rotation.z);
    clouds.position.x = Math.sin(t * 0.01) * 40;
    balloons.children.forEach((b, i) => { b.position.y += Math.sin(t * .3 + i) * dt * 0.8; });
  };
  return root;
}

export function disposeTree(o) {
  o.traverse(c => { c.geometry?.dispose?.(); const m = c.material; if (Array.isArray(m)) m.forEach(x => x.dispose()); else m?.dispose?.(); });
  o.parent?.remove(o);
}
