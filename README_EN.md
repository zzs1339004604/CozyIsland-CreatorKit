# Cozy Island Modding

[简体中文](README.md) · English

Use this guide to play with mods or make your own for Cozy Island. This repository includes sample mods, source code, and tools for building and publishing your work. A mod can make a small change, such as showing a building’s name next to the Remove prompt when you equip the Small Wooden Hammer.

[Install mods](#install-mods) · [Make mods with AI](#make-mods-with-ai-recommended) · [Build your first mod](#build-your-first-mod) · [Publish to Steam Workshop](#publish-to-steam-workshop) · [More examples](#more-examples) · [Common questions](#common-questions) · [Disclaimer](#disclaimer)

> **Before you start**  Install a version of Cozy Island with a **Mods** menu that supports this repository’s manifest. The import tool checks compatibility before it copies the files you need to build a mod. Follow the instructions below for your operating system. [Steam store](https://store.steampowered.com/app/2918500/) · [Game build record](PlayerIdentity.json)

## Install mods

1. From the main menu, choose **Mods → Open Steam Workshop**. Find a mod, click **Subscribe**, and wait for Steam to download it. [Steam Workshop](https://steamcommunity.com/app/2918500/workshop/)
2. Return to **Mods** and turn on the mod you subscribed to. New mods start turned off. If the mod lists any required mods, subscribe to and turn on those too.
3. Enter your island when the game is ready. If prompted to restart, quit the game completely and open it again.

## Make mods with AI (recommended)

AI assistants such as GPT and Claude can help you start from an existing example, edit and build the code, or troubleshoot loading problems using the game log.

Follow these steps to open this guide in your AI assistant’s workspace. You can also send the repository URL to your assistant and ask it to handle the setup.

1. Clone this repository with Git or download and extract it to your computer.
2. In your AI assistant, open the `cozyisland-modding` folder as a workspace.
3. Tell the assistant where your compatible copy of the game is installed and what you’d like to see in-game. [Task template for AI assistants (Chinese)](AI-AUTHORING.md)

Here’s an example of how to describe the change you want.

```text
Study the existing mod examples and make a mod that changes the player character’s default clothing color to yellow.

Install the mod in the game and test it. Once you’ve confirmed it works, upload it to the Steam Workshop with visibility set to Private.
```

Once it builds, enable the local mod and verify the result in-game before deciding whether to publish it.

Some of the documentation, examples, and tools in this repository were written with AI assistance. We welcome corrections, bug reports, and suggestions. [Share feedback](https://github.com/zzs1339004604/cozyisland-modding/issues)

## Build your first mod

Start with **BuildingHints**, a ready-to-build example that adds a building name to an interaction prompt. Build and try it first, then change a small piece of text to see your own edit in-game. [Example source](Examples/BuildingHints/Source/BuildingHints.cs) · [Example notes (Chinese)](Examples/BuildingHints/README.md)

### Get the tools

- On the repository page, choose **Code → Download ZIP** and extract it somewhere you can find again.
- Install **PowerShell 7** to run the commands and the **.NET SDK** to build the mod from C# source. Choose the **SDK** download. This example works without the Unity Editor. [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) · [.NET SDK](https://dotnet.microsoft.com/download)

### Build the example

Quit the game completely. Open **PowerShell 7** in the extracted folder that contains `Tools` and `Examples`. Choose your system below and replace the example path with your game location. On Windows, use the folder containing `CozyIsland.exe`. On macOS, use the full `.app`.

**Windows**

```powershell
$game = 'D:\SteamLibrary\steamapps\common\Cozy Island'
```

**macOS**

```powershell
$game = '/Applications/CozyIsland.app'
```

Run these two commands in the same window. The first briefly starts the selected game to check whether it supports new mods and copies the files needed to build the mod. The second creates `CozyMods.BuildingHints.dll` in `Examples/BuildingHints/Code`. [Tool guide (Chinese)](Tools/README.md)

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory $game
./Tools/Build-CodeMod.ps1 -ProjectPath './Examples/BuildingHints/Source/BuildingHints.csproj'
```

### Install and try it

1. Open the same game version again. Choose **Mods → Create Mods → Open mod folder**. If it opens an existing mod’s folder, go up one level to `Mods`. Create a folder named `BuildingHints` there.
2. From `Examples/BuildingHints`, copy `mod.json` and the DLL you just built into the folders shown below. Leave `Source` where it is so you can edit the code later.

   ```text
   Mods/
     BuildingHints/
       mod.json
       Code/
         CozyMods.BuildingHints.dll
   ```

3. Return to **Mods**, turn on **建筑名称提示** (BuildingHints), and enter an island when prompted. Equip the **Small Wooden Hammer** and point it at a named building or piece of furniture that can be removed. The prompt should change from **Remove** to **Remove · [building name]**.
4. To try an edit, change `" · "` to `" / "` in `Examples/BuildingHints/Source/BuildingHints.cs`. Quit the game completely, build again, and replace the DLL in `Mods/BuildingHints/Code`. Start the game again and look for the slash in the prompt.

### Start your own mod

Once you’ve tried the example, create your own project in the same PowerShell window. Replace the display name and author name with the ones you want to use.

```powershell
./Tools/New-CodeMod.ps1 -ProjectName my-first-mod -DisplayName 'My First Mod' -Author 'Your name'
./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/my-first-mod/Source/Mod.csproj'
```

Your project is in `MyMods/my-first-mod`. Follow the same folder layout as the example when you copy its `mod.json` and DLL into `Mods/my-first-mod`. Turn on your mod and look for `Loaded 1.0.0` in the game log. In `mod.json`, `displayName` is what players see and `name` is the identifier authors use in tools and logs. The tool creates a `uuid` for each new mod. Keep that UUID when you rename or update the same mod. [Project setup (Chinese)](Tools/README.md#新建自己的作品) · [Manifest and files (Chinese)](Docs/Manifest.md)

## Publish to Steam Workshop

1. Sign in to Steam. In the game, open **Mods → Create Mods** and select your own local mod that you’ve tested in-game. Fill in its display name, author, and description, then choose **Save changes**.
2. For a new item, leave the Workshop ID blank and choose **Publish to Workshop**. First uploads are private by default. If Steam asks you to accept the Workshop agreement, do so on Steam. To update an existing item, confirm its original Workshop ID and choose **Publish update**. If the game has lost the link to your item, enter the number from its original Workshop page.
3. After the upload, subscribe to the item on Steam and wait for it to download. Turn off the local copy and turn on the Workshop copy in the game. Quit the game completely, open it again, and repeat your local test to check the version Steam delivered. [Publishing, updates, and size limits (Chinese)](Docs/Workshop.md#发布和更新)

## More examples

| What you want to do | Where to start |
| --- | --- |
| Change an interaction prompt | [BuildingHints](Examples/BuildingHints/README.md) adds a building name next to the Remove prompt. |
| See the game run your code | [MinimalCodeMod](Examples/MinimalCodeMod/README.md) writes its results to the game log. |
| Adjust crops or how fast a character walks | [GreenThumb](Examples/GreenThumb/README.md) changes crop growth and water use. [BriskWalk](Examples/BriskWalk/README.md) shows how to adjust walking speed. |
| Add something you can craft and eat | [TrailTreat](Examples/TrailTreat/README.md) adds a snack, a recipe, and a task that saves its progress. |
| Bring in your own model and save items and progress | [ResearchCamp](Examples/ResearchCamp/README.md) demonstrates a camp model, recipes, and saved progress with an editable Unity project. |

The detailed guides are in Chinese. They cover [tools and game versions](Tools/README.md), [writing gameplay code](Docs/Native-Authoring.md), [making models and items](Docs/Game-and-Prefab-Guide.md), [saving progress](Docs/Save-Data.md), and [multiplayer](Docs/Networking.md). The [item list](Reference/GameItems.json) and [recipe list](Reference/GameRecipes.json) can help you look up names and IDs. Check them against your own game version.

## Common questions

- **What if I don’t see Mods on the main menu?** Check which copy of the game you started. If that build doesn’t support mods yet, you’ll need a compatible game build to continue. [Game build check (Chinese)](Tools/README.md#核对配套-build-并导入引用)
- **What if a mod is missing from the list or has no effect?** Check that Steam finished downloading it or that `mod.json` is at the top level of your local mod folder. Return to the main menu and check for missing requirements, version errors, or a restart prompt. If you still need to troubleshoot, check the game log. [Restarts and load order (Chinese)](Docs/Reloading.md#应用时机)
- **Where is the game log?** On Windows it’s usually at `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland\Player.log`. On macOS it’s usually at `~/Library/Logs/LazyFish Studio/CozyIsland/Player.log`.
- **Can I keep playing the same island after turning off a mod?** If the save uses items added by a mod, the game may remove missing items when you load it. Try changes on a test island and keep a backup of the original save. [Missing content and saves (Chinese)](Docs/Save-Data.md#缺失内容与失败是不同情况)

## Disclaimer

- Code mods run code supplied by their authors inside the game. Install mods from sources you trust and check each mod’s supported game versions and instructions.
- Mods can conflict with one another or stop working after a game update. Before trying a mod that changes your island, use a test island or back up your existing save. [Save data (Chinese)](Docs/Save-Data.md)
- This repository has yet to declare a public reuse license. For redistribution of the source, documentation, or example assets, follow the permissions formally provided by the rights holder.
