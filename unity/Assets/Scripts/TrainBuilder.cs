using UnityEngine;

namespace Achterbahn
{
    /// <summary>
    /// Runde Wagen: bootsfoermige Wanne mit eingezogenem Polsterrand, gepolsterte Innenseite, weiche Sitze mit Kopfstuetzen,
    /// gebogene Schossbuegel, Fahrwerk mit runden Raedern. Bug je Thema: Stromlinie (Stahl), Holz mit Wappen, Dinokopf.
    /// Lokale Achsen: z = Fahrtrichtung, y = oben, Ursprung knapp ueber der Schiene.
    /// </summary>
    public static class TrainBuilder
    {
        static Material Lit(Color c, float smooth, float metal)
        {
            var b = Resources.Load<Material>("AchterbahnLit");
            var m = b != null ? new Material(b) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            return m;
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Vector2 R(float w, float h) => new Vector2(w, h);

        // Wannenprofil entlang z: Mitte y, Halbbreite, Halbhoehe
        static readonly float[] Z = { -1.16f, -1.0f, -.6f, 0f, .6f, 1.0f, 1.14f };
        static readonly float[] W = { .1f, .6f, .78f, .8f, .78f, .62f, .12f };
        static readonly float[] H = { .1f, .46f, .55f, .56f, .55f, .47f, .12f };
        const float CY = .76f, LIP = 128f;   // Mittelhoehe, Bogen ab Unterseite je Seite (Grad)

        public static Transform Car(TrackDef def, int index, Transform parent, ThemeAssets ta)
        {
            if (def.theme == "rummel") return Kleinwagen(index, parent);
            bool dino = def.theme == "dino", wood = def.theme == "mittelalter" || def.theme == "holz";
            var car = new GameObject("Wagen " + (index + 1)).transform;
            car.SetParent(parent, false);

            var shell = new MeshBuilder { tile = 1.2f };   // Aussenhaut
            var pad = new MeshBuilder { tile = .6f };      // Polster: Innenseite, Rand, Sitze
            var trim = new MeshBuilder();                  // Metall
            var dark = new MeshBuilder();                  // Fahrwerk
            var bone = new MeshBuilder(); var eyes = new MeshBuilder(); var pink = new MeshBuilder();
            bool mouse = def.theme == "rummel";

            // --- Wanne aussen und innen ---
            int nZ = Z.Length;
            var cO = new Vector3[nZ]; var rO = new Vector2[nZ]; var cI = new Vector3[nZ]; var rI = new Vector2[nZ];
            for (int i = 0; i < nZ; i++)
            {
                cO[i] = V(0, CY, Z[i]); rO[i] = R(W[i], H[i]);
                cI[i] = V(0, CY + .02f, Z[i] * .97f); rI[i] = R(Mathf.Max(.05f, W[i] - .06f), Mathf.Max(.05f, H[i] - .06f));
            }
            shell.LoftArc(cO, rO, LIP, 6, 22);
            pad.LoftArc(cI, rI, LIP - 4, 6, 20, true);
            // Polsterrand oben an beiden Seiten
            float la = (LIP - 90f) * Mathf.Deg2Rad;
            foreach (float s in new[] { -1f, 1f })
            {
                var rp = new Vector3[nZ - 2]; var rr = new Vector2[nZ - 2];
                for (int i = 1; i < nZ - 1; i++) { rp[i - 1] = V(s * W[i] * Mathf.Cos(la), CY + H[i] * Mathf.Sin(la), Z[i]); rr[i - 1] = R(.055f, .055f); }
                pad.Loft(rp, rr, 5, 10, true, true);
            }
            pad.Loft(new[] { V(-.5f, CY + .32f, -1.0f), V(0, CY + .38f, -1.04f), V(.5f, CY + .32f, -1.0f) }, new[] { R(.05f, .05f), R(.05f, .05f), R(.05f, .05f) }, 4, 10, true, true);

            // --- Sitze: Kissen, Lehne, Kopfstuetzen ---
            foreach (float z in new[] { .4f, -.48f })
            {
                pad.Loft(new[] { V(-.58f, .56f, z), V(.58f, .56f, z) }, new[] { R(.2f, .07f), R(.2f, .07f) }, 3, 12, true, true);
                pad.Loft(new[] { V(-.58f, .92f, z - .25f), V(0, .93f, z - .27f), V(.58f, .92f, z - .25f) }, new[] { R(.07f, .3f), R(.08f, .31f), R(.07f, .3f) }, 4, 12, true, true);
                foreach (float x in new[] { -.31f, .31f })
                    pad.Loft(new[] { V(x - .15f, 1.33f, z - .3f), V(x + .15f, 1.33f, z - .3f) }, new[] { R(.08f, .14f), R(.08f, .14f) }, 3, 12, true, true);
                // Schossbuegel als gebogenes Rohr mit Polster
                float bz = z + .2f;
                trim.Loft(new[] { V(-.62f, .6f, bz - .04f), V(-.6f, .84f, bz), V(-.32f, .93f, bz + .04f), V(.32f, .93f, bz + .04f), V(.6f, .84f, bz), V(.62f, .6f, bz - .04f) },
                          new[] { R(.032f, .032f), R(.032f, .032f), R(.032f, .032f), R(.032f, .032f), R(.032f, .032f), R(.032f, .032f) }, 5, 8, true, true);
                pad.Loft(new[] { V(-.3f, .93f, bz + .04f), V(.3f, .93f, bz + .04f) }, new[] { R(.07f, .06f), R(.07f, .06f) }, 3, 10, true, true);
            }

            // --- Fahrwerk: Rahmen, Achsen, runde Raeder ---
            dark.Box(V(0, .28f, 0), Quaternion.identity, new Vector3(1.0f, .12f, 1.9f));
            foreach (float z in new[] { -.72f, .72f })
            {
                trim.Loft(new[] { V(-.66f, .16f, z), V(.66f, .16f, z) }, new[] { R(.045f, .045f), R(.045f, .045f) }, 2, 8, true, true);
                foreach (float x in new[] { -.55f, .55f })
                {
                    dark.Box(V(x, .2f, z), Quaternion.identity, new Vector3(.14f, .22f, .5f));
                    dark.Loft(new[] { V(x - .07f, .17f, z), V(x + .07f, .17f, z) }, new[] { R(.15f, .15f), R(.15f, .15f) }, 2, 16, true, true);      // Laufrad
                    dark.Loft(new[] { V(x * 1.14f - .04f, .0f, z), V(x * 1.14f + .04f, .0f, z) }, new[] { R(.07f, .07f), R(.07f, .07f) }, 2, 12, true, true);  // Fuehrungsrolle
                }
            }
            trim.Loft(new[] { V(0, .32f, -1.0f), V(0, .32f, -1.32f) }, new[] { R(.05f, .05f), R(.05f, .05f) }, 2, 8, true, true);   // Kupplung

            // --- Bug am ersten Wagen ---
            if (index == 0)
            {
                if (dino)
                {
                    shell.Loft(new[] { V(0, .62f, 1.0f), V(0, .72f, 1.7f), V(0, .68f, 2.35f), V(0, .58f, 2.95f) },
                               new[] { R(.6f, .3f), R(.55f, .3f), R(.42f, .22f), R(.22f, .13f) }, 5, 16, false, true);
                    shell.Loft(new[] { V(0, .4f, 1.5f), V(0, .4f, 2.3f), V(0, .42f, 2.8f) }, new[] { R(.48f, .12f), R(.34f, .09f), R(.18f, .06f) }, 4, 12, true, true);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        eyes.Sphere(V(s * .45f, .88f, 1.8f), .08f, 12);
                        bone.Spike(V(s * .28f, .96f, 1.65f), V(s * .35f, .8f, -.5f), .07f, .3f, 8);
                        for (float z = 1.85f; z < 2.9f; z += .13f)
                        {
                            float w = Mathf.Lerp(.4f, .18f, (z - 1.85f) / 1.05f);
                            bone.Spike(V(s * (w - .03f), .49f, z), Vector3.down, .026f, .11f, 5);
                            bone.Spike(V(s * (w - .06f), .46f, z - .05f), Vector3.up, .022f, .08f, 5);
                        }
                    }
                }
                else
                {
                    shell.Loft(new[] { V(0, .7f, 1.0f), V(0, .66f, 1.4f), V(0, .58f, 1.72f), V(0, .52f, 1.9f) },
                               new[] { R(.66f, .4f), R(.56f, .33f), R(.36f, .2f), R(.08f, .06f) }, 6, 18, false, true);
                    if (wood) trim.Loft(new[] { V(0, .8f, 1.42f), V(0, .82f, 1.47f) }, new[] { R(.22f, .26f), R(.22f, .26f) }, 1, 16, true, true);   // Wappenscheibe
                    else foreach (float s in new[] { -1f, 1f }) eyes.Sphere(V(s * .36f, .66f, 1.55f), .07f, 12);                                       // Scheinwerfer
                    if (mouse)
                    {
                        // Mausgesicht: runde Ohren, Knopfnase, Schnurrhaare
                        foreach (float s in new[] { -1f, 1f })
                        {
                            trim.Loft(new[] { V(s * .5f, 1.08f, 1.2f), V(s * .5f, 1.08f, 1.26f) }, new[] { R(.24f, .24f), R(.24f, .24f) }, 1, 18, true, true);
                            pink.Loft(new[] { V(s * .5f, 1.08f, 1.265f), V(s * .5f, 1.08f, 1.275f) }, new[] { R(.15f, .15f), R(.15f, .15f) }, 1, 18, true, true);
                            for (int k = -1; k <= 1; k++)
                                dark.Loft(new[] { V(s * .08f, .56f, 1.88f), V(s * .5f, .56f + k * .07f, 1.8f) }, new[] { R(.008f, .008f), R(.004f, .004f) }, 1, 4, false, false);
                        }
                        pink.Sphere(V(0, .55f, 1.93f), .085f, 12);
                    }
                }
            }
            if (dino)
                foreach (float s in new[] { -1f, 1f })
                    for (float z = -.95f; z <= -.15f; z += .4f)
                        bone.Spike(V(s * .62f, 1.12f, z), V(s * .2f, 1f, -.25f), .13f, .24f, 6, .3f, Vector3.right);   // Rueckenplatten

            // --- Materialien ---
            Material shellMat, trimMat, padMat = Lit(new Color(.1f, .085f, .075f), .38f, 0), darkMat = Lit(new Color(.08f, .08f, .09f), .45f, .5f);
            if (dino && ta != null && ta.dinoSkin != null)
            {
                shellMat = new Material(ta.dinoSkin); shellMat.SetColor("_BaseColor", index % 2 == 0 ? new Color(.46f, .56f, .3f) : new Color(.4f, .5f, .28f));
                trimMat = Lit(new Color(.32f, .26f, .16f), .5f, .75f);
            }
            else if (wood && ta != null && ta.woodDeck != null)
            {
                shellMat = new Material(ta.woodDeck); shellMat.SetColor("_BaseColor", Color.Lerp(Color.white, def.accent, .5f)); shellMat.SetFloat("_Smoothness", .4f);
                trimMat = Lit(new Color(.36f, .28f, .15f), .5f, .85f);
            }
            else
            {
                shellMat = Lit(index % 2 == 0 ? def.accent : def.rail, .82f, .3f);
                trimMat = Lit(new Color(.78f, .79f, .8f), .75f, .95f);
            }
            shell.ToObject("Wanne", shellMat, car);
            pad.ToObject("Polster", padMat, car);
            trim.ToObject("Beschlaege", trimMat, car);
            dark.ToObject("Fahrwerk", darkMat, car);
            if (pink.Count > 0) pink.ToObject("Mausnase", Lit(new Color(.95f, .5f, .62f), .6f, 0), car);
            if (dino) bone.ToObject("Platten und Zaehne", Lit(new Color(.84f, .8f, .66f), .4f, 0), car);
            if (eyes.Count > 0) eyes.ToObject(dino ? "Augen" : "Lichter", dino ? Lit(new Color(.9f, .55f, .05f), .95f, 0) : Lit(new Color(1f, .97f, .85f), .95f, 0), car);
            return car;
        }

