using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Achterbahn
{
    /// <summary>Himmel (Poly-Haven-HDRI), Umgebungslicht, Sonne, Schatten und Nachbearbeitung.</summary>
    public static class Look
    {
        static Volume vol;

        public static void Apply(Camera cam, Light sun, TrackDef d)
        {
            var ta = ThemeAssets.Get();
            if (ta != null && ta.sky != null)
            {
                RenderSettings.skybox = ta.sky;
                cam.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1f;
                DynamicGI.UpdateEnvironment();
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = d.fog;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.Lerp(d.sky, Color.white, .45f);
            }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            // Luftperspektive: Ferne wird blasser und blaeulich
            RenderSettings.fogColor = new Color(.74f, .82f, .9f);
            RenderSettings.fogStartDistance = 120; RenderSettings.fogEndDistance = 1300;

            sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            sun.color = new Color(1f, .95f, .87f); sun.intensity = 2.2f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .9f;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 220f;
                urp.shadowCascadeCount = 4;
                urp.supportsCameraDepthTexture = true;   // fuer Wassertiefe und Uferschaum
            }

            var cd = cam.GetUniversalAdditionalCameraData();
            cd.renderPostProcessing = true;
            cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cd.antialiasingQuality = AntialiasingQuality.High;
            cam.farClipPlane = 3000f;

            if (vol == null)
            {
                var go = new GameObject("Look");
                Object.DontDestroyOnLoad(go);
                vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 50;
                var p = ScriptableObject.CreateInstance<VolumeProfile>();
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                var bl = p.Add<Bloom>(true); bl.threshold.Override(1.1f); bl.intensity.Override(.3f); bl.scatter.Override(.6f);
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(.2f); ca.contrast.Override(8f); ca.saturation.Override(4f);
                p.Add<Vignette>(true).intensity.Override(.18f);
                vol.profile = p;
            }
        }
    }
}
