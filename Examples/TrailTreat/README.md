# Trail Treat / 远足饼

这是 Cozy Island Modding 的原生新物品范例，请使用本工具包配套的 Mono Player。下方历史 Windows 测试使用较早的 Unity 2022.3.62f3c1 构建。目标是展示：从原生食物模板建立独立保存身份，接入原生工作台配方，用原生食用事件完成作者任务，并在作者 DLL 中构造新视觉层级。它不依赖游戏专用 Bridge/Content SDK。

目录应放在创作者包的 `Examples/TrailTreat`。标识名为 `trial.nobridge.trail-treat`，版本 1.1.0，程序集为 `Trial.NoBridge.TrailTreat`，入口 `Trial.NoBridge.TrailTreat`。UUID 见本目录的 `mod.json`。本地副本和工坊副本使用同一个 UUID 时，只启用一份。制作独立派生作品时，先运行 `New-CodeMod.ps1` 创建新 UUID 工程，保留生成的 `uuid` 和程序集名。把 `TrailTreat.cs` 移入新工程的 `Source/`，把 `Assets/trail.png` 复制到新工程同相对位置。移除生成的空入口，把清单 `code.type` 设为移植后的完整入口类名，保留新工程的 `code.assembly` 并按本例设置 `requiresRestart: true`。为 `RecipeName` 选唯一配方键，并更换 `GUI.Window` 的固定窗口 ID（本例为 789312）。更新同一作品时保持作品身份和局部物品 ID。

## 编译与安装

先按创作者包 Tools/README.md，从自己的配套完整 Player 导入编译引用，再在创作者包根目录执行：

```powershell
./Tools/Build-CodeMod.ps1 -ProjectPath './Examples/TrailTreat/Source/TrailTreat.csproj'
```

项目通过 `../../../Tools/CozyMod.props` 引用游戏 Mono/Unity/原生类型和共享 Harmony，生成 `Code/Trial.NoBridge.TrailTreat.dll`。只分发作者自己的 DLL；不要把游戏、Unity、Harmony、Mirror、Cecil 或 References 中的 DLL 放入作品。

将本范例目录复制为游戏用户数据 `Mods` 下的直接子目录，回主菜单勾选启用，然后完整退出并重新启动。范例声明 `requiresRestart: true`，以确保物品定义、事件、资源及主域程序集在冷启动时按保存组合准备。修改已加载 DLL 后必须完整退出进程。

## 普通玩家使用

1. 在测试岛找到**基础工作台**，把 1 根原木放在工作台的材料区域。
2. 打开工作台，在“物体”或“全部”分类选择 **Trial Trail Biscuit**，用游戏显示的普通“制作”操作生成远足饼。
3. 按游戏显示的拾取操作拿起，或拾取到背包；拿在手中时按游戏显示的食用操作吃掉。原生食物行为恢复 3 点体力。
4. 吃两块完成远足补给任务，在玩家附近生成 1 块**原版吐司**作为一次性奖励。后续食用不重复领取。
5. F8 显示或隐藏作者任务 HUD，查看食用进度及奖励状态。

本目录没有场景准备、赠送材料、自动操作或测试入口。测试岛仍使用正常游戏获取材料、制作与食用；不会由范例替玩家建岛、定位角色或发测试物品。

## 源码与原生接入

生产玩法全部在 `Source/TrailTreat.cs`。`OnLoad` 注册生命周期回调、托管 HUD 和本 owner 的 Harmony 图标补丁。`OnContentReady` 中，通过实际 Player 的 `ES3ReferenceMgrBase.Current.GetPrefab(2773636279, false)` 取得吐司模板，再调用通用 `CozyModRegistry.RegisterPrefab`。游戏使用本范例的 UUID 和稳定局部 ID `trail-biscuit` 计算出保存 ID `4898462272259585933`。

模板准备时，原生配方管理器可能尚未执行 Awake/网络生成。因此模板在同步关口完成，`OnUpdate` 等待真实 `CozyWorkshopRecipeMgr` 出现，再按当前管理器实例幂等创建 `CozyRecipe` 并检查 `AddRecipe` 返回值。只有成功插入才记录配方归属；清理前核对管理器按名称查得的对象仍为自己的实例，避免注册冲突或后来被替换时删除别人的配方。配方使用真实键 `基础工作台`、分类 `物体`、材料资源名 `原木`；`productName` 是本范例唯一查找键，不是物品身份。

任务监听公开 `EasyEvent.RegisterCallback("EatFood", ...)`。原生事件载荷 `EventFinishInfo.finisher` 是食用者，`infoData` 是被食用物品 GameObject；代码核对本作品物品身份且检查 `context.IsHost`，然后推进任务并通过原生 `SpawnMgr` 生成已存在的吐司奖励。通过 `RegisterSaveData(2, ...)` 恢复并校验当前世界状态后才建立食用订阅；食用事件只修改房主内存，正常保存时与物品、奖励一起提交。底座故障暂停会保留资源到退出世界，自行订阅的原生事件仍可能触发，因此事件入口同时检查上下文未释放且托管 Root 活跃；不能仅检查 `IsDisposed`。取消订阅使用返回 token 的 `RemoveSelf()`。范例用共享 `context.Harmony` 后补丁替换本物品的原生背包图标，不携带另一份 Harmony。

**生产玩法没有反射调用原生私有方法。** 独立验证时另有作者测试驱动调用私有 `CozyWorkshopMgr.CreateProductWithRecipe` 与 `SaveFileUIMgr.ContinueSteamGameStartHost` 来准备和推进真实流程；该驱动及其 DLL、脚本和日志均不属于本范例。普通玩家不需要这些私有调用。

