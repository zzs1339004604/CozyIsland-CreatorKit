# Appearance Mod Framework

[简体中文](README.md)

The Appearance Mod Framework supports character skins and outfits from mods such as Upright Mouse and the Moss Witch Outfit Appearance Pack. Enable the framework and an appearance pack to choose a look in the Appearances menu. Make ordinary skins, hats, and clothing appearances with a GLB model and configuration. Windows and macOS use the same files.

## Install and try it

Install and enable Appearance Mod Framework 0.2.0 with GLB support, then install an appearance pack. Restart the game and open **Appearances** on the main menu or press F10 on an island. Start with [Sprout Cap](../SproutCap/README_EN.md).

### Build and try it

Use these steps when maintaining the framework code. The same framework DLL supports Windows and macOS.

1. Use a game version that supports [UUID mod manifests](../../Docs/Manifest.md) and install the .NET SDK. Run the command from this directory, replacing the path with `CozyIsland_Data/Managed` on Windows or `CozyIsland.app/Contents/Resources/Data/Managed` on macOS.

   ```powershell
   dotnet build Source/AppearanceFramework.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. In the game, open **Mods → Mod Creator → Open Work Folder**. If it opens an existing mod, go up one level to `Mods`. Create an `AppearanceFramework` folder there and copy in `mod.json`, the [cover image](preview.png), and the generated `Code/CozyMods.AppearanceFramework.dll`, keeping their paths.
3. Place your chosen appearance pack in another folder under `Mods`. Enable both mods and restart the game. Choose **Appearances** on the main menu to select a body and clothing before entering an island. Press **F10** during play to open the same window. You can also change the shortcut or restore the default appearance there.

The host and guests need the same framework version to share appearances in a room. Received appearances go into a temporary cache. If the author allows saving, select the appearance under Room Appearances and choose **Save and Equip** to use it next time. Turning off automatic reception stops the local player from fetching missing assets. It also stops the host from fetching missing assets on behalf of guests. Installed or cached appearances remain available.

## Make an appearance pack

Copy the three runtime files from [Sprout Cap](../SproutCap/README_EN.md) into a new mod folder, then replace the model and edit the configuration.

| File | What to put in it |
| --- | --- |
| `mod.json` | Keep mod manifest `schemaVersion: 1`. Give a new mod its own UUID and name, and add the framework UUID `a2f648bf-1066-45da-91ea-1c7ca2f2511e` with minimum version `0.2.0` to `dependencies`. [Mod manifest](../../Docs/Manifest.md) |
| `appearance.json` | Set `schemaVersion: 2` and fill in `id`, `name`, `author`, `model`, and `entries`. For example, set `model` to `Assets/hat.glb`. |
| `Assets/hat.glb` | Export a self-contained glTF Binary from Blender or another modeling tool. Export a single scene, keep node names, and embed any PNG or JPEG textures. |

In Blender, choose **File → Export → glTF 2.0**, select **glTF Binary (.glb)**, and export the relevant model nodes. Use smooth shading and preserve normals for curved surfaces. Retain required shape keys and skin weights, and turn off animation and compression. You can keep FBX as editable source and export a GLB from Blender for the game. [Blender export guide](https://docs.blender.org/manual/en/latest/addons/scene_gltf2.html)

Put the three files under the game’s `Mods/<mod folder>` and enable the framework and appearance pack. Equip it under **Appearances → My Appearances**, then enter an island to check size, color, and placement. Use a game version that includes `.glb` files in Workshop uploads when publishing. [Try and publish (Chinese)](../../Docs/Workshop.md#发布和更新)

`author` in `mod.json` appears on the Mods page. `author` in `appearance.json` travels with the appearance and appears in the Appearances menu, including for players who receive it in a room. Use the same credit in both places when the mod and appearance share an author.

When updating a published pack, you can include an `appearance-upgrades.json` file with a JSON array of previous SHA-256 digests. To calculate one, hash the old catalog's byte count as a four-byte little-endian integer, followed by the original bytes of `appearance.json` and then the old model bytes (GLB or AssetBundle). The framework carries over equipped selections when the updated pack is installed and the entry ID and slot match. Remove the example's upgrade file when you use it to create a new pack.

Each entry has an `id`, `name`, wearable `slot`, and list of `parts` to attach. A part’s `path` uses case-sensitive model node names such as `Head/Ears`, and `anchor` names an attachment point on the game character. Paths start at the GLB model root. Set an entry’s optional `prefab` to a relative node path to use that node as the start. Keep a skinned part’s mesh and bones under the same part node.

- Use the `Body` slot for a new body. Attach a head at `Face`, parts that follow hair visibility at `Hair`, and the torso at `Torso`. If a part is positioned relative to one anchor and should move with another, set `followAnchor`.
- Clothing slots are `Hat`, `Cloth`, `Shoe`, `Glove`, and `Knee`. For a top, attach the body and sleeves at `Cloth` and `Sleeve`. A part can set `position`, quaternion `rotation`, and `scale`. Use `hideOriginal` to cover the game's original visuals at an anchor.
- GLB base colors and embedded textures use the game’s materials. Set `color` on an entry or a part to override its base color, for example `"#C8875C"`. To use different game materials in different model slots, add `materials` keyed by slot name. Each definition names a game prefab ID, its material, and an optional tint. A part's `materialOverrides` can redirect a slot to another definition in the same palette.
- A body can use `bodyMaterial` for exposed original skin and `hideBody` for original parts such as the neck. Use `blinkBlendShape` to pick an eyelid shape for blinking. For a skinned tail or similar part, `followThrough` describes the bones that sway as the character moves.
- A clothing part can use `nativeEquipment` to take placement and materials from an existing game item. To apply the appearance automatically when a player wears a custom craftable item, set the entry's `nativeOwner` (the registering mod UUID) and `nativeItem` (the item ID).

Set `share` to `true` to share a GLB appearance in a room. Also set `allowSave` to `true` to let recipients save and equip it. The model and catalog together may be up to 8 MiB. The framework supplies attachment, materials, and animation behavior. [Entry fields](Source/AppearanceData.cs) · [Part fields](Source/PlacementVisuals.cs)

## Existing AssetBundle appearances

The framework still supports `schemaVersion: 1` with `platform` (`win64` or `macos`), a `bundle` path, and Unity Prefab paths. Here too, `share` and `allowSave` control room sharing and saving. Build Unity-specific AssetBundles separately for each target platform. [Native asset authoring](../../Docs/Native-Authoring.md)
