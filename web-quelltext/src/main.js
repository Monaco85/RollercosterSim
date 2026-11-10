import * as THREE from 'three';
import { Track, v } from './track.js';
import { Ride, autoBank, G } from './physics.js';
import { TRACKS } from './tracks.js';
import { Cues, LEVELS, rawFromSeat } from './cues.js';
import { buildWorld, buildCar, placeBasis, disposeTree } from './world.js';
import { Sound } from './audio.js';
import { Shaker } from './shaker.js';

const $ = id => document.getElementById(id);
const store = { get(k, d) { try { return localStorage.getItem(k) ?? d; } catch { return d; } }, set(k, x) { try { localStorage.setItem(k, x); } catch {} } };
const sound = new Sound(), cues = new Cues();
cues.level = LEVELS[store.get('stufe', 'sanft')] ? store.get('stufe', 'sanft') : 'sanft';

// ---- Renderer ----
const canvas = $('c');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: 'high-performance' });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.05;
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(72, 1, 0.1, 2600);
const hemi = new THREE.HemisphereLight(0xffffff, 0x5a6a40, 1.5); scene.add(hemi);
const sun = new THREE.DirectionalLight(0xffffff, 2.4); sun.castShadow = true; sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, { left: -70, right: 70, top: 70, bottom: -70, near: 10, far: 700 }); sun.shadow.bias = -0.0006; sun.shadow.normalBias = 0.05;
scene.add(sun, sun.target);
const skyU = { top: { value: new THREE.Color() }, bot: { value: new THREE.Color() } };
const sky = new THREE.Mesh(new THREE.SphereGeometry(2000, 24, 16), new THREE.ShaderMaterial({
  side: THREE.BackSide, depthWrite: false, uniforms: skyU, fog: false,
  vertexShader: 'varying vec3 p; void main(){ p = normalize(position); gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.); }',
  fragmentShader: 'varying vec3 p; uniform vec3 top; uniform vec3 bot; void main(){ float h = pow(clamp(p.y*1.3+0.12,0.,1.),0.6); gl_FragColor = vec4(mix(bot,top,h),1.); }',
}));
sky.renderOrder = -1; scene.add(sky);
const sunDisc = new THREE.Mesh(new THREE.SphereGeometry(46, 20, 14), new THREE.MeshBasicMaterial({ color: 0xfff6d0, fog: false })); scene.add(sunDisc);
scene.fog = new THREE.Fog(0xffffff, 200, 1150);
function resize() { const w = innerWidth, h = innerHeight; renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix(); }
addEventListener('resize', resize); resize();

// ---- Bahn laden ----
const cache = {};
function prepare(i) {
  if (cache[i]) return cache[i];
  const def = TRACKS[i], track = new Track(def);
  const cars = def.trainCars, trainLen = cars * 2.4, seat = ((cars - 1) / 2) * 2.4 - 0.1;
  const ride = new Ride(track, def.plan(track), { trainLen, seat });
  autoBank(track, ride, def.bankFactor, def.bankMax);
  return cache[i] = { def, track, ride, seat };
}
let cur = null, world = null, train = [], idx = +store.get('bahn', 0) % TRACKS.length;
const CAR_GAP = 2.4;
function load(i) {
  idx = i; store.set('bahn', i);
  cues.master = 0; discont++;
  if (world) { disposeTree(world); train.forEach(c => disposeTree(c)); }
  cur = prepare(i); const d = cur.def;
  world = buildWorld(scene, d, cur.track);
  train = []; for (let k = 0; k < d.trainCars; k++) { const c = buildCar(d, k, d.trainCars); scene.add(c); train.push(c); }
  skyU.top.value.setHex(d.sky[0]); skyU.bot.value.setHex(d.sky[1]); scene.fog.color.setHex(d.fog);
  sun.color.setHex(d.sun); hemi.color.setHex(d.sun); sunDisc.material.color.setHex(d.sun).lerp(new THREE.Color(0xffffff), .5);
  cur.ride.reset(); placeTrain();
  renderCards();
}
function placeTrain() {
  const { ride, def, track } = cur;
  train.forEach((c, k) => {
    const off = ((def.trainCars - 1) / 2 - k) * CAR_GAP;
    placeBasis(c, track.at(ride.s + off), 0.05);
  });
}

