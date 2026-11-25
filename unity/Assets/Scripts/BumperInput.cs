using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Achterbahn
{
    /// <summary>
    /// Eingabe fuers Boxauto: Lenkrad (beliebiges HID-Lenkrad), Pedale, Gamepad oder Tastatur.
    /// Lenkrad wie im echten Autoscooter 1:1 mit dem Antriebsrad gekoppelt: halbe Umdrehung = Rad steht quer, ganze = rueckwaerts.
    /// Pedale werden ohne Einrichtung erkannt: das erste getretene Pedal ist Gas, das zweite Rueckwaerts.
    /// Nur das Lenkrad selbst und Geraete mit Pedal-/Lenkrad-Namen kommen als Pedal in Frage (nie z. B. eine Sitz-Steuerplatine).
    /// </summary>
    public class BumperInput
    {
        public double gas, delta;           // 0..1 bzw. -1..0 (rueckwaerts), Radwinkel in rad
        public float wheelDeg;              // gezeigter Lenkradwinkel (Grad)
        public string device = "", gasName = "", revName = "";
        public float gasShow, revShow;      // fuer die Anzeige in der Bahnwahl

        class Axis { public float rest, last, stable, age, lo, hi; }
        readonly Dictionary<string, Axis> axes = new Dictionary<string, Axis>();
        string gasPath, revPath;
        float kbSteer;

        public static int RangeDeg { get => PlayerPrefs.GetInt("lenkbereich", 900); set => PlayerPrefs.SetInt("lenkbereich", value); }
        public static float Center { get => PlayerPrefs.GetFloat("lenkMitte", 0); set => PlayerPrefs.SetFloat("lenkMitte", value); }
        /// <summary>0 = Echt (Lenkrad 1:1 aufs Antriebsrad), 1 = Leicht (kurbeln: quer erst bei halbem Einschlag, rueckwaerts erst am Anschlag),
        /// 2 = Pedale (Rad dreht hoechstens 75°, vorwaerts mit Gas, rueckwaerts mit der Bremse).</summary>
        public static int Lenkart { get => PlayerPrefs.GetInt("lenkart", 1); set => PlayerPrefs.SetInt("lenkart", value); }
        public static readonly string[] LenkartNamen = { "Echt 1:1", "Leicht", "Gas/Bremse" };
        public const double PedalMaxDeg = 75;

        /// <summary>Lenkradwinkel (Grad) -> Winkel des Antriebsrads (Grad) je nach Lenkart.</summary>
        public static double RadWinkel(double wheelDeg)
        {
            double a = Math.Abs(wheelDeg), s = Math.Sign(wheelDeg), d;
            switch (Lenkart)
            {
                case 0: return wheelDeg;
                case 2: return s * Math.Min(PedalMaxDeg, a * .5);
                default:
                {
                    // Leicht ("kurbeln"): nach dem eingestellten Lenkbereich, H = Anschlag (bei 900° also 450°).
                    //  bis 22 % von H: normal lenken bis 60° (bei 900°: ~100° Lenkrad)
                    //  22–78 %: langsamer Bereich 60°..120°, quer (90°) erst bei halbem Einschlag (225°) -> richtig kurbeln
                    //  78–100 %: 120°..180°, ganz rueckwaerts erst am Anschlag
                    double H = Math.Max(90, RangeDeg * .5), a1 = H * .22, a2 = H * .78;
                    if (a <= a1) d = a / a1 * 60;
                    else if (a <= a2) d = 60 + (a - a1) / (a2 - a1) * 60;
                    else d = 120 + Math.Min(1, (a - a2) / (H - a2)) * 60;
                    return s * d;
                }
            }
        }

        public float rawSteer;   // Lenkachse roh (fuer "Mitte setzen")
        public void SetCenter() => Center = rawSteer;

        bool loaded;
        void LoadPrefs()
        {
            loaded = true;
            gasPath = PlayerPrefs.GetString("gasAchse", ""); revPath = PlayerPrefs.GetString("rueckAchse", "");
            if (gasPath == "") gasPath = null; if (revPath == "") revPath = null;
        }

        public void Relearn()
        {
            loaded = true;
            gasPath = revPath = null; gasName = revName = ""; PlayerPrefs.DeleteKey("gasAchse"); PlayerPrefs.DeleteKey("rueckAchse");
            axes.Clear();
        }

        static readonly string[] WheelWords = { "wheel", "lenkrad", "racing", "g29", "g920", "g923", "g27", "g25", "t300", "t150", "t248", "tmx", "t500", "t-lcm", "fanatec", "csl", "podium", "moza", "simucube", "simagic", "logitech", "thrustmaster", "heusinkveld", "asetek", "cammus", "accuforce" };
        static string Name(InputDevice d) => ((d.description.product ?? "") + " " + (d.description.manufacturer ?? "") + " " + d.displayName).ToLowerInvariant();
        static bool IsPedal(InputDevice d) => Name(d).Contains("pedal");
        // Welches Geraet lenkt: Lenkrad-Basis vor allem anderen, eigene Pedal-, Schalt- oder Handbremsgeraete nie
        static int Score(InputDevice d)
        {
            string s = Name(d);
            if (s.Contains("pedal") || s.Contains("shifter") || s.Contains("handbrake") || s.Contains("handbremse")) return -5;
            int k = 0;
            foreach (var w in WheelWords) if (s.Contains(w)) { k = 1; break; }
            if (s.Contains("wheel") || s.Contains("base") || s.Contains("lenkrad")) k += 2;
            return k;
        }

        /// <summary>Achse beobachten. Die Ruhestellung folgt in der Bahnwahl einem Wert, der 1 s still steht (so stoert ein erster Nullwert vor den echten Daten nicht).</summary>
        Axis Watch(AxisControl a, float dt, bool idle)
        {
            float v = a.ReadValue();
            if (!axes.TryGetValue(a.path, out var x)) { x = new Axis { rest = v, last = v }; axes[a.path] = x; }
            x.age += dt;
            if (Mathf.Abs(v - x.last) < .01f) x.stable += dt; else x.stable = 0;
            x.last = v;
            if (idle && x.stable > 1f && a.path != gasPath && a.path != revPath) x.rest = v;
            float d = v - x.rest; x.lo = Mathf.Min(x.lo, d); x.hi = Mathf.Max(x.hi, d);
            return x;
        }

        /// <summary>Liest alle Geraete. idle = Bahnwahl (Ruhestellung lernen), learn = Pedale duerfen zugeordnet werden.</summary>
        public void Read(float dt, bool idle = false)
        {
            if (!loaded) LoadPrefs();
            double steer = 0, g = 0, rv = 0; bool wheel = false;
            // ---- Lenkrad ----
            Joystick best = null;
            foreach (var d in InputSystem.devices) if (d is Joystick j && Score(j) > 0 && (best == null || Score(j) > Score(best))) best = j;
            AxisControl steerAxis = best != null && best.stick != null ? best.stick.x : null;
            device = best != null ? best.displayName : "";
            if (steerAxis != null)
            {
                var sx = Watch(steerAxis, dt, idle);
                rawSteer = steerAxis.ReadValue();
                steer = Mathf.Clamp(rawSteer - Center, -1f, 1f);
                if (sx.age > .5f && (sx.hi - sx.lo) > .02f) wheel = true;
            }
            // ---- Pedale: Achsen am Lenkrad oder an Pedal-/Lenkrad-Geraeten ----
            gasShow = revShow = 0;
            foreach (var d in InputSystem.devices)
            {
                if (!(d is Joystick)) continue;
                if (d != best && !IsPedal(d) && Score(d) < 1) continue;   // fremde Geraete (Button-Box, Sitz-Platine ...) nie als Pedal
                foreach (var c in d.allControls)
                {
                    if (!(c is AxisControl a) || c is ButtonControl || c.synthetic || c.noisy) continue;
                    if (c is StickControl || c == steerAxis) continue;
                    var x = Watch(a, dt, idle);
                    if (x.lo < -.3f && x.hi > .3f) continue;   // zentrierte Achse (Hebel, zweites Lenkrad) -> kein Pedal
                    float span = Mathf.Abs(x.rest) > .6f ? 2f : 1f;
                    float press = Mathf.Clamp01(Mathf.Abs(x.last - x.rest) / span);
                    bool ripe = x.age > 1.5f;
                    if (ripe && press > .5f && gasPath == null) { gasPath = a.path; PlayerPrefs.SetString("gasAchse", gasPath); }
                    else if (ripe && press > .5f && revPath == null && gasPath != null && a.path != gasPath) { revPath = a.path; PlayerPrefs.SetString("rueckAchse", revPath); }
                    if (a.path == gasPath) { g = Math.Max(g, press); gasName = d.displayName + " " + a.name; }
                    else if (a.path == revPath) { rv = Math.Max(rv, press); revName = d.displayName + " " + a.name; }
                }
            }
            if (g < .04) g = 0; if (rv < .04) rv = 0;   // Totzone
            gasShow = (float)g; revShow = (float)rv;
            double wheelDelta = RadWinkel(steer * RangeDeg * .5) * Math.PI / 180;   // Echt = 1:1 wie beim echten Autoscooter
            double maxPad = Lenkart == 2 ? PedalMaxDeg * Math.PI / 180 : 10;
            // ---- Gamepad ----
            double padSteer = 0, padG = 0, padR = 0; bool pad = false;
            var gp = Gamepad.current;
            if (gp != null)
            {
                padSteer = gp.leftStick.x.ReadValue(); padG = gp.rightTrigger.ReadValue(); padR = gp.leftTrigger.ReadValue();
                if (Math.Abs(padSteer) > .15 || padG > .05 || padR > .05) pad = true;
                if (Math.Abs(padSteer) < .12) padSteer = 0;
            }
            // ---- Tastatur ----
            var kb = Keyboard.current; double kbG = 0, kbR = 0; bool keys = false;
            if (kb != null)
            {
                float want = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
                kbSteer = Mathf.MoveTowards(kbSteer, want, dt * (want == 0 ? 4f : 2.2f));
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) kbG = 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) kbR = 1;
                keys = want != 0 || kbG > 0 || kbR > 0 || kbSteer != 0;
            }

            if (pad && !wheel) { delta = Math.Max(-maxPad, Math.Min(maxPad, padSteer * 1.4)); gas = padG > padR ? padG : -padR; wheelDeg = (float)padSteer * 180f; }
            else if (keys && !wheel && g == 0 && rv == 0) { delta = kbSteer * 1.25; gas = kbG > 0 ? 1 : kbR > 0 ? -.8 : 0; wheelDeg = kbSteer * 160f; }
            else { delta = wheelDelta; gas = g > rv ? g : -rv; wheelDeg = (float)(steer * RangeDeg * .5); }
        }
    }
}
