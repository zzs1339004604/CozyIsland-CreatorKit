# 《舒舒服服小岛时光》模组指南

简体中文 · [English](README_EN.md)

这里是《舒舒服服小岛时光》的模组制作指南。制作普通皮肤、帽子或服装外观，从一个 GLB 模型和两份 JSON 开始。 [外观制作指南](Examples/AppearanceFramework/README.md#做自己的外观包)

> 本仓库部分文档、示例和工具在 AI 协助下编写。欢迎反馈说明中的错误、使用中遇到的问题，或提出改进建议。 [提交反馈](https://github.com/zzs1339004604/cozyisland-modding/issues)
>
> 也非常欢迎大家直接提交 PR 改进现有内容，或贡献新的 Mod 范例。 [参与贡献](https://github.com/zzs1339004604/cozyisland-modding/pulls)

---

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

> **创作提示**　可以参考已有的范例，并考虑沿用原版游戏的 UI 样式和材质以保持风格一致。制作角色、皮肤或服装模型时，圆弧曲面默认使用平滑着色，导出时保留网格法线。 [模型导出](Examples/AppearanceFramework/README.md#做自己的外观包)

当然也可以本地以本地模组的方式进行测试，然后再决定是否要发布到创意工坊。

## 目录

- [1. 试做第一个模组](#试做第一个模组)
- [2. 发布到创意工坊](#发布到创意工坊)
- [3. 常见问题](#常见问题)
- [4. 免责声明](#免责声明)


## 试做第一个模组

从[芽苗帽](Examples/SproutCap/README.md)开始。安装配套游戏和支持 GLB 的外观模组框架，在 `appearance.json` 改一次颜色，进外观库查看效果。做自己的帽子或皮肤时，替换 GLB 并把配置里的部件路径改成自己的节点名。 [配置字段和导出步骤](Examples/AppearanceFramework/README.md#做自己的外观包)

需要自定义玩法或接入原生物品时，再使用 C# 和 .NET SDK。想了解代码模组的编译流程，可以试做[起始上衣换色](Examples/ClothingColor/README.md)。涉及 Unity 专用 Prefab 或自定义 Shader 的资源才需要 Unity 构建工具。 [编译和资源构建](Tools/README.md)

## 发布到创意工坊

GLB 外观在 Windows 和 macOS 共用同一份模型。发布前用两个平台的游戏试戴。包含自定义代码或 Unity AssetBundle 的作品还需检查依赖或构建目标。 [兼容 Windows 和 macOS](Docs/Native-Authoring.md#兼容-windows-和-macos)

1. 登录 Steam，在游戏里打开 **模组 → 模组创作**，选中你自己制作并在游戏里试过的本地作品。填写显示名、作者和介绍，点击 **保存修改**。
2. 第一次发布时，工坊 ID 留空，点击 **发布到创意工坊**。第一次上传默认是私密的。如果 Steam 提示接受创意工坊协议，请在 Steam 页面完成。更新作品时，确认原工坊 ID 后点击 **发布更新**。换电脑后如果列表找不到原关联，可以填回原作品页上的数字 ID。
3. 上传完成后，从 Steam 订阅并等下载完成。在游戏里停用本地副本，启用工坊副本。完全退出游戏再打开，然后用和本地版一样的方法试一遍，确认订阅到的版本也能用。 [发布、更新和包大小限制](Docs/Workshop.md#发布和更新)

## 更多示例

| 想做什么 | 从这里开始 |
| --- | --- |
| 用模型和配置做新外观 | [芽苗帽](Examples/SproutCap/README.md) 演示免 Unity、免代码的 GLB 外观。 |
| 制作会眨眼和摆尾的角色皮肤 | [直立鼠](Examples/UprightMouse/README.md) 演示 GLB 身体挂接、眨眼形状键和尾巴骨骼。 |
| 加入能在工作台制作的服装 | [青苔魔法套装](Examples/WitchRobe/README.md) 添加魔法袍和帽子，沿用游戏的穿脱流程。 |
| 为角色皮肤和服装模组提供支持 | [外观模组框架](Examples/AppearanceFramework/README.md) 提供「外观库」，让玩家选择外观，并在同一个房间里同步。 |
| 改变画面风格 | [复古滤镜](Examples/RetroFilters/README.md) 演示全屏效果和实时调节。 |
| 加入物品、配方和任务 | [远足饼](Examples/TrailTreat/README.md) 演示食物和任务进度。[研究营地](Examples/ResearchCamp/README.md) 演示自制模型和联机任务。 |

交互提示、作物和移动速度等示例，以及各范例的平台和试用方法，见[完整范例索引](Examples/README.md)。

进一步制作时，可以查阅 [工具与版本配套](Tools/README.md)、[编写游戏玩法](Docs/Native-Authoring.md)、[制作模型和物品](Docs/Game-and-Prefab-Guide.md)、[保存进度](Docs/Save-Data.md) 和 [多人游戏](Docs/Networking.md)。需要查原版名称和标识时，用 [物品表](Reference/GameItems.json) 和 [配方表](Reference/GameRecipes.json) 找线索，再以自己的游戏版本为准。

## 常见问题

- **主菜单没有「模组」怎么办？** 先核对你启动的是哪一份游戏。制作和试用范例需要带「模组」菜单并支持 UUID 清单的游戏版本。 [配套游戏与编译](Tools/README.md#直接编译)
- **模组不在列表里，或启用了却没效果？** 确认 Steam 已下载完成，或确认 `mod.json` 在本地作品文件夹的第一层。回到主菜单查看依赖、版本或重启提示。如果仍然有问题，可以查看游戏日志。 [重启与加载顺序](Docs/Reloading.md#应用时机)
- **游戏日志在哪？** 在 Windows 上通常是 `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland\Player.log`，在 macOS 上通常是 `~/Library/Logs/LazyFish Studio/CozyIsland/Player.log`。
- **停用模组后还能继续原来的岛吗？** 如果存档使用了模组新增的物品，缺失物品可能在读档时被清理。用测试岛试验，并保留原存档的备份。 [缺失内容与存档](Docs/Save-Data.md#缺失内容与失败是不同情况)

## 免责声明

- 代码模组会在游戏中运行作者提供的代码。请从信任的来源安装模组，并查看作品适用的游戏版本和使用说明。
- 模组可能彼此冲突，也可能在游戏更新后失效。试用会改变岛屿内容的模组前，请使用测试岛或备份现有存档。 [存档说明](Docs/Save-Data.md)
- 仓库目前尚未声明公开复用许可。转载源码、文档或示例资源前，请以权利人正式提供的许可为准。
