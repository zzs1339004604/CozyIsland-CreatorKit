# Cozy Island Modding 工具说明

普通 C# 模组只需要配套游戏和 .NET SDK。可以在系统终端直接编译，再到游戏里试用和发布。第一次制作可以跟着[默认上衣换色](../Examples/ClothingColor/README.md)操作。要重新制作 AssetBundle，再安装与游戏配套的 Unity 编辑器和目标平台构建模块。

## 直接编译

在仓库根目录打开系统终端。把命令里的路径换成自己的 `Managed` 目录。Windows 上可以在 Steam 的 **管理 → 浏览本地文件** 找到游戏，再打开 `CozyIsland_Data/Managed`。macOS 上从 `CozyIsland.app/Contents/Resources/Data/Managed` 找到同一目录。

```text
dotnet build ./Examples/ClothingColor/Source/ClothingColor.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/Cozy Island/CozyIsland_Data/Managed"
```

范例把自己的 DLL 写到 `Code/`。`Managed` 只供编译，游戏依赖由玩家自己的游戏提供。按范例说明把 `mod.json`、`Code/` 和需要的资源放进本地 `Mods`，然后到游戏里[启用和发布](../Docs/Workshop.md)。游戏更新后，使用最新版安装目录重新编译并进岛试用。

## 可选的 PowerShell 工具