// ---- Zustand ----
let state = 'menu', view = 0, stateT = 0, countdownN = 3, discont = 0, sessionT = 0, yawPrev = null, yawRate = 0;
let stat = { vmax: 0, gmax: 0, air: 0 }, wasAir = false, landing = 0;
const VIEWS = ['Vorne sitzen', 'Hinten sitzen', 'Daneben', 'Von oben'];
function setState(s) {
  state = s; stateT = 0; document.body.dataset.state = s;
  $('menu').hidden = s !== 'menu'; $('pause').hidden = s !== 'paused'; $('done').hidden = s !== 'done'; $('hud').hidden = !['ride', 'paused', 'count'].includes(s);
  $('test').hidden = s !== 'test'; $('count').hidden = s !== 'count';
}
function start() {
  sound.unlock(); shakerUnlock(); cur.ride.reset(); discont++; stat = { vmax: 0, gmax: 0, air: 0 }; placeTrain();
  try { document.documentElement.requestFullscreen?.()?.catch?.(() => {}); } catch {}
  countdownN = 3; cues.target = 1; setState('count'); $('count').textContent = '3'; sound.bell(); sound.beep(false);
}
function pause() { if (state === 'ride' || state === 'count') { cues.target = 0; sound.silence(); shaker?.silence(); pauseFrom = state; setState('paused'); } }
let pauseFrom = 'ride';
function resume() { sound.unlock(); shakerUnlock(); cues.target = 1; countdownN = 3; setState('count'); $('count').textContent = '3'; sound.beep(false); }
function toMenu() { cues.target = 0; sound.silence(); shaker?.silence(); cur.ride.reset(); discont++; placeTrain(); setState('menu'); }
function nextView() { view = (view + 1) % VIEWS.length; $('viewName').textContent = VIEWS[view]; }

// ---- Motion-Test ----
const TEST = [['Sitz neigt sich nach vorn', { pitch: 60 }], ['Sitz neigt sich nach hinten', { pitch: -60 }], ['Sitz neigt sich nach rechts', { roll: 60 }], ['Sitz neigt sich nach links', { roll: -60 }], ['Zurück zur Mitte', {}]];
let testI = 0;
function startTest() { sound.unlock(); testI = 0; cues.target = 1; setState('test'); $('testText').textContent = TEST[0][0]; }

// ---- Bridge ----
let ws = null, wsOk = false, bridge = { simhub: false, sig: false };
const params = new URLSearchParams(location.search);
function connect() {
  if (!/^(localhost|127\.0\.0\.1)$/.test(location.hostname)) { status(); return; }
  try { ws = new WebSocket(`ws://${location.host}/telemetrie`); } catch { return; }
  ws.binaryType = 'arraybuffer';
  ws.onopen = () => { wsOk = true; status(); };
  ws.onclose = () => { wsOk = false; bridge = {}; status(); setTimeout(connect, 1500); };
  ws.onerror = () => {};
  ws.onmessage = e => { try { bridge = JSON.parse(e.data); status(); } catch {} };
}
function status() {
  const el = $('link');
  if (!wsOk) { el.dataset.s = 'off'; el.textContent = 'Vorschau ohne Sitz'; }
  else if (!bridge.sig) { el.dataset.s = 'warn'; el.textContent = 'SimHub-Einrichtung fehlt, siehe Konsolenfenster'; }
  else if (!bridge.simhub) { el.dataset.s = 'warn'; el.textContent = 'SimHub empfängt noch nicht'; }
  else { el.dataset.s = 'on'; el.textContent = 'Sitz verbunden'; }
  $('testBtn').hidden = !wsOk;
}
const frame = new Float32Array(31); let lastSend = 0;
function send(st, out, now) {
  frame.fill(0);
  // Sitz erst freigeben, wenn er ausgeblendet ist, damit SimHub nicht mitten in der Bewegung umschaltet
  const faded = cues.master < 0.02;
  frame[0] = (state === 'menu' || state === 'done') && faded ? 0 : 1;
  frame[1] = state === 'paused' && faded ? 1 : 0;
  frame[2] = discont; frame[3] = sessionT;
  if (st) {
    const { ride } = cur, F = [st.R[2], 0, -st.R[0]];
    const yaw = (Math.atan2(-F[0], F[2]) * 180 / Math.PI + 360) % 360;
    if (yawPrev !== null) { let d = yaw - yawPrev; if (d > 180) d -= 360; if (d < -180) d += 360; yawRate += (d / Math.max(0.004, (now - lastSend) / 1000) - yawRate) * 0.2; }
    yawPrev = yaw; frame[4] = yaw; frame[7] = Math.max(-90, Math.min(90, yawRate));
    frame[13] = ride.v; frame[16] = st.p[2]; frame[17] = -st.p[0]; frame[18] = st.p[1]; frame[19] = ride.v * 3.6;
    frame[20] = 900 + ride.v * 170; frame[21] = ride.seg && ride.seg.type === 'drive' ? 1 : 0;
    frame[22] = ride.seg && ride.seg.type === 'brake' ? 1 : 0;
    frame[23] = st.gSide; frame[24] = st.gUp; frame[25] = st.gFwd; frame[26] = st.gUp < 0.25 ? 1 : 0;
    frame[27] = ride.seg?.lift ? 1 : 0; frame[28] = ride.seg?.launch ? 1 : 0; frame[29] = ride.seg?.type === 'brake' ? 1 : 0; frame[30] = landing;
  }
  frame[5] = out.pitch; frame[6] = out.roll; frame[8] = out.pitchRate; frame[9] = out.rollRate;
  frame[10] = out.surge; frame[11] = out.sway; frame[12] = out.heave;
  if (ws && ws.readyState === 1 && ws.bufferedAmount < 4096) ws.send(frame.buffer);
  landing = 0;
}

