using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Achterbahn
{
    /// <summary>
    /// Backt die Quaternius-Figuren einmal beim ersten Laden: ein Mesh pro Figur (alle Teile zusammen, Farbindex je Vertex)
    /// und eine Textur mit den Knochenmatrizen aller Animationsbilder. Gerendert wird mit dem Crowd-Shader,
    /// das spart pro Person die teuren SkinnedMeshRenderer und die Animationskomponente.
    /// </summary>
    public static class CrowdBank
    {
        public const float Fps = 15f;

        public class Clip { public int start, count; public float length; public bool loop; }

        public class Template
        {
            public Mesh mesh; public Material mat; public int slots;
            public Vector4[] palette = new Vector4[16]; public bool[] cloth = new bool[16];
            public readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
            public Vector3[] handPosR, handPosL; public Quaternion[] handRotR, handRotL;
            public float hipHeight;
        }

        static List<Template> cache;
        public static List<Template> Get(ThemeAssets ta)
        {
            if (cache != null) return cache;
            cache = new List<Template>();
            if (ta == null || ta.people == null || ta.crowd == null) return cache;
            var t0 = Time.realtimeSinceStartup;
            foreach (var pf in ta.people) if (pf != null) { var t = Bake(pf, ta.crowd); if (t != null) cache.Add(t); }
            Debug.Log($"Menschenmenge gebacken: {cache.Count} Figuren in {(Time.realtimeSinceStartup - t0) * 1000:0} ms");
            return cache;
        }

        // Name -> (Quelle, Haltung). Haltung: 0 normal, 1 sitzen, 2 reitend, 3 sitzen+Arme hoch
        static readonly (string name, string src, int pose, bool loop)[] Plan =
        {
            ("Walk", "Walk", 0, true), ("Run", "Run", 0, true), ("Idle", "Idle", 0, true), ("Idle_Neutral", "Idle_Neutral", 0, true),
            ("Wave", "Wave", 0, true), ("Interact", "Interact", 0, false), ("Punch_Right", "Punch_Right", 0, false),
            ("Idle_Gun_Pointing", "Idle_Gun_Pointing", 0, true), ("Idle_Gun_Shoot", "Idle_Gun_Shoot", 0, false), ("Gun_Shoot", "Gun_Shoot", 0, false),
            ("Sit", "Idle_Neutral", 1, true), ("Straddle", "Idle_Neutral", 2, true), ("SitArmsUp", "Idle_Neutral", 3, true),
        };

        static Template Bake(GameObject prefab, Material baseMat)
        {
            var go = Object.Instantiate(prefab); go.transform.SetPositionAndRotation(new Vector3(0, -500, 0), Quaternion.identity);
            try
            {
                var an = go.GetComponent<Animation>(); if (an == null) return null;
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(); if (smrs.Length == 0) return null;
                var root = go.transform;
                var T = new Template();

                // Knochen-Plaetze: gleicher Knochen mit gleicher Bindpose teilt sich einen Platz
                var slotBone = new List<Transform>(); var slotBind = new List<Matrix4x4>();
                int SlotOf(Transform b, Matrix4x4 bp)
                {
                    for (int i = 0; i < slotBone.Count; i++)
                        if (slotBone[i] == b && Near(slotBind[i], bp)) return i;
                    slotBone.Add(b); slotBind.Add(bp); return slotBone.Count - 1;
                }

                // Mesh zusammenbauen (im Raum der Wurzel; Bindposen beziehen sich auf den Raum der jeweiligen Renderer)
                var verts = new List<Vector3>(); var nors = new List<Vector3>(); var idx = new List<Vector4>(); var wts = new List<Vector4>(); var pal = new List<Vector2>(); var tris = new List<int>();
                var matSlot = new Dictionary<Material, int>(); int palN = 0;
                foreach (var r in smrs)
                {
                    var m = r.sharedMesh; if (m == null) continue;
                    string rn = (r.name + " " + m.name).ToLowerInvariant();
                    if (rn.Contains("skate") || rn.Contains("board") || rn.Contains("sword") || rn.Contains("gun") || rn.Contains("weapon")) continue;   // keine Bretter oder Waffen auf dem Rummel
                    var bones = r.bones; var bind = m.bindposes;
                    var map = new int[bones.Length];
                    for (int b = 0; b < bones.Length; b++) map[b] = SlotOf(bones[b], bind[b]);
                    var v = m.vertices; var n = m.normals; var bw = m.boneWeights;
                    int baseV = verts.Count;
                    for (int i = 0; i < v.Length; i++)
                    {
                        verts.Add(v[i]); nors.Add(n.Length > i ? n[i] : Vector3.up);
                        var w = bw.Length > i ? bw[i] : new BoneWeight { weight0 = 1 };
                        idx.Add(new Vector4(map[w.boneIndex0], map[w.boneIndex1], map[w.boneIndex2], map[w.boneIndex3]));
                        float s = w.weight0 + w.weight1 + w.weight2 + w.weight3; if (s <= 0) s = 1;
                        wts.Add(new Vector4(w.weight0 / s, w.weight1 / s, w.weight2 / s, w.weight3 / s));
                        pal.Add(Vector2.zero);
                    }
                    var mats = r.sharedMaterials;
                    for (int sm = 0; sm < m.subMeshCount; sm++)
                    {
                        var mat = sm < mats.Length ? mats[sm] : null; int ps = 0;
                        if (mat != null && !matSlot.TryGetValue(mat, out ps))
                        {
                            ps = Mathf.Min(palN, 15); matSlot[mat] = ps; palN++;
                            Color c = mat.HasProperty("baseColorFactor") ? mat.GetColor("baseColorFactor") : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.gray;
                            T.palette[ps] = new Vector4(c.r, c.g, c.b, 1);
                            string nm = mat.name.ToLowerInvariant();
                            T.cloth[ps] = !(nm.Contains("skin") || nm.Contains("eye") || nm.Contains("hair") || nm.Contains("brow") || nm.Contains("moustache") || nm.Contains("beard") || nm.Contains("teeth") || nm.Contains("mouth") || nm.Contains("black"));
                        }
                        var st = m.GetTriangles(sm);
                        foreach (int i in st) { tris.Add(baseV + i); pal[baseV + i] = new Vector2(ps, 0); }
                    }
                }
                T.slots = slotBone.Count;
                var mesh = new Mesh { name = prefab.name + " Menge", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(verts); mesh.SetNormals(nors); mesh.SetUVs(0, idx); mesh.SetUVs(1, wts); mesh.SetUVs(2, pal); mesh.SetTriangles(tris, 0);
                mesh.bounds = new Bounds(new Vector3(0, 1, 0), new Vector3(3.2f, 3.2f, 3.2f));
                mesh.UploadMeshData(true);
                T.mesh = mesh;

                // Bilder backen
                Transform Find(string n) { foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == n) return t; return null; }
                Transform ulL = Find("UpperLeg.L"), llL = Find("LowerLeg.L"), endL = Find("LowerLeg.L_end"), ftL = Find("Foot.L");
                Transform ulR = Find("UpperLeg.R"), llR = Find("LowerLeg.R"), endR = Find("LowerLeg.R_end"), ftR = Find("Foot.R");
                Transform uaL = Find("UpperArm.L"), laL = Find("LowerArm.L"), wrL = Find("Wrist.L"), uaR = Find("UpperArm.R"), laR = Find("LowerArm.R"), wrR = Find("Wrist.R");
                var data = new List<Color>(); var hpR = new List<Vector3>(); var hpL = new List<Vector3>(); var hrR = new List<Quaternion>(); var hrL = new List<Quaternion>();
                var clipByName = new Dictionary<string, AnimationClip>();
                foreach (AnimationState s in an) { if (s.clip == null) continue; string n = s.name; int bar = n.LastIndexOf('|'); clipByName[bar >= 0 ? n.Substring(bar + 1) : n] = s.clip; }
                int frame = 0;
                if (clipByName.TryGetValue("Idle", out var idle)) { idle.SampleAnimation(go, 0); T.hipHeight = ulL != null ? ulL.position.y - root.position.y : .9f; }
                foreach (var p in Plan)
                {
                    if (!clipByName.TryGetValue(p.src, out var clip)) continue;
                    int cnt = Mathf.Clamp(Mathf.CeilToInt(clip.length * Fps), 1, p.pose > 0 ? 16 : 40);
                    var ci = new Clip { start = frame, count = cnt, length = clip.length, loop = p.loop };
                    for (int f = 0; f < cnt; f++)
                    {
                        float t = p.loop ? f / (float)cnt * clip.length : (cnt > 1 ? f / (float)(cnt - 1) * clip.length : 0);
                        clip.SampleAnimation(go, t);
                        if (p.pose > 0)
                        {
                            Vector3 fw = root.forward, dn = -root.up, rt = root.right;
                            for (int sd = -1; sd <= 1; sd += 2)
                            {
                                Transform ul = sd < 0 ? ulL : ulR, ll = sd < 0 ? llL : llR, end = sd < 0 ? endL : endR, ft = sd < 0 ? ftL : ftR;
                                bool str = p.pose == 2;
                                Aim(ul, ll, str ? fw * .35f + dn * .8f + rt * sd * .42f : fw * .97f + dn * .12f + rt * sd * .1f);
                                Aim(ll, end, str ? dn + fw * .15f : dn * .9f + fw * .3f);
                                if (ft != null && end != null) ft.position = end.position;
                            }
                            if (p.pose == 3)
                            {
                                float sway = Mathf.Sin(f / (float)cnt * Mathf.PI * 2) * .15f;
                                Aim(uaL, laL, root.up - rt * (.35f + sway)); Aim(laL, wrL, root.up - rt * .2f);
                                Aim(uaR, laR, root.up + rt * (.35f - sway)); Aim(laR, wrR, root.up + rt * .2f);
                            }
                        }
                        var inv = root.worldToLocalMatrix;
                        for (int s = 0; s < slotBone.Count; s++)
                        {
                            var M = inv * slotBone[s].localToWorldMatrix * slotBind[s];
                            data.Add(new Color(M.m00, M.m01, M.m02, M.m03)); data.Add(new Color(M.m10, M.m11, M.m12, M.m13)); data.Add(new Color(M.m20, M.m21, M.m22, M.m23));
                        }
                        hpR.Add(wrR != null ? root.InverseTransformPoint(wrR.position) : Vector3.up); hrR.Add(wrR != null ? Quaternion.Inverse(root.rotation) * wrR.rotation : Quaternion.identity);
                        hpL.Add(wrL != null ? root.InverseTransformPoint(wrL.position) : Vector3.up); hrL.Add(wrL != null ? Quaternion.Inverse(root.rotation) * wrL.rotation : Quaternion.identity);
                        frame++;
                    }
                    T.clips[p.name] = ci;
                }
                const int W = 1024;
                int h = Mathf.Max(1, Mathf.CeilToInt(data.Count / (float)W));
                while (data.Count < W * h) data.Add(Color.clear);
                var tex = new Texture2D(W, h, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = prefab.name + " Animation" };
                tex.SetPixels(data.ToArray()); tex.Apply(false, true);
                T.mat = new Material(baseMat) { name = prefab.name + " Menge" };
                T.mat.SetTexture("_AnimTex", tex); T.mat.SetFloat("_Slots", T.slots);
                T.handPosR = hpR.ToArray(); T.handPosL = hpL.ToArray(); T.handRotR = hrR.ToArray(); T.handRotL = hrL.ToArray();
                return T;
            }
            finally { Object.Destroy(go); }
        }

        static bool Near(Matrix4x4 a, Matrix4x4 b) { for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > 1e-4f) return false; return true; }
        static void Aim(Transform bone, Transform child, Vector3 want)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-10f) return;
            bone.rotation = Quaternion.FromToRotation(cur, want.normalized) * bone.rotation;
        }
    }

    /// <summary>Spielt gebackene Animationen einer Figur ab (Bildwahl + Ueberblendung ueber den Shader).</summary>
    public class CrowdAnim
    {
        readonly CrowdBank.Template T; readonly MeshRenderer mr; readonly Material mat;
        static readonly int idFrame = Shader.PropertyToID("_Frame");
        static readonly int[] idCol = { Shader.PropertyToID("_C0"), Shader.PropertyToID("_C1"), Shader.PropertyToID("_C2"), Shader.PropertyToID("_C3"), Shader.PropertyToID("_C4"), Shader.PropertyToID("_C5"),
            Shader.PropertyToID("_C6"), Shader.PropertyToID("_C7"), Shader.PropertyToID("_C8"), Shader.PropertyToID("_C9"), Shader.PropertyToID("_C10"), Shader.PropertyToID("_C11") };
        Vector4 last = new Vector4(-1, -1, -1, 0);
        CrowdBank.Clip cur, prev; float time, prevTime, fade, fadeLen = .2f; public float speed = 1; public string Name { get; private set; }
        public Transform handR, handL;   // folgen den gebackenen Handgelenken (fuer Requisiten)
        public int Frame { get; private set; }

        public CrowdAnim(CrowdBank.Template t, MeshRenderer r, Vector4[] palette)
        {
            T = t; mr = r; mat = new Material(t.mat); mr.sharedMaterial = mat; made.Add(mat);
            for (int i = 0; i < idCol.Length && i < palette.Length; i++) mat.SetVector(idCol[i], palette[i]);
        }
        static readonly List<Material> made = new List<Material>();
        /// <summary>Materialien der vorigen Menge freigeben (beim Neuaufbau der Welt).</summary>
        public static void ReleaseAll() { foreach (var m in made) if (m != null) Object.Destroy(m); made.Clear(); }
        public bool Has(string n) => T.clips.ContainsKey(n);
        public void Play(string n, float fadeTime = .2f, bool restart = false)
        {
            if (!T.clips.TryGetValue(n, out var c)) return;
            if (c == cur && !restart) return;
            prev = cur; prevTime = time; cur = c; time = 0; Name = n; fade = prev != null ? fadeTime : 0; fadeLen = Mathf.Max(.01f, fadeTime);
        }
        public void Seek01(float x) { if (cur != null) time = x * cur.length; }
        public bool Done => cur == null || (!cur.loop && time >= cur.length);
        public bool Loop => cur != null && cur.loop;
        int FrameOf(CrowdBank.Clip c, float t, out int next, out float frac)
        {
            float f = t * CrowdBank.Fps;
            if (c.loop) { f = Mathf.Repeat(f, c.count); int a = (int)f; next = c.start + (a + 1) % c.count; frac = f - a; return c.start + a; }
            f = Mathf.Clamp(t / Mathf.Max(.001f, c.length) * (c.count - 1), 0, c.count - 1); int b = (int)f; next = c.start + Mathf.Min(b + 1, c.count - 1); frac = f - b; return c.start + b;
        }
        public void Tick(float dt)
        {
            if (cur == null) return;
            time += dt * speed; if (prev != null) prevTime += dt;
            if (!mr.isVisible && handR == null && handL == null) { if (fade > 0) fade -= dt; return; }   // unsichtbar: nur Zeit weiterlaufen lassen
            int a = FrameOf(cur, time, out int nx, out float fr);
            float fa, fb, ft;
            if (fade > 0 && prev != null)
            {
                fade -= dt; int pa = FrameOf(prev, prevTime, out _, out _);
                fa = pa; fb = a; ft = 1f - Mathf.Clamp01(fade / fadeLen);
            }
            else { fa = a; fb = nx; ft = fr; prev = null; }
            Frame = a;
            var fv = new Vector4(fa, fb, Mathf.Round(ft * 16f) / 16f, 0);
            if (fv != last) { mat.SetVector(idFrame, fv); last = fv; }
            if (handR != null) { handR.localPosition = T.handPosR[a]; handR.localRotation = T.handRotR[a]; }
            if (handL != null) { handL.localPosition = T.handPosL[a]; handL.localRotation = T.handRotL[a]; }
        }
    }
}
