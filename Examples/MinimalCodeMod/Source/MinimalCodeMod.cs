using System;
using System.Runtime.CompilerServices;
using CozyIsland.Modding;
using HarmonyLib;

namespace CozyMods {
    public sealed class MinimalCodeMod : ICozyMod {
        CozyModContext running;
        int updates;

        public void OnLoad(CozyModContext context) {
            running = context;
            updates = 0;
            int before = Probe.ReadValue();
            if (before != 21) throw new InvalidOperationException("Probe was already patched before this load: " + before);

            context.Harmony.Patch(AccessTools.Method(typeof(Probe), nameof(Probe.ReadValue)),
                postfix: new HarmonyMethod(typeof(MinimalCodeMod), nameof(DoubleResult)));
            int after = Probe.ReadValue();
            if (after != 42) throw new InvalidOperationException("Harmony probe failed: " + after);
            context.Log("Loaded; Harmony probe before=" + before + ", after=" + after);

            context.OnUpdate(deltaTime => {
                updates++;
                if (updates == 1) {
                    if (Probe.ReadValue() != 42) throw new InvalidOperationException("Patch did not survive OnLoad");
                    context.Log("First update callback; Harmony probe=42");
                }
            });
            context.OnSceneLoaded(scene => context.Log("Scene callback: " + scene.name));
            context.AddCleanup(() => context.Log("Cleanup callback; observed updates=" + updates));
        }

        public void OnUnload() {
            if (running != null) running.Log("OnUnload; context will stop callbacks and remove this owner's patches");
            running = null;
        }

        static void DoubleResult(ref int __result) { __result *= 2; }
    }

    // Patch only this sample's own code. No game type or game-state change is needed.
    internal static class Probe {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadValue() { return 21; }
    }
}