## 外部贴图、动态模型与保存引用

`Assets/trail.png` 是自制 128×128 PNG，运行时由上下文载入 Sprite/Texture。几何体由作者代码在运行时构造：三个八角夹心层和四个独立罗盘块，共 7 个新 Mesh、80 个新顶点。原吐司 Renderer 被隐藏，原生碰撞、手持、食物和网络组件继续使用；新几何体不是换色。此路线没有 AssetBundle、骨骼或动画。

代码只改登记后的自有模板副本。其原生 ES3 引用表有 4 项；新增的 GameObject、Transform、MeshFilter、MeshRenderer 按固定创建顺序加入 32 个稳定正 ID，范围为 9100001–9100032，合计 **36 项 localRefs**。这个数量是本范例的实际结构，不是所有 Prefab 的通用规则。实例共有 17 个 Transform 节点，冷启动读回以及同机跨进程原生网络生成均观察到相同层级。

每次内容准备先 `FindPrefab` 避免重复登记；每个新场景管理器分别接入配方。不要调用 Player 中为空实现的 `GeneratePrefabReferences()` 来推断引用已准备。新网格/材质由作者明确持有并在清理中销毁，纹理/Sprite 由上下文管理；自建 HUD 组件位于托管 Root 下。正常清理只移除自己创建的配方与事件，不修改源模板或全局共享材质。

## 任务保存与联网范围

范例 1.1.0 使用 `context.RegisterSaveData(2, CaptureProgress, RestoreProgress)`，不再读写岛屿任务侧文件。`eaten/rewarded` 是运行时状态，版本 2 的 JSON 字段为 `biscuitsEaten/rewardClaimed`；恢复函数演示迁移版本 1 的 `eaten/rewarded` 结构。版本 1 指同一基础保存区中的旧格式示例，不是旧 1.0.x 侧文件已迁入主档。旧侧文件没有对应世界版本，不能安全推断归属，因此不自动导入。

新世界或旧档无此数据时初始化为空；读取旧存档时恢复那个版本的进度和奖励标志。食用时不提前写盘，不在捕获/恢复回调中生成奖励。主动停用时物品按登记规则清理，JSON 进度保留；重装能恢复进度，但不能补回清理后另存版本中已删除的物品。详细错误、迁移、大小和备份边界见 [基础存档契约](../../Docs/Save-Data.md)。

范例只在房主推进任务，进度按世界共享，不是每玩家任务。客机 HUD 仍不能代表房主进度。正式多人任务须由作者补充权威同步。基础存档接口不代替网络协议。

## 验证范围与限制

本范例的三文件运行包已在 Windows Steam 测试构建 `26887228` 通过游戏内置校验。

以下是 1.1.0 在 Windows Mono Player（Unity 2022.3.62f3c1）中的测试记录。该范例当时重新编译，并在独立进程调用原生制作、拾取、入背包和食用流程，验证两次食用的一次性奖励。自动化由本地外部驱动调用原生操作，不等同于完整手工 UI 导航；驱动不属于本范例，也未随工具包分发。

任务保存已实际验证两份世界存档：A 为 1/2、未领奖，B 为 2/2、已领奖；完全退出后分别读取，世界物品和任务状态随所选版本恢复。主动停用后，Mod 物品和背包引用被清理，原始存档及备份字节一致；另存仍保留不透明任务 JSON。重新启用并冷读另存版本，任务恢复为 2/2，已清理的物品没有重新出现。框架另有捕获、迁移、序列化和提交失败的故障注入回归，不能据此宣称所有第三方作者代码都安全。

1.1.0 已重新私密上传并由 Steam 下载，作者 DLL SHA-256 与上传源一致；关闭本地玩法副本后，从实际工坊目录再次完成制作、食用、奖励、主档保存及完整退出冷读。另以版本 1 数据区的独立测试副本验证冷读迁移：恢复 1/2、未领奖，正常保存生成版本 2，原版本 1 文件保留。未知版本拒绝及失败恢复后禁止捕获已做定向回归。发布与下载来源必须核对，不能把仍在内存中的旧 DLL 当作更新成功；下载完成后完整退出再启动。

同机两个独立进程验证了同包晚加入与模型生成，以及缺少本地玩法包/未加载该定义的客机晚加入、动态生成、重连。Server → Client 未知背包 ID 不使物品 UI 解析失败，快捷栏未知位置留空、原版位置不前移，房主背包和绑定数据不被过滤。此测试使用原生 TCP 回环传输；不是远程 Steam 双账号验收。共享 Steam 缓存里可能仍有禁用的旧包，因此也不把它称为完全独立机器的物理缺包测试。

原生场景的 CozyBranchenSlot 网络层级在同包和缺包测试中均有 Mirror 初始化异常，日志不全干净。历史验收 Player 修过原版吐司缺少作物组件时的空引用。当前配套游戏仍有这条原生回调路径，异常会按组件记录并继续保存。应在实际所用 Build 中核对 Player 日志与冷读结果。

历史 1.0.0 曾完成侧文件版玩法及工坊回测，1.0.1 仅做配方归属、错误状态与暂停事件的定向修订；这些结果不替代 1.1.0 的主档数据契约验证。旧侧文件没有安全迁移依据，继续保留“不自动导入”的规则。

仍需验收：完整手工导航、远程 Steam 双账号同包/缺包、客机任务进度协议。AssetBundle、骨骼、动画、自定义 NetworkBehaviour、新场景及任意跨玩法版本迁移也未由本例证明。扩展作品时按 [基础存档契约](../../Docs/Save-Data.md) 和 [网络边界](../../Docs/API.md#保存联网与失败时的生命周期) 重新测试。
