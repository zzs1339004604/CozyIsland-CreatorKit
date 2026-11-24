# 《舒舒服服小岛时光》模组指南

简体中文 · [English](README_EN.md)

这里有《舒舒服服小岛时光》的模组制作指南、范例和工具。第一次做模组，可以从给角色的默认上衣换个颜色开始。

[AI 辅助制作](#ai-辅助制作-mod推荐) · [试做第一个模组](#试做第一个模组) · [发布到创意工坊](#发布到创意工坊) · [更多示例](#更多示例) · [常见问题](#常见问题) · [免责声明](#免责声明)

> **开始前**　制作下方范例需要带「模组」菜单并支持 `displayName`、`name`、`uuid` 清单的 Cozy Island 版本。配套版本记录会列出已验收的游戏版本。 [Steam 商店](https://store.steampowered.com/app/2918500/) · [配套版本记录](PlayerIdentity.json)

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

从 **ClothingColor（起始上衣换色）** 开始。启用后，穿着默认上衣进岛就能看见变化。跟着说明改一次颜色，就能跑通编译、安装和发布的过程。 [Quick Start 指南](Examples/ClothingColor/README.md)

在仓库首页点 **Code → Download ZIP** 下载源码。准备好配套游戏和 **.NET SDK**，就能在系统终端里编译。范例只用游戏现成的衣服模型和材质。 [下载 .NET SDK](https://dotnet.microsoft.com/download) · [查找游戏引用和编译项目](Tools/README.md#直接编译)

## 发布到创意工坊

1. 登录 Steam，在游戏里打开 **模组 → 模组创作**，选中你自己制作并在游戏里试过的本地作品。填写显示名、作者和介绍，点击 **保存修改**。
2. 第一次发布时，工坊 ID 留空，点击 **发布到创意工坊**。第一次上传默认是私密的。如果 Steam 提示接受创意工坊协议，请在 Steam 页面完成。更新作品时，确认原工坊 ID 后点击 **发布更新**。换电脑后如果列表找不到原关联，可以填回原作品页上的数字 ID。
3. 上传完成后，从 Steam 订阅并等下载完成。在游戏里停用本地副本，启用工坊副本。完全退出游戏再打开，然后用和本地版一样的方法试一遍，确认订阅到的版本也能用。 [发布、更新和包大小限制](Docs/Workshop.md#发布和更新)

## 更多示例

| 想做什么 | 从这里开始 |
| --- | --- |
| 用自制模型做身体外观 | [直立鼠](Examples/UprightMouse/README.md) 演示纯数据身体外观包，可在联机外观框架的 F10 菜单选用。 |
| 加入能在工作台制作的服装 | [青苔魔法套装](Examples/WitchRobe/README.md) 添加魔法袍和帽子，沿用游戏的穿脱流程。 |
| 和朋友互相看到自定义外观 | [联机外观](Examples/AppearanceFramework/README.md) 让启用同一框架的玩家在房间里选择和分享外观。 |
| 改变画面风格 | [复古滤镜](Examples/RetroFilters/README.md) 演示全屏效果和实时调节。 |
| 加入物品、配方和任务 | [远足饼](Examples/TrailTreat/README.md) 演示食物和任务进度。[研究营地](Examples/ResearchCamp/README.md) 演示自制模型和联机任务。 |

交互提示、作物和移动速度等示例，以及各范例的平台和试用方法，见[完整范例索引](Examples/README.md)。

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