// ---- Hauptschleife ----
const clock = new THREE.Clock(); let acc = 0, orbit = 0, camS = new THREE.Vector3(), camInit = false, seatNow = null;
const FIXED = 1 / 240, zero = { pitch: 0, roll: 0, surge: 0, sway: 0, heave: 0 };
const tmp = new THREE.Vector3(), tmp2 = new THREE.Vector3(), upv = new THREE.Vector3();
function physics(dt) {
  const moving = state === 'ride', { ride } = cur;
  acc += dt;
  let raw = zero;
  while (acc >= FIXED) {
    acc -= FIXED;
    if (moving) {
      ride.step(FIXED); sessionT += FIXED;
      seatNow = ride.seatState();
      stat.vmax = Math.max(stat.vmax, ride.v); stat.gmax = Math.max(stat.gmax, seatNow.gUp);
      const air = seatNow.gUp < 0.25 && ride.v > 2;
      if (air) stat.air += FIXED; if (wasAir && !air) landing = 1; wasAir = air;
    } else if (!seatNow) seatNow = ride.seatState();
    if (state === 'test') {
      const k = Math.floor(stateT / 4);
      if (k !== testI) { testI = k; if (k >= TEST.length) { cues.target = 0; setState('menu'); break; } $('testText').textContent = TEST[k][0]; }
      raw = { ...zero, ...TEST[Math.min(k, TEST.length - 1)][1] };
    } else raw = moving ? rawFromSeat(seatNow) : (state === 'count' || state === 'paused') ? rawFromSeat(ride.seatState()) : zero;
    cues.update(raw, FIXED);
  }
  if (state === 'ride' && ride.done) { cues.target = 0; sound.silence(); shaker?.silence(); sound.fanfare(); showDone(); }
  if (ride.seg?.launch && !launched) { launched = true; sound.whoosh(); } if (!ride.seg?.launch) launched = false;
}
let launched = false;
function showDone() {
  $('dV').textContent = Math.round(stat.vmax * 3.6); $('dG').textContent = stat.gmax.toFixed(1).replace('.', ','); $('dT').textContent = stat.air.toFixed(1).replace('.', ',');
  setState('done');
}
function camera3(dt, t) {
  const { ride, track, def } = cur;
  if (state === 'menu' || state === 'done' || state === 'test') {
    const c = world.userData.center; orbit += dt * 0.045;
    const r = world.userData.radius * 1.05; camera.position.set(c[0] + Math.cos(orbit) * r, r * 0.42, c[1] + Math.sin(orbit) * r);
    camera.up.set(0, 1, 0); camera.lookAt(c[0], -r * 0.16, c[1]); camera.fov = 58; camera.updateProjectionMatrix(); camInit = false; return;
  }
  const n = def.trainCars, seat = view === 1 ? -((n - 1) / 2) * CAR_GAP - .1 : cur.seat;
  if (view <= 1) {
    const st = track.at(ride.s + seat);
    const p = tmp.fromArray(st.p).addScaledVector(upv.fromArray(st.U), 1.72).addScaledVector(tmp2.fromArray(st.T), 0.32);
    const ahead = track.at(ride.s + seat + 9);
    camera.position.copy(p); camera.up.copy(upv);
    tmp2.fromArray(ahead.p).addScaledVector(upv.fromArray(ahead.U), 1.2);
    upv.fromArray(st.U); camera.lookAt(tmp2);
    camera.fov = 74 + Math.min(14, ride.v * 0.35);
  } else if (view === 2) {
    const st = track.at(ride.s);
    const want = tmp.fromArray(st.p).add(tmp2.set(st.R[2] * 0 + st.R[0] * 11 - st.T[0] * 9, 5, st.R[2] * 11 - st.T[2] * 9));
    if (!camInit) { camS.copy(want); camInit = true; }
    camS.lerp(want, 1 - Math.exp(-dt * 2.5)); camS.y = Math.max(1.4, camS.y);
    camera.position.copy(camS); camera.up.set(0, 1, 0); camera.lookAt(st.p[0], st.p[1] + 1, st.p[2]); camera.fov = 60;
  } else {
    const st = track.at(ride.s), c = world.userData.center;
    camera.position.set(c[0] + (st.p[0] - c[0]) * 0.5, 74, c[1] + (st.p[2] - c[1]) * 0.5 + 90); camera.up.set(0, 1, 0); camera.lookAt(st.p[0], st.p[1], st.p[2]); camera.fov = 44;
  }
  camera.updateProjectionMatrix();
}
function hud() {
  const { ride, track } = cur;
  $('spd').textContent = Math.round(ride.v * 3.6);
  const g = seatNow ? seatNow.gUp : 1;
  $('gnum').textContent = g.toFixed(1).replace('.', ',');
  $('gbar').style.transform = `scaleX(${Math.max(0.01, Math.min(1, (g + 1) / 5))})`;
  $('prog').style.transform = `scaleX(${Math.min(1, ride.s / track.length)})`;
  const hint = ride.seg?.lift ? 'Kette zieht hoch' : ride.seg?.launch ? 'Abschuss!' : ride.seg?.type === 'brake' ? 'Bremse' : g < 0.3 ? 'Schwerelos!' : '';
  $('cueNow').textContent = `Sitz vor/zurück ${Math.round(Math.abs(cues.out.pitch))}°, Seite ${Math.round(Math.abs(cues.out.roll))}°`;
  $('hint').textContent = hint; $('hint').hidden = !hint;
}
function loop() {
  requestAnimationFrame(loop);
  const dt = Math.min(0.05, clock.getDelta()), t = clock.elapsedTime, nowMs = performance.now();
  stateT += dt;
  if (state === 'count') {
    const n = 3 - Math.floor(stateT);
    if (n !== countdownN) { countdownN = n; if (n > 0) { $('count').textContent = n; sound.beep(false); } else { sound.beep(true); setState('ride'); } }
  }
  physics(dt);
  if (state === 'ride' || state === 'count') sound.update(cur.ride.v, cur.ride.seg, 1, dt);
  if (state === 'ride' && shaker?.active) shaker.update(cur.ride.v, cur.ride.seg, seatNow ? seatNow.gUp : 1, dt);
  placeTrain(); world.userData.tick(t, dt);
  camera3(dt, t); sky.position.copy(camera.position);
  const P = (train[0] || camera).position, d = cur.def;
  sun.target.position.copy(P); sun.position.copy(P).add(tmp.fromArray(d.sunPos));
  sunDisc.position.copy(camera.position).add(tmp.fromArray(d.sunPos).normalize().multiplyScalar(1700));
  if (state !== 'menu') hud();
  if (!params.has('norender')) renderer.render(scene, camera);
  if (nowMs - lastSend >= 8) { send(state === 'ride' ? seatNow : null, cues.out, nowMs); lastSend = nowMs; }
  pollPad();
}

