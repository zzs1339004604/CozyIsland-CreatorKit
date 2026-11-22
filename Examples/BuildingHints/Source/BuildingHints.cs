using CozyIsland.Modding;
using HarmonyLib;

namespace CozyMods {
    public sealed class BuildingHints : ICozyMod {
        public void OnLoad(CozyModContext context) {
            context.Harmony.PatchAll(typeof(BuildingDescriptionPatch));
            context.Log("Building name hints enabled. Hold the small wooden hammer and focus a building.");
        }

        public void OnUnload() {
            // The context removes only this Mod's patches after OnUnload returns.
        }
    }

    [HarmonyPatch(typeof(PCDBuilding), nameof(PCDBuilding.interactDesc), MethodType.Getter)]
    internal static class BuildingDescriptionPatch {
        static void Postfix(PCDBuilding __instance, ref string __result) {
            if (__instance == null || string.IsNullOrWhiteSpace(__instance.buildingName)) return;
            var name = EasyLocalText.Localized(__instance.buildingName);
            if (string.IsNullOrWhiteSpace(name)) name = __instance.buildingName;
            __result = (__result ?? "") + " · " + name;
        }
    }
}