        /// <summary>
        /// Wilde-Maus-Wagen im Stil eines kantigen italienischen Kleinwagens der 90er (Cinquecento Sporting):
        /// offen ohne Dach, Rechteckscheinwerfer, schwarze Stossfaenger, Seitenstreifen, kleine Alufelgen. Ohne Markenzeichen.
        /// </summary>
        static Transform Kleinwagen(int index, Transform parent)
        {
            var car = new GameObject("Wagen " + (index + 1)).transform;
            car.SetParent(parent, false);
            var body = new MeshBuilder(); var black = new MeshBuilder(); var chrome = new MeshBuilder(); var pad = new MeshBuilder();
            var glassL = new MeshBuilder(); var red = new MeshBuilder(); var amber = new MeshBuilder(); var stripe = new MeshBuilder();
            var Q = Quaternion.identity;
            const float W = 1.5f, L = 3.2f, hw = W / 2;

            // Wanne: Boden, Seitenwaende, Motorhaube, Front, Heck
            body.Box(V(0, .42f, 0), Q, V(W - .06f, .12f, L - .1f));
            foreach (float s in new[] { -1f, 1f })
            {
                body.Box(V(s * (hw - .04f), .72f, -.25f), Q, V(.08f, .66f, L - .6f));
                stripe.Box(V(s * (hw + .002f), .6f, -.05f), Q, V(.01f, .07f, L - .5f));                       // Sporting-Streifen
                black.Box(V(s * (hw + .01f), .45f, 0), Q, V(.04f, .16f, 1.5f));                               // Schweller
                black.Box(V(s * (hw + .02f), 1.08f, .62f), Q, V(.06f, .09f, .14f));                           // Spiegel
                black.Box(V(s * (hw + .07f), 1.1f, .56f), Q, V(.08f, .14f, .2f));
            }
            body.Box(V(0, .72f, 1.12f), Quaternion.Euler(7, 0, 0), V(W - .04f, .14f, .95f));                 // Motorhaube, flach geneigt
            body.Box(V(0, .6f, 1.57f), Q, V(W - .04f, .36f, .1f));                                            // Front
            body.Box(V(0, .78f, -1.47f), Q, V(W - .04f, .74f, .26f));                                         // Heck
            body.Box(V(0, 1.13f, -1.42f), Quaternion.Euler(-30, 0, 0), V(W - .1f, .05f, .34f));               // Heckklappe-Ansatz
            black.Box(V(0, 1.2f, -1.56f), Quaternion.Euler(-10, 0, 0), V(W - .2f, .04f, .22f));               // Dachspoiler-Andeutung

            // Stossfaenger und Front
            black.Box(V(0, .48f, 1.64f), Q, V(W + .04f, .2f, .14f));
            black.Box(V(0, .5f, -1.63f), Q, V(W + .04f, .2f, .14f));
            black.Box(V(0, .78f, 1.625f), Q, V(.5f, .1f, .03f));                                              // Grill-Schlitz
            foreach (float s in new[] { -1f, 1f })
            {
                glassL.Box(V(s * .48f, .8f, 1.625f), Q, V(.4f, .17f, .03f));                                  // Rechteckscheinwerfer
                amber.Box(V(s * .7f, .8f, 1.62f), Q, V(.06f, .14f, .04f));                                    // Blinker
                glassL.Box(V(s * .45f, .5f, 1.715f), Q, V(.12f, .07f, .02f));                                 // Nebelscheinwerfer
                red.Box(V(s * .54f, .9f, -1.605f), Q, V(.3f, .22f, .04f));                                    // Ruecklichter
                amber.Box(V(s * .54f, .76f, -1.605f), Q, V(.3f, .06f, .04f));
            }

            // Windschutzscheiben-Rahmen (ohne Glas, damit die Sicht frei bleibt), niedrig unter der Augenhoehe
            foreach (float s in new[] { -1f, 1f }) black.Beam(V(s * (hw - .06f), .98f, .7f), V(s * (hw - .1f), 1.32f, .48f), .05f, Vector3.up);
            black.Beam(V(-(hw - .1f), 1.32f, .48f), V(hw - .1f, 1.32f, .48f), .05f, Vector3.up);

            // Radlaeufe, Reifen, Alufelgen
            foreach (float z in new[] { 1.08f, -1.05f })
                foreach (float s in new[] { -1f, 1f })
                {
                    black.Box(V(s * (hw + .02f), .66f, z), Q, V(.05f, .08f, .78f));
                    black.Loft(new[] { V(s * (hw - .2f), .33f, z), V(s * (hw + .02f), .33f, z) }, new[] { R(.3f, .3f), R(.3f, .3f) }, 1, 18, true, true, Vector3.forward);
                    chrome.Loft(new[] { V(s * (hw + .02f), .33f, z), V(s * (hw + .035f), .33f, z) }, new[] { R(.19f, .19f), R(.19f, .19f) }, 1, 14, true, true, Vector3.forward);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI / 2 + .4f;
                        black.Box(V(s * (hw + .04f), .33f + Mathf.Sin(a) * .1f, z + Mathf.Cos(a) * .1f), Q, V(.01f, .05f, .05f));
                    }
                }

