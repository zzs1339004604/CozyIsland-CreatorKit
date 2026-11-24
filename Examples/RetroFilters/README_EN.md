# Retro Filters

[简体中文](README.md)

Give Cozy Island a pixelated, old-game look. Turn on any of the four effects, combine them, and see the changes live on the main menu. The filters affect the local player’s main camera. Buttons and text drawn in screen overlays stay sharp. [Preview](preview.png)

## Try it in the game

You need the .NET SDK and a Windows x64 game build that supports UUID mod manifests. The example includes a Windows asset bundle, so you can compile the C# and try it right away. [Manifest guide (Chinese)](../../Docs/Manifest.md) · [Compatible builds](../../PlayerIdentity.json)

If an older 2.0.0 Workshop copy reports a missing `displayName`, follow the steps below to build and try the current example with a UUID manifest. When updating an existing Workshop item, link the new package to the original Workshop ID in the game’s mod creation menu. [Workshop updates (Chinese)](../../Docs/Workshop.md#发布和更新)

1. Open a terminal at the repository root. Replace the path in the command with `CozyIsland_Data/Managed` from your game installation. The build creates `Examples/RetroFilters/Code/CozyMods.ColorDither.dll`.

   ```text
   dotnet build Examples/RetroFilters/Source/ColorDither.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. In the game, choose **Mods → Create Mods → Open mod folder**. If it opens an existing mod, go up one level to `Mods`. Create a `RetroFilters` folder there, then copy the DLL, manifest, asset bundle, and cover image into it as shown below.

   ```text
   Mods/
     RetroFilters/
       mod.json
       preview.png
       Assets/colordither.bundle
       Code/CozyMods.ColorDither.dll
   ```

3. Enable **复古画面** (Retro Filters) in the **Mods** list and restart if prompted. Choose **复古画面** at the top right of the main menu or press **F9** on your island. Move a slider to see the change immediately. Choose **完成** (Done) to save, **全部关闭** (Turn All Off) to return to the original look, or **恢复默认** (Restore Defaults) to apply the example’s recommended settings.

| Effect | Main controls |
| --- | --- |
| Color dithering | Strength, grain size, color flecks, and dithering color levels |
| Low resolution | Pixel height. Lower values produce larger blocks. |
| Color banding | Number of brightness levels. Lower values make the bands more distinct. |
| Outline | Adjustable outlines based on differences in scene depth. |

## Code and assets

- `ColorDither.cs` creates the filter material and uses Harmony to run the full-screen effect just after the main camera’s post-processing. The pass requests depth only for outlines and skips drawing when all effects are off. [Mod entry point](Source/ColorDither.cs)
- `RetroControls.cs` builds the settings panel with the game’s existing sliders, buttons, toggles, and font. It passes control changes to the filter material and saves them in local game preferences. Edit `RetroSettings` in the same file to try a different set of defaults. [Panel and settings](Source/RetroControls.cs)
- `CozyColorDither.shader` defines the four effects. The low-resolution option gives the finished image a blocky look while the game continues rendering at its original resolution. [Shader source](Source/UnityProject/Assets/CozyColorDither.shader)

## Rebuild the asset bundle

For C# changes, including default settings, rebuild the DLL. For shader changes, add `Source/UnityProject` in Unity Hub and open it with **Unity 2022.3.62f3c1**. The project is configured for **URP 14.0.12**. Save your changes and choose **Cozy Island → Build Windows Retro Filter Bundle**. Copy the rebuilt `Assets/colordither.bundle` to your local test mod, then quit the game completely and reopen it. [Bundle builder](Source/UnityProject/Assets/Editor/BuildColorDitherBundle.cs)

To make a separate mod from this example, give it a new UUID, its own mod details, and a distinct assembly name. Set the DLL path in the manifest to your new DLL. Give the settings key in `RetroControls.cs` its own prefix so each mod can save its own preferences. Publish the runtime files you have tested in the game. [Mod identity and copies (Chinese)](../../Docs/Manifest.md#身份与副本) · [Publish to Steam Workshop (Chinese)](../../Docs/Workshop.md#发布和更新)
