# 《舒舒服服小岛时光》Mod 创作指南

这是《舒舒服服小岛时光》（Cozy Island，Steam App ID `2918500`）的独立创作者工具包。准备配套的完整 Mono Player 和本目录，再安装 PowerShell 与 .NET SDK，即可制作外部 C# Mod。作者无需游戏的 Unity 源工程。制作 AssetBundle 等资源时另需 Unity Editor。

先按[工具说明](Tools/README.md)核对 [PlayerIdentity.json](PlayerIdentity.json) 中的配套源码版本和程序集指纹。本版配套 macOS Universal Build，包含 Intel（x86_64）和 Apple Silicon（arm64）。文中的 Windows 路径是工具用法示例，应以实际使用的 Windows Build 重新验证同一流程。清单 `schemaVersion` 只描述格式。早期工坊 Build 即使能编译代码，也可能不读取 `UniqueID`。

基础层提供加载生命周期、共享 Harmony、注册归属和保存/联网协调。作者在自己的 DLL 中用原生游戏 API 实现物品、配方及行为。`mod.json` 存储作品元信息、依赖与代码入口。

## 工坊与加载基础

### 玩家如何安装和管理

1. 从游戏主菜单“模组 → 前往 Steam 创意工坊”打开[工坊页面](https://steamcommunity.com/app/2918500/workshop/)，订阅作品，等待 Steam 完成下载。
2. 打开游戏“模组”窗口。打开菜单、切回游戏及 Steam 安装事件会触发扫描；新作品默认关闭。
3. 选择作品查看说明，勾选要运行的作品。拖动左侧序号手柄预览顺序，松开后保存；靠近列表边缘会自动滚动，Esc 或拖出列表松开会取消。必要依赖自动排在前面。
4. 按提示进入岛屿或完整退出游戏后重新启动。更新已加载 DLL、不可热切换作品或清理失败时，当前运行组合保留到重启；勾选表示已保存的选择，未必是本进程正在运行的组合。

玩家在 Steam 页面取消订阅。相同 Mod ID 的本地、工坊和不同版本副本可以同时安装。启用其中一份会在同一次配置保存中停用其他已识别为同 ID 的副本，详情会显示各副本的作者、版本和安装位置。订阅回测时选择工坊副本，再确认实际运行的版本。需要重启的切换按菜单提示完成；历史配置已有多个同 ID 副本同时开启时，先在菜单保留一份，再进入岛屿。

启用状态、顺序和本机发布关联保存在 `Application.persistentDataPath/mods-<SteamID>.json`，与 `Mods` 文件夹同级。Windows 默认根目录为 `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland`；没有有效 Steam 会话时使用独立的 `mods-local.json`。上次写入备份为 `.json.bak`。启用状态按本地目录名或工坊数字 ID 记录。改安装目录名、换账号或换副本后，先在模组列表重新选定、启用并确认生效，再读含该作品物件的旧档，避免触发缺失内容清理。

### 无法启用时的恢复

本机已启用作品仍在下载/更新，或清单、依赖、初始化失败时，暂时不能进入岛屿，避免临时故障被当作卸载。Steam 加入会静默等待仍在进行的扫描或订阅下载。期间只保留最新加入请求，条件就绪后继续。遇到准备失败或需要重启时，先按界面处理；新建、读档或局域网加入需准备好后重试。主动停用后按缺失内容规则继续。若坏 Mod 导致启动即失败，在 Steam 的游戏“属性 → 通用 → 启动选项”填入 `--disable-mods` 再启动。恢复模式只扫描和管理作品，不执行作者 DLL，也不进入岛屿；原启停、排序和发布关联保留。停用问题作品后退出，移除该启动选项，再正常启动。不要为恢复直接删除整份配置。

### 作者目录与操作顺序

| 目录 | 用途 |
| --- | --- |
| 工具包中的 `MyMods/<作品名>` 或自己的源码目录 | 维护清单、源码、资源和版本记录；不会自动同步到游戏目录 |
| 游戏“模组 → 模组创作 → 打开作品文件夹” | 本地测试和上传目录；每个作品是 `Mods` 下的直接子目录，根部必须有 `mod.json` |
| `<Steam 库>/steamapps/workshop/content/2918500/<工坊 ID>` | Steam 管理的订阅安装；不要在这里维护源码或编辑作品 |

“打开作品文件夹”在有选中作品时打开其目录，没有作品时打开 `Mods` 根目录。新增作品应放到上一级 `Mods`，与已有作品并列。游戏不提供导入或手动刷新按钮；放好文件后切回游戏自动扫描。源码工程更新后，作者负责复制需要测试的运行文件。

制作流程是编辑和编译 → 复制运行文件到游戏 `Mods` → 启用、校验、测试同一份运行包 → 发布 → 订阅回测。`MyMods` 是源码目录，游戏创作页的「保存修改」只更新选中的安装副本。它会验证并写入标题、作者、版本、介绍和封面，保留代码入口、身份及依赖。用创作页改完后，把要保留的改动同步回源码工程。命令行上传时指向已测的安装副本，或先核对源码目录中的运行文件和已测包内容一致。返回或按 Esc 前先保存要保留的信息。

创作帮助当前打开[Steam 社区指南列表](https://steamcommunity.com/app/2918500/guides/)，不表示已经发布专门教程。本工具包提供完整作者流程。

世界任务进度使用 [Mod 数据与世界存档](Docs/Save-Data.md) 中的基础保存接口；作品资源、用户偏好和世界状态分别存放。

### 第一个代码 Mod

在工具包根目录打开 PowerShell，按平台设置游戏路径。Windows 的 `$game` 指向包含游戏 EXE 和 `*_Data` 的目录，Mac 指向完整的 `.app`。

```powershell
# Windows，替换为手上的 Build 路径
$game = 'D:\SteamLibrary\steamapps\common\Cozy Island'
$player = Join-Path $game 'CozyIsland.exe'

# macOS，使用这两行替换上面的 Windows 路径
# $game = '/绝对路径/CozyIsland.app'
# $player = $game

./Tools/Import-GameReferences.ps1 -GameDirectory $game
./Tools/New-CodeMod.ps1 -ProjectName garden -Title '园艺扩展' -Author 'Alice'
./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/garden/Source/Mod.csproj'
```

导入工具先检查 Player 是否支持 UUID 清单，并报告是否与本版工具包的配套指纹匹配。Mac 上正常打开配套 .app 即可，游戏和工具默认使用当前芯片的原生版本。只有在 Apple Silicon 上专门验证 Intel 版本时，才按[工具说明](Tools/README.md)指定 `-Rosetta`。

新建工具在 `MyMods/garden` 生成清单、C# 入口和项目，创建一次 UUID 并写入 `UniqueID`。程序集名和入口根据同一个 UUID 生成。编译后的 DLL 位于 `Code/`，初始代码会在 Player 日志中记录加载。已有同名工程会被拒绝覆盖。

`ProjectName` 是本地工程目录，`Title` 和 `Author` 是显示信息。显示标题允许重名。更新、改名和发布同一作品时保留 `UniqueID` 与程序集身份。独立派生作品先生成自己的 UUID，再迁入所需实现和资源。随包历史范例沿用原 `id`。完整规则见[身份与副本](Docs/Manifest.md#身份与副本)。

需要学习 Harmony 时，另行编译 `Examples/MinimalCodeMod/Source/MinimalCodeMod.csproj`，按[示例说明](Examples/MinimalCodeMod/README.md)检查自己的方法修补。参考复杂范例实现玩法时，将所需代码移入新作品，并调整入口、资源和依赖引用。

游戏提供唯一的 HarmonyX 运行时。使用 `context.Harmony`，只发布自己的 DLL；不要携带游戏、Unity、Mirror、Harmony、MonoMod 或 Cecil DLL。修改已加载 DLL 后完整退出游戏再测试，停用不会从 Mono 进程卸载程序集。生命周期与重启规则见 [Reloading.md](Docs/Reloading.md)。

### 校验、测试和发布

1. 启动上述配套 Build，在「模组 → 模组创作」打开作品文件夹，找到 `Mods` 根目录。关闭游戏，把新工程的 `mod.json`、`Code/` 和所需资源复制到 `Mods/garden`。首个日志范例只需清单和 DLL。
2. 在 PowerShell 把 `$runtime` 设为该安装副本的绝对路径，再校验。

   ```powershell
   $runtime = '<Mods 根目录的绝对路径>/garden'
   ./Tools/Validate-Mod.ps1 -GameExe $player -ModDirectory $runtime
   ```

3. 校验成功后启动游戏，在「模组」列表启用该副本，确认 Player 日志出现作品 ID 和 `Loaded 1.0.0`。校验只检查清单和包结构，入口类型是否存在、DLL 能否实际加载、依赖是否就绪和玩法效果均以实际启用及游玩为准。涉及物品或世界改写时，用独立测试岛检查保存、冷读、停用和重启，参照[原生创作验收](Docs/Native-Authoring.md#验收与证据)。
4. 用游戏发布时，在「模组创作」选中这份已测运行包，核对作者信息后保存。首次发布工坊 ID 留空，未勾选公开时默认私密。更新时已有的本机发布关联会自动显示，未勾选公开会保留工坊当前可见性。需要找回关联时填原作品数字 ID，游戏核对账号所有权和 App ID。需要把现有公开作品改为私密时，使用脚本的显式参数。

使用脚本发布时，退出游戏并保持 Steam 已登录且拥有游戏。校验、测试和上传的 `$runtime` 应是同一份。

```powershell
# 首次默认私密，更新时沿用原可见性
./Tools/Publish-Mod.ps1 -GameExe $player -ModDirectory $runtime

# 明确要求私密时
./Tools/Publish-Mod.ps1 -GameExe $player -ModDirectory $runtime -Visibility private
```

明确希望公开时才传 `-Visibility public`。工具使用已登录的 Steam 会话。JSON 结果和日志默认写入 `Results/`，以本次 `success: true` 且 `result: "OK"` 为提交成功。创建物品得到 ID 后会保留回执，失败时用原作品重试。若超时，先检查报告的进程、日志和工坊页面。账号持有人在 Steam 完成需要的工坊协议。

同一账号、同一 Mod ID 通过本机 `mods-<SteamID>.json` 中的发布记录更新原物品。CLI 没有绑定工坊 ID 的参数；记录丢失时先恢复关联，或在游戏作者页填原 ID 完成已有作品更新。不要凭标题猜测关联。

发布后订阅下载，关闭本地副本或把它移出 `Mods`，对工坊副本重复同一测试。上传 staging 不修改源目录或 Steam 缓存。预览图片小于 1 MiB；运行包最大 256 MiB、4096 个文件；源码、引用和编译缓存不上传。

## 直接使用原生 API 创作

作者可以在自己的 DLL 中调用原生类型或应用 Harmony 补丁。加载器无需为每一种玩法增加专用字段或方法。原生 API 属于游戏实现，签名和语义可能随游戏更新改变；工具包的底座接口负责通用生命周期与归属，并不保证任意原生字段修改自动保存或同步。

| 资料 | 内容 |
| --- | --- |
| [基础 API](Docs/API.md) | `ICozyMod`、上下文、内容准备时机和注册归属接口 |
| [原生 API 创作](Docs/Native-Authoring.md) | 如何查证类型、编写玩法、注册自己的物品与配方、记录阻塞和验证结果 |
| [游戏与 Prefab 指南](Docs/Game-and-Prefab-Guide.md) | 原生组件、模板、身份、引用和资源制作边界 |
| [物品目录](Reference/GameItems.json)、[配方目录](Reference/GameRecipes.json) | 精选物品与配方快照，按实际 Player 核对标识 |
| [示例目录](Examples/README.md) | 最小代码 Mod、建筑名称提示、农田倍率、移动字段修改、新物品与配方 |
| [工具说明](Tools/README.md) | 导入、编译、校验、发布与基础包打包 |
| [AI 创作任务模板](AI-AUTHORING.md) | 给编码工具的范围、证据和发布授权模板 |

先从 [BuildingHints](Examples/BuildingHints/README.md) 学习原生属性 getter Postfix，再参考 GreenThumb 的配置与房主判断、BriskWalk 的原值恢复。示例提供可编辑源码和具体观察步骤，不代表跨电脑联机或所有控制器形态已经通过验证。

[TrailTreat / 远足饼](Examples/TrailTreat/README.md) 是直接扩展原生游戏的新物品范例：稳定登记、原生制作与食用、作者 HUD、新贴图和程序化模型，包含完整源码。它用基础 `RegisterSaveData` 将任务进度与主档同版本保存，提供数据迁移例子，不自动联网同步；具体已验证范围和派生作品需要修改的身份见其说明。

[ResearchCamp / 岛屿研究营地](Examples/ResearchCamp/README.md) 是只使用完整 Player 与指南完成的独立作者范例，包含可重建的 Unity 资源工程、两个配方物品、营地交互、三目标任务及显式 Mirror 消息同步。0.1.2 随附 Windows 和 macOS 资源包，并保留原有作品、物品和旧存档的局部引用身份。范例页记录各版本的验证范围，包括蓝图最终放置、真实 UI 与远程 Steam 的覆盖情况。它没有使用旧 Bridge；多人责任和消息边界见 [作者网络消息](Docs/Networking.md)。

新增可保存或联网的物品需通过注册归属底座取得稳定身份，详见 [API.md](Docs/API.md)。保持 Mod ID 和物品局部 ID 稳定。卸载/更新后缺失的已登记定义会在读档前按已知游戏保存格式清理；原文件默认备份到 `Backups/BeforeModRemoval`，Windows 深路径时使用 API 文档说明的用户数据备份目录。本次用临时副本读取，后续正常保存才产生清理后的版本。重新装回作品不会恢复新存档中已删除的物品。作者自定义序列化状态、地图和协议仍需自己的迁移处理。

世界修改须检查实际房主状态。任务、HUD 和作者字段需要显式设计同步。托管注册物品的缺包容错也不等于任意自定义 C# 网络协议兼容；分别测试房主、远程客机、晚加入、缺包和重连。

## 工具包的独立使用与发布

`ModCreatorKit` 目录可整体移动到任意位置，或作为单独仓库根目录；所有作者指南、源码、项目导入和工具都在该目录内。无需父目录的 Unity 工程或私有仓库。六个范例提供源码、清单和所需资源，第一次使用前按各范例说明编译，DLL 会生成到对应的 `Code/`。编译引用只从作者自己的完整 Player 导入到 `References/Managed`，不随工具包分发。下载或移动工具包后，保留目录内部相对路径；不要复制本机引用、私人工程、缓存或结果日志。

范例依赖已安装 Player 的原生类型，游戏更新后须重新导入引用、编译并实测；编译成功不保证玩法或存档兼容。`Reference` 是带版本和导出时间的历史资料快照，不是实时 API 清单。本工具包当前尚未声明公开复用许可。对源码、文档、图片或 Bundle 的公开分发和复用，请以权利人随正式发行提供的许可为准。
