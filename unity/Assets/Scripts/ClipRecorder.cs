using System.Collections;
using System.IO;
using UnityEngine;

namespace Achterbahn
{
    /// <summary>Werkzeug fuer Vorschau-Clips (nur Editor-Tests): rendert eine Kamera mit fester Bildrate in JPG-Bilder.</summary>
    public class ClipRecorder : MonoBehaviour
    {
        public Camera cam; public string dir; public int frames = 300, fps = 15, width = 960, height = 540;
        public bool orbit; public Vector3 center; public float radius = 120, height3 = 45, orbitSpeed = .08f;
        public int Done { get; private set; }

        public static ClipRecorder Start(Camera c, string dir, int frames, int fps = 15)
        {
            var go = new GameObject("ClipRecorder"); var r = go.AddComponent<ClipRecorder>();
            r.cam = c; r.dir = dir; r.frames = frames; r.fps = fps; return r;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            Time.captureFramerate = fps;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            float a = 0;
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
                if (orbit && cam != null)
                {
                    a += orbitSpeed / fps;
                    cam.transform.position = center + new Vector3(Mathf.Cos(a) * radius, height3, Mathf.Sin(a) * radius);
                    cam.transform.LookAt(center + Vector3.up * 8f);
                }
                var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply(false);
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, $"f{i:0000}.jpg"), tex.EncodeToJPG(92));
                Done = i + 1;
            }
            Time.captureFramerate = 0;
            Destroy(rt); Destroy(tex); Destroy(gameObject);
        }
    }
}
