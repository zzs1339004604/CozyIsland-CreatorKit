# Cozy Island Modding

[简体中文](README.md) · English

This is the guide to making mods for Cozy Island. To make an ordinary skin, hat, or clothing appearance, start with a GLB model and two JSON files. [Appearance authoring guide](Examples/AppearanceFramework/README_EN.md#make-an-appearance-pack)

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

> **Authoring tip** You can refer to the existing examples and consider reusing the base game’s UI styles and materials for a consistent look. Use smooth shading on curved surfaces and preserve normals when exporting models. [Model export](Examples/AppearanceFramework/README_EN.md#make-an-appearance-pack)

You can also try your mod locally in the game before deciding whether to publish it to the Steam Workshop.

## Contents

- [1. Build your first mod](#build-your-first-mod)
- [2. Publish to Steam Workshop](#publish-to-steam-workshop)
- [3. Common questions](#common-questions)
- [4. Disclaimer](#disclaimer)

## Build your first mod

Start with [Sprout Cap](Examples/SproutCap/README_EN.md). Install a compatible game and Appearance Mod Framework with GLB support, change a color in `appearance.json`, and try it in Appearances. To make your own hat or skin, supply another GLB and match the part paths to your model’s node names. [Configuration and export](Examples/AppearanceFramework/README_EN.md#make-an-appearance-pack)

Use C# and the .NET SDK for custom gameplay behavior or native item integration. [ClothingColor](Examples/ClothingColor/README_EN.md) is a first example of building a code mod. Use Unity build tools when the assets require Unity-specific Prefabs or custom shaders. [Build tools (Chinese)](Tools/README.md)

## Publish to Steam Workshop

GLB appearances use the same model file on Windows and macOS. Try the release package on both platforms and list the supported platforms in the item description. For mods with custom code or Unity AssetBundles, also check the dependencies or asset build targets. [Windows and macOS compatibility guide (Chinese)](Docs/Native-Authoring.md#兼容-windows-和-macos)

1. Sign in to Steam. In the game, open **Mods → Create Mods** and select your own local mod that you’ve tested in-game. Fill in its display name, author, and description, then choose **Save changes**.
2. For a new item, leave the Workshop ID blank and choose **Publish to Workshop**. First uploads are private by default. If Steam asks you to accept the Workshop agreement, do so on Steam. To update an existing item, confirm its original Workshop ID and choose **Publish update**. If the game has lost the link to your item, enter the number from its original Workshop page.
3. After the upload, subscribe to the item on Steam and wait for it to download. Turn off the local copy and turn on the Workshop copy in the game. Quit the game completely, open it again, and repeat your local test to check the version Steam delivered. [Publishing, updates, and size limits (Chinese)](Docs/Workshop.md#发布和更新)

## More examples

| What you want to do | Where to start |
| --- | --- |
| Make an appearance from a model and configuration | [Sprout Cap](Examples/SproutCap/README_EN.md) is a GLB example you can edit without code or Unity. |
| Maintain an existing Unity model pack | [Upright Mouse](Examples/UprightMouse/README_EN.md) shows body attachment, blinking, and tail movement with the existing AssetBundle format. |
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
