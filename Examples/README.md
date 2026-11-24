# 作者源码示例

第一次制作从[起始上衣换色](ClothingColor/README.md)开始。想把模型带进游戏，可以接着看依赖联机外观框架的[直立鼠外观包](UprightMouse/README.md)。

`mod.json` 是作品清单。在 C# 范例中，`Source/` 存放可编辑的源码，`Code/` 存放编译生成、供游戏加载的 DLL。编译引用来自你安装的配套游戏。纯数据外观包可以按其 README 直接使用。 [编译方式](../Tools/README.md#直接编译)

| 示例 | 目的与依赖 | 实际观察方式 |
| --- | --- | --- |
| [ClothingColor](ClothingColor/README.md) | 用原版材质给本机 Human 的起始上衣和袖子换色 | 穿着起始上衣进岛，就能看到它从绿色变成玫红色 |
| [MinimalCodeMod](MinimalCodeMod/README.md) | 仅依赖加载上下文与 Harmony，修补自己 DLL 的方法 | Player 日志出现原值 21、补丁值 42、更新/场景/清理回调；停用重启用再次从 21 开始 |
| [BuildingHints](BuildingHints/README.md) | 原生 `PCDBuilding` 提示 getter 与 `EasyLocalText` | 拿小木锤聚焦建筑，提示追加建筑名称；停用后恢复，不需真的拆除 |
| [GreenThumb](GreenThumb/README.md) | 原生 `CozyFallowLand.Update` 的房主补丁与自有配置 | 同种作物对比成熟/耗水时间，检查停用后行为恢复 |
| [BriskWalk](BriskWalk/README.md) | 原生玩家和控制器移动字段、自有配置与原值恢复 | 同一路径/输入对比步行距离；奔跑字段是否被具体控制器使用须另行实测 |
| [TrailTreat](TrailTreat/README.md) | 通用登记、原生物品/配方/食用事件、作者 HUD 与新模型 | 原木制作远足饼，食用两次获得吐司；世界进度随主档保存；已验与未验范围见范例 README，作者状态不自动联网 |
| [UprightMouse](UprightMouse/README.md) | 给联机外观框架制作纯数据身体外观包 | 启用框架和数据包后，在 F10 外观菜单选择直立鼠 |
| [RetroFilters](RetroFilters/README.md) | 用 Shader 处理主摄像机画面，借用游戏 UI 调节参数 | 在主菜单切换抖动、像素化、色阶和描边，即时对比效果 |
| [ResearchCamp](ResearchCamp/README.md) | 独立 Unity AssetBundle、多节点营地、两配方、三目标任务、主档数据及作者网络消息 | 0.1.2 同包选择 Windows/macOS 资源，旧清单下的读档、玩法和工坊测试见该范例的历史记录 |

玩法范例展示如何在 C# 代码中使用游戏已有的功能。玩法写在作者 DLL 中，加载器不增加建筑提示、园艺倍率、移动倍率或食物奖励接口。TrailTreat 的正式玩法不调用游戏私有方法，不包含验证驱动；普通输入、多人与任务保存的具体边界见其 README。原生方法/字段可能随游戏更新改变；重新导入 Player 程序集并核对行为。

在游戏 `Mods` 下为示例创建一个子目录，复制 `mod.json` 和该范例说明中列出的资源。C# 范例还需要复制 `Code/` 下的 DLL。切回游戏后，在模组列表手动开启。若“打开作品文件夹”打开已有作品，新增示例应放在其上一级 `Mods`。修改 C# 后先编译，再复制运行文件；修改已加载 DLL 必须完整退出游戏。

各示例的 README 记录实际验收范围。涉及玩法请使用测试存档，按[原生创作验收表](../Docs/Native-Authoring.md)记录结果。复制范例做新作品时，为作品生成新的 UUID。C# 项目还需要独立的程序集名和对应的代码入口。使用同一作品的测试副本或发布更新时保留原身份。 [清单身份与副本](../Docs/Manifest.md#身份与副本)
