using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Achterbahn
{
    /// <summary>Himmel (Poly-Haven-HDRI), Umgebungslicht, Sonne, Schatten und Nachbearbeitung. Der Rummel spielt in der Daemmerung.</summary>
    public static class Look
    {
        static Volume vol; static Bloom bloom; static ColorAdjustments color; static Vignette vignette;

        public static void Apply(Camera cam, Light sun, TrackDef d)
        {
            var ta = ThemeAssets.Get();
            bool dusk = d.theme == "rummel" && ta != null && ta.duskSky != null;
            var sky = dusk ? ta.duskSky : ta != null ? ta.sky : null;
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                cam.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = dusk ? .75f : 1f;
                DynamicGI.UpdateEnvironment();
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = d.fog;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.Lerp(d.sky, Color.white, .45f);
            }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            if (dusk)
            {
                // Blaue Stunde: violetter Dunst, tiefe rosa Sonne knapp ueber dem Horizont
                RenderSettings.fogColor = new Color(.27f, .27f, .4f);
                RenderSettings.fogStartDistance = 60; RenderSettings.fogEndDistance = 900;
                sun.transform.rotation = Quaternion.Euler(6f, DuskSunYaw, 0f);
                sun.color = new Color(1f, .58f, .45f); sun.intensity = .9f;
            }
            else
            {
                // Luftperspektive: Ferne wird blasser und blaeulich
                RenderSettings.fogColor = new Color(.74f, .82f, .9f);
                RenderSettings.fogStartDistance = 120; RenderSettings.fogEndDistance = 1300;
                sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
                sun.color = new Color(1f, .95f, .87f); sun.intensity = 2.2f;
            }
            sun.shadows = LightShadows.Soft; sun.shadowStrength = dusk ? .75f : .9f;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 220f;
                urp.shadowCascadeCount = 4;
                urp.supportsCameraDepthTexture = true;   // fuer Wassertiefe, Uferschaum, weiche Lichthoefe
            }

            var cd = cam.GetUniversalAdditionalCameraData();
            cd.renderPostProcessing = true;
            cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cd.antialiasingQuality = AntialiasingQuality.High;
            cam.farClipPlane = 3000f;
            cam.allowHDR = true;

            if (vol == null)
            {
                var go = new GameObject("Look");
                Object.DontDestroyOnLoad(go);
                vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 50;
                var p = ScriptableObject.CreateInstance<VolumeProfile>();
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                bloom = p.Add<Bloom>(true);
                color = p.Add<ColorAdjustments>(true);
                vignette = p.Add<Vignette>(true);
                vol.profile = p;
            }
            if (dusk)
            {
                bloom.threshold.Override(.8f); bloom.intensity.Override(1.15f); bloom.scatter.Override(.72f);
                color.postExposure.Override(.25f); color.contrast.Override(14f); color.saturation.Override(18f);
                vignette.intensity.Override(.26f);
            }
            else
            {
                bloom.threshold.Override(1.1f); bloom.intensity.Override(.3f); bloom.scatter.Override(.6f);
                color.postExposure.Override(.2f); color.contrast.Override(8f); color.saturation.Override(4f);
                vignette.intensity.Override(.18f);
            }
        }

        /// <summary>Richtung des Abendrots im Daemmerungs-Himmel (Grad), passend zur Drehung der HDRI.</summary>
        public static float DuskSunYaw = 155f;
    }
}
