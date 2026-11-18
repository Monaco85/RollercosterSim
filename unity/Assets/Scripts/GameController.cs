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

        class Prepared { public TrackDef def; public Track track; public Ride ride; public double seat; public float peak; }

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
            if (!Application.isEditor || EditorSeatAllowed) bridge.Start();
            else Debug.Log("Achterbahn: Sitz-Verbindung im Editor gesperrt.");
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

        void OnApplicationQuit() { bridge.Stop(); }
        void OnDestroy() { bridge.Stop(); }

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
            return cache[i] = new Prepared { def = def, track = track, ride = ride, seat = seat, peak = peak };
        }

        void Load(int i)
        {
            idx = i; PlayerPrefs.SetInt("bahn", i);
            cues.master = 0; discont++;
            if (world != null) Destroy(world.root);
            cur = Prepare(i);
            cues.wild = cur.def.rough;
            world = WorldBuilder.Build(cur.def, cur.track);
            Look.Apply(cam, sun, cur.def);
            cur.ride.Reset(); haveSeat = false; PlaceTrain();
        }

        // ---------------- Zustaende ----------------
        void SetState(State s) { state = s; stateT = 0; }

        void StartRide()
        {
            cur.ride.Reset(); MotionMap.ResetFilter(); discont++;
            statVmax = statGmax = statAir = 0; wasAir = false; haveSeat = false; PlaceTrain();
            countdownN = 3; cues.target = 1; SetState(State.Count);
        }
        void Pause() { if (state == State.Ride || state == State.Count) { cues.target = 0; SetState(State.Paused); } }
        void Resume() { cues.target = 1; countdownN = 3; SetState(State.Count); }
        void ToMenu() { cues.target = 0; cur.ride.Reset(); discont++; haveSeat = false; PlaceTrain(); SetState(State.Menu); }
        void StartTest() { testI = 0; cues.target = 1; SetState(State.Test); }
        void Primary()
        {
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

        void Physics(double dt)
        {
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
                frame[13] = (float)ride.v; frame[16] = (float)st.p.z; frame[17] = (float)-st.p.x; frame[18] = (float)st.p.y;
                frame[19] = (float)(ride.v * 3.6); frame[20] = (float)(900 + ride.v * 170);
                var g = ride.seg;
                frame[21] = g != null && g.type == Seg.Kind.Drive ? 1 : 0;
                frame[22] = g != null && g.type == Seg.Kind.Brake ? 1 : 0;
                frame[23] = (float)st.gSide; frame[24] = (float)st.gUp; frame[25] = (float)st.gFwd; frame[26] = st.gUp < .25 ? 1 : 0;
                frame[27] = g != null && g.lift ? 1 : 0; frame[28] = g != null && g.launch ? 1 : 0; frame[29] = g != null && g.type == Seg.Kind.Brake ? 1 : 0;
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
                    else if (kb.rightArrowKey.wasPressedThisFrame) Load((idx + 1) % defs.Length);
                    else if (kb.leftArrowKey.wasPressedThisFrame) Load((idx + defs.Length - 1) % defs.Length);
                }
            }
            // Lenkrad- und Gamepad-Tasten: jede Taste = Start/Stopp/Weiter
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
            for (int i = 0; i < defs.Length; i++)
            {
                var p = cache[i]; var d = p.def;
                var r = new Rect(x0 + i * (cw + gap), y0, cw, ch);
                if (i == idx) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), d.accent);
                string txt = $"<size=13>{i + 1} · {d.tag}</size>\n<size=22><b>{d.name}</b></size>\n{d.blurb}\n\n<size=12>{Math.Round(p.track.length)} m lang · {Mathf.Round(p.peak)} m hoch</size>";
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
            if (GUI.Button(new Rect(W - 164, H - 64, 140, 44), "Beenden", sBtn)) Quit();
            if (Time.unscaledTime - quitAsk < 3f)
            {
                var qr = new Rect(W / 2 - 220, H / 2 - 30, 440, 60); Box(qr, new Color(0, 0, 0, .8f));
                GUI.Label(qr, "Nochmal Esc drücken zum Beenden", new GUIStyle(sText) { alignment = TextAnchor.MiddleCenter, fontSize = 22 });
            }
            GUI.Label(new Rect(24, H - 34, W - 200, 30), "Leertaste/Enter/Lenkradtaste Start · C Kamera · 1–5 / Pfeile Bahn · Esc Bahnwahl, 2× Esc Beenden · F11 Fenster/Vollbild", sSmall);
        }

        void DrawHud(float W, float H)
        {
            var ride = cur.ride;
            double g = haveSeat ? seatNow.gUp : 1;
            Box(new Rect(16, 12, 430, 150), new Color(0, 0, 0, .45f));
            GUI.Label(new Rect(30, 18, 400, 40), $"<b>{Math.Round(ride.v * 3.6)}</b> km/h    <b>{F1(g)}</b> G", sTitle);
            Box(new Rect(30, 66, 400, 8), new Color(1, 1, 1, .25f));
            Box(new Rect(30, 66, 400 * Mathf.Clamp01((float)(ride.s / cur.track.length)), 8), cur.def.accent);
            var o = cues.o;
            GUI.Label(new Rect(30, 82, 410, 24), $"Sitz vor/zurück {Math.Round(Math.Abs(o.pitch))}°, Seite {Math.Round(Math.Abs(o.roll))}°", sSmall);
            GUI.Label(new Rect(30, 106, 410, 24), $"{cur.def.name} · {Views[view]} (C)", sSmall);
            var sg = ride.seg;
            string hint = sg != null && sg.lift ? "Kette zieht hoch" : sg != null && sg.launch ? "Abschuss!" : sg != null && sg.type == Seg.Kind.Brake ? "Bremse" : g < .3 ? "Schwerelos!" : "";
            if (hint != "" && state == State.Ride) GUI.Label(new Rect(0, H - 120, W, 50), hint, new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter });
            if (state == State.Ride && GUI.Button(new Rect(W - 170, H - 70, 150, 50), "Pause", sBtn)) Pause();
        }
    }
}
