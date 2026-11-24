# Cozy Island Modding

[简体中文](README.md) · English

Find guides, examples, and tools for making Cozy Island mods. For your first mod, try giving the player’s starting shirt a new color.

[Make mods with AI](#make-mods-with-ai-recommended) · [Build your first mod](#build-your-first-mod) · [Publish to Steam Workshop](#publish-to-steam-workshop) · [More examples](#more-examples) · [Common questions](#common-questions) · [Disclaimer](#disclaimer)

> **Before you start**  The examples need a version of Cozy Island with a **Mods** menu that understands the `displayName`, `name`, and `uuid` manifest fields. The build record lists game versions that have been verified for the kit. [Steam store](https://store.steampowered.com/app/2918500/) · [Game build record](PlayerIdentity.json)

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

Start with **ClothingColor**. Turn it on and enter an island wearing the default shirt to see the change. The guide walks you through changing one color, building the mod, trying it locally, and publishing it. [Quick Start guide](Examples/ClothingColor/README_EN.md)

Download the source with **Code → Download ZIP** on the repository page. You can build the example in your system terminal with a compatible copy of the game and the **.NET SDK**. The example uses the game’s existing clothing model and material. [Download the .NET SDK](https://dotnet.microsoft.com/download) · [Find game references and build a project (Chinese)](Tools/README.md#直接编译)

## Publish to Steam Workshop

1. Sign in to Steam. In the game, open **Mods → Create Mods** and select your own local mod that you’ve tested in-game. Fill in its display name, author, and description, then choose **Save changes**.
2. For a new item, leave the Workshop ID blank and choose **Publish to Workshop**. First uploads are private by default. If Steam asks you to accept the Workshop agreement, do so on Steam. To update an existing item, confirm its original Workshop ID and choose **Publish update**. If the game has lost the link to your item, enter the number from its original Workshop page.
3. After the upload, subscribe to the item on Steam and wait for it to download. Turn off the local copy and turn on the Workshop copy in the game. Quit the game completely, open it again, and repeat your local test to check the version Steam delivered. [Publishing, updates, and size limits (Chinese)](Docs/Workshop.md#发布和更新)

## More examples

| What you want to do | Where to start |
| --- | --- |
| Change the starting shirt’s color | [ClothingColor](Examples/ClothingColor/README_EN.md) walks you through your first edit and Workshop upload. |
| Change an interaction prompt | [BuildingHints](Examples/BuildingHints/README.md) adds a building name next to the Remove prompt. |
| See the game run your code | [MinimalCodeMod](Examples/MinimalCodeMod/README.md) writes its results to the game log. |
| Adjust crops or how fast a character walks | [GreenThumb](Examples/GreenThumb/README.md) changes crop growth and water use. [BriskWalk](Examples/BriskWalk/README.md) shows how to adjust walking speed. |
| Add something you can craft and eat | [TrailTreat](Examples/TrailTreat/README.md) adds a snack, a recipe, and a task that saves its progress. |
| Make a body appearance pack with your own models | [Upright Mouse](Examples/UprightMouse/README_EN.md) shows how to configure a skin that players can select in the Appearance Framework menu. |
| Add a screen filter | [RetroFilters](Examples/RetroFilters/README_EN.md) demonstrates a full-screen shader, live controls, and local preferences. |
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
