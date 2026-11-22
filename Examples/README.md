# 作者源码示例

所有范例先用配套 Player 导入引用，再用各自的项目构建 DLL。独立仓库和 Foundation ZIP 提供范例源码、运行清单和资源。

本目录包含《舒舒服服小岛时光》配套 Mono Player 的六个外部 C# Mod。每个文件夹是独立作品，根部有 `mod.json`；`Source` 是作者源码，`Code/*.dll` 是实际加载文件。先从自己的完整游戏 Build 导入引用并编译，见[工具说明](../Tools/README.md)。不需要游戏 Unity 源码工程。

| 示例 | 目的与依赖 | 实际观察方式 |
| --- | --- | --- |
| [MinimalCodeMod](MinimalCodeMod/README.md) | 仅依赖加载上下文与 Harmony，修补自己 DLL 的方法 | Player 日志出现原值 21、补丁值 42、更新/场景/清理回调；停用重启用再次从 21 开始 |
| [BuildingHints](BuildingHints/README.md) | 原生 `PCDBuilding` 提示 getter 与 `EasyLocalText` | 拿小木锤聚焦建筑，提示追加建筑名称；停用后恢复，不需真的拆除 |
| [GreenThumb](GreenThumb/README.md) | 原生 `CozyFallowLand.Update` 的房主补丁与自有配置 | 同种作物对比成熟/耗水时间，检查停用后行为恢复 |
| [BriskWalk](BriskWalk/README.md) | 原生玩家和控制器移动字段、自有配置与原值恢复 | 同一路径/输入对比步行距离；奔跑字段是否被具体控制器使用须另行实测 |
| [TrailTreat](TrailTreat/README.md) | 通用登记、原生物品/配方/食用事件、作者 HUD 与新模型 | 原木制作远足饼，食用两次获得吐司；世界进度随主档保存；已验与未验范围见范例 README，作者状态不自动联网 |
| [ResearchCamp](ResearchCamp/README.md) | 独立 Unity AssetBundle、多节点营地、两配方、三目标任务、主档数据及作者网络消息 | 0.1.2 同包选择 Windows/macOS 资源，Mac 验证旧 0.1.1 主档的进度、物品及引用可恢复。历史玩法和工坊测试见各版本记录 |

除最小代码示例外，其余示例展示作者直接使用游戏实现类型。玩法写在作者 DLL 中，加载器不增加建筑提示、园艺倍率、移动倍率或食物奖励接口。TrailTreat 的正式玩法不调用游戏私有方法，不包含验证驱动；普通输入、多人与任务保存的具体边界见其 README。原生方法/字段可能随游戏更新改变；重新导入 Player 程序集并核对行为。

把完整示例文件夹作为游戏 `Mods` 的直接子目录放入，切回游戏自动扫描，再在模组列表手动开启。若“打开作品文件夹”打开已有作品，新增示例应放在其上一级 `Mods`。修改 C# 后先编译，再复制运行文件；修改已加载 DLL 必须完整退出游戏。

表格是示例概览；每个 README 分别记录具体版本的已验与未验范围，不能推断所有 Player 或多电脑联机均已通过。涉及玩法请使用测试存档，按[原生创作验收表](../Docs/Native-Authoring.md)记录实际结果。创作独立派生作品时，先用 `New-CodeMod.ps1` 生成自己的 `UniqueID`、程序集名和工程，再移入所需源码与资源，移除生成的空入口或让它调用真实实现，并校对清单 `code.type`。重名的配方键和自定义联网协议也要处理。保留历史范例的原身份供回测。使用同一作品的测试副本或发布更新时则保持原身份。发布时只携带运行包文件。