// ---- Eingaben ----
function primary() { if (!$('shakerPanel').hidden) return; if (state === 'menu') start(); else if (state === 'done') start(); else if (state === 'ride' || state === 'count') pause(); else if (state === 'paused') resume(); else if (state === 'test') { cues.target = 0; setState('menu'); } }
addEventListener('keydown', e => {
  if (e.repeat) return;
  if (e.code === 'Space' || e.code === 'Enter') { e.preventDefault(); primary(); }
  else if (e.code === 'Escape') { if (!$('shakerPanel').hidden) $('shakerPanel').hidden = true; else if (state !== 'menu') toMenu(); }
  else if (e.code === 'KeyC') nextView();
  else if (!$('shakerPanel').hidden) return;
  else if (state === 'menu' && ['Digit1', 'Digit2', 'Digit3'].includes(e.code)) load(+e.code.slice(5) - 1);
  else if (state === 'menu' && e.code === 'ArrowRight') load((idx + 1) % TRACKS.length);
  else if (state === 'menu' && e.code === 'ArrowLeft') load((idx + TRACKS.length - 1) % TRACKS.length);
});
const padPrev = {};
function pollPad() {
  for (const p of navigator.getGamepads?.() || []) {
    if (!p) continue;
    p.buttons.forEach((b, i) => { const k = p.index + ':' + i, on = b.pressed && b.value > 0.9; if (on && !padPrev[k]) primary(); padPrev[k] = on; });
  }
}
function renderCards() {
  const box = $('cards'); box.textContent = '';
  TRACKS.forEach((d, i) => {
    const pr = prepare(i), L = pr.track.length;
    const peak = Math.max(...pr.track.pos.map(p => p[1]));
    const b = document.createElement('button'); b.className = 'card'; b.id = 'bahn' + i; b.setAttribute('aria-pressed', i === idx);
    b.style.setProperty('--c', '#' + d.color.toString(16).padStart(6, '0'));
    b.innerHTML = `<span class="tag">${d.tag}</span><strong>${d.name}</strong><span class="blurb">${d.blurb}</span><span class="facts">${Math.round(L)} m lang · ${Math.round(peak)} m hoch</span>`;
    b.onclick = () => { sound.unlock(); if (i !== idx) load(i); };
    box.append(b);
  });
}
function renderLevels() {
  const el = $('levels'); el.textContent = '';
  for (const k of Object.keys(LEVELS)) {
    const b = document.createElement('button'); b.textContent = LEVELS[k].label; b.id = 'stufe-' + k; b.setAttribute('aria-pressed', k === cues.level);
    b.onclick = () => { cues.level = k; store.set('stufe', k); renderLevels(); };
    el.append(b);
  }
}
$('go').onclick = start; $('resume').onclick = resume; $('tomenu').onclick = toMenu; $('again').onclick = start; $('tomenu2').onclick = toMenu;
$('pauseBtn').onclick = pause; $('viewBtn').onclick = nextView; $('testBtn').onclick = startTest; $('testStop').onclick = () => { cues.target = 0; setState('menu'); };
document.addEventListener('visibilitychange', () => { if (document.hidden) pause(); });

