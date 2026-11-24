# 《舒舒服服小岛时光》模组指南

简体中文 · [English](README_EN.md)

**Cozy Island Modding** 提供模组安装指南、源码示例和制作工具。你可以订阅别人做的模组，也可以动手给游戏加一点变化，比如装备小木锤时，在「拆除」提示旁显示建筑名称。

[AI 辅助制作](#ai-辅助制作-mod推荐) · [试做第一个模组](#试做第一个模组) · [发布到创意工坊](#发布到创意工坊) · [更多示例](#更多示例) · [常见问题](#常见问题) · [免责声明](#免责声明)

> **开始前**　请另外安装带「模组」菜单且能读取本仓库清单的游戏版本。导入引用时，工具会自动检查配套情况。可以按下方对应系统的步骤操作。 [Steam 商店](https://store.steampowered.com/app/2918500/) · [配套版本记录](PlayerIdentity.json)

## AI 辅助制作 Mod（推荐）

GPT 和 Claude 等 AI 助手可以帮你从现成的范例做起，修改代码、编译，或根据日志排查加载问题。

你可以通过以下步骤将本指南导入到 AI 助手的工作区（当然你也可以直接将网址发送给你的 AI 助手让它帮你完成）：

1. 使用 git 将当前项目 clone，或者直接下载解压到本地机器。
2. 在 AI 助手中把 `cozyisland-modding` 文件夹作为工作区打开。
3. 告诉助手配套游戏安装在哪里，以及你想在游戏里看到什么效果。 [给 AI 的任务模板](AI-AUTHORING.md)

例如，可以这样交代你想要的修改：

```text
请学习目前已有的模组范例，帮我制作一个将游戏角色的默认衣服颜色改成黄色的模组。

完成后帮我导入到游戏中进行测试，测试确认无误后帮我上传到 Steam 创意工坊，默认权限为私密。
```

编译后，在游戏里启用本地模组并确认实际效果，再决定是否发布。

本仓库的部分文档、示例和工具在 AI 协助下编写。欢迎反馈说明中的错误、使用中遇到的问题，或提出改进建议。 [提交反馈](https://github.com/zzs1339004604/cozyisland-modding/issues)

## 试做第一个模组

先用 **BuildingHints（建筑名称提示）** 跑通整个流程。这个示例已经写好，你可以先编译和试用，再改一点文字看看效果。 [示例源码和说明](Examples/BuildingHints/README.md)

### 准备工具

- 在 GitHub 仓库首页选择 **Code → Download ZIP**，解压到一个方便找到的文件夹。
- 安装 **PowerShell 7** 和 **.NET SDK**。PowerShell 用来运行这里的命令，.NET SDK 用来把 C# 源码编译成游戏能读取的文件。下载时选择 **SDK**。这个示例无需安装 Unity 编辑器。 [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) · [.NET SDK](https://dotnet.microsoft.com/download)

### 编译示例

先完全退出游戏。在刚解压的目录中打开 **PowerShell 7**，确认目录里有 `Tools` 和 `Examples`。在下方选择你的系统，把示例路径换成你自己的游戏位置。Windows 选择包含 `CozyIsland.exe` 的目录，macOS 选择完整的 `.app`。

**Windows**

```powershell
$game = 'D:\SteamLibrary\steamapps\common\Cozy Island'
```

**macOS**

```powershell
$game = '/Applications/CozyIsland.app'
```

在同一个窗口运行下面两条命令。第一条会短暂启动所选游戏，检查游戏是否支持新作品，并复制编译用的文件。第二条会在 `Examples/BuildingHints/Code` 生成 `CozyMods.BuildingHints.dll`。 [工具使用说明](Tools/README.md)

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory $game
./Tools/Build-CodeMod.ps1 -ProjectPath './Examples/BuildingHints/Source/BuildingHints.csproj'
```

### 放进游戏并试用

1. 重新打开刚才选中的游戏版本。选择 **模组 → 模组创作 → 打开作品文件夹**。如果打开的是已有作品，先回到上一级 `Mods`，然后在里面新建 `BuildingHints` 文件夹。
2. 从 `Examples/BuildingHints` 复制 `mod.json` 和刚编译出的 DLL，按下面的结构放好。`Source` 目录留在原处，之后改代码时会用到。

   ```text
   Mods/
     BuildingHints/
       mod.json
       Code/
         CozyMods.BuildingHints.dll
   ```

3. 回到 **模组** 列表，启用 **建筑名称提示**，按提示进岛。拿起 **小木锤**，对准有名称、可以拆除的建筑或家具。看到提示从「拆除」变成「拆除 · 建筑名称」，就说明示例生效了。
4. 想试试自己的改动，可以把 `Examples/BuildingHints/Source/BuildingHints.cs` 里的 `" · "` 改成 `" / "`。完全退出游戏，再编译一次，用新的 DLL 替换 `Mods/BuildingHints/Code` 里的旧文件。重新开游戏，看看提示有没有换成斜线。

### 新建自己的作品

试过示例后，可以在同一个 PowerShell 窗口创建自己的工程。把显示名和作者名换成你想用的名字。

```powershell
./Tools/New-CodeMod.ps1 -ProjectName my-first-mod -DisplayName '我的第一个模组' -Author '我的名字'
./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/my-first-mod/Source/Mod.csproj'
```

新工程在 `MyMods/my-first-mod`。按上面的安装方法，把它的 `mod.json` 和编译生成的 DLL 复制到 `Mods/my-first-mod` 下的对应位置。启用后，在游戏日志中查找 `Loaded 1.0.0`。清单里的 `displayName` 是玩家看到的名字，`name` 是作者用的标识名。工具会为每个新作品生成一个 `uuid`。给同一作品改名或更新时，保留这个值。 [工程说明](Tools/README.md#新建自己的作品) · [清单和文件说明](Docs/Manifest.md)

## 发布到创意工坊

1. 登录 Steam，在游戏里打开 **模组 → 模组创作**，选中你自己制作并在游戏里试过的本地作品。填写显示名、作者和介绍，点击 **保存修改**。
2. 第一次发布时，工坊 ID 留空，点击 **发布到创意工坊**。第一次上传默认是私密的。如果 Steam 提示接受创意工坊协议，请在 Steam 页面完成。更新作品时，确认原工坊 ID 后点击 **发布更新**。换电脑后如果列表找不到原关联，可以填回原作品页上的数字 ID。
3. 上传完成后，从 Steam 订阅并等下载完成。在游戏里停用本地副本，启用工坊副本。完全退出游戏再打开，然后用和本地版一样的方法试一遍，确认订阅到的版本也能用。 [发布、更新和包大小限制](Docs/Workshop.md#发布和更新)

## 更多示例

| 想做什么 | 从这里开始 |
| --- | --- |
| 改一条游戏提示 | [BuildingHints](Examples/BuildingHints/README.md) 在拆除提示旁显示建筑名称。 |
| 看看游戏怎样运行自己的代码 | [MinimalCodeMod](Examples/MinimalCodeMod/README.md) 在游戏日志里显示加载结果。 |
| 调整作物生长或角色步行 | [GreenThumb](Examples/GreenThumb/README.md) 改作物生长和耗水，[BriskWalk](Examples/BriskWalk/README.md) 演示修改步行数值。 |
| 添加一个能制作和食用的物品 | [TrailTreat](Examples/TrailTreat/README.md) 加入远足饼、配方和可保存的任务进度。 |
| 带入自己的模型并保存物品和进度 | [ResearchCamp](Examples/ResearchCamp/README.md) 展示营地模型、配方和存档，含可编辑的 Unity 工程。 |

进一步制作时，可以查阅 [工具与版本配套](Tools/README.md)、[编写游戏玩法](Docs/Native-Authoring.md)、[制作模型和物品](Docs/Game-and-Prefab-Guide.md)、[保存进度](Docs/Save-Data.md) 和 [多人游戏](Docs/Networking.md)。需要查原版名称和标识时，用 [物品表](Reference/GameItems.json) 和 [配方表](Reference/GameRecipes.json) 找线索，再以自己的游戏版本为准。

## 常见问题

- **主菜单没有「模组」怎么办？** 先核对你启动的是哪一份游戏。如果这个版本还没有模组功能，需要先取得配套游戏版本。 [配套版本检查](Tools/README.md#核对配套-build-并导入引用)
- **模组不在列表里，或启用了却没效果？** 确认 Steam 已下载完成，或确认 `mod.json` 在本地作品文件夹的第一层。回到主菜单查看依赖、版本或重启提示。如果仍然有问题，可以查看游戏日志。 [重启与加载顺序](Docs/Reloading.md#应用时机)
- **游戏日志在哪？** 在 Windows 上通常是 `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland\Player.log`，在 macOS 上通常是 `~/Library/Logs/LazyFish Studio/CozyIsland/Player.log`。
- **停用模组后还能继续原来的岛吗？** 如果存档使用了模组新增的物品，缺失物品可能在读档时被清理。用测试岛试验，并保留原存档的备份。 [缺失内容与存档](Docs/Save-Data.md#缺失内容与失败是不同情况)

## 免责声明

- 代码模组会在游戏中运行作者提供的代码。请从信任的来源安装模组，并查看作品适用的游戏版本和使用说明。
- 模组可能彼此冲突，也可能在游戏更新后失效。试用会改变岛屿内容的模组前，请使用测试岛或备份现有存档。 [存档说明](Docs/Save-Data.md)
- 仓库目前尚未声明公开复用许可。转载源码、文档或示例资源前，请以权利人正式提供的许可为准。
