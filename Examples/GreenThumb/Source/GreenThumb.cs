using System;
using System.IO;
using CozyIsland.Modding;
using HarmonyLib;
using UnityEngine;
namespace CozyMods.Examples {
    // This behavior belongs to the author package, never to the game loader.
    public sealed class GreenThumb : ICozyMod {
        [Serializable] sealed class Settings { public float growthMultiplier = 2; public float waterUseMultiplier = .5f; }
        static CozyModContext running;
        static Settings settings;
        struct Snapshot { public bool active, growing; public float clock, water; }
        public void OnLoad(CozyModContext context) {
            var config = JsonUtility.FromJson<Settings>(File.ReadAllText(context.ReadPath("garden.json")));
            if (config == null || !Valid(config.growthMultiplier) || !Valid(config.waterUseMultiplier)) throw new InvalidDataException("Garden multipliers must be finite values between 0.1 and 10");
            running = context; settings = config;
            context.Harmony.Patch(AccessTools.Method(typeof(CozyFallowLand), "Update"),
                prefix: new HarmonyMethod(typeof(GreenThumb), nameof(BeforeUpdate)),
                postfix: new HarmonyMethod(typeof(GreenThumb), nameof(AfterUpdate)));
        }
        public void OnUnload() { running = null; settings = null; }
        static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= .1f && value <= 10;
        static void BeforeUpdate(CozyFallowLand __instance, out Snapshot __state) {
            __state = new Snapshot();
            if (running == null || running.IsDisposed || !running.IsHost) return;
            __state = new Snapshot { active = true, growing = __instance.isGrowing, clock = __instance.growTimeCount, water = __instance.waterNum };
        }
        static void AfterUpdate(CozyFallowLand __instance, Snapshot __state) {
            if (!__state.active || settings == null) return;
            // Preserve the game's lifecycle/harvest checks. Extra progress is observed
            // by those checks on the next ordinary Update, at most one frame later.
            if (__state.growing && __instance.isGrowing && __instance.growTimeCount >= __state.clock)
                __instance.growTimeCount += (__instance.growTimeCount - __state.clock) * (settings.growthMultiplier - 1);
            if (__state.water > 0 && __instance.waterNum <= __state.water)
                __instance.waterNum = Mathf.Max(0, __state.water - (__state.water - __instance.waterNum) * settings.waterUseMultiplier);
        }
    }
}
