# Multiplayer Appearance Framework

[简体中文](README.md)

The Multiplayer Appearance Framework lets players choose custom bodies and clothing in the game. Players running the framework in the same room can see one another's outfits. When the author permits sharing, the framework can also receive missing models from the room. Each wearable appearance comes from a creator's data pack.

## Build and try it

1. Use a game version that supports [UUID mod manifests](../../Docs/Manifest.md) and install the .NET SDK. Run the command from this directory, replacing the path with `CozyIsland_Data/Managed` in your game installation.

   ```powershell
   dotnet build Source/AppearanceFramework.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. In the game, open **Mods → Mod Creator → Open Work Folder**. If it opens an existing mod, go up one level to `Mods`. Create an `AppearanceFramework` folder there and copy in `mod.json` and the generated `Code/CozyMods.AppearanceFramework.dll`, keeping their paths.
3. Place your chosen appearance pack in another folder under `Mods`. Enable both mods and restart the game. Enter an island, press **F10**, and choose an appearance under Body or Clothing. The window also lets you change the shortcut or restore the default appearance.

The host and guests need the same framework version. Received appearances go into a temporary cache. If the author allows saving, select the appearance under Room Appearances and choose **Save and Equip** to use it next time. Turning off automatic reception stops the local player from fetching missing assets. It also stops the host from fetching missing assets on behalf of guests. Installed or cached appearances remain available.

## Make an appearance pack

A working appearance pack has three files in the same mod folder. Keep the asset bundle path relative to that folder.

| File | What to put in it |
| --- | --- |
| `mod.json` | Create a UUID for your mod and give it a name. Add the framework UUID `a2f648bf-1066-45da-91ea-1c7ca2f2511e` and minimum version `0.1.0` to `dependencies`. [Mod manifest](../../Docs/Manifest.md) |
| `appearance.json` | Set `schemaVersion: 1` and fill in `id`, `name`, `author`, `platform`, `bundle`, and `entries`. |
| `Assets/model-name.bundle` | Use Unity to bundle one or more prefabs for the framework to attach to the character. Build for the platform your players use. |

Use `win64` or `macos` for `platform`. Each entry has an `id` and `name`, a wearable `slot`, a `prefab` path inside the bundle, and a list of `parts` to attach. Each part's `path` is relative to the prefab root. Its `anchor` names an attachment point on the game character.

- Use the `Body` slot for a new body. Attach a head at `Face`, parts that follow hair visibility at `Hair`, and the torso at `Torso`. If a part is positioned relative to one anchor and should move with another, set `followAnchor`.
- Clothing slots are `Hat`, `Cloth`, `Shoe`, `Glove`, and `Knee`. For a top, attach the body and sleeves at `Cloth` and `Sleeve`. A part can set `position`, quaternion `rotation`, and `scale`. Use `hideOriginal` to cover the game's original visuals at an anchor.
- Set `color` on an entry or a part to tint it, for example `"#C8875C"`. To use different game materials in different model slots, add `materials` keyed by slot name. Each definition names a game prefab ID, its material, and an optional tint. A part's `materialOverrides` can redirect a slot to another definition in the same palette.
- A body can use `bodyMaterial` for exposed original skin and `hideBody` for original parts such as the neck. Use `blinkBlendShape` to pick an eyelid shape for blinking. For a skinned tail or similar part, `followThrough` describes the bones that sway as the character moves.
- A clothing part can use `nativeEquipment` to take placement and materials from an existing game item. To also sync the appearance when a player wears a custom craftable item, set the entry's `nativeOwner` (the registering mod UUID) and `nativeItem` (the item ID).

Use `share` to allow room sharing and `allowSave` to let recipients keep the appearance. Each pack may be up to 8 MiB. The framework reads visual data such as meshes, bones, and textures, then supplies its own attachment and animation. Scripts and animation controllers on an asset prefab do not run in the game. [Entry fields](Source/AppearanceData.cs) · [Part fields](Source/PlacementVisuals.cs)
