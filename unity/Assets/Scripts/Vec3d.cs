using System;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>Doppelt genauer 3D-Vektor. Die Physik der HTML-Fassung rechnet in Float64, hier ebenso.</summary>
    public struct V3
    {
        public double x, y, z;
        public V3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }

        public static V3 operator +(V3 a, V3 b) => new V3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static V3 operator *(V3 a, double k) => new V3(a.x * k, a.y * k, a.z * k);

        public static double Dot(V3 a, V3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static V3 Cross(V3 a, V3 b) => new V3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public double Len => Math.Sqrt(x * x + y * y + z * z);
        public static V3 Norm(V3 a)
        {
            double l = a.Len; if (l == 0) l = 1;
            return new V3(a.x / l, a.y / l, a.z / l);
        }
        public static V3 Lerp(V3 a, V3 b, double t) => new V3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        /// <summary>Spiegelt z, weil die Bahndaten (three.js, rechtshaendig) in Unity (linkshaendig) gleich aussehen sollen.</summary>
        public Vector3 ToUnity() => new Vector3((float)x, (float)y, (float)-z);
    }
}
