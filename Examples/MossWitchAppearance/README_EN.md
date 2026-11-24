# Moss Witch Outfit Appearance Pack

[简体中文](README.md)

This data pack adds the Moss Witch Robe and Hat to the Appearances menu. Wear either piece on its own, or combine the outfit with a custom body such as Upright Mouse.

1. Build and install the [Appearance Mod Framework](../AppearanceFramework/README_EN.md#build-and-try-it).
2. Copy `mod.json`, `appearance.json`, `appearance-upgrades.json`, the [cover image](preview.png), and `Assets/witch-robe-win64.bundle` to `Mods/MossWitchAppearance`, keeping the `Assets/` folder. The included bundle is for Windows x64.
3. Enable the framework and the Moss Witch Outfit Appearance Pack, then restart the game. On an island, press **F10** and select the robe and hat under Clothing.

Change an entry's `color` in [appearance.json](appearance.json) to recolor the fabric. The `parts` show how to attach the robe and sleeves separately while taking their placement and materials from game equipment. The [craftable Moss Witch outfit example](../WitchRobe/README_EN.md#edit-the-models-and-colors) has the editable models and build steps.

You can choose the appearance without crafting an item. With the craftable outfit mod also enabled, the pack can sync the appearance when you equip the actual crafted item. The entry's `nativeOwner` and `nativeItem` fields connect the appearance with that item. Remove both if you copy this example for clothing without its own equipment item.
