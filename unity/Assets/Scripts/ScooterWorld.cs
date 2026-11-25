using UnityEngine;
using Achterbahn.Boxauto;

namespace Achterbahn
{
    /// <summary>
    /// Boxauto (Bahn 9) in der Autoscooter-Halle bei Nacht: setzt die vorgefertigte Halle (Resources/Boxautobahn_Spiel, aus
    /// "Achterbahn/Boxauto-Asset erzeugen") auf die Fahrflaeche des Fahrmodells und stellt die drei Wagen hinein.
    /// Nur Optik und Ton – BumperRide, Sitz und SimHub-Frame bleiben unberuehrt.
    /// </summary>
    public static partial class FairWorld
    {
        static readonly Color[] ScooterLack = { new Color(.62f, .02f, .04f), new Color(.03f, .12f, .55f), new Color(.85f, .55f, .02f) };
        static readonly int[] ScooterNr = { 7, 3, 5 };

        /// <summary>Baut die Halle. false = Prefab fehlt (dann baut der Aufrufer die alte Kulisse).</summary>
        public static bool Scooter(TrackDef def, Transform parent)
        {
            var pf = Resources.Load<GameObject>("Boxautobahn_Spiel");
            if (pf == null) { Debug.LogWarning("Boxauto: Resources/Boxautobahn_Spiel fehlt – Menü \"Achterbahn/Boxauto-Asset erzeugen\" ausführen"); return false; }
            CrowdAnim.ReleaseAll(); FairPerson.blockers.Clear();
            var bd = def.bumper;
            JoustFloor = .5f;   // Podest: der Platz ringsum liegt auf Bodenhoehe
            var hall = Object.Instantiate(pf, parent, false); hall.name = "Autoscooter-Halle";
            hall.transform.localPosition = new Vector3((float)bd.cx, JoustFloor, (float)bd.cz);
            var hc = hall.GetComponent<BoxautoHalle>();

            JoustCars = new Transform[3]; JoustFx = new JoustCarFx[3]; JoustWheel = null;
            for (int i = 0; i < 3; i++)
            {
                var car = Object.Instantiate(hc.wagenPrefab, hall.transform, false).transform;
                car.name = i == 0 ? "Boxauto Spieler" : "Boxauto " + (i + 1);
                var fa = car.gameObject.AddComponent<BoxautoFarbe>(); fa.lack = ScooterLack[i]; fa.nummer = hc.Nummer(ScooterNr[i]); fa.Anwenden();
                var li = car.GetComponent<BoxautoLichter>();
                var fx = car.gameObject.AddComponent<JoustCarFx>(); fx.body = car.Find("Aufbau");
                if (li != null)
                {
                    li.lenkradWackeln = false; li.funkenProSekunde = 0;   // Funken kommen vom Fahren und von Remplern
                    fx.sparks = li.funken; var l = li; fx.funke = n => l.Funke(n);
                    if (i == 0 && li.lenkrad != null) { JoustWheel = li.lenkrad; JoustWheelRest = li.lenkrad.localRotation; JoustWheelAxis = Vector3.back; }
                }
                if (i == 0) car.gameObject.AddComponent<JoustHum>();
                JoustCars[i] = car; JoustFx[i] = fx;
            }
            JoustEye = new Vector3(0, 1.22f, -.36f);

            var leute = new GameObject("Menschen").AddComponent<BoxautoLeute>();
            leute.transform.SetParent(hall.transform, false);
            leute.wagen = JoustCars; leute.halb = hc.halb; leute.steg = hc.steg; leute.platz = hc.platz;
            ScooterHall = true;
            return true;
        }
    }
}
