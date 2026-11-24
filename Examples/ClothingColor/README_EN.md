# Starter Shirt Color

[简体中文](README.md) · English

Your player's green starter shirt and sleeves turn rose pink as soon as you enter an island. The game uses its original model and material, and other outfits still show their usual colors. The example uses one C# file and needs no Unity Editor.

## Build and try it

1. Install the .NET SDK and open a terminal at the repository root, the folder containing `Examples` and `Tools`. Replace the sample Windows path with `CozyIsland_Data/Managed` from your compatible game installation. On macOS, use `Contents/Resources/Data/Managed` inside the game's `.app`. [Game version requirements](../../README_EN.md) · [.NET SDK](https://dotnet.microsoft.com/download)

   ```text
   dotnet build Examples/ClothingColor/Source/ClothingColor.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. In the game, open **Mods → Create Mods → Open mod folder**. Find `Mods` and make a `ClothingColor` folder inside it. Copy `mod.json` and the newly built `Code/CozyMods.ClothingColor.dll` from `Examples/ClothingColor` into that folder. These two files make up the package you upload.

   ```text
   Mods/
     ClothingColor/
       mod.json
       Code/
         CozyMods.ClothingColor.dll
   ```

3. Go back to **Mods**, enable **起始上衣换色** (Starter Shirt Color), and enter an island. Switch to the starter outfit if your player is wearing a different outfit.
4. Try changing the numbers on the `ShirtColor` line in the source. The first three are red, green, and blue, each from 0 to 255. The last value, `255`, means fully opaque. Quit the game, build again, replace the DLL in your local mod folder, and reopen the game to see the change. [Source code](Source/ClothingColor.cs)

## Publish to Steam Workshop

Before publishing your own version, give it your own `displayName`, `author`, and `name` in `mod.json`. Run `[guid]::NewGuid()` in Windows PowerShell or `uuidgen` in a macOS terminal. Put the generated ID in `uuid` and keep it when you update the same mod later. Give the project's `AssemblyName` a unique name, build again, and point `code.assembly` in the manifest at your new DLL. [Manifest fields (Chinese)](../../Docs/Manifest.md)

Put the updated manifest and DLL in your local mod folder, enable the mod, and test it on an island again. With Steam signed in, open **Mods → Create Mods**, select your project, and save it. For your first upload, leave the Workshop ID blank and choose **Publish to Workshop**. The first upload is private by default. If Steam asks you to accept the Workshop agreement, do so on Steam. After uploading, subscribe to the Workshop copy, disable the local copy, enable the Workshop copy, and restart the game to check the shirt color. [Publishing and updates (Chinese)](../../Docs/Workshop.md)
