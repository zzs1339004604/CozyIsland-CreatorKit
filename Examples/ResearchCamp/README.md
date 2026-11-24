# 岛屿研究营地 / Island Research Camp

这是 Cozy Island Modding 的独立作者范例。**0.1.2** 的标识名为 `fresh-eye.research-camp-20261104`，入口为 `FreshEye.ResearchCamp.CampMod`。UUID 见本目录的 `mod.json`。源码、可编辑的 Unity 资源工程以及 Windows 和 macOS 的 Bundle 随工具包提供。DLL 由作者用配套 Mono Player 的本机引用编译。

[Steam 工坊实验作品](https://steamcommunity.com/sharedfiles/filedetails/?id=3833673375) 保持**私密**；账号之外可能无法访问。0.1.0 首次上传和 0.1.1 同项更新均有本次 `success=true/result=OK` 回执；它不是公开发布的正式兼容承诺。

## 使用

在原生“基础工作台”的“物体”分类中制作：

| 物品 | 材料 | 行为 |
| --- | --- | --- |
| 研究口粮 | 1 原木 | 原生拾取、背包、食用；增加 4 点体力 |
| 研究营地蓝图 | 2 原木 | 原生拾取、进入建造模式、选择位置和确认；实际营地使用作者模型 |

研究营地有桌面、四条桌腿、地图、样本罐、旗杆、旗帜和罗盘。它保留原生工作台、碰撞、保存、联网与拆除组件；与营地交互仍打开原生工作台。蓝图预览当前沿用原版工作台外形。**实验验证了蓝图拾取和进入建造模式，但默认测试光标位置存在碰撞，未完成最终落地；不能把营地的原生生成/冷读验证替代完整玩家放置验证。**

三个小目标是制作一次口粮、食用一次口粮、访问一次营地。全部完成后，房主在触发位置生成一次原版吐司奖励。F8 显隐英文任务 HUD；当前未接入游戏可重新绑定的输入项目，HUD 也未接入游戏多语言表。

## 从源码构建

本示例位于仓库的 `Examples/ResearchCamp`。在下载目录的根目录执行：

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory 'Windows游戏根目录或Mac游戏.app的绝对路径'
./Tools/Build-CodeMod.ps1 -ProjectPath './Examples/ResearchCamp/Source/ResearchCamp.csproj'
```

`Source/ResearchCamp.csproj` 引用 `../../../Tools/CozyMod.props`，显式排除 `UnityProject/**/*.cs`，因此 Unity Editor 构建脚本和其缓存不会混入作者 DLL。构建后得到 `Examples/ResearchCamp/Code/FreshEye.ResearchCamp.dll`。编译引用由导入命令从本机配套 Player 提取。

资源工程为 `Source/UnityProject`。随包 Windows Bundle 沿用历史版的 Unity **2022.3.62f3c1** 产物，Mac Bundle 由 Unity **2022.3.62f2c1** 构建。直接试用范例可以使用随包资源。改了资源后，按 [配套游戏版本说明](../../Tools/README.md#核对配套-build-并导入引用) 核对目标 Player 的实际 Unity 版本，并安装该版本的目标平台 Standalone Build Support。打开资源工程，编辑并保存 `Assets/camp.prefab`、`Assets/ration.prefab` 和材质，关闭 Editor，再执行相应的批处理命令。

```powershell
# Windows x64 Mono Player
& 'Unity.exe 的绝对路径' -batchmode -quit -buildTarget Win64 -projectPath 'Source/UnityProject 的绝对路径' -executeMethod BuildCamp.BuildWindows -logFile '作者日志绝对路径'

# macOS Mono Player
& 'Unity.app/Contents/MacOS/Unity 的绝对路径' -batchmode -quit -buildTarget OSXUniversal -projectPath 'Source/UnityProject 的绝对路径' -executeMethod BuildCamp.BuildMac -logFile '作者日志绝对路径'
```

两条命令使用已经保存的资源。检查进程退出码和日志中的目标平台与 Unity 版本。每次只在成功后替换目标平台的 Bundle，失败时保留现有文件。面向两个平台发布时，从同一份资源源码分别构建 Windows 与 Mac，并把两个产物放进同一运行包。游戏按平台加载对应文件。Steam 更新会替换整个工坊项的内容，因此每次更新都要带齐两个文件。

`BuildCamp.RegenerateExampleAssets` 是另外一个可选入口，会重建原始示例的 Prefab 和材质。请仅在需要恢复原始资源的工程副本中执行。

把下列运行文件放到游戏的 `Mods` 直接子目录。源码留在本地创作工程中。更新使用相同 UUID 的作品时，用完整新包替换原运行包，保留安装目录和已绑定的工坊 ID。

```text
ResearchCamp/
  mod.json
  Code/FreshEye.ResearchCamp.dll
  Assets/researchcamp-win64.bundle
  Assets/researchcamp-macos.bundle
```

启动配套 Player，确认日志中的 `AUTHOR_LOAD` 选择了当前平台的 Bundle。进入测试岛后检查 `CONTENT_READY`、两条 `RECIPE_READY`、实际材质、配方和存档冷读。

## 内容、存档与网络

创建独立派生作品时，从下载目录的根目录运行 `./Tools/New-CodeMod.ps1 -ProjectName my-camp -DisplayName '我的营地' -Author '作者名'`。保留新项目生成的 `uuid` 和程序集名，按下面步骤迁移实现。

1. 把 `Source/CampMod.cs` 和 `Source/UnityProject` 复制到 `MyMods/my-camp/Source`。删除脚手架的 `ModEntry.cs`，将 `CampMod.cs` 的命名空间改为自己的命名空间，并把 `mod.json` 的 `code.type` 更新为该命名空间下的 `CampMod`。保留脚手架的 `code.assembly`，并设置 `requiresRestart: true`。
2. 在新项目的 `Source/Mod.csproj` 中加入下面的排除规则。它让普通 .NET 编译跳过独立 Unity 工程里的 Editor 脚本。

   ```xml
   <ItemGroup>
     <Compile Remove="UnityProject/**/*.cs" />
   </ItemGroup>
   ```

3. 为自己的配方选择唯一 `productName`，按实际 Player 核对原生工作台名和材料键。在 `BuildCamp.cs` 和加载代码中同步换成派生作品自己的 Bundle 文件名，再按上节构建资源并执行 `./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/my-camp/Source/Mod.csproj'`。初次测试可以先只启用派生作品。

更新同一作品时保持原身份、局部物品 ID 和已确认的工坊数字 ID。历史范例的私密工坊项只用于记录本范例的验证。

代码在 `OnContentReady` 同步登记三个稳定身份：`field-ration`、`camp-blueprint`、`survey-camp`。使用当前 Player 的原生吐司、基础工作台与蓝图作为行为模板；Bundle 只承载原创视觉层级和 Shader。作者将视觉子节点附到自有模板，并为新增 GameObject/组件加入固定正数的 `ES3Prefab.localRefs`。蓝图和营地建立双向引用。

0.1.2 将旧版的局部引用 ID 固定在 `CampVisualNodes` 和 `RationVisualNodes`。每一行使用相对于视觉根的路径和已保存的 GameObject ID。该 ID 加一为 Transform ID，有网格时再依次保留 MeshFilter 和 MeshRenderer 的 ID。节点的遍历顺序不参与编号。重排兄弟节点无需修改表。重命名或移动同一个节点时只改路径，保留原 ID。新增节点或组件时分配从未使用过的 ID，并补齐映射。移除旧节点或组件前需先设计兼容方案。登记阶段会拒绝缺失和未映射的组件，避免游戏在含有错误引用的情况下进岛。调整层级后，用副本冷读含原营地或口粮的旧档。

世界任务用 `RegisterSaveData(2, ...)` 与主存档一起保存，包含三个计数与已领奖布尔值。v1 的 `made/ate/visits/claimed` 转成 v2 的 `crafted/eaten/visited/rewarded`；未知版本拒绝恢复。恢复回调不发奖励。跨世界偏好单独写入 `Application.persistentDataPath/<Mod UUID>/preferences.json`，作品不改加载器的启用配置。当前原生游戏的岛屿保存默认位于 `Application.dataPath/SaveFiles`，与用户偏好位置不同。

两条配方会一起检查并注册。发生冲突或任意一条被拒绝时，本次已添加的配方会一起撤销。房主是世界状态唯一写入者。同包客机每两秒发协议 1 的 Hello，请求 JSON 快照。作者显式注册普通 Mirror 消息的读写委托，无自定义 NetworkBehaviour。房主只把作者快照发给主动 Hello 的连接，已暂停的 Mod 停止处理这两类消息。

## 已测范围与边界

以下玩法、跨平台冷读与工坊记录均来自切换为 UUID 清单前的游戏构建。新版清单的物品和任务归属基于 UUID。请保留旧测试档，并用新测试岛验证当前运行包。 [旧包更新说明](../../Docs/Workshop.md#更新旧清单的作品)

**0.1.2 Mac 配套复验：** 用 0.1.1 原 DLL 和 Windows Bundle 在旧 Player 中生成旧档，包含营地、口粮、蓝图、依附在视觉子节点上的原版物件、背包中的 3 份口粮以及 1/1/1 已领奖的任务状态。用新源码编译的范例在配套 Mac Player 中冷读该档。原生 arm64 与 Rosetta x86_64 均恢复上述数据，选择 Mac Bundle 并另存成功。另行把罗盘移到桌子之前，旧档仍将原版物件挂到原本的桌子和口粮罐。预占蓝图配方名、拒绝第二条配方两种故障均留下零条范例配方，保留原版与其他作品配方，已恢复的任务数据仍可保存。测试驱动留在验收环境，本轮未更新私密工坊项。

所有玩法测试在独立产品数据中执行，测试驱动与作品分离。系统防火墙遮罩阻挡鼠标输入，未修改系统安全设置，**以下主要证据是 Player 中的原生 API 驱动，不是玩家 UI 自动点击通过**。营地 Shader 和模型另有真实世界中的离屏证据相机验证，不冒充玩家视角。

**0.1.0 完整实验：** 两配方制作成功；口粮入包、取出和真实原生食用事件，体力 0→4；营地交互；任务完成时原版吐司数量 0→1，再次访问保持 1；保存 A/B 后完整退出冷读，A 恢复 1/0/0 未领奖且背包口粮 1，B 恢复 1/1/1 已领奖。营地 21 个组合层级节点和两种物品身份保留。正确关闭 Host 并执行原生退场后，同一进程进入新世界恢复零进度；HUD 偏好独立保留。

v1 迁移通过作者回调注入旧 JSON、拒绝未知版本、正常保存 v2 并冷读验证；没有声称一个实际历史 v1 主档冷读已通过。停用读档清理 3 个 Mod 物体、保留原生吐司，原档备份哈希相同；另存后重新启用，任务 JSON 恢复，已移除物体没有复活。

同一台电脑的两个独立进程通过实际 **KCP / 127.0.0.1:27769** 通信：同包晚加入客机看到作者物体和房主任务快照；换成缺包新客机仍看见两位原生玩家，房主物体与状态保持并能保存。远程 Steam 双机、远程玩家操作、恶意请求测试与完整网络压力测试未做。单进程 Steam Host 使用 FizzyFacepunch；它不等于远程 Steam 通过。

**0.1.1 定向复验：** 后续作者代码审阅指出部分初始化清理缺口，作者将 Active 与成功取得的 Bundle 立即登记 cleanup。用真正缺少营地 Prefab 的测试 Bundle 触发 OnLoad 失败，确认无残留 Bundle 且 Active 为空；恢复正常资源后，旧 A 档可冷读，制作/食用/营地/奖励/保存通过。对同一私密工坊项更新后重新下载，关闭本地副本，确认三运行文件哈希相同；下载版加载 0.1.1 新档后状态为 1/1/1/已领奖，营地 21 节点、体力 4、吐司 1，再次交互吐司仍为 1。0.1.1 未重跑 0.1.0 的整个多人/卸载矩阵。

## 资源文件身份

| 文件 | SHA-256 |
| --- | --- |
| `Assets/researchcamp-win64.bundle`（0.1.1 的 Windows Bundle 原始字节） | `AA09B0A988CF715A1FB25132017E6394EBBB0025DBFF76BBC9CE784B091B4FA9` |
| `Assets/researchcamp-macos.bundle` | `2848B4EE6DE20F03D2BDAD68A83CA2297FC71E0724DFD426DB760094897343D2` |

历史 0.1.0 完整矩阵使用的 DLL 为 `D4D67DC1C6A136E341F1E0A00F36F5E7F89785D827A0E017ECA4B110493D962D`。历史 0.1.1 私密工坊下载的 DLL 为 `11383A026EEA86ECD87DD7FF70AACBC3696F28F7E4AB3A7FE554F4B6404491FA`。
