# Moss Witch Outfit

[简体中文](README.md)

This example adds a Moss Witch Robe and a Moss Witch Hat. The robe, sleeves, and hat use separate meshes included in the mod. The fabric is teal (`#267B73`). The hat trim and lighting use the game's materials, and the items use its existing equipment system.

For the same look in the Appearances menu (F10), enable the Appearance Mod Framework and the companion pack. [Moss Witch appearance pack](../MossWitchAppearance/README_EN.md)

## Try it in the game

Use a Windows x64 build that supports [UUID mod manifests](../../Docs/Manifest.md) and install a .NET SDK. The model bundle is included, so you only need to compile the C# code to try it.

1. Open a terminal in this example's directory and change the path below to `CozyIsland_Data/Managed` in your game installation. The command builds `Code/CozyMods.WitchRobe.dll`.

   ```powershell
   dotnet build Source/WitchRobe.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. In the game, go to **Mods → Mod Creation → Open Mod Folder**. If an existing mod folder opens, go up one level to `Mods`. Create a `WitchRobe` folder and copy in `mod.json`, `Assets/witch-robe-win64.bundle`, and the built `Code/CozyMods.WitchRobe.dll`, keeping their directory structure.
3. Enable **青苔魔法套装** (Moss Witch Outfit) and fully restart the game. On a test island, craft the robe or hat at the Basic Workbench. Each recipe costs one log. Interact with the crafted clothing to equip it.

## Edit the models and colors

1. Add `Source/UnityProject` in Unity Hub and open it with **Unity 2022.3.62f3c1** and Windows Build Support installed. The three FBX models can be edited separately. [Model files](Source/UnityProject/Assets/WitchRobe/Models)
2. To recreate the prefab from the FBX files, choose **Cozy Island → Create Witch Robe Prefab**. Choose **Cozy Island → Build Witch Robe Bundle (Windows)** to rebuild `Assets/witch-robe-win64.bundle`. In-game placement uses the original equipment attachment transforms.
3. To recolor the fabric, edit `ClothColor` in `WitchRobe.cs` and rebuild. At runtime, the mod copies the original material and sets its `_AlbedoColor`. The hat keeps its other material slots. Copy the updated DLL or bundle into your test mod and fully restart the game. [Runtime code](Source/WitchRobe.cs)

## Multiplayer

Install and enable the same version of this example on the host and guest. Once the guest is on the island, equip or remove the outfit on the host to see it update on the guest. The example registers the clothing as items with `CozyModRegistry`. The game sends equipment IDs, and each player loads the models from their local bundle.

A loopback test with the prototype manifest confirmed recipe registration and live equip and unequip in two separate Windows game processes. When a guest joins late, gear already equipped by the host may be missing from the initial update. The host can re-equip it after the guest is ready to sync it. Guests need this example to render the custom gear. Remote Steam and cross-platform sessions still need testing.
