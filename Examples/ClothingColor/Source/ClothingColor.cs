using System.Collections.Generic;
using CozyIsland.Modding;
using UnityEngine;

namespace CozyMods.Examples
{
    public sealed class ClothingColor : ICozyMod
    {
        // Red, green, blue and opacity, each from 0 to 255.
        private static readonly Color ShirtColor = new Color32(214, 82, 119, 255);
        private const string AlbedoColor = "_AlbedoColor";

        public void OnLoad(CozyModContext context)
        {
            GameObject paintedSkeleton = null;
            var originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
            var paint = new MaterialPropertyBlock();

            // The local player does not exist until an island is loaded.
            context.OnUpdate(_ =>
            {
                if (paintedSkeleton != null) return;
                var player = PlayerIdentifier.TryGetInstance()?.GetPlayer(needOriginal: true);
                if (player == null) return;
                var skeleton = player.GetComponent<CozyPlayer>().pbSkeleton;
                if (skeleton == null) return;

                paintedSkeleton = skeleton;
                originalBlocks.Clear();
                foreach (var renderer in skeleton.GetComponentsInChildren<Renderer>(includeInactive: true))
                {
                    if (!IsStarterShirt(renderer)) continue;

                    // Keep the game's material and set its color for this renderer only.
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original);
                    originalBlocks.Add(renderer, original);

                    renderer.GetPropertyBlock(paint);
                    paint.SetColor(AlbedoColor, ShirtColor);
                    renderer.SetPropertyBlock(paint);
                }
                context.Log($"Recolored {originalBlocks.Count} starter shirt renderers.");
            });

            context.AddCleanup(() =>
            {
                foreach (var entry in originalBlocks)
                    if (entry.Key != null) entry.Key.SetPropertyBlock(entry.Value);
            });
        }

        public void OnUnload() { }

        private static bool IsStarterShirt(Renderer renderer) =>
            (renderer.name == "Human_Cloth_TShirt" && renderer.transform.parent.name == "OriBodyGFX") ||
            (renderer.name == "Human_Cloth_Sleeve" && renderer.transform.parent.name == "OriSleeve");
    }
}