使用下列工具时安装 PowerShell 7。兼容性报告适合排查游戏版本，命令行校验和发布适合自动化。日常创作流程见[直接编译](#直接编译)。

| 脚本 | 适用场景 |
| --- | --- |
| `New-CodeMod.ps1` | 批量或重复创建作者工程，生成 UUID、程序集和入口。手工创建时可以从范例复制。 |
| `Build-CodeMod.ps1` | 编译已导入的引用快照，顺便核对快照指纹。直接使用游戏安装目录时用 `dotnet build` 即可。 |
| `Import-GameReferences.ps1` | 保存一份经过游戏探测的引用快照，便于固定和复查某个游戏版本。 |
| `Test-GameCompatibility.ps1` | 让选中的游戏读取测试清单，并和已登记的构建指纹比较。 |
| `Validate-Mod.ps1` | 运行游戏的包校验并留下机器可读回执。 |
| `Publish-Mod.ps1` | 在 Steam 会话中自动上传，或显式调整现有作品的可见性。 |
| `Invoke-ModOperation.ps1` | 供校验、发布和兼容性探测共用。负责启动游戏、超时和读取回执。 |
| `PlayerLayout.ps1` | 供上述工具共用。负责定位 Windows 和 macOS 游戏文件及计算指纹。 |
| `Package-ModCreatorKit.ps1` | 维护者为已验收的游戏构建制作完整工具包 ZIP。下载范例可以直接使用 GitHub 的 **Download ZIP**。 |

## 核对配套 Build 并导入引用

在下载目录的根目录（能看到 Tools 和 Examples 的那一层）打开 PowerShell 7。根据平台设置路径：

```powershell
# macOS：使用整个 .app
$game = '/Applications/CozyIsland.app'
$player = $game

# Windows：使用完整游戏目录和其中的 EXE
# $game = 'D:\SteamLibrary\steamapps\common\Cozy Island'
# $player = Join-Path $game 'CozyIsland.exe'

./Tools/Import-GameReferences.ps1 -GameDirectory $game
```

根目录的 [PlayerIdentity.json](../PlayerIdentity.json) 列出本仓库需要的清单能力，并在配套 Player 验收后记录游戏源码提交和文件指纹。游戏界面的版本号和 `schemaVersion: 1` 都不足以判断是否配套。

导入前会启动所选 Player。游戏自己的校验器会读取临时作品的显示名、标识名、UUID 和 DLL 元数据，工具也会比对当前平台的程序集和已登记的 Player 可执行文件 SHA-256。只想检查并查看配套报告时，可以运行 `./Tools/Test-GameCompatibility.ps1 -GameDirectory $game`。报告中 `exact` 表示登记的程序集和可执行文件指纹均匹配配套记录，`unmatched` 表示存在平台记录但哈希不同，`unpinned` 表示该平台尚无指纹。后两种可以继续编写和试验，需要针对实际 Build 重新确认范例、Prefab、存档和联机行为。游戏更新后应重新检查、导入、编译和测试。

导入脚本将 Managed DLL 复制到 `References/Managed`。不要上传这些文件，也不要把其中的 `reference-manifest.json` 发布到 GitHub，它记录了本机路径。随仓库分发的原生物品和配方快照位于单数 `Reference/`。

macOS 使用完整的配套 Universal `.app`，程序集位于 Unity 的 `Contents/Resources/Data/Managed`。Apple 芯片 Mac 默认以原生 arm64 启动，Intel Mac 使用原生 x86_64。在 Apple 芯片 Mac 上需要测试 Intel 分支时，可向调用 Player 的命令传 `-Rosetta`。配套记录的 `architectures` 列出 Universal 包包含的架构。新清单的 macOS 上架包以配套记录中的实际验收结果为准。Windows 需要完整的 Mono 游戏目录，包括 `*_Data/Managed` 和 `UnityPlayer.dll`。关闭同一份游戏的运行进程后再执行脚本。若 Steam 将启动请求转到另一份安装，脚本会报告未收到本次校验结果。此时核对启动日志和实际使用的 Build。

作者命令在独立进程中关闭 Mod 执行。校验和发布处理指定的作品文件，游戏的启用状态仍用于正常启动和进岛测试。

## 新建自己的作品

```powershell
./Tools/New-CodeMod.ps1 -ProjectName garden -DisplayName '园艺扩展' -Author 'Alice'
./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/garden/Source/Mod.csproj'
```

工具生成 `MyMods/garden/mod.json`、`Source/ModEntry.cs` 和 `Source/Mod.csproj`。工具把 `-ProjectName` 写入标识名 `name`，把 `-DisplayName` 写入玩家看到的 `displayName`。创建时生成一次 `uuid` 及对应的程序集名、命名空间和入口。现有同名工程会被拒绝。更新同一作品时继续使用原工程，并保留 UUID、程序集名和已发布物品的局部 ID。独立派生作品先创建新工程再迁入源码和资源。[清单身份规则](../Docs/Manifest.md#身份与副本)

工程目录名以小写字母开头，可含小写字母、数字、横线和下划线，共 3–48 字符，且必须是有效 Windows 目录名。显示名和作者各不超过 100 字符，可以重名。保持 `MyMods/<工程名>/Source` 与 `Tools/` 的相对层级，便于移动工具包。

## 编译并验证运行包

```powershell
./Tools/Build-CodeMod.ps1 -ProjectPath './MyMods/garden/Source/Mod.csproj'
```

项目通过 `../../../Tools/CozyMod.props` 使用目标 Player 的 Mono 框架和 API，目标标识为 `netstandard2.1`、C# 9.0。编译输出到作品的 `Code/`，引用程序集不随产物复制。随包范例也使用这一流程，先编译各自的 `Source/*.csproj` 再安装运行包。已有导入目录时，可通过 `-GameManagedPath` 显式选择。修改已加载的 DLL 后，应完整退出游戏再验证新版本。

把运行文件复制到游戏通过「打开作品文件夹」显示的 `Mods` 目录下，再把 `$runtime` 设置成这份待测和待发布的作品目录：

```powershell
$runtime = '/替换为/Mods/garden'
./Tools/Validate-Mod.ps1 -GameExe $player -ModDirectory $runtime
```

校验检查清单、路径、文件和包结构，即使入口类名拼错，结构校验也可能通过。随后从游戏模组列表启用该副本，观察自己代码的日志和行为。回执返回 `displayName`、`name` 和规范化的小写 `uuid`。游戏内改动作者标题、描述和版本后，校验和发布继续使用该安装副本，或把改动同步回作者工程后重新复制并验证。

## 上传单个作品

关闭目标游戏，保持 Steam 已登录并拥有游戏。私密测试请显式指定：

```powershell
./Tools/Publish-Mod.ps1 -GameExe $player -ModDirectory $runtime -Visibility private
```

省略 `-Visibility` 时，首次发布默认为私密，更新已有工坊作品则保留它的可见性。`-Visibility public` 和 `-Visibility private` 会显式修改可见性。游戏创作页也会在未勾选公开时保留已有项的可见性。要把公开作品改成私密，可使用上面的显式参数。

同一账号、同一 Mod UUID 根据本机发布记录更新原工坊物品。记录丢失时，在游戏作者页输入**原有工坊数字 ID**，核对链接、标题和账号后绑定。游戏核对账号与 App ID，作者仍需要确认选的是同一作品。CLI 不提供绑定参数。

默认将结果和 Player 日志写入 `Results/`，也可使用 `-ResultPath '<新文件.json>'`。相对路径从当前 PowerShell 目录解析。已有结果文件会拒绝覆盖。校验默认超时 180 秒，上传默认 750 秒，可用 `-TimeoutSeconds` 指定 30–3600 秒。超时先查看本次日志、仍在运行的进程及工坊页面。实际发布以本次 `success: true` 且 `result: "OK"` 为准，再订阅下载，切换到工坊副本回测。

## 维护者生成 Foundation ZIP

从干净的 Cozy Island Modding 提交或 Release 标签准备打包。打包器要求所选 Player 能实际读取包含 `displayName`、`name` 和 `uuid` 的清单，且程序集和可执行文件指纹与当前平台的 `release` 或 `release-candidate` 记录一致。当前 [PlayerIdentity.json](../PlayerIdentity.json) 还没有登记配套发行构建，待验收并登记后使用。

```powershell
./Tools/Package-ModCreatorKit.ps1 -GameDirectory $game -OutputPath '../Artifacts/CozyIsland-ModCreatorKit-Foundation.zip'
```

ZIP 收录仓库的 `PlayerIdentity.json`、指南、原生参考快照、作者范例的源码与资源和工具。打包器会排除 `EvaluationSDK`、本机引用、用户工程、范例生成的 `Code/`、结果、编辑器缓存和日志，并按配套 Player 的 Managed 程序集身份检查误带的游戏 DLL。发布时复查 ZIP 条目和授权范围。发行前用实际交付的 Player 验证启用代码 Mod、进岛、存档冷读和工坊流程。需要增加平台或重新构建时，先完成该配套 Build 的验收，再更新身份清单。
