using System;
using CozyIsland.Modding;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CozyMods
{
    public sealed class ColorDither : ICozyMod
    {
        private static FullScreenPassRendererFeature feature;
        private static RetroControls controls;

        public void OnLoad(CozyModContext context)
        {
            var bundle = AssetBundle.LoadFromFile(context.ReadPath("Assets/colordither.bundle"));
            if (bundle == null) throw new InvalidOperationException("Could not load the retro filter bundle.");
            var shader = bundle.LoadAsset<Shader>("assets/cozycolordither.shader");
            bundle.Unload(false);
            if (shader != null) context.AddCleanup(() => Object.Destroy(shader));
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("The retro filter shader is unavailable on this graphics device.");

            var material = new Material(shader);
            context.AddCleanup(() => Object.Destroy(material));
            var pass = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            pass.name = "Cozy Island Retro Filters";
            pass.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            pass.fetchColorBuffer = true;
            pass.passMaterial = material;
            pass.Create();
            feature = pass;
            context.AddCleanup(() =>
            {
                controls?.Save();
                feature = null;
                controls = null;
                pass.Dispose();
                Object.Destroy(pass);
            });

            controls = context.Root.AddComponent<RetroControls>();
            controls.Initialize(material);
            var method = AccessTools.Method(typeof(ScriptableRenderer), "AddRenderPasses",
                new[] { typeof(RenderingData).MakeByRefType() });
            context.Harmony.Patch(method, postfix: new HarmonyMethod(typeof(ColorDither), nameof(AddFilterPass)));
            context.Log("Retro Filters ready. Open 复古画面 on the main menu or press F9.");
        }

        public void OnUnload() { }

        private static void AddFilterPass(ScriptableRenderer __instance, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (feature == null || controls == null || !controls.isActiveAndEnabled || !controls.HasEffects ||
                camera.cameraType != CameraType.Game || camera.renderType != CameraRenderType.Base || camera.camera != Camera.main)
                return;

            feature.requirements = ScriptableRenderPassInput.Color;
            if (controls.UsesDepth) feature.requirements |= ScriptableRenderPassInput.Depth;
            feature.AddRenderPasses(__instance, ref renderingData);
        }
    }
}
