# Moss Witch Outfit Appearance Pack

[简体中文](README.md)

Wear the Moss Witch Robe and Hat through the Appearances menu. Equip either piece on its own or pair them with a custom body such as Upright Mouse.

## Try it in the game

1. Install and enable [Appearance Mod Framework](../AppearanceFramework/README_EN.md) 0.2.0 or later. Then subscribe to the Moss Witch Outfit Appearance Pack and enable it. [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3845056621)
2. Restart the game. Open **Appearances** in the main menu or press **F10** on an island. Choose the robe and hat separately under **Clothing**.

To install from the repository, copy `mod.json`, `appearance.json`, `appearance-upgrades.json`, `preview.png`, and [Assets/moss-witch.glb](Assets/moss-witch.glb) to `Mods/MossWitchAppearance`. Keep the `Assets/` folder. Windows x64 and macOS use the same model.

## Change the colors or model

Change `materials.ClothTeal.color` in the relevant [appearance.json](appearance.json) entry to recolor the fabric. The hat's trim, lining, and buckle each use a material from the game. Import [moss-witch.glb](Assets/moss-witch.glb) into Blender to edit the robe, sleeves, or hat. If you rename a node, update the matching `parts` path. [Appearance data](../AppearanceFramework/README_EN.md#make-an-appearance-pack)

You can choose either piece directly in Appearances. If you also enable the [craftable Moss Witch outfit mod](../WitchRobe/README_EN.md), wearing the crafted item uses its matching appearance. The `nativeOwner` and `nativeItem` fields link the item to the appearance. If you copy this example for clothing without its own item, remove both fields and use your own UUID and entry IDs.
