# Sprout Cap

[简体中文](README.md)

This wearable hat uses two JSON files and a GLB model exported from Blender. The same files work on Windows and macOS with a compatible game and Appearance Mod Framework 0.2.0.

1. Enable the [Appearance Mod Framework](../AppearanceFramework/README_EN.md) and open **Mods → Mod Creator → Open Work Folder**.
2. Create `SproutCap` under `Mods`. Copy `mod.json`, `appearance.json`, and `Assets/sprout-cap.glb`, keeping the `Assets/` folder.
3. Enable Sprout Cap and restart the game. Choose **Appearances → Clothing → Hat → My Appearances → 芽苗帽**, then equip it. Enter an island to see it on your character, or open Appearances while playing with F10.

Try changing the `color` for `Crown` and `Sprout` in your test copy of `appearance.json`, then restart and select the hat again. Import the example GLB into Blender to edit it, or supply your own exported GLB. Keep the object names or update `parts[].path` to match. For a new Workshop item, give `mod.json` a new UUID and update the mod and appearance identifiers in the two manifests. [Full appearance guide](../AppearanceFramework/README_EN.md#make-an-appearance-pack)
