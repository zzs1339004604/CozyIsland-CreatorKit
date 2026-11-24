# 模组范例

第一次制作可以从[起始上衣换色](ClothingColor/README.md)开始。这个范例用游戏已有的模型和材质，可以先改一个颜色，再观察实际效果。

| 想做什么 | 范例 | 怎么试 |
| --- | --- | --- |
| 给起始上衣换色 | [ClothingColor](ClothingColor/README.md) | 穿着默认装扮进岛，本机角色的上衣和袖子会变成玫红色。 |
| 观察代码加载与清理 | [MinimalCodeMod](MinimalCodeMod/README.md) | 从游戏日志查看 Harmony 补丁、更新回调和清理结果。 |
| 给交互提示加文字 | [BuildingHints](BuildingHints/README.md) | 用小木锤对准可拆除的建筑，观察拆除提示中的建筑名。 |
| 调整作物生长和耗水 | [GreenThumb](GreenThumb/README.md) | 用房主角色对比同种作物的生长和耗水。参数在 `garden.json`。 |
| 调整角色移动速度 | [BriskWalk](BriskWalk/README.md) | 在相同路线对比步行速度。奔跑倍率的效果取决于角色控制器。 |
| 给角色换上直立鼠皮肤 | [UprightMouse](UprightMouse/README.md) | 启用外观模组框架和此数据包，按 F10 打开「外观库」并选择直立鼠。附 Windows x64 资源包。 |
| 给画面加复古滤镜 | [RetroFilters](RetroFilters/README.md) | 使用随附的 Windows x64 和 macOS 资源包，在主菜单实时预览，或进岛后按 F9 调整效果。 |
| 添加可以制作和穿戴的服装物品 | [WitchRobe](WitchRobe/README.md) | 在基础工作台制作青苔魔法袍和魔法帽。随附 Windows x64 资源包，联机时各端安装同一版本以显示模型。 |
| 为外观包提供支持 | [AppearanceFramework](AppearanceFramework/README.md) | 外观模组框架提供「外观库」，并按作者的设置在联机房间同步和分享皮肤及服装。 |
| 在 F10 菜单选择青苔造型 | [MossWitchAppearance](MossWitchAppearance/README.md) | 启用框架和此可选纯数据包，在 F10 菜单选择魔法袍或帽子。附 Windows x64 资源包。 |
| 添加食物、配方和任务 | [TrailTreat](TrailTreat/README.md) | 制作并食用两次远足饼，触发吐司奖励。任务由房主推进并存入世界存档。 |
| 添加营地和可保存的联机任务 | [ResearchCamp](ResearchCamp/README.md) | 从营地、口粮配方和三个小目标入手。随附 Windows x64 和 macOS 资源包，同包客机可接收进度。 |

`mod.json` 是作品清单。C# 范例的 `Source/` 存放可编辑的源码，使用配套游戏的程序集编译。将清单、生成的 `Code/` 和所需资源放入游戏的 `Mods` 子目录。纯数据外观包可按各自说明安装，保留清单和资源目录。修改已加载的 DLL 后，完整退出游戏再验证。 [编译方式](../Tools/README.md#直接编译) · [启用和发布](../Docs/Workshop.md)

各范例的 README 记录了实际验收范围。涉及存档或联机的作品可以在测试岛验证。复制范例做新作品时生成新的 UUID，给 C# 项目设置独立的程序集名，并核对清单中的代码入口。更新同一作品时保留 UUID。 [验收方法](../Docs/Native-Authoring.md#验收与证据) · [作品身份与副本](../Docs/Manifest.md#身份与副本)