// ---- Bass Shaker ----
let shaker = null;
const sh = { on: store.get('shaker', '0') === '1', level: +store.get('shakerStaerke', 60) / 100, swap: store.get('shakerTausch', '0') === '1', sink: store.get('shakerGeraet', '') };
function shakerUnlock() {
  if (!sh.on) return;
  ensureShaker();
  shaker?.ctx.resume?.();
}
function ensureShaker() {
  if (shaker) return shaker;
  const A = window.AudioContext || window.webkitAudioContext; if (!A) return null;
  shaker = new Shaker(new A());
  shaker.level = sh.level; shaker.setSwap(sh.swap); shaker.setActive(sh.on); applySink();
  return shaker;
}
function applySink() {
  const c = shaker?.ctx;
  if (!sh.sink || !c?.setSinkId) return;
  c.setSinkId(sh.sink).catch(() => { $('shakerMsg').textContent = 'Das Gerät ist nicht verfügbar. Der Shaker läuft jetzt auf dem Standardausgang.'; });
}
function shakerUi() {
  $('shakerOn').setAttribute('aria-pressed', sh.on); $('shakerOn').textContent = sh.on ? 'Eingeschaltet' : 'Ausgeschaltet';
  $('shakerLevel').value = Math.round(sh.level * 100); $('shakerPct').textContent = Math.round(sh.level * 100) + ' %';
  $('shakerSwap').setAttribute('aria-pressed', sh.swap);
  $('shakerBtn').textContent = sh.on ? 'Bass Shaker: an' : 'Bass Shaker: aus';
}
async function findDevices() {
  const msg = $('shakerMsg');
  try {
    const s = await navigator.mediaDevices.getUserMedia({ audio: true });   // ohne diese Freigabe nennt der Browser keine Geraetenamen
    s.getTracks().forEach(t => t.stop());
    const list = (await navigator.mediaDevices.enumerateDevices()).filter(d => d.kind === 'audiooutput');
    const el = $('shakerDevice'); el.textContent = '';
    for (const d of list) { const o = document.createElement('option'); o.value = d.deviceId === 'default' ? '' : d.deviceId; o.textContent = d.label || 'Ausgabegerät'; el.append(o); }
    el.value = sh.sink; el.hidden = false; msg.textContent = 'Wähle den Ausgang, an dem der Verstärker der Shaker hängt.';
  } catch { msg.textContent = 'Der Browser gibt die Geräteliste hier nicht frei. Im Windows-Programm klappt die Auswahl. Hier läuft der Shaker auf dem Standardausgang.'; }
}
$('shakerBtn').onclick = () => { sound.unlock(); shakerUi(); $('shakerPanel').hidden = false; };
$('shakerClose').onclick = () => { $('shakerPanel').hidden = true; };
$('shakerOn').onclick = () => { sh.on = !sh.on; store.set('shaker', sh.on ? '1' : '0'); if (sh.on) ensureShaker(); shaker?.setActive(sh.on); shakerUnlock(); shakerUi(); };
$('shakerLevel').oninput = e => { sh.level = +e.target.value / 100; store.set('shakerStaerke', e.target.value); shaker?.setLevel(sh.level); shakerUi(); };
$('shakerSwap').onclick = () => { sh.swap = !sh.swap; store.set('shakerTausch', sh.swap ? '1' : '0'); shaker?.setSwap(sh.swap); shakerUi(); };
$('shakerFind').onclick = findDevices;
$('shakerDevice').onchange = e => { sh.sink = e.target.value; store.set('shakerGeraet', sh.sink); const c = shaker?.ctx; if (c?.setSinkId) c.setSinkId(sh.sink).catch(() => {}); };
const shakerTest = ch => {
  ensureShaker(); if (!shaker) return; shaker.ctx.resume?.();
  shaker.setActive(true);
  for (let i = 0; i < 4; i++) shaker.thump(ch, 0.8, i * 0.32);
  setTimeout(() => shaker.setActive(sh.on || false), 1500);
  $('shakerMsg').textContent = ch === 'front' ? 'Jetzt muss der vordere Shaker viermal klopfen.' : 'Jetzt muss der hintere Shaker viermal klopfen.';
};
$('shakerFront').onclick = () => shakerTest('front');
$('shakerRear').onclick = () => shakerTest('rear');
shakerUi();
if (!window.AudioContext?.prototype.setSinkId) { $('shakerFind').hidden = true; $('shakerNoSink').hidden = false; }

cues.power = Math.max(0.5, Math.min(5, +store.get('kraft', 100) / 100)) || 1;
const powerUi = () => { $('power').value = Math.round(cues.power * 100); $('powerPct').textContent = Math.round(cues.power * 100) + ' %'; };
$('power').oninput = e => { cues.power = +e.target.value / 100; store.set('kraft', e.target.value); powerUi(); };
powerUi();

renderLevels(); load(idx); setState('menu'); status(); connect(); loop();
window.__coaster = { start, get state() { return state; }, get ride() { return cur.ride; }, setS(s) { cur.ride.s = s; cur.ride.v = 8; setState('ride'); view = 0; }, setView(x) { view = x; }, load, frame, Shaker };
