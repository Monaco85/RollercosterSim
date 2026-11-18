using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>Sammelt Quader, Zylinder, Kegel und Daecher in einem Mesh. Texturkoordinaten in Metern, damit Holz und Stein nicht verzerren.</summary>
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        public float tile = 1f; // Meter pro Texturwiederholung

        public int Count => v.Count;

        // Unity: Vorderseite = im Uhrzeigersinn gesehen. Normale aus der Geometrie, damit sie sicher nach aussen zeigt.
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nor, float w, float h)
        {
            int i = v.Count;
            Vector3 g = Vector3.Cross(b - a, c - a).normalized;
            if (g.sqrMagnitude < .5f) g = nor;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            n.Add(g); n.Add(g); n.Add(g); n.Add(g);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(w / tile, 0)); uv.Add(new Vector2(w / tile, h / tile)); uv.Add(new Vector2(0, h / tile));
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>Quader mit Mittelpunkt, Drehung und Groesse (x Breite, y Hoehe, z Laenge).</summary>
        public void Box(Vector3 c, Quaternion q, Vector3 s)
        {
            Vector3 X = q * Vector3.right * (s.x / 2), Y = q * Vector3.up * (s.y / 2), Z = q * Vector3.forward * (s.z / 2);
            Vector3 nx = q * Vector3.right, ny = q * Vector3.up, nz = q * Vector3.forward;
            Quad(c - X - Y + Z, c + X - Y + Z, c + X + Y + Z, c - X + Y + Z, nz, s.x, s.y);   // vorn
            Quad(c + X - Y - Z, c - X - Y - Z, c - X + Y - Z, c + X + Y - Z, -nz, s.x, s.y);  // hinten
            Quad(c + X - Y + Z, c + X - Y - Z, c + X + Y - Z, c + X + Y + Z, nx, s.z, s.y);   // rechts
            Quad(c - X - Y - Z, c - X - Y + Z, c - X + Y + Z, c - X + Y - Z, -nx, s.z, s.y);  // links
            Quad(c - X + Y + Z, c + X + Y + Z, c + X + Y - Z, c - X + Y - Z, ny, s.x, s.z);   // oben
            Quad(c - X - Y - Z, c + X - Y - Z, c + X - Y + Z, c - X - Y + Z, -ny, s.x, s.z);  // unten
        }

        /// <summary>Balken von a nach b mit quadratischem Querschnitt.</summary>
        public void Beam(Vector3 a, Vector3 b, float w, Vector3 upHint)
        {
            Vector3 d = b - a; float len = d.magnitude; if (len < 1e-3f) return;
            Vector3 f = d / len;
            Vector3 up = Mathf.Abs(Vector3.Dot(f, upHint.normalized)) > .95f ? Vector3.right : upHint;
            Box((a + b) * .5f, Quaternion.LookRotation(f, up), new Vector3(w, w, len));
        }

        public void Cylinder(Vector3 bottom, float r, float h, int seg = 20, bool cap = true)
        {
            float circ = 2 * Mathf.PI * r;
            int start = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(bottom + dir * r); n.Add(dir); uv.Add(new Vector2(i / (float)seg * circ / tile, 0));
                v.Add(bottom + dir * r + Vector3.up * h); n.Add(dir); uv.Add(new Vector2(i / (float)seg * circ / tile, h / tile));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 2;
                t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3);
            }
            if (cap) Disc(bottom + Vector3.up * h, r, seg, Vector3.up);
        }

        public void Disc(Vector3 c, float r, int seg, Vector3 nor)
        {
            int ci = v.Count; v.Add(c); n.Add(nor); uv.Add(Vector2.zero);
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; Vector3 p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                v.Add(p); n.Add(nor); uv.Add(new Vector2((p.x - c.x) / tile, (p.z - c.z) / tile));
            }
            for (int i = 0; i < seg; i++) { t.Add(ci); t.Add(ci + i + 2); t.Add(ci + i + 1); }
        }

        public void Cone(Vector3 bottom, float r, float h, int seg = 20, float startDeg = 0f)
        {
            float slant = Mathf.Sqrt(r * r + h * h), s0 = startDeg * Mathf.Deg2Rad;
            for (int i = 0; i < seg; i++)
            {
                float a0 = s0 + i / (float)seg * Mathf.PI * 2, a1 = s0 + (i + 1) / (float)seg * Mathf.PI * 2;
                Vector3 p0 = bottom + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, p1 = bottom + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, top = bottom + Vector3.up * h;
                Vector3 nor = Vector3.Cross(p1 - p0, top - p0).normalized; if (nor.y < 0) nor = -nor;
                int k = v.Count;
                v.Add(p0); v.Add(p1); v.Add(top); n.Add(nor); n.Add(nor); n.Add(nor);
                float w = (p1 - p0).magnitude;
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(w / tile, 0)); uv.Add(new Vector2(w * .5f / tile, slant / tile));
                t.Add(k); t.Add(k + 2); t.Add(k + 1);
            }
        }

        /// <summary>Satteldach ueber einem Rechteck (Breite x, Laenge z), First in z-Richtung.</summary>
        public void Gable(Vector3 baseCenter, Quaternion q, float w, float len, float h, float over = .4f)
        {
            Vector3 X = q * Vector3.right * (w / 2 + over), Z = q * Vector3.forward * (len / 2 + over), Y = q * Vector3.up * h;
            Vector3 a = baseCenter - X - Z, b = baseCenter + X - Z, c = baseCenter + X + Z, d = baseCenter - X + Z, r0 = baseCenter + Y - Z, r1 = baseCenter + Y + Z;
            float slope = Mathf.Sqrt((w / 2 + over) * (w / 2 + over) + h * h);
            Quad(a, d, r1, r0, Vector3.up, len + 2 * over, slope);
            Quad(c, b, r0, r1, Vector3.up, len + 2 * over, slope);
            // Giebeldreiecke
            int k = v.Count; Vector3 nf = -(q * Vector3.forward);
            v.Add(a); v.Add(b); v.Add(r0); n.Add(nf); n.Add(nf); n.Add(nf); uv.Add(Vector2.zero); uv.Add(new Vector2(w / tile, 0)); uv.Add(new Vector2(w * .5f / tile, h / tile));
            t.Add(k); t.Add(k + 2); t.Add(k + 1);
            k = v.Count; Vector3 nb = q * Vector3.forward;
            v.Add(c); v.Add(d); v.Add(r1); n.Add(nb); n.Add(nb); n.Add(nb); uv.Add(Vector2.zero); uv.Add(new Vector2(w / tile, 0)); uv.Add(new Vector2(w * .5f / tile, h / tile));
            t.Add(k); t.Add(k + 2); t.Add(k + 1);
        }

        /// <summary>Zweig-Karte: vom Ansatzpunkt in Richtung 'along', Breite quer dazu. Normale zeigt weich nach aussen/oben.</summary>
        public void Card(Vector3 origin, Vector3 along, Vector3 side, float len, float width, Vector3 normal)
        {
            Vector3 s = side.normalized * (width * .5f), f = along.normalized * len;
            int i = v.Count;
            v.Add(origin - s); v.Add(origin + s); v.Add(origin + s + f); v.Add(origin - s + f);
            for (int k = 0; k < 4; k++) n.Add(normal);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }

        static Vector3 CR(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3);
        }

        /// <summary>
        /// Organische Roehre entlang einer weichen Kurve mit elliptischem Querschnitt (Breite x, Hoehe y je Kontrollpunkt).
        /// Textur: U entlang (laengentreu), V einmal herum, V = 0 unten (Bauch), V = 0,5 oben (Ruecken).
        /// </summary>
        /// <summary>Offene Rinne: nur der Bogen von unten bis 'halfAngleDeg' zu beiden Seiten (z. B. Wagenwanne). flip = Innenseite.</summary>
        public void LoftArc(IList<Vector3> ctrl, IList<Vector2> rad, float halfAngleDeg, int sub = 5, int seg = 18, bool flip = false, Vector3? upHint = null)
        {
            var P = new List<Vector3>(); var R = new List<Vector2>();
            int cn = ctrl.Count;
            for (int i = 0; i < cn - 1; i++)
            {
                Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, cn - 1)];
                for (int k = 0; k < sub; k++) { float tt = k / (float)sub, sm = tt * tt * (3 - 2 * tt); P.Add(CR(p0, p1, p2, p3, tt)); R.Add(Vector2.Lerp(rad[i], rad[i + 1], sm)); }
            }
            P.Add(ctrl[cn - 1]); R.Add(rad[cn - 1]);
            int m = P.Count, start = v.Count; Vector3 side = Vector3.zero, refUp = upHint ?? Vector3.up; float u = 0, ha = halfAngleDeg * Mathf.Deg2Rad;
            for (int j = 0; j < m; j++)
            {
                Vector3 T = (P[Mathf.Min(j + 1, m - 1)] - P[Mathf.Max(j - 1, 0)]).normalized;
                if (j == 0) { side = Vector3.Cross(refUp, T); if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(Vector3.forward, T); side.Normalize(); }
                else side = (side - T * Vector3.Dot(side, T)).normalized;
                Vector3 up2 = Vector3.Cross(T, side);
                float w = Mathf.Max(.005f, R[j].x), h = Mathf.Max(.005f, R[j].y);
                if (j > 0) u += (P[j] - P[j - 1]).magnitude;
                for (int s = 0; s <= seg; s++)
                {
                    float a = -Mathf.PI / 2 - ha + s / (float)seg * 2 * ha, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    v.Add(P[j] + side * (ca * w) + up2 * (sa * h));
                    Vector3 nn = (side * (ca / w) + up2 * (sa / h)).normalized; n.Add(flip ? -nn : nn);
                    uv.Add(new Vector2(u / tile, s / (float)seg * 2 * ha * Mathf.Sqrt((w * w + h * h) / 2) / tile));
                }
            }
            for (int j = 0; j < m - 1; j++) for (int s = 0; s < seg; s++)
            {
                int a = start + j * (seg + 1) + s, b = a + seg + 1;
                if (!flip) { t.Add(a); t.Add(a + 1); t.Add(b); t.Add(a + 1); t.Add(b + 1); t.Add(b); }
                else { t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1); }
            }
        }

        public void Loft(IList<Vector3> ctrl, IList<Vector2> rad, int sub = 5, int seg = 16, bool capStart = true, bool capEnd = true, Vector3? upHint = null)
        {
            var P = new List<Vector3>(); var R = new List<Vector2>();
            int cn = ctrl.Count;
            for (int i = 0; i < cn - 1; i++)
            {
                Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, cn - 1)];
                for (int k = 0; k < sub; k++)
                {
                    float tt = k / (float)sub, sm = tt * tt * (3 - 2 * tt);
                    P.Add(CR(p0, p1, p2, p3, tt)); R.Add(Vector2.Lerp(rad[i], rad[i + 1], sm));
                }
            }
            P.Add(ctrl[cn - 1]); R.Add(rad[cn - 1]);
            int m = P.Count, start = v.Count; Vector3 side = Vector3.zero, refUp = upHint ?? Vector3.up; float u = 0;
            var Ts = new Vector3[m];
            for (int j = 0; j < m; j++)
            {
                Vector3 T = (P[Mathf.Min(j + 1, m - 1)] - P[Mathf.Max(j - 1, 0)]).normalized; Ts[j] = T;
                if (j == 0) { side = Vector3.Cross(refUp, T); if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(Vector3.forward, T); side.Normalize(); }
                else side = (side - T * Vector3.Dot(side, T)).normalized;
                Vector3 up2 = Vector3.Cross(T, side);
                float w = Mathf.Max(.005f, R[j].x), h = Mathf.Max(.005f, R[j].y);
                if (j > 0) u += (P[j] - P[j - 1]).magnitude / Mathf.Max(.2f, 2 * Mathf.PI * Mathf.Sqrt((w * w + h * h) / 2));
                for (int s = 0; s <= seg; s++)
                {
                    float a = -Mathf.PI / 2 + s / (float)seg * Mathf.PI * 2, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    v.Add(P[j] + side * (ca * w) + up2 * (sa * h));
                    n.Add((side * (ca / w) + up2 * (sa / h)).normalized);
                    uv.Add(new Vector2(u, s / (float)seg));
                }
            }
            for (int j = 0; j < m - 1; j++) for (int s = 0; s < seg; s++)
            {
                int a = start + j * (seg + 1) + s, b = a + seg + 1;
                t.Add(a); t.Add(a + 1); t.Add(b); t.Add(a + 1); t.Add(b + 1); t.Add(b);
            }
            void Cap(int ring, Vector3 c, Vector3 nor, bool end)
            {
                int ci = v.Count; v.Add(c); n.Add(nor); uv.Add(new Vector2(u, .5f));
                int r0 = start + ring * (seg + 1);
                for (int s = 0; s < seg; s++)
                {
                    if (end) { t.Add(ci); t.Add(r0 + s); t.Add(r0 + s + 1); }
                    else { t.Add(ci); t.Add(r0 + s + 1); t.Add(r0 + s); }
                }
            }
            if (capStart) Cap(0, P[0], -Ts[0], false);
            if (capEnd) Cap(m - 1, P[m - 1], Ts[m - 1], true);
        }

        public void Sphere(Vector3 c, float r, int seg = 12)
        {
            int rows = seg / 2, start = v.Count;
            for (int j = 0; j <= rows; j++)
            {
                float lat = -Mathf.PI / 2 + j / (float)rows * Mathf.PI;
                for (int i = 0; i <= seg; i++)
                {
                    float lon = i / (float)seg * Mathf.PI * 2;
                    Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    v.Add(c + d * r); n.Add(d); uv.Add(new Vector2(i / (float)seg, j / (float)rows));
                }
            }
            for (int j = 0; j < rows; j++) for (int i = 0; i < seg; i++)
            {
                int a = start + j * (seg + 1) + i, up = a + seg + 1;
                t.Add(a); t.Add(up); t.Add(a + 1); t.Add(a + 1); t.Add(up); t.Add(up + 1);
            }
        }

        /// <summary>Spitzer Kegel von 'basis' in Richtung 'dir' (Zaehne, Hoerner, Krallen, Stacheln). flat &lt; 1 macht ihn platt (Rueckenplatten).</summary>
        public void Spike(Vector3 basis, Vector3 dir, float r, float len, int seg = 7, float flat = 1f, Vector3? flatAxis = null)
        {
            dir = dir.normalized;
            Vector3 a = flatAxis.HasValue ? Vector3.ProjectOnPlane(flatAxis.Value, dir).normalized : Vector3.Cross(dir, Mathf.Abs(dir.y) < .9f ? Vector3.up : Vector3.right).normalized;
            Vector3 b = Vector3.Cross(a, dir);
            Vector3 apex = basis + dir * len;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2, a1 = (i + 1) / (float)seg * Mathf.PI * 2;
                Vector3 p0 = basis + (a * Mathf.Cos(a0) * flat + b * Mathf.Sin(a0)) * r, p1 = basis + (a * Mathf.Cos(a1) * flat + b * Mathf.Sin(a1)) * r;
                Vector3 nor = Vector3.Cross(apex - p0, p1 - p0).normalized;
                int k = v.Count;
                v.Add(p0); v.Add(apex); v.Add(p1); n.Add(nor); n.Add(nor); n.Add(nor);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(.5f, 1)); uv.Add(new Vector2(1, 0));
                t.Add(k); t.Add(k + 1); t.Add(k + 2);
            }
        }

        /// <summary>Flacher Ring (z. B. Wassergraben), Normale nach oben.</summary>
        public void Annulus(Vector3 c, float r0, float r1, int seg = 48)
        {
            int start = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 p0 = c + d * r0, p1 = c + d * r1;
                v.Add(p0); n.Add(Vector3.up); uv.Add(new Vector2(p0.x / tile, p0.z / tile));
                v.Add(p1); n.Add(Vector3.up); uv.Add(new Vector2(p1.x / tile, p1.z / tile));
            }
            for (int i = 0; i < seg; i++) { int a = start + i * 2; t.Add(a); t.Add(a + 2); t.Add(a + 1); t.Add(a + 1); t.Add(a + 2); t.Add(a + 3); }
        }

        /// <summary>Kegelstumpf (Baumstamm), Textur in Metern.</summary>
        public void Taper(Vector3 bottom, float r0, float r1, float h, int seg = 9)
        {
            int start = v.Count; float circ = 2 * Mathf.PI * r0;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(bottom + d * r0); n.Add(d); uv.Add(new Vector2(i / (float)seg * circ / tile, 0));
                v.Add(bottom + d * r1 + Vector3.up * h); n.Add(d); uv.Add(new Vector2(i / (float)seg * circ / tile, h / tile));
            }
            for (int i = 0; i < seg; i++) { int a = start + i * 2; t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3); }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        public GameObject ToObject(string name, Material mat, Transform parent, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ToMesh(name);
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
    }
}