            // Innenraum: zwei Sitzreihen mit Sportsitzen, Kopfstuetzen, Schossbuegel, Armaturenbrett
            pad.Box(V(0, .64f, .62f), Q, V(W - .2f, .26f, .18f));                                            // Armaturenbrett
            foreach (float z in new[] { .1f, -.85f })
                foreach (float s in new[] { -.36f, .36f })
                {
                    pad.Box(V(s, .58f, z), Q, V(.56f, .2f, .58f));
                    pad.Box(V(s, .95f, z - .32f), Quaternion.Euler(-12, 0, 0), V(.56f, .66f, .14f));
                    pad.Box(V(s, 1.38f, z - .38f), Q, V(.32f, .2f, .12f));
                    red.Box(V(s, .95f, z - .24f), Quaternion.Euler(-12, 0, 0), V(.14f, .5f, .02f));           // roter Sitzstreifen
                    chrome.Beam(V(s - .2f, .9f, z + .32f), V(s + .2f, .9f, z + .32f), .05f, Vector3.up);       // Schossbuegel
                    chrome.Beam(V(s - .2f, .9f, z + .32f), V(s - .22f, .62f, z + .12f), .04f, Vector3.up);
                    chrome.Beam(V(s + .2f, .9f, z + .32f), V(s + .22f, .62f, z + .12f), .04f, Vector3.up);
                }
            black.Loft(new[] { V(-.36f, .85f, .5f), V(-.36f, .9f, .45f) }, new[] { R(.16f, .16f), R(.16f, .16f) }, 1, 14, true, true, Vector3.up);   // Lenkrad

            // Fahrgestell zur Schiene
            black.Box(V(0, .22f, 0), Q, V(.9f, .16f, 2.4f));

            var paint = Lit(index % 2 == 0 ? new Color(.95f, .72f, .05f) : new Color(.78f, .06f, .06f), .86f, .25f);
            body.ToObject("Karosserie", paint, car);
            black.ToObject("Kunststoff", Lit(new Color(.05f, .05f, .055f), .35f, 0), car);
            chrome.ToObject("Alu", Lit(new Color(.82f, .83f, .85f), .85f, .95f), car);
            pad.ToObject("Sitze", Lit(new Color(.12f, .12f, .13f), .3f, 0), car);
            glassL.ToObject("Scheinwerfer", Lit(new Color(1f, .98f, .9f), .97f, .2f), car);
            red.ToObject("Rot", Lit(new Color(.8f, .05f, .04f), .8f, 0), car);
            amber.ToObject("Blinker", Lit(new Color(1f, .55f, .05f), .85f, 0), car);
            stripe.ToObject("Streifen", Lit(new Color(.06f, .06f, .06f), .5f, 0), car);
            return car;
        }
    }
}
