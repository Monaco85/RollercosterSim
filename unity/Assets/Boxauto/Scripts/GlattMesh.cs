using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn.Boxauto
{
    /// <summary>
    /// Baut glatte Flaechen (weiche Normalen) aus Gittern: Rohre entlang von Kurven, Drehkoerper, Super-Ellipsoide.
    /// Normalen kommen aus der Gitter-Ableitung, die Richtung wird an einem Pruefpunkt gegen eine erwartete Richtung geprueft.
    /// </summary>
    public class GlattMesh
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        public int Count => v.Count;

        // ---------- Gitter ----------
        /// <summary>P[i,j]: i quer (u), j laengs (v). 'erwartet' = ungefaehre Normalenrichtung am Pruefpunkt (pi,pj).</summary>
        public void Grid(Vector3[,] P, bool wrapU, bool wrapV, int pi, int pj, Vector3 erwartet, float uvMeter = 1f, int j0 = 0, int j1 = -1)
        {
            int nu = P.GetLength(0), nv = P.GetLength(1);
            Vector3 At(int i, int j)
            {
                i = wrapU ? ((i % nu) + nu) % nu : Mathf.Clamp(i, 0, nu - 1);
                j = wrapV ? ((j % nv) + nv) % nv : Mathf.Clamp(j, 0, nv - 1);
                return P[i, j];
            }
            Vector3 Raw(int i, int j)
            {
                Vector3 du = At(i + 1, j) - At(i - 1, j), dv = At(i, j + 1) - At(i, j - 1);
                return Vector3.Cross(du, dv);
            }
            Vector3 Nor(int i, int j)
            {
                Vector3 c = Raw(i, j);
                if (c.sqrMagnitude > 1e-14f) return c.normalized;
                // Pol oder entartete Kante: Normalen des Nachbarrings mitteln
                int jn = j == 0 ? 1 : j == nv - 1 ? nv - 2 : j + 1;
                Vector3 s = Vector3.zero;
                for (int k = 0; k < nu; k++) { var r = Raw(k, jn); if (r.sqrMagnitude > 1e-14f) s += r.normalized; }
                if (s.sqrMagnitude < 1e-10f) { int in2 = i == 0 ? 1 : i - 1; s = Raw(in2, j); }
                return s.sqrMagnitude > 1e-14f ? s.normalized : Vector3.up;
            }
            float sign = Vector3.Dot(Nor(pi, pj), erwartet) < 0 ? -1f : 1f;

            int cu = wrapU ? nu + 1 : nu, cv = wrapV ? nv + 1 : nv;
            // Bogenlaengen fuer UV (in Metern)
            var uAcc = new float[cu]; var vAcc = new float[cv];
            int mj = nv / 2, mi = nu / 2;
            for (int i = 1; i < cu; i++) uAcc[i] = uAcc[i - 1] + (At(i, mj) - At(i - 1, mj)).magnitude;
            for (int j = 1; j < cv; j++) vAcc[j] = vAcc[j - 1] + (At(mi, j) - At(mi, j - 1)).magnitude;

            // Optional nur ein Zeilenbereich (Normalen trotzdem aus dem ganzen Gitter, damit Naehte glatt bleiben)
            if (j1 < 0) j1 = cv - 1;
            int start = v.Count;
            for (int j = j0; j <= j1; j++)
                for (int i = 0; i < cu; i++)
                {
                    v.Add(At(i, j)); n.Add(Nor(i, j) * sign); uv.Add(new Vector2(uAcc[i], vAcc[j]) / uvMeter);
                }
            cv = j1 - j0 + 1;
            for (int j = 0; j < cv - 1; j++)
                for (int i = 0; i < cu - 1; i++)
                {
                    int a = start + j * cu + i, b = a + 1, c = a + cu + 1, d = a + cu;
                    if (sign > 0) { t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d); }
                    else { t.Add(a); t.Add(c); t.Add(b); t.Add(a); t.Add(d); t.Add(c); }
                }
        }

        /// <summary>Flacher Faecher ueber einem Ring (Deckel), Vorderseite in Richtung 'nor'.</summary>
        public void Fan(Vector3 mitte, IList<Vector3> ring, Vector3 nor, float uvMeter = 1f)
        {
            nor.Normalize();
            Vector3 ax = Vector3.Cross(nor, Mathf.Abs(nor.y) > .9f ? Vector3.right : Vector3.up).normalized, ay = Vector3.Cross(nor, ax);
            int c = v.Count; v.Add(mitte); n.Add(nor); uv.Add(Vector2.zero);
            for (int i = 0; i < ring.Count; i++)
            {
                v.Add(ring[i]); n.Add(nor); var d = ring[i] - mitte; uv.Add(new Vector2(Vector3.Dot(d, ax), Vector3.Dot(d, ay)) / uvMeter);
            }
            for (int i = 0; i < ring.Count; i++)
            {
                int a = c + 1 + i, b = c + 1 + (i + 1) % ring.Count;
                Vector3 g = Vector3.Cross(v[a] - mitte, v[b] - mitte);
                if (Vector3.Dot(g, nor) >= 0) { t.Add(c); t.Add(a); t.Add(b); } else { t.Add(c); t.Add(b); t.Add(a); }
            }
        }

        // ---------- Grundformen ----------
        /// <summary>Rohr mit beliebigem (geschlossenem) Querschnitt entlang eines Pfads. Querschnitt: x = seitlich (aussen), y = oben.</summary>
        public void Tube(IList<Vector3> pfad, bool geschlossen, IList<Vector2> profil, Vector3 oben, bool deckel = true, float uvMeter = 1f)
        {
            int np = pfad.Count, nq = profil.Count;
            var P = new Vector3[nq, np];
            Vector3 side0 = Vector3.right;
            for (int k = 0; k < np; k++)
            {
                Vector3 a = geschlossen ? pfad[(k - 1 + np) % np] : pfad[Mathf.Max(0, k - 1)];
                Vector3 b = geschlossen ? pfad[(k + 1) % np] : pfad[Mathf.Min(np - 1, k + 1)];
                Vector3 T = (b - a).normalized;
                Vector3 up = oben; if (Mathf.Abs(Vector3.Dot(up.normalized, T)) > .98f) up = Vector3.Cross(T, Vector3.right);
                Vector3 side = Vector3.Cross(T, up).normalized, upv = Vector3.Cross(side, T).normalized;
                if (k == 0) side0 = side;
                for (int q = 0; q < nq; q++) P[q, k] = pfad[k] + side * profil[q].x + upv * profil[q].y;
            }
            int best = 0; for (int q = 1; q < nq; q++) if (profil[q].x > profil[best].x) best = q;
            Grid(P, true, geschlossen, best, 0, side0, uvMeter);
            if (!geschlossen && deckel)
            {
                var r0 = new Vector3[nq]; var r1 = new Vector3[nq]; Vector3 m0 = Vector3.zero, m1 = Vector3.zero;
                for (int q = 0; q < nq; q++) { r0[q] = P[q, 0]; r1[q] = P[q, np - 1]; m0 += r0[q]; m1 += r1[q]; }
                Fan(m0 / nq, r0, pfad[0] - pfad[1], uvMeter);
                Fan(m1 / nq, r1, pfad[np - 1] - pfad[np - 2], uvMeter);
            }
        }

        /// <summary>Drehkoerper: Profil (Radius, Hoehe) um die Achse.</summary>
        public void Lathe(Vector3 basis, Vector3 achse, IList<Vector2> profil, int seg = 32, float uvMeter = 1f)
        {
            achse.Normalize();
            Vector3 e1 = Vector3.Cross(achse, Mathf.Abs(achse.y) > .9f ? Vector3.forward : Vector3.up).normalized, e2 = Vector3.Cross(achse, e1);
            var P = new Vector3[seg, profil.Count];
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; Vector3 d = e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a);
                for (int j = 0; j < profil.Count; j++) P[i, j] = basis + achse * profil[j].y + d * profil[j].x;
            }
            int best = 0; for (int q = 1; q < profil.Count; q++) if (profil[q].x > profil[best].x) best = q;
            Grid(P, true, false, 0, best, e1, uvMeter);
        }

        static float F(float w, float e) => Mathf.Sign(w) * Mathf.Pow(Mathf.Abs(w), e);

        /// <summary>Super-Ellipsoid (abgerundeter Kasten). e = 1 Ellipsoid, klein = kantig. h = halbe Ausdehnung.</summary>
        public void Blob(Vector3 c, Quaternion q, Vector3 h, float eV = .35f, float eH = .35f, int su = 28, int sv = 16)
        {
            var P = new Vector3[su, sv + 1];
            for (int i = 0; i < su; i++)
            {
                float u = i / (float)su * Mathf.PI * 2;
                for (int j = 0; j <= sv; j++)
                {
                    float w = -Mathf.PI / 2 + j / (float)sv * Mathf.PI;
                    float cv = F(Mathf.Cos(w), eV);
                    var p = new Vector3(h.x * cv * F(Mathf.Cos(u), eH), h.y * F(Mathf.Sin(w), eV), h.z * cv * F(Mathf.Sin(u), eH));
                    P[i, j] = c + q * p;
                }
            }
            Grid(P, true, false, 0, sv / 2, q * Vector3.right, .5f);
        }

        public void Sphere(Vector3 c, float r, int su = 16, int sv = 10) => Blob(c, Quaternion.identity, Vector3.one * r, 1, 1, su, sv);

        /// <summary>Kreisrohr von a nach b (mit Deckeln).</summary>
        public void Rod(Vector3 a, Vector3 b, float r, int seg = 14, bool deckel = true)
        {
            Tube(new[] { a, b }, false, Kreis(r, r, seg), Mathf.Abs(Vector3.Dot((b - a).normalized, Vector3.up)) > .9f ? Vector3.forward : Vector3.up, deckel);
        }

        /// <summary>Offener Lichtkegel, uv.y = 0 an der Lampe, 1 am Ende (fuer den LightBeam-Shader).</summary>
        public void BeamCone(Vector3 a, Vector3 dir, float r0, float r1, float len, int seg = 24)
        {
            dir.Normalize();
            Vector3 e1 = Vector3.Cross(dir, Mathf.Abs(dir.y) > .9f ? Vector3.forward : Vector3.up).normalized, e2 = Vector3.Cross(dir, e1);
            int s = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float w = i / (float)seg * Mathf.PI * 2; Vector3 d = e1 * Mathf.Cos(w) + e2 * Mathf.Sin(w);
                v.Add(a + d * r0); n.Add(d); uv.Add(new Vector2(i / (float)seg, 0));
                v.Add(a + dir * len + d * r1); n.Add(d); uv.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++) { int k = s + i * 2; t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k + 2); t.Add(k + 1); t.Add(k + 3); }
        }

        /// <summary>Rohes Hinzufuegen (fuer Abziehbilder): Vertices mit eigenen UVs, Dreiecke wie angegeben.</summary>
        public int AddVert(Vector3 p, Vector3 nor, Vector2 tex) { v.Add(p); n.Add(nor); uv.Add(tex); return v.Count - 1; }
        public void AddTri(int a, int b, int c) { t.Add(a); t.Add(b); t.Add(c); }

        public static Vector2[] Kreis(float rx, float ry, int seg)
        {
            var p = new Vector2[seg];
            for (int i = 0; i < seg; i++) { float a = i / (float)seg * Mathf.PI * 2; p[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); }
            return p;
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }
    }
}
