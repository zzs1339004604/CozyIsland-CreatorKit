# Cozy Island Modding

[简体中文](README.md) · English

This is the guide to making mods for Cozy Island.

> Some of the documentation, examples, and tools in this repository were written with AI assistance. We welcome corrections, bug reports, and suggestions. [Share feedback](https://github.com/zzs1339004604/cozyisland-modding/issues)
>
> We also welcome pull requests to improve existing materials or add new mod examples. [Contribute](https://github.com/zzs1339004604/cozyisland-modding/pulls)

---

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

> **Authoring tip** You can refer to the existing examples and consider reusing the base game’s UI styles and materials for a consistent look. Use smooth shading by default on curved surfaces when making characters, skins, or clothing. [Model and normal settings](Examples/UprightMouse/README_EN.md#edit-the-model-or-make-your-own-character)

You can also try your mod locally in the game before deciding whether to publish it to the Steam Workshop.

## Contents

- [1. Build your first mod](#build-your-first-mod)
- [2. Publish to Steam Workshop](#publish-to-steam-workshop)
- [3. Common questions](#common-questions)
- [4. Disclaimer](#disclaimer)

## Build your first mod

Start with **ClothingColor**. Turn it on and enter an island wearing the default shirt to see the change. The guide walks you through changing one color, building the mod, trying it locally, and publishing it. [Quick Start guide](Examples/ClothingColor/README_EN.md)

Download the source with **Code → Download ZIP** on the repository page. You can build the example in your system terminal with a compatible copy of the game and the **.NET SDK**. The example uses the game’s existing clothing model and material. [Download the .NET SDK](https://dotnet.microsoft.com/download) · [Find game references and build a project (Chinese)](Tools/README.md#直接编译)

## Publish to Steam Workshop

One Workshop item can support Windows and macOS. Use managed dependencies available on both platforms, build a separate Unity AssetBundle for each target, and test the same release package in both game builds. List the platforms you support in the item description. [Windows and macOS compatibility guide (Chinese)](Docs/Native-Authoring.md#兼容-windows-和-macos)

1. Sign in to Steam. In the game, open **Mods → Create Mods** and select your own local mod that you’ve tested in-game. Fill in its display name, author, and description, then choose **Save changes**.
2. For a new item, leave the Workshop ID blank and choose **Publish to Workshop**. First uploads are private by default. If Steam asks you to accept the Workshop agreement, do so on Steam. To update an existing item, confirm its original Workshop ID and choose **Publish update**. If the game has lost the link to your item, enter the number from its original Workshop page.
3. After the upload, subscribe to the item on Steam and wait for it to download. Turn off the local copy and turn on the Workshop copy in the game. Quit the game completely, open it again, and repeat your local test to check the version Steam delivered. [Publishing, updates, and size limits (Chinese)](Docs/Workshop.md#发布和更新)

## More examples

| What you want to do | Where to start |
| --- | --- |
| Make a body appearance with custom models | [Upright Mouse](Examples/UprightMouse/README_EN.md) is a data-only pack you can select in the Appearances menu with F10. |
| Add clothing players can craft | [Moss Witch Outfit](Examples/WitchRobe/README_EN.md) adds a craftable robe and hat that use the game’s equipment system. |
| Support character skin and outfit mods | The [Appearance Mod Framework](Examples/AppearanceFramework/README_EN.md) adds the Appearances menu and syncs custom looks between players in a room. |
| Change the look of the game | [Retro Filters](Examples/RetroFilters/README_EN.md) demonstrates full-screen effects and live controls. |
| Add items, recipes, and tasks | [TrailTreat](Examples/TrailTreat/README.md) demonstrates food and saved task progress. [ResearchCamp](Examples/ResearchCamp/README.md) adds a custom model and multiplayer tasks. |

Find more examples for interaction prompts, crops, and movement, along with supported platforms and ways to try each one. [Full example index (Chinese)](Examples/README.md)

The detailed guides are in Chinese. They cover [tools and game versions](Tools/README.md), [writing gameplay code](Docs/Native-Authoring.md), [making models and items](Docs/Game-and-Prefab-Guide.md), [saving progress](Docs/Save-Data.md), and [multiplayer](Docs/Networking.md). The [item list](Reference/GameItems.json) and [recipe list](Reference/GameRecipes.json) can help you look up names and IDs. Check them against your own game version.

## Common questions

- **What if I don’t see Mods on the main menu?** Check which copy of the game you started. The examples need a game version with a **Mods** menu and support for UUID manifests. [Game requirements and build instructions (Chinese)](Tools/README.md#直接编译)
- **What if a mod is missing from the list or has no effect?** Check that Steam finished downloading it or that `mod.json` is at the top level of your local mod folder. Return to the main menu and check for missing requirements, version errors, or a restart prompt. If you still need to troubleshoot, check the game log. [Restarts and load order (Chinese)](Docs/Reloading.md#应用时机)
- **Where is the game log?** On Windows it’s usually at `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland\Player.log`. On macOS it’s usually at `~/Library/Logs/LazyFish Studio/CozyIsland/Player.log`.
- **Can I keep playing the same island after turning off a mod?** If the save uses items added by a mod, the game may remove missing items when you load it. Try changes on a test island and keep a backup of the original save. [Missing content and saves (Chinese)](Docs/Save-Data.md#缺失内容与失败是不同情况)

## Disclaimer

- Code mods run code supplied by their authors inside the game. Install mods from sources you trust and check each mod’s supported game versions and instructions.
- Mods can conflict with one another or stop working after a game update. Before trying a mod that changes your island, use a test island or back up your existing save. [Save data (Chinese)](Docs/Save-Data.md)
- This repository has yet to declare a public reuse license. For redistribution of the source, documentation, or example assets, follow the permissions formally provided by the rights holder.
