using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Achterbahn
{
    /// <summary>Spielablauf wie in der HTML-Fassung: Menue, Countdown, Fahrt, Pause, Ende, Sitztest. Sendet den Telemetrie-Frame an die Bruecke.</summary>
    public class GameController : MonoBehaviour
    {
        enum State { Menu, Count, Ride, Paused, Done, Test }

        class Prepared { public TrackDef def; public Track track; public Ride ride; public double seat; public float peak; public PendelRide pendel; public BumperRide bumper; }

        const double FIXED = 1.0 / 240;
        const double CarGap = 2.4;
        static readonly string[] Views = { "Vorne sitzen", "Hinten sitzen", "Daneben", "Von oben" };
        static readonly string[] TestText = { "Sitz neigt sich nach vorn", "Sitz neigt sich nach hinten", "Sitz neigt sich nach rechts", "Sitz neigt sich nach links", "Zurück zur Mitte" };
        static readonly double[,] TestRaw = { { 60, 0 }, { -60, 0 }, { 0, 60 }, { 0, -60 }, { 0, 0 } };

        TrackDef[] defs; Prepared[] cache; int idx;
        Prepared cur; WorldBuilder.World world;
        readonly Cues cues = new Cues();
        readonly BridgeClient bridge = new BridgeClient();
        Camera cam; Light sun;

        State state = State.Menu;
        double stateT, sessionT, acc;
        int countdownN = 3, discont, view, testI, landing;
        double statVmax, statGmax, statAir; bool wasAir;
        SeatState seatNow; bool haveSeat;
        readonly float[] frame = new float[BridgeClient.FrameLen];
        double lastSend; bool haveYaw; double yawPrev, yawRate;
        float orbit; Vector3 camS; bool camInit;

        // ---------------- Start ----------------
        void Start()
        {
            Application.targetFrameRate = 120;
            // EXE immer randlos in der echten Bildschirmaufloesung starten (alte, gespeicherte Groessen ignorieren)
            if (!Application.isEditor) FitScreen(true);
            defs = TrackLibrary.All();
            cache = new Prepared[defs.Length];
            string lv = PlayerPrefs.GetString("stufe", "sanft");
            cues.level = Array.IndexOf(Cues.LevelKeys, lv) >= 0 ? lv : "sanft";
            cues.power = Math.Max(.5, Math.Min(5, PlayerPrefs.GetInt("kraft", 100) / 100.0));

            cam = Camera.main;
            if (cam == null) { var go = new GameObject("Main Camera") { tag = "MainCamera" }; cam = go.AddComponent<Camera>(); go.AddComponent<AudioListener>(); }
            cam.nearClipPlane = .1f; cam.farClipPlane = 2600f;
            sun = FindFirstObjectByType<Light>();
            if (sun == null) { sun = new GameObject("Sonne").AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f); sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;

            for (int i = 0; i < defs.Length; i++) Prepare(i);
            Load(PlayerPrefs.GetInt("bahn", 0) % defs.Length);
            SetState(State.Menu);
            // Im Editor nur nach ausdruecklicher Freigabe (Menue "Achterbahn/Sitz im Editor verbinden"),
            // damit Test- und Entwicklungsfahrten nie unbemerkt den echten Sitz bewegen.
            if (NoSeatArg) Debug.Log("Achterbahn: Start mit -ohnesitz, keine Verbindung zur Bruecke.");
            else if (!Application.isEditor || EditorSeatAllowed) bridge.Start();
            else Debug.Log("Achterbahn: Sitz-Verbindung im Editor gesperrt.");
            if (NoSeatArg) StartCoroutine(TestRun());
        }

        // Testlauf ohne Sitz (nur zusammen mit -ohnesitz): -bahn N, -fahrt, -bilder <Ordner> (Bildschirmfotos, danach Ende)
        System.Collections.IEnumerator TestRun()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string n) { for (int i = 0; i < args.Length - 1; i++) if (string.Equals(args[i].TrimStart('-', '/'), n, StringComparison.OrdinalIgnoreCase)) return args[i + 1]; return null; }
            bool Has(string n) { foreach (var a in args) if (string.Equals(a.TrimStart('-', '/'), n, StringComparison.OrdinalIgnoreCase)) return true; return false; }
            if (int.TryParse(Arg("bahn"), out int b) && b >= 1 && b <= defs.Length) Load(b - 1);
            yield return new WaitForSeconds(3f);
            if (Has("fahrt")) StartRide();
            string dir = Arg("bilder");
            if (string.IsNullOrEmpty(dir)) yield break;
            System.IO.Directory.CreateDirectory(dir);
            for (int i = 1; i <= 8; i++)
            {
                yield return new WaitForSeconds(5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "bild" + i + ".png"));
                Debug.Log($"Testbild {i}: {1f / Time.smoothDeltaTime:0} FPS, {Screen.width}x{Screen.height}");
            }
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        public static float RideSpeed; public static bool OnLift, Riding, Onboard;

        /// <summary>Startschalter "-ohnesitz": EXE verbindet sich nie mit der Bruecke (fuer Tests ohne Sitz).</summary>
        static bool NoSeatArg
        {
            get { foreach (var a in Environment.GetCommandLineArgs()) if (string.Equals(a.TrimStart('-', '/'), "ohnesitz", StringComparison.OrdinalIgnoreCase)) return true; return false; }
        }

        public static bool EditorSeatAllowed
        {
            get { return Application.isEditor && PlayerPrefs.GetInt("editorSitzFreigabe", 0) == 1; }
        }

        static void FitScreen(bool full)
        {
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (full) Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(Mathf.RoundToInt(w * .75f), Mathf.RoundToInt(h * .75f), FullScreenMode.Windowed);
        }

        float quitAsk = -100f;
        void Quit()
        {
            cues.target = 0; bridge.Stop();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationQuit() { bridge.Stop(); net.Stop(); }
        void OnDestroy() { bridge.Stop(); net.Stop(); }

        // Wie die HTML-Fassung beim Verstecken der Seite: Fenster verliert den Fokus -> Pause, Sitz blendet aus.
        void OnApplicationFocus(bool focus) { if (!focus && !Application.isEditor) Pause(); }

        Prepared Prepare(int i)
        {
            if (cache[i] != null) return cache[i];
            var def = defs[i];
            var track = new Track(def);
            int cars = def.trainCars;
            double trainLen = cars * 2.4, seat = (cars - 1) / 2.0 * 2.4 - .1;
            double mu = def.rough ? .016 : .009, cd = .5 * 1.225 * 1.1 * 1.7 / (cars * 580.0);
            var ride = new Ride(track, def.plan(track), trainLen, seat, mu, cd);
            Ride.AutoBank(track, ride, def.bankFactor, def.bankMax);
            float peak = 0; foreach (var p in track.pos) peak = Mathf.Max(peak, (float)p.y);
            PendelRide pendel = null;
            if (def.swing != null)
            {
                var w = def.swing;
                pendel = new PendelRide(w.pivot, w.arm, w.ampDeg, w.tUp, w.tHold, w.tDown, w.seatX, w.seatR);
                peak = (float)(w.pivot.y + w.arm * Math.Sin(w.ampDeg * Math.PI / 180 - Math.PI / 2));
            }
            BumperRide bumper = null;
            if (def.bumper != null) { var b = def.bumper; bumper = new BumperRide(b.cx, b.cz, b.hx, b.hz, b.duration, b.obstacles); peak = 9.4f; }
            return cache[i] = new Prepared { def = def, track = track, ride = ride, seat = seat, peak = peak, pendel = pendel, bumper = bumper };
        }

        void Load(int i)
        {
            idx = i; PlayerPrefs.SetInt("bahn", i);
            if (net.rolle != BumperNet.Rolle.Aus && defs[i].bumper == null) net.Stop();   // Mehrspieler gibt es nur beim Boxauto
            cues.master = 0; discont++;
            if (world != null) Destroy(world.root);
            cur = Prepare(i);
            cues.wild = cur.def.rough;
            Look.Apply(cam, sun, cur.def);   // vor dem Bau, damit die Spiegelung schon den richtigen Himmel sieht
            world = WorldBuilder.Build(cur.def, cur.track);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            if (cam.GetComponent<RideAudio>() == null) cam.gameObject.AddComponent<RideAudio>();
            cur.ride.Reset(); cur.pendel?.Reset(); cur.bumper?.Reset(); haveSeat = false; PlaceTrain();
        }

        // ---------------- Zustaende ----------------
        void SetState(State s) { state = s; stateT = 0; }

        bool netFolgt;   // LAN-Gast setzt gerade den Zustand des Gastgebers um
        bool GastWartet => cur.bumper != null && net.IstGast && net.Verbunden && !netFolgt;
        void StartRide()
        {
            if (GastWartet) { net.Anfrage(); return; }
            cur.ride.Reset(); cur.pendel?.Reset(); cur.bumper?.Reset(); MotionMap.ResetFilter(); discont++;
            statVmax = statGmax = statAir = 0; wasAir = false; haveSeat = false; PlaceTrain();
            countdownN = 3; cues.target = 1; SetState(State.Count);
        }
        void Pause() { if (GastWartet) { net.Anfrage(); return; } if (state == State.Ride || state == State.Count) { cues.target = 0; SetState(State.Paused); } }
        void Resume() { if (GastWartet) { net.Anfrage(); return; } cues.target = 1; countdownN = 3; SetState(State.Count); }
        void ToMenu() { if (GastWartet) net.Stop(); cues.target = 0; cur.ride.Reset(); cur.pendel?.Reset(); cur.bumper?.Reset(); discont++; haveSeat = false; PlaceTrain(); SetState(State.Menu); }
        void StartTest() { testI = 0; cues.target = 1; SetState(State.Test); }
        void Primary()
        {
            // LAN-Gast: Start/Pause entscheidet der Gastgeber, der Wunsch wird nur weitergegeben
            if (cur.bumper != null && net.IstGast && net.Verbunden && state != State.Test) { net.Anfrage(); return; }
            switch (state)
            {
                case State.Menu: case State.Done: StartRide(); break;
                case State.Ride: case State.Count: Pause(); break;
                case State.Paused: Resume(); break;
                case State.Test: cues.target = 0; SetState(State.Menu); break;
            }
        }

        // ---------------- Schleife ----------------
        void Update()
        {
            double dt = Math.Min(.05, Time.unscaledDeltaTime);
            stateT += dt;
            HandleInput();
            if (state == State.Count)
            {
                int n = 3 - (int)Math.Floor(stateT);
                if (n != countdownN) { countdownN = n; if (n <= 0) SetState(State.Ride); }
            }
            Physics(dt);
            PlaceTrain();
            UpdateCamera((float)dt);
            // Nur lesend fuer Fahrgeraeusche (RideAudio); beeinflusst weder Physik noch Sitz
            RideSpeed = (float)(cur.bumper != null ? cur.bumper.Speed : cur.pendel != null ? cur.pendel.Speed : cur.ride.v);
            OnLift = state == State.Ride && cur.pendel == null && cur.bumper == null && cur.ride.seg != null && cur.ride.seg.lift;
            Riding = state == State.Ride; Onboard = view <= 1 && (state == State.Ride || state == State.Count || state == State.Paused);
            double now = Time.realtimeSinceStartupAsDouble * 1000;
            if (now - lastSend >= 8) { SendFrame(state == State.Ride, now); lastSend = now; }
        }

        bool RoughActive()
        {
            if (!cur.def.rough) return false;
            var g = cur.ride.seg;
            if (g == null) return true;
            if (g.lift || g.type == Seg.Kind.Stop) return false;
            if (g.type == Seg.Kind.Drive && !g.launch) return false;
            return true;
        }

        // Schiffsschaukel: gleicher Ablauf wie Physics(), nur mit dem Pendelmodell. Sitz-Filter und Frame bleiben dieselben.
        void PendelPhysics(double dt)
        {
            bool moving = state == State.Ride;
            var pr = cur.pendel;
            acc += dt;
            while (acc >= FIXED)
            {
                acc -= FIXED;
                if (moving)
                {
                    pr.Step(FIXED); sessionT += FIXED;
                    seatNow = pr.SeatAt(0); haveSeat = true;
                    statVmax = Math.Max(statVmax, seatNow.v); statGmax = Math.Max(statGmax, seatNow.gUp);
                    bool air = seatNow.gUp < .25 && seatNow.v > .5;
                    if (air) statAir += FIXED;
                    if (wasAir && !air) landing = 1;
                    wasAir = air;
                }
                else if (!haveSeat) { seatNow = pr.SeatAt(0); haveSeat = true; }

                Raw raw = default;
                if (state == State.Test)
                {
                    int k = (int)Math.Floor(stateT / 4);
                    if (k != testI) { testI = k; if (k >= TestText.Length) { cues.target = 0; SetState(State.Menu); break; } }
                    int j = Math.Min(k, TestText.Length - 1);
                    raw = new Raw { pitch = TestRaw[j, 0], roll = TestRaw[j, 1] };
                }
                else if (moving) raw = MotionMap.Sc(pr.SeatAt(cues.Lead()), FIXED);
                else if (state == State.Count || state == State.Paused) raw = MotionMap.Sc(pr.SeatAt(0), FIXED);
                cues.Update(raw, FIXED);
            }
            if (state == State.Ride && pr.done) { cues.target = 0; SetState(State.Done); }
        }

        // Boxauto: eigenes Fahrmodell, gelenkt vom Spieler. Sitz-Filter und Frame bleiben dieselben.
        readonly BumperInput bin = new BumperInput();
        readonly System.Collections.Generic.Dictionary<int, float> bumpSeen = new System.Collections.Generic.Dictionary<int, float>();
        float shake; double inGas, inDelta, autoStuck, autoBack;
        // Kinder-Modus: Wagen gibt selbst Gas, es wird nur gelenkt; haengt er fest, setzt er kurz zurueck
        static bool AutoGas { get => PlayerPrefs.GetInt("autoGas", 0) == 1; set => PlayerPrefs.SetInt("autoGas", value ? 1 : 0); }
        static bool KiAus { get => PlayerPrefs.GetInt("kiAus", 0) == 1; set => PlayerPrefs.SetInt("kiAus", value ? 1 : 0); }
        // ---- LAN-Mehrspieler ----
        readonly BumperNet net = new BumperNet();
        float netSendT, netWeg;

        /// <summary>LAN-Gast: Zustand des Gastgebers uebernehmen und seinem Ablauf folgen (Bahnwahl, Countdown, Fahrt, Pause, Ende).</summary>
        void NetGastFolgen(BumperRide br, double dt)
        {
            if (!net.Verbunden) { netWeg += (float)dt; if (netWeg > 8f && net.letzter != null) { net.Stop(); ToMenu(); } return; }
            netWeg = 0;
            var s = net.letzter; if (s == null) return;
            if (net.neuerZustand) { BumperNet.Uebernehmen(s, br); net.neuerZustand = false; }
            netFolgt = true;
            switch (s.phase)
            {
                case 0: if (state != State.Menu && state != State.Test) ToMenu(); break;
                case 1: case 2:
                    if (state == State.Menu || state == State.Done) StartRide();
                    else if (state == State.Paused) Resume();
                    if (s.phase == 2 && state == State.Count) SetState(State.Ride);
                    break;
                case 3: if (state == State.Ride || state == State.Count) Pause(); break;
                case 4: if (state == State.Ride || state == State.Count) { cues.target = 0; SetState(State.Done); } break;
            }
            netFolgt = false;
        }

        static byte NetPhase(State s) => s == State.Count ? (byte)1 : s == State.Ride ? (byte)2 : s == State.Paused ? (byte)3 : s == State.Done ? (byte)4 : (byte)0;

        void BumperPhysics(double dt)
        {
            var br = cur.bumper;
            net.Update();
            bool gast = net.IstGast && net.letzter != null;
            br.me = gast ? 1 : 0; br.replica = gast;
            if (gast) NetGastFolgen(br, dt);
            if (net.IstGastgeber && net.GastWillWas() && state != State.Test) Primary();
            bool moving = state == State.Ride;
            bin.Read((float)dt, state == State.Menu);   // immer lesen: in der Bahnwahl lernt es die Ruhestellung der Pedale
            if (moving) { inGas = bin.gas; inDelta = bin.delta; } else { inGas = 0; inDelta = 0; autoStuck = autoBack = 0; }
            if (net.IstGastgeber && br.cars.Length > 1)
            {
                // Wagen 1 faehrt der Mitspieler, solange er verbunden ist; sonst wieder die KI
                bool mit = net.Verbunden; br.cars[1].ai = !mit;
                if (mit) { br.cars[1].gas = moving ? Math.Max(-1, Math.Min(1, net.gastGas)) : 0; br.cars[1].delta = net.gastDelta; }
            }
            // Option „KI-Wagen aus“: die KI-Wagen fahren nicht mit (ein LAN-Mitspieler in Wagen 1 bleibt). Beim Gast kommt das vom Gastgeber.
            if (!br.replica)
                for (int i = 1; i < br.cars.Length; i++)
                    br.cars[i].aus = KiAus && !(i == 1 && net.IstGastgeber && net.Verbunden);
            if (moving && AutoGas)
            {
                if (autoBack > 0) { autoBack -= dt; inGas = -.75; }
                else
                {
                    if (inGas > -.05) inGas = Math.Max(inGas, .85);   // Rueckwaerts-Pedal geht weiterhin
                    autoStuck = br.Speed < .3 ? autoStuck + dt : 0;
                    if (autoStuck > 1.5) { autoStuck = 0; autoBack = 1.0; }
                }
            }
            acc += dt;
            while (acc >= FIXED)
            {
                acc -= FIXED;
                if (moving)
                {
                    br.Step(FIXED, inGas, inDelta); sessionT += FIXED;
                    seatNow = br.SeatAt(0, inGas, inDelta); haveSeat = true;
                    statVmax = Math.Max(statVmax, br.Speed); statGmax = br.hardest;
                }
                else if (state == State.Menu) br.Step(FIXED, 0, 0, true);   // Vorfuehrung in der Bahnwahl, ohne Sitz
                if (!haveSeat && !moving) { seatNow = br.SeatAt(0, 0, 0); haveSeat = true; }

                Raw raw = default;
                if (state == State.Test)
                {
                    int k = (int)Math.Floor(stateT / 4);
                    if (k != testI) { testI = k; if (k >= TestText.Length) { cues.target = 0; SetState(State.Menu); break; } }
                    int j = Math.Min(k, TestText.Length - 1);
                    raw = new Raw { pitch = TestRaw[j, 0], roll = TestRaw[j, 1] };
                }
                else if (moving)
                {
                    // kurze Vorausschau (Kopie, gleiche Eingaben), dazu leichtes Rumpeln der Stahlplatten
                    raw = MotionMap.Sc(br.SeatAt(Math.Min(cues.Lead(), .2), inGas, inDelta), FIXED);
                    raw = MotionMap.Rough(raw, br.cars[br.me].odo * 4, br.Speed * 4, .35);
                }
                else if (state == State.Count || state == State.Paused) raw = MotionMap.Sc(br.SeatAt(0, 0, 0), FIXED);
                cues.Update(raw, FIXED);
            }
            // Rempler: Ton, Kamera-Wackeln, Stoss-Signal im Frame
            float now = Time.unscaledTime;
            foreach (var e in br.events)
            {
                int key = e.a * 8 + e.b + 1;
                if (bumpSeen.TryGetValue(key, out float last) && now - last < .35f) continue;
                bumpSeen[key] = now;
                float s = (float)e.strength;
                FairSound.PlayAt(FairWorld.ThudClip(), new Vector3((float)e.x, 1, (float)e.z), Mathf.Clamp01(.25f + s * .25f), 3, 50, 0, UnityEngine.Random.Range(.85f, 1.15f));
                // Funken an beiden beteiligten Wagen (sichtbarer Index wie in PlaceBumpers; Bande = kein zweiter Wagen)
                if (FairWorld.JoustFx != null && s > .4f)
                    foreach (int ci in new[] { e.a, e.b })
                    {
                        if (ci < 0 || ci >= br.cars.Length) continue;
                        int v = ci == 0 ? br.me : ci == br.me ? 0 : ci;
                        if (v < FairWorld.JoustFx.Length && FairWorld.JoustFx[v] != null) FairWorld.JoustFx[v].Hit(s);
                    }
                if (e.a == br.me || e.b == br.me) { shake = Mathf.Max(shake, Mathf.Clamp01(s / 3f)); if (s > 1) landing = 1; }
            }
            br.events.Clear();
            if (state == State.Ride && br.done) { cues.target = 0; SetState(State.Done); }
            // LAN: Gast schickt seine Eingaben, Gastgeber den Zustand aller Wagen (je 60-mal pro Sekunde)
            netSendT += (float)dt;
            if (netSendT >= 1f / 60f)
            {
                netSendT = 0;
                if (net.IstGast) net.SendeEingabe(inGas, inDelta);
                else if (net.IstGastgeber) net.SendeZustand(NetPhase(state), br);
            }
        }

        void PlaceBumpers()
        {
            var cars = FairWorld.JoustCars; if (cars == null) return;
            var br = cur.bumper;
            for (int i = 0; i < cars.Length && i < br.cars.Length; i++)
            {
                // Sichtbarer Wagen 0 (mit Lenkrad und Kamera) ist immer der eigene; beim LAN-Gast also Wagen 1 der Rechnung
                int v = i == 0 ? br.me : i == br.me ? 0 : i;
                var c = br.cars[i]; if (v >= cars.Length || cars[v] == null) continue;
                if (cars[v].gameObject.activeSelf == c.aus) cars[v].gameObject.SetActive(!c.aus);   // ausgeschaltete KI-Wagen (mit Fahrer) ausblenden
                cars[v].SetPositionAndRotation(new Vector3((float)c.x, FairWorld.JoustFloor, (float)c.z), Quaternion.Euler(0, (float)(c.h * 180 / Math.PI), 0));
                var fx = FairWorld.JoustFx[v];
                if (fx != null)
                {
                    double along = c.ax * c.Fx + c.az * c.Fz, side = c.ax * c.Fz - c.az * c.Fx;
                    fx.speed = (float)c.Speed; fx.bounce = (float)(c.bz * 6); fx.tilt = Mathf.Clamp((float)(along / 9.81), -2, 2); fx.side = Mathf.Clamp((float)(side / 9.81), -2, 2);
                }
            }
            if (FairWorld.JoustWheel != null)
            {
                float deg = state == State.Ride ? bin.wheelDeg : (float)(br.cars[br.me].delta * 180 / Math.PI);
                FairWorld.JoustWheel.localRotation = FairWorld.JoustWheelRest * Quaternion.AngleAxis(deg, FairWorld.JoustWheelAxis);
            }
        }

        void BumperCamera(float dt)
        {
            var t = cam.transform; var br = cur.bumper; var car = FairWorld.JoustCars[0];
            Vector3 c = new Vector3((float)br.cx, 1, (float)br.cz);
            bool menu = state == State.Menu || state == State.Done || state == State.Test;
            // Eigener Fahrer (Autoscooter-Halle) beim Blick vom Fahrersitz ausblenden
            var rider = Achterbahn.Boxauto.BoxautoLeute.Aktiv != null ? Achterbahn.Boxauto.BoxautoLeute.Aktiv.Fahrer(0) : null;
            if (rider != null) rider.enabled = menu || view != 0;
            if (menu && FairWorld.ScooterHall)
            {
                // Bahnwahl: vom Platz vor der Halle auf Neon-Front und Fahrflaeche
                orbit += dt * .1f;
                t.position = new Vector3(c.x + Mathf.Sin(orbit) * 7f, 4.6f, c.z - (float)br.hz - 15f);
                t.LookAt(new Vector3(c.x - Mathf.Sin(orbit) * 2f, 3.0f, c.z - (float)br.hz + 3f), Vector3.up); cam.fieldOfView = 55; camInit = false; return;
            }
            if (menu)
            {
                // Bahnwahl: langsamer Schwenk vom Eingang aus unter dem Zeltdach (der Burghof ist zu eng zum Umkreisen)
                orbit += dt * .12f;
                t.position = new Vector3(c.x + Mathf.Sin(orbit) * 8f, 3.3f, c.z - (float)br.hz + .4f);
                t.LookAt(new Vector3(c.x - Mathf.Sin(orbit) * 3f, .4f, c.z + 2f), Vector3.up); cam.fieldOfView = 64; camInit = false; return;
            }
            shake = Mathf.MoveTowards(shake, 0, dt * 2.2f);
            Vector3 jig = shake > 0 ? new Vector3(Mathf.PerlinNoise(Time.time * 23, 1) - .5f, Mathf.PerlinNoise(Time.time * 27, 5) - .5f, 0) * shake * .12f : Vector3.zero;
            if (view == 0)
            {
                // Fahrersitz: Augenhoehe ueber dem Sitz, Blick nach vorn
                Vector3 eye = car.TransformPoint(FairWorld.JoustEye + jig);
                t.SetPositionAndRotation(eye, car.rotation * Quaternion.Euler(11 + jig.y * 30, jig.x * 20, 0));
                cam.fieldOfView = 82;
            }
            else if (view == 1)
            {
                Vector3 want = car.TransformPoint(new Vector3(0, 2.6f, -5.2f));
                if (!camInit) { camS = want; camInit = true; }
                camS = Vector3.Lerp(camS, want, 1 - Mathf.Exp(-dt * 4f));
                t.position = camS + jig; t.LookAt(car.position + Vector3.up * 1.0f, Vector3.up); cam.fieldOfView = 66;
            }
            else if (view == 2)
            {
                // Zuschauerplatz an der vorderen Bande
                t.position = new Vector3(c.x - 4.5f, FairWorld.JoustFloor + 1.7f, c.z - (float)br.hz - 1.1f);
                t.LookAt(car.position + Vector3.up * .6f, Vector3.up); cam.fieldOfView = 58;
            }
            else
            {
                // Hoch in der Ecke unter dem Dach
                t.position = new Vector3(c.x + (float)br.hx - .4f, FairWorld.ScooterHall ? FairWorld.JoustFloor + 3.3f : 4.0f, c.z + (float)br.hz - .4f);
                t.LookAt(car.position, Vector3.up); cam.fieldOfView = 70;
            }
        }

        void Physics(double dt)
        {
            if (cur.bumper != null) { BumperPhysics(dt); return; }
            if (cur.pendel != null) { PendelPhysics(dt); return; }
            bool moving = state == State.Ride;
            var ride = cur.ride;
            acc += dt;
            while (acc >= FIXED)
            {
                acc -= FIXED;
                if (moving)
                {
                    ride.Step(FIXED); sessionT += FIXED;
                    seatNow = ride.SeatStateAt(0); haveSeat = true;
                    statVmax = Math.Max(statVmax, ride.v); statGmax = Math.Max(statGmax, seatNow.gUp);
                    bool air = seatNow.gUp < .25 && ride.v > 2;
                    if (air) statAir += FIXED;
                    if (wasAir && !air) landing = 1;
                    wasAir = air;
                }
                else if (!haveSeat) { seatNow = ride.SeatStateAt(0); haveSeat = true; }

                Raw raw = default;
                if (state == State.Test)
                {
                    int k = (int)Math.Floor(stateT / 4);
                    if (k != testI) { testI = k; if (k >= TestText.Length) { cues.target = 0; SetState(State.Menu); break; } }
                    int j = Math.Min(k, TestText.Length - 1);
                    raw = new Raw { pitch = TestRaw[j, 0], roll = TestRaw[j, 1] };
                }
                else if (moving)
                {
                    raw = MotionMap.Sc(ride.SeatStateAt(cues.Lead()), FIXED);
                    if (RoughActive()) raw = MotionMap.Rough(raw, ride.s, ride.v, 1);
                }
                else if (state == State.Count || state == State.Paused) raw = MotionMap.Sc(ride.SeatStateAt(0), FIXED);
                cues.Update(raw, FIXED);
            }
            if (state == State.Ride && ride.done) { cues.target = 0; SetState(State.Done); }
        }

        // Frame wie L0() der HTML-Fassung. Reihenfolge und Bedeutung muessen zu buildPacket() in bridge/main.go passen.
        void SendFrame(bool riding, double now)
        {
            Array.Clear(frame, 0, frame.Length);
            bool faded = cues.master < .02;
            frame[0] = (state == State.Menu || state == State.Done) && faded ? 0 : 1;
            frame[1] = state == State.Paused && faded ? 1 : 0;
            frame[2] = discont; frame[3] = (float)sessionT;
            if (riding && haveSeat)
            {
                var st = seatNow; var ride = cur.ride;
                double yaw = (Math.Atan2(-st.R.z, -st.R.x) * 180 / Math.PI + 360) % 360;
                if (haveYaw)
                {
                    double d = yaw - yawPrev; if (d > 180) d -= 360; if (d < -180) d += 360;
                    yawRate += (d / Math.Max(.004, (now - lastSend) / 1000) - yawRate) * .2;
                }
                haveYaw = true; yawPrev = yaw;
                frame[4] = (float)yaw; frame[7] = (float)Math.Max(-90, Math.Min(90, yawRate));
                var pr = cur.pendel;
                var bu = cur.bumper;
                double vNow = pr != null || bu != null ? st.v : ride.v;
                frame[13] = (float)vNow; frame[16] = (float)st.p.z; frame[17] = (float)-st.p.x; frame[18] = (float)st.p.y;
                frame[19] = (float)(vNow * 3.6); frame[20] = (float)(900 + vNow * 170);
                var g = pr != null || bu != null ? null : ride.seg;
                frame[21] = g != null && g.type == Seg.Kind.Drive || pr != null && pr.Driving || bu != null && bu.Driving ? 1 : 0;
                frame[22] = g != null && g.type == Seg.Kind.Brake || pr != null && pr.Braking || bu != null && bu.Braking ? 1 : 0;
                frame[23] = (float)st.gSide; frame[24] = (float)st.gUp; frame[25] = (float)st.gFwd; frame[26] = st.gUp < .25 ? 1 : 0;
                frame[27] = g != null && g.lift ? 1 : 0; frame[28] = g != null && g.launch ? 1 : 0; frame[29] = frame[22];
                frame[30] = landing;
            }
            var o = cues.o;
            frame[5] = (float)o.pitch; frame[6] = (float)o.roll; frame[8] = (float)o.pitchRate; frame[9] = (float)o.rollRate;
            frame[10] = (float)o.surge; frame[11] = (float)o.sway; frame[12] = (float)o.heave;
            if (bridge.Connected) bridge.Send(frame);
            landing = 0;
        }

        // ---------------- Eingaben ----------------
        void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Primary();
                else if (kb.escapeKey.wasPressedThisFrame)
                {
                    // Fahrt -> Bahnwahl; in der Bahnwahl zweimal Esc = Programm beenden
                    if (state != State.Menu) ToMenu();
                    else if (Time.unscaledTime - quitAsk < 3f) Quit();
                    else quitAsk = Time.unscaledTime;
                }
                else if (kb.f11Key.wasPressedThisFrame && !Application.isEditor) FitScreen(Screen.fullScreenMode != FullScreenMode.FullScreenWindow);
                else if (kb.cKey.wasPressedThisFrame) view = (view + 1) % Views.Length;
                else if (state == State.Menu)
                {
                    if (kb.digit1Key.wasPressedThisFrame) Load(0);
                    else if (kb.digit2Key.wasPressedThisFrame) Load(1);
                    else if (kb.digit3Key.wasPressedThisFrame) Load(2);
                    else if (kb.digit4Key.wasPressedThisFrame) Load(3);
                    else if (kb.digit5Key.wasPressedThisFrame && defs.Length > 4) Load(4);
                    else if (kb.digit6Key.wasPressedThisFrame && defs.Length > 5) Load(5);
                    else if (kb.digit7Key.wasPressedThisFrame && defs.Length > 6) Load(6);
                    else if (kb.digit8Key.wasPressedThisFrame && defs.Length > 7) Load(7);
                    else if (kb.digit9Key.wasPressedThisFrame && defs.Length > 8) Load(8);
                    else if (kb.rightArrowKey.wasPressedThisFrame) Load((idx + 1) % defs.Length);
                    else if (kb.leftArrowKey.wasPressedThisFrame) Load((idx + defs.Length - 1) % defs.Length);
                }
            }
            // Lenkrad- und Gamepad-Tasten: jede Taste = Start/Stopp/Weiter (beim Boxauto waehrend der Fahrt nicht, da wird gelenkt)
            if (cur.bumper != null && (state == State.Ride || state == State.Count)) return;
            foreach (var dev in InputSystem.devices)
            {
                if (!(dev is Gamepad) && !(dev is Joystick)) continue;
                foreach (var c in dev.allControls)
                    if (c is ButtonControl b && !b.synthetic && b.wasPressedThisFrame) { Primary(); return; }
            }
        }

        // ---------------- Darstellung ----------------
        static Vector3 M(V3 v) => v.ToUnity();

        void PlaceTrain()
        {
            if (cur == null || world == null) return;
            if (cur.bumper != null) PlaceBumpers();
            if (cur.pendel != null && FairWorld.RideSwing != null)
                FairWorld.RideSwing.localRotation = Quaternion.AngleAxis((float)cur.pendel.AngleDeg, Vector3.forward);
            int n = cur.def.trainCars;
            for (int k = 0; k < world.cars.Length; k++)
            {
                double off = ((n - 1) / 2.0 - k) * CarGap;
                var st = cur.track.At(cur.ride.s + off);
                world.cars[k].SetPositionAndRotation(M(st.p + st.U * .05), Quaternion.LookRotation(M(st.T), M(st.U)));
            }
        }

        void UpdateCamera(float dt)
        {
            var ride = cur.ride; var tr = cur.track; var t = cam.transform;
            if (cur.bumper != null && FairWorld.JoustCars != null) { BumperCamera(dt); return; }
            if (cur.pendel != null && FairWorld.RideSwing != null) { SwingCamera(dt); return; }
            if (state == State.Menu || state == State.Done || state == State.Test)
            {
                orbit += dt * .045f;
                float r = world.radius * 1.05f; Vector3 c = world.center;
                t.position = new Vector3(c.x + Mathf.Cos(orbit) * r, r * .42f, c.z + Mathf.Sin(orbit) * r);
                t.LookAt(new Vector3(c.x, -r * .16f, c.z), Vector3.up);
                cam.fieldOfView = 58; camInit = false; return;
            }
            int n = cur.def.trainCars;
            if (view <= 1)
            {
                double seat = view == 1 ? -((n - 1) / 2.0) * CarGap - .1 : cur.seat;
                var st = tr.At(ride.s + seat);
                Vector3 p = M(st.p + st.U * 1.72 + st.T * .32);
                var ahead = tr.At(ride.s + seat + 9);
                Vector3 look = M(ahead.p + ahead.U * 1.2);
                t.SetPositionAndRotation(p, Quaternion.LookRotation(look - p, M(st.U)));
                cam.fieldOfView = 74 + Mathf.Min(14, (float)ride.v * .35f);
            }
            else if (view == 2)
            {
                var st = tr.At(ride.s);
                Vector3 want = M(st.p + new V3(st.R.x * 11 - st.T.x * 9, 5, st.R.z * 11 - st.T.z * 9));
                if (!camInit) { camS = want; camInit = true; }
                camS = Vector3.Lerp(camS, want, 1 - Mathf.Exp(-dt * 2.5f)); camS.y = Mathf.Max(1.4f, camS.y);
                t.position = camS; t.LookAt(M(st.p) + Vector3.up, Vector3.up); cam.fieldOfView = 60;
            }
            else
            {
                var st = tr.At(ride.s); Vector3 c = world.center, p = M(st.p);
                t.position = new Vector3(c.x + (p.x - c.x) * .5f, 74, c.z + (p.z - c.z) * .5f - 90);
                t.LookAt(p, Vector3.up); cam.fieldOfView = 44;
            }
        }

        void SwingCamera(float dt)
        {
            var t = cam.transform; var piv = FairWorld.RideSwing; Vector3 c = piv.position;
            if (state == State.Menu || state == State.Done || state == State.Test)
            {
                orbit += dt * .06f;
                const float r = 38f;
                t.position = new Vector3(c.x + Mathf.Cos(orbit) * r, 9f, c.z + Mathf.Sin(orbit) * r);
                t.LookAt(c - Vector3.up * 6f, Vector3.up);
                cam.fieldOfView = 58; return;
            }
            float arm = (float)cur.pendel.seatR;
            if (view <= 1)
            {
                // Im Boot: Sitzplatz (vorne sitzen) auf der Heckbank mit Blick zum Bug, hinten sitzen = Bugbank mit Blick zum Heck
                float sx = (float)cur.pendel.seatX, dir = view == 0 ? 1f : -1f;
                Vector3 eye = new Vector3(sx * dir + .2f * dir, -arm + 1.25f, .6f);
                t.SetPositionAndRotation(piv.TransformPoint(eye), Quaternion.LookRotation(piv.TransformDirection(new Vector3(dir, -.1f, 0)), piv.TransformDirection(Vector3.up)));
                cam.fieldOfView = 76;
            }
            else if (view == 2)
            {
                t.position = c + new Vector3(-16f, -10.5f, -21f);
                t.LookAt(piv.TransformPoint(new Vector3(0, -arm, 0)), Vector3.up); cam.fieldOfView = 55;
            }
            else
            {
                t.position = c + new Vector3(0, 22f, -34f);
                t.LookAt(c - Vector3.up * 6f, Vector3.up); cam.fieldOfView = 50;
            }
        }

        // ---------------- Oberflaeche ----------------
        GUIStyle sTitle, sText, sSmall, sBig, sCard, sBtn;
        Texture2D white;

        string LinkText(out Color c)
        {
            if (Application.isEditor && !EditorSeatAllowed) { c = new Color(.8f, .8f, .8f); return "Editor: Sitz gesperrt"; }
            if (!bridge.Connected) { c = new Color(.8f, .8f, .8f); return "Vorschau ohne Sitz"; }
            if (!bridge.Sig) { c = new Color(1f, .75f, .3f); return "SimHub-Einrichtung fehlt, siehe Konsolenfenster"; }
            if (!bridge.Simhub) { c = new Color(1f, .75f, .3f); return "SimHub empfängt noch nicht"; }
            c = new Color(.4f, 1f, .5f); return "Sitz verbunden";
        }

        void Box(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }
        static string F1(double x) => x.ToString("0.0").Replace('.', ',');

        void InitStyles()
        {
            if (sTitle != null) return;
            white = Texture2D.whiteTexture;
            sTitle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, richText = true };
            sText = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, wordWrap = true };
            sSmall = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            sBig = new GUIStyle(GUI.skin.label) { fontSize = 140, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sCard = new GUIStyle(GUI.skin.button) { fontSize = 14, richText = true, wordWrap = true, alignment = TextAnchor.UpperLeft, padding = new RectOffset(14, 14, 12, 12) };
            sBtn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            foreach (var s in new[] { sTitle, sText, sSmall, sBig }) s.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (cur == null) return;
            InitStyles();
            float k = Screen.height / 720f; GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1));
            float W = Screen.width / k, H = 720;

            string link = LinkText(out Color lc);
            var ls = new GUIStyle(sText) { alignment = TextAnchor.UpperRight }; ls.normal.textColor = lc;
            GUI.Label(new Rect(W - 620, 14, 600, 30), link, ls);

            switch (state)
            {
                case State.Menu: DrawMenu(W, H); break;
                case State.Count: DrawHud(W, H); GUI.Label(new Rect(0, 0, W, H), Math.Max(1, countdownN).ToString(), sBig); break;
                case State.Ride: DrawHud(W, H); break;
                case State.Paused:
                    DrawHud(W, H);
                    Panel(W, H, 420, 230, "Pause", "Der Sitz fährt weich in die Mitte.");
                    if (GUI.Button(new Rect(W / 2 - 190, H / 2 + 20, 180, 52), "Weiter", sBtn)) Resume();
                    if (GUI.Button(new Rect(W / 2 + 10, H / 2 + 20, 180, 52), "Zur Bahnwahl", sBtn)) ToMenu();
                    break;
                case State.Done:
                    if (cur.bumper != null)
                        Panel(W, H, 520, 270, "Strom aus!",
                            $"Spitze: {Math.Round(statVmax * 3.6)} km/h\nRempler: {cur.bumper.hits}\nHärtester Stoß: {F1(statGmax)} G");
                    else
                    Panel(W, H, 520, 270, "Geschafft!",
                        $"Spitze: {Math.Round(statVmax * 3.6)} km/h\nMax. Druck: {F1(statGmax)} G\nSchwerelos: {F1(statAir)} s");
                    if (GUI.Button(new Rect(W / 2 - 230, H / 2 + 60, 220, 52), "Nochmal", sBtn)) StartRide();
                    if (GUI.Button(new Rect(W / 2 + 10, H / 2 + 60, 220, 52), "Zur Bahnwahl", sBtn)) ToMenu();
                    break;
                case State.Test:
                    Panel(W, H, 560, 210, "Sitz testen", TestText[Math.Min(testI, TestText.Length - 1)]);
                    if (GUI.Button(new Rect(W / 2 - 110, H / 2 + 30, 220, 52), "Abbrechen", sBtn)) { cues.target = 0; SetState(State.Menu); }
                    break;
            }
        }

        void Panel(float W, float H, float w, float h, string title, string text)
        {
            var r = new Rect(W / 2 - w / 2, H / 2 - h / 2 - 30, w, h);
            Box(r, new Color(0, 0, 0, .65f));
            GUI.Label(new Rect(r.x + 24, r.y + 16, w - 48, 44), title, sTitle);
            GUI.Label(new Rect(r.x + 24, r.y + 70, w - 48, h - 80), text, sText);
        }

        void DrawMenu(float W, float H)
        {
            GUI.Label(new Rect(24, 10, 600, 50), "Achterbahn", sTitle);
            float gap = 12, cw = Mathf.Min(290, (W - 48 - (defs.Length - 1) * gap) / defs.Length), ch = 190, x0 = (W - (defs.Length * cw + (defs.Length - 1) * gap)) / 2, y0 = 80;
            sCard.fontSize = cw < 150 ? 12 : 14; sCard.padding = cw < 150 ? new RectOffset(9, 9, 9, 9) : new RectOffset(14, 14, 12, 12);
            for (int i = 0; i < defs.Length; i++)
            {
                var p = cache[i]; var d = p.def;
                var r = new Rect(x0 + i * (cw + gap), y0, cw, ch);
                if (i == idx) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), d.accent);
                string facts = d.bumper != null ? $"3 Wagen · {d.bumper.duration:0} s · selbst lenken" : d.swing != null ? $"{d.swing.arm:0} m Arm · bis {d.swing.ampDeg:0}° · {Mathf.Round(p.peak)} m hoch" : $"{Math.Round(p.track.length)} m lang · {Mathf.Round(p.peak)} m hoch";
                string txt = $"<size=13>{i + 1} · {d.tag}</size>\n<size={Mathf.Clamp(Mathf.FloorToInt((cw - 30) / (d.name.Length * .56f)), 14, 22)}><b>{d.name}</b></size>\n{d.blurb}\n\n<size=12>{facts}</size>";
                if (GUI.Button(r, txt, sCard) && i != idx) Load(i);
            }

            float y = y0 + ch + 30;
            Box(new Rect(x0, y, W - 2 * x0, 150), new Color(0, 0, 0, .55f));
            GUI.Label(new Rect(x0 + 20, y + 14, 120, 30), "Stärke", sText);
            for (int i = 0; i < Cues.LevelKeys.Length; i++)
            {
                string key = Cues.LevelKeys[i];
                var old = GUI.backgroundColor; if (key == cues.level) GUI.backgroundColor = new Color(.4f, 1f, .5f);
                if (GUI.Button(new Rect(x0 + 140 + i * 150, y + 10, 140, 40), Cues.GetLevel(key).label, sBtn)) { cues.level = key; PlayerPrefs.SetString("stufe", key); }
                GUI.backgroundColor = old;
            }
            int pct = (int)Math.Round(cues.power * 100);
            GUI.Label(new Rect(x0 + 20, y + 66, 200, 30), $"Kraft {pct} %", sText);
            float nv = GUI.HorizontalSlider(new Rect(x0 + 200, y + 76, 400, 24), pct, 50, 500);
            int np = Mathf.RoundToInt(nv / 10f) * 10;
            if (np != pct) { cues.power = np / 100.0; PlayerPrefs.SetInt("kraft", np); }
            GUI.Label(new Rect(x0 + 20, y + 104, W - 2 * x0 - 40, 40), "Kraft wirkt zusätzlich auf jede Stärke. Nach jeder Erhöhung erst ohne Mitfahrer „Sitz testen“.", sSmall);

            if (GUI.Button(new Rect(W / 2 - 160, y + 175, 320, 64), "Los geht's", sBtn)) StartRide();
            if (bridge.Connected && GUI.Button(new Rect(W / 2 + 180, y + 185, 200, 46), "Sitz testen", sBtn)) StartTest();
            if (!bridge.Connected)
                GUI.Label(new Rect(x0, y + 250, W - 2 * x0, 30), "Für den Sitz zuerst „Bruecke fuer Unity starten.bat“ im Achterbahn-Ordner starten. Nicht gleichzeitig das HTML-Spiel offen haben.", sSmall);
            if (cur.bumper != null)
            {
                // Lenkrad wie im echten Autoscooter: 1:1 aufs Antriebsrad, daher den eingestellten Lenkbereich des Rads angeben
                float by = y + (bridge.Connected ? 250 : 280);
                Box(new Rect(x0, by, W - 2 * x0, 52), new Color(0, 0, 0, .55f));
                GUI.Label(new Rect(x0 + 14, by + 12, 120, 30), "Lenkbereich", sText);
                int[] ranges = { 360, 540, 900, 1080 };
                for (int i = 0; i < ranges.Length; i++)
                {
                    var old = GUI.backgroundColor; if (BumperInput.RangeDeg == ranges[i]) GUI.backgroundColor = new Color(.4f, 1f, .5f);
                    if (GUI.Button(new Rect(x0 + 128 + i * 76, by + 8, 70, 36), ranges[i] + "°", sBtn)) BumperInput.RangeDeg = ranges[i];
                    GUI.backgroundColor = old;
                }
                { var old = GUI.backgroundColor; if (AutoGas) GUI.backgroundColor = new Color(.4f, 1f, .5f);
                  if (GUI.Button(new Rect(x0 + 436, by + 8, 170, 36), AutoGas ? "Auto-Gas: An" : "Auto-Gas: Aus", sBtn)) AutoGas = !AutoGas;
                  GUI.backgroundColor = old; }
                if (GUI.Button(new Rect(x0 + 612, by + 8, 84, 36), "Mitte", sBtn)) bin.SetCenter();
                if (GUI.Button(new Rect(x0 + 702, by + 8, 92, 36), "Pedale", sBtn)) bin.Relearn();
                string live = (bin.device != "" ? bin.device : "Kein Lenkrad") + $" · Lenkrad {Mathf.Round(bin.wheelDeg)}° · Gas {Mathf.Round(bin.gasShow * 100)} %" + (bin.revName != "" ? $" · Rückwärts {Mathf.Round(bin.revShow * 100)} %" : "");
                GUI.Label(new Rect(x0 + 804, by + 2, W - 2 * x0 - 812, 52), live + (AutoGas ? "\nAuto-Gas: fährt von selbst, nur lenken (für Kinder)." : "\n„Mitte“: Lenkrad gerade + klicken. „Pedale“: neu lernen."), sSmall);

                // Zweite Zeile: Lenkart und LAN-Mehrspieler
                float ry = by + 54, rw = W - 2 * x0 - 170;
                Box(new Rect(x0, ry, rw, 52), new Color(0, 0, 0, .55f));
                GUI.Label(new Rect(x0 + 14, ry + 12, 120, 30), "Lenkung", sText);
                float[] lw = { 118, 104, 150 }; float bx = x0 + 108;
                for (int i = 0; i < BumperInput.LenkartNamen.Length; i++)
                {
                    var old = GUI.backgroundColor; if (BumperInput.Lenkart == i) GUI.backgroundColor = new Color(.4f, 1f, .5f);
                    if (GUI.Button(new Rect(bx, ry + 8, lw[i], 36), BumperInput.LenkartNamen[i], sBtn)) BumperInput.Lenkart = i;
                    GUI.backgroundColor = old; bx += lw[i] + 6;
                }
                { var old = GUI.backgroundColor; if (!KiAus) GUI.backgroundColor = new Color(.4f, 1f, .5f);
                  if (GUI.Button(new Rect(bx + 6, ry + 8, 140, 36), KiAus ? "KI-Wagen: Aus" : "KI-Wagen: An", sBtn)) KiAus = !KiAus;
                  GUI.backgroundColor = old; bx += 152; }
                float lx = bx + 12;
                GUI.Label(new Rect(lx, ry + 12, 60, 30), "LAN", sText);
                lx += 52;
                if (net.rolle == BumperNet.Rolle.Aus)
                {
                    if (GUI.Button(new Rect(lx, ry + 8, 150, 36), "Spiel öffnen", sBtn)) net.Gastgeben();
                    if (GUI.Button(new Rect(lx + 156, ry + 8, 130, 36), "Beitreten", sBtn)) net.Suchen();
                    GUI.Label(new Rect(lx + 296, ry + 2, rw - (lx - x0) - 300, 52), "Zu zweit an zwei PCs im selben Netz: einer öffnet, der andere tritt bei.", sSmall);
                }
                else
                {
                    if (GUI.Button(new Rect(lx, ry + 8, 110, 36), "Beenden", sBtn)) net.Stop();
                    float tx = lx + 120;
                    if (net.IstGast && net.letzter == null && !net.Verbunden)
                        foreach (var g in net.gefunden)
                        {
                            if (GUI.Button(new Rect(tx, ry + 8, 190, 36), "Zu " + g.name, sBtn)) net.Beitreten(g.ep, g.name);
                            tx += 196; if (tx > x0 + rw - 200) break;
                        }
                    string hinweis = net.IstGastgeber && !net.Verbunden ? "\nBeim ersten Mal die Windows-Firewall-Frage mit „Zulassen“ beantworten."
                        : net.IstGast && net.Verbunden ? "\nDer Gastgeber startet die Fahrt (dein Start-Knopf fragt bei ihm an)." : net.IstGast && net.gefunden.Count == 0 ? "\nAuf dem anderen PC zuerst „Spiel öffnen“." : "";
                    GUI.Label(new Rect(tx, ry + 2, x0 + rw - tx - 8, 52), net.status + hinweis, sSmall);
                }
            }
            if (GUI.Button(new Rect(W - 164, H - 64, 140, 44), "Beenden", sBtn)) Quit();
            if (Time.unscaledTime - quitAsk < 3f)
            {
                var qr = new Rect(W / 2 - 220, H / 2 - 30, 440, 60); Box(qr, new Color(0, 0, 0, .8f));
                GUI.Label(qr, "Nochmal Esc drücken zum Beenden", new GUIStyle(sText) { alignment = TextAnchor.MiddleCenter, fontSize = 22 });
            }
            GUI.Label(new Rect(24, H - 34, W - 200, 30), "Leertaste/Enter/Lenkradtaste Start · C Kamera · 1–9 / Pfeile Bahn · Esc Bahnwahl, 2× Esc Beenden · F11 Fenster/Vollbild", sSmall);
        }

        static readonly string[] BumperViews = { "Fahrersitz", "Hinter dem Wagen", "Vom Rand", "Unterm Dach" };
        void DrawBumperHud(float W, float H)
        {
            var br = cur.bumper;
            double gh = haveSeat ? Math.Sqrt(seatNow.gFwd * seatNow.gFwd + seatNow.gSide * seatNow.gSide) : 0;
            Box(new Rect(16, 12, 430, 176), new Color(0, 0, 0, .45f));
            GUI.Label(new Rect(30, 18, 400, 40), $"<b>{Math.Round(br.Speed * 3.6)}</b> km/h    <b>{br.hits}</b> Rempler", sTitle);
            Box(new Rect(30, 66, 400, 8), new Color(1, 1, 1, .25f));
            Box(new Rect(30, 66, 400 * Mathf.Clamp01((float)(1 - br.time / br.duration)), 8), cur.def.accent);
            var o = cues.o;
            GUI.Label(new Rect(30, 82, 410, 24), $"Stoß {F1(gh)} G · Sitz vor/zurück {Math.Round(Math.Abs(o.pitch))}°, Seite {Math.Round(Math.Abs(o.roll))}°", sSmall);
            GUI.Label(new Rect(30, 106, 410, 24), $"{cur.def.name} · {BumperViews[view]} (C) · noch {Math.Max(0, Math.Ceiling(br.duration - br.time))} s", sSmall);
            string dev = (net.rolle != BumperNet.Rolle.Aus ? "LAN: " + (net.Verbunden ? net.partnerName : "getrennt") + " · " : "") + BumperInput.LenkartNamen[BumperInput.Lenkart] + " · " + (AutoGas ? "Auto-Gas · " : "") + (bin.device != "" ? bin.device : "Tastatur/Gamepad");
            GUI.Label(new Rect(30, 130, 410, 50), $"{dev} · Gas: {(bin.gasName != "" ? bin.gasName : "Pedal treten")}{(bin.revName != "" ? " · Rückwärts: " + bin.revName : "")}", sSmall);
            string hint = state == State.Ride && br.time < 4 && br.Speed < .5 ? "Gas geben!" : br.powerOff && !br.done ? "Strom aus" : shake > .5f ? "Rumms!" : "";
            if (hint != "" && state == State.Ride) GUI.Label(new Rect(0, H - 120, W, 50), hint, new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter });
            if (state == State.Ride && GUI.Button(new Rect(W - 170, H - 70, 150, 50), "Pause", sBtn)) Pause();
        }

        void DrawHud(float W, float H)
        {
            if (cur.bumper != null) { DrawBumperHud(W, H); return; }
            var ride = cur.ride; var pr = cur.pendel;
            double g = haveSeat ? seatNow.gUp : 1;
            double vShow = pr != null ? pr.Speed : ride.v;
            float prog = pr != null ? (float)(pr.time / pr.Total) : (float)(ride.s / cur.track.length);
            Box(new Rect(16, 12, 430, 150), new Color(0, 0, 0, .45f));
            GUI.Label(new Rect(30, 18, 400, 40), $"<b>{Math.Round(vShow * 3.6)}</b> km/h    <b>{F1(g)}</b> G", sTitle);
            Box(new Rect(30, 66, 400, 8), new Color(1, 1, 1, .25f));
            Box(new Rect(30, 66, 400 * Mathf.Clamp01(prog), 8), cur.def.accent);
            var o = cues.o;
            GUI.Label(new Rect(30, 82, 410, 24), $"Sitz vor/zurück {Math.Round(Math.Abs(o.pitch))}°, Seite {Math.Round(Math.Abs(o.roll))}°", sSmall);
            GUI.Label(new Rect(30, 106, 410, 24), $"{cur.def.name} · {Views[view]} (C)", sSmall);
            var sg = ride.seg;
            if (pr != null) sg = null;
            string hint = pr != null && pr.time < pr.tUp ? "Schwung holen …" : pr != null && pr.time > pr.tUp + pr.tHold ? "Bremse" : sg != null && sg.lift ? "Kette zieht hoch" : sg != null && sg.launch ? "Abschuss!" : sg != null && sg.type == Seg.Kind.Brake ? "Bremse" : g < .3 ? "Schwerelos!" : "";
            if (hint != "" && state == State.Ride) GUI.Label(new Rect(0, H - 120, W, 50), hint, new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter });
            if (state == State.Ride && GUI.Button(new Rect(W - 170, H - 70, 150, 50), "Pause", sBtn)) Pause();
        }
    }
}
