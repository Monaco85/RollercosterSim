// Blinkmuster fuer Rummel-Gluehbirnen. Alles laeuft auf der Grafikkarte, ohne Skript pro Lampe.
// d.x = Phase (Lage in der Kette 0..1 oder Winkel), d.y = Muster, d.z = Zufallswert, d.w = Zusatz (Anzahl oder Radius)
#ifndef ACHTERBAHN_BULB_PATTERN
#define ACHTERBAHN_BULB_PATTERN

float BulbHash(float p) { p = frac(p * 0.1031); p *= p + 33.33; p *= p + p; return frac(p); }
float3 BulbHue(float h) { return saturate(abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0); }

// k = Helligkeit (0..1+), col wird bei Regenbogen-Mustern ersetzt
void BulbPattern(float4 d, float t, inout float3 col, out float k)
{
    float phase = d.x, mode = d.y, seed = d.z, aux = max(d.w, 1.0);
    k = 1.0;
    if (mode < 0.5)            // ruhig, leicht flackernd
    {
        k = 0.9 + 0.1 * sin(t * 6.0 + seed * 40.0) * sin(t * 2.3 + seed * 17.0);
    }
    else if (mode < 1.5)       // Lauflicht: jede dritte Lampe an, wandert weiter
    {
        float idx = floor(phase * aux + 0.5);
        float step3 = fmod(idx + floor(t * 6.0), 3.0);
        k = step3 < 0.5 ? 1.0 : 0.12;
    }
    else if (mode < 2.5)       // Funkeln zufaellig
    {
        float n = BulbHash(seed * 91.7 + floor(t * 2.5 + seed * 13.0));
        k = n > 0.3 ? 1.0 : 0.1;
    }
    else if (mode < 3.5)       // Regenbogen laeuft durch
    {
        float l = max(max(col.r, col.g), col.b);
        col = BulbHue(frac(phase * 1.5 - t * 0.12)) * l + 0.04;
        k = 1.0;
    }
    else if (mode < 4.5)       // Wechselblinker: gerade/ungerade
    {
        k = fmod(floor(seed + 0.5) + floor(t * 1.6), 2.0) < 0.5 ? 1.0 : 0.1;
    }
    else if (mode < 5.5)       // Welle laeuft durch die Kette
    {
        float s = 0.5 + 0.5 * sin((phase * aux / 6.0 - t * 1.4) * 6.2832);
        k = 0.12 + 0.88 * s * s * s;
    }
    else if (mode < 6.5)       // Riesenrad-Show: wechselt alle 10 s das Programm
    {
        float prog = fmod(floor(t / 10.0), 5.0);
        float l = max(max(col.r, col.g), col.b);
        if (prog < 0.5)        { col = BulbHue(frac(phase * 2.0 - t * 0.2)) * l; k = 1.0; }                                   // drehende Farbsektoren
        else if (prog < 1.5)   { float r = frac(aux - t * 0.7); k = 0.1 + 0.9 * smoothstep(0.75, 1.0, 1.0 - abs(r - 0.5) * 2.0); col = BulbHue(0.9 + aux * 0.4) * l; } // Ringe laufen nach aussen
        else if (prog < 2.5)   { k = 0.1 + step(0.82, frac(phase * 8.0 - t * 1.2)); col = BulbHue(frac(t * 0.05)) * l; }    // Lauflicht im Kreis
        else if (prog < 3.5)   { k = BulbHash(seed * 37.0 + floor(t * 6.0)) > 0.5 ? 1.0 : 0.1; col = float3(1.0, 0.82, 0.5) * l; } // Glitzern gold
        else                   { float s = sin(phase * 6.2832 * 3.0 + aux * 6.0 - t * 3.0); k = 0.15 + 0.85 * saturate(s); col = lerp(float3(1, 0.2, 0.6), float3(0.2, 0.6, 1), 0.5 + 0.5 * sin(t)) * l; } // Spirale
    }
    else if (mode < 7.5)       // Atmen
    {
        k = 0.4 + 0.6 * (0.5 + 0.5 * sin(t * 2.2 + seed));
    }
    else                       // Steigendes Lauflicht (Turm): Leuchtpunkt faehrt nach oben
    {
        float x = frac(phase - t * 0.35);
        k = 0.14 + pow(saturate(1.0 - x), 10.0) * 1.2 + (fmod(floor(phase * aux) + floor(t * 4.0), 4.0) < 0.5 ? 0.35 : 0.0);
    }
}
#endif
